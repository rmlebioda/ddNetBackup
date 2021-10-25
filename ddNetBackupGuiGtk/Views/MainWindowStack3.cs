using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ddNetBackupLib;
using Gtk;
using UI = Gtk.Builder.ObjectAttribute;

namespace ddNetBackupGuiGtk.Views
{
    partial class MainWindow
    {
#pragma warning disable 649
        [UI] private Grid _executionGridOfExpanders;
        [UI] private Button _cancelNextBackupButton;
        [UI] private Label _executionStatusLabel;
#pragma warning restore 649

        private BackupDrivesCommandObserver _backupDrivesCommandObserver;
        private IEnumerable<DriveBackupStatus> _executionBackupStatuses = new List<DriveBackupStatus>();
        private bool _executionIsCancelRequested;

        private void MainWindow_S3()
        {
            _backupDrivesCommandObserver = null;
            _cancelNextBackupButton.Sensitive = false;
            _cancelNextBackupButton.Clicked += CancelBackupExecution;
            _executionStatusLabel.Text = GetExecutionStatusLabel(ExecutionStatus.Init);
        }

        private void CancelBackupExecution(object sender, EventArgs e)
        {
            _executionIsCancelRequested = true;
            Gtk.Application.Invoke(delegate 
            {
                _cancelNextBackupButton.Label = "Cancel requsted";
                _cancelNextBackupButton.Sensitive = false;
                CleanupCancelledTasks();
            });
        }

        private static void Window_ForbidDeleteEvent(object sender, DeleteEventArgs a)
        {
            a.RetVal = true;
        }
        
        private enum ExecutionStatus { Init, Ongoing, Finished, Cancelled };

        private string GetExecutionStatusLabel(ExecutionStatus executionStatus)
        {
            return executionStatus switch
            {
                ExecutionStatus.Finished => "Execution status: Finished!",
                ExecutionStatus.Init => "Execution status: Initialized (waiting for work)",
                ExecutionStatus.Ongoing => "Execution status: Working... (do not close or kill this window)",
                ExecutionStatus.Cancelled => "Execution status: Cancelled by user",
                _ => "Execution status: Unknown"
            };
        }

        private async Task ExecuteDriveBackupsAsync()
        {
            DeleteEvent -= Window_DeleteEvent;
            DeleteEvent += Window_ForbidDeleteEvent;
            _executionStatusLabel.Text = GetExecutionStatusLabel(ExecutionStatus.Ongoing);

            SaveDisksInformation();
            
            _backupDrivesCommandObserver = _backupLibrary.CreateCommandObserver(_selectedDrives, CreateBackupSettings());

            var selectedDrivesCopy = _selectedDrives.ToList();
            Gtk.Application.Invoke(delegate
            {
                foreach (var selectedDrive in selectedDrivesCopy)
                {
                    AddNewExecutionExpander(selectedDrive.PartitionFullPath + " (in queue)");
                }

                _cancelNextBackupButton.Sensitive = true;
                _executionGridOfExpanders.ShowAll();
            });

            DriveBackupStatus backupExecutionStarted;
            do
            {
                _backupDrivesCommandObserver
                    .Subscribe((sender, output) =>
                    {
                        Gtk.Application.Invoke(delegate { AddLineToTextExpander((Drive) sender, output); });
                    })
                    .OnComplete((sender, exitCode) =>
                    {
                        Gtk.Application.Invoke(delegate { BackupCompletionReport((Drive) sender, exitCode); });
                    });
                backupExecutionStarted = _backupDrivesCommandObserver.BackupNextDrive(clearSetActions: true);
                var backupExecutionStartedCopy = backupExecutionStarted;
                Gtk.Application.Invoke(delegate { BackupStartedExecutionReport(backupExecutionStartedCopy.Drive); });
                _executionBackupStatuses = _executionBackupStatuses.Append(backupExecutionStarted);
                if (!_runInParallel.Active && backupExecutionStarted?.Task is not null)
                {
                    await backupExecutionStarted.Task;
                }
            } while (backupExecutionStarted.Started && !_executionIsCancelRequested);

            await AwaitParallelExecutionAsync();
        }

        private async Task AwaitParallelExecutionAsync()
        {
            if (_runInParallel.Active)
            {
                foreach (var backupStatus in _executionBackupStatuses)
                {
                    if (backupStatus?.Task is not null)
                    {
                        await backupStatus.Task;
                    }
                }
            }
        }

        private void SaveDisksInformation()
        {
            if (_saveDiskPartitionsInformationCheckBoxButton.Active)
            {
                try
                {
                    var ouptut = _backupLibrary.GetDisksInformation();
                    var filePath = GetUniqueFile(_backupDirDestination.Text, "fdisks.txt");
                    File.WriteAllText(filePath, ouptut);
                }
                catch (Exception e)
                {
                    ShowMessageDialog(MessageType.Error, ButtonsType.Close,
                        "Failed to save disks information due to error: " + e.Message);
                }
            }
        }

