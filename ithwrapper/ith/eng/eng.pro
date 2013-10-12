# eng.pro
# 8/9/2013 jichi
# Build vnreng.dll

include(../dllconfig.pri)
include(../cli/cli.pri)
include(../sys/sys.pri)
include($$LIBDIR/disasm/disasm.pri)

# jichi 9/14/2013: Windows 7's msvnrt does have except handler
# Should I manually enable SEH using ITH source code?
DEFINES += ITH_HAS_SEH

#DEFINES += ITH_DISABLE_ENGINE  # disable ITH this game engine, only for debugging purpose

## Libraries

LIBS    += -lkernel32 -luser32 -lgdi32

## Sources

TEMPLATE = lib
#TARGET   = ITH_Engine # compatible with ITHv3
TARGET   = vnreng

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
