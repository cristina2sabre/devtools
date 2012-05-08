namespace CoApp.Toolkit.Text.Sgml {
    using System;

    internal static class StringUtilities {
        public static bool EqualsIgnoreCase(string a, string b) {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }
}