// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace Microsoft.Extensions.CodeGeneration.DotNet
{
    public class DefaultAssemblyLoadContext : AssemblyLoadContext, ICodeGenAssemblyLoadContext
    {
        private readonly IDictionary<AssemblyName, string> _assemblyPaths;
        private readonly IDictionary<string, string> _nativeLibraries;
        private readonly IEnumerable<string> _searchPaths;

        private static readonly string[] NativeLibraryExtensions;
        private static readonly string[] ManagedAssemblyExtensions = new[]
        {
            ".dll",
            ".ni.dll",
            ".exe",
            ".ni.exe"
        };

        static DefaultAssemblyLoadContext()
        {
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    NativeLibraryExtensions = new[] { ".dll" };
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    NativeLibraryExtensions = new[] { ".dylib" };
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    NativeLibraryExtensions = new[] { ".so" };
                }
                else
                {
                    NativeLibraryExtensions = new string[0];
                }
            }
        }

        public DefaultAssemblyLoadContext(IDictionary<AssemblyName, string> assemblyPaths,
                                   IDictionary<string, string> nativeLibraries,
                                   IEnumerable<string> searchPaths)
        {
            _assemblyPaths = assemblyPaths ?? new Dictionary<AssemblyName, string>();
            _nativeLibraries = nativeLibraries ?? new Dictionary<string, string>();
            _searchPaths = searchPaths ?? new List<string>();
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            string path;
            if (_assemblyPaths.TryGetValue(assemblyName, out path) || SearchForLibrary(ManagedAssemblyExtensions, assemblyName.Name, out path))
            {
                return LoadFromAssemblyPath(path);
            }
            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string path;
            if (_nativeLibraries.TryGetValue(unmanagedDllName, out path) || SearchForLibrary(NativeLibraryExtensions, unmanagedDllName, out path))
            {
                return LoadUnmanagedDllFromPath(path);
            }

            return base.LoadUnmanagedDll(unmanagedDllName);
        }

        private bool SearchForLibrary(string[] extensions, string name, out string path)
        {
            foreach (var searchPath in _searchPaths)
            {
                foreach (var extension in extensions)
                {
                    var candidate = Path.Combine(searchPath, name + extension);
                    if (File.Exists(candidate))
                    {
                        path = candidate;
                        return true;
                    }
                }
            }
            path = null;
            return false;
        }

        public Assembly LoadFromPath(AssemblyName assemblyName, string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if(assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }
            foreach(var extension in ManagedAssemblyExtensions)
            {
                var resolvedPath = Path.Combine(path, assemblyName.Name + extension);
                if(File.Exists(resolvedPath))
                {
                    return LoadFromAssemblyPath(resolvedPath);
                }
            }
            throw new FileNotFoundException(string.Format("Could not find assembly {0} in path {1}",assemblyName.Name, path));
        }

        public Assembly LoadFromPath(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (File.Exists(path))
            {
                return LoadFromAssemblyPath(path);
            }
            throw new FileNotFoundException(string.Format("Could not find assembly {0}", path));
        }

        public Assembly LoadStream(Stream assembly, Stream symbols)
        {
            return base.LoadFromStream(assembly, symbols);
        }

        public Assembly LoadFromName(AssemblyName AssemblyName)
        {
            if(AssemblyName == null)
            {
                throw new ArgumentNullException(nameof(AssemblyName));
            }
            return Load(AssemblyName);
        }

        public static ICodeGenAssemblyLoadContext CreateAssemblyLoadContext(string nugetPackageDir)
        {
            List<string> searchPaths = new List<string>();
            if (Directory.Exists(nugetPackageDir))
            {
                Queue<string> queue = new Queue<string>();
                queue.Enqueue(nugetPackageDir);
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    searchPaths.Add(current);
                    try
                    {
                        var subdirs = Directory.EnumerateDirectories(current);
                        if (subdirs != null)
                        {
                            foreach (var sd in subdirs)
                            {
                                queue.Enqueue(sd);
                            }
                        }
                    }
                    catch
                    {
                        // Do not want to fail if we cannot access certain directories.
                        continue;
                    }
                }
            }

            return new DefaultAssemblyLoadContext(null, null, searchPaths);
        }
    }
}
