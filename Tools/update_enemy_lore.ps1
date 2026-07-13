$base = "c:\Users\arthu\Chez Arthur\Assets\_Project\ScriptableObjects\Enemies"
$jsonPath = "c:\Users\arthu\Chez Arthur\Tools\update_enemy_lore.json"
$data = Get-Content -Raw -Encoding UTF8 $jsonPath | ConvertFrom-Json

function Escape-Yaml([string]$value) {
    $escaped = $value.Replace('\', '\\').Replace('"', '\"')
    return '"' + $escaped + '"'
}

$count = 0
foreach ($prop in $data.PSObject.Properties) {
    $rel = $prop.Name.Replace('/', '\')
    $path = Join-Path $base $rel
    $id = $prop.Value[0]
    $name = $prop.Value[1]
    $lore = $prop.Value[2]
    $text = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)

    if ($text -match '(?m)^  id:') {
        $text = [regex]::Replace($text, '(?m)^  id:.*$', "  id: $id")
    } else {
        $text = $text.Replace("  m_EditorClassIdentifier: `r`n", "  m_EditorClassIdentifier: `r`n  id: $id`r`n")
        $text = $text.Replace("  m_EditorClassIdentifier: `n", "  m_EditorClassIdentifier: `n  id: $id`n")
    }

    $quotedName = Escape-Yaml $name
    $quotedLore = Escape-Yaml $lore
    $text = [regex]::Replace($text, '(?m)^  enemyName:.*$', "  enemyName: $quotedName")
    $text = [regex]::Replace($text, '(?m)^  passiveDescription:.*$', "  passiveDescription: $quotedLore", 1)

    $utf8 = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($path, $text, $utf8)
    $count++
}

Write-Host "Updated $count enemy assets"
