namespace CoApp.Developer.Toolkit.Scripting.Utility {
    using System;

    [Flags]
    internal enum ScriptItem : uint {
        None = 0x0000,

        IsVisible = 0x0002,
        IsSource = 0x0004,
        GlobalMembers = 0x0008,
        IsPersistent = 0x0040,
        CodeOnly = 0x0200,
        NoCode = 0x0400,
    }
}