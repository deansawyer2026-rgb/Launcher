using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Launcher.Services;

public class ServerDownloadService
{
    private static readonly HttpClient HttpClient = new();

    public event EventHandler<DownloadProgressChangedEventArgs>? DownloadProgressChanged;

    public async Task<bool> DownloadServerAsync(string repositoryUrl, string installPath)
    {
        try
        {
            // Create install directory if it doesn't exist
            Directory.CreateDirectory(installPath);

            // For now, we'll implement a GitHub release downloader
            // In production, you'd download release artifacts
            var downloadUrl = $"{repositoryUrl}/archive/refs/heads/main.zip";

            var zipPath = Path.Combine(installPath, "sanctuary.zip");

            // Download the repository as ZIP
            using var response = await HttpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1 && totalBytes > 0;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[8192];
            long totalRead = 0;
            int read;

            while ((read = await contentStream.ReadAsync(buffer)) != 0)
            {
                await fileStream.WriteAsync(buffer, 0, read);
                totalRead += read;

                if (canReportProgress)
                {
                    var progress = (int)((totalRead * 100) / totalBytes);
                    DownloadProgressChanged?.Invoke(this, new DownloadProgressChangedEventArgs
                    {
                        ProgressPercentage = progress,
                        BytesReceived = totalRead,
                        TotalBytesToReceive = totalBytes
                    });
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Download failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ExtractServerAsync(string zipPath, string extractPath)
    {
        try
        {
            // Extract ZIP file
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath, overwriteFiles: true);
            
            // Clean up ZIP
            File.Delete(zipPath);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Extraction failed: {ex.Message}");
            return false;
        }
    }
}

public class DownloadProgressChangedEventArgs : EventArgs
{
    public int ProgressPercentage { get; set; }
    public long BytesReceived { get; set; }
    public long TotalBytesToReceive { get; set; }
}