namespace CoApp.Developer.Toolkit.Debugging {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Security.AccessControl;
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;
    using CoApp.Toolkit.Extensions;
    using CoApp.Toolkit.Win32;

    /// <summary>
    ///   Delegate used when firing OnOutputDebug event
    /// </summary>
    public delegate void OutputDebugStringHandler(OutputDebugStringEventArgs args);

    public class FauxProcess {
        private FauxProcess() {
        }

        public int Id { get; internal set; }
        public string ProcessName { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime FirstMessageTime { get; private set; }

        private static readonly Dictionary<int, FauxProcess> ProcessCache = new Dictionary<int, FauxProcess>();

        internal static FauxProcess GetProcess(int pid, DateTime msgTime) {
            lock (ProcessCache) {
                FauxProcess result;

                if (ProcessCache.ContainsKey(pid)) {
                    result = ProcessCache[pid];
                    if (result.FirstMessageTime == DateTime.MinValue) {
                        result.FirstMessageTime = msgTime;
                    }
                    return result;
                }
                try {
                    
                    var proc = Process.GetProcessById(pid);
                    result = new FauxProcess {
                        ProcessName = proc.ProcessName,
                        StartTime = proc.StartTime,
                        FirstMessageTime = msgTime,
                        Id = pid
                    };
                }
                catch {
                    result = new FauxProcess {
                        ProcessName = "<exited>",
                        StartTime = msgTime,
                        FirstMessageTime = msgTime,
                        Id = pid
                    };
                }
                ProcessCache.Add(pid, result);
                return result;
            }
        }

        public void ResetFirstMessageTime() {
            FirstMessageTime = DateTime.MinValue;
        }

        public static void ResetAll() {
            foreach( var v in ProcessCache.Values ) {
                v.ResetFirstMessageTime();
            }
        }
    }

    public class OutputDebugStringEventArgs {
        internal OutputDebugStringEventArgs(int processId, DateTime messageTime, string messageText) {
            ProcessId = processId;
            MessageTime = messageTime;
            Message = messageText;
            SinceFirstEvent = messageTime.Subtract(Process.FirstMessageTime);
            SinceProcessStarted= messageTime.Subtract(Process.StartTime);
        }

        public int ProcessId { get; private set; }
        public DateTime MessageTime { get; private set; }
        public FauxProcess Process { get {
            return FauxProcess.GetProcess(ProcessId, MessageTime);
        } }
        public TimeSpan SinceProcessStarted { get; private set; }
        public TimeSpan SinceFirstEvent { get; private set; }
        public string Message { get; private set; }
      
    }

    public static class TimeSpanExtensions {
        public static string AsDebugOffsetString(this TimeSpan timespan) {
            return "{0}{1}{2}{3}{4}".format(
                timespan.Days == 0 ? "    " : "{0:D3}d".format(timespan.Days),
                timespan.Hours == 0 ? "   " : "{0:D2}:".format(timespan.Hours),
                timespan.Minutes == 0 ? "   " : "{0:D2}:".format(timespan.Minutes),
                timespan.Seconds == 0 ? " 0." : "{0:D2}.".format(timespan.Seconds),
                "{0:D3}".format(timespan.Milliseconds));
        }
    }

    public class Monitor {
        public static event OutputDebugStringHandler OnOutputDebugString;
        private static Task _listenTask;
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public static void Start() {
            if (_listenTask == null) {
                lock (typeof (Monitor)) {
                    _cancellationTokenSource = new CancellationTokenSource();

                    bool wasCreated;
                    var ewhSec = new EventWaitHandleSecurity();
                    ewhSec.AddAccessRule(
                        new EventWaitHandleAccessRule(
                            new SecurityIdentifier(WellKnownSidType.WorldSid, null), EventWaitHandleRights.FullControl, AccessControlType.Allow));

                    var sessionAckEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "DBWIN_BUFFER_READY", out wasCreated, ewhSec);
                    var sessionReadyEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "DBWIN_DATA_READY", out wasCreated, ewhSec);
                    var globalAckEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "Global\\DBWIN_BUFFER_READY", out wasCreated, ewhSec);
                    var globalReadyEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "Global\\DBWIN_DATA_READY", out wasCreated, ewhSec);

                    var sd = new SECURITY_DESCRIPTOR();
                    var sa = new SECURITY_ATTRIBUTES();

