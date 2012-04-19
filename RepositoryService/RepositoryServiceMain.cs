﻿//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack . All rights reserved.
// </copyright>
// <license>
//     The software is licensed under the Apache 2.0 License (the "License")
//     You may not use the software except in compliance with the License. 
// </license>
//-----------------------------------------------------------------------

namespace CoApp.RepositoryService {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Resources;
    using System.Threading;
    using Properties;
    using Toolkit.Configuration;
    using Toolkit.Console;
    using Toolkit.Engine;
    using Toolkit.Engine.Client;
    using Toolkit.Exceptions;
    using Toolkit.Extensions;
    using Toolkit.Logging;
    using Toolkit.Network;

    public class RepositoryServiceMain  : AsyncConsoleProgram {
        private static bool _verbose = false;
        internal static readonly RegistryView Settings = RegistryView.CoAppUser["RepositoryService"];

        protected override ResourceManager Res {
            get { return Resources.ResourceManager; }
        }

        private static int Main(string[] args) {
            return new RepositoryServiceMain().Startup(args);
        }

        protected override int Main(IEnumerable<string> args) {
            var hosts = new string[] { "*" };
            var ports = new int[] { 80 };
            var commitMessage = "trigger";
            var packageUpload = "upload";
            
            string localfeedLocation = null;
            string packageStoragePath = null;
            string packagePrefixUrl = null;
            string canonicalFeedUrl = null;

            string tweetCommits = Settings["#tweet-commits"].StringValue;
            string tweetPackages = Settings["#tweet-packages"].StringValue;
            string azureAccount = Settings["#azure-account"].StringValue;

            string azureKey = null;

            var options = args.Where(each => each.StartsWith("--")).Switches();
            var parameters = args.Where(each => !each.StartsWith("--")).Parameters();
            var aliases = new Dictionary<string, string>();

            foreach (var arg in options.Keys) {
                var argumentParameters = options[arg];
                var last = argumentParameters.LastOrDefault();
                var lastAsBool = string.IsNullOrEmpty(last) || last.IsTrue();

                switch (arg) {
                        /* options  */
                    case "verbose":
                        _verbose = lastAsBool;
                        Logger.Errors = true;
                        Logger.Messages = true;
                        Logger.Warnings = true;
                        break;

                        /* global switches */
                    case "load-config":
                        // all ready done, but don't get too picky.
                        break;

                    case "nologo":
                        this.Assembly().SetLogo(string.Empty);
                        break;

                    case "help":
                        return Help();

                    case "feed-path":
                        localfeedLocation = last;
                        break;

                    case "feed-url":
                        canonicalFeedUrl = last;
                        break;

                    case "package-path":
                        packageStoragePath = last;
                        break;

                    case "package-prefix":
                        packagePrefixUrl = last;
                        break;

                    case "host":
                        hosts = argumentParameters.ToArray();
                        break;

                    case "port":
                        ports = argumentParameters.Select(each => each.ToInt32()).ToArray();
                        break;
                    
                    case "package-upload":
                        packageUpload = last;
                        break;

                    case "commit-message":
                        commitMessage = last;
                        break;

                    case "tweet-commits":
                        Settings["#tweet-commits"].StringValue = tweetCommits = last;
                        break;

                    case "tweet-packages":
                        Settings["#tweet-commits"].StringValue = tweetPackages = last;
                        break;

                    case "azure-name":
                        Settings["#azure-account"].StringValue = azureAccount = last;
                        break;

                    case "azure-key":
                        azureKey = last;
                        break;
                }
                if( arg.StartsWith("alias-")) {
                    aliases.Add(arg.Substring(6), last);
                }
            }

            Tweeter.Init(Settings, options);
            Bitly.Init(Settings, options);
            CloudFileSystem cfs = null; 

            if( !string.IsNullOrEmpty(azureAccount)) {
                cfs = new CloudFileSystem(Settings, azureAccount, azureKey);
            }

            try {
                var listener = new Listener();

                // get startup information.

                foreach( var host in hosts ) {
                    listener.AddHost(host);
                }

                foreach( var port in ports ) {
                    listener.AddPort(port);
                }

                
                listener.AddHandler(commitMessage, new CommitMessageHandler(tweetCommits, aliases));

                if( string.IsNullOrEmpty(packageStoragePath) || string.IsNullOrEmpty(localfeedLocation) || string.IsNullOrEmpty(packagePrefixUrl)  ) {
                    Console.WriteLine("[Package Uploader Disabled] specify must specify --package-path, --feed-path and  --package-prefix");
                }else {
                    listener.AddHandler(packageUpload, new UploadedFileHandler(localfeedLocation, canonicalFeedUrl, packageStoragePath, packagePrefixUrl, tweetPackages, cfs));
                }
                listener.Start();

                Console.WriteLine("Press ctrl-c to stop the listener.");

                while (true) {
                    // one day, Ill put a check to restart the server in here.
                    Thread.Sleep(60 * 1000);
                    
                }

                listener.Stop();
            } catch(Exception e) {
                Listener.HandleException(e);
                CancellationTokenSource.Cancel();
            }

            return 0;
        }

        private void Verbose(string text, params object[] objs) {
            if (_verbose) {
                Console.WriteLine(text.format(objs));
            }
        }
    }
}

