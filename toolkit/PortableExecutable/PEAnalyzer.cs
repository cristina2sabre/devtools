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

namespace CoApp.Developer.Toolkit.PortableExecutable {
    using System;
    using CoApp.Toolkit.Exceptions;
    using CoApp.Toolkit.Extensions;
    using Microsoft.Cci;

    public class PEAnalyzer {
        /// <summary>
        ///   Not Even Started.
        /// </summary>
        /// <param name="filename"> </param>
        public static void Load(string filename) {
            MetadataReaderHost _host = new PeReader.DefaultHost();

            var module = _host.LoadUnitFrom(filename) as IModule;

            if (module == null || module is Dummy) {
                throw new CoAppException("{0} is not a PE file containing a CLR module or assembly.".format(filename));
            }
            var ILOnly = module.ILOnly;

            Console.WriteLine("module: {0}", module);
        }
    }
}