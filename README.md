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

Danh mục thuốc có52 sản phẩm có nguồn và20 nhóm độc lập mẫu đơn:40 giá tham khảo công khai,12 thuốc chưa thiết lập giá. Database đang dùng có62 thuốc sau khi bổ sung và đã phân nhóm cả10 thuốc cũ. Thuốc giá0 hiển thị **Chưa thiết lập giá**, không coi là miễn phí; cần cập nhật giá trước lập hóa đơn. Không tự tạo liều dùng, mẫu đơn hay tồn kho. Xem [DANH_MUC_THUOC_RHM.md](docs/clinical-workflow/DANH_MUC_THUOC_RHM.md).

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
3. Sau khi đã làm xong toàn bộ kế hoạch, mở **4. Thực hiện**, đối chiếu dịch vụ và xác nhận. Hệ thống ghi dịch vụ thực tế theo giá đã duyệt, gồm phí khám một lần; chưa tạo hóa đơn. Kế hoạch nháp/chưa xong chặn bước kê thuốc và hoàn tất. Lượt chỉ khám, không có kế hoạch thủ thuật, vẫn xác nhận dịch vụ khám tại bước này.
4. Mở **5. Kê thuốc**, bấm **Kê thuốc** để mở cửa sổ. Lọc nhóm/tên/hàm lượng ở bên trái, thêm thuốc rồi chọn từng dòng bên phải để nhập số lượng, số ngày, liều, đường dùng và hướng dẫn. Bấm **Xác nhận đơn thuốc** để lưu nháp và đưa danh sách về lần khám; còn sửa được bằng **Sửa danh sách thuốc**. Không dùng mẫu/phiên bản hay tự áp liều. Nếu không kê, chọn Không kê thuốc, nhập lý do và Lưu khám. Đóng cửa sổ khi chưa lưu có lựa chọn lưu/bỏ/ở lại; giữ một nút Hủy nháp.
5. **6. Tổng kết** đọc dịch vụ thực tế và thuốc chờ chốt/đang hiệu lực từ SQL, không cộng lại đơn nguồn đang được thay. Kiểm tra xong chọn **Chốt và lập hóa đơn**. Hệ thống kiểm bản đã xem, ký đơn nháp, khóa lượt và tạo một hóa đơn chờ thu trong cùng giao dịch; lỗi giữ lại đơn nháp để sửa. Không tự ghi thêm phần điều trị chưa xác nhận.
6. Quầy thu dùng lại chính hóa đơn đó; giữ giá đã chốt, thu tiền/xuất thuốc phối hợp theo giao dịch. Không thêm dịch vụ sau hoàn tất; retry không sinh hóa đơn hoặc khoản thu thứ hai. Mục **Đơn thuốc** chỉ tra cứu/in đơn đã kê và bàn giao sau khi thu đủ, theo quyền.
7. Dịch vụ thực hiện chỉ là **Lịch sử dịch vụ đã thực hiện** của lượt hoàn tất. Không có thêm/sửa/xác nhận hoặc dịch vụ phát sinh. Điều dưỡng chỉ xem phần được giao; thu ngân không xem kết quả lâm sàng.

Admin toàn quyền chức năng nhưng vẫn tuân thủ trạng thái chứng từ, kiểm tra dữ liệu, tiền và tồn kho. Xem [cửa sổ kê thuốc và chốt hóa đơn](docs/clinical-workflow/CUA_SO_KE_THUOC.md). Các mốc HospitalWorkflow3/PrescriptionFlow1 mô tả nền tảng trước; hướng dẫn hiện tại áp dụng cửa sổ xác nhận nháp rồi chốt ký/hóa đơn.

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
