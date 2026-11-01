# Refactor an toàn trong Unity

## Mục lục
1. Quy trình
2. Kỹ thuật an toàn (ưu tiên)
3. Đổi tên / di chuyển / xóa script
4. Field serialized
5. Tách logic thuần C# khỏi MonoBehaviour
6. Khi nào phải dừng và hỏi

---

## 1. Quy trình

1. **Xác định phạm vi**: liệt kê file sẽ đổi. Ngoài phạm vi → ghi vào báo cáo, không sửa.
2. **Lưới an toàn**: chạy test hiện có. Phần sắp đổi chưa có test → viết
   characterization test: gọi public API với vài input đại diện + biên, assert
   đúng output **hiện tại**. Hành vi trông như bug → vẫn ghi lại, comment
   `// Characterization: hành vi hiện tại, có thể là bug — xem báo cáo`.
3. **Bước nhỏ**: mỗi bước một kỹ thuật, compile + test xanh trước bước tiếp theo.
4. **Không trộn** thay đổi hành vi. Nếu phát hiện bug cần sửa: hoàn tất refactor
   (commit), rồi sửa bug thành bước riêng có test fail trước.
5. Commit theo bước: `refactor(<scope>): <mô tả>`.

## 2. Kỹ thuật an toàn (ưu tiên)

| Kỹ thuật | Rủi ro Unity cần để ý |
|---|---|
| Đổi tên local/private method | Thấp. Cẩn thận method được gọi qua string: `Invoke("X")`, `SendMessage`, `StartCoroutine("X")`, UnityEvent gán trong Inspector, Animation Event |
| Trích xuất method | Thấp |
| Guard clause thay if lồng | Thấp |
| Magic number → `const` (mặc định); config ScriptableObject chỉ khi designer cần chỉnh hoặc hệ thống đã có SO | Trung bình nếu chuyển sang serialized: giá trị mặc định phải bằng giá trị cũ, và asset/prefab đã có cần được gán |
| Xóa dead code, option không ai set, abstraction một implementation | Thấp nếu đã tìm tham chiếu (code + GUID + string/reflection). Ưu tiên cao: xóa thắng thêm |
| Trích xuất logic thuần ra class C# | Thấp–trung bình (xem mục 5) |
| Đổi tên field serialized / public | **Cao** — mất dữ liệu nếu thiếu `[FormerlySerializedAs]` |
| Đổi tên class / file MonoBehaviour, ScriptableObject | **Cao** — mất tham chiếu nếu `.meta` không đi cùng |
| Đổi namespace / assembly | Trung bình — `[MovedFrom]` cho `SerializeReference`; kiểm tra asmdef references |
| Đổi kiểu field serialized | **Cao** — dữ liệu cũ không chuyển được tự động |

## 3. Đổi tên / di chuyển / xóa script

**Đổi tên/di chuyển**:
- Tên class MonoBehaviour/ScriptableObject **phải trùng tên file**.
- Di chuyển cả `X.cs` và `X.cs.meta`: `git mv A.cs B.cs && git mv A.cs.meta B.cs.meta`.
  GUID trong `.meta` giữ nguyên → scene/prefab vẫn trỏ đúng.
- Không bao giờ để Unity tạo `.meta` mới cho script đã có tham chiếu (GUID mới = mọi
  tham chiếu thành "Missing Script").
- Ưu tiên đổi qua Unity Editor khi người dùng có Editor.

**Tìm tham chiếu trước khi xóa/đổi**:
```bash
bash scripts/find-guid-refs.sh Assets/Scripts/Foo.cs
```
Script lấy GUID từ `.meta` rồi tìm trong `*.unity, *.prefab, *.asset, *.controller,
*.anim, *.playable, *.mat, *.overrideController`. Ngoài ra grep tên class trong code
và trong chuỗi (`AddComponent("Foo")`, `Type.GetType`, reflection).

Asset Serialization phải là **Force Text** mới grep được. Nếu file YAML là binary →
báo người dùng, không kết luận "không có tham chiếu".

**Xóa script**: chỉ sau khi tìm tham chiếu = 0 (hoặc người dùng xác nhận gỡ các
tham chiếu trong Editor). Không tự xóa asset.

## 4. Field serialized

```csharp
using UnityEngine.Serialization;

[FormerlySerializedAs("speed")]
[SerializeField] private float _moveSpeed = 5f;
```

- Giữ `[FormerlySerializedAs]` ít nhất cho đến khi mọi scene/prefab đã được Unity
  resave. Xóa sớm → mất dữ liệu ở asset chưa resave.
- Đổi tên nhiều lần: xếp chồng nhiều `[FormerlySerializedAs]`.
- **Đổi kiểu** (vd `int` → `float` có thể tự convert; `GameObject` → component
  không): cần migration — giữ field cũ `[SerializeField, HideInInspector]`, convert trong
  `OnValidate`/`ISerializationCallbackReceiver`, hoặc editor script. Đề xuất, chờ xác nhận.
- **Chuyển field sang component khác**: dữ liệu không tự đi theo. Liệt kê prefab/
  scene cần gán lại → mục "Kiểm tra thủ công trong Editor".
- `[SerializeReference]` + đổi tên class/namespace → `[MovedFrom(...)]`
  (`UnityEngine.Scripting.APIUpdating`).

## 5. Tách logic thuần C# khỏi MonoBehaviour

Trước:
```csharp
public class Inventory : MonoBehaviour
{
    [SerializeField] private int _maxStack = 99;
    private readonly List<Stack> _stacks = new();
    public bool Add(Item item, int count) { /* rule gộp stack phức tạp */ }
}
```

Sau:
```csharp
public sealed class InventoryModel            // thuần C#, test EditMode
{
    private readonly int _maxStack;
    private readonly List<Stack> _stacks = new();
    public InventoryModel(int maxStack) => _maxStack = maxStack;
    public bool Add(Item item, int count) { /* giữ nguyên logic */ }
}

public class Inventory : MonoBehaviour        // public API giữ nguyên
{
    [SerializeField] private int _maxStack = 99;
    private InventoryModel _model;
    private void Awake() => _model = new InventoryModel(_maxStack);
    public bool Add(Item item, int count) => _model.Add(item, count);
}
```

Thứ tự bước:
1. Characterization test qua `Inventory` (PlayMode hoặc EditMode với `AddComponent`).
2. Tạo `InventoryModel`, chép logic, MonoBehaviour delegate xuống. Test cũ phải xanh.
3. Viết test EditMode cho `InventoryModel`.
4. (Tùy chọn, bước riêng) Thu gọn test PlayMode chỉ còn kiểm tra wiring.

Lưu ý thời điểm: logic trước đây dùng được trước `Awake` (vd gọi từ `OnValidate`
hoặc từ object khác trong `Awake`) → `_model` có thể null. Kiểm tra chỗ gọi.

## 6. Khi nào phải dừng và hỏi

- Thay đổi public API được hệ thống khác dùng.
- Đổi kiểu serialized hoặc cần migration dữ liệu.
- Chạm save/load (định dạng file lưu của người chơi).
- Đổi kiến trúc (event system, DI, input, scene loading).
- Cần thêm asmdef mới hoặc đổi references giữa assembly.
- Test characterization lộ ra hành vi mâu thuẫn với yêu cầu.

Đưa kế hoạch từng bước, các điểm bị ảnh hưởng, rủi ro; chờ xác nhận.
