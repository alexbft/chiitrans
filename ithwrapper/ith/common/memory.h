#pragma once

// ith/common/memory.h
// 8/23/2013 jichi
// Branch: ITH/mem.h, revision 66

//#include <cstring>

// jichi 9/22/2013: disabled
//#ifndef DEFAULT_MM
//#ifndef ITH_HAS_CXX // ITH has access to C++ syntax
//
//extern "C" {
////PVOID RtlAllocateHeap(_In_ PVOID HeapHandle, _In_opt_ ULONG Flags, _In_ SIZE_T Size);
//__declspec(dllimport) void * __stdcall RtlAllocateHeap(void *hHeap, unsigned long flags, unsigned long size);
////BOOLEAN RtlFreeHeap(_In_ PVOID HeapHandle, _In_opt_ ULONG Flags, _In_  PVOID HeapBase);
//__declspec(dllimport) int __stdcall RtlFreeHeap(void *hHeap, unsigned long flags, void *hHeapBase);
//}; // extern "C"
//
//extern void *hHeap; // global shared heap
//
////HEAP_ZERO_MEMORY flag is critical. All new objects are assumed with zero initialized.
//inline void * __cdecl operator new(size_t lSize)
//{ return RtlAllocateHeap(hHeap, 8, lSize); }
//
//inline void __cdecl operator delete(void *pBlock)
//{ RtlFreeHeap(hHeap, 0, pBlock); }
//
//inline void __cdecl operator delete[](void* pBlock)
//{ RtlFreeHeap(hHeap, 0, pBlock); }
//
//#endif  // ITH_HAS_CXX
//
