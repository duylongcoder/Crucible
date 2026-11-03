# Playtest MVP — vai User và Tester

Content `mvp`, 36 vật phẩm, 3 bậc khó, 3 bộ khởi đầu. Sinh bởi `PlaytestLogTests` (chạy lại cho cùng kết quả: mọi random theo seed).

## Kiểm tra flow (2026-11-03, Unity 6000.6.7f1, Editor macOS)

| Phạm vi | Kết quả |
| --- | --- |
| EditMode (luật, khớp công thức, Effect, save/load ở mọi phase, meta, validator content, harness 1.000 run RandomAgent + GarbageAgent, GreedyAgent 900 run) | 219 pass, 0 fail (1 skip = test sinh log này, `[Explicit]`) |
| PlayMode (scene Main: Run mới → Chiếu chỉ → Chợ → Lò kéo thả → Nung + phát lại tick → Xưởng → Dâng → Trận → Chiến lợi phẩm → Kết thúc → Run mới, chỉ bằng click; resume sau khi load lại scene; Sổ công thức; không lộ thông tin ẩn) | 20 pass, 0 fail |
| Build Standalone / WebGL, so seed giữa platform, đóng tab trình duyệt | **Chưa chạy** (Editor đang mở khóa project) |

Hai phần **Phát hiện** dưới đây viết tay sau khi đọc số liệu; phần còn lại do `PlaytestLogTests` sinh. Chạy lại test sẽ ghi đè cả file.

### Phát hiện tổng hợp

| # | Mức | Phát hiện | Bằng chứng | Đề xuất (chưa làm) |
| --- | --- | --- | --- | --- |
| 1 | Cao | Người chơi mới không thắng nổi run nào: 10/10 run User bị trục xuất ở Mùa 5–6, tối đa 3 trận thắng; 58/87 lượt nung (67%) ra phế phẩm | Bảng User; Mùa 4+ chủ yếu „bỏ qua lượt” vì hết nguyên liệu | Xem #2, #3 |
| 2 | Cao | Vòng chết kinh tế: 0 vàng khởi đầu, mỗi lần thử sai đốt nguyên liệu khởi đầu không hoàn lại; phế phẩm bán 1 vàng. Thử nghiệm (cốt lõi của game) bị phạt nặng nhất đúng lúc người chơi chưa biết gì | User: Mùa 2–3 cạn Túi, không còn vàng để mua | Phế phẩm bán theo một phần giá nguyên liệu; hoặc vàng khởi đầu nhỏ; hoặc Chợ Mùa 1 có hàng miễn phí |
| 3 | Cao | Khám phá công thức quá chậm: sau 10 run User chỉ biết 2 công thức (Gang, Thép). Không có gợi ý nào dẫn tới Đồng thau, Hợp kim bạc… | Cột „Công thức đã biết” | Gợi ý trong Chiếu chỉ / mô tả vật phẩm („Đồng thau cần đồng và thiếc”); Sổ hiện công thức đã *thấy* (không chỉ đã nung) |
| 4 | Trung bình | Bộ khởi đầu mở sau cùng („Nhà luyện kim”, 120 Danh vọng) yếu hơn bộ đầu: GreedyAgent thắng 30% so với 59% ở Thường. Phần thưởng mở khóa là bước lùi | Bảng tổng hợp GreedyAgent | Thêm sắt/than cho bộ này, hoặc đổi thứ tự mốc |
| 5 | Trung bình | Phụ gia nung ra (Muối huyền, Bụi sao) dâng được nhưng Quality 0 → Power 0 → chắc thua; người chơi chỉ thấy „tier 5/7” nên dễ dâng nhầm | `RunStateMachine.Offer` chỉ kiểm `IsBrewed`; bảng công thức Q 0 | Chỉ cho dâng khoáng (Kind Mineral), hoặc ẩn phụ gia khỏi danh sách dâng |
| 6 | Trung bình | Trần tier 6 trong 8 Mùa kể cả với bot biết hết: tier 10 = 0%. Tinh thiết (tier 7) rẻ nhưng cần Thép tôi đã Kích hoạt ma thuật (≤ 60% mỗi lần nung, phải nung lại Thép tôi) | Bảng GreedyAgent: tier TB 5,7–6,0 | Khớp GDD („chủ yếu ở Endless”); cần quyết có giữ không |
| 7 | Thấp | Gang lãi 0 (bán 6 = chi phí 6), Giáp hắc diện lỗ 10: hai công thức là bẫy kinh tế | Bảng công thức | Nâng giá thị trường hai món này |
| 8 | Thấp | Mức sao sau khi giảm Power (M6) bị nén: Mùa 1–3 bậc Thường đều ★, Mùa 8 chỉ ★★★★. `starThresholds` 10/20/35/55 chưa chỉnh theo | Bảng đối thủ | Hạ ngưỡng sao, ví dụ 6/12/24/40 |
| 9 | Thấp | Danh vọng mở hết khóa quá nhanh: User (thua mọi run) cần 4 run để mở hết; meta không còn mục tiêu sau ~1 giờ chơi | Bảng tiến trình mở khóa | Thêm mốc, hoặc tăng mốc khi có thêm content |


