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
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using CoApp.Toolkit.Collections;
    using Collections;

    public class Indexer<T> : IEnumerable<T> where T : class {
        private readonly Func<IEnumerable<string>> _keysFn;
        private readonly Func<string, IEnumerable<Rule>> _lookupFn;
        private readonly Func<string, Rule> _newRuleFn;
        private readonly IDictionary<string, T> _cache = new XDictionary<string, T>();

        // this returns a new referecnce to the rule wrapper in the property sheet each time
        // this is why you shouldn't store data in the wrapper you dumbass.
        public T this[string index] {
            get {
                if( _cache.ContainsKey(index)) {
                    return _cache[index];
                }
                var result = (T)Activator.CreateInstance(typeof(T),  _lookupFn(index).FirstOrDefault() ?? _newRuleFn(index));
                _cache.Add(index, result);
                return result;
            }
        }

        public IEnumerable<string> Keys {
            get {
                return _keysFn();
            }
        }

        public Indexer(Func<IEnumerable<string>> keysFn, Func<string, IEnumerable<Rule>> lookupFn, Func<string, Rule> newRuleFn ) {
            _keysFn = keysFn;
            _lookupFn = lookupFn;
            _newRuleFn = newRuleFn;
        }

        public IEnumerator<T> GetEnumerator() {
            return new VirtualEnumerator<T>(Keys.GetEnumerator(), enumerator => this[(string)enumerator.Current]);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return new VirtualEnumerator<T>(Keys.GetEnumerator(), enumerator => this[(string)enumerator.Current]);
        }
    }
}