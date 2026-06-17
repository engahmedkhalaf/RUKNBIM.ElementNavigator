[Setup]
AppName=RUKNBIM Element Navigator for Navisworks
AppVersion=1.0.0
AppPublisher=RUKNBIM
; Install to the current user's ApplicationPlugins folder
DefaultDirName={userappdata}\Autodesk\ApplicationPlugins\RUKNBIM.ElementNavigator.bundle
DisableDirPage=yes
DefaultGroupName=RUKNBIM Element Navigator
DisableProgramGroupPage=yes
OutputBaseFilename=RUKNBIM_ElementNavigator_Setup
Compression=lzma
SolidCompression=yes
; Require lowest privileges so it installs in AppData without UAC prompts
PrivilegesRequired=lowest
OutputDir=bin\Installer

[Files]
; Copy the PackageContents.xml directly into the bundle root
Source: "PackageContents.xml"; DestDir: "{app}"; Flags: ignoreversion
; Copy the compiled plugin files into the exact Contents structure Navisworks requires
Source: "bin\Debug\*"; DestDir: "{app}\Contents\RUKNBIM.ElementNavigator"; Flags: ignoreversion recursesubdirs createallsubdirs

[UninstallDelete]
; Ensure the entire bundle folder is removed on uninstall
Type: filesandordirs; Name: "{app}"
