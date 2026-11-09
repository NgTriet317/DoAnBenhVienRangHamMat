# Dịch vụ theo tình trạng — kết quả kiểm chứng

Source nền14e1cfb. [Thiết kế và thao tác](../../DICH_VU_THEO_TINH_TRANG.md), [tổng hợp](results.json), [dịch vụ ở khung hẹp](editor-440.png), [kế hoạch ở khung hẹp](plan-440.png).

| Kiểm tra | Kết quả |
|---|---|
| RED bản08 chưa có liên kết SQL/UI | 19 đạt, 2 lỗi đúng yêu cầu mới |
| Cài mới đầy đủ, DLL publish cuối | 29/29 |
| Nâng cấp08→09, chạy09 hai lần, giữ giá fixture đã sửa | 29/29 |
| Hồi quy Admin, DLL publish cuối | 12/12 |
| Hồi quy hàng chờ, DLL publish cuối | 16/16 |
| Database chính: đăng nhập bác sĩ thật qua form, chỉ đọc khám | 1/1 |
| Debug/Release và verify SQL | đạt, 0 lỗi build; còn cảnh báo nullable cũ |

Các ca mới kiểm: FK tình trạng/dịch vụ; danh sách từ SQL; tự điền mã/giá; liên kết đúng MaTTB cả khi hai dòng cùng tên; giá thay đổi/ngừng dịch vụ; cập nhật giá giữ lượng và nội dung; chọn ComboBox DataGrid thật; tổng tiền theo lượng; không tự chọn hay duyệt thủ thuật; cửa sổ1024×768,800×600,440×760. Kiểm tra cột giá dùng vị trí thực trong editor và chiều rộng đã render, tránh nhận nhầm control ảo chưa có kích thước. Ảnh dùng BN001 hư cấu của seed, trong transaction rollback.

Review độc lập chỉ đọc phát hiện hai lỗi P2: gắn dòng cùng tên cuối cùng thay cho dòng đã chọn; cập nhật cache dịch vụ chưa đồng bộ giá chọn thủ công. Đã sửa và review lại không còn lỗi chức năng cần xử lý. Khi xem ảnh phát hiện thêm cột giá bị khuất trên khung hẹp: sửa cột cố định/co giãn, không đóng băng cột dịch vụ, giới hạn nội dung chi tiết theo viewport. Kiểm tra và ảnh cuối xác nhận giá hiện đầy đủ.

`local-apply.log`: backup COPY_ONLY CHECKSUM và RESTORE VERIFYONLY CHECKSUM trước migration09; đường dẫn trong backup-path.txt. `local-before.json`/`local-after.json` chỉ chứa tên bảng, số dòng và SHA256 của các dòng có khóa tồn tại trước nâng cấp. Cả52 bảng cũ khớp tất cả cột/dòng. Bảng liên kết mới nâng schema lên53;8 dịch vụ và14 liên kết mới không nằm trong bộ khóa gốc. Không thay bảng giá cũ, nhân thân, hồ sơ hay chứng từ. Kiểm tra GUI có thể thêm audit truy cập theo thiết kế.

## Dựng lại kiểm thử

- Bộ29ca: installer hiện tại đổi tên database sang `RHM_SimpleConditions_Test_20261028` hoặc `RHM_SimpleConditions_Fresh_20261028`; chạy SQL UTF-8 với sqlcmd `-b -f65001`. Với đường nâng cấp, dùng installer tại14e1cfb rồi09. Một ca cuối cố ý commit sửa/ký demo để kiểm seed không reset, phải dựng lại fixture trước mỗi lần chạy toàn bộ.
- Admin/hàng chờ: installer tại f2b2f02, đổi tên sang `RHM_AdminQuyen_Test_20261028` / `RHM_HangChoKham_Test_20261028`; chạy06,08,09. Không chạy07 vào các fixture hồi quy vì assertion số hàng chờ dùng seed nền. Bộ29ca kiểm riêng độ phủ dữ liệu07. Hàng chờ có ca commit; lần kiểm lại trên fixture tái sử dụng gặp trùng PK. Đã dựng lại sạch và chạy đạt16/16, không đổi assertion để né lỗi.
- Source kiểm: tools/SimpleConditionChecks.cs.txt, AdminQuyenChecks.cs.txt, HangChoKhamChecks.cs.txt. Fixture kế hoạch dùng giá thật đang có của DV001 thay vì0 để tuân thủ kiểm tra giá mới.

```powershell
.\tools\KiemTraTinhTrangDonGian.ps1 -BanPhatHanh -Database RHM_SimpleConditions_Test_20261028
.\tools\KiemTraQuyenAdmin.ps1 -BanPhatHanh
.\tools\KiemTraHangChoKham.ps1 -BanPhatHanh
.\KiemTra.bat -KiemTraSql -MayChu . -Database QLBenhVienRangHamMat
```

Bốn database fixture và project tạm riêng của đợt này đã dọn; giữ backup/database chính/bản phát hành. Hash bốn DLL publish khớp Release, ghi tại results.json. Máy in/DPI ngoài các cửa sổ ghi trên chưa được thử thêm trong đợt này.
