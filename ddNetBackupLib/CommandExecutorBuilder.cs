using System;
using ddNetBackupLib.Core;
using ddNetBackupLib.Exception;

namespace ddNetBackupLib
{
    class CommandExecutorBuilder
    {
        internal IBackupCommandExecutor Build()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    return new UnixCommandExecutor();
                default:
                    throw new UnsupportedOsException(Environment.OSVersion.Platform);
            }
        }
    }
}