#pragma once

// Managed wrapper around rti1516e::HLAfloat64Time (RTI/time/HLAfloat64Time.h),
// the only concrete LogicalTime implementation ExampleCPPFederate.cpp uses.
// Unlike the opaque Handle types in Handles/, HLAfloat64Time is a small
// value wrapping a double - no heap-allocated native object/dispose pattern
// is needed here, just a managed double and on-demand native construction.

#include <RTI/time/HLAfloat64Time.h>

using namespace System;

namespace PorticoRti1516e {

public ref class ManagedHLAfloat64Time sealed
{
internal:
   ManagedHLAfloat64Time(rti1516e::LogicalTime const & native);

public:
   ManagedHLAfloat64Time(double time);

   property double Time { double get(); }

internal:
   rti1516e::HLAfloat64Time ToNative();

private:
   double _time;
};

}
