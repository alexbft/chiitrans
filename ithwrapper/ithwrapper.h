#pragma once

#define ITH_HAS_CRT
#define ITH_HAS_CXX
#define _CRT_NON_CONFORMING_SWPRINTFS
#define _CRT_SECURE_NO_WARNINGS

#include <Windows.h>

#include "ith/common/types.h"

#define DLLEXPORT __declspec(dllexport)

typedef DWORD (__stdcall *OnCreateThreadFunc)(DWORD thread_id, LPWSTR name, DWORD hook, DWORD context, DWORD subcontext);
typedef DWORD (__stdcall *OnInputFunc)(DWORD thread_id, BYTE* data, DWORD len, DWORD is_newline);
typedef DWORD (__stdcall *OnRemoveThreadFunc)(DWORD thread_id);
typedef DWORD (__stdcall *CallbackFunc)();

extern "C" {

	DLLEXPORT DWORD __stdcall TextHookInit();
	DLLEXPORT DWORD __stdcall TextHookCleanup();
	DLLEXPORT DWORD __stdcall TextHookConnect(DWORD pid);
	DLLEXPORT DWORD __stdcall TextHookDisconnect();
	DLLEXPORT DWORD __stdcall TextHookOnConnect(CallbackFunc callback);
	DLLEXPORT DWORD __stdcall TextHookOnDisconnect(CallbackFunc callback);
	DLLEXPORT DWORD __stdcall TextHookOnCreateThread(OnCreateThreadFunc callback);
	DLLEXPORT DWORD __stdcall TextHookOnRemoveThread(OnRemoveThreadFunc callback);
	DLLEXPORT DWORD __stdcall TextHookOnInput(OnInputFunc callback);
	DLLEXPORT DWORD __stdcall TextHookAddHook(HookParam *p, LPWSTR name);
	DLLEXPORT DWORD __stdcall TextHookRemoveHook(DWORD addr);

}