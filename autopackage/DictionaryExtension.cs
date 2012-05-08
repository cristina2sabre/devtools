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
namespace CoApp.Autopackage {
    using System.Collections.Generic;

    public static class DictionaryExtension {
        public static TValue AddOrSet<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value) where TValue : class {
            if( dictionary.ContainsKey(key) ) {
                dictionary[key] = value;
            } else {
                dictionary.Add(key, value);
            }
            return value;
        }
    }
}