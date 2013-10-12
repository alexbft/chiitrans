// texthook.cc
// 8/24/2013 jichi
// Branch: ITH_DLL/texthook.cpp, rev 128
// 8/24/2013 TODO: Clean up this file

#ifdef _MSC_VER
# pragma warning (disable:4100)   // C4100: unreference formal parameter
# pragma warning (disable:4018)   // C4018: sign/unsigned mismatch
# pragma warning (disable:4733)   // C4733: Inline asm assigning to 'FS:0' : handler not registered as safe handler
#endif // _MSC_VER

#include "cli_p.h"
#include "ith/common/except.h"
//#include "ith/common/growl.h"
#include "ith/sys/sys.h"
#include "disasm/disasm.h"

//#define OutputConsole(...)   (void)0    // jichi 9/17/2013: I don't need this ><

FilterRange filter[8];
DWORD flag,
      enter_count;

TextHook *hookman,
         *current_available;

namespace { // unnamed
//provide const time hook entry.
int userhook_count;

const byte common_hook2[] = {
  0x89, 0x3c,0xe4, // mov [esp],edi
  0x60, //pushad
  0x9c, //pushfd
  0x8d,0x54,0x24,0x28, // lea edx,[esp+0x28] ; esp value
  0x8b,0x32,     // mov esi,[edx] ; return address
  0xb9, 0,0,0,0, // mov ecx, $ ; pointer to TextHook
  0xe8, 0,0,0,0, // call @hook
  0x9d, // popfd
  0x61, // popad
  0x5f, // pop edi ; skip return address on stack
}; //...

const BYTE common_hook[] = {
  0x9c,
  0x60, // pushad
  0x9c, // pushfd
  0x8d,0x54,0x24,0x28, // lea edx,[esp+0x28] --- esp value
  0x8b,0x32,     // mov esi,[edx] --- return address
  0xb9, 0,0,0,0, // mov ecx, $ --- pointer to TexHhook
  0xe8, 0,0,0,0, // call @hook
  0x9d, // popfd
  0x61, // popad
  0x9d
}; //...

//typedef void (*DataFun)(DWORD, const HookParam*, DWORD*, DWORD*, DWORD*);
DWORD recv_esp, recv_addr;
EXCEPTION_DISPOSITION ExceptHandler(EXCEPTION_RECORD *ExceptionRecord,
    void *EstablisherFrame, CONTEXT *ContextRecord, void * DispatcherContext)
{
  WCHAR str[0x40],name[0x100];

  //OutputConsole(L"Exception raised during hook processing.");
  swprintf(str,L"Exception code: 0x%.8X", ExceptionRecord->ExceptionCode);
  //OutputConsole(str);
  MEMORY_BASIC_INFORMATION info;
  if (NT_SUCCESS(NtQueryVirtualMemory(NtCurrentProcess(),(PVOID)ContextRecord->Eip,
      MemoryBasicInformation,&info,sizeof(info),0)) &&
      NT_SUCCESS(NtQueryVirtualMemory(NtCurrentProcess(),(PVOID)ContextRecord->Eip,
      MemorySectionName,name,0x200,0))) {
    swprintf(str,L"Exception offset: 0x%.8X:%s",
        ContextRecord->Eip-(DWORD)info.AllocationBase,
        wcsrchr(name,L'\\')+1);
    //OutputConsole(str);
  }
  ContextRecord->Esp=recv_esp;
  ContextRecord->Eip=recv_addr;
  return ExceptionContinueExecution;
}

} // unnamed namespace

//copy original instruction
//jmp back
DWORD GetModuleBase(DWORD hash)
{
  __asm
  {
    mov eax,fs:[0x30]
    mov eax,[eax+0xC]
    mov esi,[eax+0x14]
    mov edi,_wcslwr
listfind:
    mov edx,[esi+0x28]
    test edx,edx
    jz notfound
    push edx
    call edi
    pop edx
    xor eax,eax
calc:
    movzx ecx, word ptr [edx]
    test cl,cl
    jz fin
    ror eax,7
    add eax,ecx
    add edx,2
    jmp calc
fin:
    cmp eax,[hash]
    je found
    mov esi,[esi]
    jmp listfind
notfound:
    xor eax,eax
    jmp termin
found:
    mov eax,[esi+0x10]
termin:
  }
}

