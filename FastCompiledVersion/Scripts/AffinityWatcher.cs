using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Win32;

class AffinityWatcher
{
    enum ChangeType
    {
        None = 0,
        Affinity = 1,
        Priority = 2,
        Both = 3
    }

    class ProcessConfig
    {
        public string Name { get; set; }
        public ChangeType ChangeType { get; set; }

        public ProcessConfig(string name, ChangeType changeType)
        {
            Name = name;
            ChangeType = changeType;
        }
    }

    static void Main()
    {
        // Check for settings file next to the executable
        string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string exeDirectory = Path.GetDirectoryName(exePath);
        string settingsPath = Path.Combine(exeDirectory, "MICAHTECHSETTINGS");
        bool settingsFileExists = File.Exists(settingsPath);

        // Config file paths
        string configPath = Path.Combine(exeDirectory, "affinitywatcher.txt");
        string filterPath = Path.Combine(exeDirectory, "affinitywatcherfilter.txt");

        // Create default config files if they don't exist
        CreateDefaultConfigIfNeeded(configPath);
        CreateDefaultFilterIfNeeded(filterPath);

        // Load configuration from affinitywatcher.txt
        bool globalEnableAffinity = true; // Default to enabled
        bool globalEnablePriority = true; // Default to enabled
        int? win32PrioritySeparation = null; // Default to null (don't change registry)

        try
        {
            string[] configLines = File.ReadAllLines(configPath);
            foreach (string line in configLines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;

                string[] parts = trimmedLine.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    string key = parts[0].Trim().ToLowerInvariant();
                    string value = parts[1].Trim();

                    if (key == "enableaffinitychanging")
                    {
                        globalEnableAffinity = value.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                    }
                    else if (key == "enableprioritychanging")
                    {
                        globalEnablePriority = value.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                    }
                    else if (key == "setwin32priority")
                    {
                        int parsedValue;
                        if (int.TryParse(value, out parsedValue))
                        {
                            win32PrioritySeparation = parsedValue;
                        }
                    }
                }
            }
        }
        catch { }

