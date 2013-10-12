#pragma once

// cli_p.h
// 8/24/2013 jichi
// Branch: IHF_DLL/IHF_CLIENT.h, rev 133
//
// 8/24/2013 TODO:
// - Clean up this file
// - Reduce global variables. Use namespaces or singleton classes instead.

//#include <windows.h>
//#define IHF
#include "config.h"
#include "cli.h"

#define HEADER_SIZE 0xC

extern int current_hook;
extern WCHAR dll_mutex[];
//extern WCHAR dll_name[];
extern DWORD trigger;
//extern DWORD current_process_id;

template <class T, class D, class fComp, class fCopy, class fLength>
class AVLTree;
struct FunctionInfo {
  DWORD addr;
  DWORD module;
  DWORD size;
  LPWSTR name;
};
struct SCMP;
struct SCPY;
struct SLEN;
extern AVLTree<char, FunctionInfo, SCMP, SCPY, SLEN> *tree;

void InitFilterTable();

// jichi 9/25/2013: This class will be used by NtMapViewOfSectionfor
// interprocedure communication, where constructor/destructor will NOT work.
class TextHook : public Hook
{
public:
  int InsertHook();
  int InsertHookCode();
  int InitHook(const HookParam &hp, LPCWSTR name = 0, WORD set_flag = 0);
  int InitHook(LPVOID addr, DWORD data, DWORD data_ind,
      DWORD split_off, DWORD split_ind, WORD type, DWORD len_off = 0);
  DWORD Send(DWORD dwDataBase, DWORD dwRetn);
  int RecoverHook();
  int RemoveHook();
  int ClearHook();
  int ModifyHook(const HookParam&);
  int SetHookName(LPCWSTR name);
  int GetLength(DWORD base, DWORD in);
  void CoolDown(); // jichi 9/28/2013: flush instruction cache on wine
};

extern TextHook *hookman,
                *current_available;

void InitDefaultHook();

struct FilterRange { DWORD lower, upper; };
extern FilterRange filter[8];

extern bool running,
            live;

extern HANDLE hPipe,
              hmMutex;

DWORD WINAPI WaitForPipe(LPVOID lpThreadParameter);
DWORD WINAPI CommandPipe(LPVOID lpThreadParameter);

void RequestRefreshProfile();

typedef DWORD (*IdentifyEngineFun)();
typedef DWORD (*InsertHookFun)(DWORD);
typedef DWORD (*InsertDynamicHookFun)(LPVOID addr, DWORD frame, DWORD stack);

extern IdentifyEngineFun IdentifyEngine;
extern InsertDynamicHookFun InsertDynamicHook;

// jichi 9/28/2013: Protect pipeline in wine
void CliLockPipe();
void CliUnlockPipe();

// EOF
