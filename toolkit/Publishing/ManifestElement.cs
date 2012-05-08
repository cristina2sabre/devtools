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

    public class ManifestElement {
        protected XElement _parentElement;

        public ManifestElement(XElement parentElement) {
            _parentElement = parentElement;
            Validate();
        }

        protected virtual void Validate() {
        }

        protected virtual IEnumerable<XElement> Elements {
            get { return Enumerable.Empty<XElement>(); }
        }

        protected virtual void RemoveExcessElements(int maxElements = 1) {
            // remove excess trustinfo elements, leave the first one found.
            while (Elements.Count() > maxElements) {
                Elements.Skip(maxElements).Remove();
            }
        }

        protected virtual bool Active {
            get { return Elements.Any(); }
            // ReSharper disable ValueParameterNotUsed
            set { }
            // ReSharper restore ValueParameterNotUsed
        }
    }
}