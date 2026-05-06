#
# Start-LiveCloudKd.ps1 -path D:\LiveCloudKd\LiveCloudKd.exe -parameters "/m 1 /b /y C:\WinDBG"
#

param (
    [string] $path, 
    [string] $parameters
    )

# $path = "D:\LiveCloudKd\LiveCloudKd.exe"
# $parameters = "/m 1 /b /y C:\WinDBG"
# $parameters = "/a 0 /n 0"
# $parameters = "/a 0 /n 0 /u 0 /m 1"
# $parameters = "/a 0 /n 0 /u 0 /m 1 /w"
# $parameters = "/o D:\dump\dump.dmp /y C:\WinDBG /a 3 /n 0 /u 0 /m 1"

if ($path -eq ""){

    $path = (Get-Location).Path
    $path = $path + "\LiveCloudKd.exe"

    if ((Test-Path -path $path) -eq $false){
        Write-Error "LiveCloudKd is not present in $path. Specify path to binaries."
        return
    }
}

$processOptions = @{
    FilePath = $path 
    ArgumentList = $parameters
}

Start-Process @processOptions