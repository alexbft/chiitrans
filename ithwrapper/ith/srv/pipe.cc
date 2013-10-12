// pipe.cc
// 8/24/2013 jichi
// Branch IHF/pipe.cpp, rev 93
// 8/24/2013 TODO: Clean up this file

#include "srv_p.h"
#include "hookman.h"
#include "ith/common/defs.h"
#include "ith/common/const.h"
//#include "ith/common/growl.h"
#include "ith/sys/sys.h"
//#include "CommandQueue.h"

//DWORD WINAPI UpdateWindows(LPVOID lpThreadParameter);

namespace { // unnamed
enum NamedPipeCommand {
  NAMED_PIPE_DISCONNECT = 1
  , NAMED_PIPE_CONNECT = 2
};

bool newline = false;
bool detach = false;

// jichi 9/28/2013: Skip leading garbage
// TODO: For english games, disable this filter!
// Note:
// - In order to make repetition filter in ITH work, I need to add one more 0x20 space
// - This will break manual translations
BYTE *Filter(BYTE *str, int len)
{
#ifdef ITH_DISABLE_FILTER // jichi 9/28/2013: only for debugging purpose
  return str;
#endif // ITH_DISABLE_FILTER
//  if (len && *str == 0x10) // jichi 9/28/2013: garbage on wine, data link escape, or ^P
//    return nullptr;
  WORD s;
  while (true) {
    s = *(WORD *)str;
    if (len >= 2) {
      if (s <= 0x20) {
        str += 2;
        len -= 2;
      } else
        break;
    } else if (str[0] <= 0x20) {
      str++;
      len--;
    }
    else
      break;
  }
  return str;
}
} // unnamed namespace

//WCHAR recv_pipe[] = L"\\??\\pipe\\ITH_PIPE";
//WCHAR command_pipe[] = L"\\??\\pipe\\ITH_COMMAND";
wchar_t recv_pipe[] = ITH_TEXT_PIPE;
wchar_t command_pipe[] = ITH_COMMAND_PIPE;

CRITICAL_SECTION detach_cs; // jichi 9/27/2013: also used in main
//HANDLE hDetachEvent;
extern HANDLE hPipeExist;

void CreateNewPipe()
{
  static DWORD acl[7] = {
      0x1C0002,
      1,
      0x140000,
      GENERIC_READ | GENERIC_WRITE | SYNCHRONIZE,
      0x101,
      0x1000000,
      0};
  static SECURITY_DESCRIPTOR sd = {1, 0, 4, 0, 0, 0, (PACL)acl};

  HANDLE hTextPipe, hCmdPipe, hThread;
  IO_STATUS_BLOCK ios;
  UNICODE_STRING us;

  OBJECT_ATTRIBUTES oa = {sizeof(oa), 0, &us, OBJ_CASE_INSENSITIVE, &sd, 0};
  LARGE_INTEGER time = {-500000, -1};

  RtlInitUnicodeString(&us, recv_pipe);
  if (!NT_SUCCESS(NtCreateNamedPipeFile(
      &hTextPipe,
      GENERIC_READ | SYNCHRONIZE,
      &oa,
      &ios,
      FILE_SHARE_WRITE,
      FILE_OPEN_IF,
      FILE_SYNCHRONOUS_IO_NONALERT,
      1, 1, 0, -1,
      0x1000,
      0x1000,
      &time)))
    //ConsoleOutput(ErrorCreatePipe);
    return;

  RtlInitUnicodeString(&us, command_pipe);
  if (!NT_SUCCESS(NtCreateNamedPipeFile(
      &hCmdPipe,
      GENERIC_WRITE | SYNCHRONIZE,
      &oa,
      &ios,
      FILE_SHARE_READ,
      FILE_OPEN_IF,
      FILE_SYNCHRONOUS_IO_NONALERT,
      1, 1, 0, -1,
      0x1000,
      0x1000,
      &time)))
    //ConsoleOutput(ErrorCreatePipe);
    return;

  hThread = IthCreateThread(RecvThread, (DWORD)hTextPipe);
  man->RegisterPipe(hTextPipe, hCmdPipe, hThread);
}

void DetachFromProcess(DWORD pid)
{
  HANDLE hMutex, hEvent;
  IO_STATUS_BLOCK ios;
  ProcessRecord* pr = man->GetProcessRecord(pid);
  if (pr == 0) return;
  //IthBreak();
  hEvent = IthCreateEvent(nullptr);
  if (STATUS_PENDING == NtFsControlFile(
      man->GetCmdHandleByPID(pid),
      hEvent,
      0,0,
      &ios,
      CTL_CODE(FILE_DEVICE_NAMED_PIPE, NAMED_PIPE_DISCONNECT, 0, 0),
      0,0,0,0))
    NtWaitForSingleObject(hEvent, 0, 0);
  NtClose(hEvent);

  WCHAR mutex[0x20];
  swprintf(mutex, ITH_DETACH_MUTEX_ L"%d",pid);

  hMutex = IthOpenMutex(mutex);
  if (hMutex != INVALID_HANDLE_VALUE) {
    NtWaitForSingleObject(hMutex, 0, 0);
    NtReleaseMutant(hMutex, 0);
    NtClose(hMutex);
  }

  //NtSetEvent(hDetachEvent, 0);
  if (running)
    NtSetEvent(hPipeExist, 0);
}

