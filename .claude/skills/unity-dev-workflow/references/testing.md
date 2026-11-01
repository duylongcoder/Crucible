# Testing — Unity Test Framework (NUnit)

## Mục lục
1. Chọn EditMode hay PlayMode
2. Cấu trúc thư mục và asmdef
3. Mẫu EditMode
4. Mẫu PlayMode
5. Tính xác định (determinism)
6. Seam để inject phụ thuộc
7. Helper dùng chung
8. Checklist trường hợp biên
9. Chẩn đoán flaky test

---

## 1. Chọn EditMode hay PlayMode

| Cần gì | Loại |
|---|---|
| Tính toán, rule, state machine, parser, data transform | EditMode, class thuần C# |
| ScriptableObject logic (không cần play loop) | EditMode, `ScriptableObject.CreateInstance<T>()` |
| `Awake/Start/OnEnable` thật, `Update` qua nhiều frame | PlayMode |
| Physics, collision, trigger, `FixedUpdate` | PlayMode |
| Coroutine, `async` gắn lifecycle | PlayMode |
| Load scene, Addressables | PlayMode (chậm — giữ ít) |

Nếu đang định viết PlayMode cho logic có thể tách ra → tách logic trước, test EditMode.
PlayMode nên là lớp mỏng kiểm tra "wiring".

## 2. Cấu trúc thư mục và asmdef

```
Assets/
  Scripts/<Feature>/        -> <Game>.<Feature>.asmdef (hoặc assembly chung của dự án)
  Tests/
    EditMode/
      EditModeTests.asmdef  -> includePlatforms: ["Editor"],
                               references: [assembly cần test],
                               optionalUnityReferences / overrideReferences: nunit.framework.dll,
                               defineConstraints: ["UNITY_INCLUDE_TESTS"]
      Helpers/
    PlayMode/
      PlayModeTests.asmdef  -> includePlatforms: [] (mọi platform), cùng references
```

- Test **phải** nằm dưới `Assets/` (hoặc package có `testables`), nếu không sẽ không
  được compile và run báo 0 test → false pass.
- Code trong `Assembly-CSharp` mặc định (không có asmdef) **không reference được** từ
  test asmdef. Khi đó cần đề xuất thêm asmdef cho code — hỏi trước, vì ảnh hưởng toàn
  dự án.
- Trước khi tạo asmdef mới, kiểm tra dự án đã có cấu trúc test riêng chưa và theo nó.

## 3. Mẫu EditMode

```csharp
using NUnit.Framework;

public class HealthTests
{
    private Health _health;
    private int _deathCount;

    [SetUp]
    public void SetUp()
    {
        _health = new Health(maxHealth: 100);
        _deathCount = 0;
        _health.Died += () => _deathCount++;
    }

    [Test]
    public void TakeDamage_WhenHealthReachesZero_RaisesDiedOnce()
    {
        _health.TakeDamage(100);
        _health.TakeDamage(10);

        Assert.That(_deathCount, Is.EqualTo(1));
    }

    [TestCase(0)]
    [TestCase(-5)]
    public void TakeDamage_WithNonPositiveAmount_DoesNotChangeHealth(int amount)
    {
        _health.TakeDamage(amount);

        Assert.That(_health.Current, Is.EqualTo(100));
    }
}
```

- Ưu tiên constraint model `Assert.That(x, Is...)` — message lỗi rõ hơn.
- `[TestCase]` cho bảng giá trị biên thay vì copy test.
- Với `ScriptableObject` tạo trong test: `Object.DestroyImmediate(so)` ở `[TearDown]`.

## 4. Mẫu PlayMode

```csharp
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class DoorPlayModeTests
{
    private GameObject _root;

    [SetUp]
    public void SetUp() => _root = new GameObject("TestRoot");

    [TearDown]
    public void TearDown()
    {
        if (_root != null) Object.Destroy(_root);
    }

    [UnityTest]
    public IEnumerator Open_WhenTriggered_ReachesOpenStateWithinTimeout()
    {
        var door = new GameObject("Door").AddComponent<Door>();
        door.transform.SetParent(_root.transform);
        yield return null; // để Awake/Start chạy

        door.Open();

        yield return WaitUntilOrTimeout(() => door.IsOpen, timeoutSeconds: 2f);
        Assert.That(door.IsOpen, Is.True);
    }

    // Chờ điều kiện, không chờ thời gian cố định.
    private static IEnumerator WaitUntilOrTimeout(Func<bool> condition, float timeoutSeconds)
    {
        var deadline = Time.realtimeSinceStartup + timeoutSeconds;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > deadline)
                Assert.Fail($"Condition not met within {timeoutSeconds}s");
            yield return null;
        }
    }
}
```

