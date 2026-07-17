param(
    [Parameter(Mandatory = $true)]
    [string]$SourcePath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.Xml.Linq

function Get-ColumnIndex([string]$CellReference) {
    $index = 0

    foreach ($character in $CellReference.ToCharArray()) {
        $upper = [char]::ToUpperInvariant($character)
        if ($upper -lt 'A' -or $upper -gt 'Z') {
            break
        }

        $index = ($index * 26) + ([int]$upper - [int][char]'A' + 1)
    }

    return $index - 1
}

function Escape-CsvValue([string]$Value) {
    if ($null -eq $Value) {
        return ''
    }

    if ($Value.IndexOfAny([char[]]@(',', '"', "`r", "`n")) -ge 0) {
        return '"' + $Value.Replace('"', '""') + '"'
    }

    return $Value
}

$resolvedSource = [System.IO.Path]::GetFullPath($SourcePath)
$resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = [System.IO.Path]::GetDirectoryName($resolvedOutput)

if (-not [System.IO.File]::Exists($resolvedSource)) {
    throw "Source workbook not found: $resolvedSource"
}

if (-not [System.IO.Directory]::Exists($outputDirectory)) {
    [System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
}

$fileStream = [System.IO.File]::Open($resolvedSource, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)

try {
    $archive = [System.IO.Compression.ZipArchive]::new($fileStream, [System.IO.Compression.ZipArchiveMode]::Read)

    try {
        $mainNamespace = [System.Xml.Linq.XNamespace]::Get('http://schemas.openxmlformats.org/spreadsheetml/2006/main')
        $documentRelNamespace = [System.Xml.Linq.XNamespace]::Get('http://schemas.openxmlformats.org/officeDocument/2006/relationships')
        $packageRelNamespace = [System.Xml.Linq.XNamespace]::Get('http://schemas.openxmlformats.org/package/2006/relationships')

        $sharedStrings = [System.Collections.Generic.List[string]]::new()
        $sharedEntry = $archive.GetEntry('xl/sharedStrings.xml')
        if ($null -ne $sharedEntry) {
            $sharedStream = $sharedEntry.Open()
            try {
                $sharedDocument = [System.Xml.Linq.XDocument]::Load($sharedStream)
                foreach ($item in $sharedDocument.Descendants($mainNamespace + 'si')) {
                    $parts = foreach ($textNode in $item.Descendants($mainNamespace + 't')) { $textNode.Value }
                    $sharedStrings.Add([string]::Concat($parts))
                }
            }
            finally {
                $sharedStream.Dispose()
            }
        }

        $workbookEntry = $archive.GetEntry('xl/workbook.xml')
        $relationshipsEntry = $archive.GetEntry('xl/_rels/workbook.xml.rels')
        if ($null -eq $workbookEntry -or $null -eq $relationshipsEntry) {
            throw 'Invalid xlsx: workbook relationship files are missing.'
        }

        $workbookStream = $workbookEntry.Open()
        $relationshipsStream = $relationshipsEntry.Open()
        try {
            $workbookDocument = [System.Xml.Linq.XDocument]::Load($workbookStream)
            $relationshipsDocument = [System.Xml.Linq.XDocument]::Load($relationshipsStream)
        }
        finally {
            $workbookStream.Dispose()
            $relationshipsStream.Dispose()
        }

        $relationshipTargets = @{}
        foreach ($relationship in $relationshipsDocument.Descendants($packageRelNamespace + 'Relationship')) {
            $id = $relationship.Attribute('Id')
            $target = $relationship.Attribute('Target')
            if ($null -ne $id -and $null -ne $target) {
                $relationshipTargets[$id.Value] = $target.Value
            }
        }

        $writer = [System.IO.StreamWriter]::new($resolvedOutput, $false, [System.Text.UTF8Encoding]::new($false))
        try {
            foreach ($sheet in $workbookDocument.Descendants($mainNamespace + 'sheet')) {
                $nameAttribute = $sheet.Attribute('name')
                $relationshipAttribute = $sheet.Attribute($documentRelNamespace + 'id')
                if ($null -eq $nameAttribute -or $null -eq $relationshipAttribute) {
                    continue
                }

                $target = $relationshipTargets[$relationshipAttribute.Value]
                if ([string]::IsNullOrWhiteSpace($target)) {
                    continue
                }

                $normalizedTarget = $target.Replace('\', '/').TrimStart('/')
                if (-not $normalizedTarget.StartsWith('xl/', [System.StringComparison]::OrdinalIgnoreCase)) {
                    $normalizedTarget = 'xl/' + $normalizedTarget
                }

                $sheetEntry = $archive.GetEntry($normalizedTarget)
                if ($null -eq $sheetEntry) {
                    continue
                }

                $writer.WriteLine('# ' + $nameAttribute.Value)

                $sheetStream = $sheetEntry.Open()
                try {
                    $sheetDocument = [System.Xml.Linq.XDocument]::Load($sheetStream)
                    foreach ($row in $sheetDocument.Descendants($mainNamespace + 'row')) {
                        $values = [System.Collections.Generic.List[string]]::new()

                        foreach ($cell in $row.Elements($mainNamespace + 'c')) {
                            $referenceAttribute = $cell.Attribute('r')
                            $columnIndex = if ($null -ne $referenceAttribute) { Get-ColumnIndex $referenceAttribute.Value } else { $values.Count }

                            while ($values.Count -lt $columnIndex) {
                                $values.Add('')
                            }

                            $typeAttribute = $cell.Attribute('t')
                            $type = if ($null -ne $typeAttribute) { $typeAttribute.Value } else { '' }
                            $valueNode = $cell.Element($mainNamespace + 'v')
                            $inlineNode = $cell.Element($mainNamespace + 'is')
                            $rawValue = if ($null -ne $valueNode) {
                                $valueNode.Value
                            }
                            elseif ($null -ne $inlineNode) {
                                $parts = foreach ($textNode in $inlineNode.Descendants($mainNamespace + 't')) { $textNode.Value }
                                [string]::Concat($parts)
                            }
                            else {
                                ''
                            }

                            $sharedIndex = 0
                            $value = if ($type -eq 's' -and [int]::TryParse($rawValue, [ref]$sharedIndex) -and $sharedIndex -ge 0 -and $sharedIndex -lt $sharedStrings.Count) {
                                $sharedStrings[$sharedIndex]
                            }
                            else {
                                $rawValue
                            }

                            if ($values.Count -eq $columnIndex) {
                                $values.Add($value)
                            }
                            else {
                                $values[$columnIndex] = $value
                            }
                        }

                        if ($values.Count -eq 0) {
                            $writer.WriteLine()
                        }
                        else {
                            [string[]]$escapedValues = foreach ($value in $values) { Escape-CsvValue $value }
                            $writer.WriteLine([string]::Join(',', $escapedValues))
                        }
                    }
                }
                finally {
                    $sheetStream.Dispose()
                }

                $writer.WriteLine()
            }
        }
        finally {
            $writer.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }
}
finally {
    $fileStream.Dispose()
}

Write-Output "Converted: $resolvedSource -> $resolvedOutput"
