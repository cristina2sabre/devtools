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
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    public class AssemblyFile : ManifestElement {
        private const string FileTag = "{urn:schemas-microsoft-com:asm.v1}file";
        public AssemblyFile(XElement parentElement)
            : base(parentElement) {
        }

        protected override IEnumerable<XElement> Elements {
            get { return _parentElement.Children(FileTag); }
        }

        protected override bool Active {
            get {
                return Elements.Any();
            }
        }

        public void AddFile(string filename, string SHA1Hash=null ) {
            RemoveFile(filename);
            
            var newFile = new XElement(FileTag);
            newFile.SetAttributeValue("name", filename);
            /*
            if( !string.IsNullOrEmpty(SHA1Hash)) {
                newFile.SetAttributeValue("hash", SHA1Hash);
                newFile.SetAttributeValue("hashalg", "SHA1");
            }*/
            _parentElement.Add( newFile);
        }

        public void RemoveFile(string filename) {
            foreach (var e in Elements.Where(each => each.AttributeValue("name").Equals(filename, StringComparison.CurrentCultureIgnoreCase)).ToArray()) {
                e.Remove();
            }
        }

        public IEnumerable<KeyValuePair<string, string>> Files {
            get { return Elements.Select(each => new KeyValuePair<string,string>(each.AttributeValue("name"), each.AttributeValue("hash"))); }
        }
    }
}