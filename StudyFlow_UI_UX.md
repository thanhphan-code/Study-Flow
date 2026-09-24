# StudyFlow UI/UX

## 1. Mục đích tài liệu

Tài liệu này mô tả trải nghiệm người dùng và hệ thống giao diện hiện có của StudyFlow. Đây là nguồn tham chiếu cho việc phát triển frontend, review thiết kế và giữ trải nghiệm nhất quán khi bổ sung tính năng.

Phạm vi gồm:

- Kiến trúc thông tin và điều hướng.
- Luồng học tập chính.
- Ngôn ngữ thiết kế và component dùng chung.
- Trạng thái loading, empty, error và success.
- Responsive, accessibility và song ngữ.
- Source Grounding trong giao diện.
- Các vấn đề UX hiện tại và hướng cải thiện.

---

## 2. Định hướng trải nghiệm

StudyFlow không nên tạo cảm giác như một tập hợp nhiều công cụ học rời rạc. Trải nghiệm mục tiêu là:

```text
Mở ứng dụng
    ↓
Biết nội dung quan trọng nhất cần học
    ↓
Bắt đầu trong ít thao tác
    ↓
Nhận phản hồi rõ ràng sau mỗi câu trả lời
    ↓
Hệ thống tự cập nhật tiến độ và lần ôn tiếp theo
```

Các nguyên tắc chính:

1. **Learning-first**: hành động học quan trọng hơn hành động quản trị dữ liệu.
2. **Một bước tiếp theo rõ ràng**: mỗi màn hình nên có một CTA chính.
3. **Progressive disclosure**: chỉ hiện chi tiết nâng cao khi người dùng cần.
4. **Phản hồi tức thời**: mọi thao tác bất đồng bộ phải có trạng thái rõ ràng.
5. **Không làm gián đoạn phiên học**: giao diện học tập giảm tối đa điều hướng và yếu tố gây nhiễu.
6. **Có thể kiểm chứng**: nội dung AI sinh từ tài liệu phải cho phép xem nguồn.
7. **Mobile-first**: thao tác chính phải thuận tiện bằng một tay và không phụ thuộc hover.

---

## 3. Kiến trúc thông tin

### 3.1. Điều hướng cấp cao

StudyFlow hiện có năm khu vực chính:

| Khu vực | Route | Vai trò |
|---|---|---|
| Home | `/home` | Tổng quan hôm nay và điểm bắt đầu học nhanh |
| Subjects | `/subjects` | Quản lý môn học và bộ học |
| Due Review | `/reviews/due` | Ôn các thẻ đến hạn |
| Progress | `/progress` | Xem lịch sử và mức độ tiến bộ |
| Documents | `/documents` | Quản lý nguồn tài liệu và trạng thái xử lý |

Desktop dùng thanh điều hướng trên cùng. Mobile dùng bottom navigation gồm năm mục tương ứng.

### 3.2. Cấu trúc nội dung

```text
User
├── Home
├── Subjects
│   └── Subject
│       ├── Standard Study Set
│       │   ├── Flashcards
│       │   ├── Quiz
│       │   ├── Flashcard Study
│       │   └── Smart Learn
│       └── Combined Study Set
│           └── Study Together
├── Study Today
├── Due Review
├── Progress
└── Documents
    └── Document Detail
```

### 3.3. Focus mode

Các màn hình học và kiểm tra ẩn navigation chung để giảm mất tập trung:

- Flashcard Study.
- Smart Learn.
- Daily Study.
- Study Together.
- Quiz.
- Login và Register.

Mỗi focus mode phải luôn có:

- Nút thoát rõ ràng.
- Tiến độ hiện tại.
- Hành động chính nổi bật.
- Cơ chế hoàn tất phiên an toàn.

---

## 4. Các luồng người dùng chính

### 4.1. Luồng bắt đầu học hằng ngày

```text
Home
  ↓
Study Today
  ↓
Chọn thời lượng và trọng tâm
  ↓
Trả lời từng hoạt động
  ↓
Nhận phản hồi
  ↓
Tự đánh giá mức nhớ
  ↓
Learning Engine cập nhật tiến độ
```

Yêu cầu UX:

- Thời lượng mặc định là 15 phút.
- Người dùng không phải tự chọn từng bộ học.
- Lý do một thẻ được đề xuất nên hiển thị ngắn gọn.
- Sau khi trả lời, đáp án của người dùng và đáp án chuẩn phải dễ so sánh.
- Không cho chuyển tiếp khi kết quả chưa được ghi nhận.

### 4.2. Luồng Smart Learn

```text
Study Set
  ↓
Smart Learn
  ↓
Recognition
  ↓
Active Recall
  ↓
Delayed / Reverse Recall
  ↓
Session Summary
```