//void NotifyHookInsert()
//{
//  if (live)
//  {
//    BYTE buffer[0x10];
//    *(DWORD*)buffer=-1;
//    *(DWORD*)(buffer+4)=1;
//    IO_STATUS_BLOCK ios;
//    NtWriteFile(hPipe,0,0,0,&ios,buffer,0x10,0,0);
//  }
//}

__declspec (naked) void SafeExit() //Return to eax
{
  __asm
  {
    mov [esp + 0x24], eax
    popfd
    popad
    retn
  }
}

__declspec(naked) // jichi 10/2/2013: No prolog and epilog
int ProcessHook(DWORD dwDataBase, DWORD dwRetn,TextHook *hook)
  //Use SEH to ensure normal execution even bad hook inserted.
{
  __asm
  {
    mov eax,seh_recover
    mov recv_addr,eax
    push ExceptHandler
    push fs:[0]
    mov recv_esp,esp
    mov fs:[0],esp
    push esi
    push edx
    call TextHook::Send
    test eax,eax
    jz seh_recover
    mov ecx,SafeExit
    mov [esp + 0x8], ecx //change exit point.
seh_recover:
    pop dword ptr fs:[0]
    pop ecx
    retn
  }
}

namespace { // unnamed
  inline bool HookFilter(DWORD retn)
  {
    for (DWORD i = 0; filter[i].lower; i++)
      if (retn > filter[i].lower && retn < filter[i].upper)
        return true;
    return false;
  }
} // unnamed namespace

