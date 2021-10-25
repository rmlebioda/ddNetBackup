using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ddNetBackupLib
{
    public class BackupDrivesCommandObserver
    {
        private BackupLibrary _backupLibrary;
        private BackupSettings _backupSettings;
        private ICollection<Drive> _drives;

        private Action<object, string>? _subscribeLambda;
        private Action<object, int>? _onCompleteLambda;

        private IEnumerable<Task> _currentlyRunningTasks = new List<Task>();

        private BackupDrivesCommandObserver(BackupLibrary backupLibrary, ICollection<Drive> drives)
        {
            _backupLibrary = backupLibrary;
            _drives = drives;
            _subscribeLambda = null;
            _onCompleteLambda = null;
        }
        
        internal BackupDrivesCommandObserver(BackupLibrary backupLibrary, ICollection<Drive> drives, BackupSettings backupSettings)
            : this(backupLibrary, drives)
        {
            _backupSettings = backupSettings;
        }

        /// <summary>
        /// Subscribes to command output
        /// </summary>
        /// <param name="subscribeLambda">Action to be executed when new output appears on output (informs about output for given Drive)</param>
        /// <returns>this</returns>
        public BackupDrivesCommandObserver Subscribe(Action<object, string> subscribeLambda)
        {
            _subscribeLambda = subscribeLambda;
            return this;
        }

        /// <summary>
        /// Subscribes to command execution
        /// </summary>
        /// <param name="onCompleteLambda">Action to be executed when execution finishes (informs about exit code for given Drive)</param>
        /// <returns>this</returns>
        public BackupDrivesCommandObserver OnComplete(Action<object, int> onCompleteLambda)
        {
            _onCompleteLambda = onCompleteLambda;
            return this;
        }

        /// <summary>
        /// Executes backup command asynchronously for next drive in queue
        /// </summary>
        /// <returns>Class representing if backup execution started, command, that is being executed and executing Task</returns>
        public DriveBackupStatus BackupNextDrive(bool clearSetActions)
        {
            var drive = _drives.FirstOrDefault();
            if (drive == null)
            {
                return new DriveBackupStatus {Started = false, Command = string.Empty, Task = null, Drive = null};
            }

            _drives.Remove(drive);

            var command = !string.IsNullOrEmpty(_backupSettings.CustomCommand)
                ? _backupSettings.CustomCommand
                : _backupLibrary.GetBackupCommand(_backupSettings);
            command = string.Format(command, drive.PartitionFullPath, GenerateFileBackupPath(drive));

            var newTask = _backupLibrary._commandExecutor.ExecuteCommandAsync(command, drive,
                _subscribeLambda, _subscribeLambda, _onCompleteLambda);
            _currentlyRunningTasks = _currentlyRunningTasks.Append(newTask);

            if (clearSetActions)
            {
                _subscribeLambda = null;
                _onCompleteLambda = null;
            }

            return new DriveBackupStatus {Started = true, Command = command, Task = newTask, Drive = drive};
        }

        /// <summary>
        /// Returns drives, that are pending to be backed
        /// </summary>
        /// <returns>List of drives, that has not been yet backed</returns>
        public IEnumerable<Drive> PendingDrives()
        {
            return _drives;
        }

        private string GenerateFileBackupPath(Drive drive)
        {
            var filePath = Path.Combine(_backupSettings.OutputDirectory,
                drive.PartitionName + GetFileExtension(_backupSettings.CompressionType));
            var nextFreeFileNameIter = 2;
            while (File.Exists(filePath))
            {
                filePath = Path.Combine(_backupSettings.OutputDirectory,
                    drive.PartitionName + "_" + nextFreeFileNameIter + GetFileExtension(_backupSettings.CompressionType));
                nextFreeFileNameIter++;
            }
            
            return filePath;
        }

        private static string GetFileExtension(CompressionType compressionType)
        {
            return compressionType switch
            {
                CompressionType.GZip => ".gz",
                CompressionType.BZip2 => ".bz2",
                CompressionType.None => ".raw",
                _ => ".bcp"
            };
        }
    }
}