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

namespace CoApp.Autopackage {
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Developer.Toolkit.Publishing;
    using Packaging.Common.Model.Atom;
    using Properties;
    using Toolkit.DynamicXml;
    using Toolkit.Exceptions;
    using Toolkit.Extensions;
    using Toolkit.Tasks;

    internal class WixDocument {
        private dynamic wix;
        private XDocument wixXml;
        internal PackageSource Source;
        internal AutopackageModel Model;
        internal AtomFeed Feed;

        private dynamic TargetDir;

        private dynamic VendorDir {
            get {
                return FindOrCreateDirectory(TargetDir, Model.Vendor.MakeAttractiveFilename());
            }
        }

        private dynamic ProductDir {
            get {
                return FindOrCreateDirectory(VendorDir, Model.CanonicalName.PackageName.MakeAttractiveFilename());
            }
        }

        internal WixDocument(PackageSource source, AutopackageModel model, AtomFeed feed) {
            Source = source;
            Model = model;
            Feed = feed;
        }

        public void FillInTemplate() {
            // -----------------------------------------------------------------------------------------------------------------------------------
            // Fill in the package template

            // we need to be in the directory where we're making the wxs file.
            // since we're playing with relative file paths in the wxs source.
            using (var popd = new PushDirectory(FilesystemExtensions.TempPath)) {
                using (var reader = new StringReader(Model.WixTemplate)) {
                    wixXml = XDocument.Load(reader);
                }

                wix = new DynamicNode(wixXml);

                SetBasicWixProperties();

                // add all the files
                AddFilesToWix();

                // add the assemblies to the package.
                AddAssembliesToWix();

                // add the bootstrappers
                AddBootstrappersToWix();

                // add feed icon images 
                AddFeedIcons();

                //Add the CoApp Properties
                AddCoAppProperties();
            }
        }

        public void SetBasicWixProperties() {
            wix.Product.Attributes.Id = Model.CanonicalName.ToString().CreateGuid();
            wix.Product.Attributes.Manufacturer = Model.Vendor;
            wix.Product.Attributes.Name = Model.Name;
            wix.Product.Attributes.Version = Model.Version.ToString();

            TargetDir = wix.Product["Id=TARGETDIR"];
        }

