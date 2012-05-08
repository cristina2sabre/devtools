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


namespace OAuthTool {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using CoApp.Toolkit.Collections;
    using CoApp.Toolkit.Extensions;
    using Microsoft.Win32;
    using Twitterizer;

    internal class OAuthToolMain {
        private const string HELP =
            @"
Usage:
-------

OAuthTool [options] <service>

    <service>                   the service to oath to. Options:
                                    twitter
                                            
    Options:
    --------
    --help                      this help
    --nologo                    don't display the logo
    --load-config=<file>        loads configuration from <file>
    --verbose                   prints verbose messages

    Twitter Options:
    -----------------
    --key=<key>                 the consumer key
    --secret=<key>              the consumer secret
    --pin=<pin>                 the users pin (on the second response)

";

        private static int Main(string[] args) {
            return new OAuthToolMain().main(args);
        }

        private int main(string[] args) {
            var options = new XDictionary<string, IEnumerable<string>>(args.Where(each => each.StartsWith("--")).Switches());
            var parameters = args.Parameters().ToArray();

            #region Parse Options

            foreach (var arg in options.Keys) {
                var argumentParameters = options[arg];

                switch (arg) {
                        /* options  */

                    case "verbose":
                        break;

                        /* global switches */
                    case "load-config":
                        // all ready done, but don't get too picky.
                        break;

                    case "nologo":
                        this.Assembly().SetLogo("");
                        break;

                    case "help":
                        return Help();
                }
            }
            Logo();

            #endregion

            if (parameters.Length != 1) {
                return Fail("Must have a single parameter for service name");
            }

            switch (parameters.FirstOrDefault().ToLower()) {
                case "twitter":
                    var key = (options["key"] ?? Enumerable.Empty<string>()).LastOrDefault();
                    var secret = (options["secret"] ?? Enumerable.Empty<string>()).LastOrDefault();
                    var pin = (options["pin"] ?? Enumerable.Empty<string>()).LastOrDefault();
                    var reqToken = (options["token"] ?? Enumerable.Empty<string>()).LastOrDefault();

                    if (key == null || secret == null) {
                        return Fail("Must specify --key= and --secret for twitter");
                    }

                    var requestToken = OAuthUtility.GetRequestToken(key, secret, "oob");
                    var auth = OAuthUtility.BuildAuthorizationUri(requestToken.Token);

                    if (pin == null || reqToken == null) {
                        Console.WriteLine("User must authenticate at {0}", auth.AbsoluteUri);
                        Process.Start(new ProcessStartInfo {UseShellExecute = false, FileName = GetDefaultBrowserPath(), Arguments = auth.AbsoluteUri});
                        // Kernel32.CreateProcessW(IntPtr.Zero, auth.AbsolutePath, )
                        Console.WriteLine(GetDefaultBrowserPath());
                        Console.WriteLine("Re-run with command line:");
                        Console.WriteLine("\"{0}\" --key={1} --secret={2} --token={3} --pin=<pin>", "oauthtool", key, secret, requestToken.Token);
                        return 0;
                    }

                    Console.WriteLine("Pin: '{0}'", pin);
                    var token = OAuthUtility.GetAccessToken(key, secret, reqToken, pin);

                    Console.WriteLine("\r\n\r\n Twitter Access Information:");
                    Console.WriteLine("----------------------------------\r\n");
                    Console.WriteLine("consumer-key={0}", key);
                    Console.WriteLine("consumer-secret={0}", secret);
                    Console.WriteLine("{0}-access-token={1}", token.ScreenName, token.Token);
                    Console.WriteLine("{0}-access-secret={1}", token.ScreenName, token.TokenSecret);

                    break;
            }

            return 0;
        }

        private static string GetDefaultBrowserPath() {
            string key = @"http\shell\open\command";
            RegistryKey registryKey =
                Registry.ClassesRoot.OpenSubKey(key, false);
            return ((string)registryKey.GetValue(null, null)).Split('"')[1];
        }

        #region fail/HELP/logo

        public int Fail(string text, params object[] par) {
            Logo();
            using (new ConsoleColors(ConsoleColor.Red, ConsoleColor.Black)) {
                Console.WriteLine("Error:{0}", text.format(par));
            }
            return 1;
        }

        private int Help() {
            Logo();
            using (new ConsoleColors(ConsoleColor.White, ConsoleColor.Black)) {
                Console.WriteLine(HELP);
            }
            return 0;
        }

        private void Logo() {
            using (new ConsoleColors(ConsoleColor.Cyan, ConsoleColor.Black)) {
                Console.WriteLine(this.Assembly().Logo());
            }
            this.Assembly().SetLogo("");
        }

        #endregion
    }
}