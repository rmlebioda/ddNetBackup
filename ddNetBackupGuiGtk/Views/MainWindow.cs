using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using ByteSizeLib;
using ddNetBackupLib;
using Gdk;
using GLib;
using Gtk;
using Application = Gtk.Application;
using UI = Gtk.Builder.ObjectAttribute;
using Window = Gtk.Window;

namespace ddNetBackupGuiGtk.Views
{
    internal partial class MainWindow : Window
    {
        // disables "not assigned" warning, as gtk is assigning them under the hood in Autoconnect method,
        // so warning is not valid here 
#pragma warning disable 649
        [UI] private TreeView _disksTree;
        [UI] private Entry _backupDirDestination;
        [UI] private Button _backupDirDestBtn;
        [UI] private Label _selectedBackupsRawSizeLabel;
        [UI] private Button _nextButton;
        [UI] private Stack _stack;
#pragma warning restore 649

        private readonly BackupLibrary _backupLibrary;
        private readonly ICollection<Drive> _drives;

        private static bool IsAdministrator =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                new WindowsPrincipal(WindowsIdentity.GetCurrent())
                    .IsInRole(WindowsBuiltInRole.Administrator) :
                Mono.Unix.Native.Syscall.geteuid() == 0;
        
        public MainWindow() : this(new Builder("MainWindow.glade")) { }

        private MainWindow(Builder builder) : base(builder.GetRawOwnedObject("MainWindow"))
        {
            builder.Autoconnect(this);

            if (!IsAdministrator)
            {
                if (ShowMessageDialog(MessageType.Warning, ButtonsType.YesNo,
                        "You don't appear to have ran this app with sudo privileges, dd command will most likely not work correctly. Do you still want to continue?")
                    != Gtk.ResponseType.Yes)
                {
                    throw new UnauthorizedAccessException("Insufficient privileges, run this app again with sudo privileges");
                }
            }

            ThrowIfAnyGtkPropIsNull();

            DeleteEvent += Window_DeleteEvent;

            try
            {
                _backupLibrary = new BackupLibrary();
            }
            catch (Exception e)
            {
                ShowMessageDialog(MessageType.Error, ButtonsType.Close, e.Message);
                throw;
            }

            _drives = _backupLibrary.GetDrives().ToList();
            
            LoadWindow();
            
            MainWindow_S2();
            
            this.ShowAll();
        }

        private void ThrowIfAnyGtkPropIsNull()
        {
            var privateWindowProperties = typeof(MainWindow)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(property => property.CustomAttributes
                    .Any(customAttribute => customAttribute.AttributeType == typeof(Gtk.Builder.ObjectAttribute)));

            foreach (var privateProperty in privateWindowProperties)
            {
                if (privateProperty.GetValue(this) is null)
                {
                    throw new ArgumentException($"GTK Window did not initialize properly: internal private property {privateProperty.Name} is null");
                }
            }
        }
        
        private void LoadWindow()
        {
            var toggleCell = new CellRendererToggle();
            toggleCell.Toggled += ToggleDrive;
            _disksTree.AppendColumn(GetNewColumn(toggleCell, string.Empty, "active",
                DisksTreeToggleColumnWidth, columnIndex: DisksTreeToggleColumnIndex, false).Item1);
            _disksTree.AppendColumn(GetNewColumn(new CellRendererText(), "partition", "text",
                DisksTreePartitionNameColumnWidth, DisksTreePartitionNameColumnIndex, true).Item1);
            _disksTree.AppendColumn(GetNewColumn(new CellRendererText(), "Disk size", "text",
                DisksTreeSizeColumnWidth, DisksTreeSizeColumnIndex, false).Item1);

            _disksTree.Model = CreateNewStore();
            
            _backupDirDestBtn.Image = new Image(GetBackupDirDestPixbuf());
            _backupDirDestBtn.WidthRequest = ChooseFolderButtonIconWidth;
            _backupDirDestBtn.Clicked += ChooseFolder;

            _nextButton.Clicked += GoToNextStack2;
        }

        private void GoToNextStack2(object sender, EventArgs e)
        {
            if (!Directory.Exists(_backupDirDestination.Text))
            {
                ShowMessageDialog(MessageType.Error, ButtonsType.Ok, 
                    $"Directory '{_backupDirDestination.Text}' doesn't exists or you don't have sufficient permissions!");
                return;
            }

            _selectedDrives = GetSelectedDrives();
            if (_selectedDrives.Count == 0)
            {
                ShowMessageDialog(MessageType.Error, ButtonsType.Ok, 
                    "You need to select at least one drive to backup.");
                return;
            }

            _stack.TransitionType = StackTransitionType.SlideLeft;
            _stack.VisibleChild = _stack.Children[Stack2Index];
        }

        private ICollection<Drive> GetSelectedDrives()
        {
            var result = new List<Drive>();
            if (!_disksTree.Model.GetIterFirst(out var treeIter))
            {
                return result;
            }

            do
            {
                result.AddRange(GetSelectedNodeChildren(treeIter));
            } while (_disksTree.Model.IterNext(ref treeIter));
            return result;
        }

        private IEnumerable<Drive> GetSelectedNodeChildren(TreeIter parentIter)
        {
            var result = new List<Drive>();

            if (!_disksTree.Model.IterChildren(out var childrenIter, parentIter))
            {
                return result;
            }

            do
            {
                if ((bool) DisksTreeModelGetValue(childrenIter, DisksTreeToggleColumnIndex).Val)
                {
                    result.Add(GetDriveByTreeIter(childrenIter));
                }
            } while (_disksTree.Model.IterNext(ref childrenIter));

            return result;
        }

