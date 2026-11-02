# CLAUDE.md – Lò Giả Kim (working title: Crucible)

> Ngữ cảnh dự án cho Claude Code. Đặt file này ở root repo với tên `CLAUDE.md`.
> Nguồn sự thật về thiết kế: **GDD v2 – Run liên tục** (https://claude.ai/artifact/HUzRUkt7Q7AfyVQm5wh9HS). GDD v1 (QEc8z9hcssgad7LSN9BFGz) đã bị thay thế.
> Khi file này và GDD v2 mâu thuẫn, **hỏi lại**, không tự chọn.
> Cập nhật lần cuối: 2026-11-01 (chuyển sang run liên tục 8 Mùa).

## 1. Dự án là gì

Puzzle roguelite 2D offline, single player, màn hình ngang, chạy được trên web (WebGL). Người chơi là nhà giả kim của triều đình: mỗi Mùa luyện khoáng thạch để Nhà vua rèn trang bị cho binh lính thi đấu và ra trận. Thua 3 trận thì bị trục xuất / xử tử.

**Một ván = một run 8 Mùa.** Run bắt đầu với lưới **3×3 cố định**, bộ nguyên liệu khởi đầu nhỏ, 0 vàng, 3 Uy tín. Mỗi Mùa:

```
Chiếu chỉ → (Chợ → Lò nung) ×3 → Xưởng → Dâng → Rèn → Trận (Boss) → Kết quả
```

- 3 lượt nung đầu là phần chuẩn bị; Dâng → Rèn → Trận là màn Boss.
- Dâng 1 khoáng thạch (luôn tiêu hao) → Rèn ra trang bị có **loại ngẫu nhiên** → so sức mạnh trang bị với đối thủ (tất định).
- Thắng → chọn 1/3 chiến lợi phẩm. Thua → −1 Uy tín. Uy tín về 0 → trục xuất / xử tử, hết run.
- Hoàn thành Mùa 8 còn Uy tín = thắng run (kể cả khi thua Đại chiến), mở bậc khó kế tiếp, được chơi tiếp Endless (sau MVP).

Người chơi xếp nguyên liệu từ Túi lên lưới, sửa tự do, rồi nhấn Nung. Lưới **chỉ là dòng thời gian viết gọn**: từng ô được cho vào lò theo thứ tự quét hàng, ô trống là một nhịp chờ. Vị trí không gian không có ý nghĩa.

Thành phẩm được xác định bằng **khớp công thức**: đúng nguyên liệu, đúng thứ tự, đúng khoảng cách → ra thành phẩm. Phụ gia được phép điền vào khoảng trống mà không làm sai công thức. **Phụ gia không thay đổi công thức gốc** (thành phẩm, `Essence`, `Potency` giữ nguyên); MVP nó chỉ **tăng xác suất Kích hoạt ma thuật** của thành phẩm. Không khớp → phế phẩm. Fiction (nóng chảy, thép/gang) chỉ để dễ hình dung; Core không mô phỏng.

Chủ đề vật phẩm: khoáng sản, khoáng vật, kim loại và phi kim có giá trị cao. Khoáng và phụ gia có **tier 1–10**. Chợ chỉ bán tier 1–3; tier cao phải tự nung. Lưới 3×3 giới hạn công thức ở 9 tick, nên độ sâu đến từ **chuỗi tầng** (thành phẩm làm nguyên liệu cho công thức sau).

**Mục tiêu xa: Khoáng thạch Ma thuật Tối thượng (tier 10).** Không bắt buộc để thắng run; khó đạt trong 8 Mùa, chủ yếu đạt ở Endless. Cửa vào nhánh của nó là **Kích hoạt ma thuật**: không có ma thuật thì không thể tới.

**Khoảng cách là bí mật.** Người chơi tối đa chỉ biết nguyên liệu và thứ tự của công thức. Khoảng cách cố định ở mọi run, mọi seed, mọi bậc khó; cộng đồng chia sẻ công thức là trải nghiệm được chấp nhận.

Thuộc tính ẩn chỉ xem được bằng **Kính soi**. Sức mạnh trang bị, sức mạnh đối thủ và trọng số đều **tính ngầm**, không hiển thị chi tiết.

Nguyên liệu tiêu hao khi nung. Không có vàng khởi đầu. Vàng, Túi, Xúc tác, Uy tín chỉ sống trong một run. Thứ giữ lại qua các run: Sổ công thức (kèm trạng thái mở khóa) và Danh vọng.

Pillars ảnh hưởng trực tiếp tới code:
- **Tất định tuyệt đối:** cùng seed + cùng chuỗi input → cùng state hash, trên mọi platform kể cả WebGL.
- **Dừng là dừng:** lưu sau mỗi Command.
- **Content là data:** item, công thức, phụ gia, thuộc tính, Kính soi, đối thủ, trang bị, modifier, chiến lợi phẩm, bậc khó đều là data.
- **Không rò rỉ thông tin:** không gì trong Presentation để lộ khoảng cách công thức, vị trí sai, thuộc tính chưa soi, hay con số sức mạnh.
- **Hiểu biết là tiến bộ:** biến thiên chỉ nằm ở tài nguyên, giá, trận đấu và phần thưởng, không bao giờ ở công thức.

## 2. Trạng thái hiện tại

- Milestone đang làm: `M3` (M1, M2 xong; M3 chờ playtest WebGL)
- Unity: `6000.6.7f1` · Render pipeline: `URP 2D`
- Unity executable: `/Applications/Unity/Hub/Editor/6000.6.7f1/Unity.app/Contents/MacOS/Unity`
- Build target: WebGL (chính) + Standalone (dev/test)
- Hiển thị: 2D, landscape, reference resolution **1920×1080** (FullHD)

Lộ trình (chi tiết ở GDD v2 mục 13):

| Milestone | Nội dung chính | Tiêu chí xong |
| --- | --- | --- |
| M0 | Project, 6 asmdef, CI EditMode, build WebGL rỗng | CI xanh; Core build không có UnityEngine; WebGL build chạy được trong trình duyệt |
| M1 | Lưới 3×3, `FeedSequence`, `Recipe` + phụ gia + `RequiresMagic`, `RecipeMatcher`, `Product` + thuộc tính + Kích hoạt ma thuật, ScoreCalculator, RngStreams (7 stream), `BrewLog` không rò rỉ, I1–I3, I5, I11, I15, I18, I25 | Ví dụ điểm ra đúng 108; cùng nguyên liệu khác khoảng cách ra hai thành phẩm khác nhau; lệch 1 tick → phế phẩm; phụ gia hợp lệ vẫn khớp và không đổi thành phẩm/`Quality` cơ bản; `BrewLog` khớp/hỏng giống nhau tới sự kiện kết quả; 1.000 lần cùng seed cùng hash |
| M2 | Túi, chỉnh sửa lưới + khóa khi Nung, bỏ qua lượt, `RunStateMachine` (8 phase/Mùa), Chợ + `ShopMaxTier` + giá dao động, modifier Mùa, Kính soi, Dâng → `Forge` → `Battle`, Uy tín, chiến lợi phẩm | RandomAgent 1.000 run không vi phạm bất biến (I1–I25), mọi run đi tới `RunEnd` |
| M3 | Greybox 1920×1080: Chiếu chỉ, Chợ, Lò (kéo thả, animation theo tick, timescale 1×–3×), Xưởng, Dâng, Rèn, animation Trận, chiến lợi phẩm, màn kết thúc (thắng / trục xuất) | Người ngoài chơi trọn một run trên WebGL không cần giải thích |
| M4 | Content MVP: khoáng và phụ gia tier 1–10 (Tối thượng = 10), công thức, thuộc tính, Kính soi, Xúc tác, trang bị, 8 đối thủ, bể modifier, bể chiến lợi phẩm, validator, save + snapshot | Tắt giữa lượt (kể cả đóng tab trình duyệt), mở lại đúng trạng thái ở bất kỳ phase nào |
| M5 | Seed, Danh vọng, Sổ công thức + trạng thái mở khóa, 3 bậc khó, bộ khởi đầu | Cùng seed trên Editor, Standalone và WebGL cho cùng Chợ đầu, cùng giá Mùa 1, cùng loại trang bị rèn ở Mùa 1 |
| M6 | GreedyAgent, fuzz, báo cáo cân bằng (tỉ lệ thắng theo bậc, Mùa bị trục xuất phổ biến, tỉ lệ làm ra khoáng Tối thượng trong 8 Mùa), playtest | 50.000 run nightly sạch bất biến |
| Sau MVP | Endless, modifier đổi luật, `OnSeasonStart` | — |

**Chỉ làm việc thuộc milestone hiện tại.** Thấy việc của milestone khác thì ghi vào báo cáo.

## 3. Kiến trúc

| Assembly | Phụ thuộc | Nội dung |
| --- | --- | --- |
| `Alchemy.Core` | Không (`noEngineReferences: true`) | State, luật, khớp công thức, thuộc tính, ma thuật, rèn, trận, kinh tế, RNG, bất biến |
| `Alchemy.Content` | Core, UnityEngine | ScriptableObject Def (`ItemDef`, `StatDef`, `RunDef`, `OpponentDef`, `EquipmentDef`…), converter sang data immutable, validator |
| `Alchemy.Presentation` | Core, Content, UnityEngine | MonoBehaviour, view, UI, animation, input |
| `Alchemy.Persistence` | Core | Save JSON, versioning, storage theo platform |
| `Alchemy.Tests.EditMode` | Core, Content, Persistence | Unit test, simulation harness, replay |
| `Alchemy.Tests.PlayMode` | Tất cả | Smoke test |

Quy tắc phụ thuộc (không được phá):
- `Alchemy.Core` **không bao giờ** tham chiếu `UnityEngine`, `UnityEditor` hay assembly nào khác của dự án.
- Presentation gửi `ICommand` vào Core, nhận `CommandResult` + `BrewLog`. Core không biết animation tồn tại; Core không chờ View.
- **Thời gian trong Core là tick rời rạc**, không phải giây. Core xử lý cả lượt nung ngay lập tức; Presentation phát lại `BrewLog` theo nhịp và timescale.
- Presentation chỉ thấy những gì người chơi được phép biết. Khoảng cách công thức, slot sai, thuộc tính chưa soi, `Power` của trang bị và đối thủ, trọng số trang bị **không** đi qua ranh giới Core → Presentation (trừ `#if ALCHEMY_DEBUG`). Kết quả trận chỉ gồm loại trang bị + Thắng/Thua; đối thủ chỉ lộ tên, loại trận, mức khó định tính.
- Chỉ `EffectRunner` biết về Effect primitive. Lớp khác không switch theo loại hiệu ứng.
- Che giấu do compiler đảm bảo: thuộc tính ẩn, `Power`, trọng số, `Recipe` (pattern, phụ gia, Essence) là `internal` trong Core; chỉ `Alchemy.Content`, `Alchemy.Persistence`, `Alchemy.Tests.EditMode`, `Alchemy.Tests.PlayMode` được `InternalsVisibleTo`. **Không** thêm Presentation vào danh sách này.

Lớp chính trong Core: `RunState` (có `ComputeHash()`), `RunStateMachine`, `RecipeMatcher`, `ProductFactory`, `ScoreCalculator`, `Inspector` (Kính soi), `Forge`, `Battle`, `Market`, `SeasonModifiers`, `RewardOffer`, `EffectRunner`, `Economy`, `RngStreams` (PCG32 tự viết), `InvariantChecker`.

Luồng `RunStateMachine`:
```
RunStart
  → Decree (đối thủ Mùa này, giá chợ, modifier Mùa sau)
  → [ Market → Placing (thêm/gỡ/thay/sắp xếp tự do) → Nung (khóa lưới) → Brewing
      | Market → Skip (bỏ qua lượt, không có thành phẩm) ] × 3
  → Workshop (soi, chọn; không mua bán)
  → Offering (tiêu hao 1 khoáng) → Forge (loại trang bị, stream Forge) → Battle
      Thắng → Spoils (chọn 1/3) → Decree Mùa sau | RunEnd(Thắng) sau Mùa 8
      Thua  → Reputation − 1 → RunEnd(Trục xuất) nếu = 0 | Decree Mùa sau | RunEnd(Thắng) sau Mùa 8
```
Chợ mở trước mỗi lượt nung. Mỗi lượt bắt đầu với lưới trống. Lượt bỏ qua vẫn tính vào 3 lượt. Hết nguyên liệu thì chỉ còn bỏ qua; không có gì để dâng → tự động Thua trận.

## 4. Thuật ngữ (tên trong code)

| Tiếng Việt | Tên code | Ghi chú |
| --- | --- | --- |
| Run | `Run` / `RunDef` | Một ván, 8 Mùa. `RunDef` chứa đối thủ từng Mùa, bể trang bị, bể chiến lợi phẩm, `ShopMaxTier` |
| Mùa | `Season` | Một chu kỳ: Chiếu chỉ → 3 lượt nung → Xưởng → Dâng → Rèn → Trận |
| Chiếu chỉ | `Decree` | Phase đầu Mùa: đối thủ, giá chợ, modifier Mùa sau |
| Chợ | `Market` | Mở trước mỗi lượt nung |
| Xưởng | `Workshop` | Phase sau lượt 3: soi, chọn khoáng; không mua bán |
| Dâng | `Offering` | Nộp 1 khoáng đạt chuẩn; luôn tiêu hao |
| Rèn | `Forge` | Khoáng → trang bị, loại rút ngẫu nhiên |
| Trang bị | `Equipment` / `EquipmentDef` | Loại (Kiếm, Giáo, Giáp, Khiên…), `QualityWeight`, trọng số theo `StatDef` |
| Đối thủ | `OpponentDef` | Tên, loại trận (Thi đấu / Chiến tranh / Đại chiến), `Power` |
| Trận | `Battle` | `Power` trang bị ≥ `Power` đối thủ → Thắng |
| Uy tín | `Reputation` | 3, chỉ giảm (MVP). Về 0 → trục xuất / xử tử. **Khác Danh vọng** |
| Chiến lợi phẩm | `RewardDef` | Chọn 1/3 sau trận Thắng |
| Modifier Mùa | `SeasonModifierDef` | Hệ số, biết trước 1 Mùa |
| Bậc khó | `DifficultyDef` | Mở sau khi thắng run |
| Bộ khởi đầu | `StarterKitDef` | Nguyên liệu khởi đầu của run |
| Túi | `Bag` | Kho duy nhất, nhìn thấy toàn bộ. Sức chứa khởi đầu cố định, nới qua chiến lợi phẩm. Tách biệt với lưới |
| Vật phẩm | `ItemDef` + `Kind` | Base cho mọi vật phẩm. `Kind`: Mineral, Additive, Lens, Catalyst, Reagent |
| Khoáng / Nguyên liệu | `ItemDef` (Kind Mineral) | Có `Tier` 1–10, `MarketPrice`, `Recipe` nullable |
| Cấp | `Tier` | 1–10, khai báo tay trên khoáng và phụ gia. Không có luật thứ tự tier giữa nguyên liệu và thành phẩm |
| Cấp tối đa của Chợ | `ShopMaxTier` | 3 |
| Nhiệt độ nóng chảy | `MeltTicks` | Chỉ cho fiction/hiển thị; Core không dùng |
| Chuỗi nạp | `FeedSequence` | Lưới đọc theo thứ tự quét hàng; ô trống = tick chờ |
| Nhịp | `Tick` | 1 ô = 1 tick |
| Lượt nung | `Brew` | 3 lượt mỗi Mùa |
| Bỏ qua lượt | `SkipBrew` | Lựa chọn hợp lệ, không cho kết quả gì |
| Công thức | `ItemDef.Recipe` | Nhúng trong item thành phẩm. Null = nguyên liệu gốc (chỉ từ Chợ / bộ khởi đầu / chiến lợi phẩm) |
| Slot cần ma thuật | `RequiresMagic` | Cờ trên slot nguyên liệu của `Pattern` |
| Phụ gia | `ItemDef` (Kind Additive) | Điền vào khoảng trống mà không làm sai công thức. **Không** đổi thành phẩm, `Essence`, `Potency`; MVP chỉ cộng `MagicChance`. **Không nhầm với Xúc tác** |
| Kích hoạt ma thuật | `Magic` | Dòng thuộc tính ẩn có/không. Cửa vào nhánh Tối thượng |
| Thuộc tính ẩn | `Stat` / `StatDef` | Một "dòng" thuộc tính của thành phẩm |
| Chất lượng | `Quality` | Thuộc tính mặc định, = Essence × Potency |
| Kính soi | `ItemDef` (Kind Lens) | Tiêu hao, lộ tối đa `RevealLines` dòng thuộc tính |
| Giá thị trường | `MarketPrice` | Giá theo loại vật phẩm × hệ số Mùa × modifier; không theo thuộc tính |
| Thành phẩm | `Product` | Instance riêng: loại vật phẩm + các dòng thuộc tính + tập dòng đã lộ |
| Phế phẩm | `ScrapDef` | Một def chung. Không khớp công thức nào; không làm nguyên liệu, chỉ bán |
| Xúc tác | `ItemDef` (Kind Catalyst) | Tương đương Joker, 5 slot, giữ suốt run. Không gắn với công thức |
| Thuốc thử | `ItemDef` (Kind Reagent) | Tiêu hao, 2 slot |
| Mở khóa | `ItemUnlocked` / `RecipeUnlocked` | Meta theo từng khoáng. Nung ra → mở cả hai; mua / nhận → chỉ mở vật phẩm |
| Mục Sổ công thức | `RecipeEntry` | Meta: nguyên liệu + thứ tự của công thức đã mở khóa, **không** gồm khoảng cách |
| Tinh chất | `Essence` | Thành phần của `Quality` |
| Hiệu lực | `Potency` | Thành phần của `Quality`, bắt đầu từ 1 |
| Danh vọng | `Renown` | XP meta |
| Khoáng thạch Ma thuật Tối thượng | (content, tier 10) | Mục tiêu xa; trang bị rèn từ nó thắng mọi trận trong 8 Mùa |

Đã bỏ khỏi MVP: `Level`/`LevelDef`, `Appraisal`/Thẩm định, `Opus`, `IngredientDef`/`RecipeDef`/`LensDef` tách rời, `IsFailed`, mở rộng lưới, chiến lợi phẩm "+1 Uy tín", `Ante`/Hội, `RoundKind`, tay bài, `DiscardPile`, đổi bài, rút bài, `Reaction`/`ReactionDef` (R01–R10), `Overheat`, `Retrigger`, kề không gian, tag `Bền`, lãi vàng, thưởng khi bỏ qua vòng, `Apparatus`, `School`/Trường phái, `Trial`/Điều kiện, Cấp Lò, Tạp chất. Endless: sau MVP.

Tên trong code dùng tiếng Anh; chuỗi hiển thị lấy từ data.

## 5. Luật lõi phải code đúng

### Túi
- Kho duy nhất, người chơi thấy toàn bộ nội dung. Không có tay, không rút bài, không đổi bài.
- Sức chứa khởi đầu là config; nới qua chiến lợi phẩm "Nới Túi", có trần.
- **Túi và lưới tách biệt.** Đặt lên lưới = rút khỏi Túi. Gỡ khỏi lưới = trả về Túi; luôn vừa. Nhấn Nung = xác nhận tiêu hao 100% vật phẩm trên lưới.
- Túi đầy → không mua thêm được. Lượt nung có ít nhất 1 nguyên liệu đã rút ít nhất 1 vật phẩm khỏi Túi, nên thành phẩm luôn vừa Túi.
- Phế phẩm chiếm chỗ; người chơi nên bán sớm.

### Lưới, đặt và chỉnh sửa
- Lưới **cố định 3×3 = 9 ô = 9 tick** (config toàn game). Không mở rộng, không thu nhỏ.
- Kéo nguyên liệu từ Túi lên **ô trống**. Mỗi ô tối đa **1** nguyên liệu (I11).
- Tự do thêm, gỡ, thay, di chuyển, sắp xếp lại cho tới khi nhấn Nung. Không bắt buộc lấp đầy. Cho phép trùng loại.
- Phế phẩm không đặt được (I13).
- **Nhấn Nung là khóa lưới** (I16).
- Save chỉ cần trạng thái lưới lúc Nung (có thể vẫn lưu Command để undo/replay).

### Chuỗi nạp
Khi Nung, lưới được đọc theo thứ tự quét hàng (trái → phải, trên → dưới) thành `FeedSequence`:

```
|   | A |   |
| B |   |   |
| C | D | E |
→ ["", A, "", B, "", "", C, D, E]
```

Phần tử thứ `i` ứng với tick `i`.

### Khớp công thức
`ItemDef.Recipe` gồm:
- `Pattern`: danh sách slot, mỗi slot là một item id hoặc `Gap`. **Slot đầu và slot cuối luôn là nguyên liệu**; độ dài ≤ 9 (validator kiểm). Slot nguyên liệu có thể mang cờ `RequiresMagic`.
- `AllowedAdditives`: phụ gia được phép đứng ở bất kỳ slot `Gap` nào.
- `BaseEssence`, các dòng thuộc tính khởi đầu. Thành phẩm là chính item chứa `Recipe`.

Thuật toán khớp (`RecipeMatcher`):
1. Bỏ các ô trống ở đầu `FeedSequence`.
2. So từng slot của `Pattern`: slot nguyên liệu phải đúng item đó (và đã Kích hoạt ma thuật nếu slot có `RequiresMagic`); slot `Gap` phải trống hoặc là phụ gia trong `AllowedAdditives`.
3. Mọi ô sau khi hết `Pattern` phải trống.
4. Khớp đúng 1 công thức → `Product`, tính `Quality` cơ bản, tung Kích hoạt ma thuật và thuộc tính ẩn (stream `Chance`), chạy Effect của Xúc tác. Không khớp → phế phẩm (`ScrapDef`).

Hệ quả có chủ ý:
- Cùng nguyên liệu, khác khoảng cách → khác thành phẩm. Lệch 1 tick → phế phẩm.
- Có hay không có phụ gia trong khoảng trống → **cùng** thành phẩm, cùng `Quality` cơ bản; chỉ khác xác suất ma thuật (I18).
- Không có luật thứ tự tier: thành phẩm có thể cao hơn, ngang hoặc thấp hơn tier nguyên liệu.
- Validator đảm bảo không có hai công thức cùng khớp một chuỗi (I2).

### Kích hoạt ma thuật
- Khi một lượt nung khớp công thức, Core tung **một lần** xác suất ma thuật cho thành phẩm (stream `Chance`).
- Xác suất = **truyền thừa + phụ gia**, cộng tuyến tính, trần 90% (config), số nguyên phần trăm:
  - Truyền thừa: mỗi nguyên liệu đã kích hoạt ma thuật góp theo tier của nó (tạm: tier × 5%).
  - Phụ gia: tổng `MagicChance` của mọi phụ gia trong khoảng trống (tạm: tier 1/2/3 = 10/20/30%).
  - Không có cả hai → 0%.
- `Magic` là dòng thuộc tính ẩn, chỉ thấy bằng Kính soi. Không cộng sức mạnh trang bị (MVP).
- Công thức nhánh Tối thượng dùng slot `RequiresMagic`; nguyên liệu chưa kích hoạt → phế phẩm, không báo lý do.

### Che giấu thông tin (bắt buộc)
- Người chơi tối đa chỉ biết **nguyên liệu và thứ tự** của công thức. Khoảng cách không bao giờ xuất hiện ở UI, tooltip, Sổ công thức, thông báo lỗi hay log của release build.
- **Khoảng cách là content cố định**, giống nhau ở mọi run, mọi seed, mọi bậc khó. Modifier không bao giờ đổi công thức.
- Phát lại lượt nung: phát **mọi tick từ nguyên liệu đầu tới nguyên liệu cuối**, kể cả ô trống ở giữa. Không dừng sớm. Kết quả chỉ lộ ra sau nguyên liệu cuối. Ô trống trước nguyên liệu đầu và sau nguyên liệu cuối: không phát.
- Phế phẩm không cho biết sai ở đâu, sai bao nhiêu tick, thiếu ma thuật, hay "gần đúng" công thức nào.
- `BrewLog` chỉ chứa sự kiện từng tick ("nạp X", "chờ") và một sự kiện kết quả cuối (I15). Debug về khớp chỉ có trong `#if ALCHEMY_DEBUG`.
- `Power` trang bị, `Power` đối thủ và trọng số trang bị tính ngầm; UI chỉ hiện mức khó định tính và Thắng/Thua.
- Data công thức vẫn nằm trong build; với game offline single player, chấp nhận được; không obfuscation trừ khi được yêu cầu.

### Thuộc tính ẩn và Kính soi
- Mỗi `Product` đạt chuẩn có các dòng `Stat` (content). Thuộc tính **không** ảnh hưởng giá.
- Thuộc tính MVP: `Quality`, `Magic`, Phản đòn, Hấp thụ sát thương, Sắc bén. Danh sách là content, mở rộng sau.
- Thuộc tính tạo qua stream `Chance`; Xúc tác và Thuốc thử sửa qua Effect; phụ gia chỉ tác động `Magic`.
- Mặc định người chơi **không** thấy thuộc tính nào. `Product` lưu tập dòng đã lộ; Presentation chỉ hiển thị các dòng này.
- Kính soi: vật phẩm tiêu hao, mua ở Chợ. `RevealLines` = số dòng tối đa lộ mỗi lần.
  - Thành phẩm có ≤ `RevealLines` dòng → lộ toàn bộ.
  - Nhiều hơn → chọn ngẫu nhiên `RevealLines` dòng **trong các dòng chưa lộ** (stream `Inspect`).
- Tập dòng đã lộ thuộc về **từng instance**, giữ suốt run. Thành phẩm mới phải soi lại; nguyên liệu bị tiêu hao thì thông tin soi mất theo.
- Thành phẩm không gộp chồng; mỗi instance chiếm 1 chỗ.
- Đã lộ hết dòng → không cho soi (không tiêu hao Kính). Không soi phế phẩm. Dùng được ở mọi phase người chơi điều khiển, trừ `Brewing`.

### Công thức điểm (`Quality`)
```
Quality = sum(Essence) × ((1 + sum(AddPotency)) × product(MulPotency))
```
- `Essence` khởi đầu = `BaseEssence`; Xúc tác, Thuốc thử cộng qua Effect. **Phụ gia không cộng Essence/Potency.**
- Mọi `AddPotency` áp trước, mọi `MulPotency` áp sau, **bất kể thứ tự kích hoạt**.
- Test chuẩn: tổng 18 Essence, +3 Potency, ×1.5 → 108.

### Thành phẩm và tiêu hao
- Mỗi lượt nung có ít nhất 1 nguyên liệu tạo **1 `Product`**. Lưới trống khi Nung = không có thành phẩm.
- **Mọi nguyên liệu trên lưới (kể cả phụ gia) bị tiêu hao.**
- Thành phẩm (cả phế phẩm) vào Túi. Thành phẩm đạt chuẩn dùng được làm nguyên liệu; phế phẩm thì không.

### Nhịp hiển thị
- `BrewTickSeconds` = 3 (config). Timescale **1× / 2× / 3×** → lưới 3×3 đầy đủ mất 27s / 13,5s / 9s.
- Timescale là tốc độ phát lại `BrewLog` của Presentation. Không dùng `Time.timeScale` toàn cục.
- `MeltTicks` chỉ để hiển thị, không được lộ thông tin công thức.

### Dâng, Rèn, Trận, Uy tín
- **Đối thủ** cố định trong `RunDef` theo Mùa và bậc khó (không random). Chiếu chỉ chỉ hiện tên, loại trận, mức khó định tính (tạm ★1–5).
- **Dâng:** chọn bất kỳ thành phẩm đạt chuẩn nào trong Túi; **luôn tiêu hao**. Không có gì để dâng → tự động Thua.
- **Rèn:** loại trang bị rút ngẫu nhiên **sau khi dâng** (stream `Forge`) từ bể của Mùa.
  ```
  Power = Quality × QualityWeight + Σ(Stat_i × Weight_i)
  ```
  Mọi loại trang bị có cùng `QualityWeight`; chỉ trọng số thuộc tính khác nhau (Kiếm: Sắc bén; Giáo: Sắc bén, Phản đòn; Giáp: Hấp thụ; Khiên: Phản đòn, Hấp thụ). Chỉ cộng và nhân, `double`, clamp.
- **Trận:** `Power` trang bị ≥ `Power` đối thủ → Thắng. Tất định. Thi đấu / Chiến tranh / Đại chiến cùng luật, chỉ khác tên và độ khó.
- **Uy tín:** khởi đầu 3. Thua → −1. Về 0 → `RunEnd(Trục xuất)`. MVP không có cách hồi.
- **Chiến lợi phẩm:** sau trận Thắng (trừ Mùa 8), rút 3 lựa chọn (stream `Reward`), nhận 1: Xúc tác, Nới Túi, Khoáng hiếm (tier 4+, trần theo Mùa), Thuốc thử. Không có vàng.
- **Thắng run:** hoàn thành Mùa 8 còn ≥ 1 Uy tín. Run kết thúc (thắng hay trục xuất) luôn quy đổi Danh vọng.

### Chợ và kinh tế
- Chợ mở **trước mỗi** lượt nung. Không có Chợ ở Xưởng và sau Mùa 8.
- **Chợ bán:** khoáng và phụ gia gốc (không có `Recipe`) tier ≤ `ShopMaxTier` (3), Kính soi, Xúc tác, Thuốc thử. Tỉ lệ tier 1/2/3 = 70/25/5 (stream `Shop`), số lượng giới hạn mỗi lượt. **Không bao giờ** bán tier 4+.
- **Người chơi bán:** mọi thứ, kể cả khoáng tier cao. Phế phẩm giá cố định thấp.
- Giá = `MarketPrice` × hệ số Mùa (stream `Market`, số nguyên phần trăm) × modifier. **Không** theo thuộc tính. Giá mua ≥ 1, giá bán ≥ 0.
- **Không có vàng khởi đầu.** Vàng chỉ đến từ bán và `GainGold`. Không thưởng vàng, không lãi.
- Vàng giữ qua các lượt và các Mùa, mất khi run kết thúc.
- Nguyên liệu miễn phí chỉ từ nguồn khai báo có giới hạn (chiến lợi phẩm, `CreateIngredient`).
- Bán hết vốn là lựa chọn hợp lệ. Không chặn bằng luật.
- Reroll 2 vàng, +1 mỗi lần, reset mỗi lượt.

### Biến thiên
- Hàng Chợ (`Shop`), loại trang bị khi Rèn (`Forge`), giá theo Mùa (`Market`), modifier Mùa (`Season`), chiến lợi phẩm (`Reward`). Không bao giờ chạm công thức.
- Modifier: 0–1 mỗi Mùa, biết trước 1 Mùa, MVP **chỉ đổi số** (giá mua/bán, giá Kính soi, `Power` đối thủ, trọng số bể trang bị, reroll miễn phí). Không đổi số lượt nung.

### Meta: Sổ công thức, mở khóa, Danh vọng
- Mỗi khoáng có hai trạng thái meta: `ItemUnlocked`, `RecipeUnlocked`. Nung ra → mở cả hai. Mua ở Chợ / nhận từ chiến lợi phẩm → chỉ `ItemUnlocked`.
- Sổ ghi **nguyên liệu + thứ tự** của công thức đã mở khóa. Không ghi khoảng cách, phụ gia, thuộc tính.
- Danh vọng quy đổi cuối run theo số trận Thắng, bậc khó, tier cao nhất đã luyện. Mở bộ khởi đầu, nguyên liệu mới vào bể Chợ, Xúc tác mới. Thắng run mở bậc khó kế tiếp.
- Mở khóa chỉ thêm lựa chọn, không tăng chỉ số.

### Giá trị khởi đầu (đọc từ config ScriptableObject, không hard-code)
8 Mùa/run · 3 lượt nung/Mùa · lưới 3×3 · 3 Uy tín · vàng khởi đầu 0 · `ShopMaxTier` 3, tỉ lệ tier 70/25/5 · reroll 2 vàng (+1 mỗi lần) · `BrewTickSeconds` 3 · timescale 1/2/3 · trần ma thuật 90%, truyền thừa tier × 5%, `MagicChance` phụ gia 10/20/30% · biên độ giá Mùa 70–130% · sức chứa Túi `<chưa có số>` · `RevealLines` theo cấp `<chưa có>` · `Power` đối thủ từng Mùa `<chưa có>`.

## 6. Bất biến (không được phá)

| ID | Bất biến |
| --- | --- |
| I1 | Mỗi lượt nung xử lý tối đa 9 tick và kết thúc |
| I2 | Mỗi `FeedSequence` khớp tối đa 1 công thức (validator đảm bảo, runtime kiểm lại) |
| I3 | Thuộc tính hữu hạn; `Quality` ≥ 0; Potency ≥ 1; không NaN/Infinity (`double`, clamp 1e300) |
| I4 | Vàng ≥ 0; giá mua ≥ 1; giá bán ≥ 0, sau khi áp hệ số Mùa và modifier |
| I5 | Cùng seed + cùng input → cùng state hash, trên mọi platform |
| I6 | Mọi random qua `RngStreams`, mỗi hệ thống một stream (Shop, Chance, Inspect, Forge, Market, Season, Reward) |
| I7 | Content chỉ ghép từ primitive whitelist, trong giới hạn khai báo |
| I8 | Lưới luôn là 3×3 (config toàn game, ≥ 4 ô) |
| I9 | Không treo state: luôn bỏ qua được lượt; run luôn đi tới `RunEnd`. Nguyên liệu miễn phí chỉ từ nguồn khai báo có giới hạn |
| I10 | Số vật phẩm trong Túi ≤ sức chứa Túi hiện tại (lưới không tính) |
| I11 | Mỗi ô lưới chứa tối đa 1 nguyên liệu |
| I12 | Số lượt đã dùng trong Mùa (nung + bỏ qua) ≤ 3; số Mùa ≤ `SeasonsPerRun` |
| I13 | Phế phẩm không bao giờ nằm trên lưới |
| I14 | Vật phẩm chỉ rời sở hữu qua: tiêu hao khi nung, dùng Kính soi, bán, trao đổi, Dâng |
| I15 | Với cùng một lưới, `BrewLog` của lượt khớp và lượt hỏng có cùng chuỗi sự kiện tick; chỉ sự kiện kết quả cuối khác nhau |
| I16 | Trong `Brewing`, lưới bị khóa: không Command chỉnh sửa lưới nào được chấp nhận |
| I17 | Số dòng đã lộ ≤ số dòng thuộc tính; mỗi lần soi lộ thêm tối đa `RevealLines` dòng, chỉ trong các dòng chưa lộ |
| I18 | Phụ gia không đổi loại thành phẩm, `Essence`, `Potency` hay `Quality` cơ bản; chỉ đổi xác suất ma thuật |
| I19 | 0 ≤ `Reputation` ≤ `MaxReputation`; `Reputation` = 0 ⇒ `RunEnd(Trục xuất)`; MVP chỉ giảm, không hồi |
| I20 | Mỗi hệ số (giá Mùa, modifier) nằm trong giới hạn khai báo và là số nguyên phần trăm |
| I21 | Mỗi Mùa có đúng 1 lần Dâng: tiêu hao 1 khoáng (0 nếu không có), rèn 0–1 trang bị, trả đúng 1 kết quả Thắng / Thua |
| I22 | Chiến lợi phẩm chỉ mở sau trận Thắng, tối đa 3 lựa chọn hợp lệ, nhận đúng 1 |
| I23 | `Power` trang bị hữu hạn, ≥ 0, chỉ dùng cộng và nhân; cùng khoáng + cùng loại trang bị → cùng `Power` |
| I24 | Chợ không bao giờ bày bán vật phẩm có `Tier` > `ShopMaxTier` hoặc có `Recipe` |
| I25 | Xác suất ma thuật là số nguyên phần trăm trong [0, trần]; slot `RequiresMagic` chỉ khớp với nguyên liệu đã kích hoạt; phế phẩm không bao giờ là nguyên liệu |

Quy tắc khi làm việc với bất biến:
- Mọi thay đổi trong `Alchemy.Core` phải kèm test cho bất biến liên quan.
- `InvariantChecker` chạy sau mỗi Command trong debug build và trong mọi test.
- Vi phạm bất biến được sửa **bằng luật**, không bằng chỉnh số liệu content (ngoại lệ: I2 là lỗi content, validator phải bắt).
- Bug do harness/fuzz tìm ra: tái hiện bằng file `.replay` trong test trước, rồi mới sửa.

## 7. Effect primitive và content

Mọi hiệu ứng = Trigger + Condition + Action.

Whitelist MVP:
- Trigger: `OnPlace`, `OnBrewStart`, `OnRecipeMatch`, `OnBrewFail`, `OnBrewEnd`, `OnShopEnter`, `OnSeasonStart` (tùy chọn, có thể để sau MVP)
- Condition: `IngredientIs`, `RecipeIs`, `AdditiveCount`, `Chance`
- Action: `AddEssence`, `AddPotency`, `MulPotency` (tối đa ×5/lần), `AddStat` (có giới hạn khai báo), `GainGold`, `AddTag`, `CreateIngredient` (vào Túi, chặn nếu Túi đầy), `AddMagicChance` (chỉ phụ gia; 1–50% mỗi phụ gia, cộng dồn, trần tổng 90%)

Cơ chế ngoài Effect (đã duyệt 2026-11-01, số liệu tạm):
- `ScaleParam` (của `SeasonModifiers`): nhân một tham số trong danh sách đóng (giá mua, giá bán, giá Kính soi, giá reroll, `Power` đối thủ, trọng số bể trang bị), phần trăm số nguyên trong giới hạn khai báo.
- `OffsetParam`: cộng số nguyên; MVP chỉ cho số reroll miễn phí (0–2).
- `RewardOffer`: rút 3 `RewardDef` từ bể Mùa, lọc theo điều kiện xuất hiện.
- `RequiresMagic`: cờ trên slot nguyên liệu của `Pattern`, do `RecipeMatcher` đọc.
- Kính soi (`Inspector`), `Forge`, `Battle` là hệ thống Core riêng, không phải Effect.

**Phụ gia:** Effect của phụ gia **chỉ** được dùng `AddMagicChance` (MVP); `AddEssence`, `AddPotency`, `MulPotency` bị validator chặn.

Đã bỏ: `OnReact`, `OnTransform`, `OnRoundEnd`, `NeighborCount`, `CellTag`, `ReactionId`, `CountOnGrid`, `Transform`, `Retrigger`.

Quy tắc:
- Thêm content mới = thêm ScriptableObject trong `Assets/Content/`, không thêm code.
- **Không tự thêm primitive mới.** Nếu content cần primitive chưa có, dừng lại và đề xuất (tên, giới hạn, ảnh hưởng tới bất biến).
- Validator chạy khi Enter Play Mode và trong một EditMode test. Phải làm test fail khi: data sai schema; hai công thức trùng khớp; `Pattern` bắt đầu/kết thúc bằng `Gap` hoặc dài > 9; `RequiresMagic` trên `Gap`; phụ gia dùng Action khác `AddMagicChance`; item có `Recipe` nằm trong bể Chợ hoặc tier > `ShopMaxTier` trong bể Chợ; khoáng tier 10 hoặc khoáng cần cho đối thủ không có chuỗi công thức bắt nguồn từ nguyên liệu Chợ / bộ khởi đầu / chiến lợi phẩm.

## 8. Quy ước riêng của dự án

- Trong Core: không dùng `UnityEngine.Random`, `Time`, `DateTime.Now`, `Guid.NewGuid`, hay duyệt `Dictionary`/`HashSet` khi thứ tự ảnh hưởng kết quả. Cần thứ tự thì sort hoặc dùng `List`.
- Không dùng `float` cho thuộc tính và `Power`; dùng `double` và clamp. Xác suất và hệ số giá là số nguyên phần trăm.
- Thời gian lượt nung trong Core chỉ là số tick (`int`). Giây và timescale chỉ tồn tại ở Presentation.
- Tất định giữa WebGL và native: logic ảnh hưởng state chỉ dùng phép tính cơ bản (`+ - × ÷`, so sánh). Tránh `Math.Pow`, `Exp`, `Log`, `Sin`… trong Core.
- Core không dùng thread/`Task.Run` (WebGL không hỗ trợ đa luồng). Harness chạy song song chỉ ở EditMode/CI.
- Save trong run: `{ version, seed, runDefId, difficulty, starterKitId, actions[] }` + snapshot đầu mỗi lượt (bắt buộc; replay từ snapshot gần nhất). Meta (Sổ công thức, trạng thái mở khóa, Danh vọng, bậc khó cao nhất) lưu file riêng; vàng, Túi, Uy tín không vào meta.
- Storage qua interface trong Persistence. Standalone: ghi file tạm rồi rename. WebGL: không dựa vào atomic rename; cần flush IndexedDB sau khi ghi (kiểm tra cơ chế cụ thể theo phiên bản Unity trước khi cài đặt).
- Presentation: 2D, landscape, Canvas Scaler theo reference 1920×1080; kiểm tra layout ở tỷ lệ khác 16:9 trong trình duyệt. Chỉ phát lại `BrewLog`; dùng `ObjectPool<T>` cho view. Không dùng `Update`/`FixedUpdate`/`LateUpdate` để làm mới UI: view vẽ lại theo event (`RefreshAll()` sau mỗi Command), animation/timer dùng coroutine `WaitForSeconds`. Không gọi `GetComponent*`/`transform.Find`/`GameObject.Find` trong code runtime: reference gán sẵn qua `[SerializeField]` (builder hoặc Inspector); chỉ Editor script và test được tìm object.
- Tên test ví dụ: `Match_WhenOffByOneTick_ProducesScrap`, `Match_WithAdditiveInGap_SameProductAndQuality`, `Match_WhenRequiresMagicAndIngredientNotMagic_ProducesScrap`, `Magic_ChanceSumsInheritanceAndAdditives_CappedAt90`, `BrewLog_WhenFailed_MatchesSuccessUntilResult`, `Place_WhenBrewing_Rejects`, `Inspect_WhenLensSmallerThanStats_RevealsRandomSubset`, `Offering_Always_ConsumesProduct`, `Battle_WhenPowerEqualsOpponent_Wins`, `Reputation_WhenZero_EndsRunExiled`, `Market_NeverOffersTierAboveShopMaxTier`, `Shop_Buy_WhenBagFull_Rejects`, `Shop_SellPrice_IgnoresStats`, `Unlock_WhenBrewed_UnlocksItemAndRecipe`, `Unlock_WhenBought_UnlocksItemOnly`.
- Simulation harness: `RandomAgent`, `GreedyAgent`; CI 2.000 run/commit, nightly 50.000. Mọi run mô phỏng phải đi tới `RunEnd`.

## 9. Lệnh

```
# EditMode test (Editor phải đóng)
<Unity> -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults results/editmode.xml -quit

# Một nhóm test
<Unity> -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testFilter Alchemy.Tests.EditMode.Recipe -testResults results/recipe.xml -quit

# Build WebGL → Builds/WebGL (Assets/Editor/BuildScript.cs; exit 1 khi build hỏng)
<Unity> -batchmode -nographics -projectPath . -buildTarget WebGL -executeMethod BuildScript.WebGL -quit
```

## 10. Câu hỏi mở (không tự quyết, hỏi trước)

Nguyên tắc chung (chủ dự án chốt): khi có nhiều phương án, ưu tiên phương án đơn giản và tiện cho MVP. Vẫn hỏi trước với các câu dưới đây. Danh sách đầy đủ ở GDD v2 mục 14.

- **Mức khó đối thủ hiển thị ra sao?** Tạm: ★1–5 suy từ `Power` theo bảng khoảng.
- **Chiến lợi phẩm có nút bỏ qua không?** Tạm: không.
- **Bộ khởi đầu chọn trước run ngay MVP?** Tạm: có, 1–2 bộ.
- **Danh vọng quy đổi và giá mở khóa.** Chưa có số.
- **Độ dài run** (8 × 3 lượt nung + 8 trận, ~30–45 phút?). Cần playtest.
- **Con số cân bằng** (đọc từ config, không chặn code): sức chứa Túi, `RevealLines` theo cấp Kính soi, giá thị trường, `Power` đối thủ từng Mùa, trần tier "Khoáng hiếm" theo Mùa. Số liệu ma thuật và primitive hiện là tạm, Claude được phép đặt, hiệu chỉnh sau.

## 11. Bàn giao sau mỗi milestone

Ngoài code + test, mỗi milestone có thay đổi scene/UI phải bàn giao thêm (milestone không đụng scene thì ghi "không có thay đổi scene" trong báo cáo):

1. **Mockup UI dạng HTML/SVG**: `Docs/ui/<Milestone>-*.html`, một file tự chứa, 1920×1080, một khung cho mỗi màn, chú thích tên object trong Hierarchy. Mockup vẽ **trước** khi dựng scene để duyệt bố cục, cập nhật lại nếu scene đổi.
2. **Cấu trúc Hierarchy** của scene đã đổi (cây object, đánh dấu object bật/tắt theo phase).
3. **Bảng object → script + component**: mỗi object có script hoặc component quan trọng, ghi field serialized đã gán.

Mục 2 và 3 nằm trong `Docs/milestones/<Milestone>.md`. Scene greybox được dựng bằng `Assets/Editor/GreyboxSceneBuilder.cs` (menu **Crucible/Build Greybox UI**); chạy lại sẽ dựng lại cây `Canvas/Game` và `EventSystem`, nên chỉnh tay trong scene phải chuyển ngược vào builder, hoặc bỏ builder khi scene được chỉnh tay.

