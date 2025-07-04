# AffinityWatcher.ps1
# Monitors running processes and disables Core 0 and Core 1 (auto-calculated affinity mask)
# Pins this script's own process to the last core available

# Get logical processor count
$logicalProcessors = [Environment]::ProcessorCount

# Validate at least 3 cores
if ($logicalProcessors -lt 3)
{
    Write-Host "Not enough logical cores to apply affinity rules (need at least 3)."
    exit
}

# Pin this script's own process to the last core
$lastCoreMask = 1 -shl ($logicalProcessors - 1)
(Get-Process -Id $PID).ProcessorAffinity = $lastCoreMask

# Build affinity mask that disables Core 0 and Core 1
$affinityMask = 0
for ($i = 2; $i -lt $logicalProcessors; $i++)
{
    $affinityMask = $affinityMask -bor (1 -shl $i)
}

$processNames = @(
    "Medal", 
    "MedalEncoder", 
    "obs64", 
    "obs-ffmpeg-mux", 
    "ffmpeg-mux", 
    "chrome", 
    "Discord", 
    "Spotify", 
    #"Steam", (Would make games not use core 0 also if added)
    "SteamWebHelper", 
    "SteamService", 
    #"EpicGamesLauncher", (Would make games not use core 0 also if added)
    "EpicWebHelper", 
    #"EADesktop", (Would make games not use core 0 also if added)
    "EACefSubProcess", 
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
                if ($_.ProcessorAffinity -ne $affinityMask)
                {
                    $_.ProcessorAffinity = $affinityMask
                }
            } catch {}
        }
    }
    Start-Sleep -Seconds 10
}
