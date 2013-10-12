# ith/common/common.pri
# 8/9/2011 jichi
# Overwrite ITH headers

#DEFINES += ITH_HAS_CRT     # whether ITH is linked with msvcrt
#DEFINES += ITH_HAS_CXX     # whether ITH has access to native C++ syntax

DEPENDPATH  += $$PWD

HEADERS += \
  $$PWD/const.h \
  $$PWD/defs.h \
  $$PWD/except.h \
  $$PWD/growl.h \
  $$PWD/memory.h \
  $$PWD/string.h \
  $$PWD/types.h

DEFINES += _CRT_NON_CONFORMING_SWPRINTFS

# EOF
