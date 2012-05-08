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

namespace CoApp.Bootstrapper {
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security.AccessControl;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Principal;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using Microsoft.Win32;

    internal class SingleStep {
        public static string MIN_COAPP_VERSION_STRING = "1.2.0.228";

        public static ulong INCOMPATIBLE_VERSION = VersionStringToUInt64("1.2.0.228");

        /// <summary>
        ///   This is the version of coapp that must be installed for the bootstrapper to continue. This should really only be updated when there is breaking changes in the client library
        /// </summary>
        public static ulong MIN_COAPP_VERSION = Math.Max(VersionStringToUInt64(MIN_COAPP_VERSION_STRING), INCOMPATIBLE_VERSION);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        internal delegate int NativeExternalUIHandler(IntPtr context, int messageType, [MarshalAs(UnmanagedType.LPWStr)] string message);

        private static readonly Lazy<string> BootstrapServerUrl = new Lazy<string>(() => GetRegistryValue(@"Software\CoApp", "BootstrapServerUrl"));
        private const string CoAppUrl = "http://coapp.org/resources/";
        internal static string MsiFilename;
        internal static string MsiFolder;
        internal static bool Quiet;
        internal static bool Passive;
        internal static bool Remove;

        internal static string BootstrapFolder;
        private static int _progressDirection = 1;
        private static int _currentTotalTicks = -1;
        private static int _currentProgress;
        internal static bool Cancelling;
        internal static Task InstallTask;

        public static ProgressFactor ResourceDllDownload = new ProgressFactor(ProgressWeight.Tiny);
        public static ProgressFactor CoAppPackageDownload = new ProgressFactor(ProgressWeight.Tiny);
        public static ProgressFactor CoAppPackageInstall = new ProgressFactor(ProgressWeight.Tiny);
        public static ProgressFactor EngineStartup = new ProgressFactor(ProgressWeight.Low);

        public static MultifactorProgressTracker Progress;

        static SingleStep() {
            Progress = new MultifactorProgressTracker {ResourceDllDownload, CoAppPackageDownload, CoAppPackageInstall, EngineStartup};

            Progress.ProgressChanged += p => {
                if (MainWindow.MainWin != null) {
                    MainWindow.MainWin.Updated();
                }
            };
        }

        [STAThread]
        [LoaderOptimization(LoaderOptimization.MultiDomainHost)]
        public static void Main(string[] args) {
            var commandline = args.Aggregate(string.Empty, (current, each) => current + " \"" + each + "\"").Trim();
            ElevateSelf(commandline);

            Logger.Warning("Startup :" + commandline);
            // Ensure that we are elevated. If the app returns from here, we are.

            // get the folder of the bootstrap EXE
            BootstrapFolder = Path.GetDirectoryName(Path.GetFullPath(Assembly.GetExecutingAssembly().Location));

            if (!Cancelling) {
                if (commandline.Length == 0) {
                    MainWindow.Fail(LocalizedMessage.IDS_MISSING_MSI_FILE_ON_COMMANDLINE, "Missing MSI package name on command line!");
                } else if (!File.Exists(Path.GetFullPath(args[0]))) {
                    MainWindow.Fail(LocalizedMessage.IDS_MSI_FILE_NOT_FOUND, "Specified MSI package name does not exist!");
                } else if (!ValidFileExists(Path.GetFullPath(args[0]))) {
                    MainWindow.Fail(LocalizedMessage.IDS_MSI_FILE_NOT_VALID, "Specified MSI package is not signed with a valid certificate!");
                } else {
                    // have a valid MSI file. Alrighty!
                    MsiFilename = Path.GetFullPath(args[0]);
                    MsiFolder = Path.GetDirectoryName(MsiFilename);

                    if( args.Length > 1 ) {

                        foreach (var split in args.Skip(1).Select(a => a.ToLower().Split(new[] {'='}, StringSplitOptions.RemoveEmptyEntries)).Where(split => split.Length >= 2)) {
                            if( split[0] == "--uilevel" ) {
                                Quiet = split[1] == "2";
                                Passive= split[1] == "3";
                            }

                            if(split[0] == "--remove") {
                                Remove = !string.IsNullOrEmpty(split[1]);
                            }
                        }
                    }

                    // if this installer is present, this will exit right after.
                    if (IsCoAppInstalled) {
                        RunInstaller(true);
                        return;
                    }

                    // if CoApp isn't there, we gotta get it.
                    // this is a quick call, since it spins off a task in the background.
                    InstallCoApp();
                }
            }
            // start showin' the GUI.
            // Application.ResourceAssembly = Assembly.GetExecutingAssembly();
            new Application {
                StartupUri = new Uri("managed-bootstrap/MainWindow.xaml", UriKind.Relative)
            }.Run();
        }