        private string GetUniqueFile(string directory, string fullFileName)
        {
            var path = System.IO.Path.Combine(directory, fullFileName);
            int i = 2;
            while (File.Exists(path))
            {
                path = System.IO.Path.Combine(directory,
                    System.IO.Path.GetFileName(fullFileName) + "_" + i + System.IO.Path.GetExtension(fullFileName));
            }

            return path;
        }


        private void AddNewExecutionExpander(string expanderTitle)
        {
            var textView = new TextView(new TextBuffer(new TextTagTable()));
            textView.Hexpand = true;
            textView.Editable = false;
            var expander = new Expander(expanderTitle);
            expander.Add(textView);
            var expanderChildren = _executionGridOfExpanders.Children;
            if (expanderChildren.Length == 0)
            {
                _executionGridOfExpanders.Add(expander);
            }
            else
            {
                _executionGridOfExpanders.AttachNextTo(expander,
                    _executionGridOfExpanders.Children[0], PositionType.Bottom, 1, 1);
            }
        }

        private void AddLineToTextExpander(Drive drive, string text)
        {
            var textView = FindExecutionExpanderTextViewBy(drive);
            if (textView is not null)
            {
                AppendTextToTextViewWithBufferLength(textView, text);
            }
        }

        private TextView FindExecutionExpanderTextViewBy(Drive drive)
        {
            var expander = FindExecutionExpanderBy(drive);
            if (expander is null)
            {
                Console.WriteLine($"Failed to find children with expander label matching partition path: {drive.PartitionFullPath}");
                return null;
            }
            if (expander.Child is not TextView)
            {
                Console.WriteLine($"Fatal error, expected TextView as the only expander child, but got {expander.Child.GetType()}");
                return null;
            }

            return (TextView) expander.Child;
        }

        private Expander FindExecutionExpanderBy(Drive drive)
        {
            return (Expander)_executionGridOfExpanders.Children
                .FirstOrDefault(child => ((Expander) child).Label.StartsWith(drive.PartitionFullPath, StringComparison.Ordinal));
        }

        private static void AppendTextToTextViewWithBufferLength(TextView textView, string textToAppend)
        {
            var newText = textView.Buffer.Text + textToAppend + Environment.NewLine;
            textView.Buffer.Text = newText[Math.Max(0, newText.Length - ExecutionTextViewCharLimit)..];
        }

        private void BackupCompletionReport(Drive drive, int scriptExitStatusCode)
        {
            var expander = FindExecutionExpanderBy(drive);
            if (expander is null)
            {
                return;
            }

            if (scriptExitStatusCode != 0)
            {
                expander.Label = $"{drive.PartitionFullPath} FAILED! Status code: {scriptExitStatusCode}";
            }
            else
            {
                expander.Label = $"{drive.PartitionFullPath} completed with status code: {scriptExitStatusCode}";
            }
            expander.ShowAll();

            foreach (var executionTask in _executionBackupStatuses)
            {
                if (executionTask?.Task?.IsCompleted == false)
                {
                    return;
                }
            }

            DelegatedExecutionFinished();
        }

        private void DelegatedExecutionFinished()
        {
            if (_executionIsCancelRequested)
            {
                _executionStatusLabel.Text = GetExecutionStatusLabel(ExecutionStatus.Cancelled);
            }
            else
            {
                _executionStatusLabel.Text = GetExecutionStatusLabel(ExecutionStatus.Finished);
            }
            
            _cancelNextBackupButton.Clicked -= CancelBackupExecution;
            _cancelNextBackupButton.Clicked += (_, _) => { Application.Quit(); };
            _cancelNextBackupButton.Sensitive = true;
            _cancelNextBackupButton.Label = "Close";
            DeleteEvent += Window_DeleteEvent;
            DeleteEvent -= Window_ForbidDeleteEvent;
        }

        private void BackupStartedExecutionReport(Drive drive)
        {
            if (drive is null)
            {
                return;
            }
            var expander = FindExecutionExpanderBy(drive);
            if (expander is null)
            {
                return;
            }

            expander.Label = $"{drive.PartitionFullPath} is working";
            expander.ShowAll();
        }
        
        private void CleanupCancelledTasks()
        {
            foreach (var drive in _backupDrivesCommandObserver.PendingDrives())
            {
                CancelDriveExecution(drive);
            }
        }

        private void CancelDriveExecution(Drive drive)
        {
            var expander = FindExecutionExpanderBy(drive);
            if (expander is null)
            {
                return;
            }

            expander.Label = $"{drive.PartitionFullPath} has been cancelled by user";
            expander.ShowAll();
        }
    }
}
