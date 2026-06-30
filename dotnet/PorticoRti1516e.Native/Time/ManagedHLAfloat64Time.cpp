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
   return rti1516e::HLAfloat64Time(_time);
}

}
