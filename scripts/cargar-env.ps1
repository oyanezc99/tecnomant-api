# Uso (desde la carpeta tecnomant-api):  . .\scripts\cargar-env.ps1
param([string]$Archivo = "$PSScriptRoot\..\config\db.local.env")

if (-not (Test-Path $Archivo)) { Write-Host "No existe $Archivo" -ForegroundColor Red; return }

Get-Content $Archivo | ForEach-Object {
    $linea = $_.Trim()
    if ($linea -eq "" -or $linea.StartsWith("#")) { return }   # salta vacías y comentarios
    $nombre, $valor = $linea -split "=", 2                      # parte solo en el primer "="
    $valor = $valor.Trim().Trim("'")                            # quita las comillas simples
    Set-Item -Path "Env:$($nombre.Trim())" -Value $valor
    Write-Host "cargada: $($nombre.Trim())"
}