        private static string ExeName {
            get {
                var src = Assembly.GetEntryAssembly().Location;
                if (!src.EndsWith(".exe", StringComparison.CurrentCultureIgnoreCase)) {
                    var target = Path.Combine(Path.GetTempPath(), "Installer." + Process.GetCurrentProcess().Id + ".exe");
                    File.Copy(src, target);
                    return target;
                }
                return src;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SidIdentifierAuthority {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6, ArraySubType = UnmanagedType.I1)]
            public byte[] Value;
        }

        [DllImport("advapi32.dll", EntryPoint = "AllocateAndInitializeSid")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocateAndInitializeSid([In] ref SidIdentifierAuthority pIdentifierAuthority, byte nSubAuthorityCount, uint nSubAuthority0, uint nSubAuthority1, uint nSubAuthority2, uint nSubAuthority3, uint nSubAuthority4,
            uint nSubAuthority5, int nSubAuthority6, uint nSubAuthority7, out IntPtr pSid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CheckTokenMembership(IntPtr TokenHandle, IntPtr SidToCheck, out bool IsMember);

        internal static void ElevateSelf(string args) {
            try {
                var ntAuth = new SidIdentifierAuthority();
                ntAuth.Value = new byte[] {0, 0, 0, 0, 0, 5};

                var psid = IntPtr.Zero;
                bool isAdmin;
                if (AllocateAndInitializeSid(ref ntAuth, 2, 0x00000020, 0x00000220, 0, 0, 0, 0, 0, 0, out psid) && CheckTokenMembership(IntPtr.Zero, psid, out isAdmin) && isAdmin) {
                    return; // yes, we're an elevated admin
                }
            } catch {
                // :)
            }

            // we're not an admin I guess.
            try {
                new Process {
                    StartInfo = {
                        UseShellExecute = true,
                        WorkingDirectory = Environment.CurrentDirectory,
                        FileName = ExeName,
                        Verb = "runas",
                        Arguments = args,
                        ErrorDialog = true,
                        ErrorDialogParentHandle = GetForegroundWindow(),
                        WindowStyle = ProcessWindowStyle.Maximized,
                    }
                }.Start();
                Environment.Exit(0); // since this didn't throw, we know the kids got off to school ok. :)
            } catch {
                MainWindow.Fail(LocalizedMessage.IDS_REQUIRES_ADMIN_RIGHTS, "The installer requires administrator permissions.");
            }
        }

        public static int ToInt32(string str, int defaultValue = 0) {
            int i;
            return Int32.TryParse(str, out i) ? i : defaultValue;
        }

        public static UInt64 VersionStringToUInt64(string version) {
            if (String.IsNullOrEmpty(version)) {
                return 0;
            }

            var vers = version.Split('.');
            var major = vers.Length > 0 ? ToInt32(vers[0]) : 0;
            var minor = vers.Length > 1 ? ToInt32(vers[1]) : 0;
            var build = vers.Length > 2 ? ToInt32(vers[2]) : 0;
            var revision = vers.Length > 3 ? ToInt32(vers[3]) : 0;

            return (((UInt64)major) << 48) + (((UInt64)minor) << 32) + (((UInt64)build) << 16) + (UInt64)revision;
        }

        internal static bool IsIncompatibleCoAppInstalled {
            get {
                try {
                    // look for old versions of the coapp that are too old.
                    foreach (var ace in new [] {"CoApp.Toolkit" , "CoApp.Client", "CoApp.Toolit.Engine.Client" }.Select(asmName => new AssemblyCacheEnum(asmName))) {
                        string assembly;
                        while ((assembly = ace.GetNextAssembly()) != null) {
                            var parts = assembly.Split(", ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            // find the "version=" part
                            if ((from p in parts
                                select p.Split('=')
                                into kvp
                                where kvp[0].Equals("Version", StringComparison.InvariantCultureIgnoreCase)
                                select VersionStringToUInt64(kvp[1])).Any(installed => installed <= INCOMPATIBLE_VERSION)) {
                                return true;
                            }
                        }
                    }
                } catch {
                    
                }
                return false;
            }
        }

        internal static bool IsCoAppInstalled {
            get {
                try {
                    var ace = new AssemblyCacheEnum("CoApp.Client");
                    string assembly;
                    while ((assembly = ace.GetNextAssembly()) != null) {
                        var parts = assembly.Split(", ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        // find the "version=" part
                        if ((from p in parts
                            select p.Split('=')
                            into kvp where kvp[0].Equals("Version", StringComparison.InvariantCultureIgnoreCase)
                            select VersionStringToUInt64(kvp[1])).Any(installed => installed >= MIN_COAPP_VERSION)) {
                            return true;
                        }
                    }
                } catch {
                }
                return false;
            }
        }

        private static string GetRegistryValue(string key, string valueName) {
            try {
                var openSubKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(key);
                if (openSubKey != null) {
                    return openSubKey.GetValue(valueName).ToString();
                }
            } catch {
            }
            return null;
        }

        /// <summary>
        ///   Ok, So I think I need to explain what the hell I'm doing here. 
        ///  Once the bootstrapper has got the toolkit actually installed,
        ///  we want to launch the installer in a new appdomain in the current process.
        /// </summary>
        /// <param name="bypassingBootstrapUI"> </param>
        /// <returns> </returns>
        internal static void RunInstaller(bool bypassingBootstrapUI) {
            if (Cancelling) {
                return;
            }

            var appDomain = AppDomain.CreateDomain("appdomain" + DateTime.Now.Ticks);

            // if we didn't bypass the UI, then that means *we* had to install CoApp itself 
            // we need to make sure that the engine knows that we did that.
            if (!bypassingBootstrapUI) {
                appDomain.SetData("COAPP_INSTALLED", "TRUE");
            }

            appDomain.SetData("QUIET", Quiet);
            appDomain.SetData("PASSIVE", Passive);
            appDomain.SetData("REMOVE", Remove);

            try {
                // If we're bypassing the UI, then we can jump straight to the Installer.
                if (bypassingBootstrapUI) {
                    // no gui was involved here.
                    InstallerStageTwo(appDomain);
                    return;
                }

                // otherwise, we have to hide the bootstrapper, and jump to the installer.
                MainWindow.WhenReady += () => {
                    InstallerStageTwo(appDomain);
                };

                Logger.Message("Installer Stage One Complete...");
            } catch (Exception e) {
                Logger.Error("Critical FAIL ");
                Logger.Error(e);
                ExitQuick();
            }
        }

        private static void InstallerStageTwo(AppDomain appDomain) {
            try {
                if (Cancelling) {
                    return;
                }

                EngineStartup.Progress = 100;

                // stage two: close our bootstrap GUI, and start the Installer in the new AppDomain, 
                // of course, this has all got to happen on the original thread. *sigh*
                Logger.Message("Got to Installer Stage Two");

                bool wasCreated;
                var ewhSec = new EventWaitHandleSecurity();
                ewhSec.AddAccessRule(new EventWaitHandleAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), EventWaitHandleRights.FullControl, AccessControlType.Allow));
                var ping = new EventWaitHandle(false, EventResetMode.ManualReset, "BootstrapperPing", out wasCreated, ewhSec);
                ping.Reset();

                Task.Factory.StartNew(() => {
                    ping.WaitOne();
                    MainWindow.WhenReady += () => {
                        MainWindow.MainWin.Visibility = Visibility.Hidden;
                    };
                });

#if DEBUG_X
            //var localAssembly = AcquireFile(@"c:\root\sync\coapp\output\any\debug\bin\CoApp.Client.dll");
            //Logger.Message("Local Assembly: " + localAssembly);

            //if (!string.IsNullOrEmpty(localAssembly)) {
                // use the one found locally.
                appDomain.CreateInstanceFromAndUnwrap(@"c:\root\sync\coapp\coapp\output\any\debug\bin\CoApp.Client.dll", "CoApp.Packaging.Client.UI.Installer", false, BindingFlags.Default, null, new[] { MsiFilename }, null, null);
                // if it didn't throw here, we can assume that the CoApp service is running, and we can get to our assembly.
                ExitQuick();
            //}
#endif

              
                // meh. use strong named assembly
                appDomain.CreateInstanceAndUnwrap("CoApp.Client, Version=" + MIN_COAPP_VERSION_STRING + ", Culture=neutral, PublicKeyToken=1e373a58e25250cb",
                    "CoApp.Packaging.Client.UI.Installer", false, BindingFlags.Default, null, new[] { MsiFilename }, null, null);
                // since we've done everything we need to do, we're out of here. Right Now.
            } catch (Exception e) {
                Logger.Error("Critical FAIL ");
                Logger.Error(e);
            }
            ExitQuick();
        }

        private static void ExitQuick() {
            if (Application.Current != null) {
                Application.Current.Shutdown(0);
            }
            Environment.Exit(0);
        }

        internal static string AcquireFile(string filename, Action<int> progressCompleted = null) {
            Logger.Warning("Trying to Acquire:" + filename);
            var name = Path.GetFileNameWithoutExtension(filename);
            var extension = Path.GetExtension(filename);
            var lcid = CultureInfo.CurrentCulture.LCID;
            var localizedName = String.Format("{0}.{1}{2}", name, lcid, extension);
            string f;
            progressCompleted = progressCompleted ?? (p => {
            });

            // is the localized file in the bootstrap folder?
            if (!String.IsNullOrEmpty(BootstrapFolder)) {
                f = Path.Combine(BootstrapFolder, localizedName);
                if (ValidFileExists(f)) {
                    progressCompleted(100);
                    return f;
                }
            }

            // is the localized file in the msi folder?
            if (!String.IsNullOrEmpty(MsiFolder)) {
                f = Path.Combine(MsiFolder, localizedName);
                if (ValidFileExists(f)) {
                    progressCompleted(100);
                    return f;
                }
            }
            // try the MSI for the localized file 
            f = GetFileFromMSI(localizedName);
            if (ValidFileExists(f)) {
                progressCompleted(100);
                return f;
            }

            //------------------------
            // NORMAL FILE, ON BOX
            //------------------------

            // is the standard file in the bootstrap folder?
            if (!String.IsNullOrEmpty(BootstrapFolder)) {
                f = Path.Combine(BootstrapFolder, filename);
                if (ValidFileExists(f)) {
                    progressCompleted(100);
                    return f;
                }
            }

            // is the standard file in the msi folder?
            if (!String.IsNullOrEmpty(MsiFolder)) {
                f = Path.Combine(MsiFolder, filename);

                if (ValidFileExists(f)) {
                    progressCompleted(100);
                    return f;
                }
            }
            // try the MSI for the regular file 
            f = GetFileFromMSI(filename);
            if (ValidFileExists(f)) {
                progressCompleted(100);
                return f;
            }

            //------------------------
            // LOCALIZED FILE, REMOTE
            //------------------------

            // try localized file off the bootstrap server
            if (!String.IsNullOrEmpty(BootstrapServerUrl.Value)) {
                f = AsyncDownloader.Download(BootstrapServerUrl.Value, localizedName, progressCompleted);
                if (ValidFileExists(f)) {
                    progressCompleted(100);
                    return f;
                }
            }

            // try localized file off the coapp server
            f = AsyncDownloader.Download(CoAppUrl, localizedName, progressCompleted);
            if (ValidFileExists(f)) {
                progressCompleted(100);
                return f;
            }

            // try normal file off the bootstrap server
            if (!String.IsNullOrEmpty(BootstrapServerUrl.Value)) {
                f = AsyncDownloader.Download(BootstrapServerUrl.Value, filename, progressCompleted);
                if (ValidFileExists(f)) {
                    progressCompleted(100);
                    return f;
                }
            }

            // try normal file off the coapp server
            f = AsyncDownloader.Download(CoAppUrl, filename, progressCompleted);

            if (ValidFileExists(f)) {
                progressCompleted(100);
                return f;
            }

            Logger.Warning("NOT FOUND:" + filename);
            return null;
        }

        private static string GetFileFromMSI(string binaryFile) {
            var packageDatabase = 0;
            var view = 0;
            var record = 0;

            if (String.IsNullOrEmpty(MsiFilename)) {
                return null;
            }

            try {
                if (0 != NativeMethods.MsiOpenDatabase(binaryFile, IntPtr.Zero, out packageDatabase)) {
                    return null;
                }
                if (0 != NativeMethods.MsiDatabaseOpenView(packageDatabase, String.Format("SELECT `Data` FROM `Binary` where `Name`='{0}'", binaryFile), out view)) {
                    return null;
                }
                if (0 != NativeMethods.MsiViewExecute(view, 0)) {
                    return null;
                }
                if (0 != NativeMethods.MsiViewFetch(view, out record)) {
                    return null;
                }

                var bufferSize = NativeMethods.MsiRecordDataSize(record, 1);
                if (bufferSize > 1024*1024*1024 || bufferSize == 0) {
                    //bigger than 1Meg?
                    return null;
                }

                var byteBuffer = new byte[bufferSize];

                if (0 != NativeMethods.MsiRecordReadStream(record, 1, byteBuffer, ref bufferSize)) {
                    return null;
                }

                // got the whole file
                var tempFilenme = Path.Combine(Path.GetTempPath(), binaryFile);
                File.WriteAllBytes(tempFilenme, byteBuffer);
                return tempFilenme;
            } finally {
                if (record != 0) {
                    NativeMethods.MsiCloseHandle(record);
                }
                if (view != 0) {
                    NativeMethods.MsiCloseHandle(view);
                }
                if (packageDatabase != 0) {
                    NativeMethods.MsiCloseHandle(packageDatabase);
                }
            }
        }

        internal static bool ValidFileExists(string fileName) {
            Logger.Message("Checking for file: " + fileName);
            if (!String.IsNullOrEmpty(fileName) && File.Exists(fileName)) {
                try {
#if DEBUG
                    Logger.Message("   Validity RESULT (assumed): True");
                    return true;
#else
                    var wtd = new WinTrustData(fileName);
                    var result = NativeMethods.WinVerifyTrust(new IntPtr(-1), new Guid("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}"), wtd);
                    Logger.Message("    RESULT (a): " + (result == WinVerifyTrustResult.Success));
                    return (result == WinVerifyTrustResult.Success);
#endif
                } catch {
                }
            }
            Logger.Message("    RESULT (a): False");
            return false;
        }

        public static string GetSpecialFolderPath(KnownFolder folderId) {
            var ret = new StringBuilder(260);
            try {
                var output = NativeMethods.SHGetSpecialFolderPath(IntPtr.Zero, ret, folderId);

                if (!output) {
                    return null;
                }
            } catch /* (Exception e) */ {
                return null;
            }
            return ret.ToString();
        }

        public static string ProgramFilesAnyFolder {
            get {
                var root = CoAppRootFolder.Value;
                var programFilesAny = GetSpecialFolderPath(KnownFolder.ProgramFiles);

                var any = Path.Combine(root, "program files");

                if (Environment.Is64BitOperatingSystem) {
                    Symlink.MkDirectoryLink(Path.Combine(root, "program files (x64)"), programFilesAny);
                }

                Symlink.MkDirectoryLink(Path.Combine(root, "program files (x86)"), GetSpecialFolderPath(KnownFolder.ProgramFilesX86) ?? GetSpecialFolderPath(KnownFolder.ProgramFiles));
                Symlink.MkDirectoryLink(any, programFilesAny);

                Logger.Message("Returing '{0}' as program files directory", any);
                return any;
            }
        }

        internal static readonly Lazy<string> CoAppRootFolder = new Lazy<string>(() => {
            var result = GetRegistryValue(@"Software\CoApp", "Root");

            if (String.IsNullOrEmpty(result)) {
                result = GetSpecialFolderPath(KnownFolder.CommonApplicationData);
                try {
                    var registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).CreateSubKey(@"Software\CoApp");
                    if (registryKey != null) {
                        registryKey.SetValue("Root", result);
                    }
                } catch {
                }
            }

            if (!Directory.Exists(result)) {
                Directory.CreateDirectory(result);
            }

            return result;
        });

        // we need to keep this around, otherwise the garbage collector gets triggerhappy and cleans up the delegate before the installer is done.
        private static NativeExternalUIHandler uihandler;
        private static int _actualPercent;

        internal static void InstallCoApp() {
            InstallTask = Task.Factory.StartNew(() => {
                try {
                    Logger.Warning("Started Toolkit Installer");
                    // Thread.Sleep(4000);
                    NativeMethods.MsiSetInternalUI(2, IntPtr.Zero);
                    NativeMethods.MsiSetExternalUI((context, messageType, message) => 1, 0x400, IntPtr.Zero);

                    if (!Cancelling) {
                        var file = MsiFilename;

                        // if this is the CoApp MSI, we don't need to fetch the CoApp MSI.
                        if (!IsCoAppToolkitMSI(MsiFilename)) {
                            // get coapp.toolkit.msi
                            file = AcquireFile("CoApp.msi", percentDownloaded => CoAppPackageDownload.Progress = percentDownloaded);
                            CoAppPackageDownload.Progress = 100;

                            if (!IsCoAppToolkitMSI(file)) {
                                MainWindow.Fail(LocalizedMessage.IDS_UNABLE_TO_ACQUIRE_COAPP_INSTALLER, "Unable to download the CoApp Installer MSI");
                                return;
                            }
                        }

                        // if you have an incompatible version of CoApp, we need to block on removing it.
                        if (IsIncompatibleCoAppInstalled) {
                            
                            var okToProceed = new ManualResetEvent(false);
                            MainWindow.WhenReady += () => {
                                MainWindow.MainWin.Opacity = 0;
                                var answer = new PopupQuestion(
                                    @"The CoApp Package Manager you have installed is incompatible 
with the latest availible packages, and must be removed and 
replaced with a newer version in order to continue.

This will remove the existing CoApp installation and all packages 
that are currently installed.", "Stop, don't continue", "Yes, Upgrade CoApp").ShowDialog() == true;
                                
                                if( !answer ) {
                                    ExitQuick();
                                }
                                else {
                                    okToProceed.Set();
                                    MainWindow.MainWin.Opacity = 1;
                                }
                            };

                            okToProceed.WaitOne();

                            // bring down the cleaner, and let it do the nasty.
                            var cleanerExe = AcquireFile("coapp.cleaner.exe");
                            if( string.IsNullOrEmpty(cleanerExe)) {
                                MainWindow.Fail(LocalizedMessage.IDS_UNABLE_TO_ACQUIRE_COAPP_CLEANER, "Unable to download the CoApp Cleaner Utility.");
                                return;
                            }

                            var cleanerProc = Process.Start(cleanerExe, "--auto");
                            if (cleanerProc != null) {
                                cleanerProc.WaitForExit();
                            }

                            if (IsIncompatibleCoAppInstalled) {
                                // we've failed to clean out the old version of CoApp.
                                MainWindow.Fail(LocalizedMessage.IDS_UNABLE_TO_CLEAN_COAPP, "Unable to clean out the old versions of CoApp.");
                                return;
                            }

                            // by this time, the old versions of coapp should be removed. 
                        }

                        // We made it past downloading.

                        // bail if someone has told us to. (good luck!)
                        if (Cancelling) {
                            return;
                        }

                        // get a reference to the delegate. 
                        uihandler = UiHandler;
                        NativeMethods.MsiSetExternalUI(uihandler, 0x400, IntPtr.Zero);

                        try {
                            var CoAppCacheFolder = Path.Combine(CoAppRootFolder.Value, ".cache", "packages");
                            Directory.CreateDirectory(CoAppCacheFolder);

                            if( MsiCanonicalName.IndexOf(":") > -1 ) {
                                MsiCanonicalName = MsiCanonicalName.Substring(MsiCanonicalName.IndexOf(":") + 1);
                            }

                            var cachedPath = Path.Combine(CoAppCacheFolder, MsiCanonicalName + ".msi");
                            if (!File.Exists(cachedPath)) {
                                File.Copy(file, cachedPath);
                            }
                        } catch (Exception e) {
                            Logger.Error(e);
                        }

                        Logger.Warning("Running MSI");
                        // install CoApp.Toolkit msi. Don't blink, this can happen FAST!
                        var result = NativeMethods.MsiInstallProduct(file, String.Format(@"TARGETDIR=""{0}"" ALLUSERS=1 COAPP=1 REBOOT=REALLYSUPPRESS", ProgramFilesAnyFolder));
                        CoAppPackageInstall.Progress = 100;

                        // set the ui hander back to nothing.
                        NativeMethods.MsiSetExternalUI(null, 0x400, IntPtr.Zero);
                        InstallTask = null; // after this point, all you can do is exit the installer.

                        Logger.Warning("Done Installing MSI rc={0}.", result);

                        // did we succeed?
                        if (result == 0) {
                            // bail if someone has told us to. (good luck!)
                            if (Cancelling) {
                                return;
                            }

                            // we'll not be on the GUI thread when this runs.
                            RunInstaller(false);
                        } else {
                            MainWindow.Fail(LocalizedMessage.IDS_CANT_CONTINUE, "Installation Engine failed to install. o_O");
                        }
                    }
                } catch (Exception e) {
                    Logger.Error(e);
                    MainWindow.Fail(LocalizedMessage.IDS_SOMETHING_ODD, "This can't be good.");
                } finally {
                    InstallTask = null;
                }
                // if we got to this point, kinda feels like we should be failing
            });

            InstallTask.ContinueWith((it) => {
                Exception e = it.Exception;
                if (e != null) {
                    while (e.GetType() == typeof (AggregateException)) {
                        e = ((e as AggregateException).Flatten().InnerExceptions[0]);
                    }

                    Logger.Error(e);
                    Logger.Error(e.StackTrace);
                    MainWindow.Fail(LocalizedMessage.IDS_SOMETHING_ODD, "This can't be good.");
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        internal static int UiHandler(IntPtr context, int messageType, string message) {
            if ((0xFF000000 & (uint)messageType) == 0x0A000000 && message.Length >= 2) {
                int i;
                var msg = message.Split(": ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(each => Int32.TryParse(each, out i) ? i : 0).ToArray();

                switch (msg[1]) {
                        // http://msdn.microsoft.com/en-us/library/aa370354(v=VS.85).aspx
                    case 0: //Resets progress bar and sets the expected total number of ticks in the bar.
                        _currentTotalTicks = msg[3];
                        _currentProgress = 0;
                        if (msg.Length >= 6) {
                            _progressDirection = msg[5] == 0 ? 1 : -1;
                        }
                        break;
                    case 1:
                        //Provides information related to progress messages to be sent by the current action.
                        break;
                    case 2: //Increments the progress bar.
                        if (_currentTotalTicks == -1) {
                            break;
                        }
                        _currentProgress += msg[3]*_progressDirection;
                        break;
                    case 3:
                        //Enables an action (such as CustomAction) to add ticks to the expected total number of progress of the progress bar.
                        break;
                }
            }

            if (_currentTotalTicks > 0) {
                CoAppPackageInstall.Progress = _currentProgress*100/_currentTotalTicks;
            }

            // if the cancel flag is set, tell MSI
            return Cancelling ? 2 : 1;
        }

        private static string MsiCanonicalName;

        internal static bool IsCoAppToolkitMSI(string filename) {
            if (!ValidFileExists(filename)) {
                return false;
            }

            // First, check to see if the msi we've got *is* the coapp.toolkit msi file :)
            var cert = new X509Certificate2(filename);
            // CN=OUTERCURVE FOUNDATION, OU=CoApp Project, OU=Digital ID Class 3 - Microsoft Software Validation v2, O=OUTERCURVE FOUNDATION, L=Redmond, S=Washington, C=US
            if (cert.Subject.StartsWith("CN=OUTERCURVE FOUNDATION") && cert.Subject.Contains("OU=CoApp Project")) {
                int hProduct;
                if (NativeMethods.MsiOpenPackageEx(filename, 1, out hProduct) == 0) {
                    var sb = new StringBuilder(1024);

                    uint size = 1024;
                    NativeMethods.MsiGetProperty(hProduct, "ProductName", sb, ref size);

                    size = 1024;
                    var sb2 = new StringBuilder(1024);
                    NativeMethods.MsiGetProperty(hProduct, "CanonicalName", sb2, ref size);

                    size = 1024;
                    var sb3 = new StringBuilder(1024);
                    NativeMethods.MsiGetProperty(hProduct, "ProductVersion", sb3, ref size);

                    NativeMethods.MsiCloseHandle(hProduct);

                    var pkgVersion = VersionStringToUInt64(sb3.ToString());

                    if ( pkgVersion >= MIN_COAPP_VERSION && sb.ToString().ToLower().Equals("coapp")) {
                        MsiCanonicalName = sb2.ToString();
                        return true;
                    }
                }
            }
            return false;
        }
    }
}