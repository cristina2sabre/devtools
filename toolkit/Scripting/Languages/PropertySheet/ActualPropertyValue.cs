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


namespace CoApp.Developer.Toolkit.Scripting.Languages.PropertySheet {
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public class ActualPropertyValue : IPropertyValue {
        private IEnumerable<string> _values;

        internal IEnumerable<string> Values {
            get {
                return _values.Select(each => ParentPropertySheet.ResolveMacros(each));
            }
            set {
                _values = value;
            }
        }

        public SourceLocation SourceLocation { get; internal set; }

        private string _value;

        public string Value {
            get {
                return ParentPropertySheet.ResolveMacros(_value);
            }
            internal set {
                _value = value;
            }
        }

        public bool IsSingleValue { get; internal set; }
        public bool HasMultipleValues { get; internal set; }
        public string SourceString { get; internal set; }

        public IEnumerator<string> GetEnumerator() {
            return Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        internal Rule ParentRule { get; set; }
        // internal PropertySheet ParentPropertySheet { get; set; }
        internal PropertySheet ParentPropertySheet {
            get {
                return ParentRule.ParentPropertySheet;
            }
        }
    }
}