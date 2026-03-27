# WenBrowser One-Click Installer (Local Copy & Shortcut Creation)

$installDir = "$env:LOCALAPPDATA\WenBrowser"
$sourcePath = "$PSScriptRoot\publish"

Write-Host "--- 🌊 WenBrowser 极简安装程序 ---" -ForegroundColor Cyan

if (!(Test-Path $sourcePath)) {
    Write-Error "错误: 找不到发布目录 $sourcePath. 请确保已运行 'dotnet publish'。"
    exit
}

if (!(Test-Path $installDir)) {
    Write-Host "正在创建目录: $installDir"
    New-Item -ItemType Directory -Path $installDir | Out-Null
}

Write-Host "正在部署核心文件..."
Copy-Item -Path "$sourcePath\*" -Destination $installDir -Recurse -Force

Write-Host "正在创建桌面快捷方式..."
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$env:USERPROFILE\Desktop\Wen 浏览器.lnk")
$Shortcut.TargetPath = "$installDir\WenBrowser.exe"
$Shortcut.WorkingDirectory = $installDir
$Shortcut.IconLocation = "$installDir\assets\logo.ico"
$Shortcut.Save()

Write-Host "------------------------------------"
Write-Host "安装完成！快去桌面看看“Wen 浏览器”吧。" -ForegroundColor Green
Read-Host "按回车键退出..."
