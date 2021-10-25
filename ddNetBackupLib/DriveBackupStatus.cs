using System.Threading.Tasks;

namespace ddNetBackupLib
{
    public class DriveBackupStatus
    {
        public bool Started { get; init; }
        public string? Command { get; init; }
        public Task? Task { get; init; }
        public Drive? Drive { get; init; }
    }
}