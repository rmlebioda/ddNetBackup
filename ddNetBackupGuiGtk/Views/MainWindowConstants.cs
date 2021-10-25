using System.Collections.Generic;
using Gdk;

namespace ddNetBackupGuiGtk.Views
{
    internal partial class MainWindow
    {
        private const int DisksTreeToggleColumnIndex = 0;
        private const int DisksTreeToggleColumnWidth = 70;
        private const int DisksTreePartitionNameColumnIndex = 1;
        private const int DisksTreePartitionNameColumnWidth = 240;
        private const int DisksTreeSizeColumnIndex = 2;
        private const int DisksTreeSizeColumnWidth = 80;

        private const int ChooseFolderButtonIconWidth = 20;
        private const int ChooseFolderButtonIconHeight = 20;

        private const int Stack1Index = 0;
        private const int Stack2Index = 1;
        private const int Stack3Index = 2;

        private const int ExecutionTextViewCharLimit = 1000;

        private const string CustomCommandDriveParameter = "{{drive}}";
        private const string CustomCommandOutputPathParameter = "{{output_path}}";
        
        private readonly List<string> CompressionList = new List<string>{"No compression", "GZip", "BZip2"};
    }
}