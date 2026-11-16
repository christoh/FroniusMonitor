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

# 1. The .NET 11 SDK. Ubuntu's archive has none (only dotnet-sdk-10.0, checked 2026-11-16), so it comes from
#    Microsoft's dotnet-install.sh, into the directory of the dotnet the image already has, so that one "dotnet" sees
#    every SDK. builds.dotnet.microsoft.com, where the script and the SDK come from, was once blocked by the egress
#    policy of the cloud environment, which is why this used to install .NET 10 from apt; on 2026-11-16 it was open.
if ! dotnet --list-sdks 2>/dev/null | grep -q '^11\.'; then
  if command -v dotnet >/dev/null 2>&1; then
    dotnetDir=$(dirname "$(readlink -f "$(command -v dotnet)")")
  else
    dotnetDir=/usr/lib/dotnet
  fi

  script=$(mktemp)
  curl -sSfL -o "$script" https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.sh
  bash "$script" --channel 11.0 --install-dir "$dotnetDir" --no-path >/dev/null
  rm -f "$script"

  if ! command -v dotnet >/dev/null 2>&1; then
    ln -sf "$dotnetDir/dotnet" /usr/local/bin/dotnet
  fi
fi

# 2. No compiler of its own any more. The .NET 10 SDK from Ubuntu shipped Roslyn 5.0, which did not know the preview C#
#    the solution uses, so this hook used to put Microsoft.Net.Compilers.Toolset 5.9.0 into every build through a user
#    level MSBuild import. The .NET 11 SDK brings Roslyn 5.12, and the old toolset would now be the older compiler:
#    the analyzers of ASP.NET Core 11 refuse it (CS9057). A container cached with the import loses it here. MSBuild
#    imports every file in that folder, whatever it is called, so it has to go, not be renamed.
rm -f "$HOME/.local/share/Microsoft/MSBuild/Current/Imports/Microsoft.Common.props/ImportBefore/ClaudeRoslynPreview.props"

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
