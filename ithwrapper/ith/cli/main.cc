// main.cc
// 8/24/2013 jichi
// Branch: ITH_DLL/main.cpp, rev 128
// 8/24/2013 TODO: Clean up this file

#ifdef _MSC_VER
# pragma warning (disable:4100)   // C4100: unreference formal parameter
# pragma warning (disable:4733)   // C4733: Inline asm assigning to 'FS:0' : handler not registered as safe handler
#endif // _MSC_VER

#include "cli_p.h"
#include "avl_p.h"
#include "ith/common/defs.h"
#include "ith/common/except.h"
//#include "ith/common/growl.h"
#include "ith/sys/sys.h"
//#include "md5.h"
//#include <ITH\AVL.h>
//#include <ITH\ntdll.h>
enum { HOOK_BUFFER_SIZE = MAX_HOOK *sizeof(TextHook) };
//#define MAX_HOOK (HOOK_BUFFER_SIZE/sizeof(TextHook))

WCHAR dll_mutex[0x100];
//WCHAR dll_name[0x100];
WCHAR hm_mutex[0x100];
WCHAR hm_section[0x100];
HINSTANCE hDLL;
HANDLE hSection;
bool running,live = false;
int current_hook = 0,
    user_hook_count = 0;
DWORD trigger = 0;
HANDLE hSendThread,
    hCmdThread,
    hFile,
    hMutex,
    hmMutex;
DWORD hook_buff_len = HOOK_BUFFER_SIZE;
//DWORD current_process_id;
extern DWORD enter_count;
//extern LPWSTR current_dir;
extern DWORD engine_type;
extern DWORD module_base;
AVLTree<char, FunctionInfo, SCMP, SCPY, SLEN> *tree;

namespace { // unnamed

void AddModule(DWORD hModule, DWORD size, LPWSTR name)
{
  IMAGE_DOS_HEADER *DosHdr;
  IMAGE_NT_HEADERS *NtHdr;
  IMAGE_EXPORT_DIRECTORY *ExtDir;
  UINT uj;
  FunctionInfo info = {0, hModule, size, name};
  char *pcFuncPtr, *pcBuffer;
  DWORD dwReadAddr, dwFuncName, dwExportAddr;
  WORD wOrd;
  DosHdr = (IMAGE_DOS_HEADER *)hModule;
  if (IMAGE_DOS_SIGNATURE==DosHdr->e_magic) {
    dwReadAddr = hModule + DosHdr->e_lfanew;
    NtHdr = (IMAGE_NT_HEADERS *)dwReadAddr;
    if (IMAGE_NT_SIGNATURE == NtHdr->Signature) {
      dwExportAddr = NtHdr->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].VirtualAddress;
      if (dwExportAddr == 0)
        return;
      dwExportAddr+=hModule;
      ExtDir=(IMAGE_EXPORT_DIRECTORY*)dwExportAddr;
      dwExportAddr=hModule+ExtDir->AddressOfNames;
      for (uj = 0; uj < ExtDir->NumberOfNames; uj++) {
        dwFuncName=*(DWORD*)dwExportAddr;
        pcBuffer = (char *)(hModule+dwFuncName);
        pcFuncPtr=(char *)(hModule+(DWORD)ExtDir->AddressOfNameOrdinals+(uj*sizeof(WORD)));
        wOrd = *(WORD *)pcFuncPtr;
        pcFuncPtr = (char *)(hModule+(DWORD)ExtDir->AddressOfFunctions+(wOrd*sizeof(DWORD)));
        info.addr=hModule+*(DWORD*)pcFuncPtr;
        ::tree->Insert(pcBuffer,info);
        dwExportAddr+=sizeof(DWORD);
      }
    }
  }
}

void GetFunctionNames()
{
  // jichi 9/26/2013: AVLTree is already zero
  PPEB ppeb;
  __asm
  {
    mov eax,fs:[0x30]
    mov ppeb,eax
  }
  DWORD temp = *(DWORD *)(&ppeb->Ldr->InLoadOrderModuleList);
  PLDR_DATA_TABLE_ENTRY it = (PLDR_DATA_TABLE_ENTRY)temp;
  while (it->SizeOfImage) {
    AddModule((DWORD)it->DllBase, it->SizeOfImage, it->BaseDllName.Buffer);
    it=(PLDR_DATA_TABLE_ENTRY)it->InLoadOrderModuleList.Flink;
    if (*(DWORD *)it==temp)
      break;
  }
}

} // unnamed namespace

