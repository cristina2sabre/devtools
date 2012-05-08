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

    public class EndUserException : CoAppException {
        public EndUserException(string SourceFile, int SourceRow, int SourceColumn, string errorcode, string message, params object[] parameters)
            : base("{0}({1},{2}):{3}:{4}".format(SourceFile, SourceRow, SourceColumn, errorcode, message.format(parameters))) {
        }

        public EndUserException(string errorcode, string message, params object[] parameters)
            : base(" :{0}:{1}".format(errorcode, message.format(parameters))) {
        }
    }
}