DWORD TextHook::Send(DWORD dwDataBase, DWORD dwRetn)
{
  enum { SMALL_BUFF_SIZE = 0x80 };
  DWORD dwCount,
      dwAddr,
      dwDataIn,
      dwSplit = 0;
  BYTE *pbData,
       pbSmallBuff[SMALL_BUFF_SIZE];
  DWORD dwType = hp.type;
  if (!live)
    return 0;
  if ((dwType & NO_CONTEXT) == 0 && HookFilter(dwRetn))
    return 0;
  dwCount = 0;
  dwAddr = hp.addr;
  if (trigger) {
    if (InsertDynamicHook)
      trigger = InsertDynamicHook((LPVOID)dwAddr, *(DWORD *)(dwDataBase-0x1c), *(DWORD *)(dwDataBase-0x18));
    else
      trigger = 0;
  }
  if (dwType & HOOK_AUXILIARY) {
    //Clean hook when dynamic hook finished.
    //AUX hook is only used for a foothold of dynamic hook.
    if (!trigger) {
      ClearHook();
      return dwAddr;
    }
    return 0;
  }
  dwDataIn = *(DWORD *)(dwDataBase + hp.off);
  if (dwType & EXTERN_HOOK) {
    //DataFun fun=(DataFun)hp.extern_fun;
    //auto fun = hp.extern_fun;
    hp.extern_fun(dwDataBase, &hp, &dwDataIn, &dwSplit, &dwCount);
    if (dwCount == 0 || dwCount > 0x10000)
      return 0;
  } else {
    dwSplit = 0;
    if (dwDataIn == 0)
      return 0;
    if (dwType & USING_SPLIT) {
      dwSplit = *(DWORD *)(dwDataBase + hp.split);
      if (dwType & SPLIT_INDIRECT) {
        if (IthGetMemoryRange((LPVOID)(dwSplit + hp.split_ind), 0, 0))
          dwSplit = *(DWORD *)(dwSplit + hp.split_ind);
        else
          return 0;
      }
    }
    if (dwType & DATA_INDIRECT) {
      if (IthGetMemoryRange((LPVOID)(dwDataIn + hp.ind), 0, 0))
        dwDataIn = *(DWORD *)(dwDataIn + hp.ind);
      else
        return 0;
    }
    if (dwType & PRINT_DWORD) {
      swprintf((WCHAR *)(pbSmallBuff + HEADER_SIZE), L"%.8X ", dwDataIn);
      dwDataIn = (DWORD)pbSmallBuff + HEADER_SIZE;
    }
    dwCount = GetLength(dwDataBase, dwDataIn);
  }
  if (dwCount + HEADER_SIZE >= SMALL_BUFF_SIZE) {
    size_t sz = dwCount + HEADER_SIZE;
    pbData = new BYTE[sz];
    memset(pbData, 0, sz); // jichi 9/26/2013: zero memory
  } else
    pbData = pbSmallBuff;
  if (hp.length_offset == 1) {
    if (dwType & STRING_LAST_CHAR) {
      LPWSTR ts = (LPWSTR)dwDataIn;
      dwDataIn = ts[wcslen(ts) -1];
    }
    dwDataIn &= 0xffff;
    if (dwType & BIG_ENDIAN)
      if (dwDataIn >> 8)
        dwDataIn = _byteswap_ushort(dwDataIn & 0xffff);
    if (dwCount == 1)
      dwDataIn &= 0xff;
    *(WORD *)(pbData+HEADER_SIZE) = dwDataIn & 0xffff;
  }
  else
    memcpy(pbData + HEADER_SIZE, (void *)dwDataIn, dwCount);
  *(DWORD *)pbData = dwAddr;
  if (dwType & NO_CONTEXT)
    dwRetn = 0;
  *((DWORD *)pbData + 1) = dwRetn;
  *((DWORD *)pbData + 2) = dwSplit;
  if (dwCount) {
    IO_STATUS_BLOCK ios = {};

    IthCoolDown(); // jichi 9/28/2013: cool down to prevent parallelization in wine
    //CliLockPipe();
    if (STATUS_PENDING == NtWriteFile(hPipe, 0, 0, 0, &ios, pbData, dwCount + HEADER_SIZE, 0, 0)) {
      NtWaitForSingleObject(hPipe, 0, 0);
      NtFlushBuffersFile(hPipe, &ios);
    }
    //CliUnlockPipe();
  }
  if (pbData != pbSmallBuff)
    delete[] pbData;
  return 0;

}
int MapInstruction(DWORD original_addr, DWORD new_addr, BYTE& hook_len, BYTE& original_len)
{
  int flag = 0;
  DWORD l = 0;
  BYTE *r,*c;
  r=(BYTE*)original_addr;
  c=(BYTE*)new_addr;
  while((r-(BYTE*)original_addr)<5)
  {
    l=disasm(r);
    if (l==0) return -1;
    memcpy(c,r,l);
    if (*r>=0x70&&*r<0x80)
    {
      c[0]=0xF;
      c[1]=*r+0x10;
      c+=6;
      __asm
      {
        mov eax,r
        add eax,2
        movsx edx,byte ptr [eax-1]
        add edx,eax
        mov eax,c
        sub edx,eax
        mov [eax-4],edx
      }
    }
    else if (*r==0xEB)
    {
      c[0] = 0xe9;
      c += 5;
      __asm
      {
        mov eax,r
        add eax,2
        movsx edx,[eax-1]
        add edx,eax
        mov eax,c
        sub edx,eax
        mov [eax-4],edx
      }
      if (r-(BYTE*)original_addr<5-l)
        return -1; // Not safe to move intruction right after short jmp.
      else flag=1;

    } else if (*r == 0xe8 || *r == 0xe9) {
      c[0]=*r;
      c+=5;
      flag=(*r==0xe9);
      __asm
      {
        mov eax,r
        add eax,5
        mov edx,[eax-4]
        add edx,eax
        mov eax,c
        sub edx,eax
        mov [eax-4],edx
      }
    }
    else if (*r == 0xf && (*(r+1)>>4)==0x8)
    {
      c+=6;
      __asm
      {
        mov eax,r
        mov edx,dword ptr [eax+2]
        add eax,6
        add eax,edx
        mov edx,c
        sub eax,edx
        mov [edx-4],eax
      }
    }
    else c+=l;
    r+=l;
  }
  original_len=r-(BYTE*)original_addr;
  hook_len=c-(BYTE*)new_addr;
  return flag;
}

