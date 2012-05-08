//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2010-2012 Garrett Serack and CoApp Contributors. 
//     Contributors can be discovered using the 'git log' command.
//     All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Developer.Toolkit.Debugging {
    using System;

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
}