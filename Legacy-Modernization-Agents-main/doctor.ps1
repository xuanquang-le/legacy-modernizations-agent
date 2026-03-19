# doctor.ps1 - Windows version
param(
    [switch]$Fix
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "LEGACY MODERNIZATION AGENTS - DOCTOR" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ErrorCount = 0

# 1. Kiểm tra .NET SDK
Write-Host "🔍 Kiểm tra .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "  ✅ .NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "  ❌ .NET SDK chưa cài đặt" -ForegroundColor Red
    $ErrorCount++
}

# 2. Kiểm tra Docker
Write-Host "`n🔍 Kiểm tra Docker..." -ForegroundColor Yellow
try {
    $dockerVersion = docker --version
    Write-Host "  ✅ $dockerVersion" -ForegroundColor Green
} catch {
    Write-Host "  ❌ Docker chưa cài đặt hoặc chưa chạy" -ForegroundColor Red
    $ErrorCount++
}

# 3. Kiểm tra Docker Compose
Write-Host "`n🔍 Kiểm tra Docker Compose..." -ForegroundColor Yellow
if (Test-Path "docker-compose.yml") {
    Write-Host "  ✅ docker-compose.yml tồn tại" -ForegroundColor Green
} else {
    Write-Host "  ❌ docker-compose.yml không tìm thấy" -ForegroundColor Red
    $ErrorCount++
}

# 4. Kiểm tra file solution
Write-Host "`n🔍 Kiểm tra Solution..." -ForegroundColor Yellow
$slnFile = Get-ChildItem -Filter "*.sln" | Select-Object -First 1
if ($slnFile) {
    Write-Host "  ✅ Solution: $($slnFile.Name)" -ForegroundColor Green
} else {
    Write-Host "  ❌ Không tìm thấy file .sln" -ForegroundColor Red
    $ErrorCount++
}

# 5. Kiểm tra thư mục Agents
Write-Host "`n🔍 Kiểm tra Agents..." -ForegroundColor Yellow
if (Test-Path "Agents") {
    Write-Host "  ✅ Thư mục Agents tồn tại" -ForegroundColor Green
    $agentDirs = Get-ChildItem -Path "Agents" -Directory
    foreach ($dir in $agentDirs) {
        Write-Host "     📁 $($dir.Name)" -ForegroundColor Gray
    }
} else {
    Write-Host "  ❌ Không tìm thấy thư mục Agents" -ForegroundColor Red
    $ErrorCount++
}

# 6. Kiểm file COBOL mẫu
Write-Host "`n🔍 Kiểm tra COBOL samples..." -ForegroundColor Yellow
$samplePaths = @(
    "source\cobol",
    "Samples\COBOL",
    "Data\COBOL"
)

$foundSample = $false
foreach ($path in $samplePaths) {
    if (Test-Path $path) {
        Write-Host "  ✅ Tìm thấy COBOL samples tại: $path" -ForegroundColor Green
        $cobolFiles = Get-ChildItem -Path $path -Filter "*.cbl" -ErrorAction SilentlyContinue
        if ($cobolFiles) {
            foreach ($file in $cobolFiles | Select-Object -First 3) {
                Write-Host "     📄 $($file.Name)" -ForegroundColor Gray
            }
            if ($cobolFiles.Count -gt 3) {
                Write-Host "     ... và $($cobolFiles.Count - 3) file khác" -ForegroundColor Gray
            }
        }
        $foundSample = $true
        break
    }
}

if (-not $foundSample) {
    Write-Host "  ⚠️ Không tìm thấy file COBOL mẫu" -ForegroundColor Yellow
    Write-Host "     Bạn có thể tải mẫu từ: https://github.com/Azure-Samples/legacy-modernization-agents-dotnet" -ForegroundColor Gray
}

# 7. Kiểm tra GitHub Copilot
Write-Host "`n🔍 Kiểm tra GitHub Copilot..." -ForegroundColor Yellow
$copilot = code --list-extensions 2>$null | Select-String "GitHub.copilot"
if ($copilot) {
    Write-Host "  ✅ GitHub Copilot đã cài" -ForegroundColor Green
} else {
    Write-Host "  ⚠️ GitHub Copilot chưa cài" -ForegroundColor Yellow
    Write-Host "     Cài bằng: code --install-extension GitHub.copilot" -ForegroundColor Gray
}

# 8. Kiểm tra ports (nếu Docker đang chạy)
Write-Host "`n🔍 Kiểm tra ports cần thiết..." -ForegroundColor Yellow
$ports = @(5000, 5001, 7474, 7687) # Neo4j ports
foreach ($port in $ports) {
    $connection = Test-NetConnection -ComputerName localhost -Port $port -WarningAction SilentlyContinue -ErrorAction SilentlyContinue
    if ($connection.TcpTestSucceeded) {
        Write-Host "  ⚠️ Port $port đang được sử dụng" -ForegroundColor Yellow
    } else {
        Write-Host "  ✅ Port $port trống" -ForegroundColor Green
    }
}

# Kết luận
Write-Host "`n========================================" -ForegroundColor Cyan
if ($ErrorCount -eq 0) {
    Write-Host "✅ MỌI THỨ ĐÃ SẴN SÀNG! Bạn có thể build và chạy." -ForegroundColor Green
} else {
    Write-Host "❌ Có $ErrorCount lỗi cần xử lý" -ForegroundColor Red
}
Write-Host "========================================" -ForegroundColor Cyan