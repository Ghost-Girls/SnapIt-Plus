rmdir /s /q build

dotnet publish ../SnapIt -c Standalone -a x86 -o ./build --self-contained false

"D:\Program Files\Inno Setup 6\ISCC.exe" innoSetup.iss /DMyAppVersion=4.2.0.0

rmdir /s /q build