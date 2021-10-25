namespace ddNetBackupLib.Core
{
    internal interface IBackupCommandExecutor : ICommandExecutor
    {
        internal Drive[] GetDrives();
        internal string GetBackupCommand(BackupSettings settings);
        internal string GetDisksInformation();
    }
}