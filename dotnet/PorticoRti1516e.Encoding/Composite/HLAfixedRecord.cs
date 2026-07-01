using System;
using System.Collections;
using System.Collections.Generic;

namespace PorticoRti1516e.Encoding
{
    // Ordered, fixed-length-once-populated composite of IDataElements. Port of
    // HLA1516eFixedRecord (Java). A plain Composite pattern: Add() takes any
    // IDataElement, including another HLAfixedRecord/HLAfixedArray<T>/
    // HLAvariableArray<T> - nesting needs zero special-casing since everything shares the
    // same interface and the same ByteWrapper cursor through recursive Encode/Decode.
    public sealed class HLAfixedRecord : DataElementBase, IEnumerable<IDataElement>
    {
        private readonly List<IDataElement> _elements = new List<IDataElement>();
        private int _boundary = -1;

        public HLAfixedRecord()
        {
        }

        public HLAfixedRecord(params IDataElement[] elements)
        {
            _elements.AddRange(elements);
        }

        public void Add(IDataElement dataElement)
        {
            _elements.Add(dataElement ?? throw new ArgumentNullException(nameof(dataElement)));
            _boundary = -1;
        }

        public int Size => _elements.Count;

        public IDataElement Get(int index) => _elements[index];

        public IEnumerator<IDataElement> GetEnumerator()
        {
            _boundary = -1; // matches Java: iterator() invalidates the cached boundary
            return _elements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override int GetOctetBoundary()
        {
            if (_boundary == -1)
            {
                int temp = 1; // minimum boundary is 1
                foreach (var element in _elements)
                    temp = Math.Max(temp, element.GetOctetBoundary());
                _boundary = temp;
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
