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

namespace CoApp.Autopackage {
    using System;
    using Toolkit.Exceptions;
    using Toolkit.Utility;

    internal static class Tools {
        internal static bool ShowTools;
        internal static ProcessUtility AssemblyLinker;
        internal static ProcessUtility ManifestTool;
        internal static ProcessUtility MakeCatalog;
        internal static ProcessUtility WixCompiler;
        internal static ProcessUtility WixLinker;

        internal static void LocateCommandlineTools() {
            try {
                WixCompiler = new ProcessUtility(ProgramFinder.ProgramFilesAndDotNet.ScanForFile("candle.exe", minimumVersion: "3.6", executableType: ExecutableInfo.managed));
            } catch {
                throw new ConsoleException(
                    "Unable to find 'candle.exe' from WiX 3.5 installation. \r\n\r\n ****************\r\n Autopackage Requires Wix 3.6 to be installed\r\n Get it from (http://wix.codeplex.com/releases/view/75656)\r\n ****************\r\n");
            }

            try {
                WixLinker = new ProcessUtility(ProgramFinder.ProgramFilesAndDotNet.ScanForFile("light.exe", minimumVersion: "3.6", executableType: ExecutableInfo.managed));
            } catch {
                throw new ConsoleException("Unable to find 'light.exe' from WiX 3.5 installation. \r\n\r\n ****************\r\n Autopackage Requires Wix 3.6 to be installed\r\n Get it from (http://wix.codeplex.com/releases/view/75656)\r\n ****************\r\n");
            }

            try {
                ManifestTool = new ProcessUtility(ProgramFinder.ProgramFilesAndDotNet.ScanForFile("mt.exe"));
            } catch {
                throw new ConsoleException("Unable to find 'mt.exe' from Windows SDK.");
            }

            try {
                MakeCatalog = new ProcessUtility(ProgramFinder.ProgramFilesAndDotNet.ScanForFile("makecat.exe"));
            } catch {
                throw new ConsoleException("Unable to find 'makecat.exe' from Windows SDK.");
            }

            try {
                AssemblyLinker = new ProcessUtility(ProgramFinder.ProgramFilesAndDotNet.ScanForFile("al.exe"));
            } catch {
                throw new ConsoleException("Unable to find 'al.exe' from Windows SDK.");
            }

            if (ShowTools) {
                Console.WriteLine("Tools:");
                Console.WriteLine("Wix/Candle.exe : {0}", WixCompiler.Executable);
                Console.WriteLine("Wix/Light.exe : {0}", WixLinker.Executable);
                Console.WriteLine("SDK/mt.exe : {0}", ManifestTool.Executable);
                Console.WriteLine("SDK/makecat.exe : {0}", MakeCatalog.Executable);
                Console.WriteLine("SDK/al.exe : {0}", AssemblyLinker.Executable);
            }
        }
    }
}