int TextHook::InsertHook()
{
  enum : int { yes = 0, no = 1};
  NtWaitForSingleObject(hmMutex, 0, 0);
  int ok = no;
   // jichi 9/17/2013: might raise 0xC0000005 AccessViolationException
  ITH_TRY { ok = InsertHookCode(); } ITH_EXCEPT {}
  IthReleaseMutex(hmMutex);
  //if (ok == no)
  //  ITH_WARN(L"It seems that your /H code is wrong ><");
  if (hp.type & HOOK_ADDITIONAL)
    NotifyHookInsert(hp.addr);
    //OutputConsole(hook_name);
    //RegisterHookName(hook_name,hp.addr);
  return ok;
}

int TextHook::InsertHookCode()
{
  enum : int { yes = 0, no = 0 };
  if (hp.module && (hp.type & MODULE_OFFSET)) { //Map hook offset to real address.
    if (DWORD base = GetModuleBase(hp.module)) {
      if (hp.function && (hp.type & FUNCTION_OFFSET)) {
        base = GetExportAddress(base, hp.function);
        if (base)
          hp.addr += base;
        else {
          //OutputConsole(L"Function not found in the export table.");
          current_hook--;
          return no;
        }
      }
      else
        hp.addr += base;
      hp.type &= ~(MODULE_OFFSET|FUNCTION_OFFSET);
    }
    else {
      //OutputConsole(L"Module not present.");
      current_hook--;
      return no;
    }
  }
  {
    TextHook *it = hookman;
    for (int i = 0; i < current_hook; it++) { // Check if there is a collision.
      if (it->Address())
        i++;
      //it = hookman + i;
      if (it == this)
        continue;
      if (it->Address() <= hp.addr &&
          it->Address() + it->Length() > hp.addr) {
        it->ClearHook();
        break;
      }
    }
  }
  // Verify hp.addr.
  MEMORY_BASIC_INFORMATION info = {};
  NtQueryVirtualMemory(NtCurrentProcess(), (LPVOID)hp.addr, MemoryBasicInformation, &info, sizeof(info), nullptr);
  if (info.Type & PAGE_NOACCESS)
    return no;
  // Initialize common routine.
  memcpy(recover, common_hook, sizeof(common_hook));
  BYTE *c = (BYTE *)hp.addr,
       *r = recover;
  BYTE inst[8]; // jichi 9/27/2013: Why 8? Only 5 bytes will be written using NtWriteVirtualMemory
  inst[0] = 0xe9; // jichi 9/27/2013: 0xe9 is jump, see: http://code.google.com/p/sexyhook/wiki/SEXYHOOK_Hackers_Manual
  __asm
  {
    mov edx,r
    mov eax,this
    mov [edx+0xa],eax // push TextHook*, resolve to correspond hook.
    lea eax,[edx+0x13]
    mov edx,ProcessHook
    sub edx,eax
    mov [eax-4],edx // call ProcessHook
    mov eax,c
    add eax,5
    mov edx,r
    sub edx,eax
    lea eax,inst+1
    mov [eax],edx
  }
  r += sizeof(common_hook);
  hp.hook_len = 5;
  //bool jmpflag=false; // jichi 9/28/2013: nto used
  // Copy original code.
  switch (MapInstruction(hp.addr, (DWORD)r, hp.hook_len, hp.recover_len)) {
  case -1: return no;
  case 0:
    __asm
    {
      mov ecx,this
      movzx eax,[ecx]hp.hook_len
      movzx edx,[ecx]hp.recover_len
      add edx,[ecx]hp.addr
      add eax,r
      add eax,5
      sub edx,eax
      mov [eax-5],0xe9 // jichi 9/27/2013: 0xe9 is jump
      mov [eax-4],edx
    }
  }
  // jichi 9/27/2013: Save the original instructions in the memory
  memcpy(original, (LPVOID)hp.addr, hp.recover_len);
  //Check if the new hook range conflict with existing ones. Clear older if conflict.
  {
    TextHook *it = hookman;
    for (int i = 0; i < current_hook; it++) {
      if (it->Address())
        i++;
      if (it == this)
        continue;
      if (it->Address() >= hp.addr &&
          it->Address() < hp.hook_len + hp.addr) {
        it->ClearHook();
        break;
      }
    }
  }
  // Insert hook and flush instruction cache.
  DWORD int3[] = {0xcccccccc, 0xcccccccc};
  DWORD t = 0x100,
      old,
      len;
  DWORD addr = hp.addr;
  // jichi 9/27/2013: Overwrite the memory with inst
  // See: http://undocumented.ntinternals.net/UserMode/Undocumented%20Functions/Memory%20Management/Virtual%20Memory/NtProtectVirtualMemory.html
  // See: http://doxygen.reactos.org/d8/d6b/ndk_2mmfuncs_8h_af942709e0c57981d84586e74621912cd.html
  NtProtectVirtualMemory(NtCurrentProcess(), (PVOID *)&addr, &t, PAGE_EXECUTE_READWRITE, &old);
  NtWriteVirtualMemory(NtCurrentProcess(), (BYTE *)hp.addr, inst, 5, &t);
  len = hp.recover_len - 5;
  if (len)
    NtWriteVirtualMemory(NtCurrentProcess(), (BYTE *)hp.addr + 5, int3, len, &t);
  NtFlushInstructionCache(NtCurrentProcess(), (LPVOID)hp.addr, hp.recover_len);
  NtFlushInstructionCache(NtCurrentProcess(), (LPVOID)hookman, 0x1000);
  return 0;
}

