import os
content = '@echo off\r\nchcp 65001 >nul\r\nset "csc=%windir%\\Microsoft.NET\\Framework\\v4.0.30319\\csc.exe"\r\necho Compiling WT Launcher...\r\n"%csc%" /nologo /target:winexe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /out:"WT Launcher.exe" Program.cs\r\nif %errorlevel% equ 0 (\r\n    echo OK: WT Launcher.exe\r\n) else (\r\n    echo FAILED\r\n)\r\npause\r\n'
with open('build.bat', 'w', newline='') as f:
    f.write(content)
# verify
with open('build.bat', 'rb') as f:
    data = f.read()
print('Hex:', data[:60].hex())
assert b'\x0b' not in data, 'FAIL: has vertical tab'
assert data[8:10] == b'\r\n', 'FAIL: not CRLF'
print('OK')
