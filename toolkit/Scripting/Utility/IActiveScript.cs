namespace CoApp.Developer.Toolkit.Scripting.Utility {
    using System;
    using System.Runtime.InteropServices;
    using EXCEPINFO = System.Runtime.InteropServices.ComTypes.EXCEPINFO;

    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("BB1A2AE1-A4F9-11cf-8F20-00805F2CD064")]
    internal interface IActiveScript {
        void SetScriptSite([In, MarshalAs(UnmanagedType.Interface)] IActiveScriptSite site);
        void GetScriptSite(ref Guid riid, out IntPtr ppvObject);
        void SetScriptState(uint ss);
        void GetScriptState(out uint ss);
        void Close();
        void AddNamedItem([In, MarshalAs(UnmanagedType.BStr)] string pstrName, [In, MarshalAs(UnmanagedType.U4)] ScriptItem dwFlags);
        void AddTypeLib(ref Guid rguidTypeLib, uint dwMajor, uint dwMinor, uint dwFlags);
        void GetScriptDispatch(string pstrItemName, [Out, MarshalAs(UnmanagedType.IDispatch)] out object ppdisp);
        void GetCurrentScriptThreadiD(out uint id);
        void GetScriptThreadID(uint threadid, out uint id);
        void GetScriptThreadState(uint id, out uint state);
        void InterruptScriptThread(uint id, ref EXCEPINFO info, uint flags);
        void Clone(out IActiveScript item);
    } ;
}