        private void AddBootstrappersToWix() {
            // "CoappBootstrapNativeBin"
            var foldersWeCareAbout = new string[] {
                Path.GetDirectoryName(Assembly.GetEntryAssembly().Location.GetFullPath()),
                Environment.CurrentDirectory,
                Path.GetDirectoryName(Source.SourceFile)
            };

            var coappBootstrapNativeBin = wix.Product["Id=native_bootstrap.exe"];
            if (coappBootstrapNativeBin != null) {
                var bootstrapTempFile = "native-bootstrap.exe".GetFileInTempFolder();

                foreach( var folder in foldersWeCareAbout ) {
                    var file = Path.Combine(folder, "native-bootstrap.exe");
                    if (File.Exists(file)) {
                        File.Copy(file, bootstrapTempFile, true);
                        break;
                    }
                }

                if (!File.Exists(bootstrapTempFile)) {
                    using (var fs = File.Create(bootstrapTempFile)) {
                        fs.Write(Resources.coapp_native_bootstrap, 0, Resources.coapp_native_bootstrap.Length);
                    }
                }

                Binary.Load(bootstrapTempFile, BinaryLoadOptions.NoManaged).Continue(binary => {
                    binary.SigningCertificate = Source.Certificate;
                    binary.CompanyName = Model.Vendor;
                    binary.Comments = "Installer for " + Model.DisplayName;
                    binary.ProductName = "Installer for " + Model.DisplayName;
                    binary.AssemblyTitle = "Installer for " + Model.DisplayName;
                    binary.AssemblyDescription = "Installer for " + Model.DisplayName;
                    binary.LegalCopyright = Model.PackageDetails.CopyrightStatement;
                    binary.ProductVersion = Model.Version.ToString();
                    binary.FileVersion = Model.Version.ToString();
                    binary.FileVersion = Model.Version.ToString();
                    binary.ProductVersion = Model.Version.ToString();
                    return binary.Save();
                }).Wait();
                
                coappBootstrapNativeBin.Attributes.SourceFile = bootstrapTempFile;
            }

            var coappBootstrapBin = wix.Product["Id=managed_bootstrap.exe"];
            if (coappBootstrapBin != null) {
                var managedBootstrapTemporaryFile = "managed_bootstrap.exe".GetFileInTempFolder();

                foreach (var folder in foldersWeCareAbout) {
                    var file = Path.Combine(folder, "managed-bootstrap.exe");
                    if (File.Exists(file)) {
                        File.Copy(file, managedBootstrapTemporaryFile, true);
                        break;
                    }
                }

                if (!File.Exists(managedBootstrapTemporaryFile)) {
                    using (var fs = File.Create(managedBootstrapTemporaryFile)) {
                        fs.Write(Resources.coapp_managed_bootstrap, 0, Resources.coapp_managed_bootstrap.Length);
                    }
                }

                // resign the file
                Binary.Load(managedBootstrapTemporaryFile, BinaryLoadOptions.NoManaged).Continue(binary => {
                    binary.SigningCertificate = Source.Certificate;
                    binary.CompanyName = Model.Vendor;
                    binary.Comments = "Installer for " + Model.DisplayName;
                    binary.ProductName = "Installer for " + Model.DisplayName;
                    binary.AssemblyTitle = "Installer for " + Model.DisplayName;
                    binary.AssemblyDescription = "Installer for " + Model.DisplayName;
                    binary.LegalCopyright = Model.PackageDetails.CopyrightStatement;
                    binary.FileVersion = Model.Version.ToString();
                    binary.ProductVersion = Model.Version.ToString();
                    binary.ProductVersion = Model.Version.ToString();
                    binary.FileVersion = Model.Version.ToString();
                    return binary.Save();
                }).Wait();
                coappBootstrapBin.Attributes.SourceFile = managedBootstrapTemporaryFile;
            }
        }

        private void AddFeedIcons() {
            if (Model.IconImage != null) {
                AddIcon("DEFAULT", Model.IconImage);
            }

            foreach (var k in Model.ChildIcons.Keys) {
                try {
                    using (var srcStream = new MemoryStream(Convert.FromBase64String(Model.ChildIcons[k]))) {
                        AddIcon(k, Image.FromStream(srcStream));
                    }
                } catch  {
                    // Console.WriteLine("{0} --- {1}", e.Message,e.StackTrace);
                }
            }
        }

        private void AddIcon(string name, Image img) {
            if (img.Width > 256 || img.Height > 256) {
                var widthIsConstraining = img.Width > img.Height;
                // Prevent using images internal thumbnail
                img.RotateFlip(RotateFlipType.Rotate180FlipNone);
                img.RotateFlip(RotateFlipType.Rotate180FlipNone);
                var newWidth = widthIsConstraining ? 256 : img.Width*256/img.Height;
                var newHeight = widthIsConstraining ? img.Height*256/img.Width : 256;
                var newImage = img.GetThumbnailImage(newWidth, newHeight, null, IntPtr.Zero);
                img.Dispose();
                img = newImage;
            }

            var tmpImage = (name + ".png").GetFileInTempFolder();
            img.Save(tmpImage, ImageFormat.Png);
            var icon = wix.Product.Add("Binary");

            icon.Attributes.Id = "ICON_{0}".format(name);
            icon.Attributes.SourceFile = tmpImage;
        }

        private dynamic AddNewComponent(dynamic parentDirectory, bool? isPrimary = null) {
            var guid = Guid.NewGuid();

            var componentRef = wix.Product["Id=ProductFeature"].Add("ComponentRef");
            componentRef.Attributes.Id = guid.ComponentId();
            if (isPrimary.HasValue) {
                componentRef.Attributes.Primary = isPrimary == true ? "yes" : "no";
            }
            var component = parentDirectory.Add("Component");
            component.Attributes.Id = guid.ComponentId();
            component.Attributes.Guid = guid.ToString("B");

            return component;
        }

