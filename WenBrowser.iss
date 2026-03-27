; WenBrowser Inno Setup Script

[Setup]
AppName=WenBrowser (Wen 浏览器)
AppVersion=1.0
DefaultDirName={autopf}\WenBrowser
DefaultGroupName=WenBrowser
OutputBaseFilename=WenBrowser_Setup
Compression=lzma
SolidCompression=yes
SetupIconFile=assets\logo.ico
UninstallDisplayIcon={app}\assets\logo.ico

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{autoprograms}\WenBrowser"; Filename: "{app}\WenBrowser.exe"
Name: "{autodesktop}\WenBrowser"; Filename: "{app}\WenBrowser.exe"

[Run]
Filename: "{app}\WenBrowser.exe"; Description: "{cm:LaunchProgram,WenBrowser}"; Flags: nowait postinstall skipifsilent
