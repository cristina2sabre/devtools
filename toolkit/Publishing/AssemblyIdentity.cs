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

    public class AssemblyIdentity : ManifestElement {
        private const string AssemblyIdentityTag = "{urn:schemas-microsoft-com:asm.v1}assemblyIdentity";
        private const string TypeAttribute = "type";
        private const string LanguageAttribute = "language";
        private const string NameAttribute = "name";
        private const string VersionAttribute = "version";
        private const string ProcessorArchitectureAttribute = "processorArchitecture";
        private const string PublicKeyTokenAttribute = "publicKeyToken";

        private const string BindingRedirectTag = "{urn:schemas-microsoft-com:asm.v1}bindingRedirect";

        private const string OldVersionAttribute = "oldVersion";
        private const string NewVersionAttribute = "newVersion";


        public AssemblyIdentity(XElement parentElement)
            : base(parentElement) {
        }

        protected override IEnumerable<XElement> Elements {
            get { return _parentElement.Children(AssemblyIdentityTag); }
        }

        internal bool IsActive {
            get { return Active; }
        }

        protected override bool Active {
            set {
                if (value) {
                    if (!Active) {
                        var noinherit = _parentElement.Elements().FirstOrDefault(each => each.Name == NoInherit.NoInheritTag);
                        if (noinherit != null) {
                            noinherit.AddAfterSelf(new XElement(AssemblyIdentityTag));
                        } else {
                            _parentElement.AddFirst(new XElement(AssemblyIdentityTag));
                        }
                        Validate();
                    }
                } else {
                    while (Elements.Any()) {
                        Elements.Remove();
                    }
                }
            }
        }

        private XElement AssemblyIdentityElement {
            get {
                Active = true;
                return _parentElement.Children(AssemblyIdentityTag).FirstOrDefault();
            }
        }

        private XElement BindingRedirectElement {
            get {
                if( Active ) {
                    return _parentElement.Children(BindingRedirectTag).FirstOrDefault();
                }
                return null;
            }
        }


        public string Name {
            get { return AssemblyIdentityElement.AttributeValue(NameAttribute); }
            set { AssemblyIdentityElement.SetAttributeValue(NameAttribute, value); }
        }

        public Architecture Architecture {
            get { return AssemblyIdentityElement.AttributeValue(ProcessorArchitectureAttribute); }
            set { AssemblyIdentityElement.SetAttributeValue(ProcessorArchitectureAttribute, value.ProcessorArchitecture ); }
        }

        public FourPartVersion Version {
            get { return AssemblyIdentityElement.AttributeValue(VersionAttribute); }
            set { AssemblyIdentityElement.SetAttributeValue(VersionAttribute, value == 0L ? null : (string)value); }
        }

        public string PublicKeyToken {
            get { return AssemblyIdentityElement.AttributeValue(PublicKeyTokenAttribute); }
            set { AssemblyIdentityElement.SetAttributeValue(PublicKeyTokenAttribute, value); }
        }

        public string Language {
            get { return AssemblyIdentityElement.AttributeValue(LanguageAttribute); }
            set {
                if (string.IsNullOrEmpty(value) || value == "*") {
                    AssemblyIdentityElement.SetAttributeValue(LanguageAttribute, null);
                    return;
                }
                
                AssemblyIdentityElement.SetAttributeValue(LanguageAttribute, value);
            }
        }

        public BindingRedirect BindingRedirect {
            get {
                var bindingRedirectElement = BindingRedirectElement;
                if( bindingRedirectElement == null ) {
                    return null;
                }
                
                FourPartVersion target = bindingRedirectElement.AttributeValue(NewVersionAttribute);
                if( target == 0L ) {
                    // invalid.
                    return null;
                }

                var rangeText = bindingRedirectElement.AttributeValue(OldVersionAttribute);
                if( string.IsNullOrEmpty(rangeText)) {
                    // invalid
                    return null;
                }

                var range = rangeText.Split('-');
                FourPartVersion from = range[0];
                FourPartVersion to = range.Length > 1 ? (FourPartVersion)range[1] : from;

                if( to == 0L) {
                    // invalid
                    return null;
                }

                return new BindingRedirect {
                    Low = from,
                    High = to,
                    Target = target
                };
            }

            set {
                if (!Active) {
                    return;
                }

                if (value == null || value.High == 0L || value.Target == 0L) {
                    // bad redirect. remove and return
                    while( BindingRedirectElement != null ) {
                        BindingRedirectElement.Remove();
                    }
                    return;
                }

                var bindingRedirectElement = BindingRedirectElement;
                if( bindingRedirectElement == null ) {
                    bindingRedirectElement = new XElement(BindingRedirectTag);
                    _parentElement.Add(bindingRedirectElement);
                }
                bindingRedirectElement.SetAttributeValue(OldVersionAttribute, value.VersionRange );
                bindingRedirectElement.SetAttributeValue(NewVersionAttribute, value.Target.ToString());
            }
        }

        public AssemblyType AssemblyType {
            get {
                switch (AssemblyIdentityElement.AttributeValue(TypeAttribute)) {
                    case "win32-policy":
                        return AssemblyType.win32policy;
                }
                return AssemblyType.win32;
            }

            set {
                switch(value) {
                    case AssemblyType.win32:
                        AssemblyIdentityElement.SetAttributeValue(TypeAttribute, "win32");
                        break;
                    case AssemblyType.win32policy:
                        AssemblyIdentityElement.SetAttributeValue(TypeAttribute, "win32-policy");
                        break;
                }
            }
        }

        protected override void Validate() {
            RemoveExcessElements();

            if (!Active) {
                return;
            }

            // this must absolutely be the first element following the noiherit. (or first otherwise)
            var element = _parentElement.Children(AssemblyIdentityTag).FirstOrDefault();
            if (_parentElement.Elements().SkipWhile(each => each.Name == NoInherit.NoInheritTag).FirstOrDefault() != element) {
                element.Remove();
                var noinherit = _parentElement.Elements().FirstOrDefault(each => each.Name == NoInherit.NoInheritTag);
                if (noinherit != null) {
                    noinherit.AddAfterSelf(element);
                } else {
                    _parentElement.AddFirst(element);
                }
            }
        }
    }
}