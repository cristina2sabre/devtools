namespace CoApp.Toolkit.DynamicXml {
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    internal class DomainUtil : MarshalByRefObject {
        public IEnumerable<string> GetNamesOfLoadedAssemblies() {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                string location = GetSafeLocation(assembly);
                if (!String.IsNullOrEmpty(location)) {
                    yield return location;
                }
            }
        }

        private static string GetSafeLocation(Assembly assembly) {
            try {
                return assembly.Location;
            } catch (NotSupportedException) {
                // thrown for dynamic assemblies
                // unfortunately, I did not find a way to check whether the assembly is dynamic
                return null;
            }
        }
    }
}