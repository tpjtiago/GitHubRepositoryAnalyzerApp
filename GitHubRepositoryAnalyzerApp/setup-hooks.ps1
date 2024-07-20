# setup-hooks.ps1

$hookSourcePath = "$PSScriptRoot/hooks"
$hookTargetPath = "$PSScriptRoot/.git/hooks"

# Copiar hooks
Copy-Item -Path "$hookSourcePath/*" -Destination $hookTargetPath -Recurse -Force

# Tornar hooks executáveis
chmod +x "$hookTargetPath/commit-msg"
chmod +x "$hookTargetPath/pre-push"

Write-Output "Git hooks configurados com sucesso."
