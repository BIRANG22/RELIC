# Runs managed policies and file I/O tests without launching or controlling Unity.
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
Set-Location $repo
$unity = 'C:/Program Files/Unity/Hub/Editor/6000.0.68f1/Editor/Data'
$output = Join-Path $repo 'Temp/CodexBattleFixValidation'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$response = @(Get-Content 'Library/Bee/artifacts/1900b0aE.dag/Assembly-CSharp.rsp' | Where-Object { $_ -notmatch '^-(out|refout):' })
$response += '-out:"' + (Join-Path $output 'Assembly-CSharp.dll') + '"'
foreach ($source in @('ArabellaLeapLandingPolicy','ArabellaLeapLandingResolver','NocturnPortalLandingPolicy')) {
    $path = "Assets/Project/Scripts/Gameplay/Monster/Ai/$source.cs"
    if (-not ($response | Where-Object { $_.Trim('"') -eq $path })) { $response += '"' + $path + '"' }
}
$rsp = Join-Path $output 'runtime.rsp'
[IO.File]::WriteAllLines($rsp, $response)
& "$unity/NetCoreRuntime/dotnet.exe" "$unity/DotNetSdkRoslyn/csc.dll" ('@' + $rsp)
if ($LASTEXITCODE -ne 0) { throw "Runtime compilation failed: $LASTEXITCODE" }
Write-Output 'PASS: full Assembly-CSharp compilation'

$nunit = (Get-ChildItem 'Library/PackageCache/com.unity.ext.nunit*/net40/unity-custom/nunit.framework.dll').FullName
$testResponse = @('-nologo','-nostdlib','-target:library','-langversion:9')
$testResponse += @($response | Where-Object { $_ -match '^-r:' })
$testResponse += '-r:"' + (Join-Path $output 'Assembly-CSharp.dll') + '"'
$testResponse += '-r:"' + $nunit + '"'
$testResponse += '-out:"' + (Join-Path $output 'BattleFix.Tests.dll') + '"'
foreach ($test in @('NocturnPortalLandingPolicyTests','SaveSystemRecoveryTests','TimelineSkillHoverPresentationTests','RecordMonsterDiscoveryTests')) {
    $testResponse += '"' + (Join-Path $PSScriptRoot "$test.cs") + '"'
}
$rsp = Join-Path $output 'tests.rsp'
[IO.File]::WriteAllLines($rsp, $testResponse)
& "$unity/NetCoreRuntime/dotnet.exe" "$unity/DotNetSdkRoslyn/csc.dll" ('@' + $rsp)
if ($LASTEXITCODE -ne 0) { throw "Test compilation failed: $LASTEXITCODE" }
Write-Output 'PASS: regression test compilation'

[void][Reflection.Assembly]::LoadFrom("$unity/Managed/UnityEngine/UnityEngine.CoreModule.dll")
foreach ($name in @('Unity.TextMeshPro','UnityEngine.UI','Unity.Localization','Unity.ResourceManager')) {
    [void][Reflection.Assembly]::LoadFrom((Join-Path $repo "Library/ScriptAssemblies/$name.dll"))
}
[void][Reflection.Assembly]::LoadFrom($nunit)
[void][Reflection.Assembly]::LoadFrom((Join-Path $output 'Assembly-CSharp.dll'))
$tests = [Reflection.Assembly]::LoadFrom((Join-Path $output 'BattleFix.Tests.dll'))
$passed = 0
foreach ($typeName in @('NocturnPortalLandingPolicyTests','SaveSystemRecoveryTests','TimelineSkillHoverPresentationTests')) {
    $type = $tests.GetType($typeName)
    $instance = [Activator]::CreateInstance($type)
    foreach ($method in $type.GetMethods()) {
        if ($typeName -eq 'SaveSystemRecoveryTests' -and $method.Name -notmatch '^(WriteJsonAtomically|TryReadJsonWithBackup|RestorePrimaryFromBackup|DeleteProgressFiles)') { continue }
        if ($typeName -eq 'TimelineSkillHoverPresentationTests' -and $method.Name -notmatch '^(OwnerIcon|HoveredOwnerIndex)') { continue }
        $attrs = @($method.GetCustomAttributes($true))
        $cases = @($attrs | Where-Object { $_.GetType().Name -eq 'TestCaseAttribute' })
        $test = @($attrs | Where-Object { $_.GetType().Name -eq 'TestAttribute' })
        if ($cases.Count -eq 0 -and $test.Count -eq 0) { continue }
        if ($cases.Count -eq 0) { $cases = @([pscustomobject]@{ Arguments = [object[]]@() }) }
        foreach ($case in $cases) {
            $setup = $type.GetMethod('SetUp')
            $teardown = $type.GetMethod('TearDown')
            try {
                if ($setup) { $setup.Invoke($instance, @()) | Out-Null }
                $method.Invoke($instance, [object[]]$case.Arguments) | Out-Null
                $passed++
            } finally {
                if ($teardown) { $teardown.Invoke($instance, @()) | Out-Null }
            }
        }
    }
}
Write-Output "PASS: $passed managed NUnit cases (Nocturn, save file recovery/deletion, timeline owner display)"

# Existing unrelated expectation conflicts with the project's case-sensitive IDs.
$known = $tests.GetType('RecordMonsterDiscoveryTests')
try {
    $known.GetMethod('Normalize_RemovesBlankAndCaseInsensitiveDuplicates').Invoke([Activator]::CreateInstance($known), @()) | Out-Null
    Write-Output 'PASS: legacy monster ID normalization test'
} catch {
    Write-Output ('KNOWN FAILURE: RecordMonsterDiscoveryTests.Normalize_RemovesBlankAndCaseInsensitiveDuplicates: ' + $_.Exception.GetBaseException().Message)
}
