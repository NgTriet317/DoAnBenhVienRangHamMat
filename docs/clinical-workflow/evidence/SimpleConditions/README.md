# Bằng chứng tình trạng bệnh đơn giản — 28/10/2026

Source nền `f2b2f02`, nhánh `cai-to-quy-trinh-kham-6-chuc-nang`. Tổng hợp tại [results.json](results.json), thay đổi tại [báo cáo](../../TINH_TRANG_BENH_DON_GIAN.md). Solution sản phẩm vẫn bốn project, Views có 62 cặp XAML/code-behind; project kiểm thử chỉ sinh tạm trong obj.

## Chẩn đoán và phạm vi

`old-grid-probe.log`: DLL cũ trên bản nháp của người dùng cho phép BeginEdit và commit trong bộ nhớ; lịch sử khóa đúng. Không gọi SQL SAVE. Form cũ khó tìm dòng mới và có thêm danh mục/mức độ. `red.log` ghi 1 đạt/9 lỗi khi yêu cầu mới chưa triển khai. Form mới bỏ các bước này, có nút Thêm/Cập nhật/Xóa và Lưu nháp trực tiếp từ nội dung đang gõ. [Ảnh control WPF thật](condition-editor.png) dùng dữ liệu seed hư cấu và transaction rollback.

Gỡ API/giao diện cấu hình/gợi ý/áp mẫu điều trị; giữ ba bảng legacy và khóa nguồn để không phá lịch sử. Cấu hình hướng dẫn thuốc vẫn thuộc phân hệ kê đơn. Không thay cơ chế quyền, khóa lượt đã ký, đối chiếu snapshot, thanh toán hay tồn kho.

## Các bộ kiểm đã chạy

| Log | Kết quả / fixture |
|---|---|
| upgraded-tests.log | 18/18; installer nền cũ +06+07 |
| published-tests.log | 18/18; installer chính mới, tham chiếu DLL PhatHanh |
| admin-regression.log | 12/12; installer nền +06, DLL PhatHanh |
| queue-regression.log | 16/16; installer nền +06, DLL PhatHanh |
| seed07-first.log / seed07-repeat.log | thêm 12/7/4/224/14/14/7/7; lần hai thêm 0 |
| local-doctor-ui.log | tài khoản demo_ck002_dem đăng nhập thật, mở bản nháp CK002 và ô tình trạng sửa được; không ghi khám |
| local-admin-ui.log | đăng nhập thật, mở 26 module và lối bàn giao; 3/3 |
| delivery-check.log | cấu trúc, Debug/Release 0 lỗi, schema v2/quyền Admin/SimpleConditions v1 hợp lệ |
| publish.log | dotnet publish thành công; bốn hash DLL trong results.json khớp Release |

18 ca kiểm tình trạng bao gồm CRUD form/SQL, không bấm Thêm trước Lưu, giữ nội dung khi lỗi/đổi dòng, khóa lịch sử/sai bác sĩ/quyền giả, xung đột snapshot, không xóa nguồn kế hoạch, hoàn tất không kê thuốc, control WPF thật, bác sĩ-ca-phòng đúng chuyên khoa cho mọi dịch vụ, mật khẩu của 12 tài khoản mới, y tá CV003 tiếp nhận CK002 không cần quyền ngoài lịch, 7 nháp sửa được, và seed lặp giữ nội dung sửa/lượt đã ký.

Admin suite gỡ riêng ca duyệt mẫu điều trị vì tính năng đã bỏ, vẫn kiểm duyệt hướng dẫn thuốc và toàn luồng khám→kế hoạch→dịch vụ→đơn→thu→giao. Hai suite hồi quy chỉ bỏ trường MucDo khỏi DTO fixture. Không giảm điều kiện phân quyền/hàng chờ hay assertion số lượng để né dữ liệu mới.

Review độc lập chỉ đọc không thấy lỗi chức năng/quyền/an toàn dữ liệu mới; một trailing whitespace ở CuaSoKhamDieuTri.xaml đã sửa. Kiểm tra hiện tại không tuyên bố nghiệm thu thêm máy in/thiết bị hay mọi DPI.