Mỗi item bao gồm:

- Nhãn giai đoạn học.
- Nội dung cần nhớ.
- Trường nhập hoặc danh sách lựa chọn.
- Mức tự tin từ 1 đến 5.
- Hint khi phù hợp.
- Phản hồi `Correct`, `Almost correct` hoặc `Needs work`.
- Giải thích về misconception khi câu trả lời sai nhưng độ tự tin cao.

### 4.3. Luồng flashcard

Flashcard Study có hai chế độ:

- **Browse**: xem tự do, không ghi nhận kết quả.
- **Memory Review**: lật thẻ và đánh giá Again, Hard, Good hoặc Easy.

Với flashcard ngoại ngữ, trải nghiệm hỗ trợ thêm:

- Phát âm.
- Tốc độ đọc.
- Reading và romanization.
- Reverse recall.
- Ví dụ và mẹo nhớ.

### 4.4. Luồng quiz

```text
Quiz intro
  ↓
Làm từng câu
  ↓
Có thể quay lại sửa đáp án
  ↓
Chỉ cho nộp khi đã trả lời đủ
  ↓
Điểm và danh sách câu sai
  ↓
Xem lời giải và nguồn
```

Đáp án đúng không được hiển thị trước khi người dùng nộp bài.

### 4.5. Luồng tài liệu và AI

```text
Upload document
  ↓
Uploaded / Queued / Processing
  ↓
Ready
  ↓
Generate AI draft
  ↓
Review và chỉnh sửa
  ↓
Save
  ↓
Flashcard / Quiz có SourceReference
```

Không cho dùng tài liệu chưa ở trạng thái `Ready` để sinh nội dung grounded.

---

## 5. Hệ thống giao diện

### 5.1. Visual language

Giao diện hiện dùng phong cách sáng, nhẹ và tập trung vào nội dung:

- Nền xanh xám rất nhạt.
- Surface trắng bán trong suốt.
- Màu primary xanh dương.
- Góc bo lớn, thường từ `12px` đến `32px`.
- Shadow mềm, độ tương phản thấp.
- Màu trạng thái dùng nhất quán theo ngữ nghĩa.

### 5.2. Design tokens hiện có

```css
--ink: #152044;
--muted: #687493;
--blue: #3564df;
--blue-dark: #244db8;
--line: #e2e7f2;
--surface: rgba(255, 255, 255, .9);
--shadow: 0 18px 48px rgba(38, 56, 112, .09);
```

Ý nghĩa màu:

| Màu | Ý nghĩa |
|---|---|
| Blue / Indigo | Hành động chính, navigation, thông tin |
| Emerald | Đúng, hoàn thành, tài liệu còn khả dụng |
| Amber | Gần đúng, cảnh báo, hint |
| Red | Sai, lỗi hoặc hành động xóa |
| Slate | Nội dung phụ, disabled hoặc legacy state |

### 5.3. Typography

- Body: `Segoe UI Variable`, `Aptos`, system sans-serif.
- Heading: `Aptos Display`, `Segoe UI Variable Display`.
- Heading dùng tracking chặt và trọng lượng lớn.
- Số liệu dùng `font-variant-numeric: tabular-nums`.
- Nội dung dài dùng line-height rộng để tăng khả năng đọc.

### 5.4. Component nền tảng

#### `sf-card`

Surface chính cho dashboard, form, flashcard và summary.

#### `sf-primary`

CTA quan trọng nhất trên màn hình. Không nên có nhiều hơn một CTA primary trong cùng một vùng hành động.

#### Language switch

- Hiển thị `EN` và `VI`.
- Dùng `aria-pressed` để thể hiện trạng thái.
- Phải có mặt ở navigation chung và focus mode.

#### Progress indicator

- Dùng thanh ngang cho phiên học tuần tự.
- Luôn kèm chỉ số dạng `current / total`.
- Không chỉ dùng màu để truyền đạt trạng thái.

---

## 6. Source Grounding UX

Nội dung AI có nguồn hiển thị hành động `View source`.

Source Viewer hoạt động như:

- Bottom sheet trên mobile.
- Modal ở giữa màn hình trên desktop.
- Đóng bằng nút `×` hoặc click vùng nền.

Mỗi nguồn hiển thị:

- Tên tài liệu.
- Trang, slide, section hoặc offset.
- Đoạn trích authoritative từ backend.
- Trạng thái tài liệu còn khả dụng hay chỉ còn snapshot.
- Deep link mở tài liệu khi tài liệu vẫn tồn tại.

Quy tắc UX:

