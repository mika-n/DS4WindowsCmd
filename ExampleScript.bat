@echo off
echo.
echo Query the name of the current DS4Windows profile, output controller type (Xbox360 vs DS4) and connection method (USB vs BT) and device vendor/product id.
echo This script uses DS4WindowsCmd.exe cmdline tool to talk to the host DS4Windows app running in the background.
echo Run DS4WindowsCmd.exe command in a cmd shell to see the full list of supported commands.
echo.

for /f "tokens=*" %%i in ('DS4WindowsCmd.exe -command Query.1.ProfileName')  do set DS_PROFNAME=%%i
for /f "tokens=*" %%i in ('DS4WindowsCmd.exe -command Query.1.OutContType')  do set DS_CONTROLLERTYPE=%%i
for /f "tokens=*" %%i in ('DS4WindowsCmd.exe -command Query.1.ConnType')     do set DS_CONNTYPE=%%i
for /f "tokens=*" %%i in ('DS4WindowsCmd.exe -command Query.1.DeviceVidPid') do set DS_VIDPID=%%i

echo Results are %DS_PROFNAME% %DS_CONTROLLERTYPE% %DS_CONNTYPE% %DS_VIDPID%