## Vai User — 10 run liên tiếp

Người chơi mới: không biết công thức hay khoảng cách, tự thử ghép rồi nhớ cái thành công qua các run. Chỉ thấy tên, tier, mức sao; dâng thứ tier thấp nhất khi đối thủ ≤ ★2, ngược lại dâng tier cao nhất. Mỗi run chọn bậc khó cao nhất và bộ khởi đầu mới nhất đã mở. `·` = ô trống.

| Run | Bậc khó | Bộ khởi đầu | Kết quả | Trận thắng | Lượt nung (hỏng) | Danh vọng | Công thức đã biết |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Thường | Thợ rèn sắt | Trục xuất Mùa 5 | 2 | 10 (7) | +30 → 30 | 1 |
| 2 | Thường | Thợ rèn sắt | Trục xuất Mùa 6 | 3 | 11 (6) | +45 → 75 | 2 |
| 3 | Thường | Thợ đúc đồng | Trục xuất Mùa 5 | 2 | 9 (7) | +35 → 110 | 2 |
| 4 | Thường | Thợ đúc đồng | Trục xuất Mùa 6 | 3 | 11 (7) | +45 → 155 | 2 |
| 5 | Thường | Nhà luyện kim | Trục xuất Mùa 5 | 2 | 8 (6) | +35 → 190 | 2 |
| 6 | Thường | Nhà luyện kim | Trục xuất Mùa 5 | 2 | 7 (5) | +35 → 225 | 2 |
| 7 | Thường | Nhà luyện kim | Trục xuất Mùa 5 | 2 | 7 (5) | +35 → 260 | 2 |
| 8 | Thường | Nhà luyện kim | Trục xuất Mùa 6 | 3 | 8 (5) | +45 → 305 | 2 |
| 9 | Thường | Nhà luyện kim | Trục xuất Mùa 5 | 2 | 8 (5) | +35 → 340 | 2 |
| 10 | Thường | Nhà luyện kim | Trục xuất Mùa 5 | 2 | 8 (5) | +35 → 375 | 2 |

### Run 1 — Thường, Thợ rèn sắt, seed 1001

- **Mùa 1** · Hiệp sĩ làng Đông ★: dâng Gang → Giáp → **Thắng** (Uy tín 3). Nung: Thiếc Than đá → Phế phẩm; Hàn the · · · Than đá → Phế phẩm; Quặng sắt Than đá → Gang
- **Mùa 2** · Vệ binh bến sông ★: dâng Gang → Khiên → **Thắng** (Uy tín 3). Nung: Quặng sắt · · · Than đá → Phế phẩm; Thiên thạch · · · Quặng đồng → Phế phẩm; Quặng sắt Than đá → Gang
- **Mùa 3** · Thủ lĩnh sơn tặc ★: dâng Gang → Kiếm → **Thua** (Uy tín 2). Nung: Quặng sắt Than đá → Gang; Quặng đồng · · · Quặng sắt → Phế phẩm; Quặng sắt · · · Thiên thạch → Phế phẩm
- **Mùa 4** · Quán quân trấn Tây ★★: không có gì để dâng → **Thua** (Uy tín 1). Nung: bỏ qua lượt; bỏ qua lượt; Than đá Than đá → Phế phẩm
- **Mùa 5** · Quân biên ải ★★: không có gì để dâng → **Thua** (Uy tín 0). Nung: bỏ qua lượt; bỏ qua lượt; bỏ qua lượt

**Chung cuộc:** Trục xuất Mùa 5, thắng 2/8 trận, tier cao nhất 2, +30 Danh vọng (tổng 30).

### Run 2 — Thường, Thợ rèn sắt, seed 1002