1. Không hiển thị page hoặc section do AI tự khai báo.
2. Không tạo link nếu tài liệu đã bị xóa.
3. Legacy content không có nguồn phải hiển thị trạng thái trung thực.
4. Source alias như `C1` chỉ phù hợp trong màn review AI draft; nội dung đã lưu phải hiển thị tên tài liệu có ý nghĩa.
5. Quiz chỉ hiện source sau khi đã chấm, tránh làm lộ đáp án.

---

## 7. Trạng thái giao diện bắt buộc

Mọi màn hình lấy dữ liệu phải xử lý đủ các trạng thái sau.

### Loading

- Dashboard và danh sách ưu tiên skeleton để tránh layout shift.
- Tác vụ ngắn có thể dùng label như `Preparing…` hoặc `Saving…`.
- Button đang xử lý phải disabled để chống submit lặp.

### Empty

Empty state cần trả lời ba câu hỏi:

1. Điều gì đang trống?
2. Vì sao điều này xảy ra?
3. Người dùng nên làm gì tiếp theo?

Ví dụ: khi Daily Study không có thẻ phù hợp, cung cấp đường quay lại và hướng dẫn thêm flashcard hoặc đổi chế độ.

### Error

- Lỗi phải xuất hiện gần hành động gây lỗi.
- Dùng `role="alert"` cho lỗi quan trọng.
- Không hiển thị stack trace hoặc lỗi provider thô.
- Tác vụ có thể thử lại phải có CTA retry.

### Success

- Thành công trong phiên học thể hiện bằng phản hồi tại chỗ.
- Thành công toàn phiên dùng summary riêng.
- Tránh toast cho thông tin người dùng cần đọc hoặc so sánh.

---

## 8. Responsive behavior

### Mobile

- Chiều rộng tối thiểu: `320px`.
- Bottom navigation nằm phía trên safe-area.
- Các CTA quan trọng nên có chiều cao tối thiểu khoảng `44px`.
- Modal nguồn chuyển thành bottom sheet.
- Form nhiều cột chuyển thành một cột.
- Nội dung học tập ưu tiên full-width.

### Desktop

- Navigation chuyển lên top bar từ breakpoint `900px`.
- Nội dung giới hạn chiều rộng từ khoảng `768px` đến `1152px` tùy màn hình.
- Form và dashboard có thể dùng grid nhiều cột.
- Focus mode vẫn giữ layout hẹp để giảm quãng đường mắt di chuyển.

---

## 9. Accessibility

Các khả năng đã có:

- Skip link tới nội dung chính.
- `focus-visible` rõ ràng.
- Semantic `main`, `nav`, `header`, `section` và `article`.
- `aria-pressed` cho language switch.
- `role="dialog"`, `aria-modal` cho Source Viewer.
- Hỗ trợ `prefers-reduced-motion`.
- Một số phiên học có phím tắt.

Các yêu cầu cần giữ khi phát triển:

- Không dùng icon đơn lẻ mà thiếu accessible name.
- Không truyền đạt đúng/sai chỉ bằng màu sắc.
- Mọi input phải có label hoặc accessible name.
- Dialog phải quản lý focus, khóa focus trong dialog và trả focus về trigger khi đóng.
- Phím `Escape` phải đóng modal hoặc bottom sheet.
- Kích thước vùng chạm tối thiểu khoảng `44 × 44px`.
- Kiểm tra contrast theo WCAG AA.
- Không tự phát âm nếu người dùng chưa bật tùy chọn.

---

## 10. Song ngữ và nội dung

StudyFlow hỗ trợ tiếng Anh và tiếng Việt.

Quy tắc nội dung:

- Không hardcode text mới trực tiếp nếu text đó xuất hiện ở nhiều màn hình.
- Bản dịch phải giữ cùng ý nghĩa và mức độ ưu tiên.
- Tránh trộn tiếng Anh và tiếng Việt ngoài thuật ngữ sản phẩm cần thiết.
- Ngày giờ nên format theo locale.
- Không ghép câu bằng nhiều fragment nếu thứ tự từ có thể khác giữa ngôn ngữ.
- Mọi file nguồn phải lưu UTF-8 và cần kiểm tra dấu tiếng Việt trong production build.

Tone of voice:

- Ngắn gọn.
- Khuyến khích nhưng không trẻ con hóa.
- Không tạo áp lực khi người dùng trả lời sai.
- Giải thích bước tiếp theo thay vì chỉ thông báo lỗi.

---

## 11. Vấn đề UX hiện tại

### P0 — cần xử lý trước khi production