int TextHook::InitHook(LPVOID addr, DWORD data, DWORD data_ind,
    DWORD split_off, DWORD split_ind, WORD type, DWORD len_off)
{
  NtWaitForSingleObject(hmMutex, 0, 0);
  hp.addr = (DWORD)addr;
  hp.off = data;
  hp.ind = data_ind;
  hp.split = split_off;
  hp.split_ind = split_ind;
  hp.type = type;
  hp.hook_len = 0;
  hp.module = 0;
  hp.length_offset = len_off & 0xffff;
  current_hook++;
  if (current_available >= this)
    for (current_available = this + 1; current_available->Address(); current_available++);
  IthReleaseMutex(hmMutex);
  return this - hookman;
}

int TextHook::InitHook(const HookParam &h, LPCWSTR name, WORD set_flag)
{
  NtWaitForSingleObject(hmMutex, 0, 0);
  hp = h;
  hp.type |= set_flag;
  if (name && name != hook_name) {
    if (hook_name)
      delete[] hook_name;
    name_length = wcslen(name) + 1;
    hook_name = new wchar_t[name_length];
    //memset(hook_name, 0, sizeof(wchar_t) * name_length); // jichi 9/26/2013: zero memory
    hook_name[name_length - 1] = 0;
    wcscpy(hook_name, name);
  }
  current_hook++;
  current_available = this+1;
  while (current_available->Address())
    current_available++;
  IthReleaseMutex(hmMutex);
  return 1;
}

int TextHook::RemoveHook()
{
  if (hp.addr) {
    const LONGLONG timeout = -50000000; // jichi 9/28/2012: in 100ns, wait at most for 5 seconds
    NtWaitForSingleObject(hmMutex, 0, (PLARGE_INTEGER)&timeout);
    DWORD l = hp.hook_len;
    ITH_TRY { // jichi 9/17/2013: might crash ><
      NtWriteVirtualMemory(NtCurrentProcess(), (LPVOID)hp.addr, original, hp.recover_len, &l);
      NtFlushInstructionCache(NtCurrentProcess(), (LPVOID)hp.addr, hp.recover_len);
    } ITH_EXCEPT {
      //ITH_WARN(L"Failed to remove /H code ><");
    }
    hp.hook_len = 0;
    IthReleaseMutex(hmMutex);
    return 1;
  }
  return 0;
}

