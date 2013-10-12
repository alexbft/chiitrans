#pragma once

// engine/engine_p.h
// 8/23/2013 jichi
// See: http://ja.wikipedia.org/wiki/プロジェクト:美少女ゲーム系/ゲームエンジン

#include "config.h"

namespace Engine {

// Global variables
extern wchar_t process_name_[MAX_PATH]; // cached
extern DWORD module_base_,
             module_limit_;

//extern LPVOID trigger_addr;
typedef bool (* trigger_fun_t)(LPVOID addr, DWORD frame, DWORD stack);
extern trigger_fun_t trigger_fun_;

// Engine-specific hooks

bool InsertAliceHook();         // System40@AliceSoft; do not work for latest alice games
bool InsertArtemisHook();       // Artemis Engine: *.pfs
bool InsertCaramelBoxHook();    // Caramel: *.bin
bool InsertCandyHook();         // SystemC@CandySoft: *.fpk
bool InsertGesen18Hook();       // Gsen18: *.szs
bool InsertMalieHook();         // Malie@light: malie.ini
bool InsertNextonHook();        // NEXTON: aInfo.db
bool InsertQLIEHook();          // QLiE: GameData/*.pack
bool InsertRUGPHook();          // rUGP: rUGP.exe
bool InsertShinaHook();         // ShinaRio: Rio.ini
bool InsertSiglusHook();        // SiglusEngine: SiglusEngine.exe

//bool InsertBaldrHook();         // BaldrSkyZero (Unity3D): bsz.exe

void InsertAB2TryHook();        // Yane@AkabeiSoft2Try: YaneSDK.dll.
void InsertAbelHook();          // Abel
void InsertAnex86Hook();        // Anex86: anex86.exe
void InsertApricotHook();       // Apricot: arc.a*
void InsertAtelierHook();       // Atelier Kaguya: message.dat
void InsertBGIHook();           // BGI: BGI.*
void InsertBrunsHook();         // Bruns: bruns.exe
void InsertC4Hook();            // C4: C4.EXE or XEX.EXE
void InsertCatSystem2Hook();    // CatSystem2: *.int
void InsertCMVSHook();          // CMVS: data/pack/*.cpz; do not support the latest cmvs32.exe and cmvs64.exe
void InsertCotophaHook();       // Cotopha: *.noa
void InsertDebonosuHook();      // Debonosu: bmp.bak and dsetup.dll
void InsertEMEHook();           // EmonEngine: emecfg.ecf
void InsertGXPHook();           // GXP: *.gxp
void InsertLiveHook();          // Live: live.dll
void InsertLuneHook();          // Lune: *.mbl
void InsertMajiroHook();        // MAJIRO: *.arc
void InsertMEDHook();           // RunrunEngine: *.med
void InsertKiriKiriHook();      // KiriKiri: *.xp3, resource string
void InsertIronGameSystemHook();// IroneGameSystem: igs_sample.exe
void InsertNitroPlusHook();     // NitroPlus: *.npa
void InsertLucifenHook();       // Lucifen@Navel: *.lpk
void InsertPensilHook();        // Pensil: PSetup.exe
void InsertRyokuchaHook();      // Ryokucha: _checksum.exe
void InsertRealliveHook();      // RealLive: RealLive*.exe
void InsertRetouchHook();       // Retouch: resident.dll
void InsertRREHook();           // RRE: rrecfg.rcf
void InsertSoftHouseHook();     // SoftHouse: *.vfs
void InsertStuffScriptHook();   // Stuff: *.mpk
void InsertTanukiHook();        // Tanuki: *.tak
void InsertTinkerBellHook();    // TinkerBell: arc00.dat
void InsertTriangleHook();      // Triangle: Execle.exe
void InsertWaffleHook();        // WAFFLE: cg.pak
void InsertWhirlpoolHook();     // YU-RIS: *.ypf
void InsertWillPlusHook();      // WillPlus: Rio.arc
//void InsertWolfHook();          // Wolf: data.wolf; jichi 10/1/2013: Why remove wolf hook?!

void InsertShinyDaysHook();     // ShinyDays

// CIRCUS: avdata/
bool InsertCircusHook1();
bool InsertCircusHook2();

} // namespace Engine

// EOF
