using System;
using System.Threading.Tasks;

namespace ddNetBackupLib.Core
{
    internal interface ICommandExecutor
    {
        internal Task ExecuteCommandAsync(string command, object sender,
            Action<object, string>? receivedStdOutput,
            Action<object, string>? receivedStdError,
            Action<object, int>? onComplete);
    }
}