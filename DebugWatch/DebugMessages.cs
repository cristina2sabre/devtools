using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoApp.DebugWatch {
    using System.Collections.ObjectModel;
    using System.Windows.Threading;
    using Developer.Toolkit.Debugging;
    using Toolkit.Extensions;

    public class Message {
        public string FromProcStart { get; set; }
        public string FromFirstEvent { get; set; }
        public string Process { get; set; }
        public string Text { get; set; }
    }

    public class DebugMessages : ObservableCollection<Message> , IDisposable {
        private readonly Dispatcher _currentDispatcher;

        public DebugMessages() {
            Monitor.OnOutputDebugString += MonitorOnOnOutputDebugString;
            _currentDispatcher = Dispatcher.CurrentDispatcher;
        }

        private void Dispatch(Action action) {
            if (_currentDispatcher.CheckAccess())
                action.Invoke();
            else
                _currentDispatcher.Invoke(DispatcherPriority.DataBind, action);
        }

        private void MonitorOnOnOutputDebugString(OutputDebugStringEventArgs args) {
            // poor mans filtering here.
            if( args.Process.ProcessName.IndexOf("coapp",StringComparison.CurrentCultureIgnoreCase) > -1 ) {
                
                if(args.Message.IndexOf("berevity", StringComparison.CurrentCultureIgnoreCase) > -1 ) {
                    // skip wrapped messages
                    return;
                }



                var msg = new Message { Process = "{0}({1})".format(args.Process.ProcessName, args.Process.Id), Text = args.Message, FromProcStart = args.SinceProcessStarted.AsDebugOffsetString(), FromFirstEvent = args.SinceFirstEvent.AsDebugOffsetString() };
                Dispatch(() => Add(msg));
                
            }
        }

        public void Dispose() {
            Monitor.OnOutputDebugString -= MonitorOnOnOutputDebugString;
        }
    }
}
