namespace CoApp.Developer.Toolkit.Scripting.Utility {
    using System;

    [Flags]
    internal enum ScriptText : uint {
        None = 0x0000,

        DelayExecution = 0x0001,
        IsVisible = 0x0002,
        IsExpression = 0x0020,
        IsPersistent = 0x0040,
        HostManageSource = 0x0080,
    }
}