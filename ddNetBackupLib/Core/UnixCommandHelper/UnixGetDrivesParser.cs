using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using ddNetBackupLib.Exception;

namespace ddNetBackupLib.Core.UnixCommandHelper
{
    internal class UnixGetDrivesParser
    {
        private readonly string _commandOutput;
        
        internal UnixGetDrivesParser(string commandOutput)
        {
            _commandOutput = commandOutput;
        }

        internal Drive[] GetDrives()
        {
            List<Drive> drives = new List<Drive>();
            
            foreach (var line in
                _commandOutput.Split(Environment.NewLine).Select((value, i) => new { i, value }))
            {
                var words = line.value.Split().Where(word => word != string.Empty).ToArray();
                if (!IsLineOfWordsValid(words, line.i, line.value))
                    continue;

                var newDrive = new Drive
                (
                    size: ulong.Parse(words[2]) * 1024,
                    partitionName: words[3],
                    partitions: Array.Empty<Drive>()
                );
                drives.Add(newDrive);
            }

            for (var i = 0; i < drives.Count; i++)
            {
                var drive = drives[i];
                if (drive.Partitions.Length > 0)
                    continue;
                
                var drivePartitions = drives
                    .Where(driveItem => driveItem != drive && driveItem.PartitionName.StartsWith(drive.PartitionName, StringComparison.InvariantCulture))
                    .ToArray();
                if (drivePartitions.Length > 0)
                {
                    drive.Partitions = drivePartitions;
                    drives = drives.Except(drivePartitions).ToList();
                    i = -1;
                }
            }

            return drives.ToArray();
        }

        private bool IsLineOfWordsValid(string[] words, int lineNumber, string originalLine)
        {
            if (words.Length == 0)
                return false;
            if (words.Length != 4)
            {
                throw new CommandException(
                    $"Unsupported line output on line {lineNumber} '{originalLine}', expected 4 words");
            }
            if (words[0].ToLower() == "major")
                return false;
            return true;
        }
    }
}