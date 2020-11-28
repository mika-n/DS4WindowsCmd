@echo off
echo.
echo Query the name of the current DS4Windows profile, output controller type (Xbox360 vs DS4) and connection method (USB vs BT) and device vendor/product id.
echo This script uses DS4WindowsCmd.exe cmdline tool to talk to the host DS4Windows app running in the background.
echo Run DS4WindowsCmd.exe command in a cmd shell to see the full list of supported commands.
echo The first -command option needs to be in lowercase, but other options are case-insensitive.
echo.

for /f "tokens=*" %%i in ('DS4WindowsCmd.exe -command Query.1.ProfileName')  do set DS_PROFNAME=%%i
for /f "tokens=*" %%i in ('DS4WindowsCmd.exe -command Query.1.OutContType')  do set DS_CONTROLLERTYPE=%%i
for /f "tokens=*" %%i in ('DS4WindowsCmd.exe -command Query.1.ConnType')     do set DS_CONNTYPE=%%i
for /f "tokens=*" %%i in ('DS4WindowsCmd.exe -command Query.1.DeviceVidPid') do set DS_VIDPID=%%i

echo Results are %DS_PROFNAME% %DS_CONTROLLERTYPE% %DS_CONNTYPE% %DS_VIDPID%

DS4WindowsCmd.exe -command Query.1.Charging
if "%errorlevel%" == "1" echo The gamepad is charging a battery

DS4WindowsCmd.exe -command Query.1.Battery
echo The gamepad battery level is %errorlevel%

DS4WindowsCmd.exe -command Query.1.OutputSlotPermanentType
if "%errorlevel%" == "1" echo The output slot uses permanent type

DS4WindowsCmd.exe -command Query.1.OutputSlotInputBound
if "%errorlevel%" == "1" echo The output slot is bound to a physical gamepad input

rem Output slot can be unplugged only when the slot uses permanent type and input is not bound to a gamepad controller at the moment.
rem If you intent to disconnect all gamepads then use -command Stop
DS4WindowsCmd.exe -command OutputSlot.1.Unplug