- **Mùa 1** · Hiệp sĩ làng Đông ★: dâng Gang → Kiếm → **Thắng** (Uy tín 3). Nung: Quặng sắt Than đá → Gang; Quặng sắt Than đá → Gang; Quặng sắt · · Than đá → Phế phẩm
- **Mùa 2** · Vệ binh bến sông ★: dâng Thép → Kiếm → **Thắng** (Uy tín 3). Nung: Quặng sắt · Than đá → Thép; Quặng sắt Hàn the Than đá → Thép; Quặng đồng · · · Quặng đồng → Phế phẩm
- **Mùa 3** · Thủ lĩnh sơn tặc ★: dâng Thép → Khiên → **Thắng** (Uy tín 3). Nung: Than đá · Thiên thạch → Phế phẩm; Than đá · · Than đá → Phế phẩm; Than đá Thiếc → Phế phẩm
- **Mùa 4** · Quán quân trấn Tây ★★: dâng Thép → Kiếm → **Thua** (Uy tín 2). Nung: Hắc diện thạch · · Thiên thạch → Phế phẩm; Quặng sắt · Than đá → Thép; bỏ qua lượt
- **Mùa 5** · Quân biên ải ★★: không có gì để dâng → **Thua** (Uy tín 1). Nung: bỏ qua lượt; bỏ qua lượt; bỏ qua lượt
- **Mùa 6** · Thiết đoàn phương Nam ★★★: không có gì để dâng → **Thua** (Uy tín 0). Nung: bỏ qua lượt; bỏ qua lượt; bỏ qua lượt

**Chung cuộc:** Trục xuất Mùa 6, thắng 3/8 trận, tier cao nhất 3, +45 Danh vọng (tổng 75), mở: Thợ đúc đồng.

### Run 3 — Thường, Thợ đúc đồng, seed 1003

- **Mùa 1** · Hiệp sĩ làng Đông ★: dâng Thép → Khiên → **Thắng** (Uy tín 3). Nung: Quặng sắt Bột thạch anh Than đá → Thép; Than đá · · Thiếc → Phế phẩm; Quặng sắt · Than đá → Thép
- **Mùa 2** · Vệ binh bến sông ★: dâng Thép → Khiên → **Thắng** (Uy tín 3). Nung: Thiếc · · · Thiên thạch → Phế phẩm; Thiếc · · · Than đá → Phế phẩm; Than đá · · Quặng đồng → Phế phẩm
- **Mùa 3** · Thủ lĩnh sơn tặc ★: không có gì để dâng → **Thua** (Uy tín 2). Nung: Quặng đồng Thiên thạch → Phế phẩm; Quặng đồng Quặng đồng → Phế phẩm; bỏ qua lượt
- **Mùa 4** · Quán quân trấn Tây ★★: không có gì để dâng → **Thua** (Uy tín 1). Nung: Than đá · Than đá → Phế phẩm; bỏ qua lượt; bỏ qua lượt
- **Mùa 5** · Quân biên ải ★★: không có gì để dâng → **Thua** (Uy tín 0). Nung: bỏ qua lượt; bỏ qua lượt; bỏ qua lượt

**Chung cuộc:** Trục xuất Mùa 5, thắng 2/8 trận, tier cao nhất 3, +35 Danh vọng (tổng 110), mở: Quặng vàng, Lò đôi.

### Run 4 — Thường, Thợ đúc đồng, seed 1004

- **Mùa 1** · Hiệp sĩ làng Đông ★: dâng Thép → Giáp → **Thắng** (Uy tín 3). Nung: Quặng sắt · Than đá → Thép; Quặng sắt · Than đá → Thép; Thiếc · · Thép → Phế phẩm
- **Mùa 2** · Vệ binh bến sông ★: dâng Thép → Giáo → **Thắng** (Uy tín 3). Nung: Than đá · · · Thiên thạch → Phế phẩm; Quặng sắt · Than đá → Thép; Thiếc · · Bột thạch anh → Phế phẩm
- **Mùa 3** · Thủ lĩnh sơn tặc ★: dâng Thép → Kiếm → **Thắng** (Uy tín 3). Nung: Quặng đồng · Quặng đồng → Phế phẩm; Than đá · · · Thiếc → Phế phẩm; Quặng sắt · Than đá → Thép
- **Mùa 4** · Quán quân trấn Tây ★★: không có gì để dâng → **Thua** (Uy tín 2). Nung: Thiên thạch · · Thiên thạch → Phế phẩm; Quặng đồng · · Quặng đồng → Phế phẩm; bỏ qua lượt
- **Mùa 5** · Quân biên ải ★★: không có gì để dâng → **Thua** (Uy tín 1). Nung: bỏ qua lượt; bỏ qua lượt; bỏ qua lượt
- **Mùa 6** · Thiết đoàn phương Nam ★★★: không có gì để dâng → **Thua** (Uy tín 0). Nung: bỏ qua lượt; bỏ qua lượt; bỏ qua lượt

