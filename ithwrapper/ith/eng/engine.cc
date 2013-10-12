// eng/engine.cc
// 8/9/2013 jichi
// Branch: ITH_Engine/engine.cpp, revision 133

#ifdef _MSC_VER
# pragma warning (disable:4100)   // C4100: unreference formal parameter
# pragma warning (disable:4733)   // C4733: Inline asm assigning to 'FS:0' : handler not registered as safe handler
#endif // _MSC_VER

#include "engine.h"
#include "engine_p.h"
#include "util.h"
#include "ith/cli/cli.h"
#include "ith/sys/sys.h"
#include "ith/common/except.h"
//#include "ith/common/growl.h"
#include "disasm/disasm.h"

//#define OutputConsole(...)  (void)0     // jichi 8/18/2013: I don't need OutputConsole

enum { MAX_REL_ADDR = 0x200000 }; // jichi 8/18/2013: maximum relative address

// - Global variables -

namespace Engine {

WCHAR process_name_[MAX_PATH]; // cached

DWORD module_base_,
      module_limit_;

//LPVOID trigger_addr;
trigger_fun_t trigger_fun_;

} // namespace Engine

// - Methods -

namespace Engine {

DWORD InsertDynamicHook(LPVOID addr, DWORD frame, DWORD stack)
{ return !trigger_fun_(addr,frame,stack); }

DWORD DetermineEngineByFile1()
{
  enum : DWORD { yes = 0, no = 1 }; // return value
  if (IthFindFile(L"*.xp3") || Util::SearchResourceString(L"TVP(KIRIKIRI)")) {
    InsertKiriKiriHook();
    return yes;
  }
  if (IthFindFile(L"bgi.*")) {
    InsertBGIHook();
    return yes;
  }
  if (IthFindFile(L"data*.arc") && IthFindFile(L"stream*.arc")) {
    InsertMajiroHook();
    return yes;
  }
  if (IthFindFile(L"data\\pack\\*.cpz")) {
    InsertCMVSHook();
    return yes;
  }
  // jichi 10/1/2013: Why remove Wolf hook from ITH3?!
  //if (IthFindFile(L"data.wolf")) {
  //  InsertWolfHook();
  //  return 0;
  //}
  if (IthCheckFile(L"message.dat")) {
    InsertAtelierHook();
    return yes;
  }
  if (IthCheckFile(L"advdata\\dat\\names.dat")) {
    InsertCircusHook1();
    return yes;
  }
  if (IthCheckFile(L"advdata\\grp\\names.dat")) {
    InsertCircusHook2();
    return yes;
  }
  if (IthFindFile(L"*.noa")) {
    InsertCotophaHook();
    return yes;
  }
  if (IthFindFile(L"*.pfs")) { // jichi 10/1/2013
    InsertArtemisHook();
    return yes;
  }
  if (IthFindFile(L"*.int")) {
    InsertCatSystem2Hook();
    return yes;
  }
  if (IthFindFile(L"GameData\\*.pack")) {
    InsertQLIEHook();
    return yes;
  }
  if (IthFindFile(L"*.szs") || IthFindFile(L"Data\\*.szs")) {
    InsertGesen18Hook();
    return yes;
  }
  return no;
}

DWORD DetermineEngineByFile2()
{
  enum : DWORD { yes = 0, no = 1 }; // return value
  if (IthFindFile(L"*.lpk")) {
    InsertLucifenHook();
    return yes;
  }
  if (IthCheckFile(L"cfg.pak")) {
    InsertWaffleHook();
    return yes;
  }
  if (IthCheckFile(L"Arc00.dat")) {
    InsertTinkerBellHook();
    return yes;
  }
  if (IthFindFile(L"*.vfs")) {
    InsertSoftHouseHook();
    return yes;
  }
  if (IthFindFile(L"*.mbl")) {
    InsertLuneHook();
    return yes;
  }
  if (IthFindFile(L"pac\\*.ypf") || IthFindFile(L"*.ypf")) {
    // jichi 8/14/2013: CLOCLUP: "ノーブレスオブリージュ" would crash the game.
    if (!IthCheckFile(L"noblesse.exe"))
      InsertWhirlpoolHook();
    return yes;
  }
  if (IthFindFile(L"*.npa")) {
    InsertNitroPlusHook();
    return yes;
  }
  if (IthCheckFile(L"resident.dll")) {
    InsertRetouchHook();
    return yes;
  }
  if (IthCheckFile(L"malie.ini")) {
    InsertMalieHook();
    return yes;
  }
  if (IthCheckFile(L"live.dll")) {
    InsertLiveHook();
    return yes;
  }
  // 9/5/2013 jichi
  if (IthCheckFile(L"aInfo.db")) {
    InsertNextonHook();
    return yes;
  }
  return no;
}

DWORD DetermineEngineByFile3()
{
  enum : DWORD { yes = 0, no = 1 }; // return value
  //if (IthCheckFile(L"libscr.dll")) { // already checked
  //  InsertBrunsHook();
  //  return yes;
  //}
  if (IthFindFile(L"emecfg.ecf")) {
    InsertEMEHook();
    return yes;
  }
  if (IthFindFile(L"rrecfg.rcf")) {
    InsertRREHook();
    return yes;
  }
  if (IthFindFile(L"*.fpk") || IthFindFile(L"data\\*.fpk")) {
    InsertCandyHook();
    return yes;
  }
  if (IthFindFile(L"arc.a*")) {
    InsertApricotHook();
    return yes;
  }
  if (IthFindFile(L"*.mpk")) {
    InsertStuffScriptHook();
    return yes;
  }
  if (IthCheckFile(L"Execle.exe")) {
    InsertTriangleHook();
    return yes;
  }
  if (IthCheckFile(L"PSetup.exe")) {
    InsertPensilHook();
    return yes;
  }
  if (IthFindFile(L"*.med")) {
    InsertMEDHook();
    return yes;
  }
  if (IthCheckFile(L"Yanesdk.dll")) {
    InsertAB2TryHook();
    return yes;
  }
  return no;
}

DWORD DetermineEngineByFile4()
{
  enum : DWORD { yes = 0, no = 1 }; // return value
  if (IthCheckFile(L"bmp.pak") && IthCheckFile(L"dsetup.dll")) {
    InsertDebonosuHook();
    return yes;
  }
  if (IthFindFile(L"C4.EXE") || IthFindFile(L"XEX.EXE")) {
    InsertC4Hook();
    return yes;
  }
  if (IthCheckFile(L"Rio.arc") && IthFindFile(L"Chip*.arc")) {
    InsertWillPlusHook();
    return yes;
  }
  if (IthFindFile(L"*.tac")) {
    InsertTanukiHook();
    return yes;
  }
  if (IthFindFile(L"*.gxp")) {
    InsertGXPHook();
    return yes;
  }
  return no;
}

DWORD DetermineEngineByProcessName()
{
  enum : DWORD { yes = 0, no = 1 }; // return value
  WCHAR str[MAX_PATH];
  wcscpy(str, process_name_);
  _wcslwr(str); // lower case

  if (wcsstr(str,L"reallive")) {
    InsertRealliveHook();
    return yes;
  }

  // jichi 8/19/2013: DO NOT WORK for games like「ハピメア」
  //if (wcsstr(str,L"cmvs32") || wcsstr(str,L"cmvs64")) {
  //  InsertCMVSHook();
  //  return yes;
  //}

  // jichi 8/17/2013: Handle "~"
  if (wcsstr(str, L"siglusengine") || !wcsncmp(str, L"siglus~", 7)) {
    InsertSiglusHook();
    return yes;
  }

  if (wcsstr(str,L"rugp")) {
    InsertRUGPHook();
    return yes;
  }

  // jichi 8/17/2013: Handle "~"
  if (wcsstr(str, L"igs_sample") || !wcsncmp(str, L"igs_sa~", 7)) {
    InsertIronGameSystemHook();
    return yes;
  }

  if (wcsstr(str, L"bruns")) {
    InsertBrunsHook();
    return yes;
  }

  if (wcsstr(str, L"anex86")) {
    InsertAnex86Hook();
    return yes;
  }

  // jichi 8/17/2013: Handle "~"
  if (wcsstr(str, L"shinydays") || !wcsncmp(str, L"shinyd~", 7)) {
    InsertShinyDaysHook();
    return yes;
  }

  // jichi 10/3/2013: FIXME: Does not work
  // Raise C0000005 even with admin priv
  //if (wcsstr(str, L"bsz")) { // BALDRSKY ZERO
  //  InsertBaldrHook();
  //  return yes;
  //}

  DWORD len = wcslen(str);

  static WCHAR saveman[] = L"_checksum.exe";
  wcscpy(str + len - 4, saveman);
  if (IthCheckFile(str)) {
    InsertRyokuchaHook();
    return yes;
  }

  // jichi 8/24/2013: Checking for Rio.ini or $procname.ini
  //wcscpy(str+len-4, L"_?.war");
  //if (IthFindFile(str)) {
  //  InsertShinaHook();
  //  return yes;
  //}
  if (InsertShinaHook())
    return yes;

  // jichi 8/10/2013: Since *.bin is common, move CaramelBox to the end
  str[len - 3] = L'b';
  str[len - 2] = L'i';
  str[len - 1] = L'n';
  str[len] = 0;
  if (IthCheckFile(str)) {
    InsertCaramelBoxHook();
    return yes;
  }
  return no;
}

DWORD DetermineEngineOther()
{
  enum : DWORD { yes = 0, no = 1 }; // return value
  if (InsertAliceHook())
    return yes;

  // jichi 8/24/2013: Move into functions
  static BYTE static_file_info[0x1000];
  if (IthGetFileInfo(L"*01", static_file_info))
    if (*(DWORD*)static_file_info == 0) {
      STATUS_INFO_LENGTH_MISMATCH;
      static WCHAR static_search_name[MAX_PATH];
      LPWSTR name=(LPWSTR)(static_file_info+0x5E);
      int len = wcslen(name);
      name[len-2] = L'.';
      name[len-1] = L'e';
      name[len] = L'x';
      name[len+1] = L'e';
      name[len+2] = 0;
      if (IthCheckFile(name)) {
        name[len-2] = L'*';
        name[len-1] = 0;
        wcscpy(static_search_name,name);
        IthGetFileInfo(static_search_name,static_file_info);
        union {
          FILE_BOTH_DIR_INFORMATION *both_info;
          DWORD addr;
        };
        both_info = (FILE_BOTH_DIR_INFORMATION *)static_file_info;
        //BYTE* ptr=static_file_info;
        len=0;
        while (both_info->NextEntryOffset) {
          addr += both_info->NextEntryOffset;
          len++;
        }
        if (len > 3) {
          InsertAbelHook();
          return yes;
        }
      }
    }
  return no;
}

DWORD DetermineNoHookEngine()
{
  enum : DWORD { yes = 0, no = 1 }; // return value

  if (IthCheckFile(L"game_sys.exe"))  return yes; //OutputConsole(L"Atelier Kaguya BY/TH");
  if (IthFindFile(L"*.ykc")) return yes; //OutputConsole(L"YKC:Feng/HookSoft(SMEE)");
  if (IthFindFile(L"*.bsa")) return yes; //OutputConsole(L"Bishop");
  if (IthFindFile(L"*.pac")) {
    //if (IthCheckFile(L"Thumbnail.pac")) {
    //  OutputConsole(L"GIGA");
    //  return yes;
    //}
    if (Util::SearchResourceString(L"SOFTPAL")) return yes; //OutputConsole(L"SoftPal UNiSONSHIFT");
  }
  wchar_t str[MAX_PATH];
  DWORD i;
  for (i = 0; process_name_[i]; i++) {
    str[i] = process_name_[i];
    if (process_name_[i] == L'.')
      break;
  }
  *(DWORD *)(str + i + 1) = 0x630068; //.hcb
  *(DWORD *)(str + i + 3) = 0x62;
  if (IthCheckFile(str)) return yes; //OutputConsole(L"FVP"); jichi 10/3/2013: such like アトリエかぐや

  return no;
}

DWORD DetermineEngineType()
{
  enum : DWORD { yes = 0, no = 1 };
  // jichi 9/27/2013: disable game engine for debugging use
#ifdef ITH_DISABLE_ENGINE
  return no;
#else
  // jichi 9/14/2013: Certain ITH functions like FindEntryAligned might raise exception without admin priv
  ITH_TRY {
    //WCHAR engine_info[0x100];
    // jichi 8/9/2013: Remove dependence on ITH/version.h
    //swprintf(engine_info, L"Engine support module %s", build_date);
    //swprintf(engine_info, L"Engine support module");
    //OutputConsole(engine_info);
    DWORD ret =
        DetermineEngineByFile1()
        && DetermineEngineByFile2()
        && DetermineEngineByFile3()
        && DetermineEngineByFile4()
        && DetermineEngineByProcessName()
        && DetermineEngineOther()
        && DetermineNoHookEngine()
        ? no : yes;
    // jichi 10/2/2013: Only enable it if no game engine is detected
    // Lstr functions usually extracts rubbish, and might crash certain games like 「Magical Marriage Lunatics!!」
    //ITH_GROWL_DWORD(ret);
    if (ret == no)
      InsertLstrHooks();
    return ret;
  } ITH_EXCEPT {
    //ITH_WARN(L"Engine Exception");
    //ITH_GROWL(GetExceptionCode()); // usually C0000005
    return no;
  }
#endif // ITH_DISABLE_ENGINE
}

namespace { // unnamed

DWORD recv_esp,
      recv_eip;

EXCEPTION_DISPOSITION ExceptHandler(
    EXCEPTION_RECORD *ExceptionRecord,
    void *EstablisherFrame, CONTEXT *ContextRecord, void * DispatcherContext)
{
  if (ExceptionRecord->ExceptionCode == STATUS_ACCESS_VIOLATION) {
    module_limit_ = ExceptionRecord->ExceptionInformation[1];
    //OutputDWORD(module_limit_);
    __asm
    {
      mov eax,fs:[0x30]
      mov eax,[eax+0xc]
      mov eax,[eax+0xc]
      mov ecx,module_limit_
      sub ecx,module_base_
      mov [eax+0x20],ecx
    }
  }
  ContextRecord->Esp = recv_esp;
  ContextRecord->Eip = recv_eip;
  return ExceptionContinueExecution;
}

} // unnamed namespace

DWORD IdentifyEngine()
{
  FillRange(process_name_, &module_base_, &module_limit_);
  BYTE status = 0;
  __asm
  {
    mov eax,seh_recover
    mov recv_eip,eax
    push ExceptHandler
    push fs:[0]
    mov fs:[0],esp
    pushad
    mov recv_esp,esp
  }
  DetermineEngineType();
  status++;
  __asm
  {
seh_recover:
    popad
    mov eax,[esp]
    mov fs:[0],eax
    add esp,8
  }
  //if (status == 0)
  //  OutputConsole(L"Fail to identify engine type.");
  //else
  //  OutputConsole(L"Initialized successfully.");
}

} // namespace Engine

// - Initialization -

void Engine::init(HANDLE hModule)
{
  Util::GetProcessName(process_name_); // Initialize process name
  ::RegisterEngineModule((DWORD)hModule, (DWORD)IdentifyEngine, (DWORD)InsertDynamicHook);
}

// EOF

/*
extern "C" {
  // http://gmogre3d.googlecode.com/svn-history/r815/trunk/OgreMain/src/WIN32/OgreMinGWSupport.cpp
  // http://forum.osdev.org/viewtopic.php?f=8&t=22352
  //#pragma data_seg()
  //#pragma comment(linker, "/merge:.CRT=.data") // works fine in visual c++ 6
  //#pragma data_seg()
  //#pragma comment(linker, "/merge:.CRT=.rdata")
    // MSVC libs use _chkstk for stack-probing. MinGW equivalent is _alloca.
  //void _alloca();
  //void _chkstk() { _alloca(); }

  // MSVC uses security cookies to prevent some buffer overflow attacks.
  // provide dummy implementations.
  //void _fastcall __security_check_cookie(intptr_t i) {}
  void __declspec(naked) __fastcall __security_check_cookie(UINT_PTR cookie) {}
}
*/