                    // Initialize the security descriptor.
                    if (!Advapi32.InitializeSecurityDescriptor(ref sd, 1)) {
                        throw new ApplicationException(
                            string.Format("{0}. Last Win32 Error was {1}", "Failed to initializes the security descriptor.", Marshal.GetLastWin32Error()));
                    }

                    // Set information in a discretionary access control list
                    if (!Advapi32.SetSecurityDescriptorDacl(ref sd, true, IntPtr.Zero, false)) {
                        throw new ApplicationException(
                            string.Format("{0}. Last Win32 Error was {1}", "Failed to initializes the security descriptor", Marshal.GetLastWin32Error()));
                    }

                    // Create the event for slot 'DBWIN_BUFFER_READY'
                    sa.nLength = Marshal.SizeOf(sa);
                    sa.lpSecurityDescriptor = Marshal.AllocHGlobal(Marshal.SizeOf(sd));
                    Marshal.StructureToPtr(sd, sa.lpSecurityDescriptor, false);

                    // Get a handle to the readable shared memory at slot 'DBWIN_BUFFER'.
                    var sessionSharedFileHandle = Kernel32.CreateFileMapping(new IntPtr(-1), ref sa, PageProtection.ReadWrite, 0, 4096, "DBWIN_BUFFER");
                    if (sessionSharedFileHandle == IntPtr.Zero) {
                        throw new ApplicationException(
                            string.Format(
                                "{0}. Last Win32 Error was {1}", "Failed to create a file mapping to slot 'DBWIN_BUFFER'", Marshal.GetLastWin32Error()));
                    }

                    // Create a view for this file mapping so we can access it
                    var sessionSharedMemoryHandle = Kernel32.MapViewOfFile(sessionSharedFileHandle, /*SECTION_MAP_READ*/ 0x0004, 0, 0, 4096);
                    if (sessionSharedMemoryHandle == IntPtr.Zero) {
                        throw new ApplicationException(
                            string.Format(
                                "{0}. Last Win32 Error was {1}", "Failed to create a mapping view for slot 'DBWIN_BUFFER'", Marshal.GetLastWin32Error()));
                    }

                    // Get a handle to the readable shared memory at slot 'DBWIN_BUFFER'.
                    var globalSharedFileHandle = Kernel32.CreateFileMapping(new IntPtr(-1), ref sa, PageProtection.ReadWrite, 0, 4096, "Global\\DBWIN_BUFFER");
                    if (globalSharedFileHandle == IntPtr.Zero) {
                        throw new ApplicationException(
                            string.Format(
                                "{0}. Last Win32 Error was {1}", "Failed to create a file mapping to slot 'Global\\DBWIN_BUFFER'", Marshal.GetLastWin32Error()));
                    }

                    // Create a view for this file mapping so we can access it
                    var globalSharedMemoryHandle = Kernel32.MapViewOfFile(globalSharedFileHandle, /*SECTION_MAP_READ*/ 0x0004, 0, 0, 4096);
                    if (globalSharedMemoryHandle == IntPtr.Zero) {
                        throw new ApplicationException(
                            string.Format(
                                "{0}. Last Win32 Error was {1}", "Failed to create a mapping view for slot 'Global\\DBWIN_BUFFER'", Marshal.GetLastWin32Error()));
                    }

                    var queue = new Queue<Tuple<int, DateTime, string>>();
                    var dataAvailable = new ManualResetEvent(true);