**Chung cuộc:** Trục xuất Mùa 6, thắng 3/8 trận, tier cao nhất 3, +45 Danh vọng (tổng 155), mở: Nhà luyện kim, Xúc tác: Búa thợ cả.

### Run 5 — Thường, Nhà luyện kim, seed 1005

- **Mùa 1** · Hiệp sĩ làng Đông ★: dâng Thép → Giáo → **Thắng** (Uy tín 3). Nung: Quặng sắt Thần sa Than đá → Phế phẩm; Bạc · · · Quặng sắt → Phế phẩm; Quặng sắt · Than đá → Thép
- **Mùa 2** · Vệ binh bến sông ★: dâng Thép → Giáp → **Thắng** (Uy tín 3). Nung: Quặng sắt · Than đá → Thép; Than đá · · Thần sa → Phế phẩm; Than đá · · · Than đá → Phế phẩm
- **Mùa 3** · Thủ lĩnh sơn tặc ★: không có gì để dâng → **Thua** (Uy tín 2). Nung: Thiên thạch · · · Than đá → Phế phẩm; Thiên thạch · Than đá → Phế phẩm; bỏ qua lượt
- **Mùa 4** · Quán quân trấn Tây ★★: không có gì để dâng → **Thua** (Uy tín 1). Nung: bỏ qua lượt; bỏ qua lượt; bỏ qua lượt
- **Mùa 5** · Quân biên ải ★★: không có gì để dâng → **Thua** (Uy tín 0). Nung: bỏ qua lượt; bỏ qua lượt; bỏ qua lượt

**Chung cuộc:** Trục xuất Mùa 5, thắng 2/8 trận, tier cao nhất 3, +35 Danh vọng (tổng 190).

### Run 6 — Thường, Nhà luyện kim, seed 1006

- **Mùa 1** · Hiệp sĩ làng Đông ★: dâng Thép → Giáp → **Thắng** (Uy tín 3). Nung: Quặng sắt · Than đá → Thép; Quặng sắt · Than đá → Thép; Quặng sắt · Hàn the · Than đá → Phế phẩm
- **Mùa 2** · Vệ binh bến sông ★: dâng Thép → Giáo → **Thắng** (Uy tín 3). Nung: Quặng sắt · Bạc → Phế phẩm; Than đá · · Thiên thạch → Phế phẩm; Thần sa · Hàn the → Phế phẩm
- **Mùa 3** · Thủ lĩnh sơn tặc ★: không có gì để dâng → **Thua** (Uy tín 2). Nung: Thần sa · Thiên thạch → Phế phẩm; bỏ qua lượt; bỏ qua lượt
- **Mùa 4** · Quán quân trấn Tây ★★: không có gì để dâng → **Thua** (Uy tín 1). Nung: bỏ qua lượt; bỏ qua lượt; bỏ qua lượt
- **Mùa 5** · Quân biên ải ★★: không có gì để dâng → **Thua** (Uy tín 0). Nung: bỏ qua lượt; bỏ qua lượt; bỏ qua lượt

**Chung cuộc:** Trục xuất Mùa 5, thắng 2/8 trận, tier cao nhất 3, +35 Danh vọng (tổng 225).

### Run 7 — Thường, Nhà luyện kim, seed 1007

- **Mùa 1** · Hiệp sĩ làng Đông ★: dâng Thép → Kiếm → **Thắng** (Uy tín 3). Nung: Quặng sắt · Than đá → Thép; Quặng sắt · Than đá → Thép; Quặng sắt · Bột thạch anh · Than đá → Phế phẩm
- **Mùa 2** · Vệ binh bến sông ★: dâng Thép → Khiên → **Thắng** (Uy tín 3). Nung: Bạc · · · Thần sa → Phế phẩm; Than đá Thần sa → Phế phẩm; Thiên thạch Than đá → Phế phẩm
- **Mùa 3** · Thủ lĩnh sơn tặc ★: không có gì để dâng → **Thua** (Uy tín 2). Nung: Hàn the · · Thiên thạch → Phế phẩm; bỏ qua lượt; bỏ qua lượt
- **Mùa 4** · Quán quân trấn Tây ★★: không có gì để dâng → **Thua** (Uy tín 1). Nung: bỏ qua lượt; bỏ qua lượt; bỏ qua lượt
- **Mùa 5** · Quân biên ải ★★: không có gì để dâng → **Thua** (Uy tín 0). Nung: bỏ qua lượt; bỏ qua lượt; bỏ qua lượt

