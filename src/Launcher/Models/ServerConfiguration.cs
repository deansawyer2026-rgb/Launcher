using System;
using System.Collections.Generic;

namespace Launcher.Models;

public class ServerConfiguration
{
    public string ServerName { get; set; } = "Sanctuary Server";
    public string RepositoryUrl { get; set; } = "https://github.com/deansawyer2026-rgb/Sanctuary";
    public string RepositoryBranch { get; set; } = "main";
    public string InstallPath { get; set; } = string.Empty;
    public bool WatchdogEnabled { get; set; } = false;
    public int WatchdogRestartDelaySeconds { get; set; } = 5;
    public Dictionary<string, ServerComponent> Components { get; set; } = new()
    {
        { "Login", new ServerComponent { Name = "Login", ExecutableName = "Sanctuary.Login.exe" } },
        { "Gateway", new ServerComponent { Name = "Gateway", ExecutableName = "Sanctuary.Gateway.exe" } }
    };
    public string RegistrationWebsiteUrl { get; set; } = "http://localhost:5000";
    public string ClientManifestUrl { get; set; } = "http://localhost:8000/manifest/client";
    public string ServerManifestUrl { get; set; } = "http://localhost:8000/manifest/server";
}

public class ServerComponent
{
    public string Name { get; set; } = string.Empty;
    public string ExecutableName { get; set; } = string.Empty;
    public bool AutoStart { get; set; } = false;
}
