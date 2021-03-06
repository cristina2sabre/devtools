﻿//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     ResourceLib Original Code from http://resourcelib.codeplex.com
//     Original Copyright (c) 2008-2009 Vestris Inc.
//     Changes Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
// MIT License
// You may freely use and distribute this software under the terms of the following license agreement.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
// the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE
// </license>
//-----------------------------------------------------------------------

namespace CoApp.Developer.Toolkit.ResourceLib {
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using CoApp.Toolkit.Win32;

    /// <summary>
    ///   A base menu template item.
    /// </summary>
    public abstract class MenuExTemplateItem {
        /// <summary>
        ///   Menu item header.
        /// </summary>
        protected MenuExItemTemplate _header;

        /// <summary>
        ///   Menu string.
        /// </summary>
        protected string _menuString;

        /// <summary>
        ///   Menu text.
        /// </summary>
        public string MenuString {
            get { return _menuString; }
            set { _menuString = value; }
        }

        /// <summary>
        ///   Read the menu item.
        /// </summary>
        /// <param name = "lpRes">Address in memory.</param>
        /// <returns>End of the menu item structure.</returns>
        internal virtual IntPtr Read(IntPtr lpRes) {
            _header = (MenuExItemTemplate) Marshal.PtrToStructure(lpRes, typeof (MenuExItemTemplate));

            lpRes = new IntPtr(lpRes.ToInt32() + Marshal.SizeOf(_header));

            switch ((UInt32) Marshal.ReadInt32(lpRes)) {
                case 0:
                    break;
                default:
                    _menuString = Marshal.PtrToStringUni(lpRes);
                    lpRes = new IntPtr(lpRes.ToInt32() + (_menuString.Length + 1)*Marshal.SystemDefaultCharSize);
                    break;
            }

            return lpRes;
        }

        /// <summary>
        ///   Write the menu item to a binary stream.
        /// </summary>
        /// <param name = "w">Binary stream.</param>
        internal virtual void Write(BinaryWriter w) {
            // header
            w.Write(_header.dwType);
            w.Write(_header.dwState);
            w.Write(_header.dwMenuId);
            w.Write(_header.bResInfo);
            // menu string
            if (_menuString != null) {
                w.Write(Encoding.Unicode.GetBytes(_menuString));
                w.Write((UInt16) 0);
                ResourceUtil.PadToDWORD(w);
            }
        }

        /// <summary>
        ///   String representation in the MENU format.
        /// </summary>
        /// <param name = "indent">Indent.</param>
        /// <returns>String representation.</returns>
        public abstract string ToString(int indent);

        /// <summary>
        ///   String representation in the MENU format.
        /// </summary>
        /// <returns>String representation.</returns>
        public override string ToString() {
            return ToString(0);
        }
    }
}