**Chung cuộc:** Trục xuất Mùa 5, thắng 2/8 trận, tier cao nhất 3, +35 Danh vọng (tổng 260).

### Run 8 — Thường, Nhà luyện kim, seed 1008

- **Mùa 1** · Hiệp sĩ làng Đông ★: dâng Thép → Giáo → **Thắng** (Uy tín 3). Nung: Quặng sắt · Than đá → Thép; Quặng sắt · Than đá → Thép; Quặng sắt · Than đá → Thép
- **Mùa 2** · Vệ binh bến sông ★: dâng Thép → Kiếm → **Thắng** (Uy tín 3). Nung: Bạc · · Hàn the → Phế phẩm; Thiên thạch · Thần sa → Phế phẩm; Than đá · Hàn the → Phế phẩm
- **Mùa 3** · Thủ lĩnh sơn tặc ★: dâng Thép → Giáo → **Thắng** (Uy tín 3). Nung: Thiên thạch · · · Thần sa → Phế phẩm; bỏ qua lượt; bỏ qua lượt
- **Mùa 4** · Quán quân trấn Tây ★★: không có gì để dâng → **Thua** (Uy tín 2). Nung: Than đá Thiên thạch → Phế phẩm; bỏ qua lượt; bỏ qua lượt
- **Mùa 5** · Quân biên ải ★★: không có gì để dâng → **Thua** (Uy tín 1). Nung: bỏ qua lượt; bỏ qua lượt; bỏ qua lượt
- **Mùa 6** · Thiết đoàn phương Nam ★★★: không có gì để dâng → **Thua** (Uy tín 0). Nung: bỏ qua lượt; bỏ qua lượt; bỏ qua lượt

**Chung cuộc:** Trục xuất Mùa 6, thắng 3/8 trận, tier cao nhất 3, +45 Danh vọng (tổng 305).

### Run 9 — Thường, Nhà luyện kim, seed 1009

- **Mùa 1** · Hiệp sĩ làng Đông ★: dâng Thép → Giáp → **Thắng** (Uy tín 3). Nung: Quặng sắt · Than đá → Thép; Quặng sắt · Than đá → Thép; Quặng sắt · Than đá → Thép
- **Mùa 2** · Vệ binh bến sông ★: dâng Thép → Giáp → **Thắng** (Uy tín 3). Nung: Thiên thạch · · · Bạc → Phế phẩm; Thần sa · Thép → Phế phẩm; Thần sa · Than đá → Phế phẩm
- **Mùa 3** · Thủ lĩnh sơn tặc ★: không có gì để dâng → **Thua** (Uy tín 2). Nung: Bột thạch anh Than đá → Phế phẩm; Hàn the · Thiên thạch → Phế phẩm; bỏ qua lượt
- **Mùa 4** · Quán quân trấn Tây ★★: không có gì để dâng → **Thua** (Uy tín 1). Nung: bỏ qua lượt; bỏ qua lượt; bỏ qua lượt
- **Mùa 5** · Quân biên ải ★★: không có gì để dâng → **Thua** (Uy tín 0). Nung: bỏ qua lượt; bỏ qua lượt; bỏ qua lượt

**Chung cuộc:** Trục xuất Mùa 5, thắng 2/8 trận, tier cao nhất 3, +35 Danh vọng (tổng 340).

### Run 10 — Thường, Nhà luyện kim, seed 1010

- **Mùa 1** · Hiệp sĩ làng Đông ★: dâng Thép → Giáp → **Thắng** (Uy tín 3). Nung: Quặng sắt · Than đá → Thép; Quặng sắt · Than đá → Thép; Quặng sắt · Than đá → Thép
- **Mùa 2** · Vệ binh bến sông ★: dâng Thép → Kiếm → **Thắng** (Uy tín 3). Nung: Than đá · Thép → Phế phẩm; Thiên thạch Bột thạch anh → Phế phẩm; Thần sa · · · Bạc → Phế phẩm
- **Mùa 3** · Thủ lĩnh sơn tặc ★: không có gì để dâng → **Thua** (Uy tín 2). Nung: Thần sa · · · Thiên thạch → Phế phẩm; bỏ qua lượt; bỏ qua lượt
- **Mùa 4** · Quán quân trấn Tây ★★: không có gì để dâng → **Thua** (Uy tín 1). Nung: Than đá · · · Hàn the → Phế phẩm; bỏ qua lượt; bỏ qua lượt
- **Mùa 5** · Quân biên ải ★★: không có gì để dâng → **Thua** (Uy tín 0). Nung: bỏ qua lượt; bỏ qua lượt; bỏ qua lượt

