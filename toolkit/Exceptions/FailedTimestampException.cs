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


namespace CoApp.Developer.Toolkit.Exceptions {
    using CoApp.Toolkit.Exceptions;
    using CoApp.Toolkit.Extensions;

    public class FailedTimestampException : CoAppException {
        public FailedTimestampException(string filename, string timestampurl)
            : base("Failed to get timestamp for '{0}' from '{1}'".format(filename, timestampurl)) {
        }
    }
}

