#!/usr/bin/env bash
set -euo pipefail

stand_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_dir="$(cd "${stand_dir}/../.." && pwd)"
dotnet_exe="${DOTNET_EXE:-dotnet}"
work_dir="$(mktemp -d)"
feed_dir="${work_dir}/feed"
upgrade_dir="${work_dir}/UpgradeFrom1"
baseline_package="${FLITT_BASELINE_PACKAGE:-/tmp/flitt-original-csharp-sdk/FlittSDK/bin/Release/FlittSDK.1.0.0.nupkg}"

cleanup() {
  rm -rf "${work_dir}"
}
trap cleanup EXIT

mkdir -p "${feed_dir}"
"${dotnet_exe}" pack "${repo_dir}/FlittSDK/FlittSDK.csproj" \
  --configuration Release \
  --output "${feed_dir}"

if [[ -f "${baseline_package}" ]]; then
  cp "${baseline_package}" "${feed_dir}/"
fi

restore_project() {
  "${dotnet_exe}" restore "$1" \
    --source "${feed_dir}" \
    --source "https://api.nuget.org/v3/index.json" \
    --ignore-failed-sources
}

restore_project "${stand_dir}/CleanInstall/CleanInstall.csproj"
"${dotnet_exe}" run \
  --project "${stand_dir}/CleanInstall/CleanInstall.csproj" \
  --configuration Release \
  --no-restore

cp -R "${stand_dir}/UpgradeFrom1" "${upgrade_dir}"
restore_project "${upgrade_dir}/UpgradeFrom1.csproj"
"${dotnet_exe}" run \
  --project "${upgrade_dir}/UpgradeFrom1.csproj" \
  --configuration Release \
  --no-restore

"${dotnet_exe}" add "${upgrade_dir}/UpgradeFrom1.csproj" package FlittSDK \
  --version 2.0.0 \
  --source "${feed_dir}" \
  --no-restore
restore_project "${upgrade_dir}/UpgradeFrom1.csproj"
"${dotnet_exe}" run \
  --project "${upgrade_dir}/UpgradeFrom1.csproj" \
  --configuration Release \
  --no-restore

echo "Clean-install and 1.0.0-to-2.0.0 upgrade stands passed."
