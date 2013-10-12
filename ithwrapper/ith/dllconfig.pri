# dllconfig.pri
# 8/9/2013 jichi
# For linking ITH injectable dlls.
# The dll is self-containd and Windows-independent.

CONFIG += noqt noeh dll
#CONFIG += noqt dll
CONFIG -= embed_manifest_dll # Pure dynamic determined. The manifest would break Windows XP support
include(../../../config.pri)

# jichi 9/19/2013: Do not need RTL heap
#DEFINES += ITH_HAS_CXX

## Libraries

#LIBS        += -lkernel32 -luser32 -lgdi32
LIBS        += -L$$WDK_HOME/lib/wxp/i386 -lntdll
LIBS        += $$WDK_HOME/lib/crt/i386/msvcrt.lib   # Override msvcrt10
#LIBS        += -L$$WDK_HOME/lib/crt/i386 -lmsvcrt
#QMAKE_LFLAGS += $$WDK_HOME/lib/crt/i386/msvcrt.lib # This will leave runtime flags in the dll

#DEFINES += ITH_HAS_CXX
#DEFINES += ITH_HAS_CRT
DEFINES += _CRT_NON_CONFORMING_SWPRINTFS

HEADERS += $$PWD/dllconfig.h

# EOF
