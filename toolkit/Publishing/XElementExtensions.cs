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

namespace CoApp.Developer.Toolkit.Publishing {
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    public static class XElementExtensions {
        public static bool Contains(this XElement element, string childElementName) {
            return element.Elements().Any(each => each.Name == childElementName);
        }

        public static IEnumerable<XElement> Children(this XElement element, string childElementName, params string[] childElementNames) {
            if (childElementNames == null || childElementNames.Length == 0) {
                return element.Elements().Where(each => each.Name == childElementName);
            }
            var result = element.Elements().FirstOrDefault(each => each.Name == childElementName);
            return result != null ? result.Children(childElementNames[0], childElementNames.Skip(1).ToArray()) : Enumerable.Empty<XElement>();
        }

        public static XElement AddOrGet(this XElement element, string childElementName) {
            var result = element.Children(childElementName).FirstOrDefault();
            if (result == null) {
                result = new XElement(childElementName);
                element.Add(result);
            }
            return result;
        }

        public static string AttributeValue(this XElement element, string attributeName) {
            if (element == null) {
                return null;
            }

            var attr = element.Attributes().FirstOrDefault(each => each.Name == attributeName);
            return attr != null ? attr.Value : null;
        }
    }
}