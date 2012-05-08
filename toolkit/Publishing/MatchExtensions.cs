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

namespace CoApp.Developer.Toolkit.Publishing {
    using System.Text.RegularExpressions;

    internal static class MatchExtensions {
        internal static string Value(this Match match, string group, string _default = null) {
            return (match.Groups[group].Success ? match.Groups[group].Captures[0].Value : _default ?? string.Empty).Trim('-', ' ');
        }
    }
}