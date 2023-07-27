$binariesFolder = "$env:TEMP/firebird-binaries"

# Download Firebird binaries
Remove-Item $binariesFolder -Recurse -Force -ErrorAction SilentlyContinue
git clone --quiet --depth 1 --single-branch https://github.com/fdcastel/firebird-binaries $binariesFolder
