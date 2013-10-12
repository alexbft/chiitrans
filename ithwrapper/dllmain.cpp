// dllmain.cpp : Defines the entry point for the DLL application.
#include "stdafx.h"
#include "ith/sys/sys.h"


BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
					 )
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		LdrDisableThreadCalloutsForDll(hModule);
		IthInitSystemService();
		break;
	case DLL_PROCESS_DETACH:
		IthCloseSystemService();
		break;
	default:
		break;
	}
	return TRUE;
}

