# Validates Strings.ar/tr/en.xaml: well-formed XML, no duplicate keys, key parity.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$files = @(
    @{ Lang = 'ar'; Path = Join-Path $root 'Resources\Strings.ar.xaml' },
    @{ Lang = 'tr'; Path = Join-Path $root 'Resources\Strings.tr.xaml' },
    @{ Lang = 'en'; Path = Join-Path $root 'Resources\Strings.en.xaml' }
)

$keySets = @{}
foreach ($f in $files) {
    [xml]$xml = Get-Content $f.Path -Raw
    $keys = @()
    foreach ($node in $xml.ResourceDictionary.ChildNodes) {
        if ($node.LocalName -eq 'String' -and $node.Key) {
            $keys += $node.Key
        }
    }
    $dupes = $keys | Group-Object | Where-Object { $_.Count -gt 1 } | Select-Object -ExpandProperty Name
    if ($dupes) { throw "$($f.Lang): duplicate keys: $($dupes -join ', ')" }
    $keySets[$f.Lang] = [System.Collections.Generic.HashSet[string]]::new([string[]]$keys)
    Write-Host "OK $($f.Lang): $($keys.Count) keys, valid XML"
}

$ar = $keySets['ar']
$tr = $keySets['tr']
$en = $keySets['en']

$onlyEn = $en | Where-Object { -not $ar.Contains($_) } | Sort-Object
$onlyAr = $ar | Where-Object { -not $en.Contains($_) } | Sort-Object
if ($onlyEn.Count -gt 0) {
    Write-Host "WARN: keys only in en ($($onlyEn.Count)): $($onlyEn -join ', ')"
}
if ($onlyAr.Count -gt 0) {
    Write-Host "WARN: keys only in ar ($($onlyAr.Count)): $($onlyAr -join ', ')"
}
if (-not ($tr.SetEquals($ar))) {
    $trOnly = $tr | Where-Object { -not $ar.Contains($_) }
    $arOnly = $ar | Where-Object { -not $tr.Contains($_) }
    if ($trOnly) { Write-Host "WARN: keys only in tr: $($trOnly -join ', ')" }
    if ($arOnly) { Write-Host "WARN: keys only in ar vs tr: $($arOnly -join ', ')" }
}

Write-Host "Resource validation passed."