        // Set Win32PrioritySeparation in registry if value is specified
        if (win32PrioritySeparation.HasValue)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\PriorityControl", true))
                {
                    if (key != null)
                    {
                        key.SetValue("Win32PrioritySeparation", win32PrioritySeparation.Value, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }

        // Get logical processor count
        int logicalProcessors = Environment.ProcessorCount;

        // Validate at least 3 cores (only if affinity changing is enabled)
        if (globalEnableAffinity && logicalProcessors < 3)
        {
            return; // Exit silently in background mode
        }

        // Build affinity mask that disables Core 0 and Core 1
        IntPtr affinityMask = IntPtr.Zero;
        if (globalEnableAffinity)
        {
            long mask = 0;
            for (int i = 2; i < logicalProcessors; i++)
            {
                mask |= (1L << i);
            }
            affinityMask = new IntPtr(mask);

            // Pin this process to all cores except the first two
            try
            {
                Process currentProcess = Process.GetCurrentProcess();
                currentProcess.ProcessorAffinity = affinityMask;
                currentProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
            }
            catch { }
        }
        else
        {
            // Still set this process to below normal priority
            try
            {
                Process currentProcess = Process.GetCurrentProcess();
                currentProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
            }
            catch { }
        }

        // Load process list from affinitywatcherfilter.txt
        List<ProcessConfig> processConfigs = new List<ProcessConfig>();

        try
        {
            string[] filterLines = File.ReadAllLines(filterPath);
            foreach (string line in filterLines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;

                // Parse format: ProcessName|A|P|B
                // A = Affinity only, P = Priority only, B = Both, nothing = Both (default)
                string[] parts = trimmedLine.Split('|');
                string processName = parts[0].Trim();
                ChangeType changeType = ChangeType.Both; // Default to both

                if (parts.Length > 1)
                {
                    string typeStr = parts[1].Trim().ToUpperInvariant();
                    if (typeStr == "A")
                        changeType = ChangeType.Affinity;
                    else if (typeStr == "P")
                        changeType = ChangeType.Priority;
                    else if (typeStr == "B")
                        changeType = ChangeType.Both;
                }

                processConfigs.Add(new ProcessConfig(processName, changeType));
            }
        }
        catch { }

        // Determine sleep interval based on settings file
        int sleepInterval = settingsFileExists ? 15000 : 15000; // 15 seconds

        // Main monitoring loop
        while (true)
        {
            try
            {
                // Get all processes once
                Process[] allProcesses = Process.GetProcesses();

                foreach (ProcessConfig config in processConfigs)
                {
                    var matchingProcesses = allProcesses.Where(p => 
                        p.ProcessName.Equals(config.Name, StringComparison.OrdinalIgnoreCase)).ToArray();

                    foreach (Process proc in matchingProcesses)
                    {
                        try
                        {
                            // Check if process still exists
                            if (proc.HasExited)
                            {
                                continue;
                            }
                            
                            // Set affinity to exclude cores 0 and 1 (if enabled globally and for this process)
                            if (globalEnableAffinity && 
                                (config.ChangeType == ChangeType.Affinity || config.ChangeType == ChangeType.Both))
                            {
                                proc.ProcessorAffinity = affinityMask;
                            }

                            // Set priority to BelowNormal (if enabled globally and for this process)
                            if (globalEnablePriority && 
                                (config.ChangeType == ChangeType.Priority || config.ChangeType == ChangeType.Both))
                            {
                                // Use BelowNormal for better stability while still helping game performance
                                if (proc.PriorityClass != ProcessPriorityClass.BelowNormal)
                                {
                                    proc.PriorityClass = ProcessPriorityClass.BelowNormal;
                                }
                            }
                        }
                        catch { }
                    }
                }

                // Dispose all processes
                foreach (Process p in allProcesses)
                {
                    p.Dispose();
                }
            }
            catch { }

            Thread.Sleep(sleepInterval);
        }
    }

    static void CreateDefaultConfigIfNeeded(string configPath)
    {
        if (File.Exists(configPath))
            return;

        try
        {
            string defaultConfig = @"# AffinityWatcher Configuration File
# 
# enableAffinityChanging: Set to TRUE to change CPU affinity (exclude cores 0 and 1)
# enablePriorityChanging: Set to TRUE to change process priority to BelowNormal
# setwin32priority: Set to a decimal value to change Win32PrioritySeparation registry value
#                   Leave commented out or omit to not modify registry

enableAffinityChanging=TRUE
enablePriorityChanging=TRUE
# setwin32priority=(Leave this commented out unless you know what you're doing! Decimal not Hexadecimal btw.)
";
            File.WriteAllText(configPath, defaultConfig);
        }
        catch { }
    }

    static void CreateDefaultFilterIfNeeded(string filterPath)
    {
        if (File.Exists(filterPath))
            return;

        try
        {
            string defaultFilter = @"# AffinityWatcher Process Filter
#
# Format: ProcessName|Type
# Type can be:
#   A = Affinity only
#   P = Priority only
#   B = Both affinity and priority
#   (omit type for Both as default)
#
# Examples:
#   Discord|B          (change both)
#   chrome|A           (change affinity only)
#   Spotify|P          (change priority only)
#   obs64              (defaults to both)

#Warning: Changing the priority can cause instability/lag in that process, especially when a game or other intensive process is running! Only use it on apps that need run in the background that you don't actually use often! Definitely DON'T run in on build in Windows processes! (except edgeupdate especially if you don't use edge, frick edge!)

Medal|B
MedalEncoder|A
crashpad_handler|B
obs64|A
obs-ffmpeg-mux|A
ffmpeg-mux|A
chrome|A
firefox|A
Discord|A
Spotify|A
SteamWebHelper|A
SteamService|A
EpicWebHelper|A
EACefSubProcess|A
MicrosoftEdgeUpdate|B
msedgewebview2|A
crossover|B
x3|B
sunshinesvc|A
spoolsv|B
SearchIndexer|B
tailscaled|B
tailscale-ipn|B
netbird|A
netbird-ui|B
NVIDIA Broadcast|A
audiodg|A
Greenshot|B
x360ce|A
tiworker|A
";
            File.WriteAllText(filterPath, defaultFilter);
        }
        catch { }
    }
}