using System;
using System.Collections.Generic;
using ddNetBackupLib;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace ddNetBackupGuiGtk.Views
{
    internal partial class MainWindow
    {
#pragma warning disable 649
        [UI] private Button _nextButtonS2;
        [UI] private Button _previousButtonS2;
        [UI] private CheckButton _backupProgressStatus;
        [UI] private ComboBox _compressionTypeComboBox;
        [UI] private CheckButton _customCommand;
        [UI] private TextView _customCommandTextView;
        [UI] private CheckButton _saveDiskPartitionsInformationCheckBoxButton;
        [UI] private CheckButton _runInParallel;
#pragma warning restore 649
        
        private ICollection<Drive> _selectedDrives = new List<Drive>();

        private void MainWindow_S2()
        {
            _previousButtonS2.Clicked += GoToPreviousStack1;
            _backupProgressStatus.Clicked += ChangedBackupProgressDdSettings;
            _customCommand.Clicked += ChangedBackupProgressDdSettings;
            _nextButtonS2.Clicked += GoToNextStack3;
            _customCommand.Active = false;
            _customCommandTextView.Editable = false;
            _saveDiskPartitionsInformationCheckBoxButton.Active = true;
            _runInParallel.Active = true;
            _backupProgressStatus.Active = true;
            InitCompressionComboBox();
            _compressionTypeComboBox.Changed += ChangedCompressionType;
            UpdateCommandTextView();
            MainWindow_S3();
        }

        private void InitCompressionComboBox()
        {
            var textRenderer = new CellRendererText();
            _compressionTypeComboBox.PackStart(textRenderer, true);
            _compressionTypeComboBox.AddAttribute(textRenderer, "text", 0);
            var comboStore = new ListStore(typeof(string));
            foreach (var compression in CompressionList)
            {
                comboStore.AppendValues(compression);
            }

            _compressionTypeComboBox.Model = comboStore;
            _compressionTypeComboBox.Active = 1;
        }

        private void ChangedCompressionType(object sender, EventArgs e)
        {
            UpdateCommandTextView();
        }
        
        private void GoToNextStack3(object sender, EventArgs e)
        {
            try
            {
                if (_customCommand.Active)
                {
                    ValidateCustomCommand(_customCommandTextView.Buffer.Text);
                }
            }
            catch (Exception exception)
            {
                ShowMessageDialog(MessageType.Error, ButtonsType.Ok, exception.Message);
                return;
            }
            _stack.TransitionType = StackTransitionType.SlideLeft;
            _stack.VisibleChild = _stack.Children[Stack3Index];
            _ = ExecuteDriveBackupsAsync();
        }

        private void ChangedBackupProgressDdSettings(object sender, EventArgs e)
        {
            UpdateCommandTextView();
        }

        private void GoToPreviousStack1(object sender, EventArgs e)
        {
            _stack.TransitionType = StackTransitionType.SlideRight;
            _stack.VisibleChild = _stack.Children[Stack1Index];
        }
        
        private void UpdateCommandTextView()
        {
            if (_customCommand.Active)
            {
                _customCommandTextView.Editable = true;
                return;                
            }
            else
            {
                _customCommandTextView.Editable = false;
            }
            
            _customCommandTextView.Buffer.Text = FormatCustomCommand(_backupLibrary.GetBackupCommand(CreateBackupSettings()));
        }

        private BackupSettings CreateBackupSettings()
        {
            return new BackupSettings
            {
                UseProgress = _backupProgressStatus.Active,
                BlockSizeParam = "1M",
                CompressionType = GetCompressionType(),
                CustomCommand = (_customCommand.Active ? _customCommandTextView.Buffer.Text : null),
                OutputDirectory = _backupDirDestination.Text
            };
        }

        private CompressionType GetCompressionType()
        {
            return _compressionTypeComboBox.Active switch
            {
                1 => CompressionType.GZip,
                2 => CompressionType.BZip2,
                _ => CompressionType.None
            };
        }

        private static string FormatCustomCommand(string command)
        {
            return command
                .Replace("{0}", CustomCommandDriveParameter, StringComparison.Ordinal)
                .Replace("{1}", CustomCommandOutputPathParameter, StringComparison.Ordinal);
        }

        private static void ValidateCustomCommand(string command)
        {
            if (!command.Contains(CustomCommandDriveParameter, StringComparison.InvariantCulture))
            {
                throw new ArgumentException("Invalid command, failed to find parameter " + CustomCommandDriveParameter);
            }
            if (!command.Contains(CustomCommandOutputPathParameter, StringComparison.InvariantCulture))
            {
                throw new ArgumentException("Invalid command, failed to find parameter " + CustomCommandOutputPathParameter);
            }
        }

        private string DeformatCustomCommand()
        {
            var command = _customCommandTextView.Buffer.Text;
            ValidateCustomCommand(command);
            return command
                .Replace(CustomCommandDriveParameter, "{0}", StringComparison.Ordinal)
                .Replace(CustomCommandOutputPathParameter, "{1}", StringComparison.Ordinal);
        }
    }
}