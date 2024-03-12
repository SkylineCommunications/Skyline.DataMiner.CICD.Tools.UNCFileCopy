namespace Skyline.DataMiner.CICD.Tools.UNCFileCopy
{
    using System;
    using System.CommandLine;
    using System.IO;
    using System.Runtime.Versioning;

    /// <summary>
    /// This tool allows a file to be copied to a UNC network share using impersonation.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class Program
    {
        /// <summary>
        /// The entry point for the application.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <returns>0 if successful, 1 if an error occurs.</returns>
        public static int Main(string[] args)
        {
            var sourceFilePath = new Option<string>("--source-file-path", "The source file path.")
            {
                IsRequired = true
            };
            sourceFilePath.LegalFilePathsOnly();

            var uncDestinationDirectory = new Option<string>("--destination-unc-directory", "The UNC path to the destination directory.")
            {
                IsRequired = true
            };

            var destinationFileName = new Option<string>("--destination-file-name", "The file name for the destination, including extension.")
            {
                IsRequired = true
            };
            destinationFileName.LegalFileNamesOnly();

            var addTimeStampSuffix = new Option<bool>("--add-timestamp-suffix", "If true, adds a timestamp suffix (yyyyMMddHHmmss) to the destination filename.")
            {
                IsRequired = false
            };

            addTimeStampSuffix.SetDefaultValue(false);

            var username = new Option<string>("--username", "The username for impersonation.")
            {
                IsRequired = true
            };

            var domain = new Option<string>("--domain", "The domain for impersonation.")
            {
                IsRequired = true
            };

            var password = new Option<string>("--password", "The password for impersonation.")
            {
                IsRequired = true
            };

            var rootCommand = new RootCommand("WINDOWS ONLY - Copies a file to a UNC network share with impersonation.")
            {
                sourceFilePath,
                uncDestinationDirectory,
                destinationFileName,
                username,
                domain,
                password,
                addTimeStampSuffix
            };

            rootCommand.SetHandler(Process, sourceFilePath, uncDestinationDirectory, destinationFileName, domain, username, password, addTimeStampSuffix);

            return rootCommand.Invoke(args);
        }

        /// <summary>
        /// Processes the file copy operation with optional user impersonation.
        /// </summary>
        /// <param name="sourceFilePath">Source file path.</param>
        /// <param name="uncDestinationDirectory">UNC destination directory.</param>
        /// <param name="destinationFileName">Destination file name.</param>
        /// <param name="domain">Domain for impersonation.</param>
        /// <param name="username">Username for impersonation.</param>
        /// <param name="password">Password for impersonation.</param>
        /// <param name="addTimeStampSuffix">Flag to add timestamp suffix to the destination filename.</param>
        private static void Process(string sourceFilePath, string uncDestinationDirectory, string destinationFileName, string domain, string username, string password, bool addTimeStampSuffix)
        {
            try
            {
                // Validate arguments
                if (String.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
                    throw new FileNotFoundException("Source file not found.", sourceFilePath);
                if (String.IsNullOrWhiteSpace(uncDestinationDirectory))
                    throw new ArgumentException("Destination UNC directory must not be empty.", nameof(uncDestinationDirectory));
                if (String.IsNullOrWhiteSpace(destinationFileName))
                    throw new ArgumentException("Destination file name must not be empty.", nameof(destinationFileName));

                if (addTimeStampSuffix)
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(destinationFileName);
                    string extension = Path.GetExtension(destinationFileName);
                    destinationFileName = $"{fileNameWithoutExtension}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                }

                string destinationPath = Path.Combine(uncDestinationDirectory.TrimEnd('\\', '/'), destinationFileName);

                UserImpersonation user = new UserImpersonation(username, domain, password);

                user.RunAsUser(() =>
                {
                    File.Copy(sourceFilePath, destinationPath, true);
                    Console.WriteLine($"Copy succeeded: {sourceFilePath} to {destinationPath}");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Copy failed: {ex.Message}");
                Environment.Exit(1); // Indicate failure
            }
        }
    }
}