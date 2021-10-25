
namespace ddNetBackupLib
{
    public class BackupSettings
    {
        public string OutputDirectory { get; init; }
        public CompressionType CompressionType { get; init; }
        public string? CustomCommand { get; init; }
        public string BlockSizeParam { get; init; }
        public bool UseProgress { get; init; }
    }
}
