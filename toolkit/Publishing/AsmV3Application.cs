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
    using CoApp.Toolkit.Extensions;

    public class AsmV3Application : ManifestElement {
        private const string Asmv3ApplicationTag = "{urn:schemas-microsoft-com:asm.v3}application";
        private const string WindowsSettingsTag = "{urn:schemas-microsoft-com:asm.v3}windowsSettings";
        private const string DpiAwareTag = "{http://schemas.microsoft.com/SMI/2005/WindowsSettings}dpiAware";

        public AsmV3Application(XElement parentElement) : base(parentElement) {
        }

        protected override IEnumerable<XElement> Elements {
            get { return _parentElement.Children(Asmv3ApplicationTag); }
        }

        protected override bool Active {
            get {
                if (Elements.Any()) {
                    return Elements.FirstOrDefault().Children(WindowsSettingsTag, DpiAwareTag).Any();
                }
                return false;
            }
            set { EnsureElementExists(); }
        }

        private XElement EnsureElementExists() {
            if( !Elements.Any()) {
                _parentElement.Add(new XElement(Asmv3ApplicationTag));
            }
            return Elements.FirstOrDefault().AddOrGet(WindowsSettingsTag).AddOrGet(DpiAwareTag);
        }

        /// <summary>
        ///   Ensures that the element contains at most, a single valid application block
        /// </summary>
        protected override void Validate() {
            RemoveExcessElements();

            if (!Active) {
                return;
            }

            // ensure that we have the right child elements
            var x = DpiAwareElenment;
        }

        private XElement DpiAwareElenment {
            get {
                if (!Active) {
                    return null;
                }
                return EnsureElementExists();
            }
        }

        public bool DpiAware {
            get {
                if (!Active) {
                    return false;
                }
                return DpiAwareElenment != null && DpiAwareElenment.Value.Trim().IsTrue();
            }
            set {
                Active = true;
                DpiAwareElenment.Value = value ? "true" : "false";
            }
        }
    }
}