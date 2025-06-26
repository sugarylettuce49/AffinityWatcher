# AffinityWatcher.ps1
# Monitors running processes and disables Core 0 (affinity mask 14 for 4-thread CPUs)
# Pins this script's own process to core 4 only

# Set this script's own process affinity to core 4 (bit 3)
$myProcess = Get-Process -Id $PID
$myProcess.ProcessorAffinity = 8  # 2^3 = core 4

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

$affinityMask = 14  # cores 1–3 (disable core 0)

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
