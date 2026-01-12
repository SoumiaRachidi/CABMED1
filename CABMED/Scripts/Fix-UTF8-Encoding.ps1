# UTF-8 BOM Fixer for .cshtml files
# This script converts files to UTF-8 with BOM encoding

Write-Host "=== UTF-8 BOM Converter ===" -ForegroundColor Cyan
Write-Host ""

# Get all .cshtml files
$files = @(
 ".\CABMED\Views\Auth\Login.cshtml",
  ".\CABMED\Views\Auth\Register.cshtml",
    ".\CABMED\Views\Home\Index.cshtml",
    ".\CABMED\Views\Shared\_Layout.cshtml"
)

Write-Host "Files to convert:" -ForegroundColor Yellow
$files | ForEach-Object { Write-Host "  - $_" }
Write-Host ""

$converted = 0
$errors = 0

foreach ($file in $files) {
if (Test-Path $file) {
        try {
       Write-Host "Converting: $file" -ForegroundColor White
      
  # Read content
  $content = Get-Content -Path $file -Raw -Encoding UTF8
     
   # Write with UTF-8 BOM
 $utf8BOM = New-Object System.Text.UTF8Encoding $true
            [System.IO.File]::WriteAllText($file, $content, $utf8BOM)
 
            Write-Host "  ? Converted successfully" -ForegroundColor Green
            $converted++
        }
        catch {
     Write-Host "  ? Error: $_" -ForegroundColor Red
          $errors++
      }
    }
    else {
      Write-Host "? File not found: $file" -ForegroundColor Red
        $errors++
    }
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "Converted: $converted files" -ForegroundColor Green
Write-Host "Errors: $errors files" -ForegroundColor $(if ($errors -gt 0) { "Red" } else { "Green" })
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Close Visual Studio if it's open"
Write-Host "2. Rebuild the solution"
Write-Host "3. Clear browser cache (Ctrl+Shift+Delete)"
Write-Host "4. Test the pages"
Write-Host ""
