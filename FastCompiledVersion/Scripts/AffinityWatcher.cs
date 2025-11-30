using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Win32;

class AffinityWatcher
{
    static void Main()
    {
        // Check for settings file next to the executable
        string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string exeDirectory = Path.GetDirectoryName(exePath);
        string settingsPath = Path.Combine(exeDirectory, "MICAHTECHSETTINGS");
        bool settingsFileExists = File.Exists(settingsPath);

        // Get logical processor count
        int logicalProcessors = Environment.ProcessorCount;

        // Validate at least 3 cores
        if (logicalProcessors < 3)
        {
            return; // Exit silently in background mode
        }

        // Build affinity mask that disables Core 0 and Core 1
        IntPtr affinityMask = IntPtr.Zero;
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

        // If settings file exists, set Win32PrioritySeparation to 40 (0x28)
        /*if (settingsFileExists)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\PriorityControl", true))
                {
                    if (key != null)
                    {
                        key.SetValue("Win32PrioritySeparation", 40, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }*/

        // Process names to monitor
        string[] processNames = new string[]
        {
            "Medal",
            "MedalEncoder",
            "crashpad_handler",
            "obs64",
            "obs-ffmpeg-mux",
            "ffmpeg-mux",
            "chrome",
            "Discord",
            "Spotify",
            "SteamWebHelper",
            "SteamService",
            "EpicWebHelper",
            "EACefSubProcess",
            "MicrosoftEdgeUpdate",
            "msedgewebview2",
            "crossover",
            "x3",
            "sunshine",
            "sunshinesvc",
            "spoolsv",
            "SearchIndexer",
            "tailscaled",
            "tailscale-ipn",
            "netbird",
            "netbird-ui",
            "NVIDIA Broadcast",
            "audiodg"
        };

        // Determine sleep interval based on settings file
        int sleepInterval = settingsFileExists ? 15000 : 15000; // 15 seconds

        // Main monitoring loop
        while (true)
        {
            try
            {
                // Get all processes once
                Process[] allProcesses = Process.GetProcesses();

                foreach (string name in processNames)
                {
                    var matchingProcesses = allProcesses.Where(p => 
                        p.ProcessName.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();

                    foreach (Process proc in matchingProcesses)
                    {
                        try
                        {
                            // Check if process still exists
                            if (proc.HasExited)
                            {
                                continue;
                            }
                            
                            // Set affinity to exclude cores 0 and 1
                            proc.ProcessorAffinity = affinityMask;
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
}