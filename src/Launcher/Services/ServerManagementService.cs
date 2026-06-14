using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Models;

namespace Launcher.Services;

public class ServerManagementService
{
    private readonly ServerConfiguration _config;
    private readonly Dictionary<string, ServerProcessModel> _processes = new();
    private readonly Dictionary<string, CancellationTokenSource> _watchdogTokens = new();

    public event EventHandler<ServerStatusChangedEventArgs>? ServerStatusChanged;
    public event EventHandler<ServerLogEventArgs>? LogMessage;

    public ServerManagementService(ServerConfiguration config)
    {
        _config = config;
    }

    public async Task<bool> StartServerComponentAsync(string componentName)
    {
        try
        {
            if (!_config.Components.TryGetValue(componentName, out var component))
            {
                LogOutput($"Component {componentName} not found");
                return false;
            }

            var executablePath = Path.Combine(_config.InstallPath, "bin", "Release", component.ExecutableName);

            if (!File.Exists(executablePath))
            {
                LogOutput($"Executable not found: {executablePath}");
                return false;
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    WorkingDirectory = Path.GetDirectoryName(executablePath),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    LogOutput($"[{componentName}] {e.Data}");
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    LogOutput($"[{componentName}] ERROR: {e.Data}");
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var processModel = new ServerProcessModel
            {
                Name = componentName,
                ExecutablePath = executablePath,
                Process = process,
                IsRunning = true,
                StartedAt = DateTime.Now,
                WorkingDirectory = Path.GetDirectoryName(executablePath) ?? string.Empty
            };

            _processes[componentName] = processModel;

            LogOutput($"Started {componentName} (PID: {process.Id})");
            ServerStatusChanged?.Invoke(this, new ServerStatusChangedEventArgs { ComponentName = componentName, IsRunning = true });

            // Start watchdog if enabled
            if (_config.WatchdogEnabled)
            {
                _ = WatchProcessAsync(componentName, process);
            }

            return true;
        }
        catch (Exception ex)
        {
            LogOutput($"Failed to start {componentName}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> StopServerComponentAsync(string componentName)
    {
        try
        {
            if (!_processes.TryGetValue(componentName, out var processModel))
            {
                LogOutput($"Process {componentName} not found");
                return false;
            }

            // Stop watchdog
            if (_watchdogTokens.TryGetValue(componentName, out var cts))
            {
                cts.Cancel();
                _watchdogTokens.Remove(componentName);
            }

            var process = processModel.Process;
            if (process != null && !process.HasExited)
            {
                process.Kill(true);
                await Task.Delay(500); // Wait for process to fully terminate
            }

            _processes.Remove(componentName);
            LogOutput($"Stopped {componentName}");
            ServerStatusChanged?.Invoke(this, new ServerStatusChangedEventArgs { ComponentName = componentName, IsRunning = false });

            return true;
        }
        catch (Exception ex)
        {
            LogOutput($"Failed to stop {componentName}: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> StopAllServersAsync()
    {
        var tasks = _processes.Keys.ToList().Select(StopServerComponentAsync);
        await Task.WhenAll(tasks);
        return true;
    }

    public bool IsComponentRunning(string componentName)
    {
        return _processes.TryGetValue(componentName, out var model) && model.IsRunning;
    }

    public void EnableWatchdog(bool enabled)
    {
        _config.WatchdogEnabled = enabled;
        LogOutput($"Watchdog {(enabled ? "enabled" : "disabled")}");
    }

    public void OpenWebsite(string url)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);
            LogOutput($"Opening {url}");
        }
        catch (Exception ex)
        {
            LogOutput($"Failed to open website: {ex.Message}");
        }
    }

    private async Task WatchProcessAsync(string componentName, Process process)
    {
        var cts = new CancellationTokenSource();
        _watchdogTokens[componentName] = cts;

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                await Task.Delay(1000, cts.Token);

                if (process.HasExited && !cts.Token.IsCancellationRequested)
                {
                    LogOutput($"[Watchdog] {componentName} crashed, restarting in {_config.WatchdogRestartDelaySeconds}s...");
                    await Task.Delay(_config.WatchdogRestartDelaySeconds * 1000, cts.Token);
                    
                    _processes.Remove(componentName);
                    await StartServerComponentAsync(componentName);
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }
    }

    private void LogOutput(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        LogMessage?.Invoke(this, new ServerLogEventArgs { Message = $"[{timestamp}] {message}" });
    }
}

public class ServerStatusChangedEventArgs : EventArgs
{
    public string ComponentName { get; set; } = string.Empty;
    public bool IsRunning { get; set; }
}

public class ServerLogEventArgs : EventArgs
{
    public string Message { get; set; } = string.Empty;
}