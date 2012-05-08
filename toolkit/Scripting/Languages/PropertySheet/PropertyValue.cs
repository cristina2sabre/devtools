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
    using CoApp.Toolkit.Extensions;

    public class PropertyValue : IPropertyValue {
        private static readonly IEnumerable<string> NoCollection = "".SingleItemAsEnumerable();
        internal readonly string _collectionName;

        internal readonly PropertyRule ParentPropertyRule;
        private readonly List<string> _values = new List<string>();

        public SourceLocation SourceLocation { get; internal set; }
        internal string Label { get; private set; }

        internal PropertyValue(PropertyRule parent, string label, string collectionName = null) {
            ParentPropertyRule = parent;
            Label = label;
            _collectionName = collectionName;
        }

        internal IPropertyValue Actual(string label) {
            if (string.IsNullOrEmpty(_collectionName)) {
                return Label == label ? this : null; // this should shortcut nicely when there is no collection.
            }

            var values = CollectionValues.Where(each => ParentPropertySheet.ResolveMacros(Label, each) == label).SelectMany(each => _values.Select(value => ParentPropertySheet.ResolveMacros(value, each))).ToArray();
            if (values.Length > 0) {
                return new ActualPropertyValue {
                    SourceLocation = SourceLocation,
                    IsSingleValue = values.Length == 1,
                    HasMultipleValues = values.Length > 1,
                    SourceString = "",
                    Value = values[0],
                    Values = values,
                    ParentRule = ParentPropertyRule.ParentRule
                };
            }

            return null;
        }

        private PropertySheet ParentPropertySheet {
            get {
                return ParentPropertyRule.ParentRule.ParentPropertySheet;
            }
        }

        private IEnumerable<object> CollectionValues {
            get {
                return string.IsNullOrEmpty(_collectionName)
                    ? NoCollection // this makes it so there is a 1-element collection for things that don't have a collection. 
                    : (ParentPropertySheet.GetCollection != null
                        ? ParentPropertySheet.GetCollection(_collectionName)
                        : Enumerable.Empty<object>()); // this is so that when there is supposed to be a collection, but nobody is listening, we get an empty set back.
            }
        }

        public IEnumerator<string> GetEnumerator() {
            return CollectionValues.SelectMany(each => _values.Select(value => ParentPropertySheet.ResolveMacros(value, each))).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public string Value {
            get {
                return ParentPropertySheet.ResolveMacros(this.FirstOrDefault());
            }
            set {
                _values.Clear();
                _values.Add(value);
            }
        }

        public IEnumerable<string> Values {
            get {
                return this.Select(each => ParentPropertySheet.ResolveMacros(each));
            }
        }

        public int Count {
            get {
                return (CollectionValues.Count()*_values.Count);
            }
        }

        public bool IsSingleValue {
            get {
                return Count == 1;
            }
        }

        public bool HasMultipleValues {
            get {
                return Count > 1;
            }
        }

        internal void Add(string value) {
            _values.Add(value);
        }

        public string SourceString {
            get {
                if (string.IsNullOrEmpty(_collectionName)) {
                    if (string.IsNullOrEmpty(Label)) {
                        if (_values.Count == 1) {
                            return PropertySheet.QuoteIfNeeded(_values[0]) + ";\r\n";
                        }
                        if (_values.Count > 1) {
                            return _values.Aggregate("{", (current, v) => current + "\r\n        " + PropertySheet.QuoteIfNeeded(v) + ",") + "\r\n    };\r\n\r\n";
                        }
                        if (_values.Count == 0) {
                            return @"""""; // WARNING--THIS SHOULD NOT BE HAPPENING. EMPTY VALUE LISTS ARE SIGN THAT YOU HAVE NOT PAID ENOUGH ATTENTION";
                        }
                    }
                    if (_values.Count == 1) {
                        return "{0} = {1};\r\n".format(PropertySheet.QuoteIfNeeded(Label), PropertySheet.QuoteIfNeeded(_values[0]));
                    }
                    if (_values.Count > 1) {
                        return "{0} = {1}".format(PropertySheet.QuoteIfNeeded(Label), _values.Aggregate("{", (current, v) => current + "\r\n        " + PropertySheet.QuoteIfNeeded(v) + ",") + "\r\n    };\r\n\r\n");
                    }
                    if (_values.Count == 0) {
                        return @"{0} = """"; // WARNING--THIS SHOULD NOT BE HAPPENING. EMPTY VALUE LISTS ARE SIGN THAT YOU HAVE NOT PAID ENOUGH ATTENTION".format(PropertySheet.QuoteIfNeeded(Label));
                    }
                }
                if (string.IsNullOrEmpty(Label)) {
                    return _values.Aggregate("{", (current, v) => current + ("\r\n        " + PropertySheet.QuoteIfNeeded(_collectionName) + " => " + PropertySheet.QuoteIfNeeded(v) + ";")) + "\r\n    };\r\n\r\n";
                }
                return _values.Aggregate("{", (current, v) => current + ("\r\n        " + PropertySheet.QuoteIfNeeded(_collectionName) + " => " + PropertySheet.QuoteIfNeeded(Label) + " = " + PropertySheet.QuoteIfNeeded(v) + ";")) + "\r\n    };\r\n\r\n";
            }
        }

        internal IEnumerable<string> Labels {
            get {
                return CollectionValues.Select(each => ParentPropertySheet.ResolveMacros(Label, each));
            }
        }
    }
}