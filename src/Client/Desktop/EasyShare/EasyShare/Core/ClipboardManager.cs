using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace EasyShare.Core
{
    public static class ClipboardManager
    {
        public static void SetText(string newText)
        {
            StartSTATask(() =>
            {
                Clipboard.SetText(newText);
            });
        }

        public static void SetFiles(string pathToFolderWithContent)
        {
            StartSTATask(() =>
            {
                var fileColl = new System.Collections.Specialized.StringCollection();
                foreach (var entry in Directory.GetFileSystemEntries(pathToFolderWithContent))
                {
                    fileColl.Add(Path.GetFullPath(entry));
                }
                Clipboard.SetFileDropList(fileColl);
            });
        }

        private static Task StartSTATask(Action func)
        {
            var tcs = new TaskCompletionSource<object>();
            var thread = new Thread(() =>
            {
                try
                {
                    func();
                    tcs.SetResult(null);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        private static Task<T> StartSTATask<T>(Func<T> func)
        {
            var tcs = new TaskCompletionSource<T>();
            Thread thread = new Thread(() =>
            {
                try
                {
                    var result = func();
                    tcs.SetResult(result);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }
    }
}
