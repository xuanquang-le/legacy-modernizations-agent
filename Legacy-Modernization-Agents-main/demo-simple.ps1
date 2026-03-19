Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "COBOL REVERSE ENGINEERING DEMO" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Tạo thư mục output
$outputDir = "output\demo-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

Write-Host "📁 Source: source\cobol" -ForegroundColor Yellow
Write-Host "📁 Output: $outputDir" -ForegroundColor Yellow
Write-Host ""

Write-Host "🔍 Đang phân tích COBOL..." -ForegroundColor Green

# Chạy reverse-engineer
.\bin\Debug\net10.0\CobolToQuarkusMigration.exe reverse-engineer `
    --source "source\cobol" `
    --output $outputDir

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ THÀNH CÔNG!" -ForegroundColor Green
    Write-Host ""
    
    # Liệt kê files đã tạo
    Write-Host "📄 CÁC FILE ĐÃ TẠO:" -ForegroundColor Cyan
    $files = Get-ChildItem -Path $outputDir -Recurse
    foreach ($file in $files) {
        $size = "{0:N0}" -f ($file.Length / 1KB)
        Write-Host "   • $($file.Name) ($size KB)" -ForegroundColor Gray
    }
    
    # Hiển thị nội dung file đầu tiên
    $firstFile = $files | Select-Object -First 1
    if ($firstFile) {
        Write-Host ""
        Write-Host "📝 XEM TRƯỚC NỘI DUNG ($($firstFile.Name)):" -ForegroundColor Cyan
        Write-Host "=========================" -ForegroundColor Cyan
        Get-Content $firstFile.FullName -Head 20
        Write-Host ""
        Write-Host "Xem đầy đủ: notepad $($firstFile.FullName)" -ForegroundColor Yellow
    }
} else {
    Write-Host ""
    Write-Host "❌ CÓ LỖI XẢY RA" -ForegroundColor Red
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan