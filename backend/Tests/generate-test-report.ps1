param(
    [string]$ResultsDirectory = "$PSScriptRoot\TestResults",
  [string]$OutputFile = "$PSScriptRoot\TestResults\test-report.html",
  [switch]$OpenReport
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Escape-Html {
    param([AllowNull()][string]$Text)

    if ($null -eq $Text) {
        return ""
    }

    return [System.Net.WebUtility]::HtmlEncode($Text)
}

if (-not (Test-Path -Path $ResultsDirectory)) {
    Write-Warning "No existe el directorio de resultados: $ResultsDirectory"
    exit 0
}

$latestTrx = Get-ChildItem -Path $ResultsDirectory -Filter *.trx -File |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $latestTrx) {
    Write-Warning "No se encontro ningun archivo .trx en: $ResultsDirectory"
    exit 0
}

[xml]$trxXml = Get-Content -Path $latestTrx.FullName -Raw

$nsManager = New-Object System.Xml.XmlNamespaceManager($trxXml.NameTable)
$nsManager.AddNamespace("t", "http://microsoft.com/schemas/VisualStudio/TeamTest/2010")

$testRunNode = $trxXml.SelectSingleNode("/t:TestRun", $nsManager)
$timesNode = $trxXml.SelectSingleNode("/t:TestRun/t:Times", $nsManager)
$resultNodes = $trxXml.SelectNodes("//t:UnitTestResult", $nsManager)

$total = 0
$passed = 0
$failed = 0
$skipped = 0
$other = 0

$failedRows = New-Object System.Collections.Generic.List[string]
$sectionRows = @{}

function Get-TestSection {
  param([string]$TestName)

  if ($TestName -match '^backend\.Tests\.([^.]+)\.') {
    return $Matches[1]
  }

  return 'General'
}

function Get-TestDisplayName {
  param([string]$TestName)

  if ($TestName -match '^backend\.Tests\.[^.]+\.(.+)$') {
    return $Matches[1]
  }

  return $TestName
}

foreach ($result in $resultNodes) {
    $total++
  $outcome = [string]$result.outcome
  $testName = [string]$result.testName
  $testDuration = [string]$result.duration

  $sectionName = Get-TestSection -TestName $testName
  if (-not $sectionRows.ContainsKey($sectionName)) {
    $sectionRows[$sectionName] = New-Object System.Collections.Generic.List[string]
  }

  $displayName = Escape-Html(Get-TestDisplayName -TestName $testName)
  $durationDisplay = Escape-Html($testDuration)
  $outcomeDisplay = Escape-Html($outcome)
  $outcomeClass = switch ($outcome) {
    "Passed" { "badge-pass" }
    "Failed" { "badge-fail" }
    "NotExecuted" { "badge-skip" }
    default { "badge-other" }
  }

  $sectionRows[$sectionName].Add("<tr><td>$displayName</td><td><span class='badge $outcomeClass'>$outcomeDisplay</span></td><td>$durationDisplay</td></tr>")

    switch ($outcome) {
        "Passed" { $passed++ }
        "Failed" { $failed++ }
        "NotExecuted" { $skipped++ }
        default { $other++ }
    }

    if ($outcome -eq "Failed") {
      $name = Escape-Html($testName)
      $duration = Escape-Html($testDuration)

        $errorMessageNode = $result.SelectSingleNode("t:Output/t:ErrorInfo/t:Message", $nsManager)
        $errorMessage = if ($null -ne $errorMessageNode) {
            Escape-Html($errorMessageNode.InnerText)
        }
        else {
            "Sin detalle de error"
        }

        $failedRows.Add("<tr><td>$name</td><td>$duration</td><td><pre>$errorMessage</pre></td></tr>")
    }
}

