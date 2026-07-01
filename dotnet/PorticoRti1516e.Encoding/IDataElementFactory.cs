namespace PorticoRti1516e.Encoding
{
    // Creates IDataElement instances on demand. Needed by HLAvariableArray<T>.Decode to
    // grow its element list to a wire-encoded size it didn't know ahead of time. Direct
    // port of hla.rti1516e.encoding.DataElementFactory<T>.
    public interface IDataElementFactory<out T> where T : IDataElement
    {
        T CreateElement(int index);
    }
}
