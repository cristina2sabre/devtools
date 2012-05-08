namespace CoApp.Developer.Toolkit.Scripting.Utility {
    using System;
    using System.Runtime.InteropServices;

    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("71ee5b20-fb04-11d1-b3a8-00a0c911e8b2")]
    internal interface IActiveScriptParseProcedure {
        void ParseProcedureText(string code, string formalParams, string procedureName, string itemName, IntPtr context, string delimiter, int sourceContextCookie, uint startingLineNumber, uint flags, [Out, MarshalAs(UnmanagedType.IDispatch)] out object ppdisp);
    }
}