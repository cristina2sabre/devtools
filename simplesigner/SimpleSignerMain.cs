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

namespace CoApp.simplesigner {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Developer.Toolkit.Publishing;
    using Toolkit.Collections;
    using Toolkit.Extensions;
    using Toolkit.Win32;

    internal class SimpleSignerMain {
        /// <summary>
        ///   Command line help information
        /// </summary>
        private const string HelpMessage =
            @"
Usage:
-------

SimpleSigner [options] <file(s)-to-sign...>

Options:
--------
    --help                      this help
    --nologo                    don't display the logo
    --load-config=<file>        loads configuration from <file>
    --verbose                   prints verbose messages

    --certificate-path=<c.pfx>  path to load signing certificate (w/pvt key)
    --password=<pwd>            password for certificate file
    --remember                  store certificate details in registry 
                                (encrypted)

    --sign                      digitally sign the binary
    --strong-name               strong name the assembly (if it is a .NET asm)

    --no-metadata               don't try to adjust any metadata
    --force                     re-sign the binaries, even if they have 
                                a signature

    --verify                    show certificate & verification info for 
                                binaries (don't sign)

    --auto                      automatically handle unsigned dependent 
                                assemblies

Metadata Options:
-----------------

    --company=<Name>            set the Company Name to <Name>*
    --description=<value>       set the File Description to <value>
    --internal-name=<value>     set the Internal Name of the binary to <value>
    --copyright=<value>         set the Copyright to <value>
    --original-filename=<value> set the Original Filename to <value>
    --product-name=<value>      set the Product Name to <value>

    --product-version=<value>   set the Product Version to <value>
    --file-version=<value>      set the File Version to <value>

    * use value AUTO to have it pull company name from the certificate


Manifest Options:    
-----------------

    --execution-level=<level>   sets the requestedExecutionLevel in the 
                                manifest to the specified level
                                    one of 
                                        administrator 
                                        invoker
                                        highest-available

    --dpi-aware=<bool>          sets the 'dpi aware' flag in the manifest


    --reference-assembly=<ref>  adds an assembly reference to the PE binary
                                <ref> should be in the format:
                                ""<NAME>, Version=<VERSION>, PublicKeyToken=<PKT>, ProcessorArchitecture=<ARCH>""
                                where 
                                    <NAME> is the name of the assembly
                                    <VERSION> is the four-part version number (1.2.3.4)
                                    <PKT> is the public key token of the publisher
                                    <ARCH> is one of {{ x86, x64, any }}

        for example, to add a reference to zlib the option might look like this:
        --reference-assembly=""zlib, Version=1.2.5.0, PublicKeyToken=1e373a58e25250cb, ProcessorArchitecture=x86""

";

        private List<AssemblyReference> assemblyReferences = new List<AssemblyReference>();
        private bool _remember;
        private bool _sign;
        private bool _strongname;
        private bool _verbose;
        private bool _verify;
        private bool _justsign;

        private string _signingCertPath = string.Empty;
        private string _signingCertPassword;
        private CertificateReference _certificate;
        private FourPartVersion _fileVersion;
        private string _company;
        private string _description;
        private string _internalName;
        private string _copyright;
        private string _productName;
        private string _originalFilename;
        private FourPartVersion _productVersion;
        private ExecutionLevel _executionLevel = ExecutionLevel.none;
        private bool? _dpiAware;

        private static int Main(string[] args) {
            return new SimpleSignerMain().Startup(args);
        }

        private int Startup(IEnumerable<string> args) {
            var options = args.Where(each => each.StartsWith("--")).Switches();
            var parameters = args.Parameters();

            foreach (var arg in options.Keys) {
                var argumentParameters = options[arg];

                switch (arg) {
                        /* global switches */
                    case "load-config":
                        // all ready done, but don't get too picky.
                        break;

                    case "nologo":
                        this.Assembly().SetLogo(string.Empty);
                        break;

                    case "help":
                        return Help();

                    case "certificate-path":
                        var cert = argumentParameters.Last();
                        _signingCertPath = _signingCertPath.IndexOf(":") > 1 ? cert : Path.GetFullPath(argumentParameters.Last());
                        break;

                    case "password":
                        _signingCertPassword = argumentParameters.Last();
                        break;

                    case "remember":
                        _remember = true;
                        break;

                    case "auto":
                        break;

                    case "sign":
                        _sign = true;
                        break;

                    case "just-sign":
                        _sign = true;
                        _justsign = true;
                        break;

                    case "strong-name":
                        _strongname = true;
                        break;

                    case "verbose":
                        _verbose = true;
                        break;

                    case "company":
                        _company = argumentParameters.Last();
                        break;

                    case "description":
                        _description = argumentParameters.Last();
                        break;

                    case "internal-name":
                        _internalName = argumentParameters.Last();
                        break;

                    case "copyright":
                        _copyright = argumentParameters.Last();
                        break;

                    case "original-filename":
                        _originalFilename = argumentParameters.Last();
                        break;

                    case "product-name":
                        _productName = argumentParameters.Last();
                        break;

                    case "verify":
                        _verify = true;
                        break;

                    case "reference-assembly":
                        foreach (var asmRef in argumentParameters) {
                            if (string.IsNullOrEmpty(asmRef)) {
                                return Fail("Missing assembly information for --assembly-reference.");
                            }

                            var parts = asmRef.Split(", ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            var assemblyref = new AssemblyReference {Name = parts[0]};

                            foreach (var part in parts.Skip(1)) {
                                var kp = part.Split("= ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                                if (kp.Length != 2) {
                                    return Fail("Invalid option '{0}' in assembly reference '{1}'.", part, asmRef);
                                }

                                switch (kp[0].ToLower()) {
                                    case "version":
                                    case "ver":
                                        assemblyref.Version = kp[1];
                                        if (assemblyref.Version == 0L) {
                                            return Fail("Invalid Version '{0}' in assembly reference '{1}'.", kp[1], asmRef);
                                        }
                                        break;

                                    case "publickeytoken":
                                    case "pkt":
                                    case "token":
                                        if (kp[1].Length != 16) {
                                            return Fail("Invalid publicKeyToken '{0}' in assembly reference '{1}'.", kp[1], asmRef);
                                        }
                                        assemblyref.PublicKeyToken = kp[1];
                                        break;

                                    case "processorarchitecture":
                                    case "architecture":
                                    case "arch":
                                        assemblyref.Architecture = kp[1];
                                        if (assemblyref.Architecture == Architecture.Auto || assemblyref.Architecture == Architecture.Unknown) {
                                            return Fail("Invalid processorArchitecture '{0}' in assembly reference '{1}'.", kp[1], asmRef);
                                        }
                                        break;
                                }
                            }
                            if (assemblyref.Version == 0 || assemblyref.Architecture == Architecture.Unknown || string.IsNullOrEmpty(assemblyref.PublicKeyToken)) {
                                return Fail("Invalid assembly reference '{0}' ", asmRef);
                            }
                            assemblyReferences.Add(assemblyref);
                        }
                        break;

                    case "product-version":
                        _productVersion = argumentParameters.Last();
                        if (_productVersion == 0L) {
                            return Fail("--product-version must be in the form ##.##.##.##");
                        }

                        break;

                    case "file-version":
                        _fileVersion = argumentParameters.Last();
                        if (_fileVersion == 0L) {
                            return Fail("--file-version must be in the form ##.##.##.##");
                        }
                        break;

                    case "execution-level":
                        switch (argumentParameters.Last()) {
                            case "administrator":
                            case "admin":
                            case "requires-admin":
                            case "requiresadmin":
                            case "requiresadministrator":
                            case "requires-administrator":
                                _executionLevel = ExecutionLevel.requireAdministrator;
                                break;
                            case "invoker":
                            case "asinvoker":
                            case "as-invoker":
                                _executionLevel = ExecutionLevel.asInvoker;
                                break;
                            case "highest-available":
                            case "highest":
                            case "highestavailable":
                                _executionLevel = ExecutionLevel.highestAvailable;
                                break;
                        }
                        break;

                    case "dpi-aware":
                        if (argumentParameters.Last().IsTrue()) {
                            _dpiAware = true;
                        }
                        if (argumentParameters.Last().IsFalse()) {
                            _dpiAware = false;
                        }
                        break;
                    default:
                        return Fail("Unknown parameter [--{0}]", arg);
                }
            }

            Logo();

            if (_verify) {
                // return Verify(parameters);
            }

            if (string.IsNullOrEmpty(_signingCertPath)) {
                _certificate = CertificateReference.Default;
                if (_certificate == null) {
                    return Fail("No default certificate stored in the registry");
                }
            } else if (string.IsNullOrEmpty(_signingCertPassword)) {
                _certificate = new CertificateReference(_signingCertPath);
            } else {
                _certificate = new CertificateReference(_signingCertPath, _signingCertPassword);
            }

            using (new ConsoleColors(ConsoleColor.White, ConsoleColor.Black)) {
                Verbose("Loaded certificate with private key {0}", _certificate.Location);
            }

            if (_remember) {
                Verbose("Storing certificate details in the registry.");
                _certificate.RememberPassword();
                CertificateReference.Default = _certificate;
            }

            if (parameters.Count() < 1) {
                return Fail("Missing files to sign/name. \r\n\r\n    Use --help for command line help.");
            }

            var tasks = new List<Task>();

            if (_company != null && _company.Equals("auto", StringComparison.CurrentCultureIgnoreCase)) {
                _company = _certificate.CommonName;
            }
            var failures = 0;
            try {
                var allFiles = parameters.FindFilesSmarter().ToArray();
                var origMD5 = new XDictionary<string, string>();

                var loading = allFiles.Select(each =>
                    Binary.Load(each,
                        BinaryLoadOptions.PEInfo |
                            BinaryLoadOptions.VersionInfo |
                                BinaryLoadOptions.Managed |
                                    BinaryLoadOptions.Resources |
                                        BinaryLoadOptions.Manifest |
                                            BinaryLoadOptions.UnsignedManagedDependencies |
                                                BinaryLoadOptions.MD5).ContinueWith(antecedent => {
                                                    lock (allFiles) {
                                                        if (antecedent.IsFaulted) {
                                                            Console.WriteLine("Failed to load file '{0}'", each);
                                                            var e = antecedent.Exception.Flatten().InnerExceptions.First();
                                                            Console.WriteLine("{0}--{1}", e.Message, e.StackTrace);
                                                            return;
                                                        }

                                                        try {
                                                            var binary = antecedent.Result;
                                                            origMD5.Add(each, binary.MD5);

                                                            if (binary.IsPEFile && !_justsign) {
                                                                // do PE file stuff
                                                                if (_sign) {
                                                                    binary.SigningCertificate = _certificate;
                                                                }

                                                                if (binary.IsManaged && _strongname) {
                                                                    binary.StrongNameKeyCertificate = _certificate;
                                                                }

                                                                if (!assemblyReferences.IsNullOrEmpty()) {
                                                                    foreach (var asmRef in assemblyReferences) {
                                                                        binary.Manifest.Value.AddDependency(asmRef.Name, asmRef.Version, asmRef.Architecture, asmRef.PublicKeyToken);
                                                                    }
                                                                }

                                                                if (_company != null) {
                                                                    binary.CompanyName = _company;
                                                                }
                                                                if (_description != null) {
                                                                    binary.FileDescription = _description;
                                                                }
                                                                if (_internalName != null) {
                                                                    binary.InternalName = _internalName;
                                                                }
                                                                if (_copyright != null) {
                                                                    binary.LegalCopyright = _copyright;
                                                                }
                                                                if (_originalFilename != null) {
                                                                    binary.OriginalFilename = _originalFilename;
                                                                }
                                                                if (_productName != null) {
                                                                    binary.ProductName = _productName;
                                                                }
                                                                if (_productVersion != 0) {
                                                                    binary.ProductVersion = _productVersion;
                                                                }
                                                                if (_fileVersion != 0) {
                                                                    binary.FileVersion = _fileVersion;
                                                                }
                                                                if (_dpiAware != null) {
                                                                    binary.Manifest.Value.DpiAware = _dpiAware == true;
                                                                }
                                                                if (_executionLevel != ExecutionLevel.none) {
                                                                    binary.Manifest.Value.RequestedExecutionLevel = _executionLevel;
                                                                }
                                                            } else {
                                                                // do stuff for non-pe files
                                                                // we can try to apply a signature, and that's about it.
                                                                if (_sign) {
                                                                    binary.SigningCertificate = _certificate;
                                                                }
                                                            }
                                                            binary.Save().Wait();
                                                        } catch (Exception e) {
                                                            while (e.GetType() == typeof (AggregateException)) {
                                                                e = (e as AggregateException).Flatten().InnerExceptions[0];
                                                            }
                                                            failures += Fail("{0}--{1}", e.Message, e.StackTrace);
                                                        }
                                                    }
                                                }, TaskContinuationOptions.AttachedToParent)).ToArray();

                // Thread.Sleep(1000);
                // wait for loading.
                return Task.Factory.ContinueWhenAll(loading, tsks => {
                    Console.WriteLine("Done {0} files", tsks.Length);

                    (from each in Binary.Files
                        select new {
                            Filename = Path.GetFileName(each.Filename),
                            Original_MD5 = origMD5[each.Filename],
                            New_MD5 = each.MD5,
                            //  Status = each.Message,
                        }).ToTable().ConsoleOut();

                    if (failures > 0) {
                        Console.WriteLine("*** Bad News. Failed. *** ");
                    }

                    if (Binary.IsAnythingStillLoading) {
                        Console.WriteLine("\r\n==== Uh, stuff is still in the loading state?! ====\r\n");
                    }

                    return failures;
                }).Result;
            } catch (Exception e) {
                Console.WriteLine("{0}--{1}", e.Message, e.StackTrace);
                return Fail("not good.");
            }
        }

        #region fail/help/logo

        /// <summary>
        ///   Displays a failure message.
        /// </summary>
        /// <param name="text"> The text format string. </param>
        /// <param name="par"> The parameters for the formatted string. </param>
        /// <returns> returns 1 (usually passed out as the process end code) </returns>
        public int Fail(string text, params object[] par) {
            Logo();
            using (new ConsoleColors(ConsoleColor.Red, ConsoleColor.Black)) {
                Console.WriteLine("Error: {0}", text.format(par));
            }

            return 1;
        }

        /// <summary>
        ///   Displays the program help.
        /// </summary>
        /// <returns> returns 0. </returns>
        private int Help() {
            Logo();
            using (new ConsoleColors(ConsoleColor.White, ConsoleColor.Black)) {
                Console.WriteLine(HelpMessage);
            }

            return 0;
        }

        /// <summary>
        ///   Displays the program logo.
        /// </summary>
        private void Logo() {
            using (new ConsoleColors(ConsoleColor.Cyan, ConsoleColor.Black)) {
                    
            }

            this.Assembly().SetLogo(string.Empty);
        }

        private void Verbose(string text, params object[] par) {
            if (_verbose) {
                using (new ConsoleColors(ConsoleColor.White, ConsoleColor.Black)) {
                    Console.WriteLine(text.format(par));
                }
            }
        }

        #endregion
    }
}