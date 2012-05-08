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

namespace CoApp.RepositoryService {
    using System.Net;
    using System.Threading.Tasks;
    using Toolkit.Pipes;

    public class RequestHandler {
        public virtual Task Put(HttpListenerResponse response, string relativePath, byte[] data) {
            return null;
        }

        public virtual Task Get(HttpListenerResponse response, string relativePath, UrlEncodedMessage message) {
            return null;
        }

        public virtual Task Post(HttpListenerResponse response, string relativePath, UrlEncodedMessage message) {
            return null;
        }

        public virtual Task Head(HttpListenerResponse response, string relativePath, UrlEncodedMessage message) {
            return null;
        }
    }
}