# ------------------------------
# RyTuneX Installer Script
# ------------------------------

# --- Set Window Title ---
$host.UI.RawUI.WindowTitle = "RyTuneX Installer"

# --- Relaunch as Administrator ---
function Ensure-Admin {

    $identity  = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)

    if ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        return
    }

    Write-Host "Restarting installer as Administrator..." -ForegroundColor Yellow

    if ($PSCommandPath) {
        # Local .ps1 execution
        Start-Process powershell.exe -Verb RunAs -ArgumentList @(
            "-NoExit",
            "-ExecutionPolicy", "Bypass",
            "-File", $PSCommandPath
        )
    }
    else {
        # Remote (irm | iex) execution
        $url = "https://raw.githubusercontent.com/rayenghanmi/rytunex/main/install.ps1"

        Start-Process powershell.exe -Verb RunAs -ArgumentList @(
            "-NoExit",
            "-ExecutionPolicy", "Bypass",
            "-Command", "irm '$url' | iex"
        )
    }

    exit
}

Ensure-Admin

# --- Banner ---
function Show-Banner {
$banner = @"
  ____       _____                __  __
 |  _ \ _   |_   _|   _ _ __   ___\ \/ /
 | |_) | | | || || | | | '_ \ / _ \\  / 
 |  _ <| |_| || || |_| | | | |  __//  \ 
 |_| \_\\__, ||_| \__,_|_| |_|\___/_/\_\
        |___/                           
"@
    Write-Host $banner -ForegroundColor Magenta
}

# --- Download ---
function Download-FileWithProgress {
    param(
        [string]$Url,
        [string]$Output
    )

    Write-Host "Downloading RyTuneX..." -ForegroundColor Cyan

    Invoke-WebRequest `
        -Uri $Url `
        -OutFile $Output `
        -UseBasicParsing

    Write-Progress -Activity "Downloading RyTuneX" -Completed
}

Show-Banner

$repo = "RayenGhanmi/RyTuneX"
Write-Host "`nFetching latest release info..." -ForegroundColor Yellow

$latestRelease = Invoke-RestMethod "https://api.github.com/repos/$repo/releases/latest"
$asset = $latestRelease.assets | Where-Object { $_.name -like "*Setup.exe" }

if (-not $asset) {
    Write-Host "Installer not found in latest release." -ForegroundColor Red
    exit 1
}

$installerUrl  = $asset.browser_download_url
$installerPath = "$env:TEMP\RyTuneXSetup.exe"

Download-FileWithProgress -Url $installerUrl -Output $installerPath
Write-Host "Download complete!" -ForegroundColor Green

# --- Install ---
Write-Host "Installing RyTuneX..." -ForegroundColor Green
$process = Start-Process `
    -FilePath $installerPath `
    -ArgumentList "--packagemanager" `
    -PassThru `
    -Wait

Write-Progress -Activity "Installing RyTuneX" -Completed

# --- Launch ---
Write-Host "Launching RyTuneX..." -ForegroundColor Green
Start-Process explorer.exe "shell:appsFolder\Rayen.RyTuneX_h37fyha1qbnfe!App"

# --- Cleanup ---
Remove-Item $installerPath -Force -ErrorAction SilentlyContinue
Write-Host "`nRyTuneX installation complete!" -ForegroundColor Cyan

# --- Countdown before exit ---
Write-Host "RyTuneX launched successfully." -ForegroundColor Green

for ($i = 5; $i -ge 1; $i--) {
    Write-Host "`rClosing installer in $i seconds... " -NoNewline -ForegroundColor Yellow
    Start-Sleep -Seconds 1
}

Write-Host "`nInstaller closed." -ForegroundColor Cyan
