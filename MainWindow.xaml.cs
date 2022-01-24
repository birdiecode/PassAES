using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace aesPass
{
    public partial class MainWindow : Window
    {
        private ConfigManager cm;
        private string pps;
        private AesManager am;
        private MenuItem menuItem1;
        private MenuItem menuItem2;
        private MenuItem menuItem3;
        private MenuItem menuItemAddSave;
        private MenuItem menuItemAddClose;




        public MainWindow()
        {
            InitializeComponent();
            cm = new("aesPass.ini");
            pps = cm.GetPrivateString("main", "path");
            if (pps == "")
            {
                var dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;
                CommonFileDialogResult result = dialog.ShowDialog();
                if (result == CommonFileDialogResult.Ok)
                {
                    pps = dialog.FileName;
                    cm.WritePrivateString("main", "path", dialog.FileName);
                }
                else { Close(); }
            }
            sort.IsReadOnly = true;
            this.Loaded += MainWindow_Loaded;

        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            menuItem3 = new MenuItem();
            menuItem3.Header = "Add";
            menuItem3.Click += new RoutedEventHandler(this.MenuItem_Add);
            topmenu.Items.Add(menuItem3);
            DirView.Items.Clear();

            var directories = new List<String>();

            try
            {
                var dirs = Directory.GetDirectories(pps);

                if (dirs.Length > 0)
                    directories.AddRange(dirs);
            }
            catch { }

            directories.ForEach((Action<string>)(directoryPath =>
            {
                var subItem = new TreeViewItem()
                {
                    Header = directoryPath,
                    Tag = directoryPath
                };
                subItem.Items.Add(null);
                subItem.Expanded += this.Folder_Expanded;
                DirView.Items.Add(subItem);
            }));
            menuItemAddSave = new MenuItem();
            menuItemAddSave.Header = "Save";
            menuItemAddSave.Click += new RoutedEventHandler(this.MenuItem_Add_Save);
            menuItemAddClose = new MenuItem();
            menuItemAddClose.Header = "Close";
            menuItemAddClose.Click += new RoutedEventHandler(this.MenuItem_Add_Close);
        }

        private void MenuItem_Add_Close(object sender, RoutedEventArgs e)
        {
            topmenu.Items.Remove(menuItemAddSave);
            topmenu.Items.Remove(menuItemAddClose);
            topmenu.Items.Add(menuItem3);
            sort.Visibility = Visibility.Visible;
            listv.Visibility = Visibility.Visible;
            searchInput.Visibility = Visibility.Visible;
            addrecordinput.Visibility = Visibility.Collapsed;
        }

        private void MenuItem_Add_Save(object sender, RoutedEventArgs e)
        {
            if (am == null)
            {
                PasswordWindow pw = new();
                if ((bool)pw.ShowDialog())
                {
                    am = new(pw.Password);
                }
                else { return; }
            }

            SaveFileDialog dialog = new SaveFileDialog()
            {
                Filter = "Text Files(*.aes)|*.aes|All(*.*)|*"
            };

            if (dialog.ShowDialog() == true)
            {
                File.WriteAllText(dialog.FileName, am.EncryptBase64(addrecordinput.Text));
                topmenu.Items.Remove(menuItemAddSave);
                topmenu.Items.Remove(menuItemAddClose);
                topmenu.Items.Add(menuItem3);
                sort.Visibility = Visibility.Visible;
                listv.Visibility = Visibility.Visible;
                searchInput.Visibility = Visibility.Visible;
                addrecordinput.Visibility = Visibility.Collapsed;

            }
        }

        private void Folder_Expanded(object sender, RoutedEventArgs e)
        {
            var item = (TreeViewItem)sender;
            if (item.Items.Count != 1 && item.Items[0] != null)
                return;
            item.Items.Clear();

            var fullPath = (string)item.Tag;

            var directories = new List<String>();
            try
            {
                var dirs = Directory.GetDirectories(fullPath);

                if (dirs.Length > 0)
                    directories.AddRange(dirs);
            }
            catch { }
            directories.ForEach((Action<string>)(directoryPath =>
            {
                // Create directory item
                var subItem = new TreeViewItem()
                {
                    // Set header as folder name
                    Header = directoryPath,
                    // Add tag as full path
                    Tag = directoryPath
                };

                // Add dummy item so we can expand folder
                subItem.Items.Add(null);

                // Handle expanding
                subItem.Expanded += this.Folder_Expanded;

                // Add this item to the parent
                item.Items.Add(subItem);
            }));
        }

        private void TreeViewItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem tvi = (TreeViewItem)sender;
            e.Handled = true;

            stBar.Text = (string)tvi.Tag;
            listv.Items.Clear();
            var files = new List<String>();
            try
            {
                var fs = Directory.GetFiles((string)tvi.Tag);

                if (fs.Length > 0)
                    files.AddRange(fs);
            }
            catch { }
            files.ForEach((Action<string>)(filePath =>
            {
                listv.Items.Add(filePath);
            }));
            
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem lvi = (ListViewItem)sender;
            stBar.Text = (string)lvi.Content;

            PasswordWindow pw = new();
            if ((bool)pw.ShowDialog())
            {
                am = new(pw.Password);
            } else { return; }

            sort.Text = am.DecryptBase64(System.IO.File.ReadAllText((string)lvi.Content));
            topmenu.Items.Remove(menuItem1);
            topmenu.Items.Remove(menuItem2);
            topmenu.Items.Remove(menuItem3);
            menuItem1 = new MenuItem();
            menuItem1.Header = "Edit";
            menuItem1.Click += new RoutedEventHandler(this.MenuItem_Edit);
            topmenu.Items.Add(menuItem1);
            menuItem2 = new MenuItem();
            menuItem2.Header = "Close";
            menuItem2.Click += new RoutedEventHandler(this.MenuItem_Close);
            topmenu.Items.Add(menuItem2);
        }

        private void MenuItem_Edit(object sender, RoutedEventArgs e)
        {
            topmenu.Items.Remove(menuItem1);
            topmenu.Items.Remove(menuItem2);
            sort.IsReadOnly = false;
            sort.Focus();
            menuItem1 = new MenuItem();
            menuItem1.Header = "Save";
            menuItem1.Click += new RoutedEventHandler(this.MenuItem_Save);
            topmenu.Items.Add(menuItem1);
            topmenu.Items.Add(menuItem2);
        }

        private void MenuItem_Save(object sender, RoutedEventArgs e)
        {
            topmenu.Items.Remove(menuItem1);
            topmenu.Items.Remove(menuItem2);
            sort.IsReadOnly = true;

            File.WriteAllText(stBar.Text, am.EncryptBase64(sort.Text));

            menuItem1 = new MenuItem();
            menuItem1.Header = "Edit";
            menuItem1.Click += new RoutedEventHandler(this.MenuItem_Edit);
            topmenu.Items.Add(menuItem1);
            menuItem2 = new MenuItem();
            menuItem2.Header = "Close";
            menuItem2.Click += new RoutedEventHandler(this.MenuItem_Close);
            topmenu.Items.Add(menuItem2);

        }

        private void MenuItem_Close(object sender, RoutedEventArgs e)
        {
            topmenu.Items.Remove(menuItem1);
            topmenu.Items.Remove(menuItem2);
            sort.Text = "";
            sort.IsReadOnly = true;
            topmenu.Items.Add(menuItem3);

        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                pps = dialog.FileName;
                cm.WritePrivateString("main", "path", dialog.FileName);
                MainWindow_Loaded(null, null);
            }
        }

        private void MenuItem_Add(object sender, RoutedEventArgs e)
        {
            topmenu.Items.Remove(menuItem3);
            sort.Visibility = Visibility.Collapsed;
            listv.Visibility = Visibility.Collapsed;
            searchInput.Visibility = Visibility.Collapsed;
            addrecordinput.Visibility = Visibility.Visible;
            addrecordinput.Focus();
            topmenu.Items.Add(menuItemAddSave);
            topmenu.Items.Add(menuItemAddClose);

        }

        private List<String> searchFile(String path, String filter)
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            List<String> listFiles = new List<String>();

            try
            {
                var files = dir.GetFiles("*" + filter + "*.aes").ToArray();

                foreach (var directory in dir.GetDirectories())
                {
                    listFiles.AddRange(searchFile(directory.FullName, filter));
                }

                foreach (var file in files)
                {
                    listFiles.Add(file.FullName);
                }
            }
            catch
            {
                MessageBox.Show("error 2");
            }

            return listFiles;
        }

        private List<String> grep(List<String> filesSort, String grep_str)
        {

            foreach (var strfile in filesSort.ToList())
            {
                int amount = new Regex(grep_str).Matches(am.DecryptBase64(System.IO.File.ReadAllText(strfile))).Count;
                if (amount == 0)
                {
                    filesSort.Remove(strfile);
                }
            };
            return filesSort;
        }

        private void searchInput_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                listv.Items.Clear();
                PasswordWindow pw = new();
                if ((bool)pw.ShowDialog())
                {
                    am = new(pw.Password);
                }
                else { return; }
                grep(searchFile(pps, ""), searchInput.Text).ForEach((Action<string>)(path =>
                {
                    listv.Items.Add(path);
                }));

            }
        }
    }
}
