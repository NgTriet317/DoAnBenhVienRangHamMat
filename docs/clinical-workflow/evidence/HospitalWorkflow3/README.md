# HospitalWorkflow3 — kiểm chứng29/10/2026

Nguồn dữ liệu/schema duy nhất: `Database/QLBenhVienRangHamMat.sql`. Không cần thư mục Migrations. Bộ kiểm là công cụ sinh project tạm trong obj, không thêm project vào solution. Fixture có nhãn FIXTURE chỉ tồn tại trong transaction kiểm thử và luôn rollback; không được đưa vào SQL cài bệnh viện.

## Kết quả

| Kiểm tra | Kết quả | Bằng chứng |
|---|---|---|
| Tái hiện trước sửa | Hai lỗi: chưa lập hóa đơn khi hoàn tất, còn ghi dịch vụ phát sinh | initial-red.log |
| Bản nguồn |34 đạt,0 lỗi | source-tests.log |
| DLL bản phát hành |34 đạt,0 lỗi | published-tests.log |
| Admin, database sạch | Đăng nhập, mở đủ26 phân hệ và lịch sử chỉ đọc | published-fresh-ui.log |
| Admin, database đang dùng | Đăng nhập, mở đủ26 phân hệ và lịch sử chỉ đọc | published-current-ui.log |
| Cấu trúc/build/schema | Bốn project, Views XAML/code-behind, Debug/Release đạt, schema53 bảng | delivery.log |
| SQL chính cài trống và chạy lặp | Đạt;0 bệnh nhân/0 bác sĩ/0 hóa đơn,1 Admin;47 tình trạng/42 dịch vụ/112 liên kết | fresh.log, repeat.log, final-state.log |
| Nâng cấp database v2 chứa demo và dữ liệu thêm |349 dòng demo được dọn, các dòng ngoài demo giữ nguyên; chạy lặp đạt | upgrade-preservation.log, upgrade-repeat.log, upgrade-preservation.json |
| Demo đã xử lý | Từ chối và rollback giai đoạn3, không để lại schema hóa đơn/chốt dịch vụ mới | negative-sync.log, negative-rollback.log |
| Áp dụng database đang dùng | Backup kiểm chứng;349 dòng DEMO28 được dọn, tất cả dòng ngoài demo của53 bảng giữ nguyên | local-sync.log, preservation.json, backup-path.txt |

Kiểm nghiệp vụ bao gồm: kế hoạch hai bước và phí khám; hoàn tất/retry cùng một hóa đơn; rollback toàn lần hoàn tất khi nguồn tiền lỗi; đơn đã ký có mặt trong hóa đơn trước xuất kho; giữ giá thuốc khi giá danh mục đổi; thu chính hóa đơn đó, phân bổ hai lô và nối lại nguồn xuất kho; không thu hai lần; in chứng từ; không ghi dịch vụ ngoài luồng; bác sĩ khác bị từ chối; điều dưỡng chỉ thấy phần được giao; thu ngân không xem kết quả lâm sàng; tình trạng nhập tay/chọn nhanh/đổi giá/xung đột dữ liệu và hiển thị ở nhiều kích thước.

Rà soát độc lập đã tìm thấy phạm vi điều dưỡng/thu ngân chưa chặt ở lịch sử dịch vụ. Đã sửa và thêm kiểm hồi quy đạt. Phí khám được tính một lần theo mã dịch vụ nếu đã có cùng dịch vụ trong kế hoạch.

Ảnh `hospital-condition.png`, `pending-invoice.png`, `service-history.png` được dựng từ WPF thật bằng fixture rollback. Các ảnh editor/plan cho thấy hiển thị ở440/800/1024px. Nền trắng của ảnh bảng thu được đặt trong hàm chụp để tránh nền trong suốt khi xuất PNG; không đổi giao diện sản phẩm. Hash SQL/DLL và bộ kiểm ở results.json. Build sạch khi publish vẫn có cảnh báo nullable có sẵn trong các màn cũ; build kiểm bàn giao sau đó không lỗi.

## Phạm vi dữ liệu

Database đang dùng còn11 bệnh nhân (10 hồ sơ seed cũ có liên quan chứng từ và1 hồ sơ người dùng thêm),2 bác sĩ cũ,50 dịch vụ,47 tình trạng,112 liên kết. **Các hồ sơ/chứng từ cũ này chưa bị xóa.** Chỉ bộ DEMO28 được nhận diện chính xác và kiểm tra đủ sở hữu/FK/nội dung mới được dọn tự động. Bản SQL cài mới không chứa hồ sơ người giả, nhân sự giả, lịch khám, lịch làm việc, tồn kho hay chứng từ giả.

Một audit chỉ tra cứu `DICH_VU_THUC_HIEN`, không có nội dung trước/sau, của tài khoản được giữ lại không chặn việc dọn demo và được giữ nguyên. Các audit sửa dữ liệu, dữ liệu ký/chốt, chứng từ hoặc tham chiếu người dùng khác vẫn chặn và rollback; không tắt ràng buộc/trigger để xóa.

Backup COPY_ONLY/CHECKSUM đã RESTORE VERIFYONLY; vị trí trên SQL Server ghi trong backup-path.txt. preservation.json chỉ chứa số lượng/bằng chứng so sánh từng bảng, không chứa nội dung hồ sơ hay mật khẩu. Bốn database kiểm thử do tác vụ này tạo được dọn sau khi lưu bằng chứng; không xóa database đang dùng. Xem cleanup.log.

## Chạy lại

```powershell
# Từ thư mục dự án trên Windows có .NET8 SDK, SQL Server và sqlcmd:
.\KiemTra.bat -KiemTraSql
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraQuyTrinhBenhVien.ps1 -TaoDatabaseKiemThu
dotnet publish RangHamMat.Wpf -c Release -o PhatHanh
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraQuyTrinhBenhVien.ps1 -BanPhatHanh
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraQuyTrinhBenhVien.ps1 -ChiGiaoDien -Database QLBenhVienRangHamMat -BanPhatHanh
```

`-TaoDatabaseKiemThu` chỉ dùng SQL chính, đổi tên database thành tiền tố kiểm thử riêng. Bộ kiểm ghi từ chối database không có tiền tố `RHM_Hospital_Test_`. Chế độ `-ChiGiaoDien` chỉ kiểm đăng nhập/mở màn, dùng Admin đang có mật khẩu thử123456; thay mật khẩu trong bộ kiểm nếu tài khoản đã đổi. Không đặt lại mật khẩu database khi sync. Các database kiểm thử không được phân phối trong PhatHanh.