// jichi 9/27/2013: I don't need this
//void OutputDWORD(DWORD d)
//{
//  WCHAR str[0x20];
//  swprintf(str, L"%.8X", d);
//  ConsoleOutput(str);
//}

DWORD WINAPI RecvThread(LPVOID lpThreadParameter)
{
  HANDLE hTextPipe = (HANDLE)lpThreadParameter;

  IO_STATUS_BLOCK ios;
  NtFsControlFile(hTextPipe,
     0, 0, 0,
     &ios,
     CTL_CODE(FILE_DEVICE_NAMED_PIPE, NAMED_PIPE_CONNECT, 0, 0),
     0, 0, 0, 0);
  if (!running) {
    NtClose(hTextPipe);
    return 0;
  }

  BYTE *buff,
       *it;

  enum { PipeBufferSize = 0x1000 };
  buff = new BYTE[PipeBufferSize];
  memset(buff, 0, PipeBufferSize); // jichi 8/27/2013: zero memory, or it will crash wine on start up
  NtReadFile(hTextPipe, 0, 0, 0, &ios, buff, 16, 0, 0);

  DWORD pid = *(DWORD *)buff,
        hookman = *(DWORD *)(buff + 0x4),
        module = *(DWORD *)(buff + 0x8),
        engine = *(DWORD *)(buff + 0xc);
  man->RegisterProcess(pid, hookman, module, engine);

  // jichi 9/27/2013: why recursion?
  CreateNewPipe();

  //NtClose(IthCreateThread(UpdateWindows,0));
  while (running) {
    if (!NT_SUCCESS(NtReadFile(hTextPipe,
        0, 0, 0,
        &ios,
        buff,
        0xf80,
        0, 0)))
      break;

    DWORD RecvLen = ios.uInformation;
    if (RecvLen < 0xc)
      break;
    DWORD hook = *(DWORD *)buff;

    union { DWORD retn; DWORD cmd_type; };
    union { DWORD split; DWORD new_engine_type; };

    retn = *(DWORD *)(buff + 4);
    split = *(DWORD *)(buff + 8);

    buff[RecvLen] = 0;
    buff[RecvLen + 1] = 0;

    if (hook == IHF_NOTIFICATION) {
      switch (cmd_type) {
      case IHF_NOTIFICATION_NEWHOOK:
        {
          static long lock;
          while (InterlockedExchange(&lock, 1) == 1);
          ProcessEventCallback new_hook = man->ProcessNewHook();
          if (new_hook)
            new_hook(pid);
          lock = 0;
        } break;
      case IHF_NOTIFICATION_TEXT:
        // jichi 9/27/2013: I don't need this
        //ConsoleOutput((LPWSTR)(buff + 8));
        break;
      }
    } else {
      // jichi 9/28/2013: Debug raw data
      //ITH_DEBUG_DWORD9(RecvLen - 0xc,
      //    buff[0xc], buff[0xd], buff[0xe], buff[0xf],
      //    buff[0x10], buff[0x11], buff[0x12], buff[0x13]);
      it = Filter(buff + 0xc, RecvLen - 0xc);
      //if (it) { // 9/28/2013: Make filter able to return nullptr
      RecvLen -= it - buff;
      if (RecvLen >> 31) // RecvLen is too large
        RecvLen = 0;
      man->DispatchText(pid, it, hook, retn, split, RecvLen);
    }
  }

  EnterCriticalSection(&detach_cs);

  HANDLE hDisconnect = IthCreateEvent(nullptr);

  if (STATUS_PENDING == NtFsControlFile(
      hTextPipe,
      hDisconnect,
      0, 0,
      &ios,
      CTL_CODE(FILE_DEVICE_NAMED_PIPE, NAMED_PIPE_DISCONNECT, 0, 0),
      0, 0, 0, 0))
    NtWaitForSingleObject(hDisconnect, 0, 0);

  NtClose(hDisconnect);
  DetachFromProcess(pid);
  man->UnRegisterProcess(pid);

  //NtClearEvent(hDetachEvent);

  LeaveCriticalSection(&detach_cs);

  //if (running) {
  //  swprintf((LPWSTR)buff, FormatDetach, pid);
  //  ConsoleOutput((LPWSTR)buff);
  //  NtClose(IthCreateThread(UpdateWindows, 0));
  //}
  delete[] buff;
  return 0;
}

// EOF
