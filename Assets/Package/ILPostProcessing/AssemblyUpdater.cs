using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace OneLastClick.UnityObjectReferencing.ILPostProcessing
{
    public static class AssemblyModifier
    {
        public static ILPostProcessResult ModifyAssembly(ICompiledAssembly compiledAssembly, Func<AssemblyDefinition, List<DiagnosticMessage>, IAssemblyResolver, bool> modify )
        {
            List<DiagnosticMessage> diagnostics = new List<DiagnosticMessage>();
            
            bool hasSymbols = compiledAssembly.InMemoryAssembly.PdbData != null;
            using PostProcessorAssemblyResolver resolver = new PostProcessorAssemblyResolver(compiledAssembly);
            ReaderParameters readerParameters = new ReaderParameters
            {
                AssemblyResolver = resolver,
                SymbolStream = hasSymbols == true ? new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData) : null,
                ReadSymbols = hasSymbols,
                ReadingMode = ReadingMode.Immediate,
            };

            using MemoryStream peStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PeData);
            using AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(peStream, readerParameters);
            ModuleDefinition module = assemblyDefinition.MainModule;

            if (modify(assemblyDefinition, diagnostics, resolver) == true)
            {
                return WriteChanges(assemblyDefinition, diagnostics);
            }
            
            return new ILPostProcessResult(null, diagnostics);
        }
        
        private static ILPostProcessResult WriteChanges(AssemblyDefinition assemblyDefinition, List<DiagnosticMessage> diagnostics)
        {
            MemoryStream peOut = new MemoryStream();
            MemoryStream pdbOut = new MemoryStream();
            WriterParameters writerParameters = new WriterParameters
            {
                SymbolWriterProvider = new PortablePdbWriterProvider(),
                SymbolStream = pdbOut,
                WriteSymbols = true,
            };

            assemblyDefinition.Write(peOut, writerParameters);

            InMemoryAssembly resultAssembly = new InMemoryAssembly(peOut.ToArray(), pdbOut.ToArray());
            return new ILPostProcessResult(resultAssembly, diagnostics);
        }
    }
}