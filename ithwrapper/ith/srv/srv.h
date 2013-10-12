#pragma once

// srv.h
// 8/23/2013 jichi
// Branch: ITH/IHF.h, rev 105

#include "ith/srv/hookman.h"

#define IHFAPI __stdcall
#ifdef IHF
# define IHFSERVICE __declspec(dllexport)
#else
# define IHFSERVICE __declspec(dllimport)
#endif
#define ITH_DEFAULT_ENGINE 0

struct Settings;
struct HookParam;

// jichi 8/24/2013: Why extern "C"? Any specific reason to use C instead of C++ naming?
extern "C" {
IHFSERVICE DWORD IHFAPI IHF_Init();
IHFSERVICE DWORD IHFAPI IHF_Start();
IHFSERVICE DWORD IHFAPI IHF_Cleanup();
IHFSERVICE DWORD IHFAPI IHF_GetPIDByName(LPCWSTR pwcTarget);
IHFSERVICE DWORD IHFAPI IHF_InjectByPID(DWORD pid, LPCWSTR engine);
IHFSERVICE DWORD IHFAPI IHF_ActiveDetachProcess(DWORD pid);
IHFSERVICE DWORD IHFAPI IHF_GetHookManager(HookManager **hookman);
IHFSERVICE DWORD IHFAPI IHF_GetSettings(Settings **settings);
IHFSERVICE DWORD IHFAPI IHF_InsertHook(DWORD pid, HookParam *hp, LPWSTR name = 0);
IHFSERVICE DWORD IHFAPI IHF_ModifyHook(DWORD pid, HookParam *hp);
IHFSERVICE DWORD IHFAPI IHF_RemoveHook(DWORD pid, DWORD addr);
IHFSERVICE DWORD IHFAPI IHF_IsAdmin();
//IHFSERVICE DWORD IHFAPI IHF_GetFilters(PVOID *mb_filter, PVOID *uni_filter);
IHFSERVICE DWORD IHFAPI IHF_AddLink(DWORD from, DWORD to);
IHFSERVICE DWORD IHFAPI IHF_UnLink(DWORD from);
IHFSERVICE DWORD IHFAPI IHF_UnLinkAll(DWORD from);
} // extern "C"

// EOF
