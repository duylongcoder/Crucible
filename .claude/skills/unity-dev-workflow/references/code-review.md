# Code Review — Unity / C#

## Nguyên tắc

- **Đọc đủ**: file đích đầy đủ, + chỗ gọi, + test liên quan, + asmdef. Review diff thì
  đọc cả ngữ cảnh xung quanh, không chỉ dòng đổi.
- **Không dữ liệu → `NOT ASSESSED`**, không phải PASS. Ví dụ: không có test nào →
  mục Testability là `NOT ASSESSED — không có test`, không phải "OK".
- **Mỗi finding phải kiểm chứng**. Ghi:
  - `file:line`
  - **Bằng chứng**: đoạn code, hoặc input/trạng thái cụ thể gây lỗi
  - **Mức độ**: `BLOCKING` / `WARNING` / `INFO`
  - `VERIFIED` (tự lần theo code / chạy được) hoặc `UNVERIFIED` (nghi ngờ, chưa chứng minh)
- Một bug "nghe hợp lý" nhưng khi liệt kê input thì không xảy ra → bỏ. Không đẩy
  phỏng đoán thành defect.
- Không review style vụn vặt nếu dự án không có quy ước đó. Không "consider...", không lo
  lắng mơ hồ.
- **Thứ tự ưu tiên**: đúng → an toàn → chịu tải → có test → nhanh → gọn. Gọn vẫn quan
  trọng: mỗi dòng thừa phải được đọc, test và sửa sau này.
- **Phạm vi mặc định**: người dùng không chỉ định → thay đổi chưa commit; không có thì
  commit gần nhất. Đổi signature / giá trị trả về / hành vi → grep mọi chỗ gọi, kể cả
  UnityEvent gán trong Inspector và tham chiếu GUID.
- **Tải dự kiến**: game offline một người chơi khác game nhiều entity / multiplayer.
  Đánh giá "chịu tải" theo đó và ghi rõ giả định.
- Comment `shortcut: <giới hạn>, <khi nào nâng cấp>` (hoặc `ponytail:` cũ) là **quyết
  định có chủ đích**, không phải finding — trừ khi tải dự kiến đã vượt giới hạn nó nêu.
- Đề xuất **cách sửa nhỏ nhất**, ưu tiên cách xóa code. Không đề xuất thêm layer,
  framework hay config mà vấn đề không cần.

## Checklist

### Đúng đắn
- [ ] Logic đúng với yêu cầu; nhánh biên (0, rỗng, null, max) được xử lý
- [ ] `UnityEngine.Object` dùng `== null`, không `is null` / `?.` / `??` khi object có thể đã destroy
- [ ] Event: đăng ký/hủy cân xứng (`OnEnable`↔`OnDisable`, `Awake`↔`OnDestroy`); không đăng ký 2 lần
- [ ] Coroutine/async: hủy khi disable/destroy; không `async void` trừ event handler; exception không bị nuốt
- [ ] Thứ tự khởi tạo: không dựa vào `Awake` của object khác đã chạy mà không đảm bảo
- [ ] Frame-rate independence: chuyển động/timer dùng `deltaTime` (hoặc `fixedDeltaTime` trong `FixedUpdate`)
- [ ] Không `try/catch` rỗng hoặc chỉ return

### Hiệu năng (hot path: `Update`, `FixedUpdate`, `LateUpdate`, physics callback, vòng lặp theo entity)
- [ ] Không `Find*`, `GetComponent`, `Camera.main` (trước 2020.2) lặp mỗi frame
- [ ] Không allocation: string concat/interpolation, LINQ, `new List<>`, closure/lambda capture, boxing (enum làm key `Dictionary` không có comparer, `params object[]`)
- [ ] Physics query dùng biến thể NonAlloc / buffer tái dùng
- [ ] `Update` rỗng hoặc chỉ check flag → event-driven hoặc tắt component
- [ ] Object sinh/hủy liên tục → pooling (`UnityEngine.Pool.ObjectPool<T>` nếu dự án dùng)

### Serialization & asset
- [ ] `[SerializeField] private` thay vì public field
- [ ] Đổi tên field serialized có `[FormerlySerializedAs]`
- [ ] Đổi tên/di chuyển script đi kèm `.meta`; tên class = tên file
- [ ] Không có thay đổi tay vào `.unity`/`.prefab`/`.asset` (hoặc nếu có, GUID/fileID còn nguyên)
- [ ] Giá trị cấu hình không hard-code trong code gameplay (const hoặc ScriptableObject)

