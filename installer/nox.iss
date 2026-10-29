; Instalador de NOX (Inno Setup 6). Cómo generarlo: ver installer/README.md.
; Se instala solo para el usuario actual: no pide permisos de administrador.

#define AppName "NOX"
#define AppVersion "0.2.0"
#define AppExe "NOX.exe"

[Setup]
; Identificador fijo de la app: no cambiarlo nunca (así las versiones nuevas se instalan encima).
AppId={{86ED2F3D-7382-4682-904E-FD7975B7EF17}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=JeiPo
; Para el usuario actual: %LOCALAPPDATA%\Programs\NOX
PrivilegesRequired=lowest
DefaultDirName={autopf}\{#AppName}
DisableProgramGroupPage=yes
DisableDirPage=yes
; NOX necesita Windows 10 versión 2004 (compilación 19041) o posterior, de 64 bits.
MinVersion=10.0.19041
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\dist
OutputBaseFilename=NOX-{#AppVersion}-instalador
SetupIconFile=..\src\Nox.App\Assets\nox.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName={#AppName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "autostart"; Description: "Iniciar NOX con Windows"; Flags: unchecked

[Files]
; Todo lo publicado (NOX + .NET incluido)
Source: "out\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; DestName: "LEEME.md"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExe}"

[Registry]
; "Iniciar con Windows" (el mismo valor que usa NOX desde su menú de la bandeja).
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#AppName}"; ValueData: """{app}\{#AppExe}"""; Flags: uninsdeletevalue; Tasks: autostart
; Si se activó después desde la bandeja, también se borra al desinstalar.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: none; ValueName: "{#AppName}"; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#AppExe}"; Description: "Abrir NOX ahora"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Cerrar NOX antes de borrar sus archivos.
Filename: "{sys}\taskkill.exe"; Parameters: "/IM {#AppExe} /F"; Flags: runhidden; RunOnceId: "CerrarNOX"

; Las notas (Documentos\NOX) y los ajustes (%APPDATA%\NOX) NO se borran al desinstalar.

[Code]
// Si NOX está abierto (por ejemplo, al instalar una versión nueva encima), se cierra antes de copiar.
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/IM {#AppExe} /F', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := '';
end;
