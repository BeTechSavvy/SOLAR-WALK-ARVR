$ErrorActionPreference='Stop'
$R="C:\Program Files\Unity 6000.0.44f1"; $U="$R\Editor\Data\PlaybackEngines\AndroidPlayer"
$D="C:\SWdl"; New-Item -ItemType Directory -Force $D | Out-Null; Set-Location $D
function Get($url){ $f=Split-Path $url -Leaf; Write-Host "Downloading $f"; curl.exe -L --retry 10 --retry-delay 3 -C - -o $f $url; if($LASTEXITCODE -and $LASTEXITCODE -ne 33){throw "Download failed: $f"}; return "$D\$f" }
function X($zip,$dest){ New-Item -ItemType Directory -Force $dest | Out-Null; Write-Host "Extracting $(Split-Path $zip -Leaf)"; tar -xf $zip -C $dest; if($LASTEXITCODE){throw "Extract failed: $zip"} }
function Ren($from,$to){ if((Test-Path $from) -and -not (Test-Path $to)){ Rename-Item $from (Split-Path $to -Leaf) } }
$a=Get "https://download.unity3d.com/download_unity/101c91f3a8fb/TargetSupportInstaller/UnitySetup-Android-Support-for-Editor-6000.0.44f1.exe"
Write-Host "Installing Android Build Support..."; Start-Process $a -ArgumentList "/S /D=$R" -Wait
if(-not (Test-Path $U)){ throw "Android Build Support did not install into $U" }
X (Get "https://download.unity3d.com/download_unity/open-jdk/open-jdk-win-x64/jdk17.0.9-9_f12c2989c2f749b13282640a12d7d624097f6c2d45144d87331f21ad352ab63e.zip") "$U\OpenJDK"
X (Get "https://dl.google.com/android/repository/sdk-tools-windows-4333796.zip") "$U\SDK"
X (Get "https://dl.google.com/android/repository/platform-tools_r34.0.5-windows.zip") "$U\SDK"
X (Get "https://dl.google.com/android/repository/build-tools_r34-windows.zip") "$U\SDK\build-tools"; Ren "$U\SDK\build-tools\android-14" "$U\SDK\build-tools\34.0.0"
X (Get "https://dl.google.com/android/repository/platform-33_r02.zip") "$U\SDK\platforms"; Ren "$U\SDK\platforms\android-13" "$U\SDK\platforms\android-33"
X (Get "https://dl.google.com/android/repository/platform-34-ext7_r02.zip") "$U\SDK\platforms"
X (Get "https://dl.google.com/android/repository/platform-35_r01.zip") "$U\SDK\platforms"
X (Get "https://dl.google.com/android/repository/commandlinetools-win-8092744_latest.zip") "$U\SDK\cmdline-tools"; Ren "$U\SDK\cmdline-tools\cmdline-tools" "$U\SDK\cmdline-tools\6.0"
X (Get "https://dl.google.com/android/repository/cmake-3.22.1-windows.zip") "$U\SDK\cmake\3.22.1"
X (Get "https://dl.google.com/android/repository/android-ndk-r27c-windows.zip") "$U"; Ren "$U\android-ndk-r27c" "$U\NDK"
New-Item -ItemType Directory -Force "$U\SDK\licenses" | Out-Null
Set-Content "$U\SDK\licenses\android-sdk-license" "`n24333f8a63b6825ea9c5514f83c2829b004d1fee`nd56f5187479451eabf01fb78af6dfcb131a6481e`n8933bad161af4178b1185d1a37fbf41ea5269c55"
Write-Host "`nALL DONE - Android tools installed" -ForegroundColor Green
