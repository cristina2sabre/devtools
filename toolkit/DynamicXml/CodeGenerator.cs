namespace CoApp.Toolkit.DynamicXml {
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal class CodeGenerator : MarshalByRefObject {
        public IEnumerable<string> GetSources(string codeTemplate, string expression, IEnumerable<string> namespaces, ICollection<string> definitions) {
            yield return GetCode(codeTemplate, expression, namespaces);

            foreach (string source in definitions) {
                yield return source;
            }
        }

        /* internal virtual for tests */
        internal virtual string GetCode(string codeTemplate, string expression, IEnumerable<string> namespaces) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using System;");
            foreach (string @namespace in namespaces) {
                sb.AppendFormat("using {0};\r\n", @namespace);
            }

            sb.Append(codeTemplate.Replace("?", expression));
            return sb.ToString();
        }
    }
}