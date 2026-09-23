using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace MDD4All.DME.AssemblyLoading
{
    public class DataModelLoadContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver _resolver;

        private string _mainDllPath;
        private string? _proxiesDllPath;

        public DataModelLoadContext(string dllPath, string? proxiesDllPath = null)
        {
            FileInfo dllFileInfo = new FileInfo(dllPath);

            _mainDllPath = dllPath;
            _proxiesDllPath = proxiesDllPath;

            if (dllFileInfo != null && dllFileInfo.Directory != null)
            {
                _resolver = new AssemblyDependencyResolver(_mainDllPath);
            }
            else
            {
                throw new Exception("Wrong dll path.");
            }

            if (_proxiesDllPath != null)
            {
                LoadFromAssemblyPath(_proxiesDllPath);
            }
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            Assembly? result = null;

            // Only the framework is shared with the host, and it has to be: a data model brings
            // its own System.ComponentModel.Annotations along, and a second copy would put
            // DisplayAttribute in the process twice - same name, same shape, two types that never
            // compare equal. Returning null says "not mine" and hands the request to the host.
            //
            // Everything else has to come from next to the model, even when the host happens to
            // carry an assembly of the same name. The data models are exactly that case: the
            // application references its own, and letting the host answer for them would leave
            // the type in a file's $type incompatible with the type the caller resolved.
            //
            // Asking the host whether it *could* supply an assembly is not the same question -
            // it can supply plenty that it must not.
            if (IsSharedFramework(assemblyName))
            {
                return null;
            }

            string? assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                result = LoadFromAssemblyPath(assemblyPath);
            }
            else // try to load from proxy directory
            {
                if (_proxiesDllPath != null)
                {
                    FileInfo proxyDllFileInfo = new FileInfo(_proxiesDllPath);

                    string? proxiesDllFolder = proxyDllFileInfo.Directory?.FullName;

                    if (proxiesDllFolder != null)
                    {
                        string proxyAssemblyPath = Path.Combine(proxiesDllFolder, assemblyName.Name + ".dll");

                        if (File.Exists(proxyAssemblyPath))
                        {
                            result = LoadFromAssemblyPath(proxyAssemblyPath);
                        }
                    }
                }
            }

            return result;

        }

        // Names rather than a probe, because the question is not what the host has but what it
        // owns. These prefixes are the shared framework; nothing an application or a data model
        // ships is called that.
        private bool IsSharedFramework(AssemblyName assemblyName)
        {
            bool result = false;

            string? name = assemblyName.Name;

            if (name != null)
            {
                result = name.StartsWith("System.", StringComparison.Ordinal)
                         || name.StartsWith("Microsoft.", StringComparison.Ordinal)
                         || name == "System"
                         || name == "netstandard"
                         || name == "mscorlib";
            }

            return result;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string? libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }

        public List<string> AssemblyNames
        {
            get
            {
                List<string> result = new List<string>();

                foreach (Assembly? assembly in Assemblies)
                {
                    if (assembly != null)
                    {
                        string? assemblyName = assembly.GetName().Name;
                        if (assemblyName != null)
                        {
                            result.Add(assemblyName);
                        }
                    }
                }

                return result;
            }
        }

    }
}