## Dựng lại fixture

Windows, .NET8 SDK, SQL Server và sqlcmd; Windows Authentication có quyền trên các database thử. Không dùng database sử dụng cho bộ kiểm ghi.

- `RHM_SimpleConditions_Test_20261028`: lấy installer tại `git show f2b2f02:Database/QLBenhVienRangHamMat.sql`, thay tên database mặc định bằng tên fixture, lưu UTF-8 và chạy bằng sqlcmd `-b -f65001`; sau đó chạy migration06,07 trên đúng fixture.
- `RHM_SimpleConditions_Fresh_20261028`: installer chính hiện tại, thay tên database sang tên fixture và chạy toàn bộ; đã gồm05,06,07.
- `RHM_AdminQuyen_Test_20261028` và `RHM_HangChoKham_Test_20261028`: installer tại f2b2f02, thay tên tương ứng rồi chạy06. Không seed07 vào hai fixture hồi quy này vì các ca cũ đối chiếu chính xác số lượng seed nền. Bộ tình trạng mới chịu trách nhiệm kiểm dữ liệu07.

```powershell
.\tools\KiemTraTinhTrangDonGian.ps1 -Database RHM_SimpleConditions_Test_20261028
.\tools\KiemTraTinhTrangDonGian.ps1 -BanPhatHanh -Database RHM_SimpleConditions_Fresh_20261028
.\tools\KiemTraQuyenAdmin.ps1 -BanPhatHanh -Database RHM_AdminQuyen_Test_20261028
.\tools\KiemTraHangChoKham.ps1 -BanPhatHanh -Database RHM_HangChoKham_Test_20261028
```

Các nhóm tình trạng đa số rollback. Ca cuối cố ý commit sửa tình trạng CK002/ký lượt CK003 rồi chạy lại07 để chứng minh không reset. Phải dựng lại fixture sạch trước khi chạy lại toàn bộ 18 ca. Chế độ `-ChiGiaoDien` của bộ tình trạng chỉ mở đăng nhập/form, không gọi lưu khám, dùng được trên database đã có bác sĩ/bản nháp demo. Bốn database thử và các project tạm riêng của đợt này được dọn sau khi lưu bằng chứng.

## Database đang sử dụng

`local-apply.log` ghi backup COPY_ONLY CHECKSUM, RESTORE VERIFYONLY CHECKSUM, chạy06/07 thành công trên `. / QLBenhVienRangHamMat`, 52 bảng, 14 bác sĩ hoạt động, MucDo đã bỏ. Đường dẫn backup tại [backup-path.txt](backup-path.txt); giữ backup trên SQL Server.

`local-before.json` và `local-after.json` chỉ chứa tên bảng/số dòng/SHA256, không xuất nội dung hồ sơ. Executor ghi tại `apply-local-preservation.ps1.txt` để review, không phải hướng dẫn chạy lại. Nó chụp bộ khóa gốc từng bảng, bỏ duy nhất cột TINH_TRANG_BENH.MucDo theo yêu cầu, rồi so sánh mọi cột của mọi dòng gốc trước/sau; các dòng seed mới không thuộc bộ khóa gốc. Cả 52 bảng khớp. GUI kiểm sau nâng cấp có thể sinh audit truy cập theo thiết kế, không thay khám hoặc chứng từ.

Một lần tiền kiểm trước đó lỗi do ExecuteScalar nhận phân đoạn JSON của truy vấn FOR JSON. Đã sửa bằng truy vấn trả một nvarchar(max), chạy lại thành công. Lỗi xảy ra trước backup/migration, không thay dữ liệu. Các hash trong thư mục này là lần chạy hoàn chỉnh thành công.

Đã cập nhật PhatHanh, giữ nguyên appsettings và sao chép SQL/migration/tài liệu tài khoản. Đóng ứng dụng cũ trước khi mở lại để dùng DLL mới.
