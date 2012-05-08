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
    using CoApp.Toolkit.Extensions;

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
}