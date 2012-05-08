namespace CoApp.Toolkit.Text.Sgml {
    /// <summary>
    ///   Defines the different constraints on an attribute's presence on an element.
    /// </summary>
    public enum AttributePresence {
        /// <summary>
        ///   The attribute has a default value, and its presence is optional.
        /// </summary>
        Default,

        /// <summary>
        ///   The attribute has a fixed value, if present.
        /// </summary>
        Fixed,

        /// <summary>
        ///   The attribute must always be present on every element.
        /// </summary>
        Required,

        /// <summary>
        ///   The element is optional.
        /// </summary>
        Implied
    }
}