                    _listenTask = Task.Factory.StartNew(
                        () => {
                            // Everything after the first DWORD is our debugging text
                            IntPtr sessionStringPointer;
                            IntPtr globalStringPointer;

                            if (Environment.Is64BitProcess) {
                                sessionStringPointer = new IntPtr(sessionSharedMemoryHandle.ToInt64() + Marshal.SizeOf(typeof (int)));
                                globalStringPointer = new IntPtr(globalSharedMemoryHandle.ToInt64() + Marshal.SizeOf(typeof (int)));
                            } else {
                                sessionStringPointer = new IntPtr(sessionSharedMemoryHandle.ToInt32() + Marshal.SizeOf(typeof (int)));
                                globalStringPointer = new IntPtr(globalSharedMemoryHandle.ToInt32() + Marshal.SizeOf(typeof (int)));
                            }

                            while (!_cancellationTokenSource.IsCancellationRequested) {
                                sessionAckEvent.Set();
                                globalAckEvent.Set();

                                try {
                                    var i = WaitHandle.WaitAny(new[] {sessionReadyEvent, globalReadyEvent, _cancellationTokenSource.Token.WaitHandle});
                                    var now = DateTime.Now;
                                    if (i == 0) {
                                        lock (queue) {
                                            queue.Enqueue(new Tuple<int, DateTime, string>(Marshal.ReadInt32(sessionSharedMemoryHandle), now, Marshal.PtrToStringAnsi(sessionStringPointer)));
                                            dataAvailable.Set();
                                        }
                                    }

                                    if (i == 1) {
                                        lock (queue) {
                                            queue.Enqueue(new Tuple<int, DateTime, string>(Marshal.ReadInt32(globalSharedMemoryHandle), now, Marshal.PtrToStringAnsi(globalStringPointer)));
                                            dataAvailable.Set();
                                        }
                                    }
                                } catch {
                                    // it's over. 
                                    _cancellationTokenSource.Cancel();
                                }
                            }
                            _listenTask = null;

                            // cleanup after stopping.
                            globalAckEvent.Reset();
                            globalAckEvent.Dispose();
                            globalAckEvent = null;

                            sessionAckEvent.Reset();
                            sessionAckEvent.Dispose();
                            sessionAckEvent = null;

                            globalReadyEvent.Reset();
                            globalReadyEvent.Dispose();
                            globalReadyEvent = null;

                            sessionReadyEvent.Reset();
                            sessionReadyEvent.Dispose();
                            sessionReadyEvent = null;

                            // Close SharedFile
                            if (sessionSharedFileHandle != IntPtr.Zero) {
                                if (!Kernel32.CloseHandle(sessionSharedFileHandle)) {
                                    throw new ApplicationException(
                                        string.Format("{0}. Last Win32 Error was {1}", "Failed to close handle for 'SharedFile'", Marshal.GetLastWin32Error()));
                                }
                                sessionSharedFileHandle = IntPtr.Zero;
                            }

                            // Unmap SharedMem
                            if (sessionSharedMemoryHandle != IntPtr.Zero) {
                                if (!Kernel32.UnmapViewOfFile(sessionSharedMemoryHandle)) {
                                    throw new ApplicationException(
                                        string.Format(
                                            "{0}. Last Win32 Error was {1}", "Failed to unmap view for slot 'DBWIN_BUFFER'", Marshal.GetLastWin32Error()));
                                }
                                sessionSharedMemoryHandle = IntPtr.Zero;
                            }

                            // Close SharedFile
                            if (globalSharedFileHandle != IntPtr.Zero) {
                                if (!Kernel32.CloseHandle(globalSharedFileHandle)) {
                                    throw new ApplicationException(
                                        string.Format("{0}. Last Win32 Error was {1}", "Failed to close handle for 'SharedFile'", Marshal.GetLastWin32Error()));
                                }
                                globalSharedFileHandle = IntPtr.Zero;
                            }

                            // Unmap SharedMem
                            if (globalSharedMemoryHandle != IntPtr.Zero) {
                                if (!Kernel32.UnmapViewOfFile(globalSharedMemoryHandle)) {
                                    throw new ApplicationException(
                                        string.Format(
                                            "{0}. Last Win32 Error was {1}", "Failed to unmap view for slot 'Global\\DBWIN_BUFFER'", Marshal.GetLastWin32Error()));
                                }
                                globalSharedMemoryHandle = IntPtr.Zero;
                            }
                        }, _cancellationTokenSource.Token);

                    Task.Factory.StartNew(() => {
                        // handle events on seperate task to minimize the work that is done blocking the handles
                        Tuple<int, DateTime, string> item = null;

                        while (WaitHandle.WaitAny(new[] {dataAvailable, _cancellationTokenSource.Token.WaitHandle}) == 0) {
                            lock (queue) {
                                if (queue.Count > 0) {
                                    item = queue.Dequeue();
                                }
                                if (queue.Count == 0) {
                                    dataAvailable.Reset();
                                }
                            }

                            if (item == null || OnOutputDebugString == null) {
                                continue;
                            }

                            try {
                                OnOutputDebugString(new OutputDebugStringEventArgs(item.Item1, item.Item2, item.Item3));
                            } catch {
                                // if it's taken, good, if not--well too bad!
                            }
                        }
                    }, _cancellationTokenSource.Token);
                }
            }
        }

        public static void Stop() {
            _cancellationTokenSource.Cancel();
        }
    }
}