int TextHook::ClearHook()
{
  NtWaitForSingleObject(hmMutex, 0, 0);
  int err = RemoveHook();
  if (hook_name) {
    delete[] hook_name;
    hook_name = nullptr;
  }
  memset(this, 0, sizeof(TextHook));
  //if (current_available>this)
  //  current_available = this;
  current_hook--;
  IthReleaseMutex(hmMutex);
  return err;
}

int TextHook::ModifyHook(const HookParam &hp)
{
  //WCHAR name[0x40];
  DWORD len = 0;
  if (hook_name) len = wcslen(hook_name);
  LPWSTR name = 0;
  if (len) {
    name = new wchar_t[len + 1];
    //memset(name, 0, sizeof(wchar_t) * (len + 1)); // jichi 9/26/2013: zero memory
    name[len] = 0;
    wcscpy(name, hook_name);
  }
  ClearHook();
  InitHook(hp,name);
  InsertHook();
  if (name)
    delete[] name;
  return 0;
}

int TextHook::RecoverHook()
{
  if (hp.addr) {
    // jichi 9/28/2013: Only enable TextOutA to debug Cross Channel
    //if (hp.addr == (DWORD)TextOutA)
    InsertHook();
    return 1;
  }
  return 0;
}

int TextHook::SetHookName(LPCWSTR name)
{
  name_length = wcslen(name) + 1;
  if (hook_name)
    delete[] hook_name;
  hook_name = new wchar_t[name_length];
  //memset(hook_name, 0, sizeof(wchar_t) * name_length); // jichi 9/26/2013: zero memory
  hook_name[name_length - 1] = 0;
  wcscpy(hook_name, name);
  return 0;
}

int TextHook::GetLength(DWORD base, DWORD in)
{
  if (base == 0)
    return 0;
  int len;
  switch (hp.length_offset) {
  case 0:
    if (hp.type & USING_UNICODE)
      len = wcslen((LPWSTR)in) << 1;
    else
      len = strlen((char *)in);
    break;
  case 1:
    if (hp.type & USING_UNICODE)
      len=2;
    else {
      if (hp.type & BIG_ENDIAN)
        in >>= 8;
      len = LeadByteTable[in&0xff];  //Slightly faster than IsDBCSLeadByte
    }
    break;
  default:
    len = *((int *)base+hp.length_offset);
    if (len >= 0) {
      if (hp.type & USING_UNICODE)
        len <<= 1;
      break;
    }
    else if (len != -1)
      break;
    //len == -1 then continue to case 0.
  }
  return len;
}

//static LPVOID fun_table[HOOK_FUN_COUNT];
//#define DEFAULT_SPLIT
// 9/8/2013 jichi: Disabled as it seems this is never defined in ITH source code
// When enabled, ITH will try to split sentences according to the split time (?)
//#ifdef DEFAULT_SPLIT
//#define SPLIT_SWITCH USING_SPLIT
//#else
//#define SPLIT_SWITCH 0
//#endif

