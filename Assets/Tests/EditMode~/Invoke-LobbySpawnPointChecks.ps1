$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
Set-Location $repo
$unity = 'C:/Program Files/Unity/Hub/Editor/6000.0.68f1/Editor/Data'
$output = Join-Path $repo 'Temp/CodexLobbySpawnPoints'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$runtime = @(Get-Content 'Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.rsp' | Where-Object { $_ -notmatch '^-(out|refout):' })
foreach ($path in @('Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyCharacterSpawnPoint.cs')) {
    if ((Test-Path $path) -and -not ($runtime | Where-Object { $_.Trim('"') -eq $path })) { $runtime += '"' + $path + '"' }
}
$runtime += '-out:"' + (Join-Path $output 'Assembly-CSharp.dll') + '"'
[IO.File]::WriteAllLines((Join-Path $output 'runtime.rsp'), $runtime)
& "$unity/NetCoreRuntime/dotnet.exe" "$unity/DotNetSdkRoslyn/csc.dll" ('@' + (Join-Path $output 'runtime.rsp'))
if ($LASTEXITCODE -ne 0) { throw 'Runtime compilation failed' }
$nunit = (Get-ChildItem 'Library/PackageCache/com.unity.ext.nunit*/net40/unity-custom/nunit.framework.dll' | Select-Object -First 1).FullName
$tests = @('-nologo','-nostdlib','-target:library','-langversion:9')
$tests += @($runtime | Where-Object { $_ -match '^-r:' })
$tests += '-r:"' + (Join-Path $output 'Assembly-CSharp.dll') + '"'
$tests += '-r:"' + $nunit + '"'
$tests += '-out:"' + (Join-Path $output 'LobbySpawnPoints.Tests.dll') + '"'
$tests += '"Assets/Tests/EditMode~/LobbyWorldCharacterSpawnerTests.cs"'
[IO.File]::WriteAllLines((Join-Path $output 'tests.rsp'), $tests)
& "$unity/NetCoreRuntime/dotnet.exe" "$unity/DotNetSdkRoslyn/csc.dll" ('@' + (Join-Path $output 'tests.rsp'))
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
$editor = @(Get-Content 'Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp-Editor.rsp' | Where-Object { $_ -notmatch '^-(out|refout):' -and $_ -notmatch '^-r:.*[/\\]Assembly-CSharp.dll"?$' })
$editor += '-r:"' + (Join-Path $output 'Assembly-CSharp.dll') + '"'
$path = 'Assets/Project/Editor/LobbyCharacterSpawnPointEditor.cs'
if ((Test-Path $path) -and -not ($editor | Where-Object { $_.Trim('"') -eq $path })) { $editor += '"' + $path + '"' }
$editor += '-out:"' + (Join-Path $output 'Assembly-CSharp-Editor.dll') + '"'
[IO.File]::WriteAllLines((Join-Path $output 'editor.rsp'), $editor)
& "$unity/NetCoreRuntime/dotnet.exe" "$unity/DotNetSdkRoslyn/csc.dll" ('@' + (Join-Path $output 'editor.rsp'))
if ($LASTEXITCODE -ne 0) { throw 'Editor compilation failed' }
Write-Output 'PASS: runtime, editor and NUnit test compilation. Unity-dependent tests were NOT executed.'