1. **Source Viewer chưa có focus trap và Escape handling**: modal đã có semantic cơ bản nhưng keyboard navigation chưa hoàn chỉnh.
2. **Document deep link chưa highlight vị trí nguồn**: route nhận page, slide hoặc chunk nhưng Document Detail hiện mới hiển thị toàn bộ extracted text.
3. **Một số màn hình còn hardcode tiếng Anh**: Documents và một số error state chưa dùng hệ thống i18n chung.

### P1 — ảnh hưởng tính nhất quán

1. Loading state chưa đồng nhất: có nơi dùng skeleton, có nơi chỉ dùng text.
2. Các mode học dùng CTA, header và summary gần giống nhau nhưng chưa có component shell dùng chung.
3. Error handling phần lớn hiển thị inline nhưng chưa có retry pattern thống nhất.
4. Source alias trong AI draft mới cho biết `C1`, chưa cho preview nhanh metadata trước khi lưu.
5. Icon hiện dùng ký tự Unicode; độ hiển thị có thể khác nhau giữa hệ điều hành.

### P2 — cải thiện chất lượng

1. Chuẩn hóa spacing và radius thành token thay vì lặp utility tùy màn hình.
2. Thêm live region cho thay đổi trạng thái async quan trọng.
3. Thêm xác nhận trước thao tác xóa có ảnh hưởng lớn.
4. Thêm analytics cho CTA bắt đầu học, hoàn tất phiên và mở nguồn.
5. Kiểm tra usability với keyboard-only, screen reader và màn hình 320px.

---

## 12. Hướng hợp nhất trải nghiệm học

Các mode học khác nhau nên dùng một cấu trúc nhận thức thống nhất:

```text
Session Setup
    ↓
Prompt
    ↓
User Response
    ↓
Evaluation
    ↓
Explanation / Source
    ↓
Next Recommended Action
    ↓
Session Summary
```

Nên chuẩn hóa thành các UI primitive dùng chung:

- `LearningSessionShell`
- `SessionProgress`
- `LearningPrompt`
- `AnswerInput`
- `AnswerFeedback`
- `ConfidenceSelector`
- `SourceViewer`
- `SessionSummary`

Điều này giúp Flashcard Review, Daily Study, Smart Learn và Study Together trông khác nhau vừa đủ nhưng vẫn cho cảm giác cùng một hệ thống học.

---

## 13. Checklist cho màn hình mới

### Trước khi triển khai

- Màn hình phục vụ mục tiêu người dùng nào?
- CTA chính là gì?
- Route nằm ở navigation chung hay focus mode?
- Dữ liệu có loading, empty, error và stale state không?
- Có cần ghi nhận Learning Attempt không?
- Nội dung AI có cần Source Grounding không?

### Khi triển khai

- Mobile 320px không tràn ngang.
- Keyboard sử dụng được toàn bộ flow.
- Focus state nhìn thấy rõ.
- Button async chống submit lặp.
- Text có cả EN và VI.
- Màu sắc không phải tín hiệu duy nhất.
- Không hiển thị dữ liệu nhạy cảm hoặc lỗi kỹ thuật thô.

### Trước khi hoàn tất

- Frontend lint pass.
- TypeScript build pass.
- Kiểm tra Chrome desktop và mobile viewport.
- Kiểm tra reduced motion.
- Kiểm tra empty/error state.
- Kiểm tra thao tác bằng bàn phím.
- Kiểm tra source ownership và trạng thái tài liệu đã xóa nếu có grounding.

---

## 14. Definition of Done cho UI/UX

Một tính năng frontend được xem là hoàn tất khi:

```text
[ ] Luồng chính hoàn thành được trên mobile và desktop
[ ] Có loading, empty, error và success state phù hợp
[ ] CTA chính rõ ràng
[ ] Không submit lặp khi request đang chạy
[ ] Keyboard navigation hoạt động
[ ] Focus state hiển thị rõ
[ ] EN và VI đầy đủ
[ ] Responsive từ 320px
[ ] API error được chuyển thành thông báo an toàn
[ ] Nội dung grounded có View Source
[ ] Không làm lộ source hoặc content của user khác
[ ] Frontend lint pass
[ ] Production build pass
```

---

## 15. Kết luận

UI/UX của StudyFlow nên xoay quanh một lời hứa đơn giản:

> Người dùng luôn biết nên học gì tiếp theo, nhận phản hồi dễ hiểu và có thể kiểm chứng nội dung AI khi cần.

Việc hợp nhất cấu trúc phiên học, chuẩn hóa trạng thái giao diện và hoàn thiện accessibility quan trọng hơn việc tạo thêm giao diện riêng cho từng learning mode. Mỗi mode có thể khác về chiến lược học, nhưng phải cùng chia sẻ một ngôn ngữ tương tác và một hành trình học nhất quán.
