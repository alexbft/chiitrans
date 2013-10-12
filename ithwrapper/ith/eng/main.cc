// eng/main.cc
// 8/9/2013 jichi

#include "engine.h"
#include "ith/sys/sys.h"
//#include "ith/common/growl.h"
#include "cc/ccmacro.h"

BOOL WINAPI DllMain(HANDLE hModule, DWORD reason, LPVOID lpReserved)
{
  CC_UNUSED(lpReserved);
  switch(reason) {
  case DLL_PROCESS_ATTACH:
    LdrDisableThreadCalloutsForDll(hModule);
    IthInitSystemService();
    Engine::init(hModule);
    //swprintf(engine,L"ITH_ENGINE_%d",current_process_id);
    //hEngineOn=IthCreateEvent(engine);
    //NtSetEvent(hEngineOn,0);
    break;
  case DLL_PROCESS_DETACH:
    //NtClearEvent(hEngineOn);
    //NtClose(hEngineOn);
    IthCloseSystemService();
    break;
  }
  return TRUE;
}

// EOF
