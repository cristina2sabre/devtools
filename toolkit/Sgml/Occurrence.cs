namespace CoApp.Toolkit.Text.Sgml {
    /// <summary>
    ///   Qualifies the occurrence of a child element within a content model group.
    /// </summary>
    public enum Occurrence {
        /// <summary>
        ///   The element is required and must occur only once.
        /// </summary>
        Required,

        /// <summary>
        ///   The element is optional and must occur once at most.
        /// </summary>
        Optional,

        /// <summary>
        ///   The element is optional and can be repeated.
        /// </summary>
        ZeroOrMore,

        /// <summary>
        ///   The element must occur at least once or more times.
        /// </summary>
        OneOrMore
    }
}