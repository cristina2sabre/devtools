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
    using Scripting.Languages.PropertySheet;

    public class EndUserPropertyException : CoAppException {
        public PropertyRule Property;

        public EndUserPropertyException(PropertyRule property, string errorcode, string message, params object[] parameters)
            : base("{0}({1},{2}):{3}:{4}".format(property.SourceLocation.SourceFile, property.SourceLocation.Row, property.SourceLocation.Column, errorcode, message.format(parameters))) {
            Property = property;
        }
    }
}