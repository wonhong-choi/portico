#include "Marshal.h"
#include <msclr/marshal_cppstd.h>

namespace PorticoRti1516e {
namespace Marshal {

std::wstring ToNative(String^ managed)
{
   if (managed == nullptr)
      return std::wstring();

   return msclr::interop::marshal_as<std::wstring>(managed);
}

String^ ToManaged(std::wstring const & native)
{
   return msclr::interop::marshal_as<String^>(native);
}

rti1516e::VariableLengthData ToNative(array<Byte>^ managed)
{
   if (managed == nullptr || managed->Length == 0)
      return rti1516e::VariableLengthData();

   pin_ptr<Byte> pinned = &managed[0];
   return rti1516e::VariableLengthData((void*)pinned, (size_t)managed->Length);
}

array<Byte>^ ToManaged(rti1516e::VariableLengthData const & native)
{
   size_t size = native.size();
   array<Byte>^ bytes = gcnew array<Byte>((int)size);
   if (size > 0)
   {
      pin_ptr<Byte> pinned = &bytes[0];
      memcpy(pinned, native.data(), size);
   }
   return bytes;
}

rti1516e::AttributeHandleSet ToNativeAttributeHandleSet(IEnumerable<ManagedAttributeHandle^>^ managed)
{
   rti1516e::AttributeHandleSet native;
   if (managed != nullptr)
   {
      for each (ManagedAttributeHandle ^ h in managed)
         native.insert(h->ToNative());
   }
   return native;
}

List<ManagedAttributeHandle^>^ ToManagedAttributeHandleSet(rti1516e::AttributeHandleSet const & native)
{
   List<ManagedAttributeHandle^>^ managed = gcnew List<ManagedAttributeHandle^>();
   for (rti1516e::AttributeHandleSet::const_iterator it = native.begin(); it != native.end(); ++it)
   {
      managed->Add(gcnew ManagedAttributeHandle(*it));
   }
   return managed;
}

rti1516e::AttributeHandleValueMap ToNative(IDictionary<ManagedAttributeHandle^, array<Byte>^>^ managed)
{
   rti1516e::AttributeHandleValueMap native;
   if (managed != nullptr)
   {
      for each (KeyValuePair<ManagedAttributeHandle^, array<Byte>^> entry in managed)
         native[entry.Key->ToNative()] = ToNative(entry.Value);
   }
   return native;
}

IDictionary<ManagedAttributeHandle^, array<Byte>^>^ ToManaged(rti1516e::AttributeHandleValueMap const & native)
{
   Dictionary<ManagedAttributeHandle^, array<Byte>^>^ managed = gcnew Dictionary<ManagedAttributeHandle^, array<Byte>^>();
   for (rti1516e::AttributeHandleValueMap::const_iterator it = native.begin(); it != native.end(); ++it)
   {
      managed->Add(gcnew ManagedAttributeHandle(it->first), ToManaged(it->second));
   }
   return managed;
}

rti1516e::ParameterHandleValueMap ToNative(IDictionary<ManagedParameterHandle^, array<Byte>^>^ managed)
{
   rti1516e::ParameterHandleValueMap native;
   if (managed != nullptr)
   {
      for each (KeyValuePair<ManagedParameterHandle^, array<Byte>^> entry in managed)
         native[entry.Key->ToNative()] = ToNative(entry.Value);
   }
   return native;
}

IDictionary<ManagedParameterHandle^, array<Byte>^>^ ToManaged(rti1516e::ParameterHandleValueMap const & native)
{
   Dictionary<ManagedParameterHandle^, array<Byte>^>^ managed = gcnew Dictionary<ManagedParameterHandle^, array<Byte>^>();
   for (rti1516e::ParameterHandleValueMap::const_iterator it = native.begin(); it != native.end(); ++it)
   {
      managed->Add(gcnew ManagedParameterHandle(it->first), ToManaged(it->second));
   }
   return managed;
}

}
}
