#pragma once

// RTI/RTIambassadorFactory.h's createRTIambassador() (and RTI/RTIambassador.h's
// getTimeFactory()) return std::auto_ptr<T> per the old (pre-C++11) IEEE 1516.1
// C++ API spec. Those headers only forward-declare std::auto_ptr themselves
// (namespace std { template <class T> class auto_ptr; }) and expect the includer
// to supply the real definition, normally via <memory> guarded by the MSVC STL
// compatibility macro _HAS_AUTO_PTR_ETC=1.
//
// Recent MSVC STL releases have fully removed std::auto_ptr - _HAS_AUTO_PTR_ETC=1
// no longer restores it, even under an explicit older /std:c++14 (confirmed by an
// actual Visual Studio build attempt: pinning LanguageStandard to stdcpp14 alone
// did not fix "'auto_ptr' is not a member of 'std'"). Since we cannot modify the
// native RTI headers, and <memory> no longer provides a definition to conflict
// with, this supplies a minimal, faithful drop-in replacement - a single raw
// pointer member (matching the classic implementation's layout, so it stays
// ABI-compatible with what the precompiled rti1516e64(d).dll actually
// constructs/returns) plus the auto_ptr_ref<T> conversion idiom that the classic
// implementation relied on to make
// "std::auto_ptr<X> x = FunctionReturningAutoPtrByValue();" - a non-const
// lvalue-reference copy constructor cannot bind to a temporary/prvalue without
// it - actually compile.
//
// Include this AFTER <RTI/RTIambassadorFactory.h> (or any other RTI header that
// forward-declares std::auto_ptr) and BEFORE calling any auto_ptr-returning RTI
// API.

namespace std
{
   template <class T> class auto_ptr;

   template <class T>
   struct auto_ptr_ref
   {
      T* _ptr;
      explicit auto_ptr_ref(T* p) throw() : _ptr(p) {}
   };

   template <class T>
   class auto_ptr
   {
   public:
      typedef T element_type;

      explicit auto_ptr(T* p = 0) throw() : _ptr(p) {}
      auto_ptr(auto_ptr& other) throw() : _ptr(other.release()) {}
      auto_ptr(auto_ptr_ref<T> ref) throw() : _ptr(ref._ptr) {}

      template <class Y>
      auto_ptr(auto_ptr<Y>& other) throw() : _ptr(other.release()) {}

      auto_ptr& operator=(auto_ptr& other) throw()
      {
         reset(other.release());
         return *this;
      }

      auto_ptr& operator=(auto_ptr_ref<T> ref) throw()
      {
         reset(ref._ptr);
         return *this;
      }

      template <class Y>
      auto_ptr& operator=(auto_ptr<Y>& other) throw()
      {
         reset(other.release());
         return *this;
      }

      ~auto_ptr()
      {
         delete _ptr;
      }

      T& operator*() const throw() { return *_ptr; }
      T* operator->() const throw() { return _ptr; }
      T* get() const throw() { return _ptr; }

      T* release() throw()
      {
         T* tmp = _ptr;
         _ptr = 0;
         return tmp;
      }

      void reset(T* p = 0) throw()
      {
         if (p != _ptr)
            delete _ptr;
         _ptr = p;
      }

      template <class Y> operator auto_ptr_ref<Y>() throw() { return auto_ptr_ref<Y>(release()); }
      template <class Y> operator auto_ptr<Y>() throw() { return auto_ptr<Y>(release()); }

   private:
      T* _ptr;
   };
}
