namespace CoApp.Developer.Toolkit.Exceptions {
    using CoApp.Toolkit.Exceptions;
    using CoApp.Toolkit.Extensions;

    public class DigitalSignFailure : CoAppException {
        public uint Win32Code;
        public DigitalSignFailure(string filename, uint win32Code)
            : base("Failed to digitally sign '{0}' Win32 RC: '{1:x}'".format(filename, win32Code)) {
            Win32Code = win32Code;
        }
    }
}