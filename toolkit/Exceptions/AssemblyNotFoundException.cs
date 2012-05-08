namespace CoApp.Developer.Toolkit.Exceptions {
    using CoApp.Toolkit.Exceptions;
    using CoApp.Toolkit.Extensions;

    public class AssemblyNotFoundException : CoAppException {
        public AssemblyNotFoundException(string filename, string version)
            : base("Failed to find assembly '{0}' version: '{1}'".format(filename, version)) {
        }
    }
}