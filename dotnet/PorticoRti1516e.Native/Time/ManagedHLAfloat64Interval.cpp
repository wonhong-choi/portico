#include "ManagedHLAfloat64Interval.h"

namespace PorticoRti1516e {

ManagedHLAfloat64Interval::ManagedHLAfloat64Interval(double interval)
   : _interval(interval)
{
}

ManagedHLAfloat64Interval::ManagedHLAfloat64Interval(rti1516e::LogicalTimeInterval const & native)
   : _interval(rti1516e::HLAfloat64Interval(native).getInterval())
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
