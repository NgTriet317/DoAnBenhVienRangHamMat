# Kiểm tra quyền Admin — 28/10/2026

Mã nguồn test: `tools/AdminQuyenChecks.cs.txt`, runner: `tools/KiemTraQuyenAdmin.ps1`. Các ghi thử dùng transaction rollback trên database fixture riêng. Không thêm test project vào solution.

- `red-baseline.log`: chạy DLL bản phát hành cũ với fixture đúng;5 lỗi quyền được tái hiện.
- `red-handover3.log`: sau sửa quyền, phát hiện thiếu lối vào bàn giao.
- `red-context.log`: tái hiện lỗi đổi bệnh nhân khi đang bàn giao do review độc lập phát hiện.
- `green-final.log`:12/12 ca trên schema cũ đã nâng cấp quyền bằng05.
- `fresh-final.log`, `fresh-tests.log`: cài SQL chính đã sửa lên database thử sạch và12/12 ca đạt, chưa cần chạy05 riêng.
- `published-tests.log`:12/12 với DLL Release trong PhatHanh sau đồng bộ05 lặp.
- `published-local-ui.log`: đăng nhập thật, đủ26 mục, bàn giao/chuyển bệnh nhân trên database mặc định;3/3 nhóm kiểm tra đạt.
- `local-upgrade.log`: backup COPY_ONLY/CHECKSUM, VERIFYONLY, preflight, migration02, verify03 và05. `local-before.json`/`local-after.json` chỉ chứa hash/số dòng theo các cột gốc, không chứa hồ sơ hoặc mật khẩu. Cả46 bảng khớp trước/sau nâng cấp. Tra cứu Admin qua GUI sau đó có bổ sung nhật ký.
- `migration-repeat-1.log`, `migration-repeat-2.log`:05 chạy lặp thành công.
- `delivery-check.log`, `publish.log`: cấu trúc bốn project, Debug/Release0 lỗi, SQL52 bảng và3 trigger cho Admin, publish. Các cảnh báo nullable cũ được giữ trong log.

Lần viết harness đầu có mã phiếu nguồn sai và lần dựng GUI đầu mở lồng hộp đăng nhập của App. Đã sửa harness và chạy lại; các kết quả RED/GREEN được ghi ở đây dùng fixture tồn tại và Application kiểm thử riêng. Không dùng lỗi harness làm bằng chứng lỗi sản phẩm.

Một lỗi cú pháp alias trong truy vấn hash của script tiền kiểm đã dừng trước backup/migration; lần chạy thành công trong `local-upgrade.log` là bằng chứng cuối. Backup database thật nằm ngoài Git tại thư mục Backup của SQL Server, tên ghi trong log và QUYEN_ADMIN.md.

Database thử đã được dọn sau nghiệm thu. Muốn chạy lại đủ12 ca, tạo lại fixture theo hướng dẫn trong QUYEN_ADMIN.md. Chế độ `-ChiGiaoDien` chỉ đọc nghiệp vụ trên database đích, ngoại trừ audit tra cứu theo thiết kế.