### Kiến trúc
- [ ] Theo pattern sẵn có của dự án (event channel, DI, cách tổ chức asmdef)
- [ ] Hướng phụ thuộc đúng: UI/view → gameplay → core/logic thuần; không ngược
- [ ] Không phụ thuộc vòng giữa assembly/hệ thống
- [ ] UI không sở hữu game state
- [ ] Logic có thể tách khỏi MonoBehaviour đã được tách (testable ở EditMode)
- [ ] Không thêm singleton static cho game state mới (trừ khi dự án đã dùng pattern đó)
- [ ] Không có package/dependency mới chưa được duyệt

### Testability
- [ ] Có test cho hành vi mới / bug đã sửa (bug có test tái hiện)
- [ ] Phụ thuộc thời gian/random được inject
- [ ] Test theo hành vi, không phụ thuộc chi tiết nội bộ
- [ ] PlayMode test dọn dẹp GameObject trong `[TearDown]`
- [ ] Không có test bị `[Ignore]` / xóa để qua build

### Gọn (Lean) — code không nên tồn tại hoặc nên nhỏ hơn
- [ ] **delete**: dead code, option/serialized field không ai set, tính năng "để sau"
- [ ] **reuse**: dự án đã có helper/component này (nêu đường dẫn)
- [ ] **native**: Unity/.NET đã có sẵn (`ObjectPool<T>`, `Mathf`, `JsonUtility`...); package mới cho vài dòng
- [ ] **yagni**: interface/abstract class chỉ một implementation (không phải để inject fake trong test), config SO không ai chỉnh
- [ ] **merge**: các bản gần trùng phải sửa cùng nhau
- [ ] **split**: một method/MonoBehaviour làm nhiều việc không liên quan → tách theo việc, không theo số dòng

### Chịu tải
- [ ] Danh sách/cache chỉ tăng không giảm (subscriber không hủy, pool không trả, dictionary không xóa)
- [ ] O(n²) trên tập lớn (vòng lặp lồng trên mọi entity, `List.Contains` trong vòng lặp)
- [ ] Mỗi instance làm cùng một việc đáng lẽ làm một lần (mỗi enemy tự `Find` player, tự load config)

### API & version
- [ ] API dùng có trong version Unity của dự án (`ProjectSettings/ProjectVersion.txt`);
      đối chiếu `references/engine/deprecated-apis.md` và `breaking-changes.md`
- [ ] API không chắc → đánh dấu `UNVERIFIED`, không khẳng định từ trí nhớ

## Định dạng output

Viết câu ngắn, từ thông dụng; giải thích thuật ngữ lần đầu dùng nếu không hiển nhiên.
Người đọc có thể chưa từng thấy đoạn code này.

```
## Review: <file/PR/commit>

**Thay đổi này làm gì**: <2–3 câu>
**Unity version**: <từ ProjectVersion.txt hoặc "không xác định">
**Tải giả định**: <vd: game offline 1 người chơi, ~50 object trên board>

### Phải sửa   (bug, mất dữ liệu, bảo mật, vỡ ở tải dự kiến, mất dữ liệu serialized)
1. **<tên ngắn của vấn đề>** (`Assets/Scripts/Combat/Attack.cs:42-50`) — VERIFIED
   - **Đoạn này làm gì**: <1 câu>
   - **Vấn đề**: <trường hợp cụ thể → kết quả sai>
   - **Sửa**: <cách nhỏ nhất, có code nếu ngắn>
   - **Nếu bỏ qua**: <hậu quả với người chơi / dev>

### Nên sửa    (logic rủi ro thiếu test, chậm thực sự, trùng lặp, method lẫn nhiều việc, code thừa)
2. ...

### Có thì tốt (tối ưu nhỏ, dạng ngắn hơn)
3. ...

### Không đánh giá được
- <mục> — NOT ASSESSED: <thiếu gì>

Kết luận: Ship. | Kết luận: sửa 1 và 3 trước.
Lean: -<N> dòng có thể bỏ.          (chỉ khi có finding Lean)
Chưa kiểm tra: <một dòng, nếu có thứ quan trọng không kiểm tra được>
```

- Đánh số liên tục qua các nhóm để người dùng nói "sửa 2 và 5". Bỏ nhóm rỗng.
- "Phải sửa" chỉ chứa finding `VERIFIED`. Finding `UNVERIFIED` đưa xuống "Nên sửa" kèm
  việc cần làm để xác minh.
- Không có gì: "Thay đổi này làm gì", rồi `Ổn. Ship.` và một dòng đã kiểm tra những gì.
- Không đọc được file đích / thiếu toàn bộ đầu vào → `NOT ASSESSED`, nêu thiếu gì.
- Review chỉ liệt kê, không sửa code.
