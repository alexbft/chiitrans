// eng/engine_p.cc
// 8/9/2013 jichi
// Branch: ITH_Engine/engine.cpp, revision 133
//
// 8/24/2013 TODO: Clean up the code

#ifdef _MSC_VER
# pragma warning (disable:4100)   // C4100: unreference formal parameter
#endif // _MSC_VER

#include "engine_p.h"
#include "util.h"
#include "ith/cli/cli.h"
#include "ith/sys/sys.h"
//#include "ith/common/growl.h"
#include "disasm/disasm.h"

//#define OutputConsole(...)  (void)0     // jichi 8/18/2013: I don't need OutputConsole

namespace { // unnamed

// jichi 8/18/2013: Original maximum relative address in ITH
//enum { MAX_REL_ADDR = 0x200000 };

// jichi 10/1/2013: Increase relative address limit. Certain game engine like Artemis has large code region
enum { MAX_REL_ADDR = 0x300000 };

static union {
  char text_buffer[0x1000];
  wchar_t wc_buffer[0x800];

  struct { // CodeSection
    DWORD base;
    DWORD size;
  } code_section[0x200];
};

char text_buffer_prev[0x1000];
DWORD buffer_index,
      buffer_length;

} // unnamed namespace

namespace Engine {

/********************************************************************************************
KiriKiri hook:
  Usually there are xp3 files in the game folder but also exceptions.
  Find TVP(KIRIKIRI) in the version description is a much more precise way.

  KiriKiri1 correspond to AGTH KiriKiri hook, but this doesn't always work well.
  Find call to GetGlyphOutlineW and go to function header. EAX will point to a
  structure contains character (at 0x14, [EAX+0x14]) we want. To split names into
  different threads AGTH uses [EAX], seems that this value stands for font size.
  Since KiriKiri is compiled by BCC and BCC fastcall uses EAX to pass the first
  parameter. Here we choose EAX is reasonable.
  KiriKiri2 is a redundant hook to catch text when 1 doesn't work. When this happens,
  usually there is a single GetTextExtentPoint32W contains irregular repetitions which
  is out of the scope of KS or KF. This time we find a point and split them into clean
  text threads. First find call to GetTextExtentPoint32W and step out of this function.
  Usually there is a small loop. It is this small loop messed up the text. We can find
  one ADD EBX,2 in this loop. It's clear that EBX is a string pointer goes through the
  string. After the loop EBX will point to the end of the string. So EBX-2 is the last
  char and we insert hook here to extract it.
********************************************************************************************/
void SpecialHookKiriKiri(DWORD esp_base, HookParam* hp, DWORD* data, DWORD* split, DWORD* len)
{
  DWORD p1,p2,p3;
  p1=*(DWORD*)(esp_base-0x14);
  p2=*(DWORD*)(esp_base-0x18);
  if ((p1>>16)==(p2>>16))
  {
    p3=*(DWORD*)p1;
    if (p3)
    {
      p3+=8;
      for (p2=p3+2;*(WORD*)p2;p2+=2);
      *len=p2-p3;
      *data=p3;
      p1=*(DWORD*)(esp_base-0x20);
      p1=*(DWORD*)(p1+0x74);
      *split=*(DWORD*)(esp_base+0x48)|p1;
    }
    else *len=0;
  }
  else *len=0;
}
void FindKiriKiriHook(DWORD fun, DWORD size, DWORD pt, DWORD flag)
{
  DWORD t,i,j,k,addr,sig;
  sig = flag ? 0x575653 : 0xec8b55;
  //WCHAR str[0x40];
  t=0;
  for (i=0x1000;i<size-4;i++)
  {
    if (*(WORD*)(pt+i)==0x15ff)
    {
      addr=*(DWORD*)(pt+i+2);
      if (addr >= pt && addr<=pt+size-4
          && *(DWORD *)addr == fun)
        t++;
      if (t == flag+1) { // We find call to GetGlyphOutlineW or GetTextExtentPoint32W.
        //swprintf(str,L"CALL addr:0x%.8X",i+pt);
        //OutputConsole(str);
        for (j = i; j > i - 0x1000; j--) {
          if (((*(DWORD *)(pt + j))&0xffffff) == sig) {
            if (flag)  { // We find the function entry. flag indicate 2 hooks.
              t = 0;  //KiriKiri2, we need to find call to this function.
              for (k=j+0x6000;k<j+0x8000;k++) //Empirical range.
                if (*(BYTE *)(pt + k) == 0xe8) {
                  if (k + 5 + *(DWORD *)(pt + k + 1) == j)
                    t++;
                  if (t == 2) {
                    //for (k+=pt+0x14; *(WORD*)(k)!=0xC483;k++);
                    //swprintf(str,L"Hook addr: 0x%.8X",pt+k);
                    //OutputConsole(str);
                    HookParam hp = {};
                    hp.addr = pt + k + 0x14;
                    hp.off = -0x14;
                    hp.ind = -0x2;
                    hp.split = -0xc;
                    hp.length_offset = 1;
                    hp.type |= USING_UNICODE|NO_CONTEXT|USING_SPLIT|DATA_INDIRECT;
                    NewHook(hp, L"KiriKiri2");
                    return;
                  }
                }
            } else {
              //swprintf(str,L"Hook addr: 0x%.8X",pt+j);
              //OutputConsole(str);
              HookParam hp = {};
              hp.addr = (DWORD)pt+j;
              hp.off = -0x8;
              hp.ind = 0x14;
              hp.split = -0x8;
              hp.length_offset = 1;
              hp.type |= USING_UNICODE|DATA_INDIRECT|USING_SPLIT|SPLIT_INDIRECT;
              NewHook(hp, L"KiriKiri1");
            }
            return;
          }
        }
        //OutputConsole(L"Failed to find function entry.");
      }
    }
  }
}
void InsertKiriKiriHook()
{
  FindKiriKiriHook((DWORD)GetGlyphOutlineW,module_limit_-module_base_,module_base_,0);
  FindKiriKiriHook((DWORD)GetTextExtentPoint32W,module_limit_-module_base_,module_base_,1);
  //RegisterEngineType(ENGINE_KIRIKIRI);
}
/********************************************************************************************
BGI hook:
  Usually game folder contains BGI.*. After first run BGI.gdb appears.

  BGI engine has font caching issue so the strategy is simple.
  First find call to TextOutA or TextOutW then reverse to function entry point,
  until full text is caught.
  After 2 tries we will get to the right place. Use ESP value to split text since
  it's likely to be different for different calls.
********************************************************************************************/
void FindBGIHook(DWORD fun, DWORD size, DWORD pt, WORD sig)
{
  WCHAR str[0x40];
  DWORD i,j,k,l;
  i=fun;
  //i=FindCallBoth(fun,size,pt);
  if (i==0)
  {
    swprintf(str,L"Can't find BGI hook: %.8X.",fun);
    //OutputConsole(str);
    return;
  }
  //swprintf(str,L"CALL addr: 0x%.8X",pt+i);
  //OutputConsole(str);
  for (j=i;j>i-0x100;j--)
    if ((*(WORD*)(pt+j))==sig) //Fun entry 1.
    {
      //swprintf(str,L"Entry 1: 0x%.8X",pt+j);
      //OutputConsole(str);
      for (k=i+0x100;k<i+0x800;k++)
        if (*(BYTE*)(pt+k)==0xE8)
          if (k+5+*(DWORD*)(pt+k+1)==j) //Find call to fun1.
          {
            //swprintf(str,L"CALL to entry 1: 0x%.8X",pt+k);
            //OutputConsole(str);
            for (l=k;l>k-0x100;l--)
              if ((*(WORD*)(pt+l))==0xEC83) //Fun entry 2.
              {
                //swprintf(str,L"Entry 2(final): 0x%.8X",pt+l);
                //OutputConsole(str);
                HookParam hp={};
                hp.addr=(DWORD)pt+l;
                hp.off=0x8;
                hp.split=-0x18;
                hp.length_offset=1;
                hp.type|=BIG_ENDIAN|USING_SPLIT;
                NewHook(hp,L"BGI");
                return;
              }
          }
    }
}
bool InsertBGIDynamicHook(LPVOID addr, DWORD frame, DWORD stack)
{
  if (addr==TextOutA||addr==TextOutW)
  {
    DWORD i=*(DWORD*)(stack+4)-module_base_;
    FindBGIHook(i,module_limit_-module_base_,module_base_,0xEC83);
    //RegisterEngineType(ENGINE_BGI);
    return true;
  }
  return false;
}
void InsertBGIHook()
{
  union{
    DWORD i;
    DWORD* id;
    BYTE* ib;
  };
  HookParam hp = {};
  for (i = module_base_ + 0x1000; i < module_limit_; i++)
  {
    if (ib[0] == 0x3D)
    {
      i++;
      if (id[0] == 0xFFFF) //cmp eax,0xFFFF
      {
        hp.addr = Util::FindEntryAligned(i, 0x40);
        if (hp.addr)
        {
          hp.off = 0xC;
          hp.split = -0x18;
          hp.type = BIG_ENDIAN | USING_SPLIT;
          hp.length_offset = 1;
          NewHook(hp,L"BGI");
          //RegisterEngineType(ENGINE_BGI);
          return;
        }
      }
    }
    if (ib[0] == 0x81)
    {
      if ((ib[1] & 0xF8) == 0xF8)
      {
        i+=2;
        if (id[0] == 0xFFFF) //cmp reg,0xFFFF
        {
          hp.addr = Util::FindEntryAligned(i, 0x40);
          if (hp.addr)
          {
            hp.off = 0xC;
            hp.split = -0x18;
            hp.type = BIG_ENDIAN | USING_SPLIT;
            hp.length_offset = 1;
            NewHook(hp,L"BGI");
            //RegisterEngineType(ENGINE_BGI);
            return;
          }
        }
      }
    }
  }
  //OutputConsole(L"Unknown BGI engine.");

  //OutputConsole(L"Probably BGI. Wait for text.");
  //SwitchTrigger(true);
  //trigger_fun_=InsertBGIDynamicHook;
}
/********************************************************************************************
Reallive hook:
  Process name is reallive.exe or reallive*.exe.

  Technique to find Reallive hook is quite different from 2 above.
  Usually Reallive engine has a font caching issue. This time we wait
  until the first call to GetGlyphOutlineA. Reallive engine usually set
  up stack frames so we can just refer to EBP to find function entry.

********************************************************************************************/
bool InsertRealliveDynamicHook(LPVOID addr, DWORD frame, DWORD stack)
{
  if (addr != GetGlyphOutlineA)
    return false;
  if (DWORD i = frame) {
    i = *(DWORD *)(i + 4);
    for (DWORD j = i; j > i - 0x100; j--)
      if (*(WORD*)j==0xEC83) {
        HookParam hp = {};
        hp.addr = j;
        hp.off = 0x14;
        hp.split = -0x18;
        hp.length_offset=1;
        hp.type |= BIG_ENDIAN|USING_SPLIT;
        NewHook(hp, L"RealLive");
        //RegisterEngineType(ENGINE_REALLIVE);
        return true;;
      }
  }
  return true; // jichi 10/1/2013: Why true?
}
void InsertRealliveHook()
{
  //OutputConsole(L"Probably Reallive. Wait for text.");
  trigger_fun_=InsertRealliveDynamicHook;
  SwitchTrigger(true);
}
/**
 *  jichi 8/17/2013:  SiglusEngine from siglusengine.exe
 *  The old hook does not work for new games.
 *  The new hook cannot recognize character names.
 *  Insert old first. As the pattern could also be found in the old engine.
 */

/**
 *  jichi 8/16/2013: Insert new siglus hook
 *  See (CaoNiMaGeBi): http://tieba.baidu.com/p/2531786952
 *  Example:
 *  0153588b9534fdffff8b43583bd7
 *  0153 58          add dword ptr ds:[ebx+58],edx
 *  8b95 34fdffff    mov edx,dword ptr ss:[ebp-2cc]
 *  8b43 58          mov eax,dword ptr ds:[ebx+58]
 *  3bd7             cmp edx,edi    ; hook here
 *
 *  /HW-1C@D9DB2:SiglusEngine.exe
 *  - addr: 892338 (0xd9db2)
 *  - extern_fun: 0x0
 *  - function: 0
 *  - hook_len: 0
 *  - ind: 0
 *  - length_offset: 1
 *  - module: 356004490 (0x1538328a)
 *  - off: 4294967264 (0xffffffe0L, 0x-20)
 *  - recover_len: 0
 *  - split: 0
 *  - split_ind: 0
 *  - type: 66   (0x42)
 */
bool InsertSiglus2Hook()
{
  //BYTE ins[] = { // size = 14
  //  0x01,0x53, 0x58,                // 0153 58          add dword ptr ds:[ebx+58],edx
  //  0x8b,0x95, 0x34,0xfd,0xff,0xff, // 8b95 34fdffff    mov edx,dword ptr ss:[ebp-2cc]
  //  0x8b,0x43, 0x58,                // 8b43 58          mov eax,dword ptr ds:[ebx+58]
  //  0x3b,0xd7                       // 3bd7             cmp edx,edi ; hook here
  //};
  //enum { cur_ins_size = 2 };
  //enum { cur_ins_offset = sizeof(ins) - cur_ins_size }; // = 14 - 2  = 12, current inst is the last one
  BYTE ins[] = {
    0x3b,0xd7,  // cmp edx,edi
    0x75,0x4b   // jnz short
  };
  enum { cur_ins_offset = 0 };
  ULONG range = min(module_limit_ - module_base_, MAX_REL_ADDR);
  ULONG reladdr = SearchPattern(module_base_, range, ins, sizeof(ins));
  if (!reladdr)
    //OutputConsole(L"Not SiglusEngine2");
    return false;

  HookParam hp = {};
  hp.type = USING_UNICODE;
  hp.length_offset = 1;
  hp.off = -0x20;
  hp.addr = module_base_ + reladdr + cur_ins_offset;

  //index = SearchPattern(module_base_, size,ins, sizeof(ins));
  //ITH_GROWL_DWORD2(base, index);

  NewHook(hp, L"SiglusEngine2");
  //OutputConsole(L"SiglusEngine2");
  return true;
}

void SpecialHookSiglus(DWORD esp_base, HookParam* hp, DWORD* data, DWORD* split, DWORD* len)
{
  __asm
  {
    mov edx,esp_base
    mov ecx,[edx-0xc]
    mov eax,[ecx+0x14]
    add ecx,4
    cmp eax,0x8
    cmovnb ecx,[ecx]
    mov edx,len
    add eax,eax
    mov [edx],eax
    mov edx,data
    mov [edx],ecx
  }
}

// jichi: 8/17/2013: Change return type to bool
bool InsertSiglus1Hook()
{
  //BYTE ins[8]={0x33,0xC0,0x8B,0xF9,0x89,0x7C,0x24}; // jichi 8/18/2013: wrong count?
  BYTE ins[]={0x33,0xc0,0x8b,0xf9,0x89,0x7c,0x24};
  ULONG range = max(module_limit_ - module_base_, MAX_REL_ADDR);
  ULONG reladdr = SearchPattern(module_base_, range, ins, sizeof(ins));
  if (!reladdr)  // jichi 8/17/2013: Add "== 0" check to prevent breaking new games
    //OutputConsole(L"Unknown SiglusEngine");
    return false;

  DWORD base = module_base_ + reladdr;
  DWORD limit = base - 0x100;
  while (base > limit) {
    if (*(WORD*)base == 0xff6a) {
      HookParam hp = {};
      hp.addr = base;
      hp.extern_fun = SpecialHookSiglus;
      hp.type= EXTERN_HOOK|USING_UNICODE;
      NewHook(hp, L"SiglusEngine");
      //RegisterEngineType(ENGINE_SIGLUS);
      return true;
    }
    base--;
  }
  return false;
}

// jichi 8/17/2013: Insert old first. As the pattern could also be found in the old engine.
bool InsertSiglusHook()
{ return InsertSiglus1Hook() || InsertSiglus2Hook(); }

/********************************************************************************************
MAJIRO hook:
  Game folder contains both data.arc and scenario.arc. arc files is
  quite common seen so we need 2 files to confirm it's MAJIRO engine.

  Font caching issue. Find call to TextOutA and the function entry.

  The original Majiro hook will catch furiga mixed with the text.
  To split them out we need to find a parameter. Seems there's no
  simple way to handle this case.
  At the function entry, EAX seems to point to a structure to describe
  current  drawing context. +28 seems to be font size. +48 is negative
  if furigana. I don't know exact meaning of this structure,
  just do memory comparisons and get the value working for current release.

********************************************************************************************/
void SpecialHookMajiro(DWORD esp_base, HookParam* hp, DWORD* data, DWORD* split, DWORD* len)
{
  __asm
  {
    mov edx,esp_base
    mov edi,[edx+0xC]
    mov eax,data
    mov [eax],edi
    or ecx,0xFFFFFFFF
    xor eax,eax
    repne scasb
    not ecx
    dec ecx
    mov eax,len
    mov [eax],ecx
    mov eax,[edx+4]
    mov edx,[eax+0x28]
    mov eax,[eax+0x48]
    sar eax,0x1F
    mov dh,al
    mov ecx,split
    mov [ecx],edx
  }
}
void InsertMajiroHook()
{
  HookParam hp={};

  /*hp.off=0xC;
  hp.split=4;
  hp.split_ind=0x28;
  hp.type|=USING_STRING|USING_SPLIT|SPLIT_INDIRECT;*/
  hp.addr = Util::FindCallAndEntryAbs((DWORD)TextOutA,module_limit_-module_base_,module_base_,0xEC81);
  hp.type = EXTERN_HOOK;
  hp.extern_fun = SpecialHookMajiro;
  NewHook(hp, L"MAJIRO");
  //RegisterEngineType(ENGINE_MAJIRO);
}
/********************************************************************************************
CMVS hook:
  Process name is cmvs.exe or cnvs.exe or cmvs*.exe. Used by PurpleSoftware games.

  Font caching issue. Find call to GetGlyphOutlineA and the function entry.
********************************************************************************************/
void InsertCMVSHook()
{
  HookParam hp={};
  hp.off=0x8;
  hp.split=-0x18;
  hp.type|=BIG_ENDIAN|USING_SPLIT;
  hp.length_offset=1;
  hp.addr = Util::FindCallAndEntryAbs((DWORD)GetGlyphOutlineA,module_limit_-module_base_,module_base_,0xEC83);
  NewHook(hp, L"CMVS");
  //RegisterEngineType(ENGINE_CMVS);
}
/********************************************************************************************
rUGP hook:
  Process name is rugp.exe. Used by AGE/GIGA games.

  Font caching issue. Find call to GetGlyphOutlineA and keep stepping out functions.
  After several tries we comes to address in rvmm.dll and everything is catched.
  We see CALL [E*X+0x*] while EBP contains the character data.
  It's not as simple to reverse in rugp at run time as like reallive since rugp dosen't set
  up stack frame. In other words EBP is used for other purpose. We need to find alternative
  approaches.
  The way to the entry of that function gives us clue to find it. There is one CMP EBP,0x8140
  instruction in this function and that's enough! 0x8140 is the start of SHIFT-JIS
  characters. It's determining if ebp contains a SHIFT-JIS character. This function is not likely
  to be used in other ways. We simply search for this instruction and place hook around.
********************************************************************************************/
void SpecialHookRUGP(DWORD esp_base, HookParam* hp, DWORD* data, DWORD* split, DWORD* len)
{
  DWORD* stack = (DWORD*)esp_base;
  DWORD i,val;
  for (i = 0; i < 4; i++)
  {
    val = *stack++;
    if ((val>>16) == 0) break;

  }
  if (i < 4)
  {
    hp->off = i << 2;
    *data = val;
    *len = 2;
    hp->extern_fun = 0;
    hp->type &= ~EXTERN_HOOK;
  }
  else
  {
    *len = 0;
  }
}

// jichi 10/1/2013: Change return type to bool
bool InsertRUGPHook()
{
  DWORD low, high;
  if (!FillRange(L"rvmm.dll", &low, &high))
    return false;
  WCHAR str[0x40];
  LPVOID ch = (LPVOID)0x8140;
  enum { range = 0x20000 };
  DWORD t = SearchPattern(low + range, high - low - range, &ch, 4) + range;
  BYTE *s = (BYTE *)(low + t);
  //if (t) {
  if (t != range) { // jichi 10/1/2013: Changed to compare with 0x20000
    if (*(s - 2) != 0x81)
      return false;
    if (DWORD i = Util::FindEntryAligned((DWORD)s, 0x200)) {
      HookParam hp = {};
      hp.addr = i;
      //hp.off= -8;
      hp.length_offset = 1;
      hp.extern_fun = SpecialHookRUGP;
      hp.type |= BIG_ENDIAN|EXTERN_HOOK;
      NewHook(hp, L"rUGP");
      return true;
    }
  } else {
    t = SearchPattern(low, range, &s, 4);
    if (!t)
      //OutputConsole(L"Can't find characteristic instruction.");
      return false;

    s = (BYTE *)(low + t);
    for (int i = 0; i < 0x200; i++, s--)
      if (s[0] == 0x90
          && *(DWORD *)(s - 3) == 0x90909090) {
        t = low+ t - i + 1;
        swprintf(str, L"HookAddr 0x%.8x", t);
        //OutputConsole(str);
        HookParam hp = {};
        hp.addr = t;
        hp.off = 0x4;
        hp.length_offset = 1;
        hp.type |= BIG_ENDIAN;
        NewHook(hp, L"rUGP");
        //RegisterEngineType(ENGINE_RUGP);
        return true;
      }
  }
  return false;
//rt:
  //OutputConsole(L"Unknown rUGP engine.");
}
/********************************************************************************************
Lucifen hook:
  Game folder contains *.lpk. Used by Navel games.
  Hook is same to GetTextExtentPoint32A, use ESP to split name.
********************************************************************************************/
void InsertLucifenHook()
{
  HookParam hp={};
  hp.addr=(DWORD)GetTextExtentPoint32A;
  hp.off=8;
  hp.split=-0x18;
  hp.length_offset=3;
  hp.type=USING_STRING|USING_SPLIT;
  NewHook(hp,L"Lucifen");
  //RegisterEngineType(ENGINE_LUCIFEN);
}
/********************************************************************************************
System40 hook:
  System40 is a game engine developed by Alicesoft.
  Afaik, there are 2 very different types of System40. Each requires a particular hook.

  Pattern 1: Either SACTDX.dll or SACT2.dll exports SP_TextDraw.
  The first relative call in this function draw text to some surface.
  Text pointer is return by last absolute indirect call before that.
  Split parameter is a little tricky. The first register pushed onto stack at the begining
  usually is used as font size later. According to instruction opcode map, push
  eax -- 50, ecx -- 51, edx -- 52, ebx --53, esp -- 54, ebp -- 55, esi -- 56, edi -- 57
  Split parameter value:
  eax - -8,   ecx - -C,  edx - -10, ebx - -14, esp - -18, ebp - -1C, esi - -20, edi - -24
  Just extract the low 4 bit and shift left 2 bit, then minus by -8,
  will give us the split parameter. e.g. push ebx 53->3 *4->C, -8-C=-14.
  Sometimes if split function is enabled, ITH will split text spoke by different
  character into different thread. Just open hook dialog and uncheck split parameter.
  Then click modify hook.

  Pattern 2: *engine.dll exports SP_SetTextSprite.
  At the entry point, EAX should be a pointer to some structure, character at +0x8.
  Before calling this function, the caller put EAX onto stack, we can also find this
  value on stack. But seems parameter order varies from game release. If a future
  game breaks the EAX rule then we need to disassemble the caller code to determine
  data offset dynamically.
********************************************************************************************/

void InsertAliceHook1(DWORD addr, DWORD module, DWORD limit)
{
  HookParam hp={};
  DWORD c,i,j,s=addr;
  if (s==0) return;
  for (i=s;i<s+0x100;i++)
  {
    if (*(BYTE*)i==0xE8) //Find the first relative call.
    {
      j=i+5+*(DWORD*)(i+1);
      if (j>module&&j<limit)
      {
        while (1) //Find the first register push onto stack.
        {
          c=disasm((BYTE*)s);
          if (c==1) break;
          s+=c;
        }
        c=*(BYTE*)s;
        hp.addr=j;
        hp.off=-0x8;
        hp.split=-8-((c&0xF)<<2);
        hp.type=USING_STRING|USING_SPLIT;
        //if (s>j) hp.type^=USING_SPLIT;
        NewHook(hp,L"System40");
        //RegisterEngineType(ENGINE_SYS40);
        return;
      }
    }
  }
}
void InsertAliceHook2(DWORD addr)
{
  HookParam hp={};
  hp.addr=addr;
  if (hp.addr==0) return;
  hp.off=-0x8;
  hp.ind=0x8;
  hp.length_offset=1;
  hp.type=DATA_INDIRECT;
  NewHook(hp,L"System40");
  //RegisterEngineType(ENGINE_SYS40);
}

// jichi 8/23/2013 Move here from engine.cc
// Do not work for the latest Alice games
bool InsertAliceHook()
{
  DWORD low, high, addr;
  if (::GetFunctionAddr("SP_TextDraw",&addr,&low,&high,0) && addr) {
    InsertAliceHook1(addr,low,low+high);
    return true;
  }
  if (::GetFunctionAddr("SP_SetTextSprite",&addr,&low,&high,0) && addr) {
    InsertAliceHook2(addr);
    return true;
  }
  return false;
}

/********************************************************************************************
AtelierKaguya hook:
  Game folder contains message.dat. Used by AtelierKaguya games.
  Usually has font caching issue with TextOutA.
  Game engine uses EBP to set up stack frame so we can easily trace back.
  Keep step out until it's in main game module. We notice that either register or
  stack contains string pointer before call instruction. But it's not quite stable.
  In-depth analysis of the called function indicates that there's a loop traverses
  the string one character by one. We can set a hook there.
  This search process is too complex so I just make use of some characteristic
  instruction(add esi,0x40) to locate the right point.
********************************************************************************************/
void InsertAtelierHook()
{
  DWORD sig,i,j;
  //FillRange(process_name_,&base,&size);
  //size=size-base;
  sig = 0x40C683; //add esi,0x40
  //i=module_base_+SearchPattern(module_base_,module_limit_-module_base_,&sig,3);
  for (i = module_base_; i < module_limit_ - 4; i++) {
    sig = *(DWORD *)i & 0xFFFFFF;
    if (0x40C683 == sig)
      break;
  }
  if (i < module_limit_ - 4)
    for (j=i-0x200; i>j; i--)
      if (*(DWORD *)i == 0xFF6ACCCC) { //Find the function entry
        HookParam hp = {};
        hp.addr = i+2;
        hp.off = 8;
        hp.split = -0x18;
        hp.length_offset = 1;
        hp.type = USING_SPLIT;
        NewHook(hp, L"Atelier KAGUYA");
        //RegisterEngineType(ENGINE_ATELIER);
        return;
      }

  //OutputConsole(L"Unknown Atelier KAGUYA engine.");
}
/********************************************************************************************
CIRCUS hook:
  Game folder contains advdata folder. Used by CIRCUS games.
  Usually has font caching issues. But trace back from GetGlyphOutline gives a hook
  which generate repetition.
  If we study circus engine follow Freaka's video, we can easily discover that
  in the game main module there is a static buffer, which is filled by new text before
  it's drawing to screen. By setting a hardware breakpoint there we can locate the
  function filling the buffer. But we don't have to set hardware breakpoint to search
  the hook address if we know some characteristic instruction(cmp al,0x24) around there.
********************************************************************************************/
bool InsertCircusHook1() // jichi 10/2/2013: Change return type to bool
{
  for (DWORD i = module_base_ + 0x1000; i < module_limit_ - 4; i++)
    if (*(WORD *)i==0xA3C)  //cmp al, 0xA; je
      for (DWORD j = i; j < i + 0x100; j++) {
        BYTE c = *(BYTE *)j;
        if (c == 0xC3)
          break;
        if (c == 0xe8) {
          DWORD k = *(DWORD *)(j+1)+j+5;
          if (k > module_base_ && k < module_limit_) {
            HookParam hp = {};
            hp.addr = k;
            hp.off = 0xc;
            hp.split = -0x18;
            hp.length_offset = 1;
            hp.type = DATA_INDIRECT|USING_SPLIT;
            NewHook(hp, L"CIRCUS");
            //RegisterEngineType(ENGINE_CIRCUS);
            return true;
          }
        }
      }
      //break;
  //OutputConsole(L"Unknown CIRCUS engine");
  return false;
}

bool InsertCircusHook2() // jichi 10/2/2013: Change return type to bool
{
  for (DWORD i = module_base_ + 0x1000; i < module_limit_ -4; i++)
    if ((*(DWORD *)i & 0xffffff) == 0x75243c) { // cmp al, 24; je
      if (DWORD j = Util::FindEntryAligned(i, 0x80)) {
        HookParam hp = {};
        hp.addr = j;
        hp.off = 0x8;
        hp.type = USING_STRING;
        NewHook(hp, L"CIRCUS");
        //RegisterEngineType(ENGINE_CIRCUS);
        return true;
      }
      break;
    }
  //OutputConsole(L"Unknown CIRCUS engine.");
  return false;
}

/********************************************************************************************
ShinaRio hook:
  Game folder contains rio.ini.
  Problem of default hook GetTextExtentPoint32A is that the text repeat one time.
  But KF just can't resolve the issue. ShinaRio engine always perform integrity check.
  So it's very difficult to insert a hook into the game module. Freaka suggests to refine
  the default hook by adding split parameter on the stack. So far there is 2 different
  version of ShinaRio engine that needs different split parameter. Seems this value is
  fixed to the last stack frame. We just navigate to the entry. There should be a
  sub esp,* instruction. This value plus 4 is just the offset we need.

  New ShinaRio engine (>=2.48) uses different approach.
********************************************************************************************/
void SpecialHookShina(DWORD esp_base, HookParam* hp, DWORD* data, DWORD* split, DWORD* len)
{
  DWORD ptr=*(DWORD*)(esp_base-0x20);
  *split=ptr;
  char* str=*(char**)(ptr+0x160);
  strcpy(text_buffer,str);
  int skip=0;
  for (str=text_buffer;*str;str++)
  {
    if (str[0]==0x5F)
    {
      if (str[1]==0x72)
      {
        str[0]=str[1]=1;
      }
      else if (str[1]==0x74)
      {
        while (str[0]!=0x2F) *str++=1;
        *str=1;
      }
    }
  }
  for (str=text_buffer;str[skip];)
  {
    if (str[skip]==1)
    {
      skip++;
    }
    else
    {
      str[0]=str[skip];
      str++;
    }
  }
  str[0]=0;
  if (strcmp(text_buffer,text_buffer_prev)==0)
  {
    *len=0;
  }
  else
  {
    for (skip=0;text_buffer[skip];skip++)
      text_buffer_prev[skip]=text_buffer[skip];
    text_buffer_prev[skip]=0;
    *data=(DWORD)text_buffer_prev;
    *len=skip;
  }
}

// jichi 8/27/2013
// Return ShinaRio version number
// The head of Rio.ini usally looks like:
//     [椎名里緒 v2.49]
// This function will return 49 in the above case.
//
// Games from アトリエさくら do not have Rio.ini, but $procname.ini.
int GetShinaRioVersion()
{
  int ret = 0;
  HANDLE hFile = IthCreateFile(L"RIO.INI", FILE_READ_DATA, FILE_SHARE_READ, FILE_OPEN);
  if (hFile == INVALID_HANDLE_VALUE)  {
    size_t len = wcslen(process_name_);
    if (len > 3) {
      wchar_t fname[MAX_PATH];
      wcscpy(fname, process_name_);
      fname[len -1] = 'i';
      fname[len -2] = 'n';
      fname[len -3] = 'i';
      hFile = IthCreateFile(fname, FILE_READ_DATA, FILE_SHARE_READ, FILE_OPEN);
    }
  }

  if (hFile != INVALID_HANDLE_VALUE)  {
    IO_STATUS_BLOCK ios;
    //char *buffer,*version;//,*ptr;
    enum { BufferSize = 0x40 };
    char buffer[BufferSize];
    NtReadFile(hFile, 0, 0, 0, &ios, buffer, BufferSize, 0, 0);
    NtClose(hFile);
    if (buffer[0] == '[') {
      buffer[0x3f] = 0; // jichi 8/24/2013: prevent strstr from overflow
      if (char *version = strstr(buffer, "v2."))
        sscanf(version + 3, "%d", &ret); // +3 to skip "v2."
    }
  }
  return ret;
}

// jichi 8/24/2013: Rewrite ShinaRio logic.
bool InsertShinaHook()
{
  int ver = GetShinaRioVersion();
  if (ver >= 48) { // v2.48, v2.49
    HookParam hp = {};
    hp.addr = (DWORD)GetTextExtentPoint32A;
    hp.extern_fun = SpecialHookShina;
    hp.type = EXTERN_HOOK|USING_STRING;
    NewHook(hp, L"ShinaRio");
    //RegisterEngineType(ENGINE_SHINA);
    return true;

  } else if (ver > 40) // <= v2.47. Older games like あやかしびと does not require hcode
    if (DWORD s = Util::FindCallAndEntryBoth(
          (DWORD)GetTextExtentPoint32A,
          module_limit_ - module_base_,
          (DWORD)module_base_,
          0xec81)) {
      HookParam hp = {};
      hp.addr = (DWORD)GetTextExtentPoint32A;
      hp.off = 0x8;
      hp.split = *(DWORD*)(s + 2) + 4;
      hp.length_offset = 1;
      hp.type = DATA_INDIRECT|USING_SPLIT;
      NewHook(hp, L"ShinaRio");
     //RegisterEngineType(ENGINE_SHINA);
    }
  return false;
}

bool InsertWaffleDynamicHook(LPVOID addr, DWORD frame, DWORD stack)
{
  if (addr != GetTextExtentPoint32A)
    return false;

  DWORD handler;
  __asm
  {
    mov eax,fs:[0]
    mov eax,[eax]
    mov eax,[eax]
    mov eax,[eax]
    mov eax,[eax]
    mov eax,[eax]
    mov ecx, [eax + 4]
    mov handler, ecx
  }

  union
  {
    DWORD i;
    BYTE* ib;
    DWORD* id;
  };
  // jichi 9/30/2013: Fix the bug in ITH logic where j is unintialized
  for (i = module_base_ + 0x1000; i < module_limit_ - 4; i++)
    if (*id == handler && *(ib - 1) == 0x68)
      if (DWORD t = Util::FindEntryAligned(i, 0x40)) {
        HookParam hp = {};
        hp.addr = t;
        hp.off = 8;
        hp.ind = 4;
        hp.length_offset = 1;
        hp.type = DATA_INDIRECT;
        NewHook(hp, L"Waffle");
        return true;
      }
  //OutputConsole(L"Unknown waffle engine.");
  return true;
}
//  DWORD retn,limit,str;
//  WORD ch;
//  NTSTATUS status;
//  MEMORY_BASIC_INFORMATION info;
//  str = *(DWORD*)(stack+0xC);
//  ch = *(WORD*)str;
//  if (ch<0x100) return false;
//  limit = (stack | 0xFFFF) + 1;
//  __asm int 3
//  for (stack += 0x10; stack < limit; stack += 4)
//  {
//    str = *(DWORD*)stack;
//    if ((str >> 16) != (stack >> 16))
//    {
//      status = NtQueryVirtualMemory(NtCurrentProcess(),(PVOID)str,MemoryBasicInformation,&info,sizeof(info),0);
//      if (!NT_SUCCESS(status) || info.Protect & PAGE_NOACCESS) continue; //Accessible
//    }
//    if (*(WORD*)(str + 4) == ch) break;
//  }
//  if (stack < limit)
//  {
//    for (limit = stack + 0x100; stack < limit ; stack += 4)
//    if (*(DWORD*)stack == -1)
//    {
//      retn = *(DWORD*)(stack + 4);
//      if (retn > module_base_ && retn < module_limit_)
//      {
//        HookParam hp = {};
//        hp.addr = retn + *(DWORD*)(retn - 4);
//        hp.length_offset = 1;
//        hp.off = -0x20;
//        hp.ind = 4;
//        //hp.split = 0x1E8;
//        hp.type = DATA_INDIRECT;
//        NewHook(hp, L"WAFFLE");
//        //RegisterEngineType(ENGINE_WAFFLE);
//        return true;
//      }
//
//    }
//
//  }

void InsertWaffleHook()
{
  for (DWORD i = module_base_ + 0x1000; i < module_limit_ - 4; i++)
    if (*(DWORD *)i == 0xac68) {
      HookParam hp = {};
      hp.addr = i;
      hp.length_offset = 1;
      hp.off = -0x20;
      hp.ind = 4;
      hp.split = 0x1E8;
      hp.type = DATA_INDIRECT|USING_SPLIT;
      NewHook(hp, L"WAFFLE");
      return;
    }
  //OutputConsole(L"Probably Waffle. Wait for text.");
  trigger_fun_ = InsertWaffleDynamicHook;
  SwitchTrigger(true);
}

void InsertTinkerBellHook()
{
  //DWORD s1,s2,i;
  //DWORD ch=0x8141;
  DWORD i;
  WORD count;
  count = 0;
  HookParam hp = {};
  hp.length_offset = 1;
  hp.type = BIG_ENDIAN | NO_CONTEXT;
  for (i = module_base_; i< module_limit_ - 4; i++) {
    if (*(DWORD*)i == 0x8141) {
      BYTE t = *(BYTE*)(i - 1);
      if (t == 0x3d || t == 0x2d) {
        hp.off = -0x8;
        hp.addr = i - 1;
      } else if (*(BYTE*)(i-2) == 0x81) {
        t &= 0xF8;
        if (t == 0xF8 || t == 0xE8) {
          hp.off = -8 - ((*(BYTE*)(i-1) & 7) << 2);
          hp.addr = i - 2;
        }
      }
      if (hp.addr) {
        WCHAR hook_name[0x20];
        memcpy(hook_name, L"TinkerBell", 0x14);
        hook_name[0xA] = L'0' + count;
        hook_name[0xB] = 0;
        NewHook(hp, hook_name);
        count++;
        hp.addr = 0;
      }
    }
  }
}

//  s1=SearchPattern(module_base_,module_limit_-module_base_-4,&ch,4);
//  if (s1)
//  {
//    for (i=s1;i>s1-0x400;i--)
//    {
//      if (*(WORD*)(module_base_+i)==0xEC83)
//      {
//        hp.addr=module_base_+i;
//        NewHook(hp,L"C.System");
//        break;
//      }
//    }
//  }
//  s2=s1+SearchPattern(module_base_+s1+4,module_limit_-s1-8,&ch,4);
//  if (s2)
//  {
//    for (i=s2;i>s2-0x400;i--)
//    {
//      if (*(WORD*)(module_base_+i)==0xEC83)
//      {
//        hp.addr=module_base_+i;
//        NewHook(hp,L"TinkerBell");
//        break;
//      }
//    }
//  }
//  //if (count)
  //RegisterEngineType(ENGINE_TINKER);

void InsertLuneHook()
{
  HookParam hp = {};
  DWORD c = Util::FindCallOrJmpAbs((DWORD)ExtTextOutA, module_limit_ - module_base_, (DWORD)module_base_, true);
  if (!c)
    return;
  hp.addr = Util::FindCallAndEntryRel(c, module_limit_ - module_base_, (DWORD)module_base_, 0xec8b55);
  if (!hp.addr)
    return;
  hp.off = 4;
  hp.type = USING_STRING;
  NewHook(hp, L"MBL-Furigana");
  c = Util::FindCallOrJmpAbs((DWORD)GetGlyphOutlineA, module_limit_ - module_base_, (DWORD)module_base_, true);
  if (!c)
    return;
  hp.addr = Util::FindCallAndEntryRel(c, module_limit_ - module_base_, (DWORD)module_base_, 0xec8b55);
  if (!hp.addr)
    return;
  hp.split = -0x18;
  hp.length_offset = 1;
  hp.type = BIG_ENDIAN|USING_SPLIT;
  NewHook(hp,L"MBL");
  //RegisterEngineType(ENGINE_LUNE);
}
/********************************************************************************************
YU-RIS hook:
  Becomes common recently. I first encounter this game in Whirlpool games.
  Problem is name is repeated multiple times.
  Step out of function call to TextOuA, just before call to this function,
  there should be a piece of code to calculate the length of the name.
  This length is 2 for single character name and text,
  For a usual name this value is greater than 2.
********************************************************************************************/

void InsertWhirlpoolHook()
{
  DWORD i,t;
  //IthBreak();
  DWORD entry = Util::FindCallAndEntryBoth((DWORD)TextOutA,module_limit_-module_base_,module_base_,0xEC83);
  if (!entry)
    return;
  entry = Util::FindCallAndEntryRel(entry-4,module_limit_-module_base_,module_base_,0xEC83);
  if (!entry)
    return;
  entry = Util::FindCallOrJmpRel(entry-4,module_limit_-module_base_-0x10000,module_base_+0x10000,false);
  for (i = entry - 4; i > entry - 0x100; i--) {
    if (*(WORD *)i==0xC085) {
      t = *(WORD *)(i+2);
      if ((t&0xff) == 0x76) {
        t = 4;
        break;
      }
      if ((t&0xffff) == 0x860f) {
        t = 8;
        break;
      }
    }
  }
  if (i == entry - 0x100)
    return;
  HookParam hp={};
  hp.addr = i+t;
  hp.off = -0x24;
  hp.split = -0x8;
  hp.type = USING_STRING|USING_SPLIT;
  NewHook(hp, L"YU-RIS");
  //RegisterEngineType(ENGINE_WHIRLPOOL);
}

void InsertCotophaHook()
{
  HookParam hp = {};
  hp.addr = Util::FindCallAndEntryAbs((DWORD)GetTextMetricsA,module_limit_-module_base_,module_base_,0xEC8B55);
  if (!hp.addr)
    return;
  hp.off = 4;
  hp.split = -0x1c;
  hp.type = USING_UNICODE|USING_SPLIT|USING_STRING;
  NewHook(hp, L"Cotopha");
  //RegisterEngineType(ENGINE_COTOPHA);
}

void InsertCatSystem2Hook()
{
  HookParam hp = {};
  //DWORD search=0x95EB60F;
  //DWORD j,i=SearchPattern(module_base_,module_limit_-module_base_,&search,4);
  //if (i==0) return;
  //i+=module_base_;
  //for (j=i-0x100;i>j;i--)
  //  if (*(DWORD*)i==0xCCCCCCCC) break;
  //if (i==j) return;
  //hp.addr=i+4;
  //hp.off=-0x8;
  //hp.ind=4;
  //hp.split=4;
  //hp.split_ind=0x18;
  //hp.type=BIG_ENDIAN|DATA_INDIRECT|USING_SPLIT|SPLIT_INDIRECT;
  //hp.length_offset=1;

  hp.addr = Util::FindCallAndEntryAbs((DWORD)GetTextMetricsA, module_limit_ - module_base_, module_base_, 0xff6acccc);
  if (!hp.addr)
    return;
  hp.addr += 2;
  hp.off = 8;
  hp.split = -0x10;
  hp.length_offset = 1;
  hp.type = BIG_ENDIAN|USING_SPLIT;
  NewHook(hp, L"CatSystem2");
  //RegisterEngineType(ENGINE_CATSYSTEM);
}

void InsertNitroPlusHook()
{
  BYTE ins[]={0xb0, 0x74, 0x53};
  DWORD addr = SearchPattern(module_base_,module_limit_-module_base_,ins,3);
  if (!addr)
    return;
  addr += module_base_;
  ins[0] = *(BYTE *)(addr+3)&3;
  while (*(WORD *)addr != 0xec83)
    addr--;
  HookParam hp = {};
  hp.addr = addr;
  hp.off = -0x14+ (ins[0] << 2);
  hp.length_offset = 1;
  hp.type |= BIG_ENDIAN;
  NewHook(hp, L"NitroPlus");
  //RegisterEngineType(ENGINE_NITROPLUS);
}

void InsertRetouchHook()
{
  HookParam hp = {};
  if (GetFunctionAddr("?printSub@RetouchPrintManager@@AAE_NPBDAAVUxPrintData@@K@Z", &hp.addr, nullptr, nullptr, nullptr)) {
    hp.off = 4;
    hp.type = USING_STRING;
    NewHook(hp, L"RetouchSystem");
    return;
  } else if (GetFunctionAddr("?printSub@RetouchPrintManager@@AAEXPBDKAAH1@Z", &hp.addr, nullptr, nullptr, nullptr)) {
    hp.off = 4;
    hp.type = USING_STRING;
    NewHook(hp, L"RetouchSystem");
    return;
  }
  //OutputConsole(L"Unknown RetouchSystem engine.");
}

namespace { // unnamed
/********************************************************************************************
Malie hook:
  Process name is malie.exe.
  This is the most complicate code I have made. Malie engine store text string in
  linked list. We need to insert a hook to where it travels the list. At that point
  EBX should point to a structure. We can find character at -8 and font size at +10.
  Also need to enable ITH suppress function.
********************************************************************************************/
bool InsertMalieHook1()
{
  DWORD sig1 = 0x5e3c1,
        sig2 = 0xc383;
  DWORD i = SearchPattern(module_base_,module_limit_-module_base_,&sig1,3);
  if (!i)
    return false;
  DWORD j = i + module_base_ + 3;
  i = SearchPattern(j, module_limit_-j,&sig2,2);
  //if (!j)
  if (!i) // jichi 8/19/2013: Change the condition fro J to I
    return false;
  HookParam hp = {};
  hp.addr = j + i;
  hp.off = -0x14;
  hp.ind = -0x8;
  hp.split = -0x14;
  hp.split_ind = 0x10;
  hp.length_offset = 1;
  hp.type = USING_UNICODE|USING_SPLIT|DATA_INDIRECT|SPLIT_INDIRECT;
  NewHook(hp, L"Malie");
  //RegisterEngineType(ENGINE_MALIE);
  return true;
}

void SpecialHookMalie(DWORD esp_base, HookParam *hp, DWORD *data, DWORD *split, DWORD *len)
{
  static DWORD furi_flag; // jichi 8/20/2013: Move furi flag to the function
  DWORD index,ch,ptr;
  ch = *(DWORD *)(esp_base - 0x8)&0xffff;
  ptr = *(DWORD *)(esp_base - 0x24);
  *data = ch;
  *len = 2;
  if (furi_flag) {
    index = *(DWORD *)(esp_base - 0x10);
    if (*(WORD *)(ptr + index*2-2)<0xA)
      furi_flag = 0;
  }
  else if (ch==0xa) {
    furi_flag= 1;
    len = 0;
  }
  *split = furi_flag;
}

bool InsertMalieHook2() // jichi 8/20/2013: Change return type to boolean
{
  BYTE ins[]={0x66,0x3d,0x1,0x0};
  DWORD p;
  BYTE* ptr;
  p = SearchPattern(module_base_,module_limit_-module_base_,ins,4);
  if (p) {
    ptr=(BYTE *)(p+module_base_);
_again:
    if (*(WORD *)ptr == 0x3d66) {
      ptr += 4;
      if (ptr[0] == 0x75) {
        ptr += ptr[1]+2;
        goto _again;
      }
      if (*(WORD *)ptr == 0x850f) {
        ptr += *(DWORD *)(ptr + 2) + 6;
        goto _again;
      }
    }
    HookParam hp = {};
    hp.addr = (DWORD)ptr + 4;
    hp.off = -8;
    hp.length_offset = 1;
    hp.extern_fun = SpecialHookMalie;
    hp.type = EXTERN_HOOK|USING_SPLIT|USING_UNICODE|NO_CONTEXT;
    NewHook(hp, L"Malie");
    //RegisterEngineType(ENGINE_MALIE);
    return true;
  }
  //OutputConsole(L"Unknown malie system.");
  return false;
}

/**
 *  jichi 8/20/2013: Add hook for sweet light BRAVA!!
 *
 *  BRAVA!! /H code: "/HWN-4:C@1A3DF4:malie.exe"
 *  - addr: 1719796 = 0x1a3df4
 *  - extern_fun: 0x0
 *  - function: 0
 *  - hook_len: 0
 *  - ind: 0
 *  - length_offset: 1
 *  - module: 751199171 = 0x2cc663c3
 *  - off: 4294967288 = 0xfffffff8L = -0x8
 *  - recover_len: 0
 *  - split: 12 = 0xc
 *  - split_ind: 0
 *  - type: 1106 = 0x452
 */
bool InsertMalie2Hook()
{
  // 001a3dee    6900 70000000   imul eax,dword ptr ds:[eax],70
  // 001a3df4    0200            add al,byte ptr ds:[eax]   ; this is the place to hook
  // 001a3df6    50              push eax
  // 001a3df7    0069 00         add byte ptr ds:[ecx],ch
  // 001a3dfa    0000            add byte ptr ds:[eax],al
  BYTE ins1[] = {
    0x40,            // inc eax
    0x89,0x56, 0x08, // mov dword ptr ds:[esi+0x8],edx
    0x33,0xd2,       // xor edx,edx
    0x89,0x46, 0x04  // mov dword ptr ds:[esi+0x4],eax
  };
  ULONG range1 = min(module_limit_ - module_base_, MAX_REL_ADDR);
  ULONG p = SearchPattern(module_base_, range1, ins1, sizeof(ins1));
  //reladdr = 0x1a3df4;
  if (!p)
    //ITH_MSG(0, "Wrong1", "t", 0);
    //OutputConsole(L"Not malie2 engine");
    return false;

  p += sizeof(ins1); // skip ins1
  //BYTE ins2[] = { 0x85, 0xc0 }; // test eax,eax
  WORD ins2 =  0xc085; // test eax,eax
  enum { range2 = 0x200 };
  enum { cur_ins_offset = 0 };
  ULONG q = SearchPattern(module_base_ + p, range2, &ins2, sizeof(ins2));
  if (!q)
    //ITH_MSG(0, "Wrong2", "t", 0);
    //OutputConsole(L"Not malie2 engine");
    return false;

  ULONG reladdr = p + q;
  HookParam hp = {};
  hp.addr = module_base_ + reladdr + cur_ins_offset;
  hp.off = -8;
  hp.length_offset = 1;
  hp.type = USING_SPLIT|USING_UNICODE|NO_CONTEXT;
  hp.split = 0xc;
  NewHook(hp, L"Malie2");

  //ITH_GROWL_DWORD2(hp.addr, reladdr);
  //RegisterEngineType(ENGINE_MALIE);
  return true;
}

} // unnamed namespace

bool InsertMalieHook()
{
  if (IthCheckFile(L"tools.dll"))
    return InsertMalieHook1(); // For Dies irae, etc
  else {
    // jichi 8/20/2013: Add hook for sweet light engine
    // Insert both malie and malie2 hook.
    bool b1 = InsertMalieHook2(),
         b2 =  InsertMalie2Hook(); // jichi 8/20/2013 CHECKPOINT: test new sweet light game later
    return b1 || b2; // prevent shortcut
  }
}

/********************************************************************************************
EMEHook hook: (Contributed by Freaka)
  EmonEngine is used by LoveJuice company and TakeOut. Earlier builds were apparently
  called Runrun engine. String parsing varies a lot depending on the font settings and
  speed setting. E.g. without antialiasing (which very early versions did not have)
  uses TextOutA, fast speed triggers different functions then slow/normal. The user can
  set his own name and some odd control characters are used (0x09 for line break, 0x0D
  for paragraph end) which is parsed and put together on-the-fly while playing so script
  can't be read directly.
********************************************************************************************/
void InsertEMEHook()
{
  DWORD c = Util::FindCallOrJmpAbs((DWORD)IsDBCSLeadByte,module_limit_-module_base_,(DWORD)module_base_,false);

  /* no needed as first call to IsDBCSLeadByte is correct, but sig could be used for further verification
  WORD sig = 0x51C3;
  while (c && (*(WORD*)(c-2)!=sig))
  {
    //-0x1000 as FindCallOrJmpAbs always uses an offset of 0x1000
    c = Util::FindCallOrJmpAbs((DWORD)IsDBCSLeadByte,module_limit_-c-0x1000+4,c-0x1000+4,false);
  } */

  if (c) {
    HookParam hp={};
    hp.addr=c;
    hp.off=-0x8;
    hp.length_offset=1;
    hp.type=NO_CONTEXT|DATA_INDIRECT;
    NewHook(hp,L"EmonEngine");
    //OutputConsole(L"EmonEngine, hook will only work with text speed set to slow or normal!");
  }
  //else OutputConsole(L"Unknown EmonEngine engine");
}
void SpecialRunrunEngine(DWORD esp_base, HookParam* hp, DWORD* data, DWORD* split, DWORD* len)
{
  DWORD p1=*(DWORD*)(esp_base-0x8)+*(DWORD*)(esp_base-0x10); //eax+edx
  *data=*(WORD*)(p1);
  *len=2;
}
void InsertRREHook()
{
  DWORD c = Util::FindCallOrJmpAbs((DWORD)IsDBCSLeadByte,module_limit_-module_base_,(DWORD)module_base_,false);
  if (c) {
    WORD sig = 0x51C3;

    HookParam hp={};
    hp.addr=c;
    hp.length_offset=1;
    hp.type=NO_CONTEXT|DATA_INDIRECT;
    if ((*(WORD *)(c-2)!=sig)) {
      hp.extern_fun=SpecialRunrunEngine;
      hp.type|=EXTERN_HOOK;
      NewHook(hp, L"RunrunEngine Old");
    } else {
      hp.off=-0x8;
      NewHook(hp,L"RunrunEngine");
    }
    //OutputConsole(L"RunrunEngine, hook will only work with text speed set to slow or normal!");
  }
  //else OutputConsole(L"Unknown RunrunEngine engine");
}
void InsertMEDHook()
{
  DWORD i,j,k,t;
  for (i = module_base_; i<module_limit_ - 4; i++)
  {
    if (*(DWORD*)i == 0x8175) //cmp *, 8175
    {
      for (j = i, k = i + 0x100; j < k; j++)
      {
        if (*(BYTE*)j == 0xE8)
        {
          t = j + 5 + *(DWORD*)(j+1);
          if (t > module_base_ && t < module_limit_)
          {
            HookParam hp = {};
            hp.addr = t;
            hp.off = -0x8;
            hp.length_offset = 1;
            hp.type = BIG_ENDIAN;
            NewHook(hp, L"MED");
            //RegisterEngineType(ENGINE_MED);
            return;
          }
        }
      }
    }
  }
  //OutputConsole(L"Unknown MED engine.");
}
/********************************************************************************************
AbelSoftware hook:
  The game folder usually is made up many no extended name files(file name doesn't have '.').
  And these files have common prefix which is the game name, and 2 digit in order.


********************************************************************************************/
void InsertAbelHook()
{
  DWORD character[2]={0xC981D48A,0xFFFFFF00};
  DWORD i, j = SearchPattern(module_base_,module_limit_-module_base_,character,8);
  if (j)
  {
    j+=module_base_;
    for (i=j-0x100;j>i;j--)
    {
      if (*(WORD*)j==0xFF6A)
      {
        HookParam hp={};
        hp.addr=j;
        hp.off=4;
        hp.type=USING_STRING|NO_CONTEXT;
        NewHook(hp,L"AbelSoftware");
        //RegisterEngineType(ENGINE_ABEL);
        return;
      }
    }
  }
}
bool InsertLiveDynamicHook(LPVOID addr, DWORD frame, DWORD stack)
{
  if (addr!=GetGlyphOutlineA) return false;
  DWORD i,j,k;
  HookParam hp={};
  i=frame;
  if (i!=0)
  {
    k=*(DWORD*)i;
    k=*(DWORD*)(k+4);
    if (*(BYTE*)(k-5)!=0xE8)
      k=*(DWORD*)(i+4);
    j=k+*(DWORD*)(k-4);
    if (j>module_base_&&j<module_limit_)
    {
      hp.addr=j;
      hp.off=-0x10;
      hp.length_offset=1;
      hp.type|=BIG_ENDIAN;
      NewHook(hp,L"Live");
      //RegisterEngineType(ENGINE_LIVE);
      return true;
    }
  }
  return true;
}
//void InsertLiveHook()
//{
//  OutputConsole(L"Probably Live. Wait for text.");
//  trigger_fun_=InsertLiveDynamicHook;
//  SwitchTrigger(true);
//}
void InsertLiveHook()
{
  BYTE sig[7]={0x64,0x89,0x20,0x8B,0x45,0x0C,0x50};
  DWORD i = SearchPattern(module_base_,module_limit_-module_base_,sig,7);
  if (i)
  {
    HookParam hp={};
    hp.addr=i+module_base_;
    hp.off=-0x10;
    hp.length_offset=1;
    hp.type|=BIG_ENDIAN;
    NewHook(hp,L"Live");
    //RegisterEngineType(ENGINE_LIVE);
  }
  //else OutputConsole(L"Unknown Live engine");
}
void InsertBrunsHook()
{
  HookParam hp = {};
  hp.off = 4;
  hp.length_offset = 1;
  hp.type = USING_UNICODE;
  if (IthCheckFile(L"libscr.dll"))
  {
    hp.type |= MODULE_OFFSET|FUNCTION_OFFSET;
    hp.function = 0x8B24C7BC;
    //?push_back@?$basic_string@GU?$char_traits@G@std@@V?$allocator@G@2@@std@@QAEXG@Z
    if (IthCheckFile(L"msvcp90.dll")) {
      hp.module = 0xc9c36a5b; // msvcp90.dll
      NewHook(hp, L"Bruns");
      //RegisterEngineType(ENGINE_BRUNS);
      return;
    }
    if (IthCheckFile(L"msvcp80.dll")) {
      hp.module = 0xa9c36a5b; // msvcp80.dll
      NewHook(hp, L"Bruns");
      //RegisterEngineType(ENGINE_BRUNS);
      return;
    }
    // jichi 8/17/2013: MSVCRT 10.0 and 11.0
    if (IthCheckFile(L"msvcp100.dll")) {
      hp.module = 0xb571d760; // 3044136800;
      NewHook(hp, L"Bruns");
      //RegisterEngineType(ENGINE_BRUNS);
      return;
    }
    if (IthCheckFile(L"msvcp110.dll")) {
      hp.module = 0xd571d760; // 3581007712;
      NewHook(hp, L"Bruns");
      //RegisterEngineType(ENGINE_BRUNS);
      return;
    }
  }
  else
  {
    DWORD j,k,t;
    union
    {
      DWORD i;
      DWORD* id;
      WORD* iw;
      BYTE* ib;
    };
    k = module_limit_ - 4;
    for (i = module_base_ + 0x1000; i < k; i++)
    {
      if (*id != 0xFF) continue;//cmp reg,0xFF
      i += 4;
      if (*iw != 0x8F0F) continue;//jg
      i += 2;
      i += *id + 4;
      for (j = i + 0x40; i < j; i++)
      {
        if (*ib != 0xE8) continue;
        i++;
        t = i + 4 + *id;
        if (t > module_base_ && t <module_limit_)
        {
          i = t;
          for (j = i + 0x80; i < j; i++)
          {
            if (*ib != 0xE8) continue;
            i++;
            t = i + 4 + *id;
            if (t > module_base_ && t <module_limit_)
            {
              hp.addr = t;
              hp.type |= DATA_INDIRECT;
              NewHook(hp, L"Bruns");
              return;
            }
          }
          k = i; //Terminate outer loop.
          break; //Terminate inner loop.
        }
      }
    }
  }
  //OutputConsole(L"Unknown Bruns engine.");
}

/**
 * jichi 8/18/2013:  QLIE identified by GameData/data0.pack
 *
 * The old hook cannot recognize new games.
 */

namespace { // unnamed
/**
 * jichi 8/18/2013: new QLIE hook
 * See: http://www.hongfire.com/forum/showthread.php/420362-QLIE-engine-Hcode
 *
 * Ins:
 * 55 8B EC 53 8B 5D 1C
 * - 55         push ebp    ; hook here
 * - 8bec       mov ebp, esp
 * - 53         push ebx
 * - 8B5d 1c    mov ebx, dword ptr ss:[ebp+1c]
 *
 * /HBN14*0@4CC2C4
 * - addr: 5030596  (0x4cc2c4)
 * - extern_fun: 0x0
 * - function: 0
 * - hook_len: 0
 * - ind: 0
 * - length_offset: 1
 * - module: 0
 * - off: 20    (0x14)
 * - recover_len: 0
 * - split: 0
 * - split_ind: 0
 * - type: 1032 (0x408)
 */
bool InsertQLIE2Hook()
{
  BYTE ins[] = { // size = 7
    0x55,           // 55       push ebp    ; hook here
    0x8b,0xec,      // 8bec     mov ebp, esp
    0x53,           // 53       push ebx
    0x8b,0x5d, 0x1c // 8b5d 1c  mov ebx, dword ptr ss:[ebp+1c]
  };
  enum { cur_ins_offset = 0 }; // current instruction is the first one
  ULONG range = min(module_limit_ - module_base_, MAX_REL_ADDR);
  ULONG reladdr = SearchPattern(module_base_, range, ins, sizeof(ins));
  if (!reladdr)
    //OutputConsole(L"Not QLIE2");
    return false;

  HookParam hp = {};
  hp.type = DATA_INDIRECT | NO_CONTEXT; // 0x408
  hp.length_offset = 1;
  hp.off = 0x14;
  hp.addr = module_base_ + reladdr + cur_ins_offset; //sizeof(ins) - cur_ins_size;

  NewHook(hp, L"QLIE2");
  //OutputConsole(L"QLIE2");
  return true;
}

// jichi: 8/18/2013: Change return type to bool
bool InsertQLIE1Hook()
{
  DWORD i,j,t;
  for (i = module_base_ + 0x1000; i < module_limit_ - 4; i++ )
  {
    if (*(DWORD*)i == 0x7FFE8347) //inc edi, cmp esi,7f
    {
      t = 0;
      for (j = i; j < i + 0x10; j++)
      {
        if (*(DWORD*)j == 0xA0) //cmp esi,a0
        {
          t = 1;
          break;
        }
      }
      if (t)
      {
        for (j = i; j > i - 0x100; j--)
        {
          if (*(DWORD*)j == 0x83EC8B55) //push ebp, mov ebp,esp, sub esp,*
          {
            HookParam hp = {};
            hp.addr = j;
            hp.off = 0x18;
            hp.split = -0x18;
            hp.length_offset = 1;
            hp.type = DATA_INDIRECT | USING_SPLIT;
            NewHook(hp,L"QLIE");
            //RegisterEngineType(ENGINE_FRONTWING);
            return true;
          }
        }
      }
    }
  }
  //OutputConsole(L"Unknown QLIE engine");
  return false;
}

} // unnamed namespace

// jichi 8/18/2013: Add new hook
bool InsertQLIEHook()
{ return InsertQLIE1Hook() || InsertQLIE2Hook(); }

/********************************************************************************************
CandySoft hook:
  Game folder contains many *.fpk. Engine name is SystemC.
  I haven't seen this engine in other company/brand.

  AGTH /X3 will hook lstrlenA. One thread is the exactly result we want.
  But the function call is difficult to located programmatically.
  I find a equivalent points which is more easy to search.
  The script processing function needs to find 0x5B'[',
  so there should a instruction like cmp reg,5B
  Find this position and navigate to function entry.
  The first parameter is the string pointer.
  This approach works fine with game later than つよきす２学期.

  But the original つよきす is quite different. I handle this case separately.

********************************************************************************************/

namespace { // unnamed

// jichi 8/23/2013: split into two different engines
//if (_wcsicmp(process_name_, L"systemc.exe")==0)
// Process name is "SystemC.exe"
bool InsertCandyHook1()
{
  for (DWORD i = module_base_ + 0x1000; i < module_limit_ - 4; i++)
    if ((*(DWORD*)i&0xFFFFFF) == 0x24F980) // cmp cl,24
      for (DWORD j = i, k = i - 0x100; j > k; j--)
        if (*(DWORD*)j == 0xC0330A8A) { // mov cl,[edx]; xor eax,eax
          HookParam hp = {};
          hp.addr = j;
          hp.off = -0x10;
          hp.type = USING_STRING;
          NewHook(hp, L"SystemC");
          //RegisterEngineType(ENGINE_CANDY);
          return true;
        }
  return false;
}

// jichi 8/23/2013: Process name is NOT "SystemC.exe"
bool InsertCandyHook2()
{
  for (DWORD i = module_base_ + 0x1000; i < module_limit_ - 4 ;i++)
    if (*(WORD *)i == 0x5B3c || // cmp al,0x5B
        (*(DWORD *)i&0xfff8fc) == 0x5bf880) // cmp reg,0x5B
      for (DWORD j = i, k = i - 0x100; j > k; j--)
        if ((*(DWORD*)j&0xffff) == 0x8b55) { // push ebp, mov ebp,esp, sub esp,*
          HookParam hp = {};
          hp.addr = j;
          hp.off = 4;
          hp.type = USING_STRING;
          NewHook(hp, L"SystemC");
          //RegisterEngineType(ENGINE_CANDY);
          return true;
        }
  return false;
}

/** jichi 10/2/2013: CHECKPOINT
 *
 *  [5/31/2013] 恋もHもお勉強も、おまかせ！お姉ちゃん部
 *  base = 0xf20000
 *  + シナリオ: /HSN-4@104A48:ANEBU.EXE
 *    - off: 4294967288 = 0xfffffff8 = -8
 ,    - type: 1025 = 0x401
 *  + 選択肢: /HSN-4@104FDD:ANEBU.EXE
 *    - off: 4294967288 = 0xfffffff8 = -8
 *    - type: 1089 = 0x441
 */
//bool InsertCandyHook3()
//{
//  return false; // CHECKPOINT
//  BYTE ins[] = {
//    0x83,0xc4, 0x0c, // add esp,0xc ; hook here
//    0x0f,0xb6,0xc0,  // movzx eax,al
//    0x85,0xc0,       // test eax,eax
//    0x75, 0x0e       // jnz XXOO ; it must be 0xe, or there will be duplication
//  };
//  enum { cur_ins_offset = 0 };
//  ULONG range = min(module_limit_ - module_base_, MAX_REL_ADDR);
//  ULONG reladdr = SearchPattern(module_base_, range, ins, sizeof(ins));
//  reladdr = 0x104a48;
//  ITH_GROWL_DWORD(module_base_);
//  //ITH_GROWL_DWORD3(reladdr, module_base_, range);
//  if (!reladdr)
//    return false;
//
//  HookParam hp = {};
//  hp.addr = module_base_ + reladdr + cur_ins_offset;
//  hp.off = -8;
//  hp.type = USING_STRING|NO_CONTEXT;
//  NewHook(hp, L"Candy");
//  return true;
//}

} // unnamed namespace

// jichi 10/2/2013: Add new candy hook
bool InsertCandyHook()
{
  if (0 == _wcsicmp(process_name_, L"systemc.exe"))
    return InsertCandyHook1();
  else
    return InsertCandyHook2();
    //bool b2 = InsertCandyHook2(),
    //     b3 = InsertCandyHook3();
    //return b2 || b3;
}

/********************************************************************************************
Apricot hook:
  Game folder contains arc.a*.
  This engine is heavily based on new DirectX interfaces.
  I can't find a good place where text is clean and not repeating.
  The game processes script encoded in UTF32-like format.
  I reversed the parsing algorithm of the game and implemented it partially.
  Only name and text data is needed.

********************************************************************************************/
void SpecialHookApricot(DWORD esp_base, HookParam* hp, DWORD* data, DWORD* split, DWORD* len)
{
  DWORD reg_esi = *(DWORD *)(esp_base - 0x20);
  DWORD reg_esp = *(DWORD *)(esp_base - 0x18);
  DWORD base = *(DWORD *)(reg_esi + 0x24);
  DWORD index = *(DWORD *)(reg_esi + 0x3c);
  DWORD *script = (DWORD *)(base + index*4),*end;
  *split=reg_esp;
  if (script[0]==L'<')
  {
    for (end=script;*end!=L'>';end++);
    if (script[1]==L'N')
    {
      if (script[2]==L'a'&&script[3]==L'm'&&script[4]==L'e')
      {
        buffer_index=0;
        for (script+=5;script<end;script++)
          if (*script>0x20)
            wc_buffer[buffer_index++]=*script&0xFFFF;
        *len=buffer_index<<1;
        *data=(DWORD)wc_buffer;
        *split|=1<<31;
      }
    }
    else if (script[1]==L'T')
    {
      if (script[2]==L'e'&&script[3]==L'x'&&script[4]==L't')
      {
        buffer_index=0;
        for (script+=5;script<end;script++)
        {
          if (*script>0x40)
          {
            while (*script==L'{')
            {
              script++;
              while (*script!=L'\\')
              {
                wc_buffer[buffer_index++]=*script&0xFFFF;
                script++;
              }
              while (*script++!=L'}');
            }
            wc_buffer[buffer_index++]=*script&0xFFFF;
          }
        }
        *len=buffer_index<<1;
        *data=(DWORD)wc_buffer;
      }
    }
  }

}
void InsertApricotHook()
{
  DWORD i,j,k;
  for (i=module_base_+0x1000;i<module_limit_-4;i++)
  {
    if ((*(DWORD*)i&0xFFF8FC)==0x3CF880) //cmp reg,0x3C
    {
      j=i+3;
      for (k=i+0x100;j<k;j++)
      {
        if ((*(DWORD*)j&0xFFFFFF)==0x4C2) //retn 4
        {
          HookParam hp={};
          hp.addr=j+3;
          hp.extern_fun=SpecialHookApricot;
          hp.type=EXTERN_HOOK|USING_STRING|USING_UNICODE|NO_CONTEXT;
          NewHook(hp,L"ApRicot");
          //RegisterEngineType(ENGINE_APRICOT);
          return;
        }
      }
    }
  }
}
void InsertStuffScriptHook()
{
  HookParam hp = {};
  hp.addr = (DWORD)GetTextExtentPoint32A;
  hp.off = 8;
  hp.split = -0x18;
  hp.type = USING_STRING | USING_SPLIT;
  NewHook(hp, L"StuffScriptEngine");
  //RegisterEngine(ENGINE_STUFFSCRIPT);
}
void InsertTriangleHook()
{
  DWORD i,j,k,t;
  for (i = module_base_; i < module_limit_ - 4; i++)
  {
    if ((*(DWORD*)i & 0xFFFFFF) == 0x75403C) // cmp al,0x40; jne
    {
      j = i + 4 + *(BYTE*)(i+3);
      for (k = j + 0x20; j < k; j++)
      {
        if (*(BYTE*)j == 0xE8)
        {
          t = j + 5 + *(DWORD*)(j+1);
          if (t > module_base_ && t < module_limit_)
          {
            HookParam hp = {};
            hp.addr = t;
            hp.off = 4;
            hp.type = USING_STRING;
            NewHook(hp, L"Triangle");
            //RegisterEngineType(ENGINE_TRIANGLE);
            return;
          }
        }
      }

    }
  }
  //OutputConsole(L"Old/Unknown Triangle engine.");
}
void InsertPensilHook()
{
  DWORD i,j;
  for (i = module_base_; i < module_limit_ - 4; i++)
  {
    if (*(DWORD*)i == 0x6381) //cmp *,8163
    {
      j = Util::FindEntryAligned(i,0x100);
      if (j)
      {
        HookParam hp = {};
        hp.addr = j;
        hp.off = 8;
        hp.length_offset = 1;
        NewHook(hp,L"Pencil");
        return;
        //RegisterEngineType(ENGINE_PENSIL);
      }
    }
  }
  //OutputConsole(L"Unknown Pensil engine.");
}
bool IsPensilSetup()
{
  HANDLE hFile = IthCreateFile(L"PSetup.exe",FILE_READ_DATA, FILE_SHARE_READ, FILE_OPEN);
  FILE_STANDARD_INFORMATION info;
  IO_STATUS_BLOCK ios;
  LPVOID buffer = 0;
  NtQueryInformationFile(hFile, &ios, &info, sizeof(info), FileStandardInformation);
  NtAllocateVirtualMemory(NtCurrentProcess(), &buffer, 0,
    &info.AllocationSize.LowPart, MEM_RESERVE|MEM_COMMIT, PAGE_READWRITE);
  NtReadFile(hFile, 0,0,0, &ios, buffer, info.EndOfFile.LowPart, 0, 0);
  NtClose(hFile);
  BYTE* b = (BYTE*)buffer;
  bool result = 0;
  DWORD len = info.EndOfFile.LowPart & ~1;
  if (len == info.AllocationSize.LowPart) len-=2;
  b[len] = 0;
  b[len + 1] = 0;
  if (wcsstr((LPWSTR)buffer, L"PENSIL")) result = 1;
  else if (wcsstr((LPWSTR)buffer, L"Pensil")) result =1;
  NtFreeVirtualMemory(NtCurrentProcess(), &buffer, &info.AllocationSize.LowPart, MEM_RELEASE);
  return result;
}
void SpecialHookDebonosu(DWORD esp_base, HookParam* hp, DWORD* data, DWORD* split, DWORD* len)
{
  DWORD retn = *(DWORD*)esp_base;
  if (*(WORD*)retn == 0xC483) //add esp, *
    hp->off = 4;
  else
    hp->off = -0x8;
  hp->type ^= EXTERN_HOOK;
  hp->extern_fun = 0;
  *data = *(DWORD*)(esp_base + hp->off);
  *len = strlen((char*)*data);
}
void InsertDebonosuHook()
{
  DWORD fun,addr,search,i,j,k,push;
  if (GetFunctionAddr("lstrcatA",&fun,0,0,0) == 0) return;
  addr = Util::FindImportEntry(module_base_, fun);
  if (addr == 0) return;
  search = 0x15FF | (addr << 16);
  addr >>= 16;
  for (i = module_base_; i < module_limit_ - 4; i++)
  {
    if (*(DWORD*)i != search) continue;
    if (*(WORD*)(i + 4) != addr) continue;// call dword ptr lstrcatA
    if (*(BYTE*)(i - 5) != 0x68) continue;// push $
    push = *(DWORD*)(i - 4);
    j = i + 6;
    for (k = j + 0x10; j < k; j++)
    {
      if (*(BYTE*)j != 0xB8) continue;
      if (*(DWORD*)(j + 1) != push) continue;
      HookParam hp = {};
      hp.addr = Util::FindEntryAligned(i, 0x200);
      hp.extern_fun = SpecialHookDebonosu;
      if (hp.addr == 0) continue;
      hp.type = USING_STRING | EXTERN_HOOK;
      NewHook(hp, L"Debonosu");
      //RegisterEngineType(ENGINE_DEBONOSU);
      return;
    }
  }
  //OutputConsole(L"Unknown Debonosu engine.");
}

void SpecialHookSofthouse(DWORD esp_base, HookParam* hp, DWORD* data, DWORD* split, DWORD* len)
{
  DWORD i;
  union
  {
    LPWSTR string_u;
    PCHAR string_a;
  };
  string_u=*(LPWSTR*)(esp_base+4);
  if (hp->type&USING_UNICODE)
  {
    *len=wcslen(string_u);
    for (i=0;i<*len;i++)
    {
      if (string_u[i]==L'>'||string_u[i]==L']')
      {
        *data=(DWORD)(string_u+i+1);
        *split=0;
        *len-=i+1;
        *len<<=1;
        return;
      }
    }
  }
  else
  {
    *len=strlen(string_a);
    for (i=0;i<*len;i++)
    {
      if (string_a[i]=='>'||string_a[i]==']')
      {
        *data=(DWORD)(string_a+i+1);
        *split=0;
        *len-=i+1;
        return;
      }
    }
  }
}
bool InsertSofthouseDynamicHook(LPVOID addr, DWORD frame, DWORD stack)
{
  if (addr!=DrawTextExA&&addr!=DrawTextExW) return false;
  DWORD high,low,i,j,k;
  Util::GetCodeRange(module_base_,&low,&high);
  i=stack;
  j=(i&0xFFFF0000)+0x10000;
  for (;i<j;i+=4)
  {
    k=*(DWORD*)i;
    if (k>low&&k<high)
    {
      if ((*(WORD*)(k-6)==0x15FF)||*(BYTE*)(k-5)==0xE8)
      {
        HookParam hp={};
        hp.off=0x4;
        hp.extern_fun=SpecialHookSofthouse;
        hp.type=USING_STRING|EXTERN_HOOK;
        if (addr==DrawTextExW) {hp.type|=USING_UNICODE;}
        i=*(DWORD*)(k-4);
        if (*(DWORD*)(k-5)==0xE8)
          hp.addr=i+k;
        else
          hp.addr=*(DWORD*)i;
        NewHook(hp,L"SofthouseChara");
        //RegisterEngineType(ENGINE_SOFTHOUSE);
        return true;
      }
    }
  }
  //OutputConsole(L"Fail to insert hook for SofthouseChara.");
  return true;
}

void InsertSoftHouseHook()
{
  //OutputConsole(L"Probably SoftHouseChara. Wait for text.");
  trigger_fun_=InsertSofthouseDynamicHook;
  SwitchTrigger(true);
}

void SpecialHookCaramelBox(DWORD esp_base, HookParam* hp, DWORD* data, DWORD* split, DWORD* len)
{
  DWORD reg_ecx = *(DWORD*)(esp_base+hp->off);
  BYTE* ptr = (BYTE*)reg_ecx;
  buffer_index = 0;
  while (ptr[0])
  {
    if (ptr[0] == 0x28) // Furigana format: (Kanji,Furi)
    {
      ptr++;
      while (ptr[0]!=0x2C) //Copy Kanji
        text_buffer[buffer_index++] = *ptr++;
      while (ptr[0]!=0x29) // Skip Furi
        ptr++;
      ptr++;
    }
    else if (ptr[0] == 0x5C) ptr +=2;
    else
    {
      text_buffer[buffer_index++] = ptr[0];
      if (LeadByteTable[ptr[0]]==2)
      {
        ptr++;
        text_buffer[buffer_index++] = ptr[0];
      }
      ptr++;
    }
  }
  *len = buffer_index;
  *data = (DWORD)text_buffer;
  *split = 0;
}
// jichi 10/1/2013: Change return type to bool
bool InsertCaramelBoxHook()
{
  union { DWORD i; BYTE* pb; WORD* pw; DWORD* pd; };
  DWORD reg = -1;
  for (i = module_base_ + 0x1000; i < module_limit_ - 4; i++) {
    if (*pd == 0x7ff3d) //cmp eax, 7ff
      reg = 0;
    else if ((*pd & 0xfffff8fc) == 0x07fff880) //cmp reg, 7ff
      reg = pb[1] & 0x7;

    if (reg == -1)
      continue;

    DWORD flag = 0;
    if (*(pb - 6) == 3) { //add reg, [ebp+$disp_32]
      if (*(pb - 5) == (0x85 | (reg << 3)))
        flag = 1;
    } else if (*(pb - 3) == 3) { //add reg, [ebp+$disp_8]
      if (*(pb - 2) == (0x45 | (reg << 3)))
        flag = 1;
    } else if (*(pb - 2) == 3) { //add reg, reg
      if (((*(pb - 1) >> 3) & 7)== reg)
        flag = 1;
    }
    reg = -1;
    if (flag) {
      for (DWORD j = i, k = i - 0x100; j > k; j--) {
        if ((*(DWORD *)j&0xffff00ff) == 0x1000b8) { //mov eax,10??
          HookParam hp = {};
          hp.addr = j & ~0xF;
          hp.extern_fun = SpecialHookCaramelBox;
          hp.type = USING_STRING | EXTERN_HOOK;
          for (i &= ~0xFFFF; i < module_limit_ - 4; i++)
          {
            if (pb[0] == 0xE8)
            {
              pb++;
              if (pd[0] + i + 4 == hp.addr)
              {
                pb += 4;
                if ((pd[0] & 0xFFFFFF) == 0x04C483)
                  hp.off = 4;
                else hp.off = -0xC;
                break;
              }
            }
          }
          if (hp.off == 0)
            return false;
          NewHook(hp, L"CaramelBox");
          //RegisterEngineType(ENGINE_CARAMEL);
          return true;
        }
      }
    }
  }
  return false;
//_unknown_engine:
  //OutputConsole(L"Unknown CarmelBox engine.");
}

#if 0 // jichi 8/23/2013: Why remove wolf hook?!
void InsertWolfHook()
{
  __asm int 3
  DWORD c1 = Util::FindCallAndEntryAbs((DWORD)GetTextMetricsA,module_limit_-module_base_,module_base_,0xEC81);
  if (c1)
  {
    DWORD c2 = Util::FindCallOrJmpRel(c1,module_limit_-module_base_,module_base_,0);
    if (c2)
    {
      union {DWORD i; WORD *k;};
      DWORD j;
      for (i = c2 - 0x100, j = c2 - 0x400; i > j; i--)
      {
        if (*k == 0xEC83)
        {
          HookParam hp = {};
          hp.addr = i;
          hp.off = -0xC;
          hp.split = -0x18;
          hp.type = DATA_INDIRECT | USING_SPLIT;
          hp.length_offset = 1;
          NewHook(hp, L"WolfRPG");
          return;
        }
      }
    }
  }

  OutputConsole(L"Unknown WolfRPG engine.");
  return;
}
#endif // 0: remove wolf hook

bool InsertIGSDynamicHook(LPVOID addr, DWORD frame, DWORD stack)
{
  if (addr!=GetGlyphOutlineW) return false;
  HookParam hp={};
  DWORD i,j,k,t;
  i=*(DWORD*)frame;
  i=*(DWORD*)(i+4);
  if (FillRange(L"mscorlib.ni.dll",&j,&k))
  {
    while (*(BYTE*)i!=0xE8) i++;
    t=*(DWORD*)(i+1)+i+5;
    if (t>j&&t<k)
    {
      hp.addr=t;
      hp.off=-0x10;
      hp.split=-0x18;
      hp.length_offset=1;
      hp.type=USING_UNICODE|USING_SPLIT;
      NewHook(hp,L"IronGameSystem");
      //OutputConsole(L"IGS - Please set text(テキスト) display speed(表示速度) to fastest(瞬間)");
      //RegisterEngineType(ENGINE_IGS);
      return true;
    }
  }
  return true;
}
void InsertIronGameSystemHook()
{
  //OutputConsole(L"Probably IronGameSystem. Wait for text.");
  trigger_fun_=InsertIGSDynamicHook;
  SwitchTrigger(true);
}

/********************************************************************************************
AkabeiSoft2Try hook:
  Game folder contains YaneSDK.dll. Maybe we should call the engine Yane(屋根 = roof)?
  This engine is based on .NET framework. This really makes it troublesome to locate a
  valid hook address. The problem is that the engine file merely contains bytecode for
  the CLR. Real meaningful object code is generated dynamically and the address is randomized.
  Therefore the easiest method is to brute force search whole address space. While it's not necessary
  to completely search the whole address space, since non-executable pages can be excluded first.
  The generated code sections do not belong to any module(exe/dll), hence they do not have
  a section name. So we can also exclude executable pages from all modules. At last, the code
  section should be long(>0x2000). The remain address space should be several MBs in size and
  can be examined in reasonable time(less than 0.1s for P8400 Win7x64).
  Characteristic sequence is 0F B7 44 50 0C, stands for movzx eax, word ptr [edx*2 + eax + C].
  Obviously this instruction extracts one unicode character from a string.
  A main shortcoming is that the code is not generated if it hasn't been used yet.
  So if you are in title screen this approach will fail.

********************************************************************************************/
MEMORY_WORKING_SET_LIST *GetWorkingSet()
{
  DWORD len,retl;
  NTSTATUS status;
  LPVOID buffer = 0;
  len = 0x4000;
  status = NtAllocateVirtualMemory(NtCurrentProcess(), &buffer, 0, &len, MEM_RESERVE|MEM_COMMIT, PAGE_READWRITE);
  if (!NT_SUCCESS(status)) return 0;
  status = NtQueryVirtualMemory(NtCurrentProcess(), 0, MemoryWorkingSetList, buffer, len, &retl);
  if (status == STATUS_INFO_LENGTH_MISMATCH) {
    len = *(DWORD*)buffer;
    len = ((len << 2) & 0xFFFFF000) + 0x4000;
    retl = 0;
    NtFreeVirtualMemory(NtCurrentProcess(), &buffer, &retl, MEM_RELEASE);
    buffer = 0;
    status = NtAllocateVirtualMemory(NtCurrentProcess(), &buffer, 0, &len, MEM_RESERVE|MEM_COMMIT, PAGE_READWRITE);
    if (!NT_SUCCESS(status)) return 0;
    status = NtQueryVirtualMemory(NtCurrentProcess(), 0, MemoryWorkingSetList, buffer, len, &retl);
    if (!NT_SUCCESS(status)) return 0;
    return (MEMORY_WORKING_SET_LIST*)buffer;
  } else {
    retl = 0;
    NtFreeVirtualMemory(NtCurrentProcess(), &buffer, &retl, MEM_RELEASE);
    return 0;
  }

}
typedef struct _NSTRING
{
  PVOID vfTable;
  DWORD lenWithNull;
  DWORD lenWithoutNull;
  WCHAR str[1];
} NSTRING;
void SpecialHookAB2Try(DWORD esp_base, HookParam* hp, DWORD* data, DWORD* split, DWORD* len)
{
  DWORD test = *(DWORD*)(esp_base - 0x10);
  if (test != 0) return;
  NSTRING *s = *(NSTRING**)(esp_base - 0x8);
  *len = s->lenWithoutNull << 1;
  *data = (DWORD)s->str;
  *split = 0;
}

// qsort correctly identifies overflow.
int cmp(const void * a, const void * b)
{ return *(int*)a - *(int*)b; }

BOOL FindCharacteristInstruction(MEMORY_WORKING_SET_LIST* list)
{
  DWORD base,size;
  DWORD i,j,k,addr,retl;
  NTSTATUS status;
  qsort(&list->WorkingSetList, list->NumberOfPages, 4, cmp);
  base = list->WorkingSetList[0];
  size = 0x1000;
  for (i = 1; i < list->NumberOfPages; i++)
  {
    if ((list->WorkingSetList[i] & 2) == 0) continue;
    if (list->WorkingSetList[i] >> 31) break;
    if (base + size == list->WorkingSetList[i])
    {
      size += 0x1000;
    }
    else
    {
      if (size > 0x2000)
      {
        addr = base & ~0xFFF;
        status = NtQueryVirtualMemory(NtCurrentProcess(),(PVOID)addr,
          MemorySectionName,text_buffer_prev,0x1000,&retl);
        if (!NT_SUCCESS(status))
        {
          k = addr + size - 4;
          for (j = addr; j < k; j++)
          {
            if (*(DWORD*)j == 0x5044B70F)
            {
              if (*(WORD*)(j + 4) == 0x890C)
                //movzx eax, word ptr [edx*2 + eax + 0xC]; wchar = string[i];
              {
                HookParam hp = {};
                hp.addr = j;
                hp.extern_fun = SpecialHookAB2Try;
                hp.type = USING_STRING | USING_UNICODE| EXTERN_HOOK | NO_CONTEXT;
                NewHook(hp,L"AB2Try");
                //OutputConsole(L"Please adjust text speed to fastest/immediate.");
                //RegisterEngineType(ENGINE_AB2T);
                return TRUE;
              }
            }
          }
        }
      }
      size = 0x1000;
      base = list->WorkingSetList[i];
    }
  }
  return FALSE;
}
void InsertAB2TryHook()
{
  DWORD size = 0;
  MEMORY_WORKING_SET_LIST *list = GetWorkingSet();
  if (list == 0) return;
  if (!FindCharacteristInstruction(list)) // jichi 10/1/2013: What is it doing here?!
    (void)0;
    //OutputConsole(L"Can't find characteristic sequence. "
    //L"Make sure you have start the game and have seen some text on the screen.");
  NtFreeVirtualMemory(NtCurrentProcess(), (PVOID*)&list, &size, MEM_RELEASE);
}
/********************************************************************************************
C4 hook: (Contributed by Stomp)
  Game folder contains C4.EXE or XEX.EXE.

********************************************************************************************/
void InsertC4Hook()
{
  BYTE sig[8]={0x8A, 0x10, 0x40, 0x80, 0xFA, 0x5F, 0x88, 0x15};
  DWORD i = SearchPattern(module_base_,module_limit_-module_base_,sig,8);
  if (i)
  {
    HookParam hp={};
    hp.addr=i+module_base_;
    hp.off=-0x08;
    hp.type|=DATA_INDIRECT|NO_CONTEXT;
    hp.length_offset=1;
    NewHook(hp,L"C4");
    //RegisterEngineType(ENGINE_C4);
  }
  //else OutputConsole(L"Unknown C4 engine");
}
void SpecialHookWillPlus(DWORD esp_base, HookParam* hp, DWORD* data, DWORD* split, DWORD* len)
{

  static DWORD detect_offset;
  if (detect_offset) return;
  DWORD i,l;
  union{
    DWORD retn;
    WORD* pw;
    BYTE* pb;
  };
  retn = *(DWORD*)esp_base;
  i = 0;
  while (*pw != 0xc483) { //add esp, $
    l = disasm(pb);
    if (++i == 5)
      //OutputConsole(L"Fail to detect offset.");
      break;
    retn += l;
  }
  if (*pw == 0xC483) {
    hp->off = *(pb + 2) - 8;
    hp->type ^= EXTERN_HOOK;
    hp->extern_fun = 0;
    char* str = *(char**)(esp_base + hp->off);
    *data = (DWORD)str;
    *len = strlen(str);
    *split = 0;
  }
  detect_offset = 1;
}

void InsertWillPlusHook()
{
  HookParam hp = {};
  //__debugbreak();
  hp.addr = Util::FindCallAndEntryAbs((DWORD)GetGlyphOutlineA,module_limit_-module_base_,module_base_,0xEC81);
  if (hp.addr == 0)
    //OutputConsole(L"Unknown WillPlus engine.");
    return;

  hp.extern_fun = SpecialHookWillPlus;
  hp.type = USING_STRING | EXTERN_HOOK;
  NewHook(hp,L"WillPlus");
  //RegisterEngineType(ENGINE_WILLPLUS);
}

/** jichi 9/14/2013
 *  TanukiSoft (*.tac)
 *
 *  Seems to be broken for new games in 2012 such like となりの
 *
 *  微少女: /HSN4@004983E0
 *  - addr: 4817888 (0x4983e0)
 *  - extern_fun: 0x0
 *  - function: 0
 *  - hook_len: 0
 *  - ind: 0
 *  - length_of
 *  - fset: 0
 *  - module: 0
 *  - off: 4
 *  - recover_len: 0
 *  - split: 0
 *  - split_ind: 0
 *  - type: 1025 (0x401)
 *
 *  隣りのぷ～さん: /HSN-8@200FE7:TONARINO.EXE
 *  - addr: 2101223 (0x200fe7)
 *  - extern_fun: 0x0
 *  - function: 0
 *  - hook_len: 0
 *  - ind: 0
 *  - length_offset: 0
 *  - module: 2343491905 (0x8baed941)
 *  - off: 4294967284 (0xfffffff4, -0xc)
 *  - recover_len: 0
 *  - split: 0
 *  - split_ind: 0
 *  - type: 1089 (0x441)
 */
void InsertTanukiHook()
{
  for (DWORD i = module_base_; i < module_limit_ - 4; i++)
    if (*(DWORD *)i == 0x8140)
      if (DWORD j = Util::FindEntryAligned(i,0x400)) {  // jichi 9/14/2013: might crash without admin priv
        //ITH_GROWL_DWORD2(i, j);
        HookParam hp = {};
        hp.addr = j;
        hp.off = 4;
        hp.type = USING_STRING | NO_CONTEXT;
        NewHook(hp, L"TanukiSoft");
        return;
      }

  //OutputConsole(L"Unknown TanukiSoft engine.");
}
void SpecialHookRyokucha(DWORD esp_base, HookParam* hp, DWORD* data, DWORD* split, DWORD* len)
{
  DWORD *base = (DWORD*)esp_base;
  DWORD i, j;
  for (i = 1; i < 5; i++)
  {
    j = base[i];
    if ((j >> 16) == 0 && (j >> 8))
    {
      hp->off = i << 2;
      *data = j;
      *len = 2;
      hp->type &= ~EXTERN_HOOK;
      hp->extern_fun = 0;
      return;
    }
  }
  *len = 0;
}
bool InsertRyokuchaDynamicHook(LPVOID addr, DWORD frame, DWORD stack)
{
  if (addr != GetGlyphOutlineA) return false;
  bool flag;
  DWORD insert_addr;
  __asm
  {
    mov eax,fs:[0]
    mov eax,[eax]
    mov eax,[eax] //Step up SEH chain for 2 nodes.
    mov ecx,[eax + 0xC]
    mov eax,[eax + 4]
    add ecx,[ecx - 4]
    mov insert_addr,ecx
    cmp eax,[ecx + 3]
    sete al
    mov flag,al
  }
  if (flag) {
    HookParam hp = {};
    hp.addr = insert_addr;
    hp.length_offset = 1;
    hp.extern_fun = SpecialHookRyokucha;
    hp.type = BIG_ENDIAN | EXTERN_HOOK;
    NewHook(hp, L"StudioRyokucha");
  }
  //else OutputConsole(L"Unknown Ryokucha engine.");
  return true;
}
void InsertRyokuchaHook()
{
  //OutputConsole(L"Probably Ryokucha. Wait for text.");
  trigger_fun_ = InsertRyokuchaDynamicHook;
  SwitchTrigger(true);
}
void InsertGXPHook()
{
  DWORD j, k;
  bool flag;
  union
  {
    DWORD i;
    DWORD* id;
    BYTE* ib;
  };
  //__asm int 3
  for (i = module_base_ + 0x1000; i < module_limit_ - 4; i++)
  {
    //find cmp word ptr [esi*2+eax],0
    if (*id != 0x703c8366) continue;
    i += 4;
    if (*ib != 0) continue;
    i++;
    j = i + 0x200;
    j = j < (module_limit_ - 8) ? j : (module_limit_ - 8);

    while (i < j)
    {
      k = disasm(ib);
      if (k == 0) break;
      if (k == 1 && (*ib & 0xF8) == 0x50) //push reg
      {
        flag = true;
        break;
      }
      i += k;
    }
    if (flag)
    {
      while (i < j)
      {
        if (*ib == 0xE8)
        {
          i++;
          k = *id + i + 4;
          if (k > module_base_ && k < module_limit_)
          {
            HookParam hp = {};
            hp.addr = k;
            hp.type = USING_UNICODE | DATA_INDIRECT;
            hp.length_offset = 1;
            hp.off = 4;
            NewHook(hp, L"GXP");
            return;
          }
        }
        i++;
      }
    }
  }
  //OutputConsole(L"Unknown GXP engine.");
}
static BYTE JIS_tableH[0x80] = {
  0x00,0x81,0x81,0x82,0x82,0x83,0x83,0x84,
  0x84,0x85,0x85,0x86,0x86,0x87,0x87,0x88,
  0x88,0x89,0x89,0x8A,0x8A,0x8B,0x8B,0x8C,
  0x8C,0x8D,0x8D,0x8E,0x8E,0x8F,0x8F,0x90,
  0x90,0x91,0x91,0x92,0x92,0x93,0x93,0x94,
  0x94,0x95,0x95,0x96,0x96,0x97,0x97,0x98,
  0x98,0x99,0x99,0x9A,0x9A,0x9B,0x9B,0x9C,
  0x9C,0x9D,0x9D,0x9E,0x9E,0xDF,0xDF,0xE0,
  0xE0,0xE1,0xE1,0xE2,0xE2,0xE3,0xE3,0xE4,
  0xE4,0xE5,0xE5,0xE6,0xE6,0xE7,0xE7,0xE8,
  0xE8,0xE9,0xE9,0xEA,0xEA,0xEB,0xEB,0xEC,
  0xEC,0xED,0xED,0xEE,0xEE,0xEF,0xEF,0x00,
  0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
  0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
  0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
  0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00
};

static BYTE JIS_tableL[0x80] = {
  0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
  0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
  0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
  0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,
  0x00,0x40,0x41,0x42,0x43,0x44,0x45,0x46,
  0x47,0x48,0x49,0x4A,0x4B,0x4C,0x4D,0x4E,
  0x4F,0x50,0x51,0x52,0x53,0x54,0x55,0x56,
  0x57,0x58,0x59,0x5A,0x5B,0x5C,0x5D,0x5E,
  0x5F,0x60,0x61,0x62,0x63,0x64,0x65,0x66,
  0x67,0x68,0x69,0x6A,0x6B,0x6C,0x6D,0x6E,
  0x6F,0x70,0x71,0x72,0x73,0x74,0x75,0x76,
  0x77,0x78,0x79,0x7A,0x7B,0x7C,0x7D,0x7E,
  0x80,0x81,0x82,0x83,0x84,0x85,0x86,0x87,
  0x88,0x89,0x8A,0x8B,0x8C,0x8D,0x8E,0x8F,
  0x90,0x91,0x92,0x93,0x94,0x95,0x96,0x97,
  0x98,0x99,0x9A,0x9B,0x9C,0x9D,0x9E,0x00,
};

void SpecialHookAnex86(DWORD esp_base, HookParam* hp, DWORD* data, DWORD* split, DWORD* len)
{
  __asm
  {
    mov eax, esp_base
    mov ecx, [eax - 0xC]
    cmp byte ptr [ecx + 0xE], 0
    jnz _fin
    movzx ebx, byte ptr [ecx + 0xC] ; Low byte
    movzx edx, byte ptr [ecx + 0xD] ; High byte
    test edx,edx
    jnz _jis_char
    mov eax,data
    mov [eax],ebx
    mov eax, len
    mov [eax], 1
    jmp _fin
_jis_char:
    cmp ebx,0x7E
    ja _fin
    cmp edx,0x7E
    ja _fin
    test dl,1
    lea eax, [ebx + 0x7E]
    movzx ecx, byte ptr [JIS_tableL + ebx]
    cmovnz eax, ecx
    mov ah, byte ptr [JIS_tableH + edx]
    ror ax,8
    mov ecx, data
    mov [ecx], eax
    mov ecx, len
    mov [ecx], 2
_fin:
  }

}
void InsertAnex86Hook()
{
  HookParam hp = {};
  static DWORD inst[2] = {0x618AC033,0x0D418A0C};
  DWORD i;
  for (i = module_base_ + 0x1000; i < module_limit_ - 8; i++)
  {
    if (*(DWORD*)i == inst[0])
    {
      if (*(DWORD*)(i + 4) == inst[1])
      {
        hp.addr = i;
        hp.extern_fun = SpecialHookAnex86;
        hp.type = EXTERN_HOOK;
        hp.length_offset = 1;
        NewHook(hp, L"Anex86");
        return;
      }
    }
  }
  //OutputConsole(L"Unknown Anex86 engine.");
}
//static char* ShinyDaysQueueString[0x10];
//static int ShinyDaysQueueStringLen[0x10];
//static int ShinyDaysQueueIndex, ShinyDaysQueueNext;
static int ShinyDaysQueueStringLen;
void SpecialHookShinyDays(DWORD esp_base, HookParam* hp, DWORD* data, DWORD* split, DWORD* len)
{
  LPWSTR fun_str;
  char *text_str;
  DWORD l = 0;
  __asm
  {
    mov eax,esp_base
    mov ecx,[eax+0x4C]
    mov fun_str,ecx
    mov esi,[eax+0x70]
    mov edi,[eax+0x74]
    add esi,0x3C
    cmp esi,edi
    jae _no_text
    mov edx,[esi+0x10]
    mov ecx,esi
    cmp edx,8
    cmovae ecx,[ecx]
    add edx,edx
    mov text_str,ecx
    mov l,edx
_no_text:
  }
  if (memcmp(fun_str,L"[PlayVoice]",0x18) == 0)
  {
    *data = (DWORD)text_buffer;
    *len = ShinyDaysQueueStringLen;
  }
  else if (memcmp(fun_str,L"[PrintText]",0x18) == 0)
  {
    memcpy(text_buffer, text_str, l);
    ShinyDaysQueueStringLen = l;
  }
}
void InsertShinyDaysHook()
{
  static const BYTE ins[0x10] = {
    0xFF,0x83,0x70,0x03,0x00,0x00,0x33,0xF6,
    0xC6,0x84,0x24,0x90,0x02,0x00,0x00,0x02
  };
  LPVOID addr = (LPVOID)0x42ad94;
  if (memcmp(addr, ins, 0x10) != 0)
    //OutputConsole(L"Only work for 1.00");
    return;

  HookParam hp = {};
  hp.addr = 0x42ad9c;
  hp.extern_fun = SpecialHookShinyDays;
  hp.type = USING_UNICODE | USING_STRING| EXTERN_HOOK | NO_CONTEXT;
  NewHook(hp, L"ShinyDays 1.00");
  return;
}

/**
 *  jichi 9/5/2013:  aInfo.db
 *  Sample games:
 *  - /HA-C@4D69E:InnocentBullet.exe (イノセントバレット)
 *  - /HA-C@40414C:ImoutoBancho.exe (妹番長)
 *
 *  See: http://ja.wikipedia.org/wiki/ネクストン
 *  See (CaoNiMaGeBi): http://tieba.baidu.com/p/2576241908
 *
 *  md5 = 85ac031f2539e1827d9a1d9fbde4023d
 *  hcode = /HA-C@40414C:ImoutoBancho.exe
 *  - addr: 4211020 (0x40414c)
 *  - extern_fun: 0x0
 *  - function: 0
 *  - hook_len: 0
 *  - ind: 0
 *  - length_offset: 1
 *  - module: 1051997988 (0x3eb43724)
 *  - off: 4294967280 (0xfffffff0)
 *  - recover_len: 0
 *  - split: 0
 *  - split_ind: 0
 *  - type: 68 (0x44)
 */

bool InsertNextonHook()
{
  BYTE ins[] = {
  //0xe8 //??,??,??,??,      00804147   e8 24d90100      call imoutoba.00821a70
    0x89,0x44,0x24, 0x18,   // 0080414c   894424 18        mov dword ptr ss:[esp+0x18],eax; hook here
    0x85,0xc0,              // 00804150   85c0             test eax,eax
    0x0f,0x84               // 00804152  ^0f84 c0feffff    je imoutoba.00804018
  };
  //enum { cur_ins_offset = 0 };
  ULONG addr = module_base_; //- sizeof(ins);
  do {
    addr += sizeof(ins); // ++ so that each time return diff address
    ULONG range = min(module_limit_ - addr, MAX_REL_ADDR);
    ULONG offset = SearchPattern(addr, range, ins, sizeof(ins));
    if (!offset)
      //OutputConsole(L"Not NEXTON");
      return false;

    addr += offset;
    //BYTE ins[] = {
    //  0x57,       // 00804144   57               push edi
    //  0x8b,0xc3,  // 00804145   8bc3             mov eax,ebx
    //  0xe8 //??,??,??,??,      00804147   e8 24d90100      call imoutoba.00821a70
    //};
  } while(0xe8c38b57 != *(DWORD *)(addr-8));

  //ITH_GROWL_DWORD3(module_base_, addr, *(DWORD *)(addr-8));

  //} while(!offset);

  HookParam hp = {};
  hp.type = BIG_ENDIAN;
  hp.length_offset = 1;
  hp.off = -0x10;
  hp.addr = addr;

  NewHook(hp, L"NEXTON");

  //OutputConsole(L"NEXTON");
  return true;
}

/**
 *  jichi 9/16/2013: a-unicorn / gesen18
 *  See (CaoNiMaGeBi): http://tieba.baidu.com/p/2586681823
 *  Pattern: 2bce8bf8
 *      2bce      sub ecx,esi ; hook here
 *      8bf8      mov eds,eax
 *      8bd1      mov edx,ecx
 *
 *  /HBN-20*0@xxoo
 *  - length_offset: 1
 *  - off: 4294967260 (0xffffffdc)
 *  - type: 1032 (0x408)
 */
bool InsertGesen18Hook()
{
  BYTE ins[] = {
    0x2b,0xce,  // sub ecx,esi ; hook here
    0x8b,0xf8   // mov eds,eax
  };
  enum { cur_ins_offset = 0 };
  ULONG range = min(module_limit_ - module_base_, MAX_REL_ADDR);
  ULONG reladdr = SearchPattern(module_base_, range, ins, sizeof(ins));
  if (!reladdr)
    //OutputConsole(L"Not Gesen18");
    return false;


  HookParam hp = {};
  hp.type = NO_CONTEXT | DATA_INDIRECT;
  hp.length_offset = 1;
  hp.off = -0x24;
  hp.addr = module_base_ + reladdr + cur_ins_offset;

  //index = SearchPattern(module_base_, size,ins, sizeof(ins));
  //ITH_GROWL_DWORD2(base, index);

  NewHook(hp, L"Gesen18");
  //OutputConsole(L"Gesen18");
  return true;
}

/**
 *  jichi 10/1/2013: Altemis Engine
 *  See: http://www.ies-net.com/
 *  See (CaoNiMaGeBi): http://tieba.baidu.com/p/2625537737
 *  Pattern:
 *     650a2f 83c4 0c   add esp,0xc ; hook here
 *     650a32 0fb6c0    movzx eax,al
 *     650a35 85c0      test eax,eax
 *     0fb6c0 75 0e     jnz short tsugokaz.0065a47
 *
 *  Wrong: 0x400000 + 0x7c574
 *
 *  //Example: [130927]妹スパイラル /HBN-8*0:14@65589F
 *  Example: ツゴウノイイ家族 Trial /HBN-8*0:14@650A2F
 *  Note: 0x650a2f > 40000(base) + 20000(limit)
 *  - addr: 0x650a2f
 *  - extern_fun: 0x0
 *  - function: 0
 *  - hook_len: 0
 *  - ind: 0
 *  - length_offset: 1
 *  - module: 0
 *  - off: 4294967284 = 0xfffffff4 = -0xc
 *  - recover_len: 0
 *  - split: 20 = 0x14
 *  - split_ind: 0
 *  - type: 1048 = 0x418
 *
 *  @CaoNiMaGeBi:
 *  RECENT GAMES:
 *    [130927]妹スパイラル /HBN-8*0:14@65589F
 *    [130927]サムライホルモン
 *    [131025]ツゴウノイイ家族 /HBN-8*0:14@650A2F (for trial version)
 *    CLIENT ORGANIZAIONS:
 *    CROWD
 *    D:drive.
 *    Hands-Aid Corporation
 *    iMel株式会社
 *    SHANNON
 *    SkyFish
 *    SNACK-FACTORY
 *    team flap
 *    Zodiac
 *    くらむちゃうだ～
 *    まかろんソフト
 *    アイディアファクトリー株式会社
 *    カラクリズム
 *    合资会社ファーストリーム
 *    有限会社ウルクスへブン
 *    有限会社ロータス
 *    株式会社CUCURI
 *    株式会社アバン
 *    株式会社インタラクティブブレインズ
 *    株式会社ウィンディール
 *    株式会社エヴァンジェ
 *    株式会社ポニーキャニオン
 *    株式会社大福エンターテインメント
 */
bool InsertArtemisHook()
{
  BYTE ins[] = {
    0x83,0xc4, 0x0c, // add esp,0xc ; hook here
    0x0f,0xb6,0xc0,  // movzx eax,al
    0x85,0xc0,       // test eax,eax
    0x75, 0x0e       // jnz XXOO ; it must be 0xe, or there will be duplication
  };
  enum { cur_ins_offset = 0 };
  ULONG range = min(module_limit_ - module_base_, MAX_REL_ADDR);
  ULONG reladdr = SearchPattern(module_base_, range, ins, sizeof(ins));
  //ITH_GROWL_DWORD3(reladdr, module_base_, range);
  if (!reladdr)
    //OutputConsole(L"Not Artemis");
    return false;

  HookParam hp = {};
  hp.addr = module_base_ + reladdr + cur_ins_offset;
  hp.length_offset = 1;
  hp.off = -0xc;
  hp.split = 0x14;
  hp.type = NO_CONTEXT | DATA_INDIRECT | USING_SPLIT; // 0x418

  //hp.addr = 0x650a2f;
  //ITH_GROWL_DWORD(hp.addr);

  NewHook(hp, L"Artemis");
  //OutputConsole(L"Artemis");
  return true;
}

#if 0

/**
 *  jichi 10/3/2013: BALDRSKY ZERO  (Unity3D)
 *  See (ok123): http://9gal.com/read.php?tid=411756
 *  Pattern: 90FF503C83C4208B45EC
 *
 *
 *  Example: /HQN4@7620DA1 (or /HQN84:84*-C@1005FFCB)
 *  - addr: 123866529 = 0x7620da1
 *  - off: 4
 *  - type: 1027 = 0x403
 *
 *  FIXME: Raise C0000005 even with admin priv
 */
bool InsertBaldrHook()
{
  // Instruction pattern: 90FF503C83C4208B45EC
  BYTE ins[] = {0x90,0xff,0x50,0x3c,0x83,0xc4,0x20,0x8b,0x45,0xec};
  //BYTE ins[] = {0xec,0x45,0x8b,0x20,0xc4,0x83,0x3c,0x50,0xff,0x90};
  enum { cur_ins_offset = 0 };
  enum { limit = 0x10000000 }; // very large ><
  //enum { range = 0x10000000 };
  //enum { range = 0x1000000 };
  //enum { range = 0x7fffffff };
  //ULONG range = min(module_limit_ - module_base_, MAX_REL_ADDR);
  ULONG range = min(module_limit_ - module_base_, limit);
  ULONG reladdr = SearchPattern(module_base_, range, ins, sizeof(ins));
  //ITH_GROWL_DWORD3(base, range, reladdr);
  if (!reladdr)
    //OutputConsole(L"Not Artemis");
    return false;

  HookParam hp = {};
  hp.addr = module_base_ + reladdr + cur_ins_offset;
  hp.off = 4;
  hp.type = NO_CONTEXT | USING_STRING | USING_UNICODE; // 0x403

  //hp.addr = 0x650a2f;
  //ITH_GROWL_DWORD(hp.addr);

  NewHook(hp, L"BALDR");
  //OutputConsole(L"Artemis");
  return true;
}

#endif // 0

} // namespace Engine

// EOF
