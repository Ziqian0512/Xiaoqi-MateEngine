@echo off
setlocal
cd /d "%~dp0"
powershell -NoProfile -Command "if (Get-Process Xiaoqi -ErrorAction SilentlyContinue) { exit 1 } else { exit 0 }"
if errorlevel 1 (
  echo Xiaoqi is already running. Use its mode switch or exit it before opening this launcher.
  pause
  exit /b 1
)
start "" "%~dp0Xiaoqi\Xiaoqi.exe" -settings-v2 -xq-bench -xq-mode close
endlocal