**Chung cuộc:** Trục xuất Mùa 5, thắng 2/8 trận, tier cao nhất 3, +35 Danh vọng (tổng 375).


## Vai Tester — phân tích khả năng với data hiện tại

Phát hiện tổng hợp ở đầu file. Phần dưới là dữ liệu: data hiện tại cho phép làm gì, cần gì, và GreedyAgent (cận trên của người chơi đã biết hết công thức) đạt tới đâu.

### Cây công thức và kinh tế

Giá ở hệ số Mùa 100%, giá bán = 75% giá thị trường. Chi phí = tổng giá mua nguyên liệu gốc (đệ quy). Quality cơ bản = BaseEssence (Potency 1, chưa có Xúc tác). Power ước lượng = Quality + kỳ vọng thuộc tính × trọng số, khoảng theo loại trang bị. `*` = slot cần Kích hoạt ma thuật.

| Thành phẩm | Tier | Pattern | Quality | Power ước lượng | Chi phí | Giá bán | Lãi | Ma thuật tối đa khi nung |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Gang | 2 | Quặng sắt Than đá | 6 | 6–6 | 6 | 6 | 0 | 0% (không có phụ gia hợp lệ) |
| Thép | 3 | Quặng sắt · Than đá | 10 | 10–15 | 6 | 12 | +6 | 20% (phụ gia, chưa tính truyền thừa) |
| Đồng thau | 3 | Quặng đồng · · Thiếc | 12 | 12–17 | 10 | 13 | +3 | 40% (phụ gia, chưa tính truyền thừa) |
| Muối huyền | 5 | Thần sa · · Bột thạch anh | 0 | 0–0 | 22 | 30 | +8 | 0% (không có phụ gia hợp lệ) |
| Hợp kim bạc | 5 | Đồng thau · Bạc | 28 | 28–38 | 22 | 33 | +11 | 30% (phụ gia, chưa tính truyền thừa) |
| Thép tôi | 5 | Thép · Than đá · Thép | 30 | 30–43 | 14 | 37 | +23 | 60% (phụ gia, chưa tính truyền thừa) |
| Hợp kim vàng | 5 | Quặng vàng · Bạc | 30 | 30–36 | 26 | 36 | +10 | 30% (phụ gia, chưa tính truyền thừa) |
| Thép thiên thạch | 6 | Thiên thạch · Thép | 45 | 48–59 | 31 | 45 | +14 | 30% (phụ gia, chưa tính truyền thừa) |
| Giáp hắc diện | 6 | Hắc diện thạch · · Hợp kim bạc | 55 | 55–74 | 62 | 52 | -10 | 60% (phụ gia, chưa tính truyền thừa) |
| Bụi sao | 7 | Thiên thạch · Muối huyền | 0 | 0–0 | 47 | 90 | +43 | 0% (không có phụ gia hợp lệ) |
| Bạch ngân tinh | 7 | Quặng bạch ngân · Hợp kim bạc | 65 | 69–89 | 82 | 105 | +23 | 30% (phụ gia, chưa tính truyền thừa) |
| Tinh thiết | 7 | Thép tôi* · · Bạc | 70 | 70–97 | 26 | 112 | +86 | 80% (phụ gia, chưa tính truyền thừa) |
| Sơn đồng | 8 | Thép thiên thạch* · · · Giáp hắc diện | 120 | 138–156 | 93 | 187 | +94 | 90% (phụ gia, chưa tính truyền thừa) |
| Kim cương thiết | 9 | Tinh thiết* · Bạch ngân tinh* | 180 | 212–230 | 108 | 300 | +192 | 50% (phụ gia, chưa tính truyền thừa) |
| Khoáng thạch Ma thuật Tối thượng | 10 | Kim cương thiết* · · Sơn đồng* | 300 | 360–360 | 201 | 600 | +399 | 90% (phụ gia, chưa tính truyền thừa) |

Giá bán toàn bộ bộ khởi đầu (vốn tối đa lúc đầu, vàng khởi đầu 0): Thợ rèn sắt 33 · Thợ đúc đồng 42 · Nhà luyện kim 57 vàng.

### Đối thủ từng Mùa so với thành phẩm

