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
    using System.Text;
    using CoApp.Toolkit.Extensions;

    public class Rule : DynamicObject {
        // this may get set if the parent is not the current doc.
        internal PropertySheet ParentPropertySheet;

        private readonly List<PropertyRule> _properties = new List<PropertyRule>();
        public string Class { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        private string _parameter;

        public string Parameter {
            get {
                return ParentPropertySheet.ResolveMacros(_parameter);
            }
            set {
                _parameter = value;
            }
        }

        public SourceLocation SourceLocation;

        /// <summary>
        ///   Rules must be created by the property sheet only.
        /// </summary>
        /// <param name="propertySheet"> </param>
        internal Rule(PropertySheet propertySheet) {
            Name = "*";
            ParentPropertySheet = propertySheet;
        }

        public PropertyRule this[string propertyName] {
            get {
                return _properties.Where(each => each.Name == propertyName).FirstOrDefault();
            }
        }

        public IEnumerable<string> PropertyNames {
            get {
                return _properties.Select(each => each.Name);
            }
        }

        public static string CreateSelectorString(string name, string parameter, string @class, string id) {
            var result = new StringBuilder();
            if (!string.IsNullOrEmpty(name) && !name.Equals("*")) {
                result.Append(name);
            }

            if (!string.IsNullOrEmpty(parameter)) {
                result.Append("[");
                result.Append(parameter);
                result.Append("]");
            }

            if (!string.IsNullOrEmpty(@class)) {
                result.Append(".");
                result.Append(@class);
            }

            if (!string.IsNullOrEmpty(id)) {
                result.Append("#");
                result.Append(id);
            }

            return result.ToString();
        }

        public string FullSelector {
            get {
                return CreateSelectorString(Name, _parameter, Class, Id);
            }
        }

        public override string ToString() {
            return HasProperties ? _properties.Aggregate(FullSelector + " {\r\n", (current, each) => current + (each.ToString())) + "};\r\n\r\n" : string.Empty;
        }

        public bool HasProperty(string propertyName) {
            return this[propertyName] != null;
        }

        public bool HasProperties {
            get {
                return _properties.Where(each => each.HasValues).Any();
            }
        }

        internal PropertyRule GetRuleProperty(string name) {
            var property = this[name];
            if (property == null) {
                property = new PropertyRule(this, name);
                _properties.Add(property);
            }
            return property;
        }

        public string SourceString {
            get {
                return HasProperties ? _properties.Aggregate(FullSelector + " {\r\n", (current, each) => current + (each.SourceString)) + "};\r\n\r\n" : string.Empty;
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            var primary = ParentPropertySheet.PreferDashedNames ? binder.Name.CamelCaseToDashed() : binder.Name;
            var secondary = ParentPropertySheet.PreferDashedNames ? binder.Name : binder.Name.CamelCaseToDashed();

            result = GetRuleProperty(this[primary] == null && this[secondary] != null ? secondary : primary);
            return true;
        }
    }
}