// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AzureStorageEmulatorManager.cs" company="Dapr Labs">
//   Copyright © Dapr Labs. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace Orleans.Fabric.Silo
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// The azure storage emulator manager.
    /// </summary>
    public static class AzureStorageEmulator
    {
        /// <summary>
        /// The development storage connection string.
        /// </summary>
        public const string UseDevelopmentStorage = "UseDevelopmentStorage=true";

        /// <summary>
        /// The process command.
        /// </summary>
        private enum ProcessCommand
        {
            /// <summary>
            /// The init command.
            /// </summary>
            Init,
            
            /// <summary>
            /// The start command.
            /// </summary>
            Start,

            /// <summary>
            /// The stop command.
            /// </summary>
            Stop,

            /// <summary>
            /// The status command.
            /// </summary>
            Status
        }

        /// <summary>
        /// Returns true if the azure storage emulator is running.
        /// </summary>
        /// <returns>true if the azure storage emulator is running.</returns>
        public static bool IsProcessRunning()
        {
            bool status;

            using (var process = Process.Start(StorageEmulatorProcessFactory.Create(ProcessCommand.Status)))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Unable to start process.");
                }

                status = GetStatus(process);
                process.WaitForExit();
            }

            return status;
        }

        /// <summary>
        /// Initializes the storage emulator.
        /// </summary>
        public static void Init()
        {
            if (!IsProcessRunning())
            {
                ExecuteProcess(ProcessCommand.Init);
            }
        }

        /// <summary>
        /// Starts the storage emulator.
        /// </summary>
        public static void Start()
        {
            if (!IsProcessRunning())
            {
                ExecuteProcess(ProcessCommand.Start);
            }
        }

        /// <summary>
        /// Stops the storage emulator.
        /// </summary>
        public static void Stop()
        {
            if (IsProcessRunning())
            {
                ExecuteProcess(ProcessCommand.Stop);
            }
        }

        /// <summary>
        /// Executes the specified command.
        /// </summary>
        /// <param name="command">
        /// The command.
        /// </param>
        private static void ExecuteProcess(ProcessCommand command)
        {
            string error;

            using (var process = Process.Start(StorageEmulatorProcessFactory.Create(command)))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Unable to start process.");
                }

                error = GetError(process);
                process.WaitForExit();
            }

            if (!string.IsNullOrEmpty(error))
            {
                throw new InvalidOperationException(error);
            }
        }

        /// <summary>
        /// Returns the process status.
        /// </summary>
        /// <param name="process">
        /// The process.
        /// </param>
        /// <returns>
        /// The process status.
        /// </returns>
        private static bool GetStatus(Process process)
        {
            var output = process.StandardOutput.ReadToEnd();
            var isRunningLine = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).SingleOrDefault(line => line.StartsWith("IsRunning"));

            if (isRunningLine == null)
            {
                return false;
            }

            return bool.Parse(isRunningLine.Split(':').Select(part => part.Trim()).Last());
        }

        /// <summary>
        /// Returns the process error.
        /// </summary>
        /// <param name="process">
        /// The process.
        /// </param>
        /// <returns>
        /// The process error.
        /// </returns>
        private static string GetError(Process process)
        {
            var output = process.StandardError.ReadToEnd();
            return output.Split(':').Select(part => part.Trim()).Last();
        }

        /// <summary>
        /// The storage emulator process factory.
        /// </summary>
        private static class StorageEmulatorProcessFactory
        {
            /// <summary>
            /// The create.
            /// </summary>
            /// <param name="command">
            /// The command.
            /// </param>
            /// <returns>
            /// The <see cref="ProcessStartInfo"/>.
            /// </returns>
            public static ProcessStartInfo Create(ProcessCommand command)
            {
                return new ProcessStartInfo
                {
                    FileName = @"C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\WAStorageEmulator.exe",
                    Arguments = command.ToString().ToLower(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }
        }
    }
}