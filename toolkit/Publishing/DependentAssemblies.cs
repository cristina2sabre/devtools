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
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using CoApp.Toolkit.Win32;

    public class DependentAssemblies : ManifestElement {
        private const string DependencyTag = "{urn:schemas-microsoft-com:asm.v1}dependency";
        private const string DependentAssemblyTag = "{urn:schemas-microsoft-com:asm.v1}dependentAssembly";

        // AssemblyReference
        // <AssemblyReference><dependentAssembly><assemblyIdentity type=""win32"" name=""[$LIBNAME]"" version=""[$LIBVERSION]"" processorArchitecture=""[$ARCH]"" publicKeyToken=""[$PUBLICKEYTOKEN]"" /></dependentAssembly></AssemblyReference>";

        public DependentAssemblies(XElement parentElement)
            : base(parentElement) {
        }

        protected override void Validate() {
        }

        protected override IEnumerable<XElement> Elements {
            get { return _parentElement.Children(DependencyTag); }
        }

        private IEnumerable<XElement> DependentAssemblyElements {
            get { return Elements.Select(elem => XElementExtensions.Children(elem, DependentAssemblyTag).FirstOrDefault()).Where(dat => dat != null); }
        }

        public IEnumerable<AssemblyReference> Dependencies {
            get {
                return from dat in DependentAssemblyElements
                    let assemblyIdentity = new AssemblyIdentity(dat)
                    select
                        new AssemblyReference {
                            Name = assemblyIdentity.Name,
                            Version = assemblyIdentity.Version,
                            Architecture = assemblyIdentity.Architecture,
                            PublicKeyToken = assemblyIdentity.PublicKeyToken,
                            Language = assemblyIdentity.Language,
                            AssemblyType = assemblyIdentity.AssemblyType,
                            BindingRedirect = assemblyIdentity.BindingRedirect,
                        };
            }
        }

        public void AddDependency(string name, FourPartVersion version, Architecture arch, string publicKeyToken, string language = "*", AssemblyType assemblyType = AssemblyType.win32, BindingRedirect bindingRedirect = null) {
            if (!(from dat in DependentAssemblyElements
                let assemblyIdentity = new AssemblyIdentity(dat)
                where
                    assemblyIdentity.Name == name &&
                        assemblyIdentity.Version == version &&
                            assemblyIdentity.Architecture == arch &&
                                assemblyIdentity.PublicKeyToken == publicKeyToken &&
                                    ((language == "*" && string.IsNullOrEmpty(assemblyIdentity.Language)) || assemblyIdentity.Language == language)
                select assemblyIdentity).Any()) {
                // add another.
                var dat = new XElement(DependencyTag, new XElement(DependentAssemblyTag));
                var identity = new AssemblyIdentity(dat.Elements().FirstOrDefault()) {
                    AssemblyType = assemblyType,
                    Name = name,
                    Version = version,
                    Architecture = arch,
                    PublicKeyToken = publicKeyToken,
                    Language = language,
                    BindingRedirect = bindingRedirect
                };
                _parentElement.Add(dat);
            }
        }

        public void RemoveDependency(string name, FourPartVersion version, Architecture arch, string publicKeyToken, string language = "*") {
            var deleteThis = (from dat in DependentAssemblyElements
                let assemblyIdentity = new AssemblyIdentity(dat)
                where
                    assemblyIdentity.Name == name &&
                        assemblyIdentity.Version == version &&
                            assemblyIdentity.Architecture == arch &&
                                assemblyIdentity.PublicKeyToken == publicKeyToken &&
                                    ((language == "*" && string.IsNullOrEmpty(assemblyIdentity.Language)) || assemblyIdentity.Language == language)
                select dat).FirstOrDefault();

            if (deleteThis != null) {
                deleteThis.Parent.Remove();
            }
        }
    }
}