using System.Collections.Generic;

namespace OracleLoader.Internal
{
    internal class OracleLoaderRecord : Dictionary<string, object>
    {
        public object GetSafeValue(string key)
        {
            if (ContainsKey(key))
            {
                return this[key];
            }

            return null;
        }
    }
}
