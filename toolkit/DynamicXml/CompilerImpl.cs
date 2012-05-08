//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010-2012 Garrett Serack and CoApp Contributors. 
//     Contributors can be discovered using the 'git log' command.
//     All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Toolkit.DynamicXml {
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;

    internal class CompilerImpl : MarshalByRefObject {
        CodeDomProvider _provider;

        public CompilerImpl(CodeDomProvider provider) {
            _provider = provider;
        }

        public _Assembly Compile(IEnumerable<string> sources, IEnumerable<string> references) {
            CompilerParameters options = new CompilerParameters();
            options.GenerateInMemory = true;
            options.ReferencedAssemblies.AddRange(references.ToArray());
            return Check(_provider.CompileAssemblyFromSource(options, sources.ToArray()));
        }

        private static _Assembly Check(CompilerResults results) {
            if (results.Errors.Count > 0) {
                throw new CompilerException(results);
            }

            return results.CompiledAssembly;
        }

    }
}