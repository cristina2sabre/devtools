namespace CoApp.Developer.Toolkit.Scripting.Utility {
    internal enum ScriptState : uint {
        Uninitialized = 0,
        Started = 1,
        Connected = 2,
        Disconnected = 3,
        Closed = 4,
        Initialized = 5,
    }
}