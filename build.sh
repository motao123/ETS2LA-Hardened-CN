#!/usr/bin/env bash
set -euo pipefail

configuration=Release
test_only=false
publish=false
while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration) configuration="$2"; shift 2;;
    --test) test_only=true; shift;;
    --publish) publish=true; shift;;
    *) echo "未知参数：$1" >&2; exit 2;;
  esac
done

root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
dotnet_cmd="${DOTNET:-dotnet}"
for required in "TruckLib/TruckLib/TruckLib.csproj" "Assets" "ETS2LA.Game/libs/libdeflate.dll"; do
  [[ -e "$root/$required" ]] || { echo "缺少构建输入：$required，请先初始化固定 TruckLib 和资产。" >&2; exit 1; }
done

cd "$root"
"$dotnet_cmd" restore ETS2LA.sln --locked-mode
"$dotnet_cmd" build ETS2LA.sln -c "$configuration" --no-restore
if [[ "$test_only" == true ]]; then
  "$dotnet_cmd" test tests/ETS2LA.Hardened.Tests/ETS2LA.Hardened.Tests.csproj -c "$configuration" --no-restore
fi
if [[ "$publish" == true ]]; then
  "$dotnet_cmd" restore ETS2LA/ETS2LA.csproj -r linux-x64 --force-evaluate
  "$dotnet_cmd" publish ETS2LA/ETS2LA.csproj -c "$configuration" -r linux-x64 --self-contained true -o publish/linux-x64 --no-restore
  cp -r Assets publish/linux-x64/
  ./publish/linux-x64/ETS2LA --smoke-test
fi
