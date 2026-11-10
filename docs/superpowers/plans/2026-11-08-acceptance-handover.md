# P6 Acceptance and Handover Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Chạy lại đủ nhóm TN/HD/XR/DB/BC/RG, tạo ma trận có bằng chứng và hướng dẫn bàn giao/rollback; phân biệt kiểm nội bộ với nghiệm thu hạ tầng hai máy.

**Architecture:** Tái sử dụng các bộ kiểm P0–P5, chạy tuần tự trên database thử mới. PowerShell điều phối và đánh giá mã thoát/kết quả; không thêm project sản phẩm. Lưu log/ảnh/JSON và hash nguồn để kết quả gắn đúng bản mã đã chạy.

**Tech Stack:** PowerShell, .NET8/WPF, SQL Server/sqlcmd, git chỉ đọc.

**Spec:** `KeHoachTrienKhai.docx` mục9–11 và `docs/sprint3/KE_HOACH_TRIEN_KHAI_20261107.md`. Tiếp tục theo ủy quyền của người dùng; không lặp lại hỏi duyệt phương pháp triển khai.

## Global Constraints

- Giữ nhánh Sprint3_tan và thay đổi P0–P5/người dùng; không commit/push/publish, không nâng cấp DB đang dùng, không tự sửa bệnh án thiếu mã.
- Mọi ghi thử chỉ trong DB `RHM_Hospital_Test_...`, DEMO theo tiền tố riêng P5; tên mỗi lần chạy mới, từ chối DB đã tồn tại.
- Nâng cấp từ SQL gốc commit `1cafa7d2096a3e954d381e473520cba8bab4b9dd`, SHA256 `1EEB1EF8232485C86B18700BB12A81149268D9B7E495E64F6E6E06541BA56BCB`. Giữ đúng byte khi lấy blob; không coi bản cài mới khác là baseline nâng cấp.
- Lưu số dòng/hash cột cũ trước nâng cấp trên cùng DB, so lại sau nâng cấp64 bảng. Kiểm chạy lại hai lần và gián đoạn trên DB có phụ thu/ảnh.
- Không biến thiếu hạ tầng thành PASS. XR04/full P6 còn chờ kho UNC/hai máy thật nếu môi trường chưa được cung cấp. Không tạo share/sửa ACL để giả làm nghiệm thu hai máy.
- Không cộng tổng số assertion thành số tiêu chí độc lập; ma trận21 tiêu chí ghi rõ suite/ca chứng minh, bước nào chưa chạy phải giữ trạng thái chưa chạy.
- Log đầy đủ và SQL/backup thử ở obj; tài liệu chỉ đưa log lọc/số tổng hợp/ảnh dữ liệu giả. Không đưa snapshot hoặc kho ảnh/bak vào source evidence.

## Review Focus

- Runner báo PASS khi child exit khác0, có FAIL, thiếu/giảm số kết quả: kiểm bằng process thật có đầu ra kiểm soát.
- Chạy lại cùng tên hoặc tên SQL không hợp lệ: từ chối trước tạo/sửa DB; không ghi đè bộ chứng cứ.
- Lấy SQL gốc bị biến đổi Unicode/newline hoặc nâng cấp làm thay dữ liệu cũ: kiểm hash gốc và fingerprint từng bảng trước/sau.
- Chạy nhiều runner cùng lúc làm ghi đè scratch của tool cũ: khóa phiên kiểm, điều phối tuần tự.
- Bằng chứng hai máy cùng hostname/khác SQL/kho/hash hoặc thiếu proof: không xác nhận hạ tầng; không tự suy PASS từ đường dẫn UNC trên máy hiện tại.

### Task 1: Bộ điều phối và bằng chứng trung thực

**Files:** `tools/NghiemThuSupport.ps1`, `tools/KiemTraBoNghiemThu.ps1`, `tools/KiemTraNghiemThu.ps1`.

- [x] Viết kiểm RED cho exit/FAIL/thiếu summary/số kiểm thấp, parse hai định dạng summary và báo cáo không có summary.
- [x] Thêm helper ghi log mới, đánh giá kết quả, lấy blob git đúng byte, ghi JSON; ràng buộc input và không ghi đè.
- [x] Thêm runner mới cho cài mới/nâng cấp/re-run/ngắt, hồi quy, DEMO, backup/restore và smoke27 phân hệ; snapshot hash mã trước/sau, ma trận và report không giả PASS hạ tầng.

### Task 2: Chạy nghiệm thu nội bộ

- [x] Chạy cài mới64, nâng cấp62→64 và fingerprint62; re-run64 hai lần; giữ hóa đơn/phụ thu/ảnh khi ngắt trước migration cuối.
- [x] Chạy35 nền tảng,32 đồng bộ,31 tiếp nhận,1 đồng thời,47 luồng khám,32 ảnh,21 P5,7 guard,41 báo cáo; backup/restore ảnh và xem lại2 ca; smoke Admin27 phân hệ.
- [x] Build Debug/Release, kiểm SQL/FK/CHECK; lưu ảnh TN/HD/XR/BC và ví dụ truy nguồn tài chính; review độc lập, sửa finding và kiểm lại liên quan.

### Task 3: Hạ tầng và bàn giao

**Files:** `tools/KiemTraKhoAnhHaiMay.ps1`, `tools/SharedStorageProbe.cs.txt`, `docs/sprint3/NGHIEM_THU_P6_20261110.md`, `docs/sprint3/HUONG_DAN_TRIEN_KHAI_VA_ROLLBACK.md`, README/tiến độ/evidence.

