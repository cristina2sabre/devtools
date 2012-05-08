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
        AssemblyHasNoName,

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
}