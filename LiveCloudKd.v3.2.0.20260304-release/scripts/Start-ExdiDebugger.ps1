#
# Original script was taken from:
# https://github.com/microsoft/WinDbg-Samples/blob/master/Exdi/exdigdbsrv/Start-ExdiDebugger.ps1
#

<#
.Synopsis
    Installs and launches EXDI extension for Hyper-V (static view)

.Description
    This script will install ExdiHvSrv.dll if required, check for running dllhost.exe processes, and launch the debugger to connect to
    Hyper-V VM.

.Parameter ExdiDllPath
    Location of the ExdiHvSrv.dll. It will check if ExdiHvSrv.dll is not installed or is installed incorrectly and reinstall it. 

.Parameter ExtraDebuggerArgs
    Extra arguments to pass on the debugger command line

.Parameter DontTryDllHostCleanup
    Checking for existing running instances of ExdiHvSrv.dll in dllhost.exe requires elevation.
    Providing this switch will allow the script to run without elevation (although the debugger may not
    function correctly).

.Example
    .\Start-ExdiDebugger.ps1 -ExdiDllLocation C:\Exdi\ExdiHvSrv.dll -ExdiDllLocation "C:\path\to\built\exdi\files"
#>

[CmdletBinding()]
param
(
    [Parameter(Mandatory=$false)]

    [string]
    $ExdiDllLocation,

    [string]
    $DebuggerPath,

    [string[]]
    $ExtraDebuggerArgs = @(),

    [switch]
    $DontTryDllHostCleanup
)

$ErrorActionPreference = "Stop"
$ExdiDllName = "ExdiHvSrv.dll"

#region Functions

Function Test-Admin
{
    ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]"Administrator")
}

Function Find-PathToWindbgX
{
    $InternalWindbgXPath = "$env:LOCALAPPDATA\DBG\UI\WindbgX.exe"
    $ExternalWindbgXPath = "$env:LOCALAPPDATA\Microsoft\WindowsApps\WinDbgX.exe"

    if (Test-Path $InternalWindbgXPath -PathType Leaf)
    {
        return $InternalWindbgXPath
    }
    elseif (Test-Path $ExternalWindbgXPath -PathType Leaf)
    {
        return $ExternalWindbgXPath
    }
}

Function Test-ParameterValidation
{
    $CommandName = $PSCmdlet.MyInvocation.InvocationName
    $ParameterList = (Get-Command -Name $CommandName).Parameters

    foreach ($Parameter in $ParameterList) {
        Get-Variable -Name $Parameter.Values.Name -ErrorAction SilentlyContinue | Out-String | Write-Verbose
    }

    if (-not $DebuggerPath)
    {
        throw "WindbgX is not installed"
    }
    elseif (-not (Test-Path $DebuggerPath -PathType Leaf))
    {
        throw "DebuggerPath param ($DebuggerPath) does not point to a debugger."
    }

    # Searching for loaded instances of ExdiHvSrv.dll in dllhost.exe requires elevation
    if (-not $DontTryDllHostCleanup -and
        -not $(Test-Admin))
    {
        throw "Searching for loaded instances of ExdiHvSrv.dll in dllhost.exe requires elevation. Run with the -DontTryDllHostCleanup parameter to skip this check (debugger session init may fail)."
    }
}

Function Get-ExdiInstallPath
{
    Get-ItemPropertyValue -Path "Registry::HKEY_CLASSES_ROOT\CLSID\{53838F70-0936-44A9-AB4E-ABB568401508}\InProcServer32" -Name "(default)" -ErrorAction SilentlyContinue
}

Function Test-ExdiServerInstalled
{
    # Check registration of exdi server class
    if ($(Get-ExdiInstallPath) -ne $null -and $(Test-Path "$(Get-ExdiInstallPath)"))
    {
        Write-Verbose "Exdi server is installed. Checking installation..."
        $ExdiInstallDir = [System.IO.Path]::GetDirectoryName($(Get-ExdiInstallPath))
        if (-not (Test-Path $ExdiInstallDir))
        {
            Write-Host "Currently Registered exdi server does not exist. Reinstalling..."
            return $false
        }
        else
        {
            Write-Verbose "Exdi server is insalled correctly. Skipping installation..."
            return $true
        }
    }
    else
    {
        Write-Host "Exdi server is not installed. Installing..."
        return $false
    }
}

Function Install-ExdiServer
{
    [CmdletBinding()]
    param
    (
        [string] $InstallTo
    )
    
    if (-not $(Test-Admin))
    {
        throw "Script needs to be run as an Admin to install exdi software."
    }

    if (-not (Test-Path $InstallTo\ExdiHvSrv.dll -PathType Leaf))
    {
        throw "DebuggerPath param ($InstallFrom\$ExdiDllName) does not point to a debugger."
    }

    regsvr32 /s "$InstallTo\$ExdiDllName"

    if ($(Get-ExdiInstallPath) -eq $null)
    {
        throw "Unable to install exdi server"
    }
}

Function Stop-ExdiContainingDllHosts
{
    $DllHostPids = Get-Process dllhost | ForEach-Object { $_.Id }
    foreach ($DllHostPid in $DllHostPids)
    {
        $DllHostExdiDlls = Get-Process -Id $DllHostPid -Module | Where-Object { $_.FileName -like "*$ExdiDllName" }
        if ($DllHostExdiDlls.Count -ne 0)
        {
            Write-Verbose "Killing dllhost.exe with pid $DllHostPid (Contained instance of ExdiHvSrv.dll)"
            Stop-Process -Id $DllHostPid -Force
        }
    }
}

#endregion

#region Script

# Apply defaults for $DebuggerPath before Parameter validation
if (-not $DebuggerPath)
{
    $DebuggerPath = Find-PathToWindbgX
}

Test-ParameterValidation

# look clean up dllhost.exe early since it can hold a lock on files which
# need to be overwritten
if (-not $DontTryDllHostCleanup)
{
    Stop-ExdiContainingDllHosts
}

if (-not $(Test-ExdiServerInstalled))
{
    if (-not $ExdiDllLocation)
    {
        throw "ExdiServer is not installed and -ExdiDllLocation is not valid"
    }

    $ExdiInstallDir = Join-Path -Path "$([System.IO.Path]::GetDirectoryName($DebuggerPath))" -ChildPath "exdi"
    Install-ExdiServer -InstallFrom "$ExdiDllLocation" -InstallTo "$ExdiInstallDir"
}

$DebuggerArgs = @("-v", "-kx exdi:CLSID={53838F70-0936-44A9-AB4E-ABB568401508},Kd=Guess")
Write-Verbose "DebuggerPath = $DebuggerPath"
Start-Process -FilePath "$DebuggerPath" -ArgumentList ($DebuggerArgs + $ExtraDebuggerArgs)

#endregion.