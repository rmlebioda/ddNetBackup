using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ddNetBackupLib.Core
{
    internal class BaseCommandExecutor : ICommandExecutor
    {
        async Task ICommandExecutor.ExecuteCommandAsync(string command, object sender,
            Action<object, string>? receivedStdOutput, Action<object, string>? receivedStdError,
            Action<object, int>? onComplete)
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

            if (receivedStdOutput is not null)
            {
                proc.OutputDataReceived += (_, args) => { receivedStdOutput(sender, args.Data ?? string.Empty); };
            }
            if (receivedStdError is not null)
            {
                proc.ErrorDataReceived += (_, args) => { receivedStdError(sender, args.Data ?? string.Empty); };
            }

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            await proc.WaitForExitAsync();
            onComplete?.Invoke(sender, proc.ExitCode);
        }
    }
}
