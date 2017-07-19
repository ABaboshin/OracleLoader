using System;

namespace OracleLoader.Internal
{
    static internal class TypeChecker
    {
        public static bool CanConvert(object value, Type targetType)
        {
            object result = null;
            try
            {
                result = Convert.ChangeType(value, targetType);
            }
            catch
            {
            }

            return result != null;
        }
    }
}
