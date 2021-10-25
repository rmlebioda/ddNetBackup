using System;
using ddNetBackupLib;

namespace ddNetBackupCli
{
    class Program
    {
        static void Main(string[] args)
        {
            var library = new BackupLibrary();
            var drives = library.GetDrives();
            Console.WriteLine($"drives: {drives.Length}");
        }
    }
}