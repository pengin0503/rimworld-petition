#!/usr/bin/env bash
# Compiles the mod against newer RimWorld 1.7 reference assemblies to catch
# APIs that were removed or changed since the pinned version. The real build
# keeps the pinned (older) references so it still loads on older game builds.
#
# Usage: tools/check_api_compat.sh [ref-version ...]
# Default versions: the latest known 1.7 reference package.
set -euo pipefail

repo="$(cd "$(dirname "$0")/.." && pwd)"
versions=("$@")
if [ ${#versions[@]} -eq 0 ]; then
  versions=("1.7.4985-unstable")
fi

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

status=0
for version in "${versions[@]}"; do
  rm -rf "$work/Petitions"
  cp -r "$repo/Source/Petitions" "$work/Petitions"
  rm -rf "$work/Petitions/bin" "$work/Petitions/obj"
  cp "$repo/global.json" "$work/"
  sed -i "s#Include=\"Krafs.Rimworld.Ref\" Version=\"[^\"]*\"#Include=\"Krafs.Rimworld.Ref\" Version=\"$version\"#" "$work/Petitions/Petitions.csproj"
  if dotnet build "$work/Petitions/Petitions.csproj" -c Release -o "$work/out" -p:OutputPath="$work/out/" > "$work/build.log" 2>&1; then
    echo "OK: compiles against Krafs.Rimworld.Ref $version"
  else
    echo "FAIL: Krafs.Rimworld.Ref $version"
    grep -E "error" "$work/build.log" | sort -u | head -20
    status=1
  fi
done
exit $status
