using System;
using System.Diagnostics;

namespace Launcher.Models;

public class ServerProcessModel
{
    public string Name { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public Process? Process { get; set; }
    public bool IsRunning { get; set; }
    public DateTime StartedAt { get; set; }
    public string WorkingDirectory { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
}