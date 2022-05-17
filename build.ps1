#cd src/mps
Remove-Item artifacts -R
dotnet build -c Release -o artifacts
