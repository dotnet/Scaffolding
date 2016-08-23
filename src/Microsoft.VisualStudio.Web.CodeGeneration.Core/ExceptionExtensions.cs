using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGeneration
{
    public static class ExceptionExtensions
    {
        public static Exception Unwrap(this Exception ex, ILogger logger = null)
        {
            if (ex == null)
            {
                return null;
            }

            var inner = ex;
            while (inner.InnerException != null)
            {
                if(logger != null)
                {
                    logger.LogMessage($"{ex.Message} StackTrace:{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}");
                }
                inner = inner.InnerException;
            }

            return inner;
        }
    }
}
