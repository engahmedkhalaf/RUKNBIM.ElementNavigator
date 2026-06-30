; ----------------------------------------------------------------------------
;  RUKNBIM Element Navigator - Inno Setup script
;  Build the project first (Release or Debug); then compile this .iss with ISCC
; ----------------------------------------------------------------------------

#define MyAppName "RUKNBIM Element Navigator for Navisworks"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "RUKNBIM"
#define MyAppURL "https://www.ruknbim.com/"
#define BuildConfig "Debug"

[Setup]
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
; Install to the current user's ApplicationPlugins folder
DefaultDirName={userappdata}\Autodesk\ApplicationPlugins\RUKNBIM.ElementNavigator.bundle
DisableDirPage=yes
DefaultGroupName=RUKNBIM Element Navigator
DisableProgramGroupPage=yes
OutputBaseFilename=RUKNBIM_ElementNavigator_Setup_{#MyAppVersion}
Compression=lzma
SolidCompression=yes
; Run per-user, no UAC prompt
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=bin\Installer
ArchitecturesInstallIn64BitMode=x64
WizardStyle=modern
SetupIconFile=Images\ElementNavigatorAddin_16.ico
UninstallDisplayIcon={app}\Contents\RUKNBIM.ElementNavigator\Images\ElementNavigatorAddin_16.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; Bundle manifest at the bundle root
Source: "PackageContents.xml"; DestDir: "{app}"; Flags: ignoreversion

; Compiled plugin output (DLL + dependencies + Images subfolder)
Source: "bin\{#BuildConfig}\*"; DestDir: "{app}\Contents\RUKNBIM.ElementNavigator"; \
    Flags: ignoreversion recursesubdirs createallsubdirs

; Ribbon icon folder (Navisworks looks for {PluginId}.{Developer}\*)
Source: "ElementNavigatorAddin.RUKN\*"; \
    DestDir: "{app}\Contents\RUKNBIM.ElementNavigator\ElementNavigatorAddin.RUKN"; \
    Flags: ignoreversion recursesubdirs createallsubdirs

[Run]
Filename: "{#MyAppURL}"; Description: "Visit RUKNBIM website"; \
    Flags: shellexec postinstall skipifsilent unchecked

[UninstallDelete]
; Remove the entire bundle folder on uninstall
Type: filesandordirs; Name: "{app}"
