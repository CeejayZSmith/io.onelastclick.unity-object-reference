using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace OneLastClick.UnityObjectReferencing.ILPostProcessing
{
    internal sealed class PostProcessorAssemblyResolver : IAssemblyResolver
    {
        private readonly ICompiledAssembly _compiledAssembly;
        private readonly Dictionary<string, AssemblyDefinition> _cache = new();

        public PostProcessorAssemblyResolver(ICompiledAssembly compiledAssembly)
        {
            _compiledAssembly = compiledAssembly;
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            return Resolve(
                name,
                new ReaderParameters(ReadingMode.Deferred));
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            if (_cache.TryGetValue(name.Name, out AssemblyDefinition cached))
            {
                return cached;
            }

            string path = FindAssemblyPath(name.Name);

            if (path == null)
            {
                return null;
            }

            parameters.AssemblyResolver ??= this;

            AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(path, parameters);

            _cache[name.Name] = assemblyDefinition;

            return assemblyDefinition;
        }

        private string FindAssemblyPath(string assemblyName)
        {
            // First: normal compiler references.
            string path = _compiledAssembly.References
                .Select(r => (string)r)
                .FirstOrDefault(r =>
                    string.Equals(
                        Path.GetFileNameWithoutExtension(r),
                        assemblyName,
                        StringComparison.OrdinalIgnoreCase));

            if (path != null)
            {
                return path;
            }

            // Second: search the directories containing the compiler
            // references. This is important for assemblies which exist
            // in Unity's compiled output but are intentionally not a
            // compile-time reference of the assembly being processed.
            foreach (string directory in _compiledAssembly.References
                         .Select(r => Path.GetDirectoryName((string)r))
                         .Where(d => !string.IsNullOrEmpty(d))
                         .Distinct())
            {
                path = Path.Combine(directory, assemblyName + ".dll");

                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        public void Dispose()
        {
            foreach (AssemblyDefinition assembly in _cache.Values)
            {
                assembly.Dispose();
            }

            _cache.Clear();
        }
    }
}