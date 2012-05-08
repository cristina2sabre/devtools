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
    public class FileEntry {
        public FileEntry() {
            
        }

        public FileEntry( FileEntry fe ) {
            SourcePath = fe.SourcePath;
            DestinationPath = fe.DestinationPath;
        }

        public FileEntry( string source, string dest ) {
            SourcePath = source;
            DestinationPath = dest;
        }

        public string SourcePath { get; private set; }
        public string DestinationPath { get; private set; }
    }
}