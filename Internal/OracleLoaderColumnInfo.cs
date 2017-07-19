using Oracle.ManagedDataAccess.Client;
using System;

namespace OracleLoader.Internal
{
    internal class OracleLoaderColumnInfo
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public bool AllowDBNull { get; set; }
        public OracleDbType ProviderType { get; set; }
        public int Size { get; set; }
    }
}
