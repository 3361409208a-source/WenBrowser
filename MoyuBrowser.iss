; MoyuBrowser Inno Setup Script

[Setup]
AppName=MoyuBrowser (摸鱼浏览器)
AppVersion=1.0
DefaultDirName={autopf}\MoyuBrowser
DefaultGroupName=MoyuBrowser
OutputBaseFilename=MoyuBrowser_Setup
Compression=lzma
SolidCompression=yes
SetupIconFile=d:\xiangmu\MoyuBrowser\assets\logo.ico
UninstallDisplayIcon={app}\assets\logo.ico

[Files]
Source: "d:\xiangmu\MoyuBrowser\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{autoprograms}\MoyuBrowser"; Filename: "{app}\MoyuBrowser.exe"
Name: "{autodesktop}\MoyuBrowser"; Filename: "{app}\MoyuBrowser.exe"

[Run]
Filename: "{app}\MoyuBrowser.exe"; Description: "{cm:LaunchProgram,MoyuBrowser}"; Flags: nowait postinstall skipifsilent
