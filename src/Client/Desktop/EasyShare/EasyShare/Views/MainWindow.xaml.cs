using EasyShare.Api;
using EasyShare.Core;
using EasyShare.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;

namespace EasyShare.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ClipboardMonitor cbMon;
        private EasyShareHandler esHandler;
        private MainViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();

            esHandler = new EasyShareHandler("http://localhost:8080/handler.php", "rbl");

            cbMon = new ClipboardMonitor(this);
            cbMon.ClipboardUpdated += cbMon_ClipboardUpdated;

            viewModel = new MainViewModel(esHandler, cbMon);
            viewModel.Id = "rbl";
            DataContext = viewModel;
        }

        private void cbMon_ClipboardUpdated()
        {
            try
            {
                var clipboardData = Clipboard.GetDataObject();
                if (clipboardData.GetDataPresent(DataFormats.Text))
                {
                    var clipboardText = (string)clipboardData.GetData(DataFormats.Text);
                    viewModel.Status = String.Format("Found Text ({0})", SafeSubstring(clipboardText.Replace(Environment.NewLine, @"\r\n"), 0, 20));
                    esHandler.SetText(clipboardText);
                }
                else if (clipboardData.GetDataPresent(DataFormats.FileDrop))
                {
                    var clipboardFiles = (string[])clipboardData.GetData(DataFormats.FileDrop);
                    viewModel.Status = String.Format("Found Files ({0})", clipboardFiles.Length);

                    using (var ms = new MemoryStream())
                    {
                        using (var zipArchive = new ZipArchive(ms, ZipArchiveMode.Create, false))
                        {
                            foreach (var file in clipboardFiles)
                            {
                                if (File.GetAttributes(file).HasFlag(FileAttributes.Directory))
                                {
                                    CreateEntryFromFolder(zipArchive, file, Path.GetFileName(file));
                                }
                                else
                                {
                                    zipArchive.CreateEntryFromFile(file, Path.GetFileName(file));
                                }
                            }
                        }
                        File.WriteAllBytes("test.zip", ms.ToArray());
                    }

                    esHandler.SetFiles(new[] { "test.zip" });
                }
                else
                {
                    viewModel.Status = "Found Unknown";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private string SafeSubstring(string value, int startIndex, int length)
        {
            return new String((value ?? String.Empty).Skip(startIndex).Take(length).ToArray());
        }

        private static ZipArchiveEntry CreateEntryFromFolder(ZipArchive destination, string sourceFolderName, string entryName)
        {
            var sourceFolderFullPath = Path.GetFullPath(sourceFolderName);
            var basePath = entryName + "/";

            var createdFolders = new HashSet<string>();

            var entry = destination.CreateEntry(basePath);
            createdFolders.Add(basePath);
            foreach (var dirFolder in Directory.EnumerateDirectories(sourceFolderName, "*.*", SearchOption.AllDirectories))
            {
                var dirFileFullPath = Path.GetFullPath(dirFolder);
                var relativePath = (basePath + dirFileFullPath.Replace(sourceFolderFullPath + Path.DirectorySeparatorChar, ""))
                    .Replace(Path.DirectorySeparatorChar, '/');
                var relativePathSlash = relativePath + "/";

                if (!createdFolders.Contains(relativePathSlash))
                {
                    destination.CreateEntry(relativePathSlash);
                    createdFolders.Add(relativePathSlash);
                }
            }

            foreach (var dirFile in Directory.EnumerateFiles(sourceFolderName, "*.*", SearchOption.AllDirectories))
            {
                var dirFileFullPath = Path.GetFullPath(dirFile);
                var relativePath = (basePath + dirFileFullPath.Replace(sourceFolderFullPath + Path.DirectorySeparatorChar, ""))
                    .Replace(Path.DirectorySeparatorChar, '/');
                destination.CreateEntryFromFile(dirFile, relativePath);
            }

            return entry;
        }
    }
}
