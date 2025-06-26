# AffinityWatcher.ps1
# Monitors running processes and disables Core 0 (auto-calculated affinity mask)
# Pins this script's own process to the last core available

# Get logical processor count
$logicalProcessors = [Environment]::ProcessorCount

# Validate at least 2 cores
if ($logicalProcessors -lt 2)
{
    Write-Host "Not enough logical cores to apply affinity rules."
    exit
}

# Set this script's own process affinity to the last core
$lastCoreMask = 1 -shl ($logicalProcessors - 1)
(Get-Process -Id $PID).ProcessorAffinity = $lastCoreMask  # pin to last core

# Build affinity mask that disables core 0 (bit 0 = 0) and enables the rest (bits 1 to N)
$affinityMask = 0
for ($i = 1; $i -lt $logicalProcessors; $i++)
{
    $affinityMask = $affinityMask -bor (1 -shl $i)
}

# Debug output (optional)
# Write-Host "Detected $logicalProcessors logical cores"
# Write-Host "Affinity mask for target processes: $affinityMask"
# Write-Host "Watcher script is pinned to core $($logicalProcessors)"

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
