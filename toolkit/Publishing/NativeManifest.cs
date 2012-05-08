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
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using CoApp.Toolkit.Extensions;
    using CoApp.Toolkit.Win32;

    public class NativeManifest {
        private const string DefaultManifestXml =
            @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<assembly xmlns=""urn:schemas-microsoft-com:asm.v1"" manifestVersion=""1.0"">
</assembly>";

        private readonly XDocument _assembly;
        private readonly TrustInfo _trustInfo;
        private readonly AsmV3Application _asmV3Application;
        private readonly DependentAssemblies _dependentAssemblies;
        private readonly NoInherit _noInherit;
        private readonly AssemblyIdentity _assemblyIdentity;
        private readonly Compatibility _compatibility;
        private readonly AssemblyFile _files;

        public bool Modified { get; set; }

        public NativeManifest(string manifestText) {
            if (string.IsNullOrEmpty(manifestText)) {
                manifestText = DefaultManifestXml;
            }
            _assembly = XDocument.Parse(manifestText);

            _noInherit = new NoInherit(_assembly.Root);
            _assemblyIdentity = new AssemblyIdentity(_assembly.Root);
            _trustInfo = new TrustInfo(_assembly.Root);
            _asmV3Application = new AsmV3Application(_assembly.Root);
            _dependentAssemblies = new DependentAssemblies(_assembly.Root);
            _compatibility = new Compatibility(_assembly.Root);
            _files = new AssemblyFile(_assembly.Root);
            Modified = false;
        }

        public override string ToString() {
            using (var memoryStream = new MemoryStream()) {
                using (
                    var xw = XmlWriter.Create(memoryStream,
                        new XmlWriterSettings
                        {ConformanceLevel = ConformanceLevel.Document, Encoding = new UTF8Encoding(false), OmitXmlDeclaration = false, Indent = true})) {
                    _assembly.WriteTo(xw);
                }
                return Encoding.UTF8.GetString(memoryStream.GetBuffer());
            }
        }

        public ExecutionLevel RequestedExecutionLevel {
            get { return _trustInfo.Level; }
            set {
                Modified = true;
                _trustInfo.Level = value;
            }
        }

        public bool UiAccess {
            get { return _trustInfo.UiAccess; }
            set {
                Modified = true;
                _trustInfo.UiAccess = value;
            }
        }

        public bool NoInherit {
            get { return _noInherit.Value; }
            set {
                Modified = true;
                _noInherit.Value = value;
            }
        }

        public bool DpiAware {
            get { return _asmV3Application.DpiAware; }
            set {
                Modified = true;
                _asmV3Application.DpiAware = value;
            }
        }

        public bool Win8Compatible {
            get { return _compatibility.Win8Compatibile; }
            set {
                Modified = true;
                _compatibility.Win8Compatibile = value;
            }
        }

        public bool VistaCompatible {
            get { return _compatibility.WinVistaCompatibile; }
            set {
                Modified = true;
                _compatibility.WinVistaCompatibile = value;
            }
        }

        public bool Win7Compatible {
            get { return _compatibility.Win7Compatibile; }
            set {
                Modified = true;
                _compatibility.Win7Compatibile = value;
            }
        }

        public IEnumerable<AssemblyReference> Dependencies {
            get { return _dependentAssemblies.Dependencies; }
        }

        public void AddDependency(string name, FourPartVersion version, Architecture arch, string publicKeyToken, string language = "*", AssemblyType assemblyType = AssemblyType.win32, BindingRedirect redirect = null) {
            Modified = true;
            _dependentAssemblies.AddDependency(name, version, arch, publicKeyToken, language, assemblyType, redirect);
        }

        public void AddDependency(AssemblyReference assemblyReference) {
            AddDependency(assemblyReference.Name, assemblyReference.Version, assemblyReference.Architecture, assemblyReference.PublicKeyToken,
                assemblyReference.Language, assemblyReference.AssemblyType, assemblyReference.BindingRedirect);
        }

        public void RemoveDependency(AssemblyReference assemblyReference) {
            Modified = true;
            _dependentAssemblies.RemoveDependency(assemblyReference.Name, assemblyReference.Version, assemblyReference.Architecture,
                assemblyReference.PublicKeyToken, assemblyReference.Language);
        }

        public void RemoveDependency(IEnumerable<AssemblyReference> dependencies) {
            if (!dependencies.IsNullOrEmpty()) {
                foreach (var dependency in dependencies.ToArray()) {
                    RemoveDependency(dependency);
                }
            }
        }

        public string AssemblyName {
            get { return _assemblyIdentity.IsActive ? _assemblyIdentity.Name : null; }
            set {
                Modified = true;
                _assemblyIdentity.Name = value;
            }
        }

        public FourPartVersion AssemblyVersion {
            get { return _assemblyIdentity.IsActive ? _assemblyIdentity.Version : 0; }
            set {
                Modified = true;
                _assemblyIdentity.Version = value;
            }
        }

        public Architecture AssemblyArchitecture {
            get { return _assemblyIdentity.IsActive ? _assemblyIdentity.Architecture : Architecture.Unknown; }
            set {
                Modified = true;
                _assemblyIdentity.Architecture = value;
            }
        }

        public string AssemblyPublicKeyToken {
            get { return _assemblyIdentity.IsActive ? _assemblyIdentity.PublicKeyToken : null; }
            set {
                Modified = true;
                _assemblyIdentity.PublicKeyToken = value;
            }
        }

        public string AssemblyLanguage {
            get { return _assemblyIdentity.IsActive ? _assemblyIdentity.Language : null; }
            set {
                Modified = true;
                _assemblyIdentity.Language = value;
            }
        }

        public AssemblyType AssemblyType {
            get {
                if (_assemblyIdentity.IsActive) {
                    return _assemblyIdentity.AssemblyType;
                }
                return AssemblyType.win32;
            }

            set { _assemblyIdentity.AssemblyType = value; }
        }
        

        public IEnumerable<KeyValuePair<string ,string>> AssemblyFiles {
            get { return _files.Files; }
        }

        public void AddFile(string filename, string SHA1Hash=null) {
            _files.AddFile(filename, SHA1Hash);
        }

        public void RemoveFile(string filename) {
            _files.RemoveFile(filename);
        }
    }

}