DWORD IHFAPI GetFunctionAddr(const char *name, DWORD *addr, DWORD *base, DWORD *size, LPWSTR *base_name)
{
  TreeNode<char *,FunctionInfo> *node = ::tree->Search(name);
  if (node) {
    if (addr) *addr = node->data.addr;
    if (base) *base = node->data.module;
    if (size) *size = node->data.size;
    if (base_name) *base_name = node->data.name;
    return TRUE;
  }
  else
    return FALSE;
}
void RequestRefreshProfile()
{
  if (live) {
    BYTE buffer[0x80];
    *(DWORD *)buffer = -1;
    *(DWORD *)(buffer + 4) = 1;
    *(DWORD *)(buffer + 8) = 0;
    IO_STATUS_BLOCK ios;
    CliLockPipe();
    NtWriteFile(hPipe, 0, 0, 0, &ios, buffer, HEADER_SIZE, 0, 0);
    CliUnlockPipe();
  }
}

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
  //static WCHAR dll_exist[] = L"ITH_DLL_RUNNING";
  static WCHAR dll_exist[] = ITH_CLIENT_MUTEX;
  static HANDLE hDllExist;

  // jichi 9/23/2013: wine deficenciy on mapping sections
  // Whe set to false, do not map sections.
  //static bool ith_has_section = true;

  switch (fdwReason)
  {
  case DLL_PROCESS_ATTACH:
    {
      LdrDisableThreadCalloutsForDll(hinstDLL);
      IthBreak();
      module_base = (DWORD)hinstDLL;
      IthInitSystemService();
      swprintf(hm_section, ITH_SECTION_ L"%d", current_process_id);

      // jichi 9/25/2013: Interprocedural communication with vnrsrv.
      hSection = IthCreateSection(hm_section, 0x2000, PAGE_EXECUTE_READWRITE);
      ::hookman = nullptr;
      NtMapViewOfSection(hSection, NtCurrentProcess(),
          (LPVOID *)&::hookman, 0, hook_buff_len, 0, &hook_buff_len, ViewUnmap, 0,
          PAGE_EXECUTE_READWRITE);
          //PAGE_EXECUTE_READWRITE);

      //if (!::hookman) {
      //  ith_has_section = false;
      //  ::hookman = new TextHook[MAX_HOOK];
      //  memset(::hookman, 0, MAX_HOOK * sizeof(TextHook));
      //}

      //LPCWSTR p;
      //for (p = GetMainModulePath(); *p; p++);
      //for (p = p; *p != L'\\'; p--);
      //wcscpy(dll_name, p + 1);
      //swprintf(dll_mutex,L"ITH_%.4d_%s",current_process_id,current_dir);
      swprintf(dll_mutex, ITH_PROCESS_MUTEX_ L"%d", current_process_id);
      swprintf(hm_mutex, ITH_HOOKMAN_MUTEX_ L"%d", current_process_id);
      hmMutex = IthCreateMutex(hm_mutex, FALSE);

      DWORD s;
      hMutex = IthCreateMutex(dll_mutex, TRUE, &s); // jichi 9/18/2013: own is true
      if (s)
        return FALSE;

      hDllExist = IthCreateMutex(dll_exist, 0);
      hDLL = hinstDLL;
      running = true;
      current_available = hookman;
      ::tree = new AVLTree<char, FunctionInfo, SCMP, SCPY, SLEN>;
      GetFunctionNames();
      InitFilterTable();
      InitDefaultHook();
      hSendThread = IthCreateThread(WaitForPipe, 0);
      hCmdThread = IthCreateThread(CommandPipe, 0);
    } break;
  case DLL_PROCESS_DETACH:
    {
      // jichi 10/2/2103: Cannot use __try in functions that require object unwinding
      //ITH_TRY {
      running = false;
      live = false;
      const LONGLONG timeout = -50000000; // in nanoseconds = 5 seconds
      NtWaitForSingleObject(hSendThread, 0, (PLARGE_INTEGER)&timeout);
      NtWaitForSingleObject(hCmdThread, 0, (PLARGE_INTEGER)&timeout);
      NtClose(hCmdThread);
      NtClose(hSendThread);
      for (TextHook *man = hookman; man->RemoveHook(); man++);
      //LARGE_INTEGER lint = {-10000, -1};
      while (::enter_count)
        IthSleep(1); // jichi 9/28/2013: sleep for 1 ms
        //NtDelayExecution(0, &lint);
      for (TextHook *man = hookman; man < hookman + MAX_HOOK; man++)
        man->ClearHook();
      //if (ith_has_section)
      NtUnmapViewOfSection(NtCurrentProcess(), hookman);
      //else
      //  delete[] ::hookman;
      NtClose(hSection);
      NtClose(hMutex);

      delete ::tree;
      IthCloseSystemService();
      NtClose(hmMutex);
      NtClose(hDllExist);
      //} ITH_EXCEPT {}
    } break;
   }
  return TRUE;
}

