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

namespace CoApp.mkRepo {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Resources;
    using System.ServiceModel.Syndication;
    using System.Xml;
    using Packaging.Client;
    using Packaging.Common.Model.Atom;
    using Properties;
    using Toolkit.Collections;
    using Toolkit.Console;
    using Toolkit.Exceptions;
    using Toolkit.Extensions;
    using Toolkit.Logging;
    using Toolkit.Tasks;

    public class mkRepoMain : AsyncConsoleProgram {
        private bool _verbose;
        private string _output = "feed.atom.xml";
        private string _input;
        private Uri _baseUrl;
        private Uri _feedLocation;
        private IEnumerable<string> _packages;

        internal AtomFeed Feed;

        private readonly PackageManager _packageManager = new PackageManager();

        private static int Main(string[] args) {
            return new mkRepoMain().Startup(args);
        }

        protected override ResourceManager Res {
            get {
                return Resources.ResourceManager;
            }
        }

        protected override int Main(IEnumerable<string> args) {
            CurrentTask.Events += new DownloadProgress((remoteLocation, location, progress) => {
                "Downloading {0}".format(remoteLocation.UrlDecode()).PrintProgressBar(progress);
            });

            CurrentTask.Events += new DownloadCompleted((remoteLocation, locallocation) => {
                Console.WriteLine();
            });

            try {
                #region command line parsing

                var options = args.Where(each => each.StartsWith("--")).Switches();
                var parameters = args.Where(each => !each.StartsWith("--")).Parameters();

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

                        case "output":
                            _output = last;
                            break;

                        case "input":
                            _input = last;
                            break;

                        case "feed-location":
                            try {
                                _feedLocation = new Uri(last);
                            } catch {
                                throw new ConsoleException("Feed Location '{0}' is not a valid URI ", last);
                            }

                            break;

                        case "base-url":
                            try {
                                _baseUrl = new Uri(last);
                            } catch {
                                throw new ConsoleException("Base Url Location '{0}' is not a valid URI ", last);
                            }
                            break;

                        default:
                            throw new ConsoleException(Resources.UnknownParameter, arg);
                    }
                }
                Logo();

                #endregion

                if (parameters.Count() < 1) {
                    throw new ConsoleException(Resources.MissingCommand);
                }

                _packages = parameters.Skip(1);

                switch (parameters.FirstOrDefault()) {
                    case "create":
                        Logger.Message("Creating Feed ");
                        Create();
                        break;

                    default:
                        throw new ConsoleException(Resources.UnknownCommand, parameters.FirstOrDefault());
                }
            } catch (ConsoleException failure) {
                Fail("{0}\r\n\r\n    {1}", failure.Message, Resources.ForCommandLineHelp);
                CancellationTokenSource.Cancel();
            }
            return 0;
        }

        private void Create() {
            Feed = new AtomFeed();
            AtomFeed originalFeed = null;

            if (!string.IsNullOrEmpty(_input)) {
                Logger.Message("Loading existing feed.");
                if (_input.IsWebUri()) {
                    var inputFilename = "feed.atom.xml".GenerateTemporaryFilename();

                    var rf = new RemoteFile(_input, inputFilename, (uri) => {
                    },
                        (uri) => {
                            inputFilename.TryHardToDelete();
                        },
                        (uri, progress) => {
                            "Downloading {0}".format(uri).PrintProgressBar(progress);
                        });
                    rf.Get();

                    if (!File.Exists(inputFilename)) {
                        throw new ConsoleException("Failed to get input feed from '{0}' ", _input);
                    }
                    originalFeed = AtomFeed.LoadFile(inputFilename);
                } else {
                    originalFeed = AtomFeed.LoadFile(_input);
                }
            }

            if (originalFeed != null) {
                Feed.Add(originalFeed.Items.Where(each => each is AtomItem).Select(each => each as AtomItem));
            }

            Logger.Message("Selecting local packages");
            var files = _packages.FindFilesSmarter();

            _packageManager.QueryPackages(files, dependencies: false, latest: false).ContinueWith((antecedent) => {
                var packages = antecedent.Result;

                foreach (var pkg in packages) {
                    _packageManager.GetPackageDetails(pkg.CanonicalName).Wait();

                    if (!string.IsNullOrEmpty(pkg.PackageItemText)) {
                        var item = SyndicationItem.Load<AtomItem>(XmlReader.Create(new StringReader(pkg.PackageItemText)));

                        var feedItem = Feed.Add(item);

                        // first, make sure that the feeds contains the intended feed location.
                        if (feedItem.Model.Feeds == null) {
                            feedItem.Model.Feeds = new XList<Uri>();
                        }

                        if (!feedItem.Model.Feeds.Contains(_feedLocation)) {
                            feedItem.Model.Feeds.Insert(0, _feedLocation);
                        }

                        var location = new Uri(_baseUrl, Path.GetFileName(pkg.LocalPackagePath));

                        if (feedItem.Model.Locations == null) {
                            feedItem.Model.Locations = new XList<Uri>();
                        }

                        if (!feedItem.Model.Locations.Contains(location)) {
                            feedItem.Model.Locations.Insert(0, location);
                        }
                    } else {
                        throw new ConsoleException("Missing ATOM data for '{0}'", pkg.Name);
                    }
                }
            }).Wait();

            Feed.Save(_output);

            // Feed.ToString()
            // PackageFeed.Add(PackageModel);
        }

        private void Verbose(string text, params object[] objs) {
            if (_verbose) {
                Console.WriteLine(text.format(objs));
            }
        }
    }
}