# AffinityWatcher.ps1
# Monitors running processes and disables Core 0 (affinity mask 4094 for 12-thread CPUs)
# Pins this script's own process to core 12 only

# Set this script's own process affinity to core 12 (bit 11)
$myProcess = Get-Process -Id $PID
$myProcess.ProcessorAffinity = 2048  # 2^11 = core 12

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

$affinityMask = 4094  # cores 1–11 (disable core 0)

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
