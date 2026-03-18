using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;
using System.IO.Compression;
using System;
using Blish_HUD.Controls;

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

                var installSuccess = await installModels(filePath);
                if (installSuccess)
                    return true;
                else
                    return false;
            }
            catch (Exception)
            {
                ScreenNotification.ShowNotification("Irgendwas ist schiefgelaufen");
                return false;
            }

        }

        public async Task<bool> installModels(string zipPath)
        {
            try
            {
                ScreenNotification.ShowNotification("Updating Models...");

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
                return true;
            }
            catch (Exception ex)
            {
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
            string filePath = Path.Combine(iconFolder, fileName);

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

            string txtPath = Path.Combine(iconFolder, "File_Versions.txt");

            if (File.Exists(txtPath))
            {
                File.Delete(txtPath);
                //Debug.WriteLine("Deleted!______");

            }

            ZipFile.ExtractToDirectory(filePath, iconFolder);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return txtPath;
        }



    }
}
