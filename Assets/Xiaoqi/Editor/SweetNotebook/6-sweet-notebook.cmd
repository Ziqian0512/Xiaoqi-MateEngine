@echo off
tasklist /FI "IMAGENAME eq Xiaoqi.exe" /NH 2>NUL | find /I "Xiaoqi.exe" >NUL
if not errorlevel 1 (
  echo Another Xiaoqi instance is already running.
  echo Close the current Xiaoqi before opening this workbench.
  pause
  exit /b 1
)
cd /d "%~dp0Xiaoqi"
start "" "%~dp0Xiaoqi\Xiaoqi.exe" -settings-v2 -xq-bench -xq-mode bench
