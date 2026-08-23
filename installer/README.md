# Windows installer

`ForamEcoQS.iss` packages the Windows x64 self-contained publication with Inno Setup.

The installer runs without administrator privileges, installs by default to `%LOCALAPPDATA%\Programs\ForamEcoQS`, adds a Start Menu shortcut, selects desktop-icon creation by default, and registers a standard uninstaller.

Build with:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\installer\build_installer.ps1
```

The resulting executable is written to `artifacts\installer` unless `-OutputDirectory` is supplied.
