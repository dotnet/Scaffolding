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
            if (logger != null)
            {
                logger.LogMessage($"{inner.Message} StackTrace:{Environment.NewLine}{inner.StackTrace}{Environment.NewLine}");
            }

            while (inner.InnerException != null)
            {
                inner = inner.InnerException;
                if (logger != null)
                {
                    logger.LogMessage($"{inner.Message} StackTrace:{Environment.NewLine}{inner.StackTrace}{Environment.NewLine}");
                }
            }

            return inner;
        }
    }
}
