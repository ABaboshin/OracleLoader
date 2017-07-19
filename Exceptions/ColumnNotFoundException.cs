using System;

namespace OracleLoader.Exceptions
{
    public class ColumnNotFoundException : Exception
    {
        public string ColumnName { get; set; }
    }
}
