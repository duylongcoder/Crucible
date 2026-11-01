#!/usr/bin/env bash
# Tìm mọi asset (scene, prefab, ScriptableObject, ...) tham chiếu tới một file
# qua GUID trong .meta của nó. Chỉ đọc, không sửa gì.
#
# Usage: bash find-guid-refs.sh <path/to/File.cs> [project_root]
# Output: danh sách file:line chứa GUID; tổng số; cảnh báo nếu có YAML binary.
set -euo pipefail

target="${1:?Usage: find-guid-refs.sh <asset-path> [project_root]}"
root="${2:-.}"
meta="${target}.meta"

if [[ ! -f "$meta" ]]; then
  echo "ERROR: không thấy $meta — không lấy được GUID." >&2
  exit 1
fi

guid="$(grep -m1 '^guid:' "$meta" | awk '{print $2}')"
if [[ -z "$guid" ]]; then
  echo "ERROR: $meta không có dòng guid." >&2
  exit 1
fi

echo "GUID: $guid  ($target)"
echo "---"

exts=(unity prefab asset controller overrideController anim playable mat
      mask preset lighting signal spriteatlas spriteatlasv2 uxml uss)
include_args=()
for e in "${exts[@]}"; do include_args+=(--include="*.${e}"); done

search_dirs=()
for d in Assets Packages; do [[ -d "$root/$d" ]] && search_dirs+=("$root/$d"); done
if [[ ${#search_dirs[@]} -eq 0 ]]; then
  echo "ERROR: không thấy Assets/ hoặc Packages/ dưới $root" >&2
  exit 1
fi

matches="$(grep -rn -F "$guid" "${include_args[@]}" "${search_dirs[@]}" 2>/dev/null || true)"
if [[ -n "$matches" ]]; then
  echo "$matches"
  echo "---"
  echo "Số dòng tham chiếu: $(printf '%s\n' "$matches" | wc -l)"
  echo "Số file: $(printf '%s\n' "$matches" | cut -d: -f1 | sort -u | wc -l)"
else
  echo "Không tìm thấy tham chiếu GUID trong asset text."
fi

# Cảnh báo: asset binary không grep được -> kết quả rỗng không có nghĩa là không có tham chiếu.
binary_count=0
while IFS= read -r -d '' f; do
  head -c 5 "$f" | grep -q '%YAML' || binary_count=$((binary_count + 1))
done < <(find "${search_dirs[@]}" \( -name '*.unity' -o -name '*.prefab' -o -name '*.asset' \) -print0 2>/dev/null | head -z -n 500)

if [[ $binary_count -gt 0 ]]; then
  echo "WARNING: $binary_count scene/prefab/asset không phải YAML text (trong 500 file đầu)."
  echo "         Asset Serialization có thể không phải Force Text — kết quả có thể thiếu."
fi

echo "Nhắc: còn phải grep tên class trong code và chuỗi (AddComponent(\"X\"), Type.GetType, reflection)."
