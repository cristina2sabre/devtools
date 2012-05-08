namespace CoApp.Toolkit.Text.Sgml {
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    ///   The different types of literal text returned by the SgmlParser.
    /// </summary>
    public enum LiteralType {
        /// <summary>
        ///   CDATA text literals.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1705", Justification = "This capitalisation is appropriate since the value it represents has all upper-case capitalisation.")] CDATA,

        /// <summary>
        ///   SDATA entities.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1705", Justification = "This capitalisation is appropriate since the value it represents has all upper-case capitalisation.")] SDATA,

        /// <summary>
        ///   The contents of a Processing Instruction.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1705", Justification = "This capitalisation is appropriate since the value it represents has all upper-case capitalisation.")] PI
    } ;
}