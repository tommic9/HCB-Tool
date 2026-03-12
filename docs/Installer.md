# MSI Installer

This repository includes a WiX-based MSI project for distributing the add-in to end users.

## What It Installs

The installer copies:

- `HCB.RevitAddin.addin`
- the full `HCB.RevitAddin` payload folder

to:

- `%AppData%\Autodesk\Revit\Addins\2025\` for `R25`
- `%AppData%\Autodesk\Revit\Addins\2026\` for `R26`

The package is per-user and does not require administrator rights.

## Build Commands

Build the MSI from the repository root:

- `dotnet build .\src\HCB.RevitInstaller\HCB.RevitInstaller.wixproj -c "Release R25"`
- `dotnet build .\src\HCB.RevitInstaller\HCB.RevitInstaller.wixproj -c "Release R26"`

The installer project first builds `HCB.RevitAddin` for the same configuration, then packages the published add-in output.

## Output

The MSI is generated under:

- `src\HCB.RevitInstaller\bin\x64\Release R25\`
- `src\HCB.RevitInstaller\bin\x64\Release R26\`

## Versioning

The MSI product version is currently controlled in:

- `src/HCB.RevitInstaller/HCB.RevitInstaller.wixproj`

Update `InstallerProductVersion` before shipping a new release.