        private dynamic FindOrCreateDirectory(dynamic parentDirectory, string subFolderPath) {
            if (string.IsNullOrEmpty(subFolderPath)) {
                return parentDirectory;
            }

            var path = subFolderPath;
            string childPath = null;
            var i = subFolderPath.IndexOf('\\');
            if (i > -1) {
                path = subFolderPath.Substring(0, i);
                childPath = subFolderPath.Substring(i + 1);
            }

            var immediateSubdirectory = parentDirectory["Name=" + path];

            if (immediateSubdirectory == null) {
                immediateSubdirectory = parentDirectory.Add("Directory");
                // immediateSubdirectory.Attributes.Id = path.MakeSafeDirectoryId() + (subFolderPath.MD5Hash());
                string pid = parentDirectory.Attributes.Id.ToString();

                immediateSubdirectory.Attributes.Id = "dir_" + ((pid + subFolderPath).MD5Hash());

                immediateSubdirectory.Attributes.Name = path;
            }

            if (! string.IsNullOrEmpty(childPath)) {
                return FindOrCreateDirectory(immediateSubdirectory, childPath);
            }

            return immediateSubdirectory;
        }

        private void AddFilesToWix() {
            var folders = Model.DestinationDirectoryFiles.Select(each => Path.GetDirectoryName(each.DestinationPath)).Distinct();
            foreach (var folder in folders) {
                var directoryElement = FindOrCreateDirectory(ProductDir, folder);
                var component = AddNewComponent(directoryElement);
                var filesInFolder = Model.DestinationDirectoryFiles.Where(each => Path.GetDirectoryName(each.DestinationPath) == folder).ToArray();
                var first = true;

                foreach (var file in filesInFolder) {
                    var filename = Path.GetFileName(file.DestinationPath);
                    var newFile = component.Add("File");
                    // newFile.Attributes.Id = filename.MakeSafeDirectoryId() + (folder.MD5Hash());

                    // GS01: Autopackage should detect collisions in destination files.
                    newFile.Attributes.Id = "file_" + ((filename + folder).MD5Hash());

                    newFile.Attributes.Name = filename;
                    if (first) {
                        newFile.Attributes.KeyPath = "yes";
                    }
                    newFile.Attributes.DiskId = "1";
                    /*
                    var localPath = Path.GetFileName(file.SourcePath).GetFileInTempFolder();
                    if (!File.Exists(localPath)) {
                        File.Copy(file.SourcePath, localPath);
                    }
                     */
                    // newFile.Attributes.Source = Path.GetFileName(file.SourcePath);

                    newFile.Attributes.Source = file.SourcePath;
                    first = false;
                }
            }
        }