„Rẻ nhất đủ thắng chắc” = thành phẩm chi phí thấp nhất có Quality cơ bản ≥ Power (thắng với mọi loại trang bị, không cần thuộc tính).

**Thường** (Danh vọng ×100%)

| Mùa | Đối thủ | Loại | Power | Sao | Rẻ nhất đủ thắng chắc |
| --- | --- | --- | --- | --- | --- |
| 1 | Hiệp sĩ làng Đông | Thi đấu | 4 | ★ | Gang (tier 2, Q 6, chi phí 6) |
| 2 | Vệ binh bến sông | Thi đấu | 6 | ★ | Gang (tier 2, Q 6, chi phí 6) |
| 3 | Thủ lĩnh sơn tặc | Chiến tranh | 8 | ★ | Thép (tier 3, Q 10, chi phí 6) |
| 4 | Quán quân trấn Tây | Thi đấu | 12 | ★★ | Đồng thau (tier 3, Q 12, chi phí 10) |
| 5 | Quân biên ải | Chiến tranh | 17 | ★★ | Thép tôi (tier 5, Q 30, chi phí 14) |
| 6 | Thiết đoàn phương Nam | Chiến tranh | 23 | ★★★ | Thép tôi (tier 5, Q 30, chi phí 14) |
| 7 | Kiếm khách ngự tiền | Thi đấu | 32 | ★★★ | Tinh thiết (tier 7, Q 70, chi phí 26) |
| 8 | Đại quân phương Bắc | Đại chiến | 49 | ★★★★ | Tinh thiết (tier 7, Q 70, chi phí 26) |

**Khó** (Danh vọng ×150%)

| Mùa | Đối thủ | Loại | Power | Sao | Rẻ nhất đủ thắng chắc |
| --- | --- | --- | --- | --- | --- |
| 1 | Kỵ sĩ đồi Thông | Thi đấu | 5 | ★ | Gang (tier 2, Q 6, chi phí 6) |
| 2 | Đội tuần biên | Thi đấu | 8 | ★ | Thép (tier 3, Q 10, chi phí 6) |
| 3 | Băng cướp Hắc Lĩnh | Chiến tranh | 12 | ★★ | Đồng thau (tier 3, Q 12, chi phí 10) |
| 4 | Quán quân kinh thành | Thi đấu | 16 | ★★ | Thép tôi (tier 5, Q 30, chi phí 14) |
| 5 | Quân phiên trấn | Chiến tranh | 23 | ★★★ | Thép tôi (tier 5, Q 30, chi phí 14) |
| 6 | Thiết kỵ phương Tây | Chiến tranh | 31 | ★★★ | Tinh thiết (tier 7, Q 70, chi phí 26) |
| 7 | Ngự lâm kiếm sư | Thi đấu | 44 | ★★★★ | Tinh thiết (tier 7, Q 70, chi phí 26) |
| 8 | Liên quân Bắc Địa | Đại chiến | 65 | ★★★★★ | Tinh thiết (tier 7, Q 70, chi phí 26) |

**Cực khó** (Danh vọng ×200%)

| Mùa | Đối thủ | Loại | Power | Sao | Rẻ nhất đủ thắng chắc |
| --- | --- | --- | --- | --- | --- |
| 1 | Hiệp khách Lạc Sơn | Thi đấu | 6 | ★ | Gang (tier 2, Q 6, chi phí 6) |
| 2 | Thủy quân sông Cái | Chiến tranh | 9 | ★ | Thép (tier 3, Q 10, chi phí 6) |
| 3 | Chúa núi Tản Viên | Chiến tranh | 13 | ★★ | Thép tôi (tier 5, Q 30, chi phí 14) |
| 4 | Đệ nhất võ quan | Thi đấu | 18 | ★★ | Thép tôi (tier 5, Q 30, chi phí 14) |
| 5 | Quân Thiết Thành | Chiến tranh | 25 | ★★★ | Thép tôi (tier 5, Q 30, chi phí 14) |
| 6 | Đại thủy quân phương Nam | Chiến tranh | 35 | ★★★★ | Tinh thiết (tier 7, Q 70, chi phí 26) |
| 7 | Kiếm thánh vô danh | Thi đấu | 50 | ★★★★ | Tinh thiết (tier 7, Q 70, chi phí 26) |
| 8 | Liên minh Tứ quốc | Đại chiến | 72 | ★★★★★ | Sơn đồng (tier 8, Q 120, chi phí 93) |

### Xúc tác

