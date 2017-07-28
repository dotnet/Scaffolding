using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Web.CodeGenerators.Mvc
{
    public class NameSpaceUtilities
    {
        private static string GetSafeName(string name)
        {
            char[] chars = name.ToCharArray();
            string result = String.Empty;
            for (int i = 0; i < chars.Length; i++)
            {
                if (!Char.IsDigit(chars[i]) && !Char.IsLetter(chars[i]) && chars[i] != '.')
                {
                    chars[i] = '_';
                }
                result = result + chars[i];
            }
            if (Char.IsDigit(result[0]))
            {
                result = "_" + result;
            }
            return result;
        }

        /// <summary>
        /// Converts a namespace name to a safe namespace name.
        /// </summary>
        public static string GetSafeNameSpaceName(string namespaceName)
        {
            if(namespaceName == null)
            {
                throw new ArgumentNullException(nameof(namespaceName));
            }

            if (namespaceName.Trim().Length == 0)
            {
                return "_";
            }

            string[] names = namespaceName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            string name = String.Empty;
            for (int i = 0; i < names.Length; i++)
            {
                if (i != 0)
                {
                    name = name + ".";
                }
                name = name + GetSafeName(names[i]);
            }
            return string.IsNullOrEmpty(name) ? "_" : name;
        }

        /// <summary>
        /// Converts a path like a/b/c/d to namespace like a.b.c.d
        /// </summary>
        public static string GetSafeNameSpaceFromPath(string path, string namespacePrefix = null)
        {
            if(path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var namespaceName = path.Replace(Path.DirectorySeparatorChar, '.');
            if (!string.IsNullOrEmpty(namespacePrefix))
            {
                namespaceName = namespacePrefix +"." + namespaceName;
            }

            return GetSafeNameSpaceName(namespaceName);
        }
    }
}
