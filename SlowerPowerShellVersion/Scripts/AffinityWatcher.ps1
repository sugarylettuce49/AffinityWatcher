# AffinityWatcher.ps1
# Monitors running processes and disables Core 0 and Core 1 (auto-calculated affinity mask)
# Pins this script's own process to use all threads except the first two

# Get logical processor count
$logicalProcessors = [Environment]::ProcessorCount

# Validate at least 3 cores
if ($logicalProcessors -lt 3)
{
    Write-Host "Not enough logical cores to apply affinity rules (need at least 3)."
    exit
}

# Build affinity mask that disables Core 0 and Core 1
$affinityMask = 0
for ($i = 2; $i -lt $logicalProcessors; $i++)
{
    $affinityMask = $affinityMask -bor (1 -shl $i)
}

# Pin this script's own process to all cores except the first two
$currentProcess = Get-Process -Id $PID
$currentProcess.ProcessorAffinity = $affinityMask
$currentProcess.PriorityClass = 'BelowNormal'

$processNames = @(
    "Medal", 
    "MedalEncoder",
    "crashpad_handler",
    "obs64", 
    "obs-ffmpeg-mux", 
    "ffmpeg-mux", 
    "chrome", 
    "Discord", 
    "Spotify", 
    #"Steam",
    "SteamWebHelper", 
    "SteamService", 
    #"EpicGamesLauncher",
    "EpicWebHelper", 
    #"EADesktop",
    "EACefSubProcess", 
    "MicrosoftEdgeUpdate", 
    "msedgewebview2", 
    "crossover", 
    "x3", 
    "VirtualDesktop.Service",
    "sunshine",
    "sunshinesvc",
    "spoolsv",
    "SearchIndexer",
    "tailscaled",
    "tailscale-ipn"
)

while ($true)
{
    # Get all processes once per loop instead of per process name
    $allProcesses = Get-Process -ErrorAction SilentlyContinue
    
    foreach ($name in $processNames)
    {
        $allProcesses | Where-Object {$_.Name -eq $name} | ForEach-Object {
            try {
                if ($_.ProcessorAffinity -ne $affinityMask)
                {
                    $_.ProcessorAffinity = $affinityMask
                }
            } catch {}
        }
    }
    
    Start-Sleep -Seconds 15
}