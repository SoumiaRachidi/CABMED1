# Fix Character Encoding - Step by Step Guide

## Problem
French characters display as garbled text:
- `CrÈer` appears as `Cr√©er`
- `mÈdicales` appears as `m√©dicales`  
- `AntÈcÈdents` appears as `Ant√©c√©dents`
- `‡` appears as `√ `

## Root Cause
The `.cshtml` files are not saved as UTF-8 with BOM (Byte Order Mark), which ASP.NET MVC requires for proper French character rendering.

---

## Solution Methods

### Method 1: PowerShell Script (Easiest)

1. **Close Visual Studio** completely
2. **Open PowerShell** as Administrator
3. **Navigate** to your project folder:
   ```powershell
   cd "C:\Users\salah\Desktop\All\Studies\4eme annee\S7\Dot Net\Project\CABMED1"
   ```
4. **Run the conversion script**:
```powershell
   .\CABMED\Scripts\Fix-UTF8-Encoding.ps1
   ```
5. **Open Visual Studio** again
6. **Rebuild** the solution (Ctrl+Shift+B)
7. **Clear browser cache** (Ctrl+Shift+Delete)
8. **Test** the pages

---

### Method 2: Visual Studio (File by File)

#### For Each File (Register.cshtml, Login.cshtml, etc.):

1. **Open the file** in Visual Studio
2. **File** ? **Advanced Save Options...**
3. **Encoding**: Select **Unicode (UTF-8 with signature) - Codepage 65001**
4. Click **OK**
5. **Save** the file (Ctrl+S)
6. Repeat for all files with French text

#### Files to Fix:
- ? `Views\Auth\Register.cshtml`
- ? `Views\Auth\Login.cshtml`
- ? `Views\Home\Index.cshtml`
- ? `Views\Shared\_Layout.cshtml`

---

### Method 3: Notepad++ (Alternative)

1. **Close Visual Studio**
2. **Open each .cshtml file** in Notepad++
3. **Encoding** ? **Convert to UTF-8-BOM**
4. **File** ? **Save**
5. Repeat for all files
6. **Open Visual Studio** and rebuild

---

## Quick Fix: Direct File Replacement

If the above methods don't work, I can recreate the Register file content for you to copy-paste:

### Step 1: Backup Current File
1. Right-click `Register.cshtml` ? Copy
2. Paste somewhere safe

### Step 2: Delete and Recreate
1. **Delete** `Register.cshtml`
2. **Right-click** `Views\Auth` folder ? **Add** ? **View**
3. Name it `Register.cshtml`
4. **Copy the content from the backup**
5. **Save immediately as UTF-8-BOM** (File ? Advanced Save Options)

---

## Verification Steps

### Check if Encoding is Correct:

**PowerShell Command:**
```powershell
[System.IO.File]::ReadAllBytes(".\CABMED\Views\Auth\Register.cshtml")[0..2]
```

**Expected Output (UTF-8 with BOM):**
```
239
187
191
```

**Wrong Output (UTF-8 without BOM or ANSI):**
```
64
109
111
```

---

## Testing After Fix

1. **Stop debugging** (Shift+F5)
2. **Clean solution** (Build ? Clean Solution)
3. **Rebuild solution** (Ctrl+Shift+B)
4. **Clear browser cache**:
   - Chrome/Edge: Ctrl+Shift+Delete
   - Select "All time"
   - Check "Cached images and files"
   - Click "Clear data"
5. **Start application** (F5)
6. **Navigate** to `/Auth/Register`

### Expected Results:
- ? Before: `Cr√©er mon compte`
- ? After: `CrÈer mon compte`

- ? Before: `Ant√©c√©dents m√©dicaux`
- ? After: `AntÈcÈdents mÈdicaux`

---

## Common Issues & Solutions

### Issue 1: "Advanced Save Options" Not Visible
**Solution:**
1. **Tools** ? **Customize**
2. **Commands** tab
3. **Menu bar:** File
4. **Add Command** ? **File** ? **Advanced Save Options**

### Issue 2: File Keeps Reverting to Wrong Encoding
**Solution:**
1. Check `.editorconfig` file (if exists)
2. Check `.gitattributes` file
3. Ensure Visual Studio settings:
   - Tools ? Options ? Environment ? Documents
   - Check "Save documents as Unicode when data cannot be saved in codepage"

### Issue 3: Still Garbled After Fixing
**Solution:**
1. **Hard refresh** browser: Ctrl+F5
2. **Clear ASP.NET temporary files**:
   ```
   C:\Windows\Microsoft.NET\Framework\v4.0.30319\Temporary ASP.NET Files
   C:\Windows\Microsoft.NET\Framework64\v4.0.30319\Temporary ASP.NET Files
   ```
3. **Restart IIS Express**

### Issue 4: Works Locally, Not on Server
**Check:**
1. Server's `Web.config` has globalization settings
2. Server IIS has UTF-8 response encoding
3. Files uploaded to server are UTF-8-BOM

---

## Prevention for Future

### Visual Studio Settings:
1. **Tools** ? **Options**
2. **Text Editor** ? **Advanced**
3. Check **"Auto-detect UTF-8 encoding without signature"**

### Git Settings (`.gitattributes`):
Add to `.gitattributes`:
```
*.cshtml text eol=crlf encoding=utf-8
*.cs text eol=crlf encoding=utf-8
```

### EditorConfig (`.editorconfig`):
```ini
[*.cshtml]
charset = utf-8-bom
end_of_line = crlf
```

---

## Quick Test File

Create this test file to verify encoding:

**`test-encoding.cshtml`:**
```html
<!DOCTYPE html>
<html lang="fr">
<head>
    <meta charset="utf-8" />
    <title>Test UTF-8</title>
</head>
<body>
  <h1>Test des caractËres franÁais</h1>
    <p>‡‚‰ÈËÍÎÔÓÙ˘˚¸ˇÁúÊ¿¬ƒ…» ÀœŒ‘Ÿ€‹ü«å∆</p>
    <p>CrÈer ï MÈdical ï AntÈcÈdents ï TÈlÈphone</p>
</body>
</html>
```

If this displays correctly, your encoding is fixed!

---

## Summary Checklist

- [ ] Close Visual Studio
- [ ] Run PowerShell script OR use Advanced Save Options
- [ ] Verify all .cshtml files are UTF-8-BOM
- [ ] Rebuild solution
- [ ] Clear browser cache
- [ ] Test all pages
- [ ] Commit changes to Git

---

## Need Help?

If issues persist:
1. Check `Web.config` has globalization settings (already done ?)
2. Check `Views\Web.config` has globalization settings (already done ?)
3. Verify browser is set to UTF-8 encoding
4. Check server response headers include `charset=utf-8`

**The PowerShell script is the fastest solution!** It will convert all files automatically.
