# AffinityWatcher.ps1
# Monitors running processes and disables Core 0 (affinity mask 16382 for 14-thread CPUs)
# Pins this script's own process to core 14 only

# Set this script's own process affinity to core 14 (bit 13)
$myProcess = Get-Process -Id $PID
$myProcess.ProcessorAffinity = 8192  # 2^13 = core 14

$processNames = @(
    "Medal", 
    "MedalEncoder", 
    "obs64", 
    "obs-ffmpeg-mux", 
    "ffmpeg-mux", 
    "chrome", 
    "Discord", 
    "Spotify", 
    #"Steam", will make games use not enough cores
    "SteamWebHelper", 
    "SteamService", 
    #"EpicGamesLauncher", will make games use not enough cores
    "EpicWebHelper", 
    "MicrosoftEdgeUpdate", 
    "msedgewebview2", 
    "tailscaled"
)

$affinityMask = 16382  # cores 1–13 (disable core 0)

while ($true)
{
    foreach ($name in $processNames)
    {
        Get-Process -Name $name -ErrorAction SilentlyContinue | ForEach-Object {
            try {
                $_.ProcessorAffinity = $affinityMask
            } catch {}
        }
    }
    Start-Sleep -Seconds 10
}
