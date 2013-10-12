# clixp.pro
# 8/9/2013 jichi
# Build vnrclixp.dll

include(../dllconfig.pri)
include(../sys/sys.pri)
include($$LIBDIR/disasm/disasm.pri)

VPATH += ../cli
INCLUDEPATH += ../cli

# jichi 9/14/2013: Windows XP's msvnrt does not have except handler
DEFINES -= ITH_HAS_SEH

# jichi 9/22/2013: When ITH is on wine, mutex is needed to protect NtWriteFile
#DEFINES += ITH_WINE
#DEFINES += ITH_SYNC_PIPE

## Libraries

LIBS    += -lkernel32 -luser32 -lgdi32

## Sources

TEMPLATE = lib
#TARGET   = IHF_DLL # compatible with ITHv3
TARGET   = vnrclixp

#CONFIG += staticlib

HEADERS += \
  avl_p.h \
  config.h \
  cli.h \
  cli_p.h
  #util.h

SOURCES += \
  main.cc \
  pipe.cc \
  texthook.cc
  #util.cc

#RC_FILE += engine.rc
#OTHER_FILES += engine.rc

OTHER_FILES += clixp.pri

# EOF
