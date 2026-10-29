# Phục hồi dữ liệu và chạy một file SQL

Mở **Database/QLBenhVienRangHamMat.sql** trong SSMS, kết nối đúng SQL Server rồi chạy toàn bộ bằng F5. File tự tạo duy nhất **QLBenhVienRangHamMat** nếu chưa có; không cần SQLCMD mode, file backup bên ngoài hoặc các script nâng cấp rời. Ứng dụng kết nối database này theo appsettings.json.

Khi database chưa có bảng, snapshot nạp đủ schema và dữ liệu trong một giao dịch. Các khóa ngoại/CHECK được kiểm tra trước khi hoàn tất. Khi database đã có bảng, snapshot được bỏ qua; các pha đồng bộ giữ dữ liệu hiện tại, không khôi phục đè nội dung mới sửa, không tự xóa dữ liệu DEMO28. Không cần xóa database trước mỗi lần chạy SQL.

## Nguồn dữ liệu phục hồi

Backup gốc lúc **15:32 ngày29/10/2026** đã được xác minh rồi phục hồi và đồng bộ lên schema hiện tại. Bản đóng gói có60 bảng,1.410 dòng:11 bệnh nhân,10 nhân viên,10 thiết bị,10 lịch sử bảo trì,11 hóa đơn,10 phiếu lương,10 tài khoản cùng danh mục và chứng từ liên quan. Mười lần bảo trì cũ có ngày kết thúc được chuẩn hóa sang Đã hoàn thành; phiếu lương cũ giữ nguyên tiền trước đồng bộ. Không có nguồn để cam kết phục hồi các thay đổi sau mốc backup này.

Database chính đã được phục hồi từ bản nguồn đã kiểm chứng. Đối chiếu từng bảng bằng số dòng và SHA256 xác nhận dữ liệu sau cài mới/chạy lại/phục hồi khớp bản nguồn. Kiểm giao diện có ghi8 nhật ký truy cập theo cơ chế hiện tại; bản đóng gói cuối giữ cả8 dòng này, tổng1.418 dòng. Toàn bộ59 bảng nghiệp vụ/danh mục giữ nguyên so với nguồn đã đồng bộ; bảng nhật ký chỉ tăng8 dòng. Database thử phục vụ kiểm tra được dọn sau khi hoàn tất; các database bài tập khác trên SQL Server được giữ nguyên.

Các backup trong thư mục Backup mặc định của instance SQL Server trên máy hiện tại:

- Nguồn gốc: QLBenhVienRangHamMat_before_prescription_flow1_20261029_153231_9326f297db1344bf96bbdb71e64194fe.bak.
- Hiện trạng trước phục hồi: QLBenhVienRangHamMat_before_recovery_20261030_37118B95C1984FE38948F3CC24C20320.bak và QLBenhVienRangHamMat_immediately_before_restore_20261030.bak.
- Nguồn đã đồng bộ: QLBenhVienRangHamMat_recovered_migrated_20261030.bak.

## Đưa dữ liệu nhập thêm vào file SQL trước khi chuyển máy

Dữ liệu nhập qua phần mềm được lưu trong SQL Server. File .sql không tự thay đổi theo mỗi lần nhập. Đóng ứng dụng trước khi xuất để lấy bản dữ liệu nhất quán, rồi chạy tại thư mục dự án:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\CapNhatDuLieuSql.ps1 -MayChu . -Database QLBenhVienRangHamMat
```

Lệnh chỉ đọc database nguồn, kiểm tra đủ schema/60 bảng trước khi xuất, thay vùng snapshot trong file SQL chính và giữ bản SQL trước đó cùng manifest kiểm tra tại obj/SqlSnapshot/<thời điểm>/. Nguồn không hợp lệ khiến thao tác dừng và file SQL giữ nguyên. Không thay các pha nâng cấp. Giữ file SQL vừa xuất để cài mới; sao lưu .bak định kỳ nếu cần phục hồi đầy đủ sau sự cố. File có dữ liệu hồ sơ/tài khoản nên chỉ chuyển cho người được phép sử dụng.

Giới hạn nhỏ của công cụ xuất: giữ giá trị identity hiện có và seed/increment, chưa giữ riêng bộ đếm identity đã tiêu hao do giao dịch rollback/xóa dòng. Không ảnh hưởng bản phục hồi hiện tại: bảng identity duy nhất NHAT_KY_DUYET_CHAM_CONG đang trống và chưa cấp số. Khi cần giữ chính xác cả trạng thái bộ đếm trong các bản sau, dùng backup/restore .bak.

Chạy lại file SQL trên database đã có dữ liệu không tự đưa các dòng bị xóa trở lại. Khi cần khôi phục sau khi đã xóa dữ liệu, dùng bản backup phù hợp. Cài vào database mới chưa có bảng sẽ nạp bản snapshot đã đóng gói.

## Kiểm tra schema và giao diện

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\KiemTraBanGiao.ps1 -ChiCauTruc -KiemTraSql
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\KiemTraQuyTrinhBenhVien.ps1 -ChiGiaoDien -Database QLBenhVienRangHamMat
```

Các bộ kiểm nghiệp vụ có ghi chỉ chạy trên database tên RHM_Hospital_Test_*. Chúng không được gọi bởi file SQL chính. Tạo database thử là thao tác kiểm thử chủ động; F5 file SQL chính không sinh database thử. Riêng KiemTraQuyTrinhBenhVien.ps1 -TaoDatabaseKiemThu bỏ snapshot trong bản sao kiểm thử để dựng nền trống cho fixture khám; không xóa dữ liệu khỏi file SQL hoặc database đang dùng. Không trỏ bộ fixture này vào database thử đã có hồ sơ phục hồi; dùng -ChiGiaoDien để kiểm giao diện trên bản dữ liệu có sẵn.

Kiểm schema chỉ đọc. Kiểm giao diện không sửa hồ sơ/chứng từ, nhưng đăng nhập/mở phân hệ có thể phát sinh nhật ký truy cập.
