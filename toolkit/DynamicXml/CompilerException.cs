/*
namespace CoApp.Toolkit.DynamicXml {
    using System;
    using System.CodeDom.Compiler;
    using System.Text;

    public class CompilerException : Exception {
        private CompilerResults _results;

        public CompilerException(CompilerResults results)
            :
                this(results, new MessageBuilder()) {
        }

        internal CompilerException(CompilerResults results, MessageBuilder messageBuilder)
            :
                base(messageBuilder.GetMessage(results)) {
            _results = results;
        }

        public CompilerResults CompilerResults {
            get { return _results; }
        }

        internal class MessageBuilder {
            public virtual string GetMessage(CompilerResults results) {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Compilation error(s):");
                foreach (CompilerError error in results.Errors) {
                    sb.AppendLine(GetMessageForError(error));
                }

                return sb.ToString();
            }

            internal virtual string GetMessageForError(CompilerError error) {
                return
                    String.Format("{0}({1},{2}): Error {3}: {4}",
                        error.FileName,
                        error.Line,
                        error.Column,
                        error.ErrorNumber,
                        error.ErrorText);
            }
        }
    }
}*/