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
    using CoApp.Toolkit.Extensions;

    public class TrustInfo : ManifestElement {
        private const string TrustInfoTag = "{urn:schemas-microsoft-com:asm.v3}trustInfo";
        private const string SecurityTag = "{urn:schemas-microsoft-com:asm.v3}security";
        private const string RequestedPrivilegesTag = "{urn:schemas-microsoft-com:asm.v3}requestedPrivileges";
        private const string RequestedExecutionLevelTag = "{urn:schemas-microsoft-com:asm.v3}requestedExecutionLevel";
        private const string LevelAttribute = "level";
        private const string UiAccessAttribute = "uiAccess";

        public TrustInfo(XElement parentElement) : base(parentElement) {
        }

        protected override IEnumerable<XElement> Elements {
            get { return _parentElement.Children(TrustInfoTag); }
        }

        /// <summary>
        ///   Ensures that the element contains at most, a single valid trustInfo block
        /// </summary>
        protected override void Validate() {
            RemoveExcessElements();

            if (!Active) {
                return;
            }

            // ensure that we have the right child elements
            var x = RequestedExecutionLevelElement;
        }

        private XElement RequestedExecutionLevelElement {
            get {
                if (!Active) {
                    return null;
                }

                return Elements.FirstOrDefault().AddOrGet(SecurityTag)
                    .AddOrGet(RequestedPrivilegesTag)
                    .AddOrGet(RequestedExecutionLevelTag);
            }
        }

        protected override bool Active {
            set {
                if (value) {
                    if (!Active) {
                        _parentElement.Add(new XElement(TrustInfoTag));
                        Validate();
                    }
                } else {
                    while (Elements.Any()) {
                        Elements.Remove();
                    }
                }
            }
        }

        public ExecutionLevel Level {
            get {
                if (!Active) {
                    return ExecutionLevel.none;
                }
                return RequestedExecutionLevelElement.AttributeValue(LevelAttribute).ParseEnum(ExecutionLevel.none);
            }
            set {
                if (value == ExecutionLevel.none) {
                    if (!UiAccess) {
                        Active = false;
                    } else {
                        RequestedExecutionLevelElement.SetAttributeValue(LevelAttribute, null);
                    }
                } else {
                    Active = true;
                    RequestedExecutionLevelElement.SetAttributeValue(LevelAttribute, value.CastToString());
                }
            }
        }

        public bool UiAccess {
            get {
                if (!Active) {
                    return false;
                }
                return RequestedExecutionLevelElement.AttributeValue(UiAccessAttribute).IsTrue();
            }
            set { RequestedExecutionLevelElement.SetAttributeValue(UiAccessAttribute, value?"true":"false"); }
        }
    }
}