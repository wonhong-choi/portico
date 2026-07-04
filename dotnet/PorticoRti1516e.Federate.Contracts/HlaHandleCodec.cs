using System.Collections.Generic;

namespace PorticoRti1516e.Federate.Contracts
{
    /// <summary>
    /// Bridges the serializer's FOM-name-keyed maps to the RTI's handle-keyed maps.
    /// Generic over the handle type (THandle) so it carries no dependency on the native
    /// wrapper assembly; the federate supplies its resolved handle maps.
    /// </summary>
    public static class HlaHandleCodec
    {
        /// <summary>
        /// Send side: convert { fomName -&gt; bytes } (from HlaSerializer.Serialize) into
        /// { handle -&gt; bytes } for UpdateAttributeValues / SendInteraction. Names with no
        /// resolved handle are skipped.
        /// </summary>
        public static Dictionary<THandle, byte[]> ToHandleMap<THandle>(
            IDictionary<string, byte[]> nameBytes,
            IReadOnlyDictionary<string, THandle> nameToHandle)
        {
            var result = new Dictionary<THandle, byte[]>();
            foreach (KeyValuePair<string, byte[]> entry in nameBytes)
            {
                if (nameToHandle.TryGetValue(entry.Key, out THandle handle))
                    result[handle] = entry.Value;
            }
            return result;
        }

        /// <summary>
        /// Receive side: convert { handle -&gt; bytes } (from a Reflect/Receive callback) into
        /// { fomName -&gt; bytes } for HlaSerializer.Deserialize. Handles with no known name fall
        /// back to their string form so nothing is silently dropped.
        /// </summary>
        public static Dictionary<string, byte[]> ToNameMap<THandle>(
            IDictionary<THandle, byte[]> handleBytes,
            IDictionary<THandle, string> handleToName)
        {
            var result = new Dictionary<string, byte[]>();
            foreach (KeyValuePair<THandle, byte[]> entry in handleBytes)
            {
                string name = handleToName.TryGetValue(entry.Key, out string resolved)
                    ? resolved
                    : entry.Key.ToString();
                result[name] = entry.Value;
            }
            return result;
        }
    }
}
