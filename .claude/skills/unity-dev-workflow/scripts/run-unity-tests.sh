#!/usr/bin/env bash
# Chạy Unity Test Framework ở batchmode và tóm tắt kết quả NUnit XML.
#
# Usage:
#   UNITY_EDITOR=/path/to/Unity bash run-unity-tests.sh <EditMode|PlayMode> [testFilter] [project_root]
#
# Exit code: 0 pass | 2 có test fail | 1 lỗi compile/không có kết quả
#            3 run lỗi | 10 Editor đang mở project | 11 thiếu test framework | 12 0 test (false pass)
set -uo pipefail

platform="${1:?Usage: run-unity-tests.sh <EditMode|PlayMode> [testFilter] [project_root]}"
filter="${2:-}"
root="$(cd "${3:-.}" && pwd)"

case "$platform" in EditMode|PlayMode) ;; *)
  echo "ERROR: platform phải là EditMode hoặc PlayMode." >&2; exit 3 ;;
esac

editor="${UNITY_EDITOR:-}"
if [[ -z "$editor" ]]; then
  echo "ERROR: đặt UNITY_EDITOR = đường dẫn đầy đủ tới Unity Editor executable." >&2
  echo "       ('Unity' trên PATH có thể là Unity CLI, không phải Editor.)" >&2
  if [[ -f "$root/ProjectSettings/ProjectVersion.txt" ]]; then
    echo "       Project version: $(grep m_EditorVersion: "$root/ProjectSettings/ProjectVersion.txt" | awk '{print $2}')" >&2
  fi
  exit 3
fi

# Editor đang mở project -> batchmode lỗi lock.
if [[ -f "$root/Temp/UnityLockfile" ]]; then
  echo "BLOCKED: có Temp/UnityLockfile — Unity Editor có thể đang mở project này. Đóng Editor rồi chạy lại." >&2
  exit 10
fi

# Thiếu test framework -> -runTests treo.
if ! grep -q '"com.unity.test-framework"' "$root/Packages/manifest.json" 2>/dev/null; then
  echo "BLOCKED: Packages/manifest.json không có com.unity.test-framework (-runTests sẽ treo)." >&2
  exit 11
fi

mkdir -p "$root/results"
results="$root/results/${platform,,}.xml"
log="$root/results/${platform,,}.log"
rm -f "$results"

args=(-batchmode -nographics -projectPath "$root" -runTests -testPlatform "$platform"
      -testResults "$results" -logFile "$log")
[[ -n "$filter" ]] && args+=(-testFilter "$filter")

echo "Running: $editor ${args[*]}"
"$editor" "${args[@]}"
code=$?

if [[ ! -f "$results" ]]; then
  echo "FAIL: không có file kết quả (exit $code). Thường là lỗi compile — xem $log:" >&2
  grep -E "error CS[0-9]+" "$log" 2>/dev/null | sort -u | head -20 >&2
  exit 1
fi

# Tóm tắt từ <test-run ...>
run_line="$(grep -m1 -o '<test-run[^>]*>' "$results")"
attr() { printf '%s' "$run_line" | grep -o "$1=\"[^\"]*\"" | head -1 | cut -d'"' -f2; }
total="$(attr testcasecount)"; passed="$(attr passed)"; failed="$(attr failed)"
skipped="$(attr skipped)"; result="$(attr result)"

echo "=== $platform: result=$result total=$total passed=$passed failed=$failed skipped=$skipped (exit $code)"

if [[ "${total:-0}" == "0" ]]; then
  echo "WARNING: 0 test chạy — false pass. Kiểm tra test nằm dưới Assets/, asmdef đúng, filter đúng." >&2
  exit 12
fi

if [[ "${failed:-0}" != "0" ]]; then
  echo "--- Failed tests:"
  # In fullname của test-case có result="Failed"
  grep -o '<test-case [^>]*result="Failed"[^>]*>' "$results" \
    | grep -o 'fullname="[^"]*"' | cut -d'"' -f2 | sed 's/^/  - /'
  echo "Chi tiết message/stack trace: $results"
  exit 2
fi

exit "$code"
