using System;
using System.Collections.Generic;

namespace OracleLoader.Exceptions
{
    public class NullValueNotAllowedException : Exception
    {
        public List<string> ColumnNames { get; set; }
    }
}
