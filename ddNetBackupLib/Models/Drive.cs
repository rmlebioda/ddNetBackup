using System;
using ByteSizeLib;

namespace ddNetBackupLib
{
    /// <summary>
    /// Represents drive in system.
    /// </summary>
    public sealed class Drive
    {
        /// <summary>
        /// Total drive size in bytes.
        /// </summary>
        public ulong Size { get; internal set; }
        /// <summary>
        /// Partition name (without preceding path, for example 'sda', 'sdb1', 'nvme0n1').
        /// </summary>
        public string PartitionName { get; internal set; }
        /// <summary>
        /// List of partitions for this drive.
        /// </summary>
        public Drive[] Partitions { get; internal set; }
        
        /// <summary>
        /// Full dev path (fe. '/dev/sdc', '/dev/nvme1n2p1').
        /// </summary>
        public string PartitionFullPath => $"/dev/{PartitionName}";

        internal Drive(ulong size, string partitionName, Drive[] partitions)
        {
            Size = size;
            PartitionName = partitionName;
            Partitions = partitions;
        }

        public string PrettySize(bool useDecimal)
        {
            var size = ByteSize.FromBytes(Convert.ToDouble(Size));
            return useDecimal
                ? $"{size.LargestWholeNumberDecimalValue:0.##} {size.LargestWholeNumberDecimalSymbol}" 
                : $"{size.LargestWholeNumberBinaryValue:0.##} {size.LargestWholeNumberBinarySymbol}";
        }
    }
}