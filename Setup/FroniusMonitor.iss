#define AppName "Fronius Monitor"
#define AppExeFileName "FroniusMonitor"
#define ApplicationVersion GetFileVersion('..\FroniusMonitor\bin\Release\net6.0-windows7.0\FroniusMonitor.exe')

[Setup]
AppName={#AppName}
AppVersion={#ApplicationVersion}
AppVerName={#AppName} {#ApplicationVersion}
AppCopyright=Copyright (c) 2021-2022, Christoph Hochstätter
DefaultDirName={pf64}\Hochstätter\{#AppName}
UninstallDisplayIcon={app}\{#AppExeFileName}.exe
Compression=lzma2
SolidCompression=yes
DefaultGroupName={#AppName}
AppPublisher=Christoph Hochstätter
;SetupIconFile=..\FroniusMonitor\Assets\Images\sun.ico
OutputBaseFilename={#AppName}-Setup-v.{#ApplicationVersion}
AppUpdatesURL=https://github.com/christoh/FroniusMonitor
AppSupportURL=https://github.com/christoh/FroniusMonitor
AppPublisherURL=http://www.hochstaetter.de
MinVersion=0,6.1.0
LicenseFile=..\LICENSE
ChangesAssociations=yes
AlwaysRestart=no
RestartIfNeededByRun=no

[InstallDelete]
Type: filesandordirs; Name: {app}\*;

[Files]
Source: "..\FroniusMonitor\bin\Release\net6.0-windows7.0\*"; DestDir: "{app}"; Excludes:"IoOfficeApp.exe.WebView2"; Flags: recursesubdirs
Source: "windowsdesktop-runtime-6.0.7-win-x64.exe"; DestDir: "{tmp}";

[Messages]
SetupWindowTitle={#AppName} {#ApplicationVersion} Setup
WindowsVersionNotSupported={#AppName} requires Windows 7 or a later version of Windows.

[Icons]
Name: "{commonprograms}\Hochstätter\{#AppName}\{#AppExeFileName}"; Filename: "{app}\{#AppExeFileName}.exe"
Name: "{userdesktop}\{#AppName}"; Filename: "{app}\{#AppExeFileName}.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Run]
Filename: "{tmp}\windowsdesktop-runtime-6.0.7-win-x64.exe"; Description:"Install .NET 6"; WorkingDir: "{tmp}"; Parameters: "/install /quiet /norestart";  StatusMsg: "Installing .NET 6 ..."; BeforeInstall: SetMarqueeProgress(True);
Filename: {app}\{#AppExeFileName}.exe; Description: {cm:LaunchProgram,{#AppName}}; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNetDetected(version: String; service: Integer): Boolean;
var
    key, versionKey: string;
    install, release, serviceCount, versionRelease: cardinal;
    success: boolean;
begin
    versionKey := version;
    versionRelease := 0;

    // .NET 1.1 and 2.0 embed release number in version key
    if version = 'v1.1' then begin
        versionKey := 'v1.1.4322';
    end else if version = 'v2.0' then begin
        versionKey := 'v2.0.50727';
    end

    // .NET 4.5 and newer install as update to .NET 4.0 Full
    else if Pos('v4.', version) = 1 then begin
        versionKey := 'v4\Full';
        case version of
          'v4.5':   versionRelease := 378389;
          'v4.5.1': versionRelease := 378675; // 378758 on Windows 8 and older
          'v4.5.2': versionRelease := 379893;
          'v4.6':   versionRelease := 393295; // 393297 on Windows 8.1 and older
          'v4.6.1': versionRelease := 394254; // 394271 before Win10 November Update
          'v4.6.2': versionRelease := 394802; // 394806 before Win10 Anniversary Update
          'v4.7':   versionRelease := 460798; // 460805 before Win10 Creators Update
		      'v4.7.1': versionRelease := 461308;
	    	  'v4.7.2': versionRelease := 461808;
		      'v4.8'  : versionRelease := 528040;
        end;
    end;

    // installation key group for all .NET versions
    key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\' + versionKey;

    // .NET 3.0 uses value InstallSuccess in subkey Setup
    if Pos('v3.0', version) = 1 then begin
        success := RegQueryDWordValue(HKLM, key + '\Setup', 'InstallSuccess', install);
    end else begin
        success := RegQueryDWordValue(HKLM, key, 'Install', install);
    end;

    // .NET 4.0 and newer use value Servicing instead of SP
    if Pos('v4', version) = 1 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Servicing', serviceCount);
    end else begin
        success := success and RegQueryDWordValue(HKLM, key, 'SP', serviceCount);
    end;

    // .NET 4.5 and newer use additional value Release
    if versionRelease > 0 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Release', release);
        success := success and (release >= versionRelease);
    end;

    result := success and (install = 1) and (serviceCount >= service);
end;

procedure SetMarqueeProgress(Marquee: Boolean);
begin
  if Marquee then
  begin
    WizardForm.ProgressGauge.Style := npbstMarquee;
  end
    else
  begin
    WizardForm.ProgressGauge.Style := npbstNormal;
  end;
end;
