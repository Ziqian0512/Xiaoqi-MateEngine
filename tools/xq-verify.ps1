<#
  xq-verify.ps1 - one-shot build + smoke check for the Xiaoqi Unity project (Windows PowerShell 5.1).
  Written by WB (WorkBuddy) without a Unity editor at hand: READ IT BEFORE RUNNING, then run it ONCE per build.

  What it does (and nothing else):
    1. optional: git fetch / checkout <Branch> / pull --ff-only
    2. refuses to run if the working tree has uncommitted changes (unless -Force)
    3. runs Unity batchmode: XiaoqiBuild.Win64  -> Build\Xiaoqi\Xiaoqi.exe
    4. parses the log: compile errors (error CS....), warning count, build result
    5. lists files the build generated/changed (git status), so nobody commits them by accident
    6. smoke: starts Xiaoqi.exe with a few demo flags, waits, takes a full-screen screenshot, copies the
       XQ_* lines of Player.log, stops the process
    7. writes Build\evidence\<sha>\summary.md  (+ copies evidence to -CopyTo if given)

  It does NOT: push, commit, edit source, touch xiaoqi_config.json of an installed copy, send any token anywhere.

  Usage (from anywhere):
    powershell -ExecutionPolicy Bypass -File tools\xq-verify.ps1 -Project "D:\path\to\Xiaoqi-MateEngine" -Branch xiaoqi/p1
  Optional: -NoGit (do not fetch/checkout/pull; build whatever is checked out - use when GitHub is unreachable)  -Unity "C:\Program Files\Unity\Hub\Editor\6000.2.6f2\Editor\Unity.exe"  -NoBuild  -NoSmoke  -Force  -CopyTo <dir>
#>
param(
  [string]$Project = (Get-Location).Path,
  [string]$Unity = "",
  [string]$Branch = "",
  [switch]$NoBuild,
  [switch]$NoSmoke,
  [switch]$Force,
  [switch]$NoGit,
  [string]$CopyTo = "",
  [int]$SmokeWaitSec = 20
)

$ErrorActionPreference = 'Continue'
Set-Location $Project

function Say($m) { Write-Host ("[xq-verify] " + $m) }

# ---------- 1. git ----------
if ($Branch -ne "" -and -not $NoGit) {
  git fetch origin 2>&1 | Out-Null
  git checkout $Branch 2>&1 | Out-Null
  git pull --ff-only origin $Branch 2>&1 | Out-Null
}
$sha = (git rev-parse --short HEAD).Trim()
$branchNow = (git rev-parse --abbrev-ref HEAD).Trim()
Say ("project=" + $Project + " branch=" + $branchNow + " sha=" + $sha)

# Files that a previous build leaves behind (XiaoqiIdentity.Apply rewrites ProjectSettings.asset on every build, etc.).
# They are NOT source changes and are ignored by the clean check. Anything else (especially *.cs) aborts.
$known = '(addressables_content_state\.bin|CurrentRP\.txt|ProjectSettings/ProjectSettings\.asset|presets_known_materials\.txt|EditorUserSettings\.asset|StreamingAssets/LLMManager\.json|fake_day\.meta|__VrmaBake)'
$dirty = (git status --porcelain) | Where-Object { $_ -notmatch '^\?\? (Build|Library|Temp|Logs)/' -and $_ -notmatch $known }
$leftover = (git status --porcelain) | Where-Object { $_ -match $known }
if ($leftover) { Say ("ignoring " + @($leftover).Count + " known build-leftover change(s)") }
if ($dirty -and -not $Force) {
  Say "ABORT: working tree is not clean. Commit or stash first, or pass -Force. Changed files:"
  $dirty | ForEach-Object { Say ("  " + $_) }
  exit 2
}

