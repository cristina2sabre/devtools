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


namespace CoApp.Developer.Toolkit.Scripting.Utility {
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using CoApp.Toolkit.Collections;
    using CoApp.Toolkit.Exceptions;
    using EXCEPINFO = System.Runtime.InteropServices.ComTypes.EXCEPINFO;

    #region Enums

    #endregion

    #region ActiveScripting

    #endregion

    [ComVisible(true)]
    public class ActiveScriptHost : IActiveScriptSite, IDisposable {
        private object scriptEngine;
        private int returnValue;
        private readonly ScriptLanguage scriptLanguage;
        private readonly IDictionary<string, object> visibleGlobalMembers = new XDictionary<string, object>();

        private IActiveScript ActiveScript {
            get { return (scriptEngine as IActiveScript); }
        }
        private IActiveScriptParse ActiveScriptParse {
            get { return (scriptEngine as IActiveScriptParse); }
        }

        public ActiveScriptHost(ScriptLanguage language) {
            scriptLanguage = language;
        }

        public int ReturnValue {
            get { return returnValue; }
        }

        public void Dispose() {
            Close();
        }

        public void Close() {
            if(null != scriptEngine) {
                ActiveScript.SetScriptState((uint) ScriptState.Disconnected);
                ActiveScript.Close();
            }
            scriptEngine = null;
        }

        #region WScript style methods

        // ReSharper disable InconsistentNaming

        public void Quit(int i) {
            returnValue = i;
            ActiveScript.SetScriptState((uint) ScriptState.Disconnected);
        }

        public string ScriptFullName() {
            return "scriptfullname";
        }

        public string ScriptName() {
            return "scriptname";
        }

        public string FullName() {
            return "fullanme";
        }

        public void echo(string text) {
            Console.WriteLine(text);
        }

        // ReSharper restore InconsistentNaming

        #endregion

        public IDictionary<string, object> GlobalMembers {
            get { return visibleGlobalMembers; }
        }

        public string ScriptText { get; set; }

        public void GetItemInfo([In, MarshalAs(UnmanagedType.BStr)] string pstrName, [In, MarshalAs(UnmanagedType.U4)] uint dwReturnMask, [Out, MarshalAs(UnmanagedType.IUnknown)] out object item, IntPtr ppti) {
            if(GlobalMembers.ContainsKey(pstrName)) {
                item = GlobalMembers[pstrName];
            }
            else {
                item = null;
                return;
            }

            if(ppti != IntPtr.Zero) {
                Marshal.WriteIntPtr(ppti, Marshal.GetITypeInfoForType(item.GetType()));
            }
        }

        public void OnScriptError(IActiveScriptError err) {
            EXCEPINFO excepinfo;
            int ctx, line, col;
            err.GetSourcePosition(out ctx, out line, out col);
            err.GetExceptionInfo(out excepinfo);
            if(excepinfo.bstrSource.Equals("ScriptControl")) {
                return;
            }

            Console.WriteLine("Script Error ({0},{1}) {2}", line, col, excepinfo.bstrDescription);
        }

        public void Run() {
            try {
                switch(scriptLanguage) {
                    case ScriptLanguage.JScript:
                        scriptEngine = new JScript();
                        break;
                    case ScriptLanguage.VBScript:
                        scriptEngine = new VBScript();
                        break;
                    default:
                        throw new CoAppException("Invalid Script Language");
                }
                EXCEPINFO info;
                ActiveScriptParse.InitNew();
                ActiveScript.SetScriptSite(this);

                // add this object in 
                GlobalMembers.Add("WScript", this);

                foreach(string key in GlobalMembers.Keys)
                    ActiveScript.AddNamedItem(key, ScriptItem.IsVisible | ScriptItem.GlobalMembers);

                ActiveScriptParse.ParseScriptText(ScriptText, null, IntPtr.Zero, null, 0, 0, 0, IntPtr.Zero, out info);
                ActiveScript.SetScriptState((uint) ScriptState.Connected);
            }
            catch(Exception e) {
                Console.WriteLine(e.Message);
            }
        }

        public void Invoke(string functionName, params object[] args) {
            object dispatch;
            ActiveScript.GetScriptDispatch(null, out dispatch);
            var t = dispatch.GetType();
            t.InvokeMember(functionName, BindingFlags.InvokeMethod, null, dispatch, args);
        }

        public void GetLCID(out uint id) {
            id = 0x80004001;
        }

        public void OnEnterScript() {
        }

        public void OnLeaveScript() {
        }

        public void OnScriptTerminate(ref object result, ref EXCEPINFO info) {
        }

        public void OnStateChange(uint state) {
        }

        public void GetDocVersionString(out string v) {
            v = "ScriptHost Version 1";
        }
    }
}