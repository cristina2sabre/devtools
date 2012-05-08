namespace CoApp.Toolkit.Text.Sgml {
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Xml;

    /// <summary>
    ///   This class models an XML node, an array of elements in scope is maintained while parsing
    ///   for validation purposes, and these Node objects are reused to reduce object allocation,
    ///   hence the reset method.
    /// </summary>
    internal class Node {
        internal XmlNodeType NodeType;
        internal string Value;
        internal XmlSpace Space;
        internal string XmlLang;
        internal bool IsEmpty;
        internal string Name;
        internal ElementDecl DtdType; // the DTD type found via validation
        internal State CurrentState;
        internal bool Simulated; // tag was injected into result stream.
        private HWStack attributes = new HWStack(10);

        /// <summary>
        ///   Attribute objects are reused during parsing to reduce memory allocations, 
        ///   hence the Reset method.
        /// </summary>
        public void Reset(string name, XmlNodeType nt, string value) {
            this.Value = value;
            this.Name = name;
            this.NodeType = nt;
            this.Space = XmlSpace.None;
            this.XmlLang = null;
            this.IsEmpty = true;
            this.attributes.Count = 0;
            this.DtdType = null;
        }

        public Attribute AddAttribute(string name, string value, char quotechar, bool caseInsensitive) {
            Attribute a;
            // check for duplicates!
            for(int i = 0, n = this.attributes.Count; i < n; i++) {
                a = (Attribute) this.attributes[i];
                if(string.Equals(a.Name, name, caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) {
                    return null;
                }
            }
            // This code makes use of the high water mark for attribute objects,
            // and reuses exisint Attribute objects to avoid memory allocation.
            a = (Attribute) this.attributes.Push();
            if(a == null) {
                a = new Attribute();
                this.attributes[this.attributes.Count - 1] = a;
            }
            a.Reset(name, value, quotechar);
            return a;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811", Justification = "Kept for potential future usage.")]
        public void RemoveAttribute(string name) {
            for(int i = 0, n = this.attributes.Count; i < n; i++) {
                var a = (Attribute) this.attributes[i];
                if(string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)) {
                    this.attributes.RemoveAt(i);
                    return;
                }
            }
        }

        public void CopyAttributes(Node n) {
            for(int i = 0, len = n.attributes.Count; i < len; i++) {
                var a = (Attribute) n.attributes[i];
                var na = this.AddAttribute(a.Name, a.Value, a.QuoteChar, false);
                na.DtdType = a.DtdType;
            }
        }

        public int AttributeCount {
            get { return this.attributes.Count; }
        }

        public int GetAttribute(string name) {
            for(int i = 0, n = this.attributes.Count; i < n; i++) {
                var a = (Attribute) this.attributes[i];
                if(string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase)) {
                    return i;
                }
            }
            return -1;
        }

        public Attribute GetAttribute(int i) {
            if(i >= 0 && i < this.attributes.Count) {
                var a = (Attribute) this.attributes[i];
                return a;
            }
            return null;
        }
    }
}