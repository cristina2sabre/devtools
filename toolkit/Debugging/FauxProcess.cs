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
    using System.Collections.Generic;
    using System.Diagnostics;
    using CoApp.Toolkit.Collections;

    public class FauxProcess {
        private FauxProcess() {
        }

        public int Id { get; internal set; }
        public string ProcessName { get; private set; }
        public DateTime StartTime { get; private set; }
        public DateTime FirstMessageTime { get; private set; }

        private static readonly IDictionary<int, FauxProcess> ProcessCache = new XDictionary<int, FauxProcess>();

        internal static FauxProcess GetProcess(int pid, DateTime msgTime) {
            lock (ProcessCache) {
                FauxProcess result;

                if (ProcessCache.ContainsKey(pid)) {
                    result = ProcessCache[pid];
                    if (result.FirstMessageTime == DateTime.MinValue) {
                        result.FirstMessageTime = msgTime;
                    }
                    try {
                        var checkProc = Process.GetProcessById(pid);
                        if( checkProc.StartTime != result.StartTime ) {
                            // new proc with same id.
                            result = new FauxProcess {
                                ProcessName = checkProc.ProcessName,
                                StartTime = checkProc.StartTime,
                                FirstMessageTime = msgTime,
                                Id = pid
                            };
                            ProcessCache[pid] = result;
                        }
                    } catch {
                        
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
}