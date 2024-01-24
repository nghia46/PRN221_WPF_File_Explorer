using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows;
using System.Windows.Media.Imaging;
using MessageBox = System.Windows.MessageBox;
using WinForms = System.Windows.Forms;

namespace File_Explorer
{
    public partial class MainWindow
    {
        public static MainWindow? Instance { get; private set; }
        private readonly Stack<string> _folderHistory = new Stack<string>();
        private readonly Stack<string> _forwardHistory = new Stack<string>();

        public dynamic? SelectedItem => ListView.SelectedItem;

        public MainWindow()
        {
            InitializeComponent();
            UpdateButtonStates();
            Instance = this;
        }

        private void BrowseFileBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedFolder = BrowseForFolder();
            if (string.IsNullOrEmpty(selectedFolder)) return;

            _folderHistory.Push(selectedFolder);
            _forwardHistory.Clear();

            UpdateFileList(selectedFolder);
            UpdateButtonStates();
        }

        private void ForwardBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_forwardHistory.Count > 0)
            {
                var forwardFolder = _forwardHistory.Pop();
                _folderHistory.Push(forwardFolder);
                UpdateFileList(forwardFolder);
                UpdateFilePathIntoTextBox(forwardFolder);
                UpdateButtonStates();
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_folderHistory.Count <= 1) return;
            var currentFolder = _folderHistory.Pop();
            _forwardHistory.Push(currentFolder);

            var previousFolder = _folderHistory.Peek();
            UpdateFileList(previousFolder);
            UpdateFilePathIntoTextBox(previousFolder);
            UpdateButtonStates();
        }

        private string? BrowseForFolder()
        {
            var dialog = new FolderBrowserDialog();
            dialog.SelectedPath = @"";
            var result = dialog.ShowDialog();
            UpdateFilePathIntoTextBox(dialog.SelectedPath);
            return result == WinForms.DialogResult.OK ? dialog.SelectedPath : null;
        }

        public void UpdateFileList(string? selectedFolder)
        {
            if (selectedFolder == null) return;
            var entries = Directory.GetFileSystemEntries(selectedFolder);
            var fileInfos = entries.Select(entry => CreateFileInfo(entry))
                .OrderByDescending(item => item.Type == "Folder").ToList();
            ListView.ItemsSource = fileInfos;
        }

        private void UpdateFilePathIntoTextBox(string path)
        {
            TxtPath.Text = !string.IsNullOrWhiteSpace(path) ? path : "Invalid Path";
        }

        private static dynamic CreateFileInfo(string entry)
        {
            return new
            {
                Type = GetFileType(entry),
                Name = Path.GetFileName(entry),
                Path = entry,
                Icon = GetFileIcon(entry)
            };
        }

        private static string GetFileType(string path)
        {
            return File.GetAttributes(path).HasFlag(FileAttributes.Directory) ? "Folder" : "File";
        }

        private static BitmapImage GetFileIcon(string path)
        {
            var isDirectory = File.GetAttributes(path).HasFlag(FileAttributes.Directory);
            var iconPath = isDirectory
                ? @"D:\\Repos\\PRN221\\File_Explorer\\File_Explorer\\Resources\\folder.png"
                : @"D:\\Repos\\PRN221\\File_Explorer\\File_Explorer\\Resources\\file.png";
            return new BitmapImage(new Uri(iconPath));
        }

        private void ListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ListView.SelectedItem == null) return;
            var selectedItem = (dynamic)ListView.SelectedItem;

            if (selectedItem.Type == "Folder")
            {
                _folderHistory.Push(selectedItem.Path);
                _forwardHistory.Clear(); 

                UpdateFilePathIntoTextBox(selectedItem.Path);
                UpdateFileList(selectedItem.Path);
                UpdateButtonStates();
            }
            else if (selectedItem.Type == "File")
            {
                OpenFile(selectedItem.Path);
            }
        }

        private static void OpenFile(string filePath)
        {
            var processStartInfo = new ProcessStartInfo(filePath)
            {
                UseShellExecute = true
            };
            Process.Start(processStartInfo);
        }

        private void UpdateButtonStates()
        {
            NewFolderBtn.IsEnabled = _folderHistory.Count > 0;
            RenameBtn.IsEnabled = ListView.SelectedItem != null;
            DeleteFileBtn.IsEnabled = ListView.SelectedItem != null;
            BackBtn.IsEnabled = _folderHistory.Count > 1;
            ForwardBtn.IsEnabled = _forwardHistory.Count > 0;
        }

        private bool IsAdministrator()
        {
            var windowsIdentity = WindowsIdentity.GetCurrent();
            var windowsPrincipal = new WindowsPrincipal(windowsIdentity);

            return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private async void DeleteFileBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ListView.SelectedItem == null) return;

            var result = MessageBox.Show("Do you want to proceed?", "Confirmation", MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;
            var selectedData = (dynamic)ListView.SelectedItem;

            try
            {
                if (IsAdministrator())
                {
                    await RunAsAdministratorAsync(selectedData);
                }
                else
                {
                    MessageBox.Show("You need administrator privileges to delete this file or folder.", "Access Denied",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting file or folder: {ex.Message}", "Error");
            }
        }

        private async Task RunAsAdministratorAsync(dynamic selectedData)
        {
            string path = selectedData.Path;

            var isDirectory = File.GetAttributes(path).HasFlag(FileAttributes.Directory);

            var command = isDirectory ? $"cmd /c rmdir /s /q \"{path}\"" : $"cmd /c del \"{path}\"";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                Verb = "runas",
                UseShellExecute = true,
                CreateNoWindow = true,
            };

            using (var process = new Process())
            {
                process.StartInfo = processStartInfo;
                process.Start();
                await process.WaitForExitAsync();
            }

            var directoryPath = Path.GetDirectoryName(path);
            UpdateFileList(directoryPath);
            if (directoryPath != null) UpdateFilePathIntoTextBox(directoryPath);
            UpdateButtonStates();
        }


        private void ListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateButtonStates();
        }

        private void RenameBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ListView.SelectedItem == null)
            {
                MessageBox.Show("Please select an item to rename.", "Information", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var renameWindow = new RenameForm
            {
                Owner = this, 
                WindowStartupLocation = WindowStartupLocation.CenterOwner 
            };
            renameWindow.ShowDialog();
        }

        private void NewFolderBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var currentFolder = GetCurrentFolder();
            try
            {
                var newFolderName = GetUniqueFolderName(currentFolder, "New Folder");

                // Combine the current path with the new folder name
                var newFolderPath = Path.Combine(currentFolder, newFolderName);

                // Create the new folder
                Directory.CreateDirectory(newFolderPath);

                // Update the file list and UI
                UpdateFileList(currentFolder);
                UpdateFilePathIntoTextBox(currentFolder);
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating new folder: {ex.Message}", "Error");
            }
        }

        private string GetCurrentFolder()
        {
            return TxtPath.Text;
        }

        private static string GetUniqueFolderName(string directoryPath, string baseName)
        {
            var newFolderName = baseName;
            var counter = 1;

            // Generate a unique folder name by appending a counter until a unique name is found
            while (Directory.Exists(Path.Combine(directoryPath, newFolderName)))
            {
                newFolderName = $"{baseName} ({counter++})";
            }

            return newFolderName;
        }
    }
}