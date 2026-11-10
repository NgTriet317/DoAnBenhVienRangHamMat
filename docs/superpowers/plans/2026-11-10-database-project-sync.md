# Database Project Sync Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cập nhật SQL chính để hợp đồng kiểm schema tổng khớp dự án64 bảng hiện tại, kiểm cả khám dịch vụ/phụ thu và ảnh X-quang/phân công.

**Architecture:** Giữ nguyên ET/DAL/BUS/WPF và dữ liệu snapshot. Bổ sung định nghĩa cuối của `usp_RHM_KiemTraSchema` sau P4; mở rộng checker kho ảnh cho metadata đúng ET, trigger hoạt động và index đúng filter. Các pha nâng cấp có cờ hoàn tất để không in xác nhận cuối sau lỗi.

**Tech Stack:** SQL Server, sqlcmd, .NET8/Dapper, harness rollback và runner P6 hiện có.

**Spec/design:** Yêu cầu người dùng cập nhật `Database/QLBenhVienRangHamMat.sql` đồng bộ/liên kết với dự án; ET_ANHXQUANGLANKHAM, ET_PHANCONGANHXQUANG, DAL_TIEPNHAN/DAL_ANHXQUANG và cấu hình appsettings là nguồn đối chiếu. Database đang dùng chưa nâng cấp không có nghĩa file nguồn tự kết nối ứng dụng; ứng dụng kết nối SQL Server qua cấu hình.

## Global Constraints

- Chỉ sửa SQL nghiệp vụ trong file chính; không nạp lại snapshot từ DB đang dùng, không hồi tố phí/mã bệnh và không đổi cột ET.
- Không chạy migration lên `QLBenhVienRangHamMat`; chỉ database thử tên mới `RHM_Hospital_Test_...`. Giữ branch/working tree, không commit/push.
- Bảo toàn snapshot hiện có và fingerprint cột dữ liệu cũ khi nâng cấp/rerun. Không tắt trigger để migration chạy được.
- Giữ bộ chứng cứ P6/kit cũ nguyên trạng; nghiệm thu mới có mã/hash riêng. Hạ tầng hai máy vẫn chưa được kiểm.
- Một checker tổng sau cài đầy đủ phải từ chối thiếu bảo vệ phụ thu, thiếu bảng/phụ thuộc ảnh và sai cột ánh xạ ET.

## Review Focus

- Định nghĩa checker mới phải nằm sau mọi định nghĩa cũ và không tạo vòng gọi đệ quy.
- Cài mới62→64/rerun với chứng từ đã chốt không mất dữ liệu hay sinh phụ thu trùng.
- Kiểm trigger dựa trạng thái hoạt động, index dựa đúng filter; không chỉ dựa tên tồn tại.
- Pha lỗi trong chế độ SSMS tiếp tục batch không được xác nhận đồng bộ cuối; session flags không còn giá trị thành công cũ.
- Cấu hình mặc định và đích CREATE/USE trong SQL thống nhất tên, không ghi DB thật trong kiểm thử.

### Task 1: Tái hiện thiếu hợp đồng

- [x] Sao lưu file trước sửa/hash snapshot vào obj.
- [x] Thêm9 ca rollback: checker tổng thiếu trigger phí/bảng phân công/index audit; checker ảnh với trigger tắt/cột ET đổi tên/filter hoặc khóa index sai/FK sai bảng. Chạy RED trên DB thử đã cài bản trước.

### Task 2: Đồng bộ SQL chính

- [x] Cập nhật phần hướng dẫn đầu file và trạng thái các pha; mở rộng checker metadata/P4/audit; đặt checker tổng cuối file.
- [x] Chạy GREEN9 ca mới và các ca nền tảng cũ trên DB thử cài file mới.
- [x] Chạy toàn bộ runner P6 với DB mới: cài mới/nâng cấp62→64/rerun hai lần/ngắt, đối chiếu dữ liệu, ET/DAL, clinical/thu phí/ảnh/báo cáo và build.

