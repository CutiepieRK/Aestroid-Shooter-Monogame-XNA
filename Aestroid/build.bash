# Define your custom output directory
OUTPUT_DIR="$HOME/Builds/Monogame/Aestroid"

# Publish for Linux (x64)
dotnet publish -c Release -r linux-x64 --self-contained -o "$OUTPUT_DIR/linux"

# Publish for Windows (x64)
dotnet publish -c Release -r win-x64 --self-contained -o "$OUTPUT_DIR/windows"

# Publish for macOS (universal for Apple Silicon & Intel)
dotnet publish -c Release -r osx-x64 --self-contained -o "$OUTPUT_DIR/macos-x64"
dotnet publish -c Release -r osx-arm64 --self-contained -o "$OUTPUT_DIR/macos-arm64"
