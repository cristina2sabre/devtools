namespace CoApp.Developer.Toolkit.Scripting.Utility {
    using System;
    using System.Runtime.InteropServices;
    using EXCEPINFO = System.Runtime.InteropServices.ComTypes.EXCEPINFO;

    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("BB1A2AE2-A4F9-11cf-8F20-00805F2CD064")]
    internal interface IActiveScriptParse {
        void InitNew();
        void AddScriptlet(string defaultName, string code, string itemName, string subItemName, string eventName, string delimiter, uint sourceContextCookie, uint startingLineNumber, uint flags, out string name, out EXCEPINFO info);
        void ParseScriptText(string code, string itemName, IntPtr context, string delimiter, uint sourceContextCookie, uint startingLineNumber, uint flags, IntPtr result, out EXCEPINFO info);
    }
}