$ev = Join-Path $Project ("Build\evidence\" + $sha)
New-Item -ItemType Directory -Force -Path $ev | Out-Null
$summary = New-Object System.Collections.ArrayList
[void]$summary.Add("# xq-verify summary")
[void]$summary.Add("")
[void]$summary.Add("- time: " + (Get-Date -Format "yyyy-MM-dd HH:mm:ss"))
[void]$summary.Add("- machine: " + $env:COMPUTERNAME)
[void]$summary.Add("- branch: " + $branchNow + "  sha: " + $sha)

# ---------- 2. find Unity ----------
$ver = "6000.2.6f2"
try {
  $pv = Get-Content (Join-Path $Project "ProjectSettings\ProjectVersion.txt") | Select-String "m_EditorVersion:" | Select-Object -First 1
  if ($pv) { $ver = ($pv.ToString() -replace '.*m_EditorVersion:\s*', '').Trim() }
} catch {}
if ($Unity -eq "") {
  $cands = @()
  if ($env:XQ_UNITY) { $cands += $env:XQ_UNITY }
  foreach ($d in @("C:", "D:", "E:", "F:")) {
    $cands += ($d + "\Program Files\Unity\Hub\Editor\" + $ver + "\Editor\Unity.exe")
    $cands += ($d + "\Unity\Hub\Editor\" + $ver + "\Editor\Unity.exe")
    $cands += ($d + "\Unity\Editor\" + $ver + "\Editor\Unity.exe")
  }
  foreach ($c in $cands) { if ($c -and (Test-Path $c)) { $Unity = $c; break } }
}
if (-not $NoBuild) {
  if ($Unity -eq "" -or -not (Test-Path $Unity)) {
    Say ("ABORT: Unity " + $ver + " not found. Pass -Unity <path to Unity.exe>.")
    exit 3
  }
  [void]$summary.Add("- unity: " + $Unity + " (project wants " + $ver + ")")
}

# ---------- 3. build ----------
$exe = Join-Path $Project "Build\Xiaoqi\Xiaoqi.exe"
$buildOk = $false
$logFile = Join-Path $Project ("Build\verify-" + $sha + ".log")
New-Item -ItemType Directory -Force -Path (Join-Path $Project "Build") | Out-Null
if (-not $NoBuild) {
  if (Test-Path (Join-Path $Project "Temp\UnityLockfile")) {
    Say "NOTE: Temp\UnityLockfile exists - if a Unity editor has this project open, close it first."
  }
  Say ("building ... log: " + $logFile)
  $t0 = Get-Date
  $uargs = @("-batchmode", "-quit", "-projectPath", ('"' + $Project + '"'), "-buildTarget", "Win64",
            "-executeMethod", "XiaoqiBuild.Win64", "-logFile", ('"' + $logFile + '"'))
  $p = Start-Process -FilePath $Unity -ArgumentList $uargs -PassThru -WindowStyle Hidden
  if (-not $p.WaitForExit(2400000)) { try { $p.Kill() } catch {}; Say "build timed out (40 min)" }
  $secs = [int]((Get-Date) - $t0).TotalSeconds
  $exit = $p.ExitCode
  Copy-Item $logFile (Join-Path $ev "build.log") -ErrorAction SilentlyContinue

  $errs = @(); $warnN = 0; $okLine = ""
  if (Test-Path $logFile) {
    $errs = @(Select-String -Path $logFile -Pattern 'error CS\d+' | ForEach-Object { $_.Line.Trim() } | Select-Object -Unique)
    $warnN = @(Select-String -Path $logFile -Pattern 'warning CS\d+').Count
    $ok = Select-String -Path $logFile -Pattern 'Build completed with a result of ''(\w+)''' | Select-Object -Last 1
    if ($ok) { $okLine = $ok.Matches[0].Groups[1].Value }
  }
  $buildOk = ($exit -eq 0 -and (Test-Path $exe))
  [void]$summary.Add("")
  [void]$summary.Add("## Build")
  [void]$summary.Add("- unity exit code: " + $exit + "   build result line: '" + $okLine + "'   seconds: " + $secs)
  [void]$summary.Add("- compile errors: " + $errs.Count + "   warnings: " + $warnN)
  [void]$summary.Add("- exe exists: " + (Test-Path $exe))
  [void]$summary.Add("- RESULT: " + $(if ($buildOk) { "BUILD OK" } else { "BUILD FAILED" }))
  if ($errs.Count -gt 0) {
    [void]$summary.Add("")
    [void]$summary.Add("### compile errors (file(line,col): message)")
    $errs | Select-Object -First 60 | ForEach-Object { [void]$summary.Add("    " + $_) }
  }
  if (-not $buildOk -and (Test-Path $logFile)) {
    [void]$summary.Add("")
    [void]$summary.Add("### last 40 lines of log")
    Get-Content $logFile -Tail 40 | ForEach-Object { [void]$summary.Add("    " + $_) }
  }
  $gen = (git status --porcelain) | Where-Object { $_ -notmatch '^\?\? (Build|Library|Temp|Logs)/' -and $_ -notmatch $known }
  [void]$summary.Add("")
  [void]$summary.Add("### files changed by the build (DO NOT commit prefab/scene/ProjectSettings; new .meta files are fine)")
  if ($gen) { $gen | ForEach-Object { [void]$summary.Add("    " + $_) } } else { [void]$summary.Add("    (none)") }
} else {
  $buildOk = (Test-Path $exe)
  [void]$summary.Add("- build skipped (-NoBuild), exe exists: " + $buildOk)
}

# ---------- 4. smoke ----------
if (-not $NoSmoke -and $buildOk) {
  Add-Type -AssemblyName System.Windows.Forms, System.Drawing
  Add-Type @"
using System.Runtime.InteropServices;
public class XqDpi { [DllImport("user32.dll")] public static extern bool SetProcessDPIAware(); }
"@
  [void][XqDpi]::SetProcessDPIAware()
  $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
  [void]$summary.Add("")
  [void]$summary.Add("## Smoke (primary screen " + $b.Width + "x" + $b.Height + ")")
  $fakeDay = Join-Path $Project "Build\Xiaoqi\Xiaoqi_Data\StreamingAssets\Xiaoqi\fake_day\sample.json"
  $scen = @(
    @{ n = "base";        a = @("-fake-stage", "stand_tidy", "-fake-day", $fakeDay) },
    @{ n = "nap";         a = @("-fake-stage", "stand_tidy", "-demo-nap") },
    @{ n = "theme-blue";  a = @("-fake-stage", "stand_tidy", "-xq-theme", "blue") },
    @{ n = "settings-v2"; a = @("-fake-stage", "stand_tidy", "-settings-v2", "-demo-settings") }
  )
  $playerLog = Join-Path $env:USERPROFILE "AppData\LocalLow\Xiaoqi\Xiaoqi\Player.log"
  foreach ($s in $scen) {
    Get-Process Xiaoqi -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    Remove-Item $playerLog -ErrorAction SilentlyContinue
    $sp = Start-Process -FilePath $exe -ArgumentList $s.a -WorkingDirectory (Split-Path $exe) -PassThru
    Start-Sleep -Seconds $SmokeWaitSec
    $alive = -not $sp.HasExited
    $shot = Join-Path $ev ("smoke-" + $s.n + ".png")
    try {
      $bmp = New-Object System.Drawing.Bitmap $b.Width, $b.Height
      $g = [System.Drawing.Graphics]::FromImage($bmp)
      $g.CopyFromScreen($b.Location, [System.Drawing.Point]::Empty, $b.Size)
      $bmp.Save($shot, [System.Drawing.Imaging.ImageFormat]::Png); $g.Dispose(); $bmp.Dispose()
    } catch { Say ("screenshot failed: " + $_.Exception.Message) }
    $xq = @(); $exc = 0
    if (Test-Path $playerLog) {
      Copy-Item $playerLog (Join-Path $ev ("player-" + $s.n + ".log")) -ErrorAction SilentlyContinue
      $xq = @(Select-String -Path $playerLog -Pattern 'XQ_|XIAOQI_' | ForEach-Object { $_.Line.Trim() })
      $exc = @(Select-String -Path $playerLog -Pattern 'Exception|NullReference|error CS|ERROR').Count
      $xq | Out-File (Join-Path $ev ("xq-lines-" + $s.n + ".txt")) -Encoding utf8
    }
    if (-not $sp.HasExited) { try { $sp.Kill() } catch {} }
    [void]$summary.Add("- **" + $s.n + "**: alive after " + $SmokeWaitSec + "s = " + $alive + "; XQ_ lines = " + $xq.Count + "; exception-ish lines = " + $exc + "; screenshot = smoke-" + $s.n + ".png")
  }
  Get-Process Xiaoqi -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
  [void]$summary.Add("")
  [void]$summary.Add("NOTE: smoke only proves the program starts and logs. Click-through, dancing, sleeping, fullscreen-hide, 175% scale need a human at the screen (see G60 B1 table).")
}

$sumFile = Join-Path $ev "summary.md"
$summary | Out-File $sumFile -Encoding utf8
Say ("done. summary: " + $sumFile)
Get-Content $sumFile | ForEach-Object { Write-Host $_ }
if ($CopyTo -ne "") {
  New-Item -ItemType Directory -Force -Path $CopyTo | Out-Null
  Copy-Item (Join-Path $ev "*") $CopyTo -Recurse -Force
  Say ("evidence copied to " + $CopyTo)
}
if ($buildOk) { exit 0 } else { exit 1 }

