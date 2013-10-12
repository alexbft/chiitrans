# engxp.pro
# 8/9/2013 jichi
# Build vnrengxp.dll

include(../dllconfig.pri)
include(../clixp/clixp.pri)
include(../sys/sys.pri)
include($$LIBDIR/disasm/disasm.pri)

VPATH += ../eng
INCLUDEPATH += ../eng

# jichi 9/14/2013: Windows XP's msvnrt does not have except handler
DEFINES -= ITH_HAS_SEH

# 9/27/2013: disable ITH this game engine, only for debugging purpose
#DEFINES += ITH_DISABLE_ENGINE

## Libraries

LIBS    += -lkernel32 -luser32 -lgdi32

## Sources

TEMPLATE = lib
#TARGET   = ITH_Engine # compatible with ITHv3
TARGET   = vnrengxp

#CONFIG += staticlib

HEADERS += \
  config.h \
  engine.h \
  engine_p.h \
  util.h

SOURCES += \
  engine.cc \
  engine_p.cc \
  main.cc \
  util.cc

#RC_FILE += engine.rc
#OTHER_FILES += engine.rc

# EOF
