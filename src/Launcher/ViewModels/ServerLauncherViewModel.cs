using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launcher.Models;
using Launcher.Services;

namespace Launcher.ViewModels;

public partial class ServerLauncherViewModel : ObservableObject
{
    private readonly ServerManagementService _managementService;
    private readonly ServerDownloadService _downloadService;
    private readonly ServerConfiguration _config;

    [ObservableProperty]
    private string status = "Stopped";

    [ObservableProperty]
    private bool isDownloading;

    [ObservableProperty]
    private int downloadProgress;

    [ObservableProperty]
    private bool watchdogEnabled;

    [ObservableProperty]
    private ObservableCollection<string> logs = new();

    [ObservableProperty]
    private bool loginServerRunning;

    [ObservableProperty]
    private bool gatewayServerRunning;

    public ServerLauncherViewModel()
    {
        _config = new ServerConfiguration
        {
            InstallPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SanctuaryServer")
        };

        _managementService = new ServerManagementService(_config);
        _downloadService = new ServerDownloadService();

        // Hook up events
        _managementService.LogMessage += (s, e) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                Logs.Add(e.Message);
                if (Logs.Count > 1000) // Keep log manageable
                    Logs.RemoveAt(0);
            });
        };

        _managementService.ServerStatusChanged += (s, e) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (e.ComponentName == "Login")
                    LoginServerRunning = e.IsRunning;
                else if (e.ComponentName == "Gateway")
                    GatewayServerRunning = e.IsRunning;

                Status = (LoginServerRunning || GatewayServerRunning) ? "Running" : "Stopped";
            });
        };

        _downloadService.DownloadProgressChanged += (s, e) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                DownloadProgress = e.ProgressPercentage;
            });
        };

        WatchdogEnabled = _config.WatchdogEnabled;
    }

    [RelayCommand]
    public async Task DownloadServer()
    {
        try
        {
            IsDownloading = true;
            DownloadProgress = 0;

            AddLog("Starting server download...");

            var success = await _downloadService.DownloadServerAsync(_config.RepositoryUrl, _config.InstallPath);
            if (success)
            {
                AddLog("Download complete, extracting files...");
                success = await _downloadService.ExtractServerAsync(
                    System.IO.Path.Combine(_config.InstallPath, "sanctuary.zip"),
                    _config.InstallPath);
            }

            if (success)
                AddLog("Server setup complete!");
            else
                AddLog("Failed to download/extract server");
        }
        catch (Exception ex)
        {
            AddLog($"Error: {ex.Message}");
        }
        finally
        {
            IsDownloading = false;
        }
    }

    [RelayCommand]
    public async Task StartAllServers()
    {
        AddLog("Starting all servers...");
        await _managementService.StartServerComponentAsync("Login");
        await Task.Delay(2000);
        await _managementService.StartServerComponentAsync("Gateway");
    }

    [RelayCommand]
    public async Task StopAllServers()
    {
        AddLog("Stopping all servers...");
        await _managementService.StopAllServersAsync();
    }

    [RelayCommand]
    public async Task StartLoginServer()
    {
        await _managementService.StartServerComponentAsync("Login");
    }

    [RelayCommand]
    public async Task StopLoginServer()
    {
        await _managementService.StopServerComponentAsync("Login");
    }

    [RelayCommand]
    public async Task StartGatewayServer()
    {
        await _managementService.StartServerComponentAsync("Gateway");
    }

    [RelayCommand]
    public async Task StopGatewayServer()
    {
        await _managementService.StopServerComponentAsync("Gateway");
    }

    [RelayCommand]
    public void ToggleWatchdog()
    {
        WatchdogEnabled = !WatchdogEnabled;
        _managementService.EnableWatchdog(WatchdogEnabled);
    }

    [RelayCommand]
    public void OpenRegistrationWebsite()
    {
        _managementService.OpenWebsite(_config.RegistrationWebsiteUrl);
    }

    [RelayCommand]
    public void OpenClientManifest()
    {
        _managementService.OpenWebsite(_config.ClientManifestUrl);
    }

    [RelayCommand]
    public void OpenServerManifest()
    {
        _managementService.OpenWebsite(_config.ServerManifestUrl);
    }

    [RelayCommand]
    public void ClearLogs()
    {
        Logs.Clear();
        AddLog("Logs cleared");
    }

    private void AddLog(string message)
    {
        Logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        if (Logs.Count > 1000)
            Logs.RemoveAt(0);
    }
}