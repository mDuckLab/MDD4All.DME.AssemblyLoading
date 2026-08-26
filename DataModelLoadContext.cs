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

            // A type has to mean the same thing on both sides of this boundary. A data model
            // brings its own copy of System.ComponentModel.Annotations along, and the resolver
            // finds it - loading it here would put DisplayAttribute in the process twice, same
            // name, same shape, different types. Every "is DisplayAttribute" in the editor then
            // fails silently, which is why annotated labels never showed up.
            //
            // Returning null says "not mine" and lets the runtime take the host's copy.
            try
            {
                Default.LoadFromAssemblyName(assemblyName);

                return null;
            }
            catch (FileNotFoundException)
            {
                // The host does not have it, so it has to come from next to the model.
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