        private void AddAssembliesToWix() {
            int index = 0;
            foreach (var assembly in Model.Assemblies.Where(each => each.IsNative || each.IsNativePolicy)) {
                var manifestFilename = assembly.Name + ".manifest";
                var catFilename = assembly.Name + ".cat";
                var cdfFilename = assembly.Name + ".cdf";

                // generate each native assembly Manifest
                var manifestTempFile = manifestFilename.GetFileInTempFolder();
                File.WriteAllText(manifestTempFile, assembly.AssemblyManifest);

                // create the CDF from the manifest
                // GAH: Stupid workaround.
                // since we need the dsig hashes in there, we need to have the files all in the same spot as the damn manifest
                foreach (var file in assembly.Files) {
                    var destPath = Path.Combine(Path.GetDirectoryName(manifestTempFile), Path.GetFileName(file.SourcePath));
                    if (!File.Exists(destPath)) {
                        File.Copy(file.SourcePath, destPath);
                    }
                }

                var rc = Tools.ManifestTool.Exec("-manifest \"{0}\" -hashupdate -makecdfs", manifestTempFile);
                if (rc != 0) {
                    throw new CoAppException("Unable to create manifest: [mt.exe -manifest \"{0}\" -hashupdate -makecdfs]".format(manifestTempFile));
                }

                // create the CAT from the CDF
                rc = Tools.MakeCatalog.Exec("\"{0}.cdf\"", manifestTempFile);
                if (rc != 0) {
                    throw new CoAppException("Unable to create catalog: [makecat.exe \"{0}.cdf\"]".format(manifestTempFile));
                }

                // sign the CAT file 
                var catfile = manifestTempFile.ChangeFileExtensionTo(".cat");
                Binary.Load(catfile).ContinueWith(antecedent => {
                    antecedent.Result.SigningCertificate = Source.Certificate;
                    antecedent.Result.Save().Wait();
                }, TaskContinuationOptions.AttachedToParent).Wait();

                // add manifest to wix document 
                var component = AddNewComponent(ProductDir, true);
                var newFile = component.Add("File");
                var assemblyManifestId = "X" + manifestFilename.MakeSafeDirectoryId().MD5Hash();
                newFile.Attributes.Id = assemblyManifestId;
                newFile.Attributes.Name = manifestFilename;
                newFile.Attributes.Source = Path.GetFileName(manifestTempFile);
                newFile.Attributes.Vital = "yes";
                newFile.Attributes.DiskId = "1";

                if (assembly.IsNativePolicy) {
                    newFile.Attributes.KeyPath = "yes";
                    newFile.Attributes.AssemblyManifest = assemblyManifestId;
                    newFile.Attributes.Assembly = "win32";
                }

                // add the catalog to the WIX document
                newFile = component.Add("File");
                newFile.Attributes.Id = "X" + catFilename.MakeSafeDirectoryId().MD5Hash();
                newFile.Attributes.Name = catFilename;
                newFile.Attributes.Source = Path.GetFileName(catfile);
                newFile.Attributes.DiskId = "1";
                newFile.Attributes.Vital = "yes";

                bool first = true;
                // add the files to the WIX document
                foreach (var file in assembly.Files) {
                    var filename = file.DestinationPath; //  Path.GetFileName(file.SourcePath);

                    // copy every file local. 
                    //var localPath = Path.Combine(Path.GetDirectoryName(manifestTempFile), Path.GetFileName(file.SourcePath));
                    //if (!File.Exists(localPath)) {
                    //  File.Copy(file.SourcePath, localPath);
                    //}

                    newFile = component.Add("File");
                    newFile.Attributes.Id = "X" + (index++) + filename.MakeSafeDirectoryId().MD5Hash();
                    newFile.Attributes.Name = filename;
                    newFile.Attributes.DiskId = "1";
                    newFile.Attributes.Source = file.SourcePath;
                    // newFile.Attributes.Source = Path.GetFileName(file.SourcePath);
                    newFile.Attributes.Vital = "yes";

                    if (first) {
                        newFile.Attributes.KeyPath = "yes";
                        newFile.Attributes.AssemblyManifest = assemblyManifestId;
                        newFile.Attributes.Assembly = "win32";
                    }

                    first = false;
                }
            }

            /*foreach (var nativePolicyAssembly in Model.Assemblies.Where(each => each.IsNativePolicy)) {
                // generate each native assembly Manifest
                // create the CDF from the manifest
                // create the CAT from the CDF
                // sign the CAT file 
                // add the files to the WIX document
            }*/

            foreach (var managedAssembly in Model.Assemblies.Where(each => each.IsManaged)) {
                // var component = AddNewComponent(ProductDir, true);
                bool first = true;

                var folders = managedAssembly.Files.Select(each => Path.GetDirectoryName(each.DestinationPath)).Distinct();
                foreach (var folder in folders) {
                    var directoryElement = FindOrCreateDirectory(ProductDir, folder);
                    var component = AddNewComponent(directoryElement);
                    var filesInFolder = managedAssembly.Files.Where(each => Path.GetDirectoryName(each.DestinationPath) == folder).ToArray();

                    foreach (var fileEntry in filesInFolder) {
                        var filename = Path.GetFileName(fileEntry.DestinationPath);
                        var newFile = component.Add("File");
                        newFile.Attributes.Id = "X" + ((folder + filename).MakeSafeDirectoryId()).MD5Hash();

                        newFile.Attributes.Name = filename;
                        if (first) {
                            newFile.Attributes.KeyPath = "yes";
                            newFile.Attributes.Assembly = ".net";
                            newFile.Attributes.AssemblyManifest = newFile.Attributes.Id;
                        }

                        newFile.Attributes.DiskId = "1";
                        /*
                        // copy every file local. 
                        var localPath = Path.GetFileName(file).GetFileInTempFolder();
                        if (!File.Exists(localPath)) {
                            File.Copy(file, localPath);
                        }
                        newFile.Attributes.Source = Path.GetFileName(localPath);
                        */
                        newFile.Attributes.Source = fileEntry.SourcePath;

                        first = false;
                    }
                }
                // any policy files needed?
                // foreach( var policy in Model)
            }
        }