| Xúc tác | Tier | Giá | Mở bằng | Hiệu ứng |
| --- | --- | --- | --- | --- |
| Ống bễ | 1 | 10 | Chợ | Mỗi lần khớp công thức: +2 Tinh chất. |
| Rây xỉ | 1 | 8 | Chợ | Mỗi lượt ra phế phẩm: +2 vàng. |
| Đá mài | 2 | 14 | Chợ | Thép nung ra: +2 Sắc bén. |
| Nồi bạc | 2 | 16 | Chợ | Lượt nung có Bạc: +1 Hiệu lực. |
| Sàng đãi quặng | 2 | 12 | Chợ | Mỗi lần vào Chợ: 50% nhận 1 Than đá. |
| Lò đôi | 3 | 24 | Chợ, ≥ 80 Danh vọng | Mỗi lần khớp công thức: 25% Hiệu lực ×1,5. |
| Búa thợ cả | 4 | 40 | chiến lợi phẩm Mùa 4+, ≥ 150 Danh vọng | Mỗi lần khớp công thức: Hiệu lực ×1,25. |

### GreedyAgent (biết mọi công thức, nhìn Quality và Power)

12 run chi tiết: mỗi bậc khó × mỗi bộ khởi đầu (+3 run thêm ở Thường), mọi thứ đã mở khóa.

| # | Bậc khó | Bộ khởi đầu | Seed | Kết quả | Trận thắng | Tier cao nhất | Thắng/thua từng Mùa |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Thường | Thợ rèn sắt | 501 | Thắng run | 6 | 6 | `TTTTTTBB` |
| 2 | Thường | Thợ đúc đồng | 502 | Thắng run | 7 | 6 | `TTTTTTTB` |
| 3 | Thường | Nhà luyện kim | 503 | Trục xuất Mùa 4 | 1 | 5 | `TBBB` |
| 4 | Khó | Thợ rèn sắt | 504 | Trục xuất Mùa 7 | 4 | 6 | `TTTTBBB` |
| 5 | Khó | Thợ đúc đồng | 505 | Trục xuất Mùa 6 | 3 | 5 | `TTTBBB` |
| 6 | Khó | Nhà luyện kim | 506 | Trục xuất Mùa 4 | 1 | 5 | `TBBB` |
| 7 | Cực khó | Thợ rèn sắt | 507 | Trục xuất Mùa 6 | 3 | 6 | `TTTBBB` |
| 8 | Cực khó | Thợ đúc đồng | 508 | Trục xuất Mùa 8 | 5 | 6 | `TTTTTBBB` |
| 9 | Cực khó | Nhà luyện kim | 509 | Trục xuất Mùa 5 | 2 | 5 | `TTBBB` |
| 10 | Thường | Thợ rèn sắt | 510 | Trục xuất Mùa 8 | 5 | 6 | `TTTTTBBB` |
| 11 | Thường | Thợ đúc đồng | 511 | Thắng run | 6 | 5 | `TTTTTBTB` |
| 12 | Thường | Nhà luyện kim | 512 | Thắng run | 7 | 6 | `TTTTTTBT` |

`T` = thắng, `B` = thua trận của Mùa đó.

Tổng hợp 300 run mỗi ô (bộ khởi đầu × bậc khó, đã mở hết khóa):

| Bộ khởi đầu | Thường | Khó | Cực khó |
| --- | --- | --- | --- |
| Thợ rèn sắt | thắng 59% · tier TB 6.0 · DV 105 | thắng 30% · tier TB 6.0 · DV 133 | thắng 10% · tier TB 6.0 · DV 144 |
| Thợ đúc đồng | thắng 58% · tier TB 5.9 · DV 103 | thắng 31% · tier TB 5.8 · DV 130 | thắng 15% · tier TB 5.8 · DV 148 |
| Nhà luyện kim | thắng 30% · tier TB 5.8 · DV 78 | thắng 18% · tier TB 5.8 · DV 104 | thắng 11% · tier TB 5.7 · DV 127 |

### Tiến trình mở khóa

Danh vọng TB mỗi run: User 38 · GreedyAgent (Thường, bộ đầu) 106.

| Mốc | Danh vọng | Số run cần (User) | Số run cần (Greedy) |
| --- | --- | --- | --- |
| Thợ đúc đồng | 40 | 2 | 1 |
| Lò đôi vào Chợ | 80 | 3 | 1 |
| Quặng vàng vào Chợ | 100 | 3 | 1 |
| Nhà luyện kim | 120 | 4 | 2 |
| Xúc tác: Búa thợ cả | 150 | 4 | 2 |

