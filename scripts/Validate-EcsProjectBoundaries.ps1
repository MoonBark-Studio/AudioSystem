[CmdletBinding()]
param(
    [string]$WorkspaceRoot,
    [string[]]$Plugin,
    [switch]$Fix,
    [switch]$IncludeTests,
    [switch]$Json
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($WorkspaceRoot)) {
    $scriptRoot = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $PSCommandPath }
    $WorkspaceRoot = (Resolve-Path (Join-Path $scriptRoot '..')).Path
}

$ignoredPathFragments = @(
    '\bin\',
    '\obj\',
    '\.godot\',
    '\node_modules\',
    '\packages\'
)

$componentDeclarationPattern = '(?m)^\s*(?:public|internal|private|protected)?\s*(?:sealed\s+|partial\s+|readonly\s+|abstract\s+|static\s+)*?(?:class|struct|record(?:\s+struct|\s+class)?)\s+([A-Za-z_][A-Za-z0-9_]*Component)\b'
$namespacePattern = '(?m)^\s*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)\s*(?:;|\{)'

function Get-NormalizedPath {
    param([string]$Path)

    return $Path.Replace('/', '\\')
}

function Test-IgnoredPath {
    param([string]$Path)

    $normalized = Get-NormalizedPath $Path
    foreach ($fragment in $ignoredPathFragments) {
        if ($normalized.IndexOf($fragment, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            return $true
        }
    }

    return $false
}

function Test-TestLikePath {
    param([string]$Path)

    $normalized = Get-NormalizedPath $Path
    return $normalized -match '(?:^|\\)(?:Tests?|Benchmarks?|PerformanceTests?|Performance|Examples?|Demos?)(?:\\|$)'
}

function Get-RelativeDisplayPath {
    param(
        [string]$BasePath,
        [string]$TargetPath
    )

    $relativePathMethod = [System.IO.Path].GetMethod('GetRelativePath', [System.Type[]]@([string], [string]))
    if ($relativePathMethod) {
        return $relativePathMethod.Invoke($null, @($BasePath, $TargetPath)).Replace('\\', '/')
    }

    $baseFullPath = [System.IO.Path]::GetFullPath((Get-NormalizedPath $BasePath)).TrimEnd('\\') + '\\'
    $targetFullPath = [System.IO.Path]::GetFullPath((Get-NormalizedPath $TargetPath))
    $baseUri = [System.Uri]::new($baseFullPath)
    $targetUri = [System.Uri]::new($targetFullPath)

    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace('/', '/')
}

function Get-RelativePath {
    param(
        [string]$BasePath,
        [string]$TargetPath
    )

    return Get-NormalizedPath (Get-RelativeDisplayPath -BasePath $BasePath -TargetPath $TargetPath)
}

function Get-PluginRoots {
    param(
        [string]$Root,
        [string[]]$PluginFilter
    )

    $pluginsRoot = Join-Path $Root 'plugins'
    if (-not (Test-Path $pluginsRoot)) {
        throw "Plugins root not found: $pluginsRoot"
    }

    $roots = @(Get-ChildItem -Path $pluginsRoot -Directory | Sort-Object Name)
    $normalizedPluginFilter = @(
        foreach ($entry in @($PluginFilter)) {
            foreach ($name in ($entry -split ',')) {
                $trimmedName = $name.Trim()
                if (-not [string]::IsNullOrWhiteSpace($trimmedName)) {
                    $trimmedName
                }
            }
        }
    )

    if ($normalizedPluginFilter -and @($normalizedPluginFilter).Count -gt 0) {
        $requested = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
        foreach ($name in $normalizedPluginFilter) {
            [void]$requested.Add($name)
        }

        $roots = $roots | Where-Object { $requested.Contains($_.Name) }
    }

    return @($roots)
}

function Get-ProjectKind {
    param([string]$ProjectPath)

    $normalized = Get-NormalizedPath $ProjectPath
    $fileName = [System.IO.Path]::GetFileName($normalized)

    if ($normalized -match '(?:^|\\)(?:Tests?|Benchmarks?|PerformanceTests?|Performance|Examples?|Demos?)(?:\\|$)' -or
        $fileName -match '\.(?:Tests?|Benchmarks?|Demo|Example|StressTests?)\.csproj$') {
        return 'Auxiliary'
    }

    if ($fileName -match '\.ECS\.csproj$' -or $normalized -match '(?:^|\\)ECS\\[^\\]+\.csproj$') {
        return 'ECS'
    }

    if ($fileName -match '\.Godot(?:\.Views)?\.csproj$' -or $normalized -match '(?:^|\\)Godot(?:\.Views)?\\') {
        return 'Godot'
    }

    if ($fileName -match '\.Core\.csproj$' -or $normalized -match '(?:^|\\)Core\\') {
        return 'Core'
    }

    return 'Runtime'
}

function Get-ProjectInfo {
    param([System.IO.FileInfo]$ProjectFile)

    $projectPath = $ProjectFile.FullName
    $projectText = Get-Content -Path $projectPath -Raw
    try { [xml]$xml = $projectText } catch { return $null }

    $rootNamespace = $null
    $assemblyName = $null
    foreach ($propertyGroup in @($xml.Project.PropertyGroup)) {
        $rootNamespaceNode = $propertyGroup.SelectSingleNode('RootNamespace')
        if (-not $rootNamespace -and $rootNamespaceNode -and -not [string]::IsNullOrWhiteSpace($rootNamespaceNode.InnerText)) {
            $rootNamespace = [string]$rootNamespaceNode.InnerText
        }
        $assemblyNameNode = $propertyGroup.SelectSingleNode('AssemblyName')
        if (-not $assemblyName -and $assemblyNameNode -and -not [string]::IsNullOrWhiteSpace($assemblyNameNode.InnerText)) {
            $assemblyName = [string]$assemblyNameNode.InnerText
        }
    }

    $packageReferences = @()
    $projectReferences = @()
    $compileRemovePatterns = @()

    foreach ($itemGroup in @($xml.Project.ItemGroup)) {
        if ($itemGroup -isnot [System.Xml.XmlElement]) { continue }
        foreach ($packageReference in @($itemGroup.SelectNodes('PackageReference'))) {
            $include = $packageReference.GetAttribute('Include')
            if (-not [string]::IsNullOrWhiteSpace($include)) {
                $packageReferences += [string]$include
            }
        }

        foreach ($projectReference in @($itemGroup.SelectNodes('ProjectReference'))) {
            $include = $projectReference.GetAttribute('Include')
            if (-not [string]::IsNullOrWhiteSpace($include)) {
                $projectReferences += [string]$include
            }
        }

        foreach ($compile in @($itemGroup.SelectNodes('Compile'))) {
            $remove = $compile.GetAttribute('Remove')
            if (-not [string]::IsNullOrWhiteSpace($remove)) {
                $compileRemovePatterns += [string]$remove
            }
        }
    }

    $kind = Get-ProjectKind $projectPath
    $effectiveRootNamespace = if ($rootNamespace) { $rootNamespace } elseif ($assemblyName) { $assemblyName } else { [System.IO.Path]::GetFileNameWithoutExtension($projectPath) }

    [pscustomobject]@{
        Path = $projectPath
        Directory = Split-Path -Parent $projectPath
        FileName = [System.IO.Path]::GetFileName($projectPath)
        Kind = $kind
        RootNamespace = $effectiveRootNamespace
        PackageReferences = @($packageReferences | Sort-Object -Unique)
        ProjectReferences = @($projectReferences | Sort-Object -Unique)
        CompileRemovePatterns = @($compileRemovePatterns | Sort-Object -Unique)
        RawText = $projectText
    }
}

function Get-NearestProject {
    param(
        [string]$FilePath,
        [object[]]$Projects
    )

    $normalizedFile = Get-NormalizedPath $FilePath
    $candidates = @(
        $Projects | Where-Object {
            $normalizedProjectDir = Get-NormalizedPath $_.Directory
            $normalizedFile.StartsWith($normalizedProjectDir + '\', [System.StringComparison]::OrdinalIgnoreCase) -or
            $normalizedFile.Equals($normalizedProjectDir, [System.StringComparison]::OrdinalIgnoreCase)
        } | Sort-Object { $_.Directory.Length } -Descending
    )

    if (@($candidates).Count -gt 0) {
        return $candidates[0]
    }

    return $null
}

function Get-DeclaredNamespace {
    param([string]$Text)

    $match = [System.Text.RegularExpressions.Regex]::Match($Text, $namespacePattern)
    if ($match.Success) {
        return $match.Groups[1].Value
    }

    return $null
}

function Get-ExpectedNamespace {
    param(
        [string]$FilePath,
        [object]$Project
    )

    $projectRootNamespace = [string]$Project.RootNamespace
    $relativeDirectory = [System.IO.Path]::GetDirectoryName((Get-RelativePath -BasePath $Project.Directory -TargetPath $FilePath))
    if ([string]::IsNullOrWhiteSpace($relativeDirectory) -or $relativeDirectory -eq '.') {
        return $projectRootNamespace
    }

    $namespaceSuffix = ($relativeDirectory -split '[\\/]') -join '.'
    return "$projectRootNamespace.$namespaceSuffix"
}

function Set-FileNamespace {
    param(
        [string]$FilePath,
        [string]$ExpectedNamespace
    )

    $text = Get-Content -Path $FilePath -Raw
    $updated = [System.Text.RegularExpressions.Regex]::Replace(
        $text,
        $namespacePattern,
        { param($match)
            if ($match.Value.TrimEnd().EndsWith(';')) {
                return ($match.Value -replace [System.Text.RegularExpressions.Regex]::Escape($match.Groups[1].Value), $ExpectedNamespace)
            }

            return ($match.Value -replace [System.Text.RegularExpressions.Regex]::Escape($match.Groups[1].Value), $ExpectedNamespace)
        },
        1)

    if ($updated -ne $text) {
        [System.IO.File]::WriteAllText($FilePath, $updated)
        return $true
    }

    return $false
}

function Test-CompileRemoveContainsEcs {
    param([object]$Project)

    foreach ($pattern in $Project.CompileRemovePatterns) {
        if ($pattern -match '(?:^|[\\/])ECS(?:[\\/]|$)') {
            return $true
        }
    }

    return $false
}

function Add-CompileRemovePattern {
    param(
        [string]$ProjectPath,
        [string]$Pattern
    )

    [xml]$xml = Get-Content -Path $ProjectPath -Raw
    $itemGroup = $xml.CreateElement('ItemGroup')
    $compile = $xml.CreateElement('Compile')
    $null = $compile.SetAttribute('Remove', $Pattern)
    $null = $itemGroup.AppendChild($compile)
    $null = $xml.Project.AppendChild($itemGroup)
    $xml.Save($ProjectPath)
}

function Add-Violation {
    param(
        [System.Collections.Generic.List[object]]$Collection,
        [string]$PluginName,
        [string]$RuleId,
        [string]$Severity,
        [string]$Target,
        [string]$Message,
        [bool]$Fixable = $false,
        [string]$FixDescription = ''
    )

    $Collection.Add([pscustomobject]@{
        Plugin = $PluginName
        RuleId = $RuleId
        Severity = $Severity
        Target = $Target
        Message = $Message
        Fixable = $Fixable
        FixDescription = $FixDescription
    }) | Out-Null
}

$workspaceRootPath = (Resolve-Path $WorkspaceRoot).Path
$pluginRoots = Get-PluginRoots -Root $workspaceRootPath -PluginFilter $Plugin
$violations = [System.Collections.Generic.List[object]]::new()
$fixesApplied = [System.Collections.Generic.List[string]]::new()

foreach ($pluginRoot in $pluginRoots) {
    $pluginName = $pluginRoot.Name
    $projectFiles = @(Get-ChildItem -Path $pluginRoot.FullName -Recurse -File -Filter *.csproj |
        Where-Object { -not (Test-IgnoredPath $_.FullName) }
    )

    $projects = @($projectFiles | ForEach-Object { Get-ProjectInfo $_ } | Where-Object { $_ -ne $null })
    $runtimeProjects = @($projects | Where-Object { $_.Kind -ne 'Auxiliary' })
    $ecsProjects = @($runtimeProjects | Where-Object { $_.Kind -eq 'ECS' })
    $ecsDirectories = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($ecsProject in $ecsProjects) {
        [void]$ecsDirectories.Add((Get-NormalizedPath $ecsProject.Directory).TrimEnd('\'))
    }

    $ecsFoldersOnDisk = @(Get-ChildItem -Path $pluginRoot.FullName -Recurse -Directory |
        Where-Object { $_.Name -eq 'ECS' -and -not (Test-IgnoredPath $_.FullName) }
    )

    foreach ($folder in $ecsFoldersOnDisk) {
        $normalizedFolder = (Get-NormalizedPath $folder.FullName).TrimEnd('\')
        $folderHasProject = @($ecsProjects | Where-Object {
            (Get-NormalizedPath $_.Directory).TrimEnd('\').Equals($normalizedFolder, [System.StringComparison]::OrdinalIgnoreCase)
        })
        if (-not $folderHasProject) {
            Add-Violation -Collection $violations -PluginName $pluginName -RuleId 'PLUGIN_HAS_ECS_FOLDER_WITHOUT_ECS_CSPROJ' -Severity 'Error' -Target (Get-RelativeDisplayPath -BasePath $workspaceRootPath -TargetPath $folder.FullName) -Message 'Plugin contains an ECS folder with no dedicated .ECS.csproj. Create a project such as ECS/<Plugin>.ECS.csproj and stop compiling ECS code from a root project.'
        }
    }

    foreach ($project in $runtimeProjects) {
        $hasFrifloDependency = $project.PackageReferences -contains 'Friflo.Engine.ECS'
        $hasFrameworkEcsDependency = @($project.ProjectReferences | Where-Object { $_ -match 'MoonBark\.Framework\.ECS\.csproj' }).Count -gt 0
        $hasEcsCompileRemove = Test-CompileRemoveContainsEcs -Project $project
        $relativeProjectPath = Get-RelativeDisplayPath -BasePath $workspaceRootPath -TargetPath $project.Path

        if ($project.Kind -ne 'ECS' -and ($hasFrifloDependency -or $hasFrameworkEcsDependency)) {
            Add-Violation -Collection $violations -PluginName $pluginName -RuleId 'NON_ECS_PROJECT_HAS_ECS_DEPENDENCY' -Severity 'Error' -Target $relativeProjectPath -Message 'Non-ECS runtime project references ECS dependencies. ECS-dependent runtime code should compile from a dedicated .ECS.csproj.'
        }

        if ($project.Kind -eq 'ECS') {
            if (-not $project.RootNamespace.EndsWith('.ECS', [System.StringComparison]::Ordinal)) {
                Add-Violation -Collection $violations -PluginName $pluginName -RuleId 'ECS_PROJECT_ROOT_NAMESPACE_MISSING_SUFFIX' -Severity 'Error' -Target $relativeProjectPath -Message "ECS project RootNamespace '$($project.RootNamespace)' should end with .ECS." -Fixable:$false
            }

            if (-not $hasFrifloDependency) {
                Add-Violation -Collection $violations -PluginName $pluginName -RuleId 'ECS_PROJECT_MISSING_FRIFLO_DEPENDENCY' -Severity 'Error' -Target $relativeProjectPath -Message 'ECS project is missing the Friflo.Engine.ECS package reference.'
            }
        }

        if ($project.Kind -ne 'ECS' -and @($ecsFoldersOnDisk).Count -gt 0 -and -not $hasEcsCompileRemove) {
            $projectDir = $project.Directory
            $reachableEcsFolder = @($ecsFoldersOnDisk |
                Where-Object {
                    $normalizedProjectDir = (Get-NormalizedPath $projectDir).TrimEnd('\')
                    $normalizedEcsDir = (Get-NormalizedPath $_.FullName).TrimEnd('\')
                    $normalizedEcsDir.StartsWith($normalizedProjectDir + '\', [System.StringComparison]::OrdinalIgnoreCase)
                } |
                Select-Object -First 1)

            if ($reachableEcsFolder) {
                $relativePattern = (Get-RelativePath -BasePath $projectDir -TargetPath $reachableEcsFolder.FullName).TrimEnd('\') + '\**\*.cs'
                Add-Violation -Collection $violations -PluginName $pluginName -RuleId 'NON_ECS_PROJECT_MISSING_ECS_COMPILE_REMOVE' -Severity 'Warning' -Target $relativeProjectPath -Message "Non-ECS project does not exclude ECS sources. Add a Compile Remove for '$relativePattern'." -Fixable:$true -FixDescription "Add <Compile Remove=\"$relativePattern\" /> to $relativeProjectPath"

                if ($Fix) {
                    Add-CompileRemovePattern -ProjectPath $project.Path -Pattern $relativePattern
                    $fixesApplied.Add("Added Compile Remove '$relativePattern' to $relativeProjectPath") | Out-Null
                }
            }
        }
    }

    $sourceFiles = @(Get-ChildItem -Path $pluginRoot.FullName -Recurse -File -Filter *.cs |
        Where-Object {
            -not (Test-IgnoredPath $_.FullName) -and
            ($IncludeTests -or -not (Test-TestLikePath $_.FullName))
        }
    )

    foreach ($sourceFile in $sourceFiles) {
        $text = Get-Content -Path $sourceFile.FullName -Raw
        $nearestProject = Get-NearestProject -FilePath $sourceFile.FullName -Projects $runtimeProjects
        $relativeSourcePath = Get-RelativeDisplayPath -BasePath $workspaceRootPath -TargetPath $sourceFile.FullName
        $normalizedFilePath = Get-NormalizedPath $sourceFile.FullName
        $inEcsFolder = $normalizedFilePath -match '(?:^|\\)ECS(?:\\|$)'
        $declaredNamespace = Get-DeclaredNamespace -Text $text
        $hasEcsNamespace = $declaredNamespace -like '*.ECS*'

        $usesFriflo = $text -match '(?m)^\s*using\s+Friflo\.Engine\.ECS\s*;'
        $usesFrameworkEcs = $text -match '(?m)^\s*using\s+MoonBark\.Framework\.ECS\s*;'
        $mentionsIComponent = $text -match '\bIComponent\b'
        $mentionsEntityStore = $text -match '\bEntityStore\b'
        $mentionsArchetypeQuery = $text -match '\bArchetypeQuery\b'
        $componentTypeMatches = [System.Text.RegularExpressions.Regex]::Matches($text, $componentDeclarationPattern)
        $declaresComponentType = @($componentTypeMatches).Count -gt 0
        $isEcsDependent = $usesFriflo -or $usesFrameworkEcs -or $mentionsIComponent -or $mentionsEntityStore -or $mentionsArchetypeQuery -or $declaresComponentType

        if (-not $nearestProject) {
            Add-Violation -Collection $violations -PluginName $pluginName -RuleId 'SOURCE_FILE_WITHOUT_PROJECT' -Severity 'Warning' -Target $relativeSourcePath -Message 'Source file is not under any discovered runtime csproj directory.'
            continue
        }

        if ($isEcsDependent -and -not $inEcsFolder -and $nearestProject.Kind -ne 'ECS') {
            Add-Violation -Collection $violations -PluginName $pluginName -RuleId 'ECS_DEPENDENT_FILE_OUTSIDE_ECS_FOLDER' -Severity 'Error' -Target $relativeSourcePath -Message 'ECS-dependent source file is outside an ECS folder and non-ECS project boundary.'
        }

        if ($inEcsFolder -and $nearestProject.Kind -ne 'ECS') {
            Add-Violation -Collection $violations -PluginName $pluginName -RuleId 'ECS_FOLDER_COMPILED_BY_NON_ECS_PROJECT' -Severity 'Error' -Target $relativeSourcePath -Message "File sits under an ECS folder but the nearest runtime project is '$($nearestProject.FileName)', not a dedicated .ECS.csproj."
        }

        if ($declaresComponentType -and -not $inEcsFolder) {
            $componentNames = @($componentTypeMatches | ForEach-Object { $_.Groups[1].Value }) -join ', '
            Add-Violation -Collection $violations -PluginName $pluginName -RuleId 'COMPONENT_TYPE_OUTSIDE_ECS' -Severity 'Error' -Target $relativeSourcePath -Message "Type names with the Component suffix must live in ECS code. Found: $componentNames"
        }

        if ($inEcsFolder -and $declaredNamespace) {
            $expectedNamespace = Get-ExpectedNamespace -FilePath $sourceFile.FullName -Project $nearestProject
            if (-not $declaredNamespace.Equals($expectedNamespace, [System.StringComparison]::Ordinal)) {
                Add-Violation -Collection $violations -PluginName $pluginName -RuleId 'ECS_NAMESPACE_MISMATCH' -Severity 'Warning' -Target $relativeSourcePath -Message "Namespace '$declaredNamespace' does not match expected ECS namespace '$expectedNamespace'." -Fixable:$true -FixDescription "Rewrite namespace to '$expectedNamespace'"

                if ($Fix) {
                    if (Set-FileNamespace -FilePath $sourceFile.FullName -ExpectedNamespace $expectedNamespace) {
                        $fixesApplied.Add("Rewrote namespace in $relativeSourcePath to $expectedNamespace") | Out-Null
                    }
                }
            }
        }

        if (-not $inEcsFolder -and $hasEcsNamespace) {
            Add-Violation -Collection $violations -PluginName $pluginName -RuleId 'NON_ECS_FILE_HAS_ECS_NAMESPACE' -Severity 'Warning' -Target $relativeSourcePath -Message "Non-ECS file declares ECS namespace '$declaredNamespace'."
        }

        if ($usesFrameworkEcs -and @($nearestProject.ProjectReferences | Where-Object { $_ -match 'MoonBark\.Framework\.ECS\.csproj' }).Count -eq 0) {
            Add-Violation -Collection $violations -PluginName $pluginName -RuleId 'MISSING_FRAMEWORK_ECS_PROJECT_REFERENCE' -Severity 'Error' -Target $relativeSourcePath -Message 'File uses MoonBark.Framework.ECS but the nearest project has no MoonBark.Framework.ECS project reference.'
        }
    }
}

$orderedViolations = @($violations | Sort-Object Plugin, Severity, RuleId, Target)

if ($Json) {
    [pscustomobject]@{
        WorkspaceRoot = $workspaceRootPath
        PluginCount = @($pluginRoots).Count
        ViolationCount = @($orderedViolations).Count
        FixCount = @($fixesApplied).Count
        Violations = $orderedViolations
        FixesApplied = @($fixesApplied)
    } | ConvertTo-Json -Depth 6
}
else {
    Write-Host "ECS boundary validation root: $workspaceRootPath"
    Write-Host "Plugins scanned: $(@($pluginRoots).Count)"
    Write-Host "Violations found: $(@($orderedViolations).Count)"
    Write-Host "Fixes applied: $(@($fixesApplied).Count)"

    foreach ($violation in $orderedViolations) {
        $line = "[$($violation.Severity)] [$($violation.Plugin)] [$($violation.RuleId)] $($violation.Target) - $($violation.Message)"
        Write-Host $line
        if ($violation.Fixable -and $violation.FixDescription) {
            Write-Host "  fix: $($violation.FixDescription)"
        }
    }

    if (@($fixesApplied).Count -gt 0) {
        Write-Host ''
        Write-Host 'Applied fixes:'
        foreach ($fixMessage in $fixesApplied) {
            Write-Host "- $fixMessage"
        }
    }
}

if (@($orderedViolations | Where-Object { $_.Severity -eq 'Error' }).Count -gt 0) {
    exit 1
}

exit 0