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
    using System.Xml.Linq;

    internal static class Namespaces {
        internal static XNamespace AssemblyV3 = XNamespace.Get("urn:schemas-microsoft-com:asm.v3");
        internal static XNamespace AssemblyV1 = XNamespace.Get("urn:schemas-microsoft-com:asm.v1");
        internal static XNamespace CompatabilityV1 = XNamespace.Get("urn:schemas-microsoft-com:compatibility.v1");
        internal static XNamespace WindowsSettings = XNamespace.Get("http://schemas.microsoft.com/SMI/2005/WindowsSettings");
    }
}