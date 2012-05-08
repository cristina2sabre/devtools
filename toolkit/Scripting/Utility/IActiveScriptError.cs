namespace CoApp.Developer.Toolkit.Scripting.Utility {
    using System.Runtime.InteropServices;
    using EXCEPINFO = System.Runtime.InteropServices.ComTypes.EXCEPINFO;

    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("EAE1BA61-A4ED-11CF-8F20-00805F2CD064")]
    public interface IActiveScriptError {
        void GetExceptionInfo(out EXCEPINFO excepinfo);
        void GetSourcePosition(out int sourceContext, out int pulLineNumber, out int plCharacterPosition);
        void GetSourceLineText(out string sourceLine);
    }
}