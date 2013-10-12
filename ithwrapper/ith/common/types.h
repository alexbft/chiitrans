#pragma once

// ith/common/types.h
// 8/23/2013 jichi
// Branch: ITH/common.h, rev 128

#include <windows.h> // needed for windef types

struct HookParam { // size: 0x24
  // jichi 8/24/2013: For special hooks. Orignial name: DataFun
  typedef void (* extern_fun_t)(DWORD, HookParam *, DWORD *, DWORD *, DWORD *);

  DWORD addr;
  DWORD off,
        ind,
        split,
        split_ind;
  DWORD module,
        function;
  extern_fun_t extern_fun;
  DWORD type;
  WORD length_offset;
  BYTE hook_len,
       recover_len;
};

struct SendParam {
  DWORD type;
  HookParam hp;
};

struct Hook { // size: 0x80
  HookParam hp;
  LPWSTR hook_name;
  int name_length;
  BYTE recover[0x68 - sizeof(HookParam)];
  BYTE original[0x10];

  DWORD Address() const { return hp.addr; }
  DWORD Type() const { return hp.type; }
  WORD Length() const { return hp.hook_len; }
  LPWSTR Name() const { return hook_name; }
  int NameLength() const { return name_length; }
};

// EOF
