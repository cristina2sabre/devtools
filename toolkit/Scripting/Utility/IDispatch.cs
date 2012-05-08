namespace CoApp.Developer.Toolkit.Scripting.Utility {
    using System;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
    using DISPPARAMS = System.Runtime.InteropServices.ComTypes.DISPPARAMS;
    using EXCEPINFO = System.Runtime.InteropServices.ComTypes.EXCEPINFO;

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("00020400-0000-0000-C000-000000000046")]
    public interface IDispatch {
        int GetTypeInfoCount();

        [return: MarshalAs(UnmanagedType.Interface)]
        ITypeInfo GetTypeInfo([In, MarshalAs(UnmanagedType.U4)] int iTInfo, [In, MarshalAs(UnmanagedType.U4)] int lcid);

        [PreserveSig]
        int GetIDsOfNames([In] ref Guid riid, [In, MarshalAs(UnmanagedType.LPArray)] string[] rgszNames, [In, MarshalAs(UnmanagedType.U4)] int cNames, [In, MarshalAs(UnmanagedType.U4)] int lcid, [Out, MarshalAs(UnmanagedType.LPArray)] int[] rgDispId);

        [PreserveSig]
        int Invoke(int dispIdMember, ref Guid riid, int lcid, ushort wFlags, out DISPPARAMS pDispParams, out object varResult, out EXCEPINFO pExcepInfo, out int puArgErr);
    }
}