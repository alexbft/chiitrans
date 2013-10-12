#pragma once

// hookman.h
// 8/23/2013 jichi
// Branch: ITH/HookManager.h, rev 133

#include "ith/srv/hookman_p.h"
#include "ith/srv/textthread.h"

enum { MAX_REGISTER = 0xf };
enum { MAX_PREV_REPEAT_LENGTH = 0x20 };

struct ProcessRecord {
  DWORD pid_register;
  DWORD hookman_register;
  DWORD module_register;
  DWORD engine_register;
  HANDLE process_handle;
  HANDLE hookman_mutex;
  HANDLE hookman_section;
  LPVOID hookman_map;
};

class ThreadTable : public MyVector<TextThread *, 0x40>
{
public:
  virtual void SetThread(DWORD number, TextThread *ptr);
  virtual TextThread *FindThread(DWORD number);
};

struct TCmp { char operator()(const ThreadParameter *t1,const ThreadParameter *t2); };
struct TCpy { void operator()(ThreadParameter *t1,const ThreadParameter *t2); };
struct TLen { int operator()(const ThreadParameter *t); };

typedef DWORD (*ProcessEventCallback)(DWORD pid);

class HookManager : public AVLTree<ThreadParameter, DWORD, TCmp, TCpy, TLen>
{
public:
  HookManager();
  ~HookManager();
  virtual TextThread *FindSingle(DWORD pid, DWORD hook, DWORD retn, DWORD split);
  virtual TextThread *FindSingle(DWORD number);
  virtual ProcessRecord *GetProcessRecord(DWORD pid);
  virtual DWORD GetProcessIDByPath(LPCWSTR str);
  virtual void RemoveSingleThread(DWORD number);
  virtual void LockHookman();
  virtual void UnlockHookman();
  virtual void ResetRepeatStatus();
  virtual void ClearCurrent();
  virtual void AddLink(WORD from, WORD to);
  virtual void UnLink(WORD from);
  virtual void UnLinkAll(WORD from);
  virtual void SelectCurrent(DWORD num);
  virtual void DetachProcess(DWORD pid);
  virtual void SetCurrent(TextThread *it);
  //virtual void AddConsoleOutput(LPCWSTR text);

  void DispatchText(DWORD pid, BYTE *text, DWORD hook, DWORD retn, DWORD split, int len);
  void ClearText(DWORD pid, DWORD hook, DWORD retn, DWORD split);
  void RemoveProcessContext(DWORD pid);
  void RemoveSingleHook(DWORD pid, DWORD addr);
  void RegisterThread(TextThread*, DWORD);
  void RegisterPipe(HANDLE text, HANDLE cmd, HANDLE thread);
  void RegisterProcess(DWORD pid, DWORD hookman, DWORD module, DWORD engine);
  void UnRegisterProcess(DWORD pid);
  //void SetName(DWORD);

  DWORD GetCurrentPID();
  HANDLE GetCmdHandleByPID(DWORD pid);

  ThreadEventCallback RegisterThreadCreateCallback(ThreadEventCallback cf)
  { return (ThreadEventCallback)_InterlockedExchange((long*)&create,(long)cf); }

  ThreadEventCallback RegisterThreadRemoveCallback(ThreadEventCallback cf)
  { return (ThreadEventCallback)_InterlockedExchange((long*)&remove,(long)cf); }

  ThreadEventCallback RegisterThreadResetCallback(ThreadEventCallback cf)
  { return (ThreadEventCallback)_InterlockedExchange((long*)&reset,(long)cf); }

  ProcessEventCallback RegisterProcessAttachCallback(ProcessEventCallback cf)
  { return (ProcessEventCallback)_InterlockedExchange((long*)&attach,(long)cf); }

  ProcessEventCallback RegisterProcessDetachCallback(ProcessEventCallback cf)
  { return (ProcessEventCallback)_InterlockedExchange((long*)&detach,(long)cf); }

  ProcessEventCallback RegisterProcessNewHookCallback(ProcessEventCallback cf)
  { return (ProcessEventCallback)_InterlockedExchange((long*)&hook,(long)cf); }

  ProcessEventCallback ProcessNewHook() { return hook; }
  TextThread *GetCurrentThread() { return current; }
  ProcessRecord *Records() { return record; }
  ThreadTable *Table() { return thread_table; }

  //DWORD& SplitTime() { return split_time; }
  //DWORD& RepeatCount() { return repeat_count; }
  //DWORD& CyclicRemove() { return cyclic_remove; }
  //DWORD& GlobalFilter() { return global_filter; }

private:
  CRITICAL_SECTION hmcs;
  TextThread *current;
  ThreadEventCallback create,
                      remove,
                      reset;
  ProcessEventCallback attach,
                       detach,
                       hook;
  DWORD current_pid;
  ThreadTable *thread_table;
  HANDLE destroy_event;
  ProcessRecord record[MAX_REGISTER + 1];
  HANDLE text_pipes[MAX_REGISTER + 1],
         cmd_pipes[MAX_REGISTER + 1],
         recv_threads[MAX_REGISTER + 1];
  WORD register_count,
       new_thread_number;
};

// EOF
