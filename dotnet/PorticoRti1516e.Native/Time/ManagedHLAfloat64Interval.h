#pragma once

// Managed wrapper around rti1516e::HLAfloat64Interval (RTI/time/
// HLAfloat64Interval.h), the only concrete LogicalTimeInterval
// implementation ExampleCPPFederate.cpp uses (federate lookahead). Same
// thin double-backed shape as ManagedHLAfloat64Time - see that file for
// why no native pointer/dispose pattern is needed here.

#include <RTI/time/HLAfloat64Interval.h>

using namespace System;

namespace PorticoRti1516e {

public ref class ManagedHLAfloat64Interval sealed
{
internal:
   ManagedHLAfloat64Interval(rti1516e::LogicalTimeInterval const & native);

public:
   ManagedHLAfloat64Interval(double interval);

   property double Interval { double get(); }

internal:
   rti1516e::HLAfloat64Interval ToNative();

private:
   double _interval;
};

}
