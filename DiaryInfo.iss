; Inno Setup source
; Features:
; - x86 and x64 support in one file
; - Language support
; - Autorun via WinRegistry
; - Append Path sample [deactivate]
; - Write install path to Registry.
; - Icons
; - Check .NET Framework (method from internet)
; - Install .NET Framework if it not exists
; - Set default dir from script
; - Prev Installations Options
#define MyAppName "DiaryInfo" 
#define MyAppVersion GetFileVersion("C:\Users\%UserName%\Documents\Atlassian\DiaryInfo\DiaryInfo\bin\x86\Release\DiaryInfo.exe")
;"2.5.2.0" 
#define MyAppPublisher "Yuriy Astrov" 
#define MyAppURL "https://github.com/yastrov/DiaryInfo"
#define MyAppUpdatedUrl "https://github.com/yastrov/DiaryInfo/releases"
#define MyAppExeName "DiaryInfo.exe"
#define MyDistFolder "C:\Users\%UserName%\Documents\Atlassian\DiaryInfo"  

[Setup]
;GUID no needed really
AppId={{AFEF5AE0-F8A5-4A4E-AFAD-FFCF6DD60526}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultGroupName={#MyAppName}
DefaultDirName={pf}\{#MyAppName}
;DefaultDirName={code:GetDefaultDir}

;About App
AppVerName={#MyAppName} version {#MyAppVersion}
AppCopyright=Copyright (C) {#MyAppPublisher}
AppPublisher={#MyAppPublisher} 
AppPublisherURL={#MyAppURL}
AppUpdatesURL={#MyAppUpdatedUrl}

;Config Installator
VersionInfoVersion=0.0.0.0
VersionInfoDescription=Notifier for http://www.diary.ru
VersionInfoCopyright={#MyAppPublisher}
VersionInfoProductVersion={#MyAppVersion}

AllowNoIcons=yes
;SetupIconFile={#MyDistFolder}DiaryInfo/images/MainIcon.ico
OutputBaseFilename={#MyAppName}{#MyAppVersion}
ArchitecturesInstallIn64BitMode=x64  
Compression=bzip
;Win7, because .NET 4.5 needed
MinVersion=6.1.7600

;Previous installation
UsePreviousAppDir=yes 
UsePreviousGroup=yes
UsePreviousSetupType=yes
UsePreviousTasks=yes

;Uninstaller
Uninstallable=not IsTaskSelected('portablemode')
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallFilesDir={app}\uninst

[Tasks]
Name: "autorunmode"; Description: "{cm:CreateAutorunM}"
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "desktopicon\common"; Description: "{cm:ForAllUsersM}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: exclusive
Name: "desktopicon\user"; Description: "{cm:CurrentUsersOnlyM}"; GroupDescription: "{cm:AdditionalIconsGroupM}"; Flags: exclusive unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIconsGroupM}"; Flags: unchecked
Name: "portablemode"; Description: "{cm:PortableModeM}"; Flags: unchecked
;Name: pathappend; Description: "{cm:PathAppendM}"
Name: "writeinstalledpath"; Description: "{cm:WriteInstalledPathM}"

[Registry]
Root: HKCU; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autorunmode
Root: HKLM; Subkey: Software\YAstrov\{#MyAppName}; ValueType: string; ValueName: InstallPath; ValueData: {app}; Tasks: writeinstalledpath
;Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"; ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}";Tasks: pathappend 

[Files]
;Source: "CTL3DV2.DLL"; DestDir: "{sys}"; Flags: onlyifdoesntexist uninsneveruninstall
Source: "{#MyDistFolder}\DiaryInfo\bin\x86\Release\{#MyAppExeName}"; DestDir: "{app}"; Check: Is64BitInstallMode; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#MyDistFolder}\DiaryInfo\bin\x64\Release\{#MyAppExeName}"; DestDir: "{app}"; Check: not Is64BitInstallMode; Flags: ignoreversion recursesubdirs createallsubdirs 
Source: "dotNetFx40_Full_setup.exe"; DestDir: {tmp}; Flags: deleteafterinstall; Check: not IsRequiredDotNetDetected 

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon\user
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon\common
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"

[CustomMessages]
;Some messages from language file
en.CreateAutorunM=Autorun while windows starting
ru.CreateAutorunM=Запускать при старте системы
en.PortableModeM=Portable Mode
ru.PortableModeM=Портативная установка
en.ForAllUsersM=For all users
ru.ForAllUsersM=Для всех пользователей
en.CurrentUsersOnlyM=For the current user only
ru.CurrentUsersOnlyM=Только для текущего пользователя
en.RequiresM=requires
ru.RequiresM=нуждается в
en.InstAttemptM=The installer will attempt to install it
ru.InstAttemptM=Программа установки попытается установить
en.FrameworkInstalled=Microsoft Framework 4.5 is beïng installed. Please wait...
ru.FrameworkInstalled=Microsoft Framework 4.5 Устанавливается. Пожалуйста, подождите...
en.PathAppendM=Append PATH
ru.PathAppendM=Добавить папку в PATH
en.WriteInstalledPathM=Write path to registry
ru.WriteInstalledPathM=Записать путь установки в ресстр системы
en.RunAfterM=Run application after installation
ru.RunAfterM=Запустить приложение после установки

;Delete before install
[InstallDelete]
Type: filesandordirs; Name: "{app}"

;Uninstaller
[UninstallDelete]
Type: files; Name: "{%username}\DiaryInfoCookies.data"
Type: filesandordirs; Name: "{%localappdata}\{#MyAppName}"

[CODE]
//function GetDefaultDir(def: string): string;
//var
//    sTemp : string;
//begin
//    if Is64BitInstallMode then
//    begin
//        sTemp := ExpandConstant('{pf64}') + '{#MyAppName}';
//    else
//        sTemp := ExpandConstant('{pf32}') + '{#MyAppName}';
//    end;
//    Result := sTemp;
//end;

// http://www.codeproject.com/Tips/506096/InnoSetup-with-NET-installer-x-x-sample
function IsDotNetDetected(version: string; service: cardinal): boolean;
// Indicates whether the specified version and service pack of the .NET Framework is installed.
//
// version -- Specify one of these strings for the required .NET Framework version:
//    'v1.1.4322'     .NET Framework 1.1
//    'v2.0.50727'    .NET Framework 2.0
//    'v3.0'          .NET Framework 3.0
//    'v3.5'          .NET Framework 3.5
//    'v4\Client'     .NET Framework 4.0 Client Profile
//    'v4\Full'       .NET Framework 4.0 Full Installation
//    'v4.5'          .NET Framework 4.5
//
// service -- Specify any non-negative integer for the required service pack level:
//    0               No service packs required
//    1, 2, etc.      Service pack 1, 2, etc. required
var
    key: string;
    install, release, serviceCount: cardinal;
    check45, success: boolean;
var reqNetVer : string;
begin
    // .NET 4.5 installs as update to .NET 4.0 Full
    if version = 'v4.5' then begin
        version := 'v4\Full';
        check45 := true;
    end else
        check45 := false;

    // installation key group for all .NET versions
    key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\' + version;

    // .NET 3.0 uses value InstallSuccess in subkey Setup
    if Pos('v3.0', version) = 1 then begin
        success := RegQueryDWordValue(HKLM, key + '\Setup', 'InstallSuccess', install);
    end else begin
        success := RegQueryDWordValue(HKLM, key, 'Install', install);
    end;

    // .NET 4.0/4.5 uses value Servicing instead of SP
    if Pos('v4', version) = 1 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Servicing', serviceCount);
    end else begin
        success := success and RegQueryDWordValue(HKLM, key, 'SP', serviceCount);
    end;

    // .NET 4.5 uses additional value Release
    if check45 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Release', release);
        success := success and (release >= 378389);
    end;
    
    result := success and (install = 1) and (serviceCount >= service);
end;

function IsRequiredDotNetDetected(): Boolean;  
begin
    result := IsDotNetDetected('v4\Full', 0);
end;

function InitializeSetup(): Boolean;
begin
    if not IsDotNetDetected('v4\Full', 0) then begin
        MsgBox('{#MyAppName} {cm:RequiresM} Microsoft .NET Framework 4.5.'#13#13
          '{cm:InstAttemptM}', mbInformation, MB_OK);        
    end;
    
    result := true;
end; 

[Run]
Filename: {tmp}\dotNetFx40_Full_setup.exe; Parameters: "/q:a /c:""install /l /q"""; Check: not IsRequiredDotNetDetected; StatusMsg: {cm:FrameworkInstalled}
FileName: "{#MyAppExeName}"; WorkingDir: "{app}"; Description: "{cm:RunAfterM}"; Flags: postinstall nowait skipifsilent
