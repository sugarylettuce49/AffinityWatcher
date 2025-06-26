# AffinityWatcher.ps1
# Monitors running processes and disables Core 0 (affinity mask 65534 for 16-thread CPUs)
# Pins this script's own process to core 16 only

# Set this script's own process affinity to core 16 (bit 15)
$myProcess = Get-Process -Id $PID
$myProcess.ProcessorAffinity = 32768  # 2^15 = core 16

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

$affinityMask = 65534  # cores 1–15 (disable core 0)

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