### Task 3: Review và bàn giao

- [x] Rà soát độc lập, xử lý finding xác nhận và kiểm liên quan.
- [x] Ghi log/hash nguồn/kết quả và cách áp dụng bằng SSMS/sqlcmd; giải thích file SQL không tự cập nhật DB đang dùng. Nối README/tiến độ, giữ chứng cứ cũ.

## Execution Ledger

- Pre-flight: checker tổng cũ gọi SchemaNen/KeThuoc/DongBoV9/BaoTriSerial; checker P4 gọi nền tảng P1. Định nghĩa mới tổng→P4→P1, không gọi tổng từ P4.
- Ruling: cập nhật hợp đồng tổng ở cuối file thay vì nạp lại snapshot62 từ DB đang dùng. Giữ dữ liệu người dùng; nếu bỏ qua thứ tự định nghĩa có thể bị định nghĩa cũ ghi đè.
- Ruling: phạm vi là file SQL và kiểm thử, không tự áp dụng vào DB đang vận hành. Chạy DB thử mới để chứng minh; nếu coi sửa file là đã nâng cấp DB sẽ báo sai trạng thái kết nối.
- Task 1: complete — before.sql tại `obj/SqlProjectSync/20261110_122549`, SHA trước `C32BC5A606FE7F615DB862259D6637B07C5C26F1F70568DC027360DAAD2CD1BC`; snapshot không đổi. RED35/7, index-key RED42/1; GREEN43/0 trước review.
- Review độc lập `review_sql_project_sync`: xác nhận một Important, lỗi audit trước P1/P4 có thể vẫn tới READY nếu SSMS tiếp tục batch. Đã tái hiện đúng lỗi unique index1505 bằng hai sự kiện giả cùng MaSuKien/UTC trên DB thử; bản trước in ReadyPrinted=True. Không xóa nhật ký UTC để ép migration.
- Final: fixed audit failure — cờ hoàn tất cho bảo trì/phân quyền/schema audit/trigger audit/module lương nối trước P1/P4, xác minh tất cả ở cuối; module lương có transaction/TRY-CATCH và dọn cursor khi lỗi. Checker tổng kiểm index chống trùng sự kiện audit. Index RED43/1→GREEN44/0; lỗi audit thực GREEN ReadyPrinted=False, giữ đủ hai dòng UTC giả; fixture được dọn đúng PK và DB thử sửa lại bằng cả file.
- Guard SSMS tiếp tục GO: lỗi có chủ đích P1/P4 không in READY, cờ cũ bị xóa, số người/lượt/hóa đơn/ảnh/phân công giữ nguyên; chạy lại sửa DB thử thành công.
- Lần P6 trước sửa audit `20261110_123148_9dc1bf54` đạt32 bước; không dùng nó kết luận bản SQL sau sửa.
- Task 2: complete — bản cuối SHA256 `0167478AC3BCDDFFEEE011659B3F450566E3DA5BA29012C0A109D6C2CFF24200`,1.121.553 byte. Lần cuối `20261110_124022_78d39f69` đạt32/32 bước,485 mục nguồn trước/sau giống nhau; nền tảng44/44 trên cả cài mới và nâng cấp, ET47 bảng/392 thuộc tính, fingerprint62/64 giữ nguyên, Debug/Release0 lỗi.
- Task 3: complete — review Important đã xử lý và kiểm RED→GREEN/full suite; không có finding xác nhận còn hoãn trong phạm vi này. Báo cáo `docs/sprint3/DONG_BO_DATABASE_20261110.md`, evidence riêng, giữ nguyên biên bản/ZIP cũ theo hash cũ. SELECT cuối DB nghiệp vụ62 bảng/11 người/11 lượt/11 hóa đơn, không có migration trên DB đó. XR04 vẫn chờ hạ tầng, không biến kết quả SQL nội bộ thành duyệt phát hành.