void InitDefaultHook()
{
  // int TextHook::InitHook(LPVOID addr, DWORD data, DWORD data_ind, DWORD split_off, DWORD split_ind, WORD type, DWORD len_off)
  //
  // jichi 9/8/2013: Guessed meaning
  // - data: 4 * the n-th (base 1) parameter representing the data of the string
  // - len_off:
  //   - the n-th (base 1) parameter representing the length of the string
  //   - or 1 if is char
  //   - or 0 if detect on run time
  // - type: USING_STRING if len_off != 1 else BIG_ENDIAN or USING_UNICODE
  //
  // Examples:
  // int WINAPI lstrlenA(LPCSTR lpString)
  // - data: 4 * 1 = 0x4, as lpString is the first
  // - len_off: 0, as no parameter representing string length
  // - type: BIG_ENDIAN, since len_off == 1
  // BOOL GetTextExtentPoint32(HDC hdc, LPCTSTR lpString, int c, LPSIZE lpSize);
  // - data: 4 * 2 = 0x8, as lpString is the second
  // - len_off: 3, as nCount is the 3rd parameter
  // - type: USING_STRING, since len_off != 1

  LPCWSTR names[] = {HOOK_FUN_NAME_LIST};
#define _(Name, ...) \
  hookman[HF_##Name].InitHook(Name, __VA_ARGS__); \
  hookman[HF_##Name].SetHookName(names[HF_##Name]);

  _(GetTextExtentPoint32A,  0x8,  0,4,0, USING_STRING,  3) // BOOL GetTextExtentPoint32(HDC hdc, LPCTSTR lpString, int c, LPSIZE lpSize);
  _(GetGlyphOutlineA,       0x8,  0,4,0, BIG_ENDIAN,    1) // DWORD GetGlyphOutline(HDC hdc,  UINT uChar,  UINT uFormat, LPGLYPHMETRICS lpgm, DWORD cbBuffer, LPVOID lpvBuffer, const MAT2 *lpmat2);
  _(ExtTextOutA,            0x18, 0,4,0, USING_STRING,  7) // BOOL ExtTextOut(HDC hdc, int X, int Y, UINT fuOptions, const RECT *lprc, LPCTSTR lpString, UINT cbCount, const INT *lpDx);
  _(TextOutA,               0x10, 0,4,0, USING_STRING,  5) // BOOL TextOut(HDC hdc, int nXStart, int nYStart, LPCTSTR lpString, int cchString);
  _(GetCharABCWidthsA,      0x8,  0,4,0, BIG_ENDIAN,    1) // BOOL GetCharABCWidths(HDC hdc, UINT uFirstChar, UINT uLastChar,  LPABC lpabc);
  _(DrawTextA,              0x8,  0,4,0, USING_STRING,  3) // int DrawText(HDC hDC, LPCTSTR lpchText, int nCount, LPRECT lpRect, UINT uFormat);
  _(DrawTextExA,            0x8,  0,4,0, USING_STRING,  3) // int DrawTextEx(HDC hdc, LPTSTR lpchText,int cchText, LPRECT lprc, UINT dwDTFormat, LPDRAWTEXTPARAMS lpDTParams);
  //_(lstrlenA,               0x4,  0,4,0, USING_STRING,  0) // int WINAPI lstrlen(LPCTSTR lpString);
  _(GetTextExtentPoint32W,  0x8,  0,4,0, USING_UNICODE|USING_STRING, 3)
  _(GetGlyphOutlineW,       0x8,  0,4,0, USING_UNICODE, 1)
  _(ExtTextOutW,            0x18, 0,4,0, USING_UNICODE|USING_STRING, 7)
  _(TextOutW,               0x10, 0,4,0, USING_UNICODE|USING_STRING, 5)
  _(GetCharABCWidthsW,      0x8,  0,4,0, USING_UNICODE, 1)
  _(DrawTextW,              0x8,  0,4,0, USING_UNICODE|USING_STRING, 3)
  _(DrawTextExW,            0x8,  0,4,0, USING_UNICODE|USING_STRING, 3)
  //_(lstrlenW,               0x4,  0,4,0, USING_UNICODE|USING_STRING, 0) // 9/8/2013 jichi: add lstrlen
#undef _
}

// jichi 10/2/2013
void IHFAPI InsertLstrHooks()
{
  // int TextHook::InitHook(LPVOID addr, DWORD data, DWORD data_ind, DWORD split_off, DWORD split_ind, WORD type, DWORD len_off)
#define _(_name, _addr, _data, _data_ind, _split_off, _split_ind, _type, _len_off) \
  { \
    HookParam hp = {}; \
    hp.addr = (DWORD)_addr; \
    hp.off = _data; \
    hp.ind = _data_ind; \
    hp.split = _split_off; \
    hp.split_ind = _split_ind; \
    hp.type = _type; \
    hp.length_offset = _len_off; \
    NewHook(hp, _name); \
  }

  // int WINAPI lstrlen(LPCTSTR lpString);
  _(L"lstrlenA", lstrlenA, 0x4,  0,4,0, USING_STRING,  0)
  _(L"lstrlenW", lstrlenW, 0x4,  0,4,0, USING_UNICODE|USING_STRING, 0)
#undef _
}

// EOF
