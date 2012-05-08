namespace CoApp.Toolkit.Text.Sgml {
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    ///   Defines the different possible attribute types.
    /// </summary>
    public enum AttributeType {
        /// <summary>
        ///   Attribute type not specified.
        /// </summary>
        Default,

        /// <summary>
        ///   The attribute contains text (with no markup).
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1705", Justification = "This capitalisation is appropriate since the value it represents has all upper-case capitalisation.")] CDATA,

        /// <summary>
        ///   The attribute contains an entity declared in a DTD.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1705", Justification = "This capitalisation is appropriate since the value it represents has all upper-case capitalisation.")] ENTITY,

        /// <summary>
        ///   The attribute contains a number of entities declared in a DTD.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1705", Justification = "This capitalisation is appropriate since the value it represents has all upper-case capitalisation.")] ENTITIES,

        /// <summary>
        ///   The attribute is an id attribute uniquely identifie the element it appears on.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1705", Justification = "This capitalisation is appropriate since the value it represents has all upper-case capitalisation.")] [SuppressMessage("Microsoft.Naming", "CA1706", Justification = "This capitalisation is appropriate since the value it represents has all upper-case capitalisation.")] ID,

        /// <summary>
        ///   The attribute value can be any declared subdocument or data entity name.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1705", Justification = "This capitalisation is appropriate since the value it represents has all upper-case capitalisation.")] IDREF,

        /// <summary>
        ///   The attribute value is a list of (space separated) declared subdocument or data entity names.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1705", Justification = "This capitalisation is appropriate since the value it represents has all upper-case capitalisation.")] IDREFS,

        /// <summary>
        ///   The attribute value is a SGML Name.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1705", Justification = "This capitalisation is appropriate since the value it represents has all upper-case capitalisation.")] NAME,

        /// <summary>
        ///   The attribute value is a list of (space separated) SGML Names.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1705", Justification = "This capitalisation is appropriate since the value it represents has all upper-case capitalisation.")] NAMES,

        /// <summary>
        ///   The attribute value is an XML name token (i.e. contains only name characters, but in this case with digits and other valid name characters accepted as the first character).
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1705", Justification = "This capitalisation is appropriate since the value it represents has all upper-case capitalisation.")] NMTOKEN,

        /// <summary>
        ///   The attribute value is a list of (space separated) XML NMTokens.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1705", Justification = "This capitalisation is appropriate since the value it represents has all upper-case capitalisation.")] NMTOKENS,

        /// <summary>
        ///   The attribute value is a number.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1705", Justification = "This capitalisation is appropriate since the value it represents has all upper-case capitalisation.")] NUMBER,

        /// <summary>
        ///   The attribute value is a list of (space separated) numbers.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1705", Justification = "This capitalisation is appropriate since the value it represents has all upper-case capitalisation.")] NUMBERS,

        /// <summary>
        ///   The attribute value is a number token (i.e. a name that starts with a number).
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1705", Justification = "This capitalisation is appropriate since the value it represents has all upper-case capitalisation.")] NUTOKEN,

        /// <summary>
        ///   The attribute value is a list of number tokens.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1705", Justification = "This capitalisation is appropriate since the value it represents has all upper-case capitalisation.")] NUTOKENS,

        /// <summary>
        ///   Attribute value is a member of the bracketed list of notation names that qualifies this reserved name.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1705", Justification = "This capitalisation is appropriate since the value it represents has all upper-case capitalisation.")] NOTATION,

        /// <summary>
        ///   The attribute value is one of a set of allowed names.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1705", Justification = "This capitalisation is appropriate since the value it represents has all upper-case capitalisation.")] ENUMERATION
    }
}