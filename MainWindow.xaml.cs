using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace aesPass
{
    public partial class MainWindow : Window
    {
        private ConfigManager cm;
        private string pps;
        private AesManager am;
        private MenuItem menuItemEdit;
        private MenuItem menuItemEditSave;
        private MenuItem menuItemEditClose;
        private MenuItem menuItemAdd;
        private MenuItem menuItemAddSave;
        private MenuItem menuItemAddClose;

        public MainWindow()
        {
            InitializeComponent();
            cm = new ConfigManager("aesPass.ini");
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

            menuItemEdit = new MenuItem();
            menuItemEdit.Header = "_Edit";
            menuItemEdit.Click += new RoutedEventHandler(this.MenuItem_Edit);
            menuItemEditSave = new MenuItem();
            menuItemEditSave.Header = "_Save";
            menuItemEditSave.Click += new RoutedEventHandler(this.MenuItem_Save);
            menuItemEditClose = new MenuItem();
            menuItemEditClose.Header = "C_lose";
            menuItemEditClose.Click += new RoutedEventHandler(this.MenuItem_Close);

            menuItemAdd = new MenuItem();
            menuItemAdd.Header = "A_dd";
            menuItemAdd.Click += new RoutedEventHandler(this.MenuItem_Add);
            menuItemAddSave = new MenuItem();
            menuItemAddSave.Header = "_Save";
            menuItemAddSave.Click += new RoutedEventHandler(this.MenuItem_Add_Save);
            menuItemAddClose = new MenuItem();
            menuItemAddClose.Header = "C_lose";
            menuItemAddClose.Click += new RoutedEventHandler(this.MenuItem_Add_Close);

            timerTick(null, null);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            topmenu.Items.Add(menuItemAdd);
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
                    Header = directoryPath.Split('\\').Last(),
                    Tag = directoryPath
                };
                subItem.Items.Add(null);
                subItem.Expanded += this.Folder_Expanded;
                DirView.Items.Add(subItem);
            }));

            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += new EventHandler(timerTick);
            timer.Interval = new TimeSpan(0, 0, 900);
            timer.Start();
        }

        private void timerTick(object sender, EventArgs e)
        {
            am = null;
            PasswordWindow pw = new PasswordWindow();
            if ((bool)pw.ShowDialog())
            {
                am = new AesManager(pw.Password);
            }
            else { Close(); }

        }

        private void MenuItem_Open(object sender, RoutedEventArgs e)
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
            topmenu.Items.Remove(menuItemAdd);
            sort.Visibility = Visibility.Collapsed;
            listv.Visibility = Visibility.Collapsed;
            searchInput.Visibility = Visibility.Collapsed;
            addrecordinput.Visibility = Visibility.Visible;
            addrecordinput.Focus();
            topmenu.Items.Add(menuItemAddSave);
            topmenu.Items.Add(menuItemAddClose);

        }

        private void MenuItem_Add_Close(object sender, RoutedEventArgs e)
        {
            topmenu.Items.Remove(menuItemAddSave);
            topmenu.Items.Remove(menuItemAddClose);
            topmenu.Items.Add(menuItemAdd);
            sort.Visibility = Visibility.Visible;
            listv.Visibility = Visibility.Visible;
            searchInput.Visibility = Visibility.Visible;
            addrecordinput.Visibility = Visibility.Collapsed;
        }

        private void MenuItem_Add_Save(object sender, RoutedEventArgs e)
        {
            if (am == null)
            {
                PasswordWindow pw = new PasswordWindow();
                if ((bool)pw.ShowDialog())
                {
                    am = new AesManager(pw.Password);
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
                topmenu.Items.Add(menuItemAdd);
                sort.Visibility = Visibility.Visible;
                listv.Visibility = Visibility.Visible;
                searchInput.Visibility = Visibility.Visible;
                addrecordinput.Visibility = Visibility.Collapsed;

            }
        }

        private void MenuItem_Edit(object sender, RoutedEventArgs e)
        {
            topmenu.Items.Remove(menuItemEdit);
            topmenu.Items.Remove(menuItemEditClose);
            sort.IsReadOnly = false;
            sort.Focus();
            topmenu.Items.Add(menuItemEditSave);
            topmenu.Items.Add(menuItemEditClose);
        }

        private void MenuItem_Save(object sender, RoutedEventArgs e)
        {
            topmenu.Items.Remove(menuItemEditSave);
            topmenu.Items.Remove(menuItemEditClose);
            sort.IsReadOnly = true;

            File.WriteAllText(stBar.Text, am.EncryptBase64(sort.Text));
            topmenu.Items.Add(menuItemEdit);
            topmenu.Items.Add(menuItemEditClose);

        }

        private void MenuItem_Close(object sender, RoutedEventArgs e)
        {
            topmenu.Items.Remove(menuItemEdit);
            topmenu.Items.Remove(menuItemEditClose);
            sort.Text = "";
            sort.IsReadOnly = true;
            topmenu.Items.Add(menuItemAdd);

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
                var subItem = new TreeViewItem()
                {
                    Header = directoryPath.Split('\\').Last(),
                    Tag = directoryPath
                };
                subItem.Items.Add(null);
                subItem.Expanded += this.Folder_Expanded;
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

        private void TreeViewItem_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
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
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListViewItem lvi = (ListViewItem)sender;
            stBar.Text = (string)lvi.Content;

            if (am == null)
            {
                PasswordWindow pw = new PasswordWindow();
                if ((bool)pw.ShowDialog())
                {
                    am = new AesManager(pw.Password);
                }
                else { return; }
            }

            sort.Text = am.DecryptBase64(System.IO.File.ReadAllText((string)lvi.Content));
            topmenu.Items.Remove(menuItemEdit);
            topmenu.Items.Remove(menuItemEditSave);
            topmenu.Items.Remove(menuItemEditClose);
            topmenu.Items.Remove(menuItemAdd);
            sort.IsReadOnly = true;
            topmenu.Items.Add(menuItemEdit);
            topmenu.Items.Add(menuItemEditClose);
        }
        
        private void ListViewItem_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                ListViewItem lvi = (ListViewItem)sender;
                stBar.Text = (string)lvi.Content;

                if (am == null)
                {
                    PasswordWindow pw = new PasswordWindow();
                    if ((bool)pw.ShowDialog())
                    {
                        am = new AesManager(pw.Password);
                    }
                    else { return; }
                }

                sort.Text = am.DecryptBase64(System.IO.File.ReadAllText((string)lvi.Content));
                topmenu.Items.Remove(menuItemEdit);
                topmenu.Items.Remove(menuItemEditSave);
                topmenu.Items.Remove(menuItemEditClose);
                topmenu.Items.Remove(menuItemAdd);
                sort.IsReadOnly = true;
                topmenu.Items.Add(menuItemEdit);
                topmenu.Items.Add(menuItemEditClose);
            }
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
                if (am == null)
                {
                    PasswordWindow pw = new PasswordWindow();
                    if ((bool)pw.ShowDialog())
                    {
                        am = new AesManager(pw.Password);
                    }
                    else { return; }
                }
                grep(searchFile(pps, ""), searchInput.Text).ForEach((Action<string>)(path =>
                {
                    listv.Items.Add(path);
                }));
            }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            var destinationurl = "https://github.com/birdiecode/PassAES";
            var sInfo = new System.Diagnostics.ProcessStartInfo(destinationurl)
            {
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(sInfo);
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            var destinationurl = "https://github.com/birdiecode/PassAES/blob/master/README.md";
            var sInfo = new System.Diagnostics.ProcessStartInfo(destinationurl)
            {
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(sInfo);
        }


    }
}
