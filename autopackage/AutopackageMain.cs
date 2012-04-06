﻿//-----------------------------------------------------------------------
// <copyright company="CoApp Project">
//     Copyright (c) 2011 Garrett Serack, Eric Schultz. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace CoApp.Autopackage {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Resources;
    using System.Text;
    using System.Xml.Linq;
    using Developer.Toolkit.Publishing;
    using Properties;
    using Toolkit.Console;
    using Toolkit.Crypto;
    using Toolkit.DynamicXml;
    using Toolkit.Engine;
    using Toolkit.Engine.Client;
    using Toolkit.Engine.Model.Atom;
    using Toolkit.Exceptions;
    using Toolkit.Extensions;
    using Toolkit.Logging;
    using Toolkit.Network;
    using Toolkit.Scripting.Languages.PropertySheet;
    using Toolkit.Tasks;
    using Toolkit.Utility;
    using Toolkit.Win32;

    public enum MessageCode {
        // severe unhandleable messages
        Unknown = 100,

        // Illogical errors
        UnknownFileList = 200,
        MultipleFileLists,
        MultipleApplications,
        MultipleAssemblyArchitectures,
        MultipleAssemblyVersions,

        // bad user supplied information
        FileNotFound = 300,
        CircularFileReference,
        DependentFileListUnavailable,
        IncludeFileReferenceMatchesZeroFiles,
        ZeroPackageRolesDefined,
        DuplicateAssemblyDefined,
        FailedToFindRequiredPackage,
        ManagedAssemblyWithMoreThanOneFile,
        MissingPackageName,
        AssemblyHasNoVersion,
        UnableToDeterminePackageVersion,
        UnableToDeterminePackageArchitecture,
        UnknownCompositionRuleType,
        MultiplePackagesMatched,
        ManifestReferenceNotFound,
        AssemblyVersionDoesNotMatch,
        AssembliesMustBeSigned,

        // warnings
        WarningUnknown = 500,
        TrimPathOptionInvalid,
        AssumingVersionFromAssembly,
        AssumingVersionFromApplicationFile,
        BadIconReference,
        NoIcon,
        BadLicenseLocation,
        BadDate,


        // other stuff.
        WixCompilerError = 600,
        WixLinkerError,
        AssemblyLinkerError,
        SigningFailed, 

    }

    public class AutopackageMessages : MessageHandlers<AutopackageMessages> {
        #region Delegates

        public delegate void ErrorHandler(MessageCode code, SourceLocation sourceLocation, string message, params object[] args);
        public delegate void WarningHandler(MessageCode code, SourceLocation sourceLocation, string message, params object[] args);
        public delegate void MessageHandler(MessageCode code, SourceLocation sourceLocation, string message, params object[] args);
        public delegate void VerboseHandler(string message, params object[] args);

        #endregion

        public ErrorHandler Error;
        public WarningHandler Warning;
        public MessageHandler Message;
        public VerboseHandler Verbose;
    }

    public class AutopackageException : CoAppException {
    }

    /// <summary>
    ///   Main Program for command line coapp tool
    /// </summary>
    public class AutopackageMain : AsyncConsoleProgram {
        public static bool Override;

        // error/warning handling
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _warnings = new List<string>();
        private readonly List<string> _msgs = new List<string>();

        internal static EasyPackageManager _easyPackageManager = new EasyPackageManager();
        
        // command line stuff
        
        internal static bool _verbose;

        internal PackageSource PackageSource;
        internal AutopackageModel PackageModel;
        internal AtomFeed PackageFeed;
        private string _signingCertPath;
        private string _signingCertPassword;
        private bool _remember;

        protected override ResourceManager Res {
            get {
                return Resources.ResourceManager;
            }
        }

        /// <summary>
        ///   Main entrypoint for Autopackage.
        /// </summary>
        /// <param name = "args">
        ///   The command line arguments
        /// </param>
        /// <returns>
        ///   int value representing the ERRORLEVEL.
        /// </returns>
        public static int Main(string[] args) {
            return new AutopackageMain().Startup(args);
        }

        private void UnknownPackage(string canonicalName) {
            Console.WriteLine("PKGMGR RESPONSE: Unknown Package {0}", canonicalName);
        }

        private void BlockedPackage(string canonicalName) {
            Console.WriteLine("PKGMGR RESPONSE: Package {0} is blocked", canonicalName);
        }

        private void CancellationRequested(string obj) {
            Console.WriteLine("PKGMGR RESPONSE: Cancellation Requested.");
        }

        private void MessageArgumentError(string arg1, string arg2, string arg3) {
            Console.WriteLine("PKGMGR RESPONSE: Message Argument Error {0}, {1}, {2}.", arg1, arg2, arg3);
        }

        private void OperationRequiresPermission(string policyName) {
            Console.WriteLine("PKGMGR RESPONSE: Operation requires permission Policy:{0}", policyName);
        }

        private void NoPackagesFound() {
            Console.WriteLine("PKGMGR RESPONSE: Did not find any packages.");
        }

        private void UnexpectedFailure(Exception obj) {
            throw new ConsoleException("SERVER EXCEPTION: {0}\r\n{1}", obj.Message, obj.StackTrace);
        }

        /// <summary>
        ///   The (non-static) startup method
        /// </summary>
        /// <param name = "args">
        ///   The command line arguments.
        /// </param>
        /// <returns>
        ///   Process return code.
        /// </returns>
        protected override int Main(IEnumerable<string> args) {
            // force temporary folder to be where we want it to be.

            _easyPackageManager.AddSessionFeed(Environment.CurrentDirectory).Wait();

            var macrovals = new Dictionary<string, string>();

            try {
                // default:
                var options = args.Where(each => each.StartsWith("--")).Switches();
                var parameters = args.Where(each => !each.StartsWith("--")).Parameters();

                foreach (var arg in options.Keys) {
                    var argumentParameters = options[arg];
                    var last = argumentParameters.LastOrDefault();
                    var lastAsBool = string.IsNullOrEmpty(last) || last.IsTrue();

                    switch (arg) {
                            /* options  */

                            /* global switches */
                        case "verbose":
                            _verbose = lastAsBool;
                            Logger.Messages = true;
                            Logger.Warnings = true;
                            Logger.Errors = true;
                            break;

                        case "load-config":
                            // all ready done, but don't get too picky.
                            break;

                        case "nologo":
                            this.Assembly().SetLogo(string.Empty);
                            break;

                        case "show-tools":
                            Tools.ShowTools = lastAsBool;
                            break;

                        case "certificate-path":
                            _signingCertPath = Path.GetFullPath(last);
                            break;

                        case "password":
                            _signingCertPassword = last;
                            break;

                        case "remember":
                            _remember = lastAsBool;
                            break;

                        case "override":
                            Override = true;
                            break;

                        case "help":
                            return Help();

                        case "no-toolkit-dependency":
                            break;

                        default:
                            macrovals.Add(arg, last);
                            break;
                    }
                }

                // set up the stuff to catch our errors and warnings
                new AutopackageMessages {
                    Error = HandleErrors,
                    Warning = HandleWarnings,
                    Verbose = Verbose,
                    Message = HandleMessage,
                }.Register();

                // find all the command line tools that we're gonna need.
                Tools.LocateCommandlineTools();

                if (parameters.Count() < 1) {
                    throw new ConsoleException("Missing .autopkg script.");
                    // throw new ConsoleException(Resources.NoConfigFileLoaded);
                }

                Logo();

                var allFiles = parameters.FindFilesSmarter().ToArray();
                foreach (var file in allFiles) {
                    FilesystemExtensions.ResetTempFolder();
                    using (var popd = new PushDirectory(Path.GetDirectoryName(file.GetFullPath()))) {
                        Binary.UnloadAndResetAll();

                        PackageSource = new PackageSource();
                        foreach( var k in macrovals.Keys) {
                            PackageSource.MacroValues.Add(k, macrovals[k]);
                        }

                        PackageSource.SigningCertPath = _signingCertPath;
                        PackageSource.SigningCertPassword = _signingCertPassword;
                        PackageSource.Remember = _remember;

                        // ------ Load Information to create Package 
                        PackageSource.LoadPackageSourceData(file);

                        // ------- Create data model for package
                        CreatePackageModel();

                        // ------ Generate package MSI from model
                        CreatePackageFile();
                    }
                }

            } catch (AutopackageException) {
                return Fail("Autopackage encountered errors.\r\n");
            } catch (ConsoleException failure) {
                return Fail("{0}\r\n\r\n    {1}", failure.Message, Resources.ForCommandLineHelp);
            } catch (Exception failure) {
                if( failure.InnerException != null ) {
                    Fail("Exception Caught: {0}\r\n{1}\r\n\r\n    {2}", failure.InnerException.Message, failure.InnerException.StackTrace, Resources.ForCommandLineHelp);
                }
                
                return Fail("Exception Caught: {0}\r\n{1}\r\n\r\n    {2}", failure.Message, failure.StackTrace, Resources.ForCommandLineHelp);
            }
            return 0;
        }

        private void CreatePackageModel() {
            PackageFeed = new AtomFeed();
            PackageModel = new AutopackageModel(PackageSource, PackageFeed);
            PackageFeed.Add(PackageModel);

            PackageModel.ProcessCertificateInformation();

            // find the xml templates that we're going to generate content with
            PackageModel.ProcessPackageTemplates();

            // Run through the file lists and gather in all the files that we're going to include in the package.
            PackageModel.ProcessFileLists();
            // at the end of the step, if there are any errors, let's print them out and exit now.
            FailOnErrors();

            // Ensure digital signatures and strong names are all good to go
            // this doesn't commit the files to disk tho' ...
            PackageModel.ProcessDigitalSigning();
            // at the end of the step, if there are any errors, let's print them out and exit now.
            FailOnErrors();

            PackageModel.ProcessApplicationRole();
            // at the end of the step, if there are any errors, let's print them out and exit now.
            FailOnErrors();

            PackageModel.ProcessDeveloperLibraryRoles();
            // at the end of the step, if there are any errors, let's print them out and exit now.
            FailOnErrors();

            PackageModel.ProcessServiceRoles();
            // at the end of the step, if there are any errors, let's print them out and exit now.
            FailOnErrors();

            PackageModel.ProcessDriverRoles();
            // at the end of the step, if there are any errors, let's print them out and exit now.
            FailOnErrors();

            PackageModel.ProcessWebApplicationRoles();
            // at the end of the step, if there are any errors, let's print them out and exit now.
            FailOnErrors();

            PackageModel.ProcessSourceCodeRoles();
            // at the end of the step, if there are any errors, let's print them out and exit now.
            FailOnErrors();

            // identify all assemblies to create in the package
            PackageModel.ProcessAssemblyRules();
            // at the end of the step, if there are any errors, let's print them out and exit now.
            FailOnErrors();
           
            // Validate the basic information of this package
            PackageModel.ProcessBasicPackageInformation();
            // at the end of the step, if there are any errors, let's print them out and exit now.
            FailOnErrors();

            // Gather the dependency information for the package
            PackageModel.ProcessDependencyInformation();
            // at the end of the step, if there are any errors, let's print them out and exit now.
            FailOnErrors();

            // update manifests for things that need them.
            PackageModel.UpdateApplicationManifests();
            // at the end of the step, if there are any errors, let's print them out and exit now.
            FailOnErrors();

            // Build Assembly policy files
            PackageModel.CreateAssemblyPolicies();
            // at the end of the step, if there are any errors, let's print them out and exit now.
            FailOnErrors();

            // persist all the changes to any binaries that we've touched.
            PackageModel.SaveModifiedBinaries();
            // at the end of the step, if there are any errors, let's print them out and exit now.
            FailOnErrors();

            PackageModel.ProcessCosmeticMetadata();
            // at the end of the step, if there are any errors, let's print them out and exit now.
            FailOnErrors();
            
            PackageModel.ProcessCompositionRules();
            // at the end of the step, if there are any errors, let's print them out and exit now.
            FailOnErrors();
        }

        private void CreatePackageFile() {
            var msiFile = Path.Combine(Environment.CurrentDirectory, "{0}-{1}-{2}.msi".format(PackageModel.Name, (string)PackageModel.Version, PackageModel.Architecture.ToString()));
            PackageSource.MacroValues.Add("outputfilename", Path.GetFileName(msiFile));
            PackageSource.MacroValues.Add("name", Path.GetFileNameWithoutExtension(msiFile));
            PackageSource.MacroValues.Add("canonicalname", Path.GetFileNameWithoutExtension(PackageModel.CanonicalName));

            var wixDocument = new WixDocument(PackageSource, PackageModel, PackageFeed);
            wixDocument.FillInTemplate();
            FailOnErrors();

            wixDocument.CreatePackageFile(msiFile);
            FailOnErrors();

            PeBinary.SignFile(msiFile, PackageSource.Certificate);
            Console.WriteLine("\r\n ==========\r\n DONE : Signed MSI File: {0}", msiFile);

            // recognize the new package in case it is needed for another package.
            if (!string.IsNullOrEmpty(msiFile) && File.Exists(msiFile)) {
                // Console.WriteLine("\r\n Recognizing: {0}", msiFile);
                PackageManager.Instance.RecognizeFile(null, msiFile, null, null).Wait();
            }

        }

        private void HandleWarnings(MessageCode code, SourceLocation sourceLocation, string message, object[] args) {
            var warning = string.Empty;

            if (sourceLocation != null) {
                warning = "{0}({1},{2}):AP{3}:{4}".format(
                    sourceLocation.SourceFile, sourceLocation.Row, sourceLocation.Column, (int)code,
                    message.format(args));
                _warnings.Add(warning);
            } else {
                warning = ":AP{0}:{1}".format((int)code, message.format(args));
                _warnings.Add(warning);
            }
            using (new ConsoleColors(ConsoleColor.Yellow, ConsoleColor.Black)) {
                Console.WriteLine(warning);
            }
        }

        private void HandleMessage(MessageCode code, SourceLocation sourceLocation, string message, object[] args) {
            var msg = string.Empty;

            if (sourceLocation != null) {
                msg = "{0}({1},{2}):AP{3}:{4}".format(
                    sourceLocation.SourceFile, sourceLocation.Row, sourceLocation.Column, (int)code,
                    message.format(args));
                _msgs.Add(msg);
            } else {
                msg = ":AP{0}:{1}".format((int)code, message.format(args));
                _msgs.Add(msg);
            }
            using (new ConsoleColors(ConsoleColor.White, ConsoleColor.Black)) {
                Console.WriteLine(msg);
            }
        }


        private void HandleErrors(MessageCode code, SourceLocation sourceLocation, string message, object[] args) {
            if (sourceLocation != null) {
                _errors.Add(
                    "{0}({1},{2}):AP{3}:{4}".format(
                        sourceLocation.SourceFile, sourceLocation.Row, sourceLocation.Column, (int)code,
                        message.format(args)));
            } else {
                _errors.Add(":AP{0}:{1}".format((int)code, message.format(args)));
            }
        }

        private void Verbose(string text, params object[] par) {
            if (_verbose) {
                using (new ConsoleColors(ConsoleColor.White, ConsoleColor.Black)) {
                    Console.WriteLine(text.format(par));
                }
            }
        }

        internal void FailOnErrors() {
            if (_errors.Any()) {
                using (new ConsoleColors(ConsoleColor.Red, ConsoleColor.Black)) {
                    foreach (var e in _errors) {
                        Console.WriteLine(e);
                    }
                }
                throw new AutopackageException();
            }
        }

        protected int Fail(string text, IEnumerable<Exception> failures, params object[] par) {
            Logo();

            var sb = new StringBuilder();

            foreach (var f in failures) {
                sb.AppendLine("Error: {0}".format(f.Message));
            }
            IEnumerable<object> output = sb.SingleItemAsEnumerable();
            output = output.Concat(par);
            using (new ConsoleColors(ConsoleColor.Red, ConsoleColor.Black)) {
                Console.WriteLine(text.format(output.ToArray()));
            }
            Console.WriteLine("Press Enter To Continue.");
            Console.ReadLine();

            return 1;
        }
    }
}