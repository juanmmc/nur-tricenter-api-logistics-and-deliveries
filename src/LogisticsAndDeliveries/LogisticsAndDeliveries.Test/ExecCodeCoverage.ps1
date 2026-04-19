# Ejecutar desde el directorio del script o llama el script desde la ra�z del proyecto de tests
# Uso: pwsh .\ExecCodeCoverage.ps1

# Obtener directorio del script (proyecto de tests)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $scriptDir

# Comprueba si existe ejecutable reportgenerator global
function Has-GlobalReportGenerator {
    try {
        & reportgenerator -version > $null 2>&1
        return $true
    } catch {
        return $false
    }
}

# Comprueba si hay un manifest de herramientas local y si reportgenerator est� instalado all�
function Has-LocalReportGenerator {
    try {
        # Ejecutar desde el directorio del script para detectar manifest local
        $output = & dotnet tool list --local 2>$null
        if ($output -and $output -match 'dotnet-reportgenerator-globaltool') {
            return $true
        }
    } catch { }
    return $false
}

# Intentar instalar reportgenerator en el tool manifest del repo
function Ensure-LocalReportGenerator {
    try {
        if (-not (Test-Path (Join-Path $scriptDir '.config\dotnet-tools.json'))) {
            Write-Host "Creando tool manifest local..."
            & dotnet new tool-manifest > $null 2>&1
        }
        Write-Host "Instalando dotnet-reportgenerator-globaltool en el manifest local..."
        & dotnet tool install dotnet-reportgenerator-globaltool > $null 2>&1
        return $LASTEXITCODE -eq 0
    } catch {
        return $false
    }
}

# Intentar instalar globalmente como fallback
function Ensure-GlobalReportGenerator {
    try {
        Write-Host "Instalando dotnet-reportgenerator-globaltool globalmente..."
        & dotnet tool install -g dotnet-reportgenerator-globaltool > $null 2>&1
        return $LASTEXITCODE -eq 0
    } catch {
        return $false
    }
}

# Determinar comando a usar para invocar reportgenerator
$useGlobal = Has-GlobalReportGenerator
$useLocal = Has-LocalReportGenerator

if (-not $useGlobal -and -not $useLocal) {
    # intentar instalar local; si falla, intentar global
    if (Ensure-LocalReportGenerator) {
        $useLocal = $true
    } elseif (Ensure-GlobalReportGenerator) {
        $useGlobal = $true
    } else {
        Write-Error "No se pudo instalar reportgenerator ni local ni globalmente. A�ade el tool manualmente o instala globalmente."
        exit 1
    }
}

# Patrones de exclusión de cobertura (Coverlet): [assembly]namespace.Clase
# Usar %2C como separador (coma URL-encoded) para evitar que MSBuild lo interprete como separador de lista
$coverletExclude = '[*]*.Migrations.*%2C[*]*.DependencyInjection%2C[*]*Program%2C[LogisticsAndDeliveries.Core]*'

# Argumentos de dotnet test como array (evita problemas de escape con Invoke-Expression)
$testArgs = @(
    'test', '--no-build',
    '-p:CollectCoverage=true',
    '-p:CoverletOutputFormat=opencover',
    '-p:CoverletOutput=TestResults/coverage.opencover.xml',
    "-p:Exclude=$coverletExclude"
)

# Archivar TestResults previo en vez de eliminar, para mantener rastros si hace falta
$testResultsDir = Join-Path $scriptDir 'TestResults'
if (Test-Path $testResultsDir) {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $archiveDir = Join-Path $scriptDir ("TestResults_Archive_$timestamp")
    Write-Host "Archivando TestResults previo en $archiveDir"
    Move-Item -Path $testResultsDir -Destination $archiveDir
}

# Ejecutar tests y recogida de cobertura
Write-Host "Ejecutando tests y recogiendo cobertura..."
& dotnet @testArgs
if ($LASTEXITCODE -ne 0) {
    Write-Warning "dotnet test devolvio codigo $LASTEXITCODE. Revisar errores de test antes de generar informe."
}

# Buscar el archivo de cobertura: primero opencover, si no existe intentar .coverage (Visual Studio)
$coverageFile = Get-ChildItem -Path $scriptDir -Recurse -Filter "coverage.opencover.xml" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1

$usingBinaryCoverage = $false
if (-not $coverageFile) {
    $coverageBin = Get-ChildItem -Path $scriptDir -Recurse -Filter "*.coverage" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($coverageBin) {
        Write-Host "Se encontr� archivo binario de cobertura: $($coverageBin.FullName)"
        $coverageFile = $coverageBin
        $usingBinaryCoverage = $true
    }
}

if (-not $coverageFile) {
    Write-Error "No se encontr� coverage.opencover.xml ni archivo .coverage en TestResults. Aseg�rate de que la recolecci�n de cobertura se ejecut� correctamente."
    exit 1
}

# Preparar carpetas de salida e historial
$reportsDir = Join-Path $scriptDir 'CoverageReport'
$historyDir = Join-Path $scriptDir 'CoverageHistory'
$runStamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$targetDir = Join-Path $reportsDir $runStamp

New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
New-Item -ItemType Directory -Force -Path $historyDir | Out-Null

# Copiar el archivo de cobertura al historial con marca temporal (preserva evoluci�n)
$ext = if ($usingBinaryCoverage) { ".coverage" } else { ".opencover.xml" }
$baseName = $coverageFile.BaseName
$historyCopyName = "{0}_{1}{2}" -f $baseName, $runStamp, $ext
Copy-Item $coverageFile.FullName -Destination (Join-Path $historyDir $historyCopyName) -Force

# Invocar ReportGenerator pasando par�metros como argumentos (evita que PowerShell interprete ;)
$reportsArg = $coverageFile.FullName
$historyArg = $historyDir
$targetArg = $targetDir
$reportTypesValue = "Html;HtmlSummary"

if ($useGlobal) {
    Write-Host "Generando informe HTML (ejecutable global)..."
    & reportgenerator "-reports:$reportsArg" "-targetdir:$targetArg" "-reporttypes:$reportTypesValue" "-historydir:$historyArg"
    $exitCode = $LASTEXITCODE
} else {
    Write-Host "Generando informe HTML (tool local via dotnet tool run)..."
    # Pasar los argumentos despu�s del --. Cada argumento como string evita que PowerShell rompa por ';'
    & dotnet 'tool' 'run' 'reportgenerator' '--' "-reports:$reportsArg" "-targetdir:$targetArg" "-reporttypes:$reportTypesValue" "-historydir:$historyArg"
    $exitCode = $LASTEXITCODE
}

if ($exitCode -ne 0) {
    Write-Error "ReportGenerator devolvi� c�digo $exitCode."
    exit 1
}

# Determinar el fichero �ndice (ReportGenerator suele crear index.html)
$indexHtml = Join-Path $targetDir 'index.html'
$indexHtm  = Join-Path $targetDir 'index.htm'
$indexFile = $null
if (Test-Path $indexHtml) { $indexFile = $indexHtml }
elseif (Test-Path $indexHtm) { $indexFile = $indexHtm }

# Abrir el informe principal en el navegador (opcional, se puede evitar en CI detectando $env:CI)
if ($indexFile -and (-not $env:CI)) {
    Write-Host "Abriendo informe: $indexFile"
    Start-Process -FilePath $indexFile
} elseif (-not $indexFile) {
    Write-Warning "No se encontr� index.html ni index.htm en $targetDir."
}

Write-Host "Proceso finalizado. Informes en: $targetDir y historial en: $historyDir"