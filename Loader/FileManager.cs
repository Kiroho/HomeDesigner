using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;
using System.IO.Compression;
using System;
using Blish_HUD.Controls;
using System.Collections.Generic;
using Newtonsoft.Json;
using Flurl.Http;
using Newtonsoft.Json.Linq;
using Flurl;

namespace HomeDesigner.Loader
{
    public class FileManager
    {
        public string modelFolder { get; private set; }
        public string iconFolder { get; private set; }

        public FileManager(string modelPath, string iconPath)
        {
            modelFolder = modelPath;
            iconFolder = iconPath;

            if (!Directory.Exists(modelFolder))
                Directory.CreateDirectory(modelFolder);

            if (!Directory.Exists(iconFolder))
                Directory.CreateDirectory(iconFolder);

        }

        // Dispose Funktion (für FileDownloader)


        // _________________ Decos _________________ 

        public void SaveDecorationsToFile(List<Decoration> decorations)
        {
            var filePath = Path.Combine(iconFolder, "decorations.json");
            var json = JsonConvert.SerializeObject(decorations, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public List<Decoration> LoadDecorationsFromFile()
        {
            var filePath = Path.Combine(iconFolder, "decorations.json");
            if (!File.Exists(filePath))
                return new List<Decoration>();

            var json = File.ReadAllText(filePath);

            var data = JsonConvert.DeserializeObject<List<Decoration>>(json);

            return data ?? new List<Decoration>();
        }

        public async Task<Dictionary<int, string>> LoadDecoCategories()
        {
            const string endpoint = "https://api.guildwars2.com/v2/homestead/decorations/categories";

            // 1) Alle IDs holen
            var ids = await endpoint
                .GetJsonAsync<List<int>>();

            // 2) Detaildaten laden
            var json = await endpoint
                .SetQueryParam("ids", string.Join(",", ids))
                .GetJsonAsync<JArray>();

            // 3) Dictionary erstellen (ID → Name)
            var dict = new Dictionary<int, string>();

            foreach (var item in json)
            {
                int id = item.Value<int>("id");
                string name = item.Value<string>("name");

                dict[id] = name;
            }

            return dict;
        }




        // _________________ Models _________________ 
        public async Task<bool> checkForNewModelVersion(int currentVersion)
        {
            int newModelVersion = await GetModelVersionAsync();
            if (newModelVersion > currentVersion)
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// Lädt eine Datei von Google Drive über die FileId herunter
        /// und speichert sie im DownloadFolder.
        /// </summary>
        public async Task<bool> DownloadFromDriveAsync()
        {
            var fileName = "models.zip";
            var fileId = "1o8rYDVwCXkPS89eZxpSPB4weprtYlDx8";
            string url = "https://drive.google.com/uc?export=download&id=" + fileId;
            string filePath = Path.Combine(modelFolder, fileName);
            try
            {
                ScreenNotification.ShowNotification("Downloading Models...");
                using (var downloader = new FileDownloader())
                {
                    // Fortschritt melden
                    long lastReportedBytes = 0;
                    downloader.DownloadProgressChanged += (s, progress) =>
                    {
                        // Wir ignorieren kleine Downloads < 1 MB (meist Bestätigungsseiten)
                        if (progress.TotalBytesToReceive > 1024 * 1024)
                        {
                            long delta = progress.BytesReceived - lastReportedBytes;
                            double percentDelta = (delta / (double)progress.TotalBytesToReceive) * 100;

                            if (percentDelta >= 5)
                            {
                                lastReportedBytes = progress.BytesReceived;
                                ScreenNotification.ShowNotification($"Download: {progress.ProgressPercentage}%");
                            }
                        }
                    };

                    // FileDownloader arbeitet noch nicht mit async/await nativ, deshalb TaskCompletionSource nutzen
                    var tcs = new TaskCompletionSource<bool>();
                    downloader.DownloadFileCompleted += (s, e) =>
                    {
                        if (e.Error != null)
                            tcs.SetException(e.Error);
                        else if (e.Cancelled)
                            tcs.SetCanceled();
                        else
                            tcs.SetResult(true);
                    };

                    // Async Download starten
                    downloader.DownloadFileAsync(url, filePath);

                    await tcs.Task; // Warten bis fertig
                    ScreenNotification.ShowNotification("Download Finished");
                }

                var installSuccess = await InstallModels(filePath);
                if (installSuccess)
                    return true;
                else
                    return false;
            }
            catch (Exception)
            {
                ScreenNotification.ShowNotification("Something went Wrong...");
                return false;
            }

        }

        public async Task<bool> InstallModels(string zipPath)
        {
            try
            {
                // 1️⃣ Alle .obj Dateien löschen
                foreach (var file in Directory.GetFiles(modelFolder, "*.obj", SearchOption.AllDirectories))
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Delete failed: {file} - {ex.Message}");
                    }
                }

                // 2️⃣ ZIP entpacken
                ZipFile.ExtractToDirectory(zipPath, modelFolder);

                ScreenNotification.ShowNotification("Models installed");
                File.Delete(zipPath);
                await Task.Delay(30);
                return true;
            }
            catch (Exception)
            {
                await Task.Delay(30);
                return false;
            }
        }




        public async Task<int> GetModelVersionAsync()
        {
            string filePath = await LoadVersionFileAsync();
            string content = File.ReadAllText(filePath).Trim();

            //Debug.WriteLine("Path: " + filePath);

            if (int.TryParse(content, out int version))
            {
                return version;
            }

            return 0; // Fallback-Version
        }


        public async Task<string> LoadVersionFileAsync()
        {
            var fileName = "File_Versions.zip";
            var fileId = "1PNYtCAZVprhFdie5sd6f6QInNn75Zu2a";
            string url = "https://drive.google.com/uc?export=download&id=" + fileId;
            string filePath = Path.Combine(modelFolder, fileName);

            using (var downloader = new FileDownloader())
            {
                // FileDownloader arbeitet noch nicht mit async/await nativ, deshalb TaskCompletionSource nutzen
                var tcs = new TaskCompletionSource<bool>();
                downloader.DownloadFileCompleted += (s, e) =>
                {
                    if (e.Error != null)
                        tcs.SetException(e.Error);
                    else if (e.Cancelled)
                        tcs.SetCanceled();
                    else
                        tcs.SetResult(true);
                };

                // Async Download starten
                downloader.DownloadFileAsync(url, filePath);

                await tcs.Task; // Warten bis fertig
            }

            string txtPath = Path.Combine(modelFolder, "File_Versions.txt");

            if (File.Exists(txtPath))
            {
                File.Delete(txtPath);
                //Debug.WriteLine("Deleted!______");

            }

            ZipFile.ExtractToDirectory(filePath, modelFolder);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return txtPath;
        }



    }
}
