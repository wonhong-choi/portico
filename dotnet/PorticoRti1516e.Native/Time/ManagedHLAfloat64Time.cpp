#include "ManagedHLAfloat64Time.h"

namespace PorticoRti1516e {

ManagedHLAfloat64Time::ManagedHLAfloat64Time(double time)
   : _time(time)
{
}

ManagedHLAfloat64Time::ManagedHLAfloat64Time(rti1516e::LogicalTime const & native)
   // Use dynamic_cast to reinterpret the existing LogicalTime as an HLAfloat64Time,
   // exactly as the reference example does (ExampleFedAmb::convertTime). Do NOT construct
   // a new HLAfloat64Time from the LogicalTime via HLAfloat64Time(LogicalTime const&) -
   // that constructor recurses infinitely in Portico's build and blows the stack
   // (observed as a StackOverflowException the moment timeRegulationEnabled fires),
   // which is presumably why the example avoids it too.
   : _time(dynamic_cast<rti1516e::HLAfloat64Time const &>(native).getTime())
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
