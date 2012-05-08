namespace CoApp.Developer.Toolkit.Scripting.Utility {
    using System;

    [Flags]
    internal enum ScriptInfo : uint {
        None = 0x0000,
        // ReSharper disable InconsistentNaming
        IUnknown = 0x0001,
        ITypeInfo = 0x0002,
        // ReSharper restore InconsistentNaming
    }
}