        private static Tuple<TreeViewColumn, CellRenderer> GetNewColumn(CellRenderer cell, string title, string cellAttributeName, int minWidth, int columnIndex, bool expand)
        {
            var partitionColumn = new TreeViewColumn(title, cell);
            partitionColumn.AddAttribute(cell, cellAttributeName, columnIndex);
            partitionColumn.MinWidth = minWidth;
            partitionColumn.Sizing = TreeViewColumnSizing.Autosize;
            partitionColumn.Expand = expand;
            partitionColumn.Resizable = expand;
            return new Tuple<TreeViewColumn, CellRenderer>(partitionColumn, cell);
        }

        private TreeStore CreateNewStore()
        {
            var store = new TreeStore(typeof(bool), typeof(string), typeof(string));
            foreach (var drive in _drives.OrderBy(x => x.PartitionName))
            {
                var treeIter = store.AppendValues(false, drive.PartitionName, drive.PrettySize(true));
                foreach (var partition in drive.Partitions)
                {
                    store.AppendValues(treeIter, false, partition.PartitionName, partition.PrettySize(true));
                }
            }

            return store;
        }
        
        private Pixbuf GetBackupDirDestPixbuf()
        {
            var imageStream = this.GetType().Assembly.GetManifestResourceStream( typeof(MainWindow).Assembly.GetName().Name + ".Resources.folder-open.svg");
            return new Pixbuf(imageStream, ChooseFolderButtonIconWidth, ChooseFolderButtonIconHeight);
        }

        private void ToggleDrive(object widget, ToggledArgs args)
        {
            _disksTree.Model.GetIterFromString(out var treeIter, args.Path);
            var toggle = DisksTreeModelGetValue(treeIter, DisksTreeToggleColumnIndex);
            var newToggleValue = !(bool) toggle.Val;
            _disksTree.Model.SetValue(treeIter, DisksTreeToggleColumnIndex, newToggleValue);
            
            if (_disksTree.Model.IterChildren(out var childrenIter, treeIter))
            {
                do
                {
                    _disksTree.Model.SetValue(childrenIter, DisksTreeToggleColumnIndex, newToggleValue);
                } while (_disksTree.Model.IterNext (ref childrenIter));                
            }
            else
            {
                EnsureCorrectParentToggle(treeIter);
            }

            CalculateSelectedRawBackupSize();
        }

        private void CalculateSelectedRawBackupSize()
        {
            ulong selectedSize = 0;
            if (_disksTree.Model.GetIterFirst(out var treeIter))
            {
                do
                {
                    selectedSize += GetSubtreeSelectedSize(treeIter);
                } while (_disksTree.Model.IterNext(ref treeIter));
            }

            var bytes = ByteSize.FromBytes(selectedSize);
            _selectedBackupsRawSizeLabel.Text = $"{bytes.LargestWholeNumberDecimalValue:0.##} {bytes.LargestWholeNumberDecimalSymbol}";
        }

        private Drive GetDriveByTreeIter(TreeIter treeIter)
        {
            var partitionName = (string) DisksTreeModelGetValue(treeIter, DisksTreePartitionNameColumnIndex).Val;
            return _drives
                .SelectMany(drive => drive.Partitions.Concat(new[] { drive }))
                .First(drive => drive.PartitionName.Equals(partitionName, StringComparison.Ordinal));
        }

        private ulong GetSubtreeSelectedSize(TreeIter nodeIter)
        {
            ulong result = 0;
            if (_disksTree.Model.IterChildren(out var childrenIter, nodeIter))
            {
                do
                {
                    if ((bool) DisksTreeModelGetValue(childrenIter, DisksTreeToggleColumnIndex).Val)
                    {
                        result += GetDriveByTreeIter(childrenIter).Size;
                    }
                } while (_disksTree.Model.IterNext (ref childrenIter));                
            }

            return result;
        }
        
        
        private Value DisksTreeModelGetValue(TreeIter treeIter, int column)
        {
            var returnValue = Value.Empty;
            _disksTree.Model.GetValue(treeIter, column, ref returnValue);
            return returnValue;
        }
        
        private void EnsureCorrectParentToggle(TreeIter treeIter)
        {
            if (!_disksTree.Model.IterParent(out var parentIter, treeIter))
            {
                return;
            }

            var childTogglesStatus = new List<bool>();
            if (_disksTree.Model.IterChildren(out var childrenIter, parentIter))
            {
                do
                {
                    childTogglesStatus.Add((bool) DisksTreeModelGetValue(childrenIter, DisksTreeToggleColumnIndex).Val);
                } while (_disksTree.Model.IterNext (ref childrenIter));
            }
            _disksTree.Model.SetValue(parentIter, DisksTreeToggleColumnIndex, childTogglesStatus.All(toggled => toggled));
        }
        
        private void ChooseFolder(object sender, EventArgs e)
        {
            var dialog = new FileChooserDialog("", this, FileChooserAction.SelectFolder);
            dialog.AddButton(Stock.Cancel, ResponseType.Cancel);
            dialog.AddButton(Stock.Ok, ResponseType.Ok);
            dialog.SelectMultiple = false;
            if ((ResponseType)dialog.Run() == Gtk.ResponseType.Ok)
            {
                _backupDirDestination.Text = dialog.Filename;
            }
            dialog.Destroy();
        }

        private Gtk.ResponseType ShowMessageDialog(MessageType msgType, ButtonsType btnType, string message)
        {
            var md = new MessageDialog(this, DialogFlags.Modal, msgType, btnType, 
                message
                    .Replace("{", "{{", StringComparison.Ordinal)
                    .Replace("}", "}}", StringComparison.Ordinal));
            var response = md.Run();
            md.Destroy();
            return (Gtk.ResponseType) response;
        }
        
        private static void Window_DeleteEvent(object sender, DeleteEventArgs a)
        {
            Application.Quit();
        }
    }
}
