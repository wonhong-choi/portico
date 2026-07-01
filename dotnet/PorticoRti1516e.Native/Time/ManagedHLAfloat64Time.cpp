#include "ManagedHLAfloat64Time.h"

namespace PorticoRti1516e {

ManagedHLAfloat64Time::ManagedHLAfloat64Time(double time)
   : _time(time)
{
}

ManagedHLAfloat64Time::ManagedHLAfloat64Time(rti1516e::LogicalTime const & native)
   : _time(rti1516e::HLAfloat64Time(native).getTime())
{
}

double ManagedHLAfloat64Time::Time::get()
{
   return _time;
}

rti1516e::HLAfloat64Time ManagedHLAfloat64Time::ToNative()
{
   // HLAfloat64Time's double-taking constructor takes `double const &` (a native
   // reference), but _time is a field of this ref class and therefore lives on the GC
   // heap - C++/CLI cannot bind a native reference directly over it. Copy to a native
   // stack-local first, which can be safely referenced.
   double time = _time;
   return rti1516e::HLAfloat64Time(time);
}

}
