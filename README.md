# DS4WindowsCmd

Command line tool for Ryochan7/DS4Windows host application (https://github.com/Ryochan7/DS4Windows/).

DS4Windows app supports various command line options to start/stop DS4Windows app or controllers, loadProfile or query current property values of the app or profile. However, the host app is Windows GUI application, so especially the use of Query command line option is a bit difficult to integrate with Windows batch scripts (GUI app doesn't have console and it doesn't wait for command completion until returning to a calling process). This DS4WindowsCmd tool solves these problem because this is a "true" Windows console application.

See https://github.com/Ryochan7/DS4Windows/wiki/Command-line-options documentation page for details about supported command line options.

Usage examples:
- DS4WindowsCmd.exe -command Query.1.ProfileName
- DS4WindowsCmd.exe -command Query.1.OutContType
- DS4WindowsCmd.exe -command Query.1.DeviceVidPid
- DS4WindowsCmd.exe -command Query.1.MacAddress  
- DS4WindowsCmd.exe -command LoadProfile.1.SnakeGame
- DS4WindowsCmd.exe -command LoadTempProfile.1.RacingGame
- DS4WindowsCmd.exe -command Shutdown
- DS4WindowsCmd.exe     (running in a cmdline shell without options shows all supported options)

Note! This command line tool does nothing without the host DS4Windows application running in the background, so make sure to download Ryochan7/DS4Windows host application first.

## Downloads

- **[Main builds of DS4WindowsCmd](https://github.com/mika-n/DS4WindowsCmd/releases)**

## Requirements

- Windows 8.1 or newer
- [Microsoft .NET 4.6 or higher](https://dotnet.microsoft.com/download/dotnet-framework)
- [Ryochan7/DS4Windows host application](https://github.com/Ryochan7/DS4Windows/releases)

## License

See "License_DS4WindowsCmd.txt" file for details. This app is provided free of charge and as-is with and without bugs. Use at your own risk. No warranty given.
WTFPL license http://www.wtfpl.net/

