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
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using CoApp.Toolkit.Extensions;

    /// <summary>
    ///   A RuleProperty represents a single property name, with potentially multiple property-labels, each label can have 1 or more values.
    /// </summary>
    public class PropertyRule : DynamicObject {
        internal readonly Rule ParentRule;
        private readonly List<PropertyValue> _propertyValues = new List<PropertyValue>();
        public SourceLocation SourceLocation { get; internal set; }
        public string Name { get; set; }

        /// <summary>
        ///   RuleProperty object must be created by the Rule.
        /// </summary>
        /// <param name="parent"> </param>
        /// <param name="name"> </param>
        internal PropertyRule(Rule parent, string name) {
            ParentRule = parent;
            Name = name;
        }

        internal string SourceString {
            get {
                return _propertyValues.Aggregate("", (current, v) => current + "    {0} : {1}".format(Name, v.SourceString));
            }
        }

        public override string ToString() {
            var items = Labels.Select(each => new {label = each, values = this[each] ?? Enumerable.Empty<string>()});
            var result = items.Where(item => item.values.Any()).Aggregate("", (current1, item) => current1 + (item.values.Count() == 1
                ? PropertySheet.QuoteIfNeeded(Name) + PropertySheet.QuoteIfNeeded(item.label) + " = " + PropertySheet.QuoteIfNeeded(item.values.First()) + ";\r\n"
                : PropertySheet.QuoteIfNeeded(Name) + PropertySheet.QuoteIfNeeded(item.label) + " = {\r\n" + item.values.Aggregate("", (current, each) => current + "        " + PropertySheet.QuoteIfNeeded(each) + ",\r\n") + "    };\r\n"));

            return result;
        }

        public string Value {
            get {
                var v = this[string.Empty];
                return v == null ? null : this[string.Empty].Value;
            }
        }

        public IEnumerable<string> Values {
            get {
                return this[string.Empty] ?? Enumerable.Empty<string>();
            }
        }

        public IEnumerable<string> Labels {
            get {
                return _propertyValues.SelectMany(each => each.Labels).Distinct();
            }
        }

        public bool HasValues {
            get {
                return _propertyValues.Count > 0;
            }
        }

        public bool HasValue {
            get {
                return _propertyValues.Count > 0;
            }
        }

        public IPropertyValue this[string label] {
            get {
                // looks up the property collection
                return (from propertyValue in _propertyValues let actual = propertyValue.Actual(label) where actual != null select actual).FirstOrDefault();
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            var primary = ParentRule.ParentPropertySheet.PreferDashedNames ? binder.Name.CamelCaseToDashed() : binder.Name;
            var secondary = ParentRule.ParentPropertySheet.PreferDashedNames ? binder.Name : binder.Name.CamelCaseToDashed();

            result = GetPropertyValue(!_propertyValues.Where(each => each.Label == primary).Any() && _propertyValues.Where(each => each.Label == secondary).Any() ? secondary : primary);
            return true;
        }

        /// <summary>
        ///   Gets Or Adds a PropertyValue with the given label and collection.
        /// </summary>
        /// <param name="label"> </param>
        /// <param name="collection"> </param>
        /// <returns> </returns>
        internal PropertyValue GetPropertyValue(string label, string collection = null) {
            var result = _propertyValues.Where(each => each.Label == label).FirstOrDefault();
            if (result == null) {
                _propertyValues.Add(result = new PropertyValue(this, label, string.IsNullOrEmpty(collection) ? null : collection));
            }
            return result;
        }
    }
}