#pragma once

// textthread.h
// 8/23/2013 jichi
// Branch: ITH/TextThread.h, rev 120

#include "ith/srv/textthread_p.h"
#include <intrin.h> // require _InterlockedExchange

struct RepeatCountNode {
  short repeat;
  short count;
  RepeatCountNode *next;

  //RepeatCountNode() : repeat(0), count(0), next(nullptr) {}
};

struct ThreadParameter {
  DWORD pid;
  DWORD hook;
  DWORD retn;
  DWORD spl;
};

#define CURRENT_SELECT 0x1000
#define REPEAT_NUMBER_DECIDED  0x2000
#define BUFF_NEWLINE 0x4000
#define CYCLIC_REPEAT 0x8000
#define COUNT_PER_FOWARD 0x200
#define REPEAT_DETECT 0x10000
#define REPEAT_SUPPRESS 0x20000
#define REPEAT_NEWLINE 0x40000

class TextThread;
typedef DWORD (* ThreadOutputFilterCallback)(TextThread *, BYTE *, DWORD, DWORD, PVOID);
typedef DWORD (* ThreadEventCallback)(TextThread *);

//extern DWORD split_time,repeat_count,global_filter,cyclic_remove;

class TextThread : public MyVector<BYTE, 0x200>
{
public:
  TextThread(DWORD pid, DWORD hook, DWORD retn, DWORD spl, WORD num);
  ~TextThread();
  //virtual void CopyLastSentence(LPWSTR str);
  //virtual void SetComment(LPWSTR);
  //virtual void ExportTextToFile(LPWSTR filename);

  virtual bool CheckCycle(TextThread *start);
  virtual DWORD GetThreadString(LPWSTR str, DWORD max);
  virtual DWORD GetEntryString(LPWSTR str, DWORD max = 0x200);

  void Reset();
  void AddText(BYTE *con,int len, bool new_line=false, bool console=false);
  void AddTextDirect(BYTE *con, int len);
  void RemoveSingleRepeatAuto(BYTE *con, int &len);
  void RemoveSingleRepeatForce(BYTE *con, int &len);
  void RemoveCyclicRepeat(BYTE *&con, int &len);
  void ResetRepeatStatus();
  void AddLineBreak();
  //void ResetEditText();
  void ComboSelectCurrent();
  void UnLinkAll();
  //void CopyLastToClipboard();

  //void AdjustPrevRepeat(DWORD len);
  //void PrevRepeatLength(DWORD &len);

  //bool AddToCombo();
  bool RemoveFromCombo();

  void SetNewLineFlag();
  void SetNewLineTimer();

  BYTE *GetStore(DWORD *len) { if (len) *len = used; return storage; }
  DWORD LastSentenceLen() { return used - last_sentence; }
  DWORD PID() const { return tp.pid; }
  DWORD Addr() const {return tp.hook; }
  DWORD &Status() { return status; }
  WORD Number() const { return thread_number; }
  WORD &Last() { return last; }
  WORD &LinkNumber() { return link_number; }
  UINT_PTR &Timer() { return timer; }
  ThreadParameter *GetThreadParameter() { return &tp; }
  TextThread *&Link() { return link; }
  //LPCWSTR GetComment() { return comment; }

  ThreadOutputFilterCallback RegisterOutputCallBack(ThreadOutputFilterCallback cb, PVOID data)
  {
    app_data = data;
    return (ThreadOutputFilterCallback)_InterlockedExchange((long*)&output,(long)cb);
  }

  ThreadOutputFilterCallback RegisterFilterCallBack(ThreadOutputFilterCallback cb, PVOID data)
  {
    app_data = data;
    return (ThreadOutputFilterCallback)_InterlockedExchange((long*)&filter,(long)cb);
  }

  void SetRepeatFlag() { status |= CYCLIC_REPEAT; }
  void ClearNewLineFlag() { status &= ~BUFF_NEWLINE; }
  void ClearRepeatFlag() { status &= ~CYCLIC_REPEAT; }

private:
  ThreadParameter tp;

  WORD thread_number,
       link_number;
  WORD last,
       align_space;
  WORD repeat_single;
  WORD repeat_single_current;
  WORD repeat_single_count;
  WORD repeat_detect_count;
  RepeatCountNode *head;

  TextThread *link;
  ThreadOutputFilterCallback filter,
                             output;
  PVOID app_data;
  //LPWSTR comment,
  LPWSTR thread_string;
  UINT_PTR timer;
  DWORD status,repeat_detect_limit;
  DWORD last_sentence,
        prev_sentence,
        sentence_length,
        repeat_index,
        last_time;
};

// EOF
