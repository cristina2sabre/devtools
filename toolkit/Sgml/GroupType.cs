namespace CoApp.Toolkit.Text.Sgml {
    /// <summary>
    ///   The type of the content model group, defining the order in which child elements can occur.
    /// </summary>
    public enum GroupType {
        /// <summary>
        ///   No model group.
        /// </summary>
        None,

        /// <summary>
        ///   All elements must occur, in any order.
        /// </summary>
        And,

        /// <summary>
        ///   One (and only one) must occur.
        /// </summary>
        Or,

        /// <summary>
        ///   All element must occur, in the specified order.
        /// </summary>
        Sequence
    } ;
}