$sectionTables = New-Object System.Collections.Generic.List[string]
foreach ($sectionName in ($sectionRows.Keys | Sort-Object)) {
    $sectionRowsHtml = $sectionRows[$sectionName] -join "`n"
    $safeSectionName = Escape-Html([string]$sectionName)
    $sectionCount = $sectionRows[$sectionName].Count

    $sectionTables.Add(@"
<h2>Seccion: $safeSectionName ($sectionCount)</h2>
<table>
  <thead>
    <tr>
      <th>Prueba</th>
      <th>Estado</th>
      <th>Duracion</th>
    </tr>
  </thead>
  <tbody>
    $sectionRowsHtml
  </tbody>
</table>
"@)
}

$sectionsHtml = if ($sectionTables.Count -gt 0) {
    $sectionTables -join "`n"
}
else {
    '<p>No se encontraron pruebas para mostrar por seccion.</p>'
}

$runName = Escape-Html([string]$testRunNode.name)
$runUser = Escape-Html([string]$testRunNode.runUser)
$createdAt = Escape-Html([string]$timesNode.creation)

$durationText = "N/A"
if ($null -ne $timesNode -and $timesNode.start -and $timesNode.finish) {
    $startTime = [DateTimeOffset]::Parse([string]$timesNode.start)
    $finishTime = [DateTimeOffset]::Parse([string]$timesNode.finish)
    $duration = $finishTime - $startTime
    $durationText = "{0:mm\:ss\.fff}" -f $duration
}

$statusClass = if ($failed -gt 0) { "fail" } else { "pass" }
$statusText = if ($failed -gt 0) { "Fallido" } else { "Exitoso" }

$failedSection = if ($failed -gt 0) {
@"
<h2>Pruebas Fallidas ($failed)</h2>
<table>
  <thead>
    <tr>
      <th>Prueba</th>
      <th>Duracion</th>
      <th>Error</th>
    </tr>
  </thead>
  <tbody>
    $($failedRows -join "`n")
  </tbody>
</table>
"@
}
else {
  '<p class="all-good">No hay pruebas fallidas.</p>'
}

$html = @"
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>Reporte de Pruebas</title>
  <style>
    :root {
      --bg: #f6f8fb;
      --card: #ffffff;
      --text: #1a1f2b;
      --muted: #5a6475;
      --ok: #177245;
      --bad: #b42318;
      --border: #d8dee9;
    }
    body {
      margin: 0;
      font-family: "Segoe UI", Tahoma, sans-serif;
      background: linear-gradient(120deg, #edf2ff 0%, #f6f8fb 60%, #eefaf5 100%);
      color: var(--text);
    }
    .wrap {
      max-width: 1000px;
      margin: 32px auto;
      padding: 0 16px;
    }
    .card {
      background: var(--card);
      border: 1px solid var(--border);
      border-radius: 12px;
      padding: 20px;
      box-shadow: 0 8px 22px rgba(23, 32, 61, 0.08);
    }
    h1 {
      margin: 0 0 8px;
      font-size: 1.6rem;
    }
    .meta {
      color: var(--muted);
      margin-bottom: 16px;
      font-size: 0.95rem;
    }
    .status {
      display: inline-block;
      padding: 6px 12px;
      border-radius: 999px;
      font-weight: 600;
      margin-bottom: 18px;
    }
    .status.pass { background: #e8f7ef; color: var(--ok); }
    .status.fail { background: #ffe8e8; color: var(--bad); }
    .grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
      gap: 10px;
      margin-bottom: 18px;
    }
    .kpi {
      border: 1px solid var(--border);
      border-radius: 10px;
      padding: 12px;
      background: #fff;
    }
    .kpi strong {
      display: block;
      font-size: 1.2rem;
      margin-top: 4px;
    }
    table {
      width: 100%;
      border-collapse: collapse;
      margin-top: 10px;
      font-size: 0.92rem;
    }
    th, td {
      border: 1px solid var(--border);
      padding: 10px;
      vertical-align: top;
      text-align: left;
    }
    th {
      background: #f4f6fa;
    }
    pre {
      margin: 0;
      white-space: pre-wrap;
      word-break: break-word;
      font-family: Consolas, monospace;
    }
    .all-good {
      color: var(--ok);
      font-weight: 600;
    }
    h2 {
      margin: 26px 0 8px;
      font-size: 1.1rem;
    }
    .badge {
      display: inline-block;
      padding: 2px 8px;
      border-radius: 999px;
      font-weight: 600;
      font-size: 0.82rem;
    }
    .badge-pass { background: #e8f7ef; color: var(--ok); }
    .badge-fail { background: #ffe8e8; color: var(--bad); }
    .badge-skip { background: #fff4de; color: #8a6116; }
    .badge-other { background: #eef2f7; color: #334155; }
  </style>
</head>
<body>
  <div class="wrap">
    <div class="card">
      <h1>Reporte de pruebas .NET</h1>
      <div class="meta">Archivo fuente: $(Escape-Html($latestTrx.Name))</div>
      <div class="meta">Ejecucion: $runName | Usuario: $runUser | Creado: $createdAt</div>
      <span class="status $statusClass">Estado: $statusText</span>

      <div class="grid">
        <div class="kpi">Total<strong>$total</strong></div>
        <div class="kpi">Pasaron<strong>$passed</strong></div>
        <div class="kpi">Fallaron<strong>$failed</strong></div>
        <div class="kpi">Omitidas<strong>$skipped</strong></div>
        <div class="kpi">Otros<strong>$other</strong></div>
        <div class="kpi">Duracion<strong>$durationText</strong></div>
      </div>

      <h2>Pruebas Por Seccion</h2>
      $sectionsHtml

      $failedSection
    </div>
  </div>
</body>
</html>
"@

$outputDirectory = Split-Path -Path $OutputFile -Parent
if (-not (Test-Path -Path $outputDirectory)) {
    New-Item -Path $outputDirectory -ItemType Directory -Force | Out-Null
}

Set-Content -Path $OutputFile -Value $html -Encoding UTF8
Write-Host "Reporte HTML generado en: $OutputFile"

if ($OpenReport) {
  try {
    Start-Process -FilePath $OutputFile -ErrorAction Stop | Out-Null
  }
  catch {
    cmd /c start "" "$OutputFile" | Out-Null
  }
}
