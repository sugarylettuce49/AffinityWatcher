# AffinityWatcher.ps1
# Monitors running processes and disables Core 0 (affinity mask 16777214 for 24-thread CPUs)
# Pins this script's own process to core 24 only

# Set this script's own process affinity to core 24 (bit 23)
$myProcess = Get-Process -Id $PID
$myProcess.ProcessorAffinity = 8388608  # 2^23 = core 24

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

$affinityMask = 16777214  # cores 1–23 (disable core 0)

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
