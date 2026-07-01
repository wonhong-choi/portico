using System;

namespace PorticoRti1516e.Encoding
{
    // Adapter so callers can pass a plain Func<int, T> instead of implementing
    // IDataElementFactory<T> themselves.
    public sealed class DelegateDataElementFactory<T> : IDataElementFactory<T> where T : IDataElement
    {
        private readonly Func<int, T> _factory;

        public DelegateDataElementFactory(Func<int, T> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public T CreateElement(int index) => _factory(index);
    }
}