- [x] Chuẩn bị phép thử kho UNC từ BUS/metadata/hash, proof hostname; từ chối path local và cấu hình/DB không phải fixture. Ghi phần chưa kiểm nếu chưa có hai máy.
- [x] Viết ma trận21 tiêu chí với PASS/FAIL/CHỜ HẠ TẦNG, hướng dẫn backup/nâng cấp/xác minh/rollback, đối chiếu file/hash và tình trạng chưa commit/publish.
- [ ] Chỉ đánh dấu P6 hoàn tất khi đủ cả nghiệm thu hạ tầng; nếu chưa có, chốt phần nội bộ và giữ điều kiện phát hành còn thiếu.

## Execution Ledger

- Ruling: tài khoản sandbox ban đầu không đọc thấy share; kiểm bằng thông tin Windows hiện tại thấy share quản trị có sẵn. Dùng UNC loopback kiểm giao thức thật trên một máy, vẫn chờ SQL/UNC máy thứ hai; không tự tạo share/đổi ACL hay coi thời gian chờ là xác nhận. Nếu nhầm phạm vi sẽ dẫn đến đánh giá sai khả năng dùng chung.
- Ruling: dùng nguồn trước P1 từ git có hash cố định thay vì phụ thuộc file obj có thể bị clean. Không đổi mã nghiệp vụ nếu nghiệm thu không phát hiện lỗi.
- Baseline: blob git SHA256 `37938EC64813917BDDA239A0F5E0B99A108F0934884C24A418DFFD63BB244699`, 1.080.881 byte LF; chuẩn hóa CRLF khớp hash gốc 1.086.701 byte ở Global Constraints. Không xuất blob bằng chuyển mã PowerShell.
- Task 1: complete — bộ helper16/16, gồm process thật exit7; summary lỗi trước kết quả xanh đã RED15/1→GREEN16/0 (`obj/P6Evidence/summary-review-*.log`).
- Nghiệm thu phát hiện lỗi UI còn lỗi trường sau khi đổi dịch vụ→thường. Thêm `ClinicalFormInput.ClearErrors(this)` khi cấu hình chế độ; RED27/1→GREEN28/0 (`stale-error-*.log`), sau mở rộng đạt31/0 (`service-review.log`).
- Review độc lập `review_p6`: bổ sung bằng chứng TN04/HD04; đã kiểm thật dịch vụ ngừng, đổi bác sĩ khác chuyên khoa, ghi TongTien sai và nguồn dịch vụ giả30.000đ. Không tắt trigger. Ca cũ đã bảo vệ đúng; bổ sung kiểm bao phủ, không sửa nghiệp vụ để làm xanh.
- Final: đánh giá lại lỗi chỉ đọc summary cuối là Important vì có thể nghiệm thu nhầm; đã sửa và kiểm RED→GREEN như Task1. Không còn minor hoãn được ghi nhận từ review này.
- Ruling: biên bản/evidence cuối mang ngày10/11 là ngày tiếp tục kiểm thực tế, giữ tên kế hoạch08/11 để không tách lịch sử. Rủi ro nếu không ghi ngày là nhầm lần chạy nguồn.
- Bổ sung DBCC/trạng thái FK/CHECK cho DB cài mới, nâng cấp và chạy lại có chứng từ; không suy kết quả từ DEMO sang dữ liệu nâng cấp.
- Task 2: complete — lần cuối `20261110_114040_eae3f295` đạt32/32 bước, source-before/after giống nhau; 20/21 tiêu chí PASS, XR04 PENDING_ENVIRONMENT. DBCC/FK/CHECK ba DB không vi phạm; Debug/Release0 lỗi. Log gốc `obj/P6Evidence/acceptance-verified.log`, bộ chọn `docs/sprint3/evidence/20261110-p6`.
- Task 3: hoàn tất phần chuẩn bị/bàn giao nội bộ — ma trận21,17 ảnh UI, log và hash, hướng dẫn backup62/SQL+ảnh64, triển khai, hai máy và rollback; README/tiến độ đã nối tài liệu. Chưa có máy/SQL/UNC thứ hai để nghiệm thu hạ tầng; không đánh dấu full P6.
- Ruling: giữ nguyên working tree/nhánh để người dùng xem xét, không dọn hiện vật lỗi, commit/push/publish hoặc triển khai DB đang dùng. Chuyển vận hành chỉ sau đủ bằng chứng máy khác và xác nhận cấu hình; rủi ro nếu bỏ điều kiện là mất khả năng truy cập/khôi phục kho chung.
- Kiểm bàn giao: 76 link cục bộ hợp lệ; source cuối481 tệp vẫn khớp manifest đã chạy, bốn script P6 parse đúng. SELECT tổng hợp DB đang dùng vẫn62 bảng/11 người/11 lượt/11 hóa đơn, không có phép ghi từ P6. Bộ evidence có hash từng tệp và loại trừ `.sql`/`.bak`/MDF/LDF/ảnh y khoa thật.
- Tiếp ngày10/11: đã chuẩn bị gói chạy XR04 mang sang máy khác, theo kế hoạch `2026-11-10-xr04-handover-kit.md`.17/17 guard và12/12 integration trên ZIP giải nén; bảo toàn481 mục nguồn P6, chỉ thêm bốn script bàn giao. Gói/source hiện tại có manifest riêng485 mục; không sửa manifest hoặc trạng thái P6 lịch sử. Vẫn thiếu bằng chứng máy thứ hai.
