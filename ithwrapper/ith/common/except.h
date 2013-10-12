#pragma once

// ith/common/except.h
// 9/17/2013 jichi

#ifdef ITH_HAS_SEH
# define ITH_TRY    __try
#else
# define ITH_TRY    if (true)
#endif // ITH_HAS_SEH

#ifdef ITH_HAS_SEH
# define ITH_EXCEPT __except(EXCEPTION_EXECUTE_HANDLER)
#else
# define ITH_EXCEPT else
#endif // ITH_HAS_SEH

// EOF
