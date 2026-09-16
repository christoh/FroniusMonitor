#!/bin/bash
# SessionStart hook for Claude Code on the web: gives the cloud container a .NET SDK that can build this solution.
# Does nothing on a developer's machine. Safe to run repeatedly; the container state is cached afterwards.
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

export DEBIAN_FRONTEND=noninteractive
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

# 1. The .NET 10 SDK from apt. The Microsoft download host (builds.dotnet.microsoft.com, used by dotnet-install.sh)
#    is blocked by the egress policy of the cloud environment; Ubuntu's own archive and packages.microsoft.com are not.
if ! command -v dotnet >/dev/null 2>&1 || ! dotnet --list-sdks 2>/dev/null | grep -q '^10\.'; then
  if [ ! -f /etc/apt/sources.list.d/microsoft-prod.list ]; then
    tmp=$(mktemp)
    curl -sSL -o "$tmp" https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb
    dpkg -i "$tmp" >/dev/null
    rm -f "$tmp"
  fi
  # A third party PPA in the image is blocked too; its failure must not stop the update.
  apt-get update -qq || true
  apt-get install -y -qq dotnet-sdk-10.0
fi

# 2. A newer C# compiler. The SDK's Roslyn 5.0 does not know the preview C# the solution uses (collection expression
#    arguments, "with(...)"), the newest Microsoft.Net.Compilers.Toolset from NuGet does. A user level MSBuild import
#    adds it to every .csproj built in this container, so the repository's own build files stay as they are.
importDir="$HOME/.local/share/Microsoft/MSBuild/Current/Imports/Microsoft.Common.props/ImportBefore"
mkdir -p "$importDir"
cat > "$importDir/ClaudeRoslynPreview.props" <<'PROPS'
<Project>
  <!-- Installed by .claude/hooks/session-start.sh in Claude Code on the web. The Ubuntu .NET 10 SDK ships Roslyn 5.0,
       which does not know the preview C# the solution uses (collection expression arguments, "with(...)").
       The newest compiler toolset from NuGet replaces it for every build in this container. -->
  <ItemGroup Condition="'$(MSBuildProjectExtension)' == '.csproj'">
    <PackageReference Include="Microsoft.Net.Compilers.Toolset" Version="5.9.0" PrivateAssets="all" IsImplicitlyDefined="true" />
  </ItemGroup>
</Project>
PROPS

# 3. Environment for the session.
if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  {
    echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1'
    echo 'export DOTNET_NOLOGO=1'
  } >> "$CLAUDE_ENV_FILE"
fi

# 4. Restore the packages the desktop client and the unit tests need, so the first build in the session is quick.
cd "${CLAUDE_PROJECT_DIR:-$(cd "$(dirname "$0")/../.." && pwd)}"
dotnet restore HomeAutomationClient/HomeAutomationClient/HomeAutomationClient.csproj >/dev/null
dotnet restore HomeAutomationServerTests/HomeAutomationServerTests.csproj >/dev/null

echo "session-start: $(dotnet --version) ready"
