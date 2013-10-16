// ithwrapper.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"

#include "ithwrapper.h"
#include "ith/srv/srv.h"
#include "ith/sys/sys.h"
#include "ith/common/defs.h"
#include "ith/common/const.h"

//static HANDLE hHeap;
static HookManager *hm = NULL;
static bool initialized = false;
static bool connected = false;
static DWORD my_pid = 0;

static CallbackFunc onConnect = NULL;
static CallbackFunc onDisconnect = NULL;
static OnCreateThreadFunc onCreateThreadFunc = NULL;
static OnRemoveThreadFunc onRemoveThreadFunc = NULL;
static OnInputFunc onInputFunc = NULL;

DWORD ProcessAttach(DWORD pid);
DWORD ProcessDetach(DWORD pid);
DWORD ThreadCreate(TextThread *t);
DWORD ThreadRemove(TextThread *t);
DWORD ThreadOutput(TextThread* t, BYTE* data, DWORD len, DWORD new_line, PVOID user_data);

//////////////////////// INTERFACE //////////////////////////////////////////////////

DLLEXPORT DWORD __stdcall TextHookInit() 
{
	if (initialized) {
		return 0;
	}
	DWORD exists;
	HANDLE m = IthCreateMutex(ITH_SERVER_MUTEX, 1, &exists);
	if (exists) {
		return 1001;
	} else {
		NtClose(m);
	}
	if (!IHF_Init()) {
		return 1002;
	}
	IHF_GetHookManager(&hm);
	if (hm) {
		hm->RegisterProcessAttachCallback(ProcessAttach);
		hm->RegisterProcessDetachCallback(ProcessDetach);
		hm->RegisterThreadCreateCallback(ThreadCreate);
		hm->RegisterThreadRemoveCallback(ThreadRemove);
		IHF_Start();
		initialized = true;
		return 0;
	} else {
		IHF_Cleanup();
		return 1003;
	}
}

DLLEXPORT DWORD __stdcall TextHookConnect(DWORD pid)
{
	if (connected) {
		TextHookDisconnect();
	}
	if (!initialized) {
		return 1;
	}
	my_pid = pid;
	HANDLE module = (HANDLE) IHF_InjectByPID(pid, NULL);
	connected = true;
	if (module != INVALID_HANDLE_VALUE) {
		return 0;
	} else {
		return 1002;
	}
}

DLLEXPORT DWORD __stdcall TextHookDisconnect() {
	if (!initialized) return 1;
	if (my_pid != 0) {
		IHF_ActiveDetachProcess(my_pid);
		my_pid = 0;
	}
	connected = false;
	return 0;
}

DLLEXPORT DWORD __stdcall TextHookCleanup() {
	onCreateThreadFunc = NULL;
	onRemoveThreadFunc = NULL;
	onInputFunc = NULL;
	onConnect = NULL;
	onDisconnect = NULL;
	if (connected) {
		TextHookDisconnect();
	}
	initialized = false;
	connected = false;
	return IHF_Cleanup();
}

DLLEXPORT DWORD __stdcall TextHookOnConnect(CallbackFunc callback) {
	onConnect = callback;
	return 0;
}

DLLEXPORT DWORD __stdcall TextHookOnDisconnect(CallbackFunc callback) {
	onDisconnect = callback;
	return 0;
}

DLLEXPORT DWORD __stdcall TextHookOnCreateThread(OnCreateThreadFunc callback) {
	onCreateThreadFunc = callback;
	return 0;
}

DLLEXPORT DWORD __stdcall TextHookOnRemoveThread(OnRemoveThreadFunc callback) {
	onRemoveThreadFunc = callback;
	return 0;
}

DLLEXPORT DWORD __stdcall TextHookOnInput(OnInputFunc callback) {
	onInputFunc = callback;
	return 0;
}

DLLEXPORT DWORD __stdcall TextHookAddHook(HookParam *p, LPWSTR name) {
	if (!connected) return 1;
	return IHF_InsertHook(my_pid, p, name);
}

DLLEXPORT DWORD __stdcall TextHookRemoveHook(DWORD addr) {
	if (!connected) return 1;
	return IHF_RemoveHook(my_pid, addr);
}

///////////////////// PRIVATE FUNCTIONS /////////////////////////////////////////////

DWORD ProcessAttach(DWORD pid) {
	if (pid != my_pid) {
		IHF_ActiveDetachProcess(pid);
	} else {
		if (onConnect != NULL) {
			onConnect();
		}
	}
	return 0;
}

DWORD ProcessDetach(DWORD pid) {
	if (pid == my_pid) {
		if (onDisconnect != NULL) {
			onDisconnect();
		}
		my_pid = 0;
	}
	return 0;
}

DWORD ThreadCreate(TextThread *t) {
	ThreadParameter *tp = t->GetThreadParameter();
	if (tp->pid != my_pid) {
		IHF_ActiveDetachProcess(tp->pid);
		return 0;
	}
	if (onCreateThreadFunc != NULL) {
		WCHAR buf[1001];
		t->GetThreadString(buf, 1000);
		bool is_unicode = false;
		if (ProcessRecord *pr = hm->GetProcessRecord(t->PID())) {
			NtWaitForSingleObject(pr->hookman_mutex, 0, 0);
			Hook *hk = static_cast<Hook *>(pr->hookman_map);
			for (int i = 0; i < MAX_HOOK; i++) {
				if (hk[i].Address() == t->Addr()) {
				if (hk[i].Type() & USING_UNICODE)
					is_unicode = true;
				break;
				}
			}
			NtReleaseMutant(pr->hookman_mutex, 0);
		}
		DWORD err = onCreateThreadFunc(t->Number(), buf, tp->hook, tp->retn, tp->spl, is_unicode ? 1 : 0);
		if (err) {
			return 0;
		}
	}
	t->RegisterFilterCallBack(ThreadOutput, NULL);
	return 0;
}

DWORD ThreadRemove(TextThread *t) {
	if (onRemoveThreadFunc != NULL) {
		onRemoveThreadFunc(t->Number());
	}
	return 0;
}

DWORD ThreadOutput(TextThread* t, BYTE* data,DWORD len, DWORD new_line, PVOID user_data) {
	if (onInputFunc != NULL) {
		onInputFunc(t->Number(), data, len, new_line);
	}
	return 0;
}

