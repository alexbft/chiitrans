#pragma once

// ith/common/const.h
// 8/23/2013 jichi
// Branch: ITH/common.h, rev 128

// jichi 9/9/2013: Another importnat function is lstrcatA, which is already handled by
// Debonosu hooks. Wait until it is really needed by certain games.
enum HookFunType {
  HF_Null = -1
  , HF_GetTextExtentPoint32A
  , HF_GetGlyphOutlineA
  , HF_ExtTextOutA
  , HF_TextOutA
  , HF_GetCharABCWidthsA
  , HF_DrawTextA
  , HF_DrawTextExA
  //, HF_lstrlenA
  , HF_GetTextExtentPoint32W
  , HF_GetGlyphOutlineW
  , HF_ExtTextOutW
  , HF_TextOutW
  , HF_GetCharABCWidthsW
  , HF_DrawTextW
  , HF_DrawTextExW
  //, HF_lstrlenW
  , HookFunCount // 14
};

enum { HOOK_FUN_COUNT = HookFunCount };
enum { MAX_HOOK = 32 }; // must be larger than HookFunCount

#define HOOK_FUN_NAME_LIST \
  L"GetTextExtentPoint32A" \
  , L"GetGlyphOutlineA" \
  , L"ExtTextOutA" \
  , L"TextOutA" \
  , L"GetCharABCWidthsA" \
  , L"DrawTextA" \
  , L"DrawTextExA" \
  , L"GetTextExtentPoint32W" \
  , L"GetGlyphOutlineW" \
  , L"ExtTextOutW" \
  , L"TextOutW" \
  , L"GetCharABCWidthsW" \
  , L"DrawTextW" \
  , L"DrawTextExW"
  //, L"lstrlenA"
  //, L"lstrlenW"

enum IhfCommandType {
  IHF_COMMAND = -1 // null type
  , IHF_COMMAND_NEW_HOOK = 0
  , IHF_COMMAND_REMOVE_HOOK = 1
  , IHF_COMMAND_MODIFY_HOOK = 2
  , IHF_COMMAND_DETACH = 3
};

enum IhfNotificationType {
  IHF_NOTIFICATION = -1 // null type
  , IHF_NOTIFICATION_TEXT = 0
  , IHF_NOTIFICATION_NEWHOOK = 1
};

// jichi 9/8/2013: The meaning are gussed
enum HookParamType : unsigned long {
  HP_Null             = 0       // never used
  , USING_STRING      = 0x1     // type(data) is char* or wchar_t* and has length
  , USING_UNICODE     = 0x2     // type(data) is wchar_t or wchar_t*
  , BIG_ENDIAN        = 0x4     // type(data) is char
  , DATA_INDIRECT     = 0x8
  , USING_SPLIT       = 0x10    // aware of split time?
  , SPLIT_INDIRECT    = 0x20
  , MODULE_OFFSET     = 0x40    // do hash module, and the address is relative to module
  , FUNCTION_OFFSET   = 0x80    // do hash function, and the address is relative to funccion
  , PRINT_DWORD       = 0x100
  , STRING_LAST_CHAR  = 0x200
  , NO_CONTEXT        = 0x400
  , EXTERN_HOOK       = 0x800   // use external hook function
  , HOOK_AUXILIARY    = 0x2000
  , HOOK_ENGINE       = 0x4000
  , HOOK_ADDITIONAL   = 0x8000
};

// EOF
