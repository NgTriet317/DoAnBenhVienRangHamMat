# Quản lý Bệnh viện Răng Hàm Mặt

WPF .NET8 + SQL Server2016SP1 trở lên. Solution có đúng bốn project: RangHamMat.Wpf (giao diện), ET, DAL và BUS. Views chỉ có các cặp XAML/code-behind; C# phụ trợ ở Presentation/Controls.

## Database: một file SQL duy nhất

Dùng SSMS hoặc sqlcmd chạy **toàn bộ** [Database/QLBenhVienRangHamMat.sql](Database/QLBenhVienRangHamMat.sql). Không còn thư mục hay bước chạy Migrations.

```powershell
sqlcmd -S . -E -C -b -f65001 -i Database/QLBenhVienRangHamMat.sql
```

File tạo database trống hoặc đồng bộ database ClinicalWorkflow v2/v3 được hỗ trợ, tổng53 bảng. Sao lưu trước khi đồng bộ. Script kiểm tra schema, giữ mật khẩu/giá/hồ sơ và chứng từ cũ; chỉ dọn bộ DEMO28 còn nguyên bản, không bị nghiệp vụ người dùng tham chiếu. Nhật ký tra cứu chỉ đọc được giữ nguyên. Dữ liệu demo đã được sửa/ký hoặc có chứng từ khiến thao tác dừng để đối chiếu, không ép xóa bằng cách tắt trigger.

Cài mới chỉ có **admin001 / 123456** và danh mục cơ sở; không tạo bệnh nhân, bác sĩ, lịch trực, bệnh án, tồn kho hoặc chứng từ giả. Đổi mật khẩu sau đăng nhập. Nhập nhân sự/phòng/lịch làm việc, hàng hóa/thuốc và chứng từ nhập kho thực tế trước khi sử dụng các nghiệp vụ liên quan. Không tự tạo danh tính người thật khi chưa có nguồn được phép sử dụng.

Danh mục khởi tạo có47 tình trạng,42 dịch vụ,112 liên kết; gồm chấn thương, khớp thái dương hàm, niêm mạc, tuyến nước bọt, nhiễm trùng, nang/u, khe hở, thần kinh, hậu phẫu và bệnh răng. Nguồn/giá/ngày đối chiếu tại [DANH_MUC_RHM.md](docs/clinical-workflow/DANH_MUC_RHM.md). Giá công khai của cơ sở nguồn là mức tham khảo ban đầu; bệnh viện quản lý giá áp dụng trong DICH_VU.

## Cấu hình và chạy

Mở BenhVienRangHamMat.sln bằng Visual Studio2022 có .NET desktop development/.NET8 SDK, chọn RangHamMat.Wpf làm startup rồi F5. Hoặc mở PhatHanh/RangHamMat.Wpf.exe với .NET8 Desktop Runtime trên Windows.

Ứng dụng kết nối **SQL Server**, không kết nối trực tiếp vào file .sql. File SQL là nguồn khởi tạo/đồng bộ duy nhất. Sửa RangHamMat.Wpf/appsettings.json (hoặc bản cạnh exe):

```json
{"ConnectionStrings":{"RangHamMat":"Data Source=.;Initial Catalog=QLBenhVienRangHamMat;Integrated Security=True;TrustServerCertificate=True;"}}
```

Biến môi trường RHM_CONNECTION_STRING được ưu tiên hơn file. Instance có tên cần viết hai dấu gạch chéo trong JSON, ví dụ TEN_MAY\\SQLEXPRESS. Giữ toàn bộ thư mục PhatHanh khi chuyển máy, không chỉ chép exe.

## Luồng khám và thu tiền

1. Tiếp nhận chọn bác sĩ/phòng/dịch vụ; bác sĩ mở Hàng chờ khám theo giờ tiếp nhận, ai đến trước hiện trước. Admin xem được tất cả, User theo chức vụ và phạm vi SQL.
2. Bác sĩ ghi chẩn đoán/tình trạng, chọn nhanh theo nhóm bệnh và chọn dịch vụ liên quan. Kế hoạch có sẵn mã/giá từ SQL; chỉnh lượng/buổi rồi lưu và duyệt. Không có Mức độ hoặc Vị trí răng trong tình trạng bệnh.
3. Ký đơn thuốc hoặc ghi lý do Không kê thuốc. Khi **toàn bộ dịch vụ điều trị đã làm xong**, xác nhận **Hoàn tất và lập hóa đơn**. Hệ thống chốt các bước đã duyệt, ghi nhận dịch vụ khám một lần, khóa lần khám và tạo hóa đơn chờ thu trong cùng transaction. Không hoàn tất sớm nếu kế hoạch nhiều buổi còn chưa làm xong.
4. Quầy thu dùng lại chính hóa đơn đó; giá đã chốt được giữ, thu tiền/xuất thuốc phối hợp theo giao dịch. Không thêm dịch vụ sau khi hoàn tất. Thử lại cùng yêu cầu không sinh hóa đơn hoặc khoản thu thứ hai.
5. Dịch vụ thực hiện chỉ là **Lịch sử dịch vụ đã thực hiện** của lượt hoàn tất. Không có thêm/sửa/xác nhận hoặc dịch vụ phát sinh. Điều dưỡng chỉ xem phần được giao; thu ngân không xem kết quả lâm sàng.

Admin toàn quyền chức năng nhưng vẫn tuân thủ trạng thái chứng từ, kiểm tra dữ liệu, tiền và tồn kho. Đơn thuốc có nút Bàn giao thuốc cho đơn đã thu đủ. Xem [thay đổi luồng và kiểm chứng](docs/clinical-workflow/CHOT_KHAM_VA_SQL_THONG_NHAT.md).

## Kiểm tra và đóng gói

```powershell
.\KiemTra.bat -KiemTraSql -MayChu . -Database QLBenhVienRangHamMat
# Dựng/chạy bộ kiểm trên database thử riêng; mọi dữ liệu nghiệp vụ fixture rollback:
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\KiemTraQuyTrinhBenhVien.ps1 -TaoDatabaseKiemThu
# Chỉ kiểm đăng nhập Admin/mở26 phân hệ, không ghi hồ sơ khám:
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\KiemTraQuyTrinhBenhVien.ps1 -ChiGiaoDien -Database QLBenhVienRangHamMat

dotnet publish RangHamMat.Wpf -c Release -o PhatHanh
```

Kiểm SQL gọi thủ tục chỉ đọc được cài từ file SQL chính. Project kiểm tạm sinh trong obj, không thêm project sản phẩm. Bộ kiểm ghi chỉ nhận database tiền tố RHM_Hospital_Test_. Hướng dẫn và bằng chứng các mốc cũ có nhắc Migrations/dữ liệu demo là lịch sử đã ngừng sử dụng; dùng README và file SQL hiện tại để cài/chạy.