extern "C" {
DWORD IHFAPI NewHook(const HookParam &hp, LPCWSTR name, DWORD flag)
{
  //WCHAR str[0x80];
  int current = current_available - hookman;
  if (current < MAX_HOOK) {
    //flag &= 0xffff;
    //if ((flag & HOOK_AUXILIARY) == 0)
    flag |= HOOK_ADDITIONAL;
      //if (name == 0 || *name == 0) {
      //  name = str;
      //  swprintf(name,L"UserHook%d",user_hook_count++);
      //}

    hookman[current].InitHook(hp, name, flag & 0xffff);
    if (hookman[current].InsertHook() == 0)
      //OutputConsole(L"Additional hook inserted.");
      //swprintf(str,L"Insert address 0x%.8X.", hookman[current].Address());
      //OutputConsole(str);
      RequestRefreshProfile();

    //else
    //  OutputConsole(L"Unable to insert hook.");
  }
  return 0;
}
DWORD IHFAPI RemoveHook(DWORD addr)
{
  for (int i=0; i<MAX_HOOK; i++)
    if (hookman[i].Address ()== addr) {
      hookman[i].ClearHook();
      return 0;
    }
  return 0;
}

DWORD IHFAPI SwitchTrigger(DWORD t)
{
  trigger = t;
  return 0;
}

} // extern "C"

static int filter_count;
static DWORD recv_esp, recv_addr;
static CONTEXT recover_context;
static __declspec(naked) void MySEH()
{
  __asm{
  mov eax, [esp+0xC]
  mov edi,eax
  mov ecx,0xB3
  mov esi, offset recover_context
  rep movs
  mov ecx, [recv_esp]
  mov [eax+0xC4],ecx
  mov edx, [recv_addr]
  mov [eax+0xB8],edx
  xor eax,eax
  retn
  }
}
EXCEPTION_DISPOSITION ExceptHandler(
  EXCEPTION_RECORD *ExceptionRecord,
  void * EstablisherFrame,
  CONTEXT *ContextRecord,
  void * DispatcherContext )
{
  ContextRecord->Esp=recv_esp;
  ContextRecord->Eip=recv_addr;
  return ExceptionContinueExecution;
}
int GuardRange(LPWSTR module, DWORD* a, DWORD* b)
{
  int flag=0;
  __asm
  {
    mov eax,seh_recover
    mov recv_addr,eax
    push ExceptHandler
    push fs:[0]
    mov recv_esp,esp
    mov fs:[0],esp
  }
  flag = FillRange(module,a,b);
  __asm
  {
seh_recover:
    mov eax,[esp]
    mov fs:[0],eax
    add esp,8
  }
  return flag;
}

void AddRange(LPWSTR dll)
{
  if (GuardRange(dll, &filter[filter_count].lower, &filter[filter_count].upper))
    filter_count++;
}

void InitFilterTable()
{
  filter_count = 0;
  AddRange(L"uxtheme.dll");
  AddRange(L"usp10.dll");
  AddRange(L"msctf.dll");
  AddRange(L"gdiplus.dll");
  AddRange(L"lpk.dll");
  AddRange(L"psapi.dll");
  AddRange(L"user32.dll");
}

// EOF
