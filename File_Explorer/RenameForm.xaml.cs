using System.IO;
using System.Windows;
using MessageBox = System.Windows.MessageBox;

namespace File_Explorer
{
    public partial class RenameForm
    {
        private readonly dynamic? _selectedItem;

        public RenameForm()
        {
            InitializeComponent();
            _selectedItem = MainWindow.Instance?.SelectedItem;
            RenameTxt.Text = _selectedItem?.Name ?? throw new InvalidOperationException();
        }

        private void SubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            string newName = RenameTxt.Text.Trim();

            if (!string.IsNullOrEmpty(newName))
            {
                try
                {
                    // Get the directory of the selected item
                    string parentDirectory = Path.GetDirectoryName(_selectedItem?.Path);

                    // Create the new path with the new name using Path.Combine
                    string newPath = Path.Combine(parentDirectory, newName);

                    if (_selectedItem?.Type == "File")
                    {
                        // Check if the file exists before attempting to move it
                        if (File.Exists(_selectedItem.Path))
                        {
                            // Perform the renaming operation for files
                            File.Move(_selectedItem.Path, newPath);
                        }
                        else
                        {
                            MessageBox.Show("The file does not exist.", "Error");
                        }
                    }
                    else if (_selectedItem?.Type == "Folder")
                    {
                        // Check if the folder exists before attempting to move it
                        if (Directory.Exists(_selectedItem.Path))
                        {
                            // Perform the renaming operation for folders
                            Directory.Move(_selectedItem.Path, newPath);
                        }
                        else
                        {
                            MessageBox.Show("The folder does not exist.", "Error");
                        }
                    }

                    // Update the UI in the main window after renaming
                    MainWindow.Instance?.UpdateFileList(parentDirectory);
                }
                catch (IOException ex)
                {
                    MessageBox.Show($"Error renaming item: {ex.Message}", "Error");
                }
            }

            this.Close();
        }

    }
}