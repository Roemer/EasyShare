using EasyShare.Api.Types;
using EasyShare.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace EasyShare.Api
{
    public class EasyShareHandler
    {
        private string baseUrl;
        private string id;
        private Timer fetchTimer;
        private int lastCheckTime;
        private static readonly int maxFilePartSize = 1024 * 1024 * 10;

        public EasyShareHandler(string baseUrl, string id)
        {
            this.baseUrl = baseUrl;
            this.id = id;

            fetchTimer = new Timer(5000);
            fetchTimer.Elapsed += FetchTimerElapsed;
        }

        public void UpdateId(string newId)
        {
            id = newId;
        }

        public void StartFetching()
        {
            fetchTimer.Enabled = true;
        }

        public void StopFetching()
        {
            fetchTimer.Enabled = false;
        }

        public async Task<Status> GetStatus()
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetStringAsync(GetMethodUrl("check"));
                return JsonConvert.DeserializeObject<Status>(response);
            }
        }

        public async Task<string> GetText()
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetStringAsync(GetMethodUrl("gettext"));
                return response;
            }
        }

        public async Task<string> GetFiles()
        {
            using (var client = new HttpClient())
            {
                var response = await client.GetByteArrayAsync(GetMethodUrl("getfiles"));
                var responseString = Encoding.Default.GetString(response, 0, response.Length);
                return responseString;
            }
        }

        public async Task<byte[]> Download(string file)
        {
            using (var client = new HttpClient())
            {
                var values = new Dictionary<string, string>();
                values.Add("file", file);
                var content = new FormUrlEncodedContent(values);
                var response = await client.PostAsync(GetMethodUrl("download"), content);
                var responseBytes = await response.Content.ReadAsByteArrayAsync();
                return responseBytes;
            }
        }

        public async void SetText(string text)
        {
            using (var client = new HttpClient())
            {
                using (var form = new MultipartFormDataContent())
                {
                    var content = new StringContent(text);
                    form.Add(content, "text");
                    var response = await client.PostAsync(GetMethodUrl("update"), form);
                    var responseString = await response.Content.ReadAsStringAsync();
                }
            }
        }

        public async void SetFiles(string[] filePaths)
        {
            using (var client = new HttpClient())
            {
                using (var form = new MultipartFormDataContent())
                {
                    foreach (var filePath in filePaths)
                    {
                        using (var fs = File.OpenRead(filePath))
                        {
                            var fileSize = fs.Length;
                            for (var i = 0; i < fileSize; i += maxFilePartSize)
                            {
                                var readSize = Math.Min(maxFilePartSize, (int)(fileSize - i));
                                var bytes = new byte[readSize];
                                fs.Read(bytes, i, readSize);
                                using (var streamContent = new ByteArrayContent(bytes))
                                {
                                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                                    streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                                    {
                                        Name = "file[]",
                                        FileName = "\"" + WebUtility.HtmlEncode(Path.GetFileName(filePath)) + ".part" + i + "\""
                                    };
                                    form.Add(streamContent);
                                }
                            }

                            //using (var streamContent = new StreamContent(fs))
                            //{
                            //    streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                            //    streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                            //    {
                            //        Name = "file[]",
                            //        FileName = "\"" + WebUtility.HtmlEncode(Path.GetFileName(filePath)) + "\""
                            //    };
                            //    form.Add(streamContent);
                            //}
                        }
                    }
                    var response = await client.PostAsync(GetMethodUrl("update"), form);
                    var responseString = await response.Content.ReadAsStringAsync();
                }
            }
        }

        private string GetMethodUrl(string method)
        {
            return String.Format("{0}?id={1}&method={2}", baseUrl, id, method);
        }

        private void FetchTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var status = GetStatus().Result;
                if (lastCheckTime < status.date)
                {
                    Console.WriteLine("Update needed");
                    if (status.type == "text")
                    {
                        var value = GetText().Result;
                        ClipboardManager.SetText(value);
                    }
                    else if (status.type == "files")
                    {
                        CreateOrClearDirectory("Temp");
                        var value = GetFiles().Result;
                        using (var reader = new StringReader(value))
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                var bytes = Download(line).Result;
                                var fileName = Path.Combine("Temp", line);
                                File.WriteAllBytes(fileName, bytes);

                                CreateOrClearDirectory("Files");
                                ZipFile.ExtractToDirectory(fileName, "Files");
                                File.Delete(fileName);
                            }
                        }
                        ClipboardManager.SetFiles("Files");
                    }
                    lastCheckTime = status.date;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void CreateOrClearDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
            Directory.CreateDirectory(directory);
        }
    }
}

