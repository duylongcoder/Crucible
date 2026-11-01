# Unity Pitfalls & Best Practices

Rút từ agent `unity-specialist` của claude-code-game-studios, điều chỉnh: những mục
mang tính "chọn công nghệ" (Addressables, UI Toolkit, Input System, URP) chỉ áp dụng
khi **dự án đã dùng**. Không tự chuyển đổi công nghệ.

## Kiểm tra version trước khi dùng API

Kiến thức huấn luyện có thể chỉ phủ tốt tới khoảng Unity 2022.3. Trước khi đề xuất API:

1. Đọc `ProjectSettings/ProjectVersion.txt`.
2. Unity 6.x → đối chiếu `engine/deprecated-apis.md`, `engine/breaking-changes.md`,
   `engine/current-best-practices.md` (pin 6.3 LTS, xác minh docs tới 2026-10).
3. Dự án < Unity 6 → các file `engine/` có thể **đi trước** version dự án; API ở đó có
   thể chưa tồn tại. Kiểm tra trước khi dùng.
4. API không có trong reference và không chắc → nói rõ "chưa xác minh".

## Lifecycle & reference

- Cache component trong `Awake`; tham chiếu tới object khác lấy trong `Start` (sau khi
  mọi `Awake` đã chạy) hoặc inject qua serialized field.
- `Find*`, `FindObjectOfType`, `SendMessage`: tránh trong code production; tuyệt đối không
  trong hot path. (Unity 2023.1+: `FindObjectOfType` deprecated → `FindFirstObjectByType`
  / `FindAnyObjectByType`.)
- `Camera.main`: rẻ từ 2020.2, nhưng vẫn nên cache nếu gọi mỗi frame trên nhiều object.
- `DontDestroyOnLoad` quá nhiều → khó test, rò trạng thái giữa scene.
- Phụ thuộc Script Execution Order ngầm → khởi tạo tường minh (bootstrapper gọi `Init`).

## Null & destroy

- `UnityEngine.Object` override `==`: object đã `Destroy` so `== null` là `true`, nhưng
  reference C# vẫn khác null.
- `?.`, `??`, `is null`, `is not null`, pattern matching **bỏ qua** override → có thể gọi
  vào object đã destroy (`MissingReferenceException`).
- `Destroy` có hiệu lực cuối frame; trong cùng frame object vẫn "sống". Trong EditMode
  test dùng `DestroyImmediate`.

## Event & async

- `OnEnable`↔`OnDisable`, `Awake/Start`↔`OnDestroy`. Disable rồi enable lại không được
  đăng ký trùng.
- Lambda đăng ký event không hủy được (không có reference) → dùng method.
- Coroutine dừng khi GameObject inactive, **không** dừng khi chỉ disable component.
- `async`/`await`: dùng `destroyCancellationToken` (Unity 2022.2+) hoặc
  `CancellationTokenSource` hủy trong `OnDestroy`/`OnDisable`. Sau `await` kiểm tra
  `this == null` nếu không dùng token. Tránh `async void` (exception bị mất) trừ handler.
- Unity 2023.1+ có `Awaitable`; dự án dùng UniTask thì theo UniTask.

## GC / hot path

- Không trong `Update`/`FixedUpdate`/`LateUpdate`/vòng lặp nóng: string concat/interpolation,
  LINQ, `new List<>`/`new T[]`, lambda capture, boxing, `foreach` trên interface (allocate
  enumerator), `GetComponents` không có list buffer.
- Physics: `RaycastNonAlloc`, `OverlapSphereNonAlloc` với buffer tái dùng.
- `StringBuilder` khi buộc phải dựng chuỗi lặp; UI text cập nhật chỉ khi giá trị đổi.
- Pooling: `UnityEngine.Pool.ObjectPool<T>` (2021.1+) nếu dự án đã dùng; không trộn với
  pool tự viết sẵn có.
- `CompareTag("x")` thay vì `tag == "x"`.
- Kiểm tra cột GC.Alloc trong Profiler khi nghi ngờ.

## Serialization

- `[SerializeField] private` + `[Header]`, `[Tooltip]`, `[Min]`, `[Range]` khi có ích.
- Đổi tên: `[FormerlySerializedAs]`. Đổi tên class với `[SerializeReference]`: `[MovedFrom]`.
- Unity không serialize: property (trừ `[field: SerializeField]` cho auto-property),
  `Dictionary`, interface (trừ `[SerializeReference]`), static, readonly.
- ScriptableObject sửa trong Play Mode ở Editor sẽ **giữ thay đổi** sau khi thoát Play →
  không dùng SO làm runtime state mà không clone/reset.

## Kiến trúc

- Composition hơn kế thừa MonoBehaviour sâu.
- ScriptableObject cho data/config/event channel; MonoBehaviour đọc data.
- Interface nhỏ (`IDamageable`) cho hành vi đa hình.
- Asmdef cho mọi thư mục code để test reference được và compile nhanh hơn.
- UI đọc state, không sở hữu state.
- DOTS/ECS chỉ khi cần hàng nghìn entity và dự án đã chọn hướng đó.

## Scene / Prefab / .meta

- Không sửa tay `.unity`/`.prefab`/`.asset` khi Editor có thể đang mở project — Editor sẽ
  ghi đè hoặc xung đột. Không chắc → coi như đang mở. Hướng dẫn người dùng thao tác trong
  Editor.
- Không có Editor: giữ nguyên mọi `fileID`/`guid`, không bịa GUID, không sửa/xóa `.meta`,
  và nhắc mở scene trong Editor để xác nhận load được.
- Project Settings > Editor: Asset Serialization = Force Text, Version Control = Visible Meta Files.

## Công nghệ (chỉ khi dự án đã dùng)

- Input System: callback `performed`/`canceled` thay vì polling; định nghĩa trong `.inputactions`.
- Addressables thay `Resources.Load`; nhớ `Release` handle.
- UI Toolkit cho runtime UI mới; UGUI cho world-space. Pool phần tử list.
- URP/HDRP custom pass: RenderGraph API (Unity 6). Xem `engine/breaking-changes.md`.
