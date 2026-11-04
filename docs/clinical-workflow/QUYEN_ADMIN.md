> LỊCH SỬ: dữ liệu demo và các bước Migrations dưới đây đã ngừng sử dụng từ29/10/2026. Dùng [README hiện tại](../../README.md) và duy nhất QLBenhVienRangHamMat.sql; tài liệu này chỉ lưu bằng chứng mốc trước.

# Toàn quyền Admin — 28/10/2026

Yêu cầu: tài khoản Admin được sử dụng mọi mục chức năng để kiểm tra dự án.

Nguyên nhân: quyền nghề nghiệp và điều kiện sở hữu bản nháp chặn Admin CV001; SQL còn yêu cầu người duyệt/giao thuốc thuộc chức vụ chuyên môn. Database mặc định đang ở 46 bảng.

Thiết kế: quyền Admin phải được đọc từ vai trò trong SQL, cấp đủ quyền nghiệp vụ và phạm vi mọi bác sĩ. Giữ bác sĩ phụ trách trên lượt/đơn/kế hoạch; người thao tác thực tế ghi trong nhật ký. User giữ phạm vi hiện có. Không bỏ kiểm tra trạng thái, phiên bản, tiền, tồn kho hoặc dữ liệu bắt buộc.

- [x] Kiểm thử tái hiện quyền Admin thiếu, sở hữu bản nháp và trigger SQL trước sửa.
- [x] Sửa DAL, giao diện và SQL cài mới; migration 05 cập nhật database v2 hiện có.
- [x] Kiểm thử Admin xuyên suốt tiếp nhận → khám → kế hoạch → dịch vụ → kê đơn → thu tiền → giao thuốc; User/giả quyền/bị khóa vẫn bị chặn.
- [x] Sao lưu, tiền kiểm, nâng cấp database mặc định; kiểm tra bảo toàn dữ liệu cũ.
- [x] Build Debug/Release, cập nhật PhatHanh và hướng dẫn.

Kiểm thử dùng mã nguồn trong `tools/AdminQuyenChecks.cs.txt`, runner `tools/KiemTraQuyenAdmin.ps1`; project tạm sinh trong `obj` để solution vẫn đúng bốn project. Dữ liệu thử chỉ ở database riêng `RHM_AdminQuyen_Test_20261028`.

## Sử dụng

Đóng phiên ứng dụng cũ rồi chạy `PhatHanh/RangHamMat.Wpf.exe`, đăng nhập `admin001` với mật khẩu đang có (dữ liệu mẫu: `123456`). Vai trò Admin/Quản trị được kiểm tra trong SQL; không cần đổi chức vụ NV001 thành bác sĩ hoặc sửa mật khẩu. Admin chọn bác sĩ chuyên môn trong tiếp nhận như bình thường, sau đó được xử lý lượt của bác sĩ đó. Trong Đơn thuốc, chọn **Bàn giao thuốc** để đối chiếu và giao các đơn đã thu đủ.

Database `QLBenhVienRangHamMat` trên server `.` đã được tiền kiểm, nâng cấp v2 và áp dụng05. Database v2 ở máy khác cần chạy `Database/Migrations/20261028_05_AdminFullAccess.sql`; bộ cài chính đã có cùng thay đổi. Script05 chạy lặp an toàn và không cập nhật dòng chứng từ.

## Bằng chứng ngày28/10/2026

- Bản DLL bàn giao cũ:1/6 ca đạt,5 ca tái hiện thiếu quyền. Kiểm thử được chạy lại với fixture đúng, không dựa vào mã phiếu không tồn tại trong lần viết harness đầu tiên.
- Sau sửa:12/12 trên database nâng cấp,12/12 trên database cài mới và12/12 khi dùng trực tiếp DLL Release trong PhatHanh. Bao gồm quyền User, cờ Admin giả trong DTO, khóa/thu hồi tài khoản, tên vai trò Quản trị, giữ MaBS, audit, thu/giao lặp, khóa chứng từ đã chốt và các giao diện nháp khác bác sĩ.
- Đăng nhập PBKDF2 thật qua hộp thoại, mở đủ26 mục trang chủ, vào bàn giao và đổi bệnh nhân/quay lại:3/3 trên database mặc định bằng DLL Release. Các kiểm tra mặc định chỉ đọc nghiệp vụ; tra cứu Admin có thêm nhật ký theo thiết kế.
- Review độc lập phát hiện1 lỗi ngữ cảnh bệnh nhân khi đang bàn giao. Đã tái hiện RED, sửa đồng bộ cha/con và kiểm tra GREEN. Không còn phát hiện cần xử lý từ lượt review này.
- Debug và Release đều0 lỗi; lần build ghi bằng chứng:Debug0 cảnh báo,Release53 cảnh báo có sẵn (nullable, tên kiểu và một số khai báo cũ). Kiểm tra cấu trúc và schema/quyền SQL đạt; publish thành công.
- Trước nâng cấp database mặc định, tạo backup `COPY_ONLY,CHECKSUM` và `RESTORE VERIFYONLY WITH CHECKSUM`. HashSHA256 và số dòng của toàn bộ46 bảng/cột gốc khớp sau nâng cấp; thêm6 bảng, không thay chức vụ/mật khẩu hoặc chứng từ gốc.
- Backup trên máy SQL Server: `C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\QLBenhVienRangHamMat_before_admin_20261028_173520_7a86792c1d274bcda070114f271eb475.bak`. Backup không nằm trong Git.

Log và hash được lưu ở [evidence/AdminAccess](evidence/AdminAccess). Các test ghi dữ liệu nằm trong transaction rollback ở database thử; không tạo hóa đơn/đơn thử trên database mặc định.

Chạy lại GUI chỉ đọc trên database mặc định:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraQuyenAdmin.ps1 -Database QLBenhVienRangHamMat -ChiGiaoDien -BanPhatHanh
```

Chạy đủ12 ca cần database thử riêng theo đúng tên trong runner, tạo từ SQL chính bằng cách đổi đồng thời tên database sang `RHM_AdminQuyen_Test_20261028`. Sau đó chạy runner không có `-ChiGiaoDien`. Bộ test từ chối ghi thử trên tên database khác. Bản test không thêm project vào solution.

Quy định Admin trong tài liệu nghiệm thu P01–P12 là lịch sử trước yêu cầu này; quyền hiện hành theo tài liệu này và README.