- Mọi object tạo ra là con của `_root` → một lệnh destroy dọn hết.
- Physics: `yield return new WaitForFixedUpdate()` để qua một bước physics.
- `LogAssert.Expect(LogType.Error, ...)` khi test cố ý gây log lỗi; log lỗi không
  được mong đợi sẽ làm test fail (mặc định của Test Framework).
- Không dùng `Thread.Sleep`.

## 5. Tính xác định

| Nguồn không xác định | Cách xử lý |
|---|---|
| `UnityEngine.Random` / `new System.Random()` | Inject `System.Random` hoặc interface `IRandom` có seed |
| `Time.time`, `Time.deltaTime` | Truyền `deltaTime` vào method logic, hoặc interface `IClock` |
| `DateTime.Now` | Interface `IClock` / `TimeProvider` |
| Thứ tự `Dictionary`/`HashSet` | Không assert theo thứ tự; sort trước khi so |
| Thứ tự chạy test | Mỗi test tự `SetUp` đủ; không dùng static state |
| Static / singleton | Reset trong `[SetUp]`, hoặc tốt hơn: bỏ static, inject |

Khi test logic có random: assert trên **seed cố định cho ra kết quả cố định**, và
một test riêng kiểm tra cùng seed → cùng kết quả (replay).

## 6. Seam để inject phụ thuộc

Theo thứ tự ưu tiên (đơn giản trước):

1. **Tham số method**: `Tick(float deltaTime)` thay vì đọc `Time.deltaTime` bên trong.
2. **Constructor của class thuần**: `new Spawner(IRandom rng, SpawnConfig cfg)`.
3. **Interface nhỏ + fake viết tay**: `class FakeClock : IClock { public float Now; }`.
4. **Serialized reference tới ScriptableObject config**: test tạo instance và gán.
5. DI container của dự án (Zenject/VContainer) — chỉ khi dự án đã dùng.

Hạn chế mock framework; fake viết tay dễ đọc và ít gãy khi refactor.

## 7. Helper dùng chung

Chỉ tạo khi ≥2 test lặp cùng boilerplate. Đặt trong
`Assets/Tests/EditMode/Helpers/`; nếu PlayMode cũng cần → hỏi trước khi tạo asmdef
`TestHelpers` dùng chung.

- Helper assert qua NUnit (`Assert.That`), **không** dùng `Debug.Assert` (bị strip,
  không làm test fail).
- Factory tạo object tối thiểu, không load scene.
- Builder cho data phức tạp: `new EnemyConfigBuilder().WithHealth(10).Build()`.

## 8. Checklist trường hợp biên

- [ ] 0, âm, `int.MaxValue`, vượt max/min
- [ ] Collection rỗng, 1 phần tử, trùng phần tử
- [ ] `null` input (với API public)
- [ ] Object đã `Destroy` (Unity `== null` trả true nhưng reference C# vẫn còn)
- [ ] Gọi lặp liên tiếp trong cùng frame (double click, double submit)
- [ ] Disable/enable lại component giữa chừng (event đăng ký lại 2 lần?)
- [ ] Gọi trước `Awake`/`Start` (thứ tự khởi tạo)
- [ ] Hủy giữa chừng coroutine/async

## 9. Chẩn đoán flaky test

Không tăng timeout, không retry. Kiểm tra theo thứ tự:

1. **Rò trạng thái**: object/static từ test trước còn sót? Chạy riêng test đó với
   `-testFilter` có pass không? Pass riêng, fail khi chạy chung → rò trạng thái.
2. **Phụ thuộc thời gian**: assert sau N frame / N giây cố định → chuyển sang chờ điều
   kiện; hoặc tách logic khỏi thời gian thực.
3. **Random không seed** hoặc thứ tự hash collection.
4. **Thứ tự khởi tạo**: `Awake` của A phụ thuộc B chưa chạy → `yield return null`
   trước Act, hoặc sửa thứ tự trong code (Script Execution Order / khởi tạo tường minh).
5. **Physics**: assert vị trí chính xác → dùng sai số (`Is.EqualTo(x).Within(0.01f)`),
   đợi `WaitForFixedUpdate`.
6. **Async**: task chưa hoàn thành khi assert; exception bị nuốt trong `async void`.

Ghi rõ nguyên nhân gốc trong báo cáo.
