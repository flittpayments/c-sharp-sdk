#!/usr/bin/env bash
set -euo pipefail

tests_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_dir="$(cd "${tests_dir}/.." && pwd)"
dotnet_exe="${DOTNET_EXE:-dotnet}"
baseline_package="${FLITT_BASELINE_PACKAGE:-/tmp/flitt-original-csharp-sdk/FlittSDK/bin/Release/FlittSDK.1.0.0.nupkg}"
validation_dir="$(mktemp -d)"

cleanup() {
  rm -rf "${validation_dir}"
}
trap cleanup EXIT

"${dotnet_exe}" build "${repo_dir}/FlittSDK/FlittSDK.csproj" \
  --configuration Release \
  -p:GeneratePackageOnBuild=false

"${dotnet_exe}" run \
  --project "${tests_dir}/FlittSDK.CompatibilityHarness/FlittSDK.CompatibilityHarness.csproj" \
  --configuration Release

DOTNET_EXE="${dotnet_exe}" \
FLITT_BASELINE_PACKAGE="${baseline_package}" \
  bash "${tests_dir}/TestStands/run.sh"

if [[ -f "${baseline_package}" ]]; then
  "${dotnet_exe}" pack "${repo_dir}/FlittSDK/FlittSDK.csproj" \
    --configuration Release \
    --output "${validation_dir}" \
    -p:EnablePackageValidation=true \
    -p:PackageValidationBaselinePath="${baseline_package}"
else
  echo "API package validation skipped: baseline package not found at ${baseline_package}."
fi

if [[ "${RUN_LIVE_TESTS:-0}" == "1" ]]; then
  "${dotnet_exe}" test \
    "${tests_dir}/FlittSDK.LegacyTests/FlittSDK.LegacyTests.csproj" \
    --configuration Release
  "${dotnet_exe}" run \
    --project "${tests_dir}/FlittSDK.LiveHarness/FlittSDK.LiveHarness.csproj" \
    --configuration Release
else
  echo "Live API tests skipped. Set RUN_LIVE_TESTS=1 to enable them."
fi

echo "All requested FlittSDK test suites passed."
