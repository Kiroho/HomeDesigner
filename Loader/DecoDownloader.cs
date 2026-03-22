using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HomeDesigner.Loader
{
    public class DecoDownloader : IDisposable
    {
        public static readonly HttpClient _httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public DecoDownloader()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "BlishHUD-DecorationModule");
        }


        public async Task DownloadIcons(List<Decoration> decorations, string folder)
        {
            var semaphore = new SemaphoreSlim(5); // max 5 parallel
            var tasks = new List<Task>();

            foreach (var deco in decorations)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();

                    try
                    {
                        await EnsureIconDownloaded(deco, folder);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }


        public async Task EnsureIconDownloaded(Decoration decoration, string folder)
        {
            var filePath = Path.Combine(folder, decoration.id + ".png");

            if (File.Exists(filePath))
                return;

            try
            {
                var bytes = await _httpClient.GetByteArrayAsync(decoration.icon);
                File.WriteAllBytes(filePath, bytes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("_________Icon Download Failure: " + ex.Message);
            }
        }



        public async Task<List<Decoration>> DownloadDecorationsByChunksAsync(List<List<int>> chunks)
        {
            var results = new List<Decoration>();
            var tasks = new List<Task<List<Decoration>>>();

            var semaphore = new SemaphoreSlim(4); // max 4 parallel

            foreach (var chunk in chunks)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();

                    try
                    {
                        return await FetchChunkAsync(chunk);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            var chunkResults = await Task.WhenAll(tasks);

            foreach (var list in chunkResults)
            {
                results.AddRange(list);
            }

            return results;
        }



        public async Task<List<Decoration>> FetchChunkAsync(List<int> chunk)
        {
            int maxRetries = 3;
            int delayMs = 500;

            var ids = string.Join(",", chunk);
            var url = "https://api.guildwars2.com/v2/homestead/decorations?ids=" + ids;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var response = await _httpClient.GetAsync(url);

                    // Rate Limit oder Serverfehler → Retry sinnvoll
                    if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500)
                    {
                        throw new HttpRequestException("Server/RateLimit error: " + response.StatusCode);
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        // andere Fehler → kein Retry nötig
                        return new List<Decoration>();
                    }

                    var json = await response.Content.ReadAsStringAsync();

                    var data = JsonConvert.DeserializeObject<List<Decoration>>(json);

                    return data ?? new List<Decoration>();
                }
                catch (Exception ex)
                {
                    if (attempt == maxRetries)
                    {
                        Console.WriteLine($"_______Chunk endgültig fehlgeschlagen: {ex.Message}");
                        return new List<Decoration>();
                    }

                    // exponentielles Backoff
                    int waitTime = delayMs * attempt;

                    Console.WriteLine($"Retry {attempt} in {waitTime}ms...");

                    await Task.Delay(waitTime);
                }
            }

            return new List<Decoration>();
        }




        public async Task<List<int>> GetAllDecorationIdsAsync()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "BlishHUD-DecorationModule");

            try
            {
                var response = await client.GetAsync("https://api.guildwars2.com/v2/homestead/decorations");
                response.EnsureSuccessStatusCode();

                var stream = await response.Content.ReadAsStreamAsync();

                var ids = await System.Text.Json.JsonSerializer.DeserializeAsync<List<int>>(stream);

                return ids ?? new List<int>();
            }
            finally
            {
                client.Dispose();
            }
        }


        public List<List<int>> ChunkList(List<int> source, int chunkSize)
        {
            var chunks = new List<List<int>>();

            for (int i = 0; i < source.Count; i += chunkSize)
            {
                var chunk = source.GetRange(i, Math.Min(chunkSize, source.Count - i));
                chunks.Add(chunk);
            }

            return chunks;
        }












        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