        private void AddCoAppProperties() {
            var feed = Feed.ToString().FormatWithMacros(Source.PropertySheets.First().GetMacroValue, null);
            var property = wix.Product.Add("Property", feed);
            property.Attributes.Id = "CoAppPackageFeed";

            //property = wix.Product.Add("Property", Model.CompositionRules.ToXml("CompositionRules").FormatWithMacros(Source.PropertySheets.First().GetMacroValue, null));
            //property.Attributes.Id = "CoAppCompositionRules";

            property = wix.Product.Add("Property", Model.CompositionData.ToXml("CompositionData").FormatWithMacros(Source.PropertySheets.First().GetMacroValue, null));
            property.Attributes.Id = "CoAppCompositionData";

            property = wix.Product.Add("Property", Model.CanonicalName);
            property.Attributes.Id = "CanonicalName";
        }

        public string CreatePackageFile(string msiFilename) {
            // -----------------------------------------------------------------------------------------------------------------------------------
            // Run WiX to generate the MSI

            // at the end of the step, if there are any errors, let's print them out and exit now.

            // Set the namespace on all the elements in the doc. :(
            XNamespace wixNS = "http://schemas.microsoft.com/wix/2006/wi";
            foreach (var n in wixXml.Descendants()) {
                n.Name = wixNS + n.Name.LocalName;
            }

            Event<Verbose>.Raise("Generated WixFile\r\n\r\n{0}", wixXml.ToString());

            // file names
            var wixfile = (Path.GetFileNameWithoutExtension(msiFilename) + ".wxs").GetFileInTempFolder();
            var wixobj = wixfile.ChangeFileExtensionTo("wixobj");

            msiFilename.TryHardToDelete();

            // Write out the WixFile
            wixXml.Save(wixfile);

            // Compile the Wix File
            Event<Verbose>.Raise("==> Compiling Generated Wix Package.");
            var rc = Tools.WixCompiler.Exec(@"-nologo -sw1075 -out ""{0}"" ""{1}"" ", wixobj, wixfile);
            if (rc != 0) {
                Event<Error>.Raise(MessageCode.WixCompilerError, null, "{0}\r\n{1}", Tools.WixCompiler.StandardOut, Tools.WixCompiler.StandardError);
                return null;
            }

            Event<Verbose>.Raise("==> Linking Wix object files into MSI.");

            rc = Tools.WixLinker.Exec(@"-nologo -sw1076  -out ""{0}"" ""{1}""", msiFilename, wixobj);
            if (rc != 0) {
                Event<Error>.Raise(MessageCode.WixLinkerError, null, "{0}\r\n{1}", Tools.WixLinker.StandardOut, Tools.WixLinker.StandardError);
                return null;
            }
            Event<Verbose>.Raise("MSI Generated [{0}].", msiFilename);
            return msiFilename;
        }
    }
}