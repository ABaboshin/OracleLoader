using System;

namespace OracleLoader.Exceptions
{
    public class TypeIncompatibleException : Exception
    {
        public Type ExpectedType { get; set; }
        public Type GivenType { get; set; }
    }
}
