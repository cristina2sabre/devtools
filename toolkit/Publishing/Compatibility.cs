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

    public class Compatibility : ManifestElement {
        private const string CompatibilityTag = "{urn:schemas-microsoft-com:compatibility.v1}compatibility";
        private const string ApplicationTag = "{urn:schemas-microsoft-com:compatibility.v1}application";
        private const string SupportedOSTag = "{urn:schemas-microsoft-com:compatibility.v1}supportedOS";

        private const string WinVistaCompatabilityId = "{e2011457-1546-43c5-a5fe-008deee3d3f0}";
        private const string Win7CompatabilityId = " {35138b9a-5d96-4fbd-8e2d-a2440225f93a}";
        private const string Win8CompatabilityId = "{4a2f28e3-53b9-4441-ba9c-d69d4a4a6e38}";

        public Compatibility(XElement parentElement)
            : base(parentElement) {
        }

        private IEnumerable<XElement> SupportedOsElements {
            get { return _parentElement.Children(CompatibilityTag, ApplicationTag, SupportedOSTag); }
        }

        protected override void Validate() {
            RemoveExcessElements();
        }

        public bool WinVistaCompatibile {
            get {
                if (!Active) {
                    return false;
                }
                return SupportedOsElements.Any(each => XElementExtensions.AttributeValue(each, "Id") == WinVistaCompatabilityId);
            }
            set {
                if (value) {
                    if (!WinVistaCompatibile) {
                        var vista = new XElement(SupportedOSTag);
                        vista.SetAttributeValue("Id", WinVistaCompatabilityId);
                        _parentElement.AddOrGet(CompatibilityTag).AddOrGet(ApplicationTag).Add(vista);
                    }
                } else {
                    SupportedOsElements.Where(each => each.AttributeValue("Id") == WinVistaCompatabilityId).Remove();
                    if (!SupportedOsElements.Any()) {
                        Active = false;
                    }
                }
            }
        }

        public bool Win7Compatibile {
            get {
                if (!Active) {
                    return false;
                }
                return SupportedOsElements.Any(each => each.AttributeValue("Id") == Win7CompatabilityId);
            }
            set {
                if (value) {
                    if (!Win7Compatibile) {
                        var w7 = new XElement(SupportedOSTag);
                        w7.SetAttributeValue("Id", Win7CompatabilityId);
                        _parentElement.AddOrGet(CompatibilityTag).AddOrGet(ApplicationTag).Add(w7);
                    }
                } else {
                    SupportedOsElements.Where(each => each.AttributeValue("Id") == Win7CompatabilityId).Remove();
                    if (!SupportedOsElements.Any()) {
                        Active = false;
                    }
                }
            }
        }

        public bool Win8Compatibile {
            get {
                if (!Active) {
                    return false;
                }
                return SupportedOsElements.Any(each => each.AttributeValue("Id") == Win8CompatabilityId);
            }
            set {
                if (value) {
                    if (!Win8Compatibile) {
                        var w8 = new XElement(SupportedOSTag);
                        w8.SetAttributeValue("Id", Win8CompatabilityId);
                        _parentElement.AddOrGet(CompatibilityTag).AddOrGet(ApplicationTag).Add(w8);
                    }
                } else {
                    SupportedOsElements.Where(each => each.AttributeValue("Id") == Win8CompatabilityId).Remove();
                    if (!SupportedOsElements.Any()) {
                        Active = false;
                    }
                }
            }
        }

        protected override bool Active {
            set {
                if (value) {
                    if (!Active) {
                        _parentElement.Add(new XElement(CompatibilityTag));
                        Validate();
                    }
                } else {
                    while (Elements.Any()) {
                        Elements.Remove();
                    }
                }
            }
        }

        protected override IEnumerable<XElement> Elements {
            get { return _parentElement.Children(CompatibilityTag); }
        }
    }
}