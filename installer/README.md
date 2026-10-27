# Cómo generar el instalador de NOX

Requisito (una vez): Inno Setup 6, instalado para el usuario con
`winget install --id JRSoftware.InnoSetup -e --scope user` (no pide administrador).

Desde `C:\Dev\NOX`, en una terminal:

```
# 1) Publicar NOX con .NET incluido (funciona en PCs sin .NET instalado)
dotnet publish src/Nox.App -c Release -r win-x64 --self-contained true -o installer/out/app -p:SatelliteResourceLanguages=es -p:DebugType=none

# 2) Crear el instalador
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" installer\nox.iss
```

Resultado: `dist\NOX-<versión>-instalador.exe` (un solo archivo para enviar).

Para una versión nueva: cambiar `<Version>` en `src/Nox.App/Nox.App.csproj` y `AppVersion` en `installer/nox.iss`
(los dos iguales). **No cambiar `AppId`**: así el instalador nuevo actualiza el que ya está instalado.

Notas:
- El instalador no tiene firma digital: Windows SmartScreen avisará ("Windows protegió tu PC").
  Hay que pulsar **Más información → Ejecutar de todas formas**. Es normal en programas personales.
- `installer/out/` y `dist/` no se guardan en git (se regeneran).
