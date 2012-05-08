﻿//-----------------------------------------------------------------------
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
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using CoApp.Toolkit.Collections;
    using CoApp.Toolkit.Extensions;

    public class PropertySheet : DynamicObject {
        private static readonly Regex Macro = new Regex(@"(\$\{(.*?)\})");
        private readonly List<Rule> _rules = new List<Rule>();
        private readonly IDictionary<string, PropertySheet> _importedSheets = new XDictionary<string, PropertySheet>();

        public delegate IEnumerable<object> GetCollectionDelegate(string collectionName);

        public StringExtensions.GetMacroValueDelegate GetMacroValue;
        public StringExtensions.GetMacroValueDelegate PreprocessProperty;
        public StringExtensions.GetMacroValueDelegate PostprocessProperty;

        public GetCollectionDelegate GetCollection;

        public string Filename { get; internal set; }

        public IDictionary<string, PropertySheet> ImportedSheets {
            get {
                return _importedSheets;
            }
        } 

        public bool HasRules {
            get {
                return Rules.Any();
            }
        }

        public bool HasRule(string name = "*", string parameter = null, string @class = null, string id = null) {
            return (from rule in Rules
                    where rule.Name == name &&
                        (string.IsNullOrEmpty(parameter) ? null : parameter) == rule.Parameter &&
                            (string.IsNullOrEmpty(@class) ? null : @class) == rule.Class &&
                                (string.IsNullOrEmpty(id) ? null : id) == rule.Id
                    select rule).Any();
        }

        public IEnumerable<Rule> Rules {
            get {
                return _importedSheets.Values.SelectMany(each => each.Rules).Union(_rules);
            }
        }

        public virtual IEnumerable<string> FullSelectors {
            get {
                return Rules.Select(each => each.FullSelector);
            }
        }

        public IEnumerable<Rule> this[string name] {
            get { return from r in Rules where r.Name == name select r; }
        }

        public static PropertySheet Parse(string text, string originalFilename) {
            return PropertySheetParser.Parse(text, originalFilename);
        }

        public static PropertySheet Load(string path) {
            var result = Parse(File.ReadAllText(path),path);
            result.Filename = path;
            return result;
        }

        public virtual void Save(string path) {
            File.WriteAllText(path, ToString());
        }

        internal string ResolveMacros(string value, object eachItem = null) {
            if( PreprocessProperty != null) {
                foreach (StringExtensions.GetMacroValueDelegate preprocess in PreprocessProperty.GetInvocationList()) {
                    value = preprocess(value);
                }
            }
            
            if (GetMacroValue != null) {
                value = ProcessMacroInternal(value, eachItem);    
            }

            if (PostprocessProperty != null) {
                foreach (StringExtensions.GetMacroValueDelegate postprocess in PostprocessProperty.GetInvocationList()) {
                    value = postprocess(value);
                }
            }

            return value;
        }

        private string ProcessMacroInternal(string value, object eachItem) {
            bool keepGoing;
            if (value == null) {
                return null;
            }

            do {
                
                keepGoing = false;

                var matches = Macro.Matches(value);
                foreach (var m in matches) {
                    var match = m as Match;
                    var innerMacro = match.Groups[2].Value;
                    var outerMacro = match.Groups[1].Value;
                    // var replacement = GetMacroValue(innerMacro);
                    string replacement = null;

                    // get the first responder.
                    foreach (StringExtensions.GetMacroValueDelegate del in GetMacroValue.GetInvocationList()) {
                        replacement = del(innerMacro);
                        if (replacement != null) {
                            break;
                        }
                    }

                    if (eachItem != null) {
                        // try resolving it as an ${each.property} style.
                        // the element at the front is the 'this' value
                        // just trim off whatever is at the front up to and including the first dot.
                        try {
                            if( innerMacro.Equals("each", StringComparison.CurrentCultureIgnoreCase) ) {
                                value = value.Replace(outerMacro, eachItem.ToString());
                                keepGoing = true;
                            } else 
                            if (innerMacro.Contains(".")) {
                                innerMacro = innerMacro.Substring(innerMacro.IndexOf('.') + 1).Trim();
                                var v = eachItem.SimpleEval(innerMacro);
                                if (v != null) {
                                    var r = v.ToString();
                                    value = value.Replace(outerMacro, r);
                                    keepGoing = true;
                                }
                            }
                        }
                        catch {
                            // meh. screw em'
                        }
                    }

                    if (replacement != null) {
                        value = value.Replace(outerMacro, replacement);
                        keepGoing = true;
                        break;
                    }
                }
            } while (keepGoing);
            return value;
        }

        public override string ToString() {
            var imports = _importedSheets.Keys.Aggregate((current, each) => current + "@import {0};\r\n".format(QuoteIfNeeded(each)));
            return _rules.Aggregate(imports, (current, each) => current + each.SourceString);
        }

        /// <summary>
        /// Gets a rule by the selector criteria. If the rule doesn't exists, it creates it and adds it to the propertysheet.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parameter"></param>
        /// <param name="class"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public Rule GetRule( string name = "*" , string parameter = null, string @class = null , string id = null ) {
            var r = (from rule in Rules
            where rule.Name == name &&
                (string.IsNullOrEmpty(parameter) ? null : parameter) == rule.Parameter &&
                    (string.IsNullOrEmpty(@class) ? null : @class) == rule.Class &&
                        (string.IsNullOrEmpty(id) ? null : id) == rule.Id
            select rule).FirstOrDefault();

            if( r == null ) {
                _rules.Add(r = new Rule(this) {
                    Name = name,
                    Parameter = parameter,
                    Class = @class,
                    Id = id,
                });
            }
            
            return r;
        }

        public bool PreferDashedNames { get; set; }

        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            // we have to also potentially translate fooBar into foo-bar so we can test for 
            // dashed-names properly.
            var alternateName = binder.Name.CamelCaseToDashed();

            var val = (from rule in Rules where rule.Name == binder.Name || rule.Name == alternateName select rule).ToArray();

            switch (val.Length) {
                case 0:
                    result = GetRule(PreferDashedNames ? alternateName :binder.Name); // we'll implicity add one by this name.
                    break;
                case 1:
                    result = val[0]; // will return the single item *as* a single item.
                    break;
                default:
                    result = val; // will return the collection instead 
                    break;
            }
            return true;
        }

        internal static string QuoteIfNeeded(string val) {
            if (val.OnlyContains(StringExtensions.LettersNumbersUnderscoresAndDashesAndDots)) {
                return val;
            }

            return val.Contains("\r") || val.Contains("\n") || val.Contains("=") || val.Contains("\t")
                ? @"@""{0}""".format(val)
                : @"""{0}""".format(val);
        }
    }
}
