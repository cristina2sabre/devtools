namespace CoApp.Toolkit.Text.Sgml {
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///   An element declaration in a DTD.
    /// </summary>
    public class ElementDecl {
        private string m_name;
        private bool m_startTagOptional;
        private bool m_endTagOptional;
        private ContentModel m_contentModel;
        private string[] m_inclusions;
        private string[] m_exclusions;
        private IDictionary<string, AttDef> m_attList;

        /// <summary>
        ///   Initialises a new element declaration instance.
        /// </summary>
        /// <param name = "name">The name of the element.</param>
        /// <param name = "sto">Whether the start tag is optional.</param>
        /// <param name = "eto">Whether the end tag is optional.</param>
        /// <param name = "cm">The <see cref = "ContentModel" /> of the element.</param>
        /// <param name = "inclusions"></param>
        /// <param name = "exclusions"></param>
        public ElementDecl(string name, bool sto, bool eto, ContentModel cm, string[] inclusions, string[] exclusions) {
            m_name = name;
            m_startTagOptional = sto;
            m_endTagOptional = eto;
            m_contentModel = cm;
            m_inclusions = inclusions;
            m_exclusions = exclusions;
        }

        /// <summary>
        ///   The element name.
        /// </summary>
        public string Name {
            get { return m_name; }
        }

        /// <summary>
        ///   The <see cref = "Sgml.ContentModel" /> of the element declaration.
        /// </summary>
        public ContentModel ContentModel {
            get { return m_contentModel; }
        }

        /// <summary>
        ///   Whether the end tag of the element is optional.
        /// </summary>
        /// <value>true if the end tag of the element is optional, otherwise false.</value>
        public bool EndTagOptional {
            get { return m_endTagOptional; }
        }

        /// <summary>
        ///   Whether the start tag of the element is optional.
        /// </summary>
        /// <value>true if the start tag of the element is optional, otherwise false.</value>
        public bool StartTagOptional {
            get { return m_startTagOptional; }
        }

        /// <summary>
        ///   Finds the attribute definition with the specified name.
        /// </summary>
        /// <param name = "name">The name of the <see cref = "AttDef" /> to find.</param>
        /// <returns>The <see cref = "AttDef" /> with the specified name.</returns>
        /// <exception cref = "InvalidOperationException">If the attribute list has not yet been initialised.</exception>
        public AttDef FindAttribute(string name) {
            if(m_attList == null) {
                throw new InvalidOperationException("The attribute list for the element declaration has not been initialised.");
            }

            AttDef a;
            m_attList.TryGetValue(name.ToUpperInvariant(), out a);
            return a;
        }

        /// <summary>
        ///   Adds attribute definitions to the element declaration.
        /// </summary>
        /// <param name = "list">The list of attribute definitions to add.</param>
        public void AddAttDefs(Dictionary<string, AttDef> list) {
            if(list == null) {
                throw new ArgumentNullException("list");
            }

            if(m_attList == null) {
                m_attList = list;
            }
            else {
                foreach(AttDef a in list.Values) {
                    if(!m_attList.ContainsKey(a.Name)) {
                        m_attList.Add(a.Name, a);
                    }
                }
            }
        }

        /// <summary>
        ///   Tests whether this element can contain another specified element.
        /// </summary>
        /// <param name = "name">The name of the element to check for.</param>
        /// <param name = "dtd">The DTD to use to do the check.</param>
        /// <returns>True if the specified element can be contained by this element.</returns>
        public bool CanContain(string name, SgmlDtd dtd) {
            // return true if this element is allowed to contain the given element.
            if(m_exclusions != null) {
                foreach(string s in m_exclusions) {
                    if(string.Equals(s, name, StringComparison.OrdinalIgnoreCase)) {
                        return false;
                    }
                }
            }

            if(m_inclusions != null) {
                foreach(string s in m_inclusions) {
                    if(string.Equals(s, name, StringComparison.OrdinalIgnoreCase)) {
                        return true;
                    }
                }
            }
            return m_contentModel.CanContain(name, dtd);
        }
    }
}