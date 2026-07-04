#include "ManagedHLAfloat64Interval.h"

namespace PorticoRti1516e {

ManagedHLAfloat64Interval::ManagedHLAfloat64Interval(double interval)
   : _interval(interval)
{
}

ManagedHLAfloat64Interval::ManagedHLAfloat64Interval(rti1516e::LogicalTimeInterval const & native)
   // dynamic_cast the existing interval rather than constructing a new
   // HLAfloat64Interval from the LogicalTimeInterval - same reasoning as
   // ManagedHLAfloat64Time's LogicalTime constructor (avoid the self-recursive
   // HLAfloat64Interval(LogicalTimeInterval const&) constructor that would blow the
   // stack). Mirrors the reference example's cast-based time conversion.
   : _interval(dynamic_cast<rti1516e::HLAfloat64Interval const &>(native).getInterval())
{
}

double ManagedHLAfloat64Interval::Interval::get()
{
   return _interval;
}

rti1516e::HLAfloat64Interval ManagedHLAfloat64Interval::ToNative()
{
   return rti1516e::HLAfloat64Interval(_interval);
}

}
