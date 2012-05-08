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
    using CoApp.Developer.Toolkit.Scripting.Languages.PropertySheet;
    using CoApp.Toolkit.Exceptions;
    using CoApp.Toolkit.Extensions;

    public class EndUserRuleException : CoAppException {
        public Rule Rule;

        public EndUserRuleException(Rule rule, string errorcode, string message, params object[] parameters)
            : base("{0}({1},{2}):{3}:{4}".format(rule.SourceLocation.SourceFile, rule.SourceLocation.Row, rule.SourceLocation.Column, errorcode, message.format(parameters))) {
            Rule = rule;
        }
    }
}