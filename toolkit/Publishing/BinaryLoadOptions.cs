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


namespace CoApp.Developer.Toolkit.Publishing {
    using System;

    [Flags]
    public enum BinaryLoadOptions {
        DelayLoad = 0, // load nothing by default

        PEInfo = 1, // load any PE header information 
        VersionInfo = 2, // load any file version information
        DependencyData = 4, // load any information about native dependencies

        Managed = 8, // explicitly load managed IL 
        Resources = 16, // explicitly load resources
        Manifest = 32, // explicitly load manifest

        MD5 = 64, // calculate the MD5 hash

        NoManaged = 128, // don't do any managed-code stuff at all.
        NoResources = 256, // don't change any native resources
        NoManifest = 512, // don't change any manifest data
        NoSignature = 1024, // don't attempt to sign this when you save it.

        ValidateSignature = 2048, // validate that this file has a valid signature

        UnsignedManagedDependencies = 32768, // loads unsigned dependent assemblies too
        NoUnsignedManagedDependencies = 65536, // don't load unsigned dependent assemblies too

        All = PEInfo | VersionInfo | Managed | Resources | Manifest | UnsignedManagedDependencies, // explictly preload all useful data
    }
}