$binFolder = "Bin"

if (Test-Path $binFolder)
{
    Remove-Item -Recurse $binFolder
}

Write-Host "cleaned."