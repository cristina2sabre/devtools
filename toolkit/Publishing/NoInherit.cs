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

    public class NoInherit : ManifestElement {
        internal const string NoInheritTag = "{urn:schemas-microsoft-com:asm.v1}noInherit";

        public NoInherit(XElement parentElement)
            : base(parentElement) {
        }

        protected override void Validate() {
            RemoveExcessElements();

            if (!Active) {
                return;
            }

            // this must absolutely be the first element 
            var element = _parentElement.Children(NoInheritTag).FirstOrDefault();
            if (_parentElement.Elements().FirstOrDefault() != element) {
                element.Remove();
                _parentElement.AddFirst(element);
            }
        }

        protected override IEnumerable<XElement> Elements {
            get { return _parentElement.Children(NoInheritTag); }
        }

        public bool Value {
            get {
                if (!Active) {
                    return false;
                }
                return (Elements.FirstOrDefault().Value ?? string.Empty).Trim().IsTrue();
            }
            set {
                if (value) {
                    Active = true;
                    Elements.FirstOrDefault().Value = "true";
                }
                Active = false;
            }
        }

        protected override bool Active {
            set {
                if (value) {
                    if (!Active) {
                        _parentElement.AddFirst(new XElement(NoInheritTag));
                        Validate();
                    }
                } else {
                    while (Elements.Any()) {
                        Elements.Remove();
                    }
                }
            }
        }
    }
}