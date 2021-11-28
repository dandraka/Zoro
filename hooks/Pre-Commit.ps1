
# To enable:
# 1.Rename .git/hooks/pre-commit.sample to pre-commit
# 2. Add the following line:
# pwsh -NoProfile -ExecutionPolicy Bypass -File "./hooks/Pre-Commit.ps1"

$rootPath = [System.IO.Path]::Combine($PSScriptRoot, '..')

$nuspecList = Get-ChildItem -path $rootPath -recurse -filter '*.nuspec' `
	| Where-Object { -not ($_.FullName.Contains('obj') -or `
	($_.FullName.Contains('bin'))) } `
	| Select-Object -property 'FullName'
$projList = Get-ChildItem -path $rootPath -recurse -filter '*.csproj' `
	| Select-Object -property 'FullName'

#$nuspecList 
#$projList 

$nuspecFile = $nuspecList[0]

$nuspecContents = Get-Content -Path $nuspecFile.FullName
$nuspecVersion = (Select-String -inputObject $nuspecContents -Pattern '<version>.*</version>').Matches[0].Value.ToLowerInvariant().Replace('<version>','').Replace('</version>','')
Write-Host "Setting all csproj to version $nuspecVersion"

foreach($projFile in $projList ) {
	$projContents = Get-Content -Path $projFile.FullName	
	$projVersion = (Select-String -inputObject $projContents -Pattern '<version>.*</version>').Matches[0].Value.ToLowerInvariant().Replace('<version>','').Replace('</version>','')
	
	$projContentsUpd = $projContents.Replace($projVersion, $nuspecVersion)
	Out-File -FilePath $projFile.FullName -InputObject $projContentsUpd
}
