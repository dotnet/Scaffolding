using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGeneration.MsBuild
{
    public static class Requires
    {
        public static void NotNull(object o)
        {
            if (o == null)
            {
                throw new ArgumentNullException();
            }
        }

        public static void NotNullOrEmpty(string s)
        {
            if(string.IsNullOrEmpty(s))
            {
                throw new ArgumentNullException();
            }
        }
    }
}
