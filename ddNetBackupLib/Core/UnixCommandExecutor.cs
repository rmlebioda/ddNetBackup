using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ddNetBackupLib.Core.UnixCommandHelper;
using ddNetBackupLib.Exception;

namespace ddNetBackupLib.Core
{
    internal class UnixCommandExecutor : BaseCommandExecutor, IBackupCommandExecutor
    {
        internal struct CommandOutput
        {
            internal int ExitCode;
            internal string StdOutput;
            internal string StdError;
        }

        private const string GetPartitionsCommand = "cat /proc/partitions";
        
        Drive[] IBackupCommandExecutor.GetDrives()
        {
            var commandOutput = ExecuteBashCommand(GetPartitionsCommand);
            if (commandOutput.ExitCode != 0)
            {
                string errMessage =
                    $"Failed to execute command {GetPartitionsCommand} due to error: {commandOutput.StdError}";
                if (!string.IsNullOrEmpty(commandOutput.StdOutput))
                {
                    errMessage += Environment.NewLine + "Output: " + commandOutput.StdOutput;
                }

                throw new CommandException(errMessage);
            }

            return new UnixGetDrivesParser(commandOutput.StdOutput).GetDrives();
        }
        
        string IBackupCommandExecutor.GetBackupCommand(BackupSettings settings)
        {
            return GetDriveBackupString(settings);
        }

        string IBackupCommandExecutor.GetDisksInformation()
        {
            const string commandToRun = "sudo -A fdisk -l";
            var result = ExecuteBashCommand(commandToRun);
            if (result.ExitCode != 0)
            {
                throw new CommandException(
                    $"Failed to execute command '{commandToRun}' due to error code {result.ExitCode}: {result.StdError}  {result.StdOutput}");
            }

            return result.StdOutput;
        }

        internal static CommandOutput ExecuteBashCommand(string command)
        {
            // according to: https://stackoverflow.com/a/15262019/637142
            command = command.Replace("\"","\"\"", StringComparison.Ordinal);

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \""+ command + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            proc.WaitForExit();

            return new CommandOutput
            {
                ExitCode = proc.ExitCode,
                StdOutput = proc.StandardOutput.ReadToEnd(),
                StdError = proc.StandardError.ReadToEnd()
            };
        }
        
        private static string GetDriveBackupString(BackupSettings backupSettings)
        {
            var result = new StringBuilder("set -o pipefail; sleep 0.1; sudo -A dd if={0}");
            if (backupSettings.UseProgress)
            {
                result.Append(" status=progress");
            }

            if (!string.IsNullOrEmpty(backupSettings.BlockSizeParam))
            {
                result.Append(" bs=" + backupSettings.BlockSizeParam);
            }
            
            if (backupSettings.CompressionType == CompressionType.GZip)
            {
                result.Append(" | gzip");
            }
            else if (backupSettings.CompressionType == CompressionType.BZip2)
            {
                result.Append(" | bzip2");
            }
            result.Append(" > {1}");
            return result.ToString();
        }
    }
}