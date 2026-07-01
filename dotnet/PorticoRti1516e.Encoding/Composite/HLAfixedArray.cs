using System;
using System.Collections;
using System.Collections.Generic;

namespace PorticoRti1516e.Encoding
{
    // Fixed-count array of homogeneous IDataElements - no length prefix on the wire, both
    // sides must already agree on the element count (e.g. via a FOM array attribute
    // definition). Port of HLA1516eFixedArray<T> (Java).
    //
    // Verified asymmetry vs HLAfixedRecord (preserved faithfully, not "fixed"): this
    // type's OctetBoundary is the max of each element's GetEncodedLength(), NOT
    // GetOctetBoundary() as HLAfixedRecord uses.
    public sealed class HLAfixedArray<T> : DataElementBase, IEnumerable<T> where T : IDataElement
    {
        private readonly List<T> _elements;
        private int _boundary = -1;

        public HLAfixedArray(params T[] provided)
        {
            _elements = new List<T>(provided);
        }

        public HLAfixedArray(IDataElementFactory<T> factory, int size)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _elements = new List<T>(size);
            for (int i = 0; i < size; i++)
                _elements.Add(factory.CreateElement(i));
        }

        public int Size => _elements.Count;

        public T Get(int index) => _elements[index];

        public IEnumerator<T> GetEnumerator()
        {
            _boundary = -1;
            return _elements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override int GetOctetBoundary()
        {
            if (_boundary == -1)
            {
                int maxSize = 1;
                foreach (var element in _elements)
                    maxSize = Math.Max(maxSize, element.GetEncodedLength());
                _boundary = maxSize;
            }

            return _boundary;
        }

        public override int GetEncodedLength()
        {
            int length = 0;
            foreach (var element in _elements)
            {
                int boundary = element.GetOctetBoundary();
                while (length % boundary != 0)
                    length++;

                length += element.GetEncodedLength();
            }

            return length;
        }

        public override void Encode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());
            foreach (var element in _elements)
                element.Encode(byteWrapper);
        }

        public override void Decode(ByteWrapper byteWrapper)
        {
            byteWrapper.Align(GetOctetBoundary());
            foreach (var element in _elements)
                element.Decode(byteWrapper);
        }
    }
}
