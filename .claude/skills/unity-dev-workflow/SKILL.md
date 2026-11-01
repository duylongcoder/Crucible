---
name: unity-dev-workflow
description: Quy trình implement, test, refactor và review code C# trong dự án Unity theo hướng test-first, refactor an toàn và "thay đổi nhỏ nhất đủ dùng" (Ponytail). Dùng khi được giao task Unity/C#: thêm tính năng, sửa bug, viết hoặc sửa test EditMode/PlayMode, refactor MonoBehaviour/ScriptableObject, đổi tên/di chuyển script, review code Unity, hoặc liệt kê các shortcut đã hoãn.
argument-hint: "[lite|full|ultra] <mô tả task>"
---

# Unity Dev Workflow

Skill độc lập, rút gọn từ `dev-story`, `code-review`, `test-setup`, `test-helpers`,
agent `unity-specialist` của
[claude-code-game-studios](https://github.com/donchitos/claude-code-game-studios)
(MIT), và lấy [Ponytail](https://github.com/DietrichGebert/ponytail) (MIT) làm
**nguyên tắc mặc định**. Không phụ thuộc studio, hooks hay plugin nào.

## Nguyên tắc mặc định — Ponytail (luôn bật, mọi chế độ)

Làm như một senior dev lười: code tốt nhất là code không phải viết. Giải quyết **trọn
vẹn** vấn đề với ít code mới nhất. Lười về lời giải, không bao giờ lười về thay đổi.

**Phạm vi trước khi viết**: liệt kê mọi chỗ thay đổi phải chạm tới — chỗ gọi, test,
prefab/scene/asset tham chiếu, asmdef, serialized data. Kiểm tra cái gì có thể vỡ: dữ
liệu đã gán trong Inspector, save file người chơi, chỗ gọi khác. Đó là phạm vi; tính
năng thêm thì không.

**Lấy phương án đầu tiên chạy được trọn vẹn**, theo thứ tự:

1. Có cần tồn tại không? Bỏ tính năng/option/độ linh hoạt không ai yêu cầu, ghi tên
   trong một dòng. Yêu cầu mơ hồ → bản nhỏ nhất làm được việc cốt lõi.
2. Codebase đã có chưa (helper, component, service, event channel, pattern)? Dùng như
   code xung quanh đang dùng.
3. Unity/.NET đã có chưa (`ObjectPool<T>`, `Mathf`, `Physics.*NonAlloc`, `JsonUtility`,
   UnityEvent...)? Dùng, trừ khi dự án có bản riêng — bản của dự án thắng.
4. Package đã cài có chưa? Dùng. Không thêm package cho vài dòng code.
5. Một dòng đọc là hiểu? Một dòng.
6. Còn lại: code tối thiểu chạy được.

**Luật**:
- Không abstraction, wrapper, interface, option, config, boilerplate hay code "để sau"
  khi không ai yêu cầu. Giữ cấu trúc, layer, quy ước sẵn có. Xóa thắng thêm.
- Diff ngắn nhất thắng — sau khi đã biết mọi chỗ nó phải chạm. One-liner phải giải mã
  không phải là ngắn.
- Comment chỉ cho cái "tại sao" mà code không nói được, một dòng.
- Sửa bug: grep mọi chỗ gọi hàm sắp sửa, sửa nguyên nhân gốc một lần ở code dùng chung.
- Code di chuyển/gộp giữ nguyên error handling và validation.
- Hai phương án cùng cỡ: chọn cái đúng ở trường hợp biên.
- Shortcut có giới hạn biết trước → comment `// shortcut: <giới hạn>, <khi nào nâng cấp>`.

**Không bao giờ cắt**: test theo mục "Test" (quy tắc test thắng Ponytail), validation
ở biên tin cậy (input người chơi, file save, network), error handling chống mất dữ
liệu, bảo mật, accessibility, `[FormerlySerializedAs]` / `.meta` / dữ liệu serialized,
hủy event/coroutine/async, bất cứ thứ gì người dùng yêu cầu.

**Áp dụng vào Unity khi có xung đột**:
- *Tách logic thuần C# khỏi MonoBehaviour* chỉ khi cần test hoặc logic đủ phức tạp; một
  class thuần, không thêm interface trừ khi cần để inject fake trong test.
- *Magic number*: mặc định `const`. Chỉ đưa vào `[SerializeField]`/ScriptableObject khi
  designer cần tinh chỉnh hoặc dự án đã có config SO cho hệ thống đó.
- *Pooling, cache, NonAlloc*: chỉ ở hot path thực sự (mỗi frame, nhiều instance) hoặc
  khi profiler chỉ ra; không tối ưu sớm ở code chạy một lần.

**Mức độ** (người dùng ghi ở đầu yêu cầu, mặc định `full`):

| Mức | Hành vi |
|---|---|
| `lite` | Làm đúng thứ được yêu cầu; nêu phương án nhỏ hơn trong một dòng để người dùng chọn |
| `full` | Các luật trên |
| `ultra` | Thêm: chất vấn yêu cầu trước khi làm, phản biện phần nào nhu cầu không biện minh được |

Người dùng nói "tắt ponytail" / "normal mode" → bỏ phần này cho đến hết phiên, các phần
khác của skill vẫn giữ.

## Chọn chế độ

| Yêu cầu | Chế độ | Đọc thêm |
|---|---|---|
| Thêm tính năng, sửa bug | **Implement** | `references/testing.md` |
| Viết / sửa / chống flaky test | **Test** | `references/testing.md` |
| Đổi cấu trúc, giữ nguyên hành vi | **Refactor** | `references/refactor.md`, `references/testing.md` |
| Review file / diff / PR / "có over-engineer không" | **Review** | `references/code-review.md` |
| Liệt kê shortcut đã hoãn | **Debt** | (mục Debt bên dưới) |
| Câu hỏi API, version, pattern Unity | (bất kỳ) | `references/unity-pitfalls.md`, `references/engine/` |

## Bước 0 — Nắm bối cảnh (mọi chế độ)

Không đoán khi chưa đọc code. Trước khi sửa:

1. **Version Unity**: đọc `ProjectSettings/ProjectVersion.txt`. So với
   `references/engine/VERSION.md` (pin Unity 6.3 LTS). Dự án thấp hơn → API trong
   `references/engine/` có thể chưa tồn tại; nói rõ và đánh dấu "chưa xác minh".
2. **Packages**: `Packages/manifest.json` — test-framework, Input System, Addressables,
   DI (Zenject/VContainer), UniTask...
3. **Assembly**: `Glob **/*.asmdef` dưới `Assets/`. Code ở assembly nào, test assembly
   nào reference nó.
4. **Pattern sẵn có**: đọc 2–3 file cùng hệ thống + test hiện có (`Assets/Tests/**`).
   Tìm helper/pattern có thể tái dùng trước khi viết mới (Ponytail bước 2).
5. **Ai dùng code này**: chỗ gọi trong code, và tham chiếu trong scene/prefab/asset qua
   GUID (`scripts/find-guid-refs.sh <file.cs>`). Grep tên class là chưa đủ.

Yêu cầu mơ hồ, hoặc chạm kiến trúc / input / save-load / nhiều hệ thống: dừng, đề xuất
2–3 hướng kèm ưu nhược (phương án nhỏ nhất đứng đầu), chọn một, chờ xác nhận.

## Implement

1. **Test trước.** Bug → test tái hiện, chạy, xác nhận **fail đúng lý do**. Tính năng →
   test cho từng hành vi/điều kiện chấp nhận.
2. **Thay đổi nhỏ nhất** làm test xanh, theo thang Ponytail. Không sửa thứ không liên
   quan; vấn đề khác ghi vào báo cáo.
3. **Tự rà** (chi tiết `references/unity-pitfalls.md`):
   - Không `Find*` / `GetComponent` trong `Update`/`FixedUpdate`/`LateUpdate`; cache ở `Awake`.
   - Không allocation trong hot path (string concat, LINQ, boxing, `new List<>` mỗi frame, closure).
   - Event: `OnEnable`↔`OnDisable`, `Awake/Start`↔`OnDestroy`.
   - Coroutine/async hủy khi disable/destroy (`destroyCancellationToken`).
   - `UnityEngine.Object`: dùng `== null`, cẩn thận `?.` / `??` / `is null`.
   - `[SerializeField] private`; đổi tên field serialized → `[FormerlySerializedAs]`.
   - Không `try/catch` nuốt exception; dùng `Debug.LogException`.
   - Rà lần cuối: dòng nào xóa được mà test vẫn xanh? Xóa.
4. **Chạy test + compile** (xem "Chạy test"). Sửa đến khi xanh.
5. **Báo cáo** (xem "Báo cáo cuối").

## Test

Theo `references/testing.md`. Quy tắc test **không** bị Ponytail rút gọn:

- Bug luôn có test tái hiện. Logic không tầm thường (nhánh, vòng lặp, parser, tính
  điểm/tiền, random, save/load) luôn có test. Thay đổi tầm thường (đổi text, đổi
  hằng số hiển thị) không cần test mới.
- Ponytail áp dụng *bên trong* test: không framework mock, không helper khi chưa lặp ≥2
  lần, mỗi test một khái niệm, phủ biên bằng `[TestCase]` thay vì copy test.
- EditMode cho logic thuần; PlayMode chỉ khi cần lifecycle, physics, coroutine, scene.
- Tên `Method_Condition_ExpectedResult`. Arrange–Act–Assert. Test hành vi qua public API.
- Xác định: inject `System.Random(seed)`, truyền `deltaTime`/clock giả, không phụ thuộc
  thứ tự chạy.
- PlayMode: chờ **điều kiện có timeout**. `[TearDown]` destroy mọi object tạo ra.
- Biên: 0, âm, vượt ngưỡng, rỗng, object đã destroy, input nhanh liên tiếp.
- Không xóa / `[Ignore]` test fail để qua build. Flaky → tìm nguyên nhân gốc.

## Refactor

Theo `references/refactor.md`. Luật cứng:

1. Có test bao phủ trước. Chưa có → **characterization test** ghi lại hành vi hiện tại.
2. Refactor và thay đổi tính năng **không trộn** trong một bước/commit.
3. Bước nhỏ, chạy test sau mỗi bước. Fail → dừng, tìm nguyên nhân.
4. Đổi tên/di chuyển script đi cùng `.meta`. Tên class MonoBehaviour = tên file.
5. Giữ public API và contract. Buộc phải đổi → liệt kê mọi điểm bị ảnh hưởng.
6. Ưu tiên refactor **xóa** code: dead code, option không ai set, abstraction một
   implementation, bản gần trùng phải đổi cùng nhau. Không refactor để "đẹp hơn" khi
   không giảm code hay rủi ro.
7. Thay đổi lớn → kế hoạch từng bước, chờ xác nhận.

## Review

Theo `references/code-review.md`. Thứ tự ưu tiên: đúng → an toàn → chịu tải → có
test → nhanh → gọn. Mỗi finding phải có file:line, trường hợp cụ thể gây lỗi, và
`VERIFIED`/`UNVERIFIED`. Không có trường hợp cụ thể → không phải finding. Đề xuất cách
sửa nhỏ nhất, ưu tiên cách xóa code. Review chỉ báo cáo, không sửa code.

## Debt

Liệt kê mọi comment `shortcut:` (và `ponytail:` cũ) thành sổ nợ. Chỉ đọc.

```bash
git grep -nE '(//|/[*]) ?(shortcut|ponytail):' -- 'Assets/**/*.cs' 'Packages/**/*.cs'
```

Mỗi dòng: `<file>:<line> — <đơn giản hóa gì>. giới hạn: <...>. nâng cấp khi: <...>.`
Comment không nêu điều kiện nâng cấp → gắn `no-trigger`. Kết thúc bằng
`<N> shortcut, <M> không có trigger.` Người dùng muốn lưu → hỏi trước khi ghi file.

## Chạy test

```bash
UNITY_EDITOR=/path/to/Unity bash scripts/run-unity-tests.sh EditMode [testFilter]
UNITY_EDITOR=/path/to/Unity bash scripts/run-unity-tests.sh PlayMode [testFilter]
```

Lưu ý quan sát trên Unity 6000.3 (`references/engine/current-best-practices.md`):

- `-runTests` tự thoát, **không cần `-quit`**. Exit 0 pass, 2 có test fail, 1 lỗi
  compile (không có file kết quả), 3 run lỗi.
- Test ngoài `Assets/` không được compile → 0 test, exit 0: **false pass**.
- Thiếu `com.unity.test-framework` → `-runTests` treo.
- `Unity` trên PATH có thể là Unity CLI — dùng đường dẫn đầy đủ tới Editor.
- Editor đang mở project → lỗi lock. Hỏi người dùng đóng Editor.

Không chạy được → nói rõ lý do và đưa lệnh chính xác. Không báo "đã xanh" khi chưa chạy.

## Báo cáo cuối

Ngắn, người bận đọc một lần là hiểu:

- **Đã đổi gì**: file + một dòng mỗi file.
- **Test**: test mới/đã sửa; kết quả chạy (pass/fail) hoặc lý do chưa chạy.
- **Kiểm tra thủ công trong Editor** (nếu có): prefab, inspector, animation, UI.
- Một đến hai dòng cuối: **đã bỏ qua / chưa kiểm tra gì**, và **rủi ro** người dùng
  phải biết (serialized data, tham chiếu prefab/scene, public API, shortcut mới thêm).
- Commit message gợi ý: `<type>(<scope>): <mô tả>`. Không commit khi test fail, không push.

## Không được làm

- Sửa tay YAML `.unity` / `.prefab` / `.asset` khi Editor có thể đang mở project.
  Không có Editor: giữ nguyên mọi `fileID`/`guid`, không bịa GUID, không sửa `.meta`.
- Thêm package UPM / Asset Store khi chưa hỏi.
- Sửa `Library/`, `Temp/`, `Logs/`, `obj/`, `.meta` của asset bên thứ ba, code sinh tự động.
- Hard-code secret.
- Lệnh phá hủy (xóa asset, reset, force push) khi chưa xác nhận.
