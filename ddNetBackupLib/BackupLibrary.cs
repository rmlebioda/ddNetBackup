using System.Collections.Generic;
using ddNetBackupLib.Core;

namespace ddNetBackupLib
{
    public sealed class BackupLibrary
    {
        internal readonly IBackupCommandExecutor _commandExecutor;
        
        public BackupLibrary()
        {
            _commandExecutor = new CommandExecutorBuilder()
                .Build();
        }

        /// <summary>
        /// Returns list of drives in system.
        /// </summary>
        public Drive[] GetDrives()
        {
            return _commandExecutor.GetDrives();
        }

        /// <summary>
        /// Returns command to be executed with provided backup settings.
        /// </summary>
        /// <param name="settings">Backup settings</param>
        /// <returns>Executable commands, with parameters {0} being drive, {1} output path.</returns>
        public string GetBackupCommand(BackupSettings settings)
        {
            return _commandExecutor.GetBackupCommand(settings);
        }

        /// <summary>
        /// Creates command observer for drives backup
        /// </summary>
        /// <param name="drives">Drives to be backed</param>
        /// <param name="settings">Backup settings</param>
        /// <returns>Command observer, that allows asynchronous execution and notifications</returns>
        public BackupDrivesCommandObserver CreateCommandObserver(ICollection<Drive> drives, BackupSettings settings)
        {
            return new BackupDrivesCommandObserver(this, drives, settings);
        }

        /// <summary>
        /// Returns disks and partitions information via command "fdisk -l"
        /// </summary>
        /// <returns>Output of command "fdisk -l"</returns>
        public string GetDisksInformation()
        {
            return _commandExecutor.GetDisksInformation();
        }
    }
}
