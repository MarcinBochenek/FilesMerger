using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FilesMerger
{
    public partial class MainForm : Form
    {
        private List<string> _allowedExtensions = new List<string> { ".cs", ".json", ".ts", ".tsx" };

        // Comprehensive list of commonly ignored folders
        private List<string> _excludedFolders = new List<string>
        { 
            // .NET / Visual Studio
            "bin", "obj", "Properties", ".vs", ".vscode", "packages", "TestResults",
            "Debug", "Release", "x64", "x86", "AnyCPU", ".nuget",
            
            // Node.js / npm
            "node_modules", "npm-debug.log", "yarn-error.log", ".npm", ".yarn",
            
            // Build outputs
            "dist", "build", "out", "target", "coverage", ".next", ".nuxt",
            
            // Version control
            ".git", ".svn", ".hg", ".bzr", "CVS",
            
            // IDEs and editors
            ".idea", ".eclipse", ".vscode", ".settings", "*.swp", "*.swo",
            ".DS_Store", "Thumbs.db", "desktop.ini",
            
            // Logs and temp files
            "logs", "*.log", "tmp", "temp", ".tmp", ".temp", "cache", ".cache",
            
            // Other common folders
            ".sass-cache", ".env.local", ".env.development.local", ".env.test.local", ".env.production.local"
        };

        private string _selectedFolderPath;
        private ImageList _imageList;
        private string _defaultStartPath = @"c:\source\repos\SIGames\SI.Live.Admin.Api";

        public MainForm()
        {
            InitializeComponent();
            InitializeTreeView();
            InitializeExtensionsTextBox();
            InitializeIgnoredFoldersTextBox();

            // Set the initial folder path if it exists
            if (Directory.Exists(_defaultStartPath))
            {
                _selectedFolderPath = _defaultStartPath;
                folderPathTextBox.Text = _selectedFolderPath;
                LoadFileTree(_selectedFolderPath);
            }
        }

        private void InitializeExtensionsTextBox()
        {
            // Set the default extensions in the text box
            // Assuming you have a TextBox named extensionsTextBox
            if (extensionsTextBox != null)
            {
                extensionsTextBox.Text = string.Join(", ", _allowedExtensions);
                // Remove the automatic refresh event handler
                // extensionsTextBox.TextChanged += ExtensionsTextBox_TextChanged;
            }
        }

        private void InitializeIgnoredFoldersTextBox()
        {
            // Initialize ignored folders list - no UI component needed
            // The _excludedFolders list is already initialized with default values
            // Users won't see or modify this list directly
        }

        // This method is no longer needed since we're not auto-refreshing
        // private void ExtensionsTextBox_TextChanged(object sender, EventArgs e)
        // {
        //     // Update the allowed extensions when the text changes
        //     UpdateAllowedExtensions();
        // }

        private void UpdateAllowedExtensions()
        {
            if (extensionsTextBox == null) return;

            try
            {
                // Parse the extensions from the text box
                string[] extensions = extensionsTextBox.Text
                    .Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(ext => ext.Trim())
                    .Where(ext => !string.IsNullOrWhiteSpace(ext))
                    .Select(ext => ext.StartsWith(".") ? ext : "." + ext) // Ensure extensions start with a dot
                    .ToArray();

                _allowedExtensions = extensions.ToList();

                // Remove the automatic refresh - this will now only happen when Refresh button is clicked
                // if (!string.IsNullOrEmpty(_selectedFolderPath) && Directory.Exists(_selectedFolderPath))
                // {
                //     LoadFileTree(_selectedFolderPath);
                // }
            }
            catch (Exception ex)
            {
                // Handle any parsing errors gracefully
                statusLabel.Text = $"Error parsing extensions: {ex.Message}";
            }
        }

        private void UpdateIgnoredFolders()
        {
            // This method is kept for potential future use but not needed 
            // since users can't modify the ignored folders list
            // The list remains static with the predefined ignored folders
        }

        private void InitializeTreeView()
        {
            // Create image list for tree view
            _imageList = new ImageList();
            _imageList.Images.Add("folder", CreateFolderIcon());
            _imageList.Images.Add("file", CreateFileIcon());

            // Set up tree view
            fileTreeView.ImageList = _imageList;
            fileTreeView.CheckBoxes = true;
            fileTreeView.AfterCheck += FileTreeView_AfterCheck;
        }

        // Create a simple folder icon
        private Bitmap CreateFolderIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);

                // Draw folder
                g.FillRectangle(Brushes.Gold, 1, 3, 14, 11);
                g.FillRectangle(Brushes.Goldenrod, 6, 1, 8, 3);
                g.DrawRectangle(Pens.DarkGoldenrod, 1, 3, 14, 11);
                g.DrawRectangle(Pens.DarkGoldenrod, 6, 1, 8, 3);
            }
            return bmp;
        }

        // Create a simple file icon
        private Bitmap CreateFileIcon()
        {
            Bitmap bmp = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);

                // Draw document
                g.FillRectangle(Brushes.White, 2, 1, 11, 14);
                g.DrawRectangle(Pens.Gray, 2, 1, 11, 14);

                // Draw horizontal lines (text)
                g.DrawLine(Pens.LightGray, 4, 4, 11, 4);
                g.DrawLine(Pens.LightGray, 4, 7, 11, 7);
                g.DrawLine(Pens.LightGray, 4, 10, 11, 10);
            }
            return bmp;
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            using (var folderBrowser = new FolderBrowserDialog())
            {
                folderBrowser.Description = "Select a folder to scan for files";
                folderBrowser.ShowNewFolderButton = false;

                // Set the initial directory if it exists
                if (Directory.Exists(_defaultStartPath))
                {
                    folderBrowser.SelectedPath = _defaultStartPath;
                }
                // Otherwise if we already have a path selected, start from there
                else if (!string.IsNullOrEmpty(_selectedFolderPath) && Directory.Exists(_selectedFolderPath))
                {
                    folderBrowser.SelectedPath = _selectedFolderPath;
                }

                if (folderBrowser.ShowDialog() == DialogResult.OK)
                {
                    _selectedFolderPath = folderBrowser.SelectedPath;
                    folderPathTextBox.Text = _selectedFolderPath;
                    LoadFileTree(_selectedFolderPath);
                }
            }
        }

        private void LoadFileTree(string rootPath)
        {
            fileTreeView.Nodes.Clear();
            statusLabel.Text = "Loading file tree...";
            Cursor = Cursors.WaitCursor;

            try
            {
                DirectoryInfo rootDir = new DirectoryInfo(rootPath);
                TreeNode rootNode = CreateDirectoryNode(rootDir);
                if (rootNode != null)
                {
                    fileTreeView.Nodes.Add(rootNode);
                }

                // Expand all nodes in the tree
                fileTreeView.ExpandAll();

                statusLabel.Text = $"Ready - Extensions: {string.Join(", ", _allowedExtensions)} | {_excludedFolders.Count} folders ignored";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading directory: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Error loading file tree";
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private TreeNode CreateDirectoryNode(DirectoryInfo directoryInfo)
        {
            // Enhanced folder checking with pattern matching
            if (IsFolderIgnored(directoryInfo.Name))
            {
                return null;
            }

            var directoryNode = new TreeNode(directoryInfo.Name)
            {
                ImageKey = "folder",
                SelectedImageKey = "folder",
                Tag = directoryInfo.FullName
            };

            try
            {
                // Add subdirectories (filtering out excluded ones)
                foreach (var subDir in directoryInfo.GetDirectories()
                    .Where(d => !IsFolderIgnored(d.Name))
                    .OrderBy(d => d.Name))
                {
                    var childNode = CreateDirectoryNode(subDir);
                    if (childNode != null)
                    {
                        directoryNode.Nodes.Add(childNode);
                    }
                }

                // Add files with allowed extensions
                foreach (var file in directoryInfo.GetFiles()
                    .Where(f => _allowedExtensions.Contains(f.Extension.ToLower()))
                    .OrderBy(f => f.Name))
                {
                    var fileNode = new TreeNode(file.Name)
                    {
                        ImageKey = "file",
                        SelectedImageKey = "file",
                        Tag = file.FullName
                    };
                    directoryNode.Nodes.Add(fileNode);
                }
            }
            catch (UnauthorizedAccessException)
            {
                directoryNode.Text += " (Access Denied)";
            }
            catch (Exception ex)
            {
                directoryNode.Text += $" (Error: {ex.Message})";
            }

            return directoryNode;
        }

        /// <summary>
        /// Check if a folder should be ignored based on the exclusion list
        /// Supports both exact matches and simple pattern matching
        /// </summary>
        private bool IsFolderIgnored(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName)) return false;

            foreach (var excludedPattern in _excludedFolders)
            {
                if (string.IsNullOrWhiteSpace(excludedPattern)) continue;

                // Exact match (case-insensitive)
                if (string.Equals(folderName, excludedPattern, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Simple wildcard pattern matching
                if (excludedPattern.Contains("*"))
                {
                    // Convert simple wildcard to regex
                    string pattern = "^" + excludedPattern.Replace("*", ".*") + "$";
                    if (System.Text.RegularExpressions.Regex.IsMatch(folderName, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    {
                        return true;
                    }
                }

                // Check if folder starts with the pattern (for patterns like ".env")
                if (excludedPattern.StartsWith(".") && folderName.StartsWith(excludedPattern, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // Handle parent/child checkbox checking
        private void FileTreeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            // Prevent infinite recursion
            fileTreeView.AfterCheck -= FileTreeView_AfterCheck;

            try
            {
                // Check all child nodes when parent is checked
                if (e.Action != TreeViewAction.Unknown)
                {
                    CheckAllChildNodes(e.Node, e.Node.Checked);
                }
            }
            finally
            {
                // Restore the event handler
                fileTreeView.AfterCheck += FileTreeView_AfterCheck;
            }
        }

        private void CheckAllChildNodes(TreeNode treeNode, bool nodeChecked)
        {
            foreach (TreeNode node in treeNode.Nodes)
            {
                node.Checked = nodeChecked;
                CheckAllChildNodes(node, nodeChecked);
            }
        }

        private void clearAllButton_Click(object sender, EventArgs e)
        {
            // Temporarily remove event handler to prevent recursive events
            fileTreeView.AfterCheck -= FileTreeView_AfterCheck;

            try
            {
                // Uncheck all nodes
                foreach (TreeNode node in fileTreeView.Nodes)
                {
                    UncheckAllNodes(node);
                }

                statusLabel.Text = "All selections cleared";
            }
            finally
            {
                // Restore the event handler
                fileTreeView.AfterCheck += FileTreeView_AfterCheck;
            }
        }

        private void UncheckAllNodes(TreeNode node)
        {
            // Uncheck this node
            node.Checked = false;

            // Recursively uncheck all child nodes
            foreach (TreeNode childNode in node.Nodes)
            {
                UncheckAllNodes(childNode);
            }
        }

        private void generateButton_Click(object sender, EventArgs e)
        {
            List<string> selectedFiles = GetCheckedFiles(fileTreeView.Nodes);

            if (selectedFiles.Count == 0)
            {
                MessageBox.Show("Please select at least one file to merge.", "No Files Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                string mergedContent = MergeFiles(selectedFiles);
                Clipboard.SetText(mergedContent);

                statusLabel.Text = $"Successfully merged {selectedFiles.Count} files to clipboard.";
                MessageBox.Show($"{selectedFiles.Count} files have been merged and copied to clipboard.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error merging files: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Failed to merge files.";
            }
        }

        private List<string> GetCheckedFiles(TreeNodeCollection nodes)
        {
            var selectedFiles = new List<string>();

            foreach (TreeNode node in nodes)
            {
                // If this is a file node (no children) and it's checked
                if (node.Checked && node.Nodes.Count == 0 && IsAllowedFile(node.Tag.ToString()))
                {
                    selectedFiles.Add(node.Tag.ToString());
                }

                // Recursively check child nodes
                selectedFiles.AddRange(GetCheckedFiles(node.Nodes));
            }

            return selectedFiles;
        }

        private bool IsAllowedFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return _allowedExtensions.Contains(extension);
        }

        private string MergeFiles(List<string> filePaths)
        {
            var stringBuilder = new StringBuilder();

            foreach (var filePath in filePaths)
            {
                stringBuilder.AppendLine($"// File: {filePath}");

                // Read the file content and remove the copyright comment
                string fileContent = File.ReadAllText(filePath);
                string copyrightPattern = @"// --------------------------------------------------------------------------\r?\n// \(c\) Sports Interactive Ltd\.\r?\n// --------------------------------------------------------------------------\r?\n?";
                string cleanedContent = System.Text.RegularExpressions.Regex.Replace(fileContent, copyrightPattern, string.Empty);

                stringBuilder.AppendLine(cleanedContent);
                stringBuilder.AppendLine();
                stringBuilder.AppendLine("// ----------------------");
                stringBuilder.AppendLine();
            }

            return stringBuilder.ToString();
        }

        // Modified refresh button method - now updates extensions before refreshing
        // Ignored folders remain static and don't need updating
        private void refreshButton_Click(object sender, EventArgs e)
        {
            // Update the allowed extensions from the text box
            UpdateAllowedExtensions();

            // Refresh the tree if a folder is selected
            // Ignored folders are automatically applied during tree building
            if (!string.IsNullOrEmpty(_selectedFolderPath) && Directory.Exists(_selectedFolderPath))
            {
                LoadFileTree(_selectedFolderPath);
            }
        }

        // Optional: Add method to programmatically modify ignored folders if needed in the future
        // This could be called from a settings dialog or configuration file
        public void SetIgnoredFolders(List<string> ignoredFolders)
        {
            _excludedFolders = ignoredFolders ?? new List<string>();

            // Refresh the tree if a folder is already loaded
            if (!string.IsNullOrEmpty(_selectedFolderPath) && Directory.Exists(_selectedFolderPath))
            {
                LoadFileTree(_selectedFolderPath);
            }
        }

        // Optional: Get current ignored folders list for external configuration
        public List<string> GetIgnoredFolders()
        {
            return new List<string>(_excludedFolders);
        }
    }
}