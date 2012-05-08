namespace CoApp.Developer.Toolkit.Scripting.Utility {
    using System;
    using System.Runtime.InteropServices;
    using EXCEPINFO = System.Runtime.InteropServices.ComTypes.EXCEPINFO;

    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("DB01A1E3-A42B-11cf-8F20-00805F2CD064")]
    internal interface IActiveScriptSite {
        void GetLCID(out uint id);
        void GetItemInfo(string pstrName, uint dwReturnMask, [Out, MarshalAs(UnmanagedType.IUnknown)] out object item, IntPtr ppti);
        void GetDocVersionString(out string v);
        void OnScriptTerminate(ref object result, ref EXCEPINFO info);
        void OnStateChange(uint state);
        void OnScriptError(IActiveScriptError err);
        void OnEnterScript();
        void OnLeaveScript();
    }
}