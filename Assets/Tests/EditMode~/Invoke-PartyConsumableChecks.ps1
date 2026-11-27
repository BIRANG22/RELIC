$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
Set-Location $repo
$unity = 'C:/Program Files/Unity/Hub/Editor/6000.0.68f1/Editor/Data'
$output = Join-Path $repo 'Temp/CodexPartyConsumables'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$response = @(Get-Content 'Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.rsp' | Where-Object { $_ -notmatch '^-(out|refout):' })
$response += '-out:"' + (Join-Path $output 'Assembly-CSharp.dll') + '"'
foreach ($source in @('LobbyConsumablePanelUI','LobbyConsumableSlotUI')) {
    $path = "Assets/Project/Scripts/$source.cs"
    if (-not ($response | Where-Object { $_.Trim('"') -eq $path })) { $response += '"' + $path + '"' }
}
$rsp = Join-Path $output 'runtime.rsp'
[IO.File]::WriteAllLines($rsp, $response)
& "$unity/NetCoreRuntime/dotnet.exe" "$unity/DotNetSdkRoslyn/csc.dll" ('@' + $rsp)
if ($LASTEXITCODE -ne 0) { throw "Runtime compilation failed: $LASTEXITCODE" }
Write-Output 'PASS: full Assembly-CSharp compilation (STEAMWORKS_NET included)'
$nunit = (Get-ChildItem 'Library/PackageCache/com.unity.ext.nunit*/net40/unity-custom/nunit.framework.dll').FullName
$testResponse = @('-nologo','-nostdlib','-target:library','-langversion:9')
$testResponse += @($response | Where-Object { $_ -match '^-r:' })
$testResponse += '-r:"' + (Join-Path $output 'Assembly-CSharp.dll') + '"'
$testResponse += '-r:"' + $nunit + '"'
$testResponse += '-out:"' + (Join-Path $output 'PartyConsumable.Tests.dll') + '"'
$types = @('BattleConsumableServiceTests','PartyConsumableRuntimeTests')
foreach ($test in $types) { $testResponse += '"' + (Join-Path $PSScriptRoot "$test.cs") + '"' }
$rsp = Join-Path $output 'tests.rsp'
[IO.File]::WriteAllLines($rsp, $testResponse)
& "$unity/NetCoreRuntime/dotnet.exe" "$unity/DotNetSdkRoslyn/csc.dll" ('@' + $rsp)
if ($LASTEXITCODE -ne 0) { throw "Test compilation failed: $LASTEXITCODE" }
[void][Reflection.Assembly]::LoadFrom("$unity/Managed/UnityEngine/UnityEngine.CoreModule.dll")
foreach ($name in @('Unity.TextMeshPro','UnityEngine.UI','Unity.Localization','Unity.ResourceManager')) {
    [void][Reflection.Assembly]::LoadFrom((Join-Path $repo "Library/ScriptAssemblies/$name.dll"))
}
[void][Reflection.Assembly]::LoadFrom($nunit)
[void][Reflection.Assembly]::LoadFrom((Join-Path $output 'Assembly-CSharp.dll'))
$tests = [Reflection.Assembly]::LoadFrom((Join-Path $output 'PartyConsumable.Tests.dll'))
$passed = 0
foreach ($typeName in $types) {
    $type = $tests.GetType($typeName)
    $instance = [Activator]::CreateInstance($type)
    foreach ($method in $type.GetMethods()) {
        $attrs = @($method.GetCustomAttributes($true))
        $cases = @($attrs | Where-Object { $_.GetType().Name -eq 'TestCaseAttribute' })
        $test = @($attrs | Where-Object { $_.GetType().Name -eq 'TestAttribute' })
        if ($cases.Count -eq 0 -and $test.Count -eq 0) { continue }
        if ($cases.Count -eq 0) { $cases = @([pscustomobject]@{ Arguments = [object[]]@() }) }
        foreach ($case in $cases) {
            try { $method.Invoke($instance, [object[]]$case.Arguments) | Out-Null }
            catch { throw "$typeName.$($method.Name): $($_.Exception.GetBaseException().Message)" }
            $passed++
        }
    }
}
Write-Output "PASS: $passed managed NUnit cases (party effects, command validation, shared inventory and save migration)"
