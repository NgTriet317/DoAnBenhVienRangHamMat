# Quản lý Bệnh viện Răng Hàm Mặt

Ứng dụng WPF .NET8, SQL Server2016SP1 trở lên. Solution bàn giao có đúng bốn project:

- `RangHamMat.Wpf`: giao diện, điều hướng, phiên đăng nhập.
- `ET`: thực thể và hợp đồng dữ liệu.
- `BUS`: quy tắc và các đầu mối nghiệp vụ.
- `DAL`: SQL, transaction, kiểm tra quyền và kết nối.

Trong `RangHamMat.Wpf/Views`, mỗi màn hình gồm `.xaml` và `.xaml.cs` được Visual Studio lồng cùng nhau. File C# phụ trợ/partial đã chuyển sang `Presentation` hoặc `Controls`. Không xóa `.xaml.cs`: đây là code-behind xử lý sự kiện của XAML.

## 1. Chuẩn bị database

**Cài mới:** mở `Database/QLBenhVienRangHamMat.sql` bằng SSMS và chạy toàn bộ file trên SQL Server. Script tạo database `QLBenhVienRangHamMat`,52 bảng và schema quy trình khám2; dữ liệu mẫu là hư cấu. Script từ chối database đã có bảng, không xóa/ghi đè dữ liệu cũ.

**Đã có database bản cũ:** sao lưu trước, chọn đúng database trong SSMS rồi lần lượt chạy:

1. `Database/Migrations/20261024_01_Preflight_ClinicalWorkflow.sql`
2. `Database/Migrations/20261024_02_ClinicalWorkflow.sql`
3. `Database/Migrations/20261024_03_Verify_ClinicalWorkflow.sql`
4. `Database/Migrations/20261028_05_AdminFullAccess.sql`

Nâng cấp giữ chứng từ/số tiền/tồn cũ, không thu hoặc xuất lại. Chạy bước2 lần hai an toàn. Không dùng file cài mới để nâng cấp. Rollback04 chỉ dành cho trước khi có dữ liệu nghiệp vụ mới; sau đó cần quy trình restore/đối chiếu, xem [hướng dẫn](docs/clinical-workflow/HUONG_DAN_CHAY.md).

## 2. Cấu hình và chạy

Cài Visual Studio2022 với .NET desktop development và .NET8 SDK. Mở `BenhVienRangHamMat.sln`, chọn `RangHamMat.Wpf` làm Startup Project rồi F5.

Sửa `RangHamMat.Wpf/appsettings.json` theo SQL Server của máy. Mặc định Windows Authentication, server `.` và database `QLBenhVienRangHamMat`:

```json
{
  "ConnectionStrings": {
    "RangHamMat": "Data Source=.;Initial Catalog=QLBenhVienRangHamMat;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

File này được chép cạnh executable khi build/publish. Có thể ưu tiên cấu hình bằng biến môi trường:

```powershell
$env:RHM_CONNECTION_STRING='Data Source=TEN_SERVER;Initial Catalog=QLBenhVienRangHamMat;Integrated Security=True;TrustServerCertificate=True;'
dotnet run --project RangHamMat.Wpf -c Release
```

Nếu SQL Server dùng instance có tên, dùng dạng `TEN_MAY\SQLEXPRESS` trong chuỗi kết nối; trong JSON phải viết `TEN_MAY\\SQLEXPRESS`. Máy đích cần database đã cài/nâng cấp và .NET8 Desktop Runtime khi chạy bản publish phụ thuộc framework.

Tài khoản mẫu `admin001` và `user002`…`user010`, mật khẩu mẫu `123456`. **Admin có toàn quyền ở mọi mục chức năng**, gồm tiếp nhận, khám/kế hoạch, duyệt mẫu, xác nhận dịch vụ, ký đơn, thu tiền và giao thuốc cho mọi bác sĩ. Trong **Đơn thuốc**, dùng nút **Bàn giao thuốc** để chuyển sang quầy giao. User vẫn theo chức vụ và phạm vi được phân trong SQL. Các chứng từ đã chốt, kiểm tra dữ liệu bắt buộc, tiền và tồn kho vẫn được bảo vệ. Database v2 đã cài trước ngày28/10 cần chạy migration05 để đồng bộ quyền duyệt/giao; bản cài mới đã tích hợp. Không dùng tài khoản mẫu cho dữ liệu thật. Xem [thay đổi quyền Admin](docs/clinical-workflow/QUYEN_ADMIN.md) và [hướng dẫn chạy](docs/clinical-workflow/HUONG_DAN_CHAY.md).

## 3. Luồng khám mới

**Bệnh nhân →Hồ sơ →Tiếp nhận →Khám/kế hoạch →Dịch vụ đã làm/đơn ký →Thu đủ →Bàn giao thuốc →In lại.**

Ký đơn chưa trừ kho; thu đủ mới ghi xuất và chờ giao trong cùng transaction. Bàn giao/in lại không thu hay trừ tồn lần nữa. Khi rời trang có nhập liệu, chọn Lưu nháp/Bỏ thay đổi/Hủy; Lưu nháp không ký hoặc thu tiền. Yêu cầu thu/giao chưa rõ kết quả giữ nguyên mã để đối chiếu khi thử lại.

## 4. Kiểm tra và đóng gói

Chạy `KiemTra.bat` để kiểm cấu trúc bốn project và build Debug/Release. Kiểm thêm SQL bằng Windows Authentication:

```powershell
.\KiemTra.bat -KiemTraSql -MayChu . -Database QLBenhVienRangHamMat
```

Phần SQL chỉ đọc/đối chiếu schema. Đóng gói bằng `dotnet publish RangHamMat.Wpf -c Release -o PhatHanh`; sao chép toàn bộ thư mục kết quả và sửa appsettings cạnh exe. Không chỉ chép một fileexe.

Các project kiểm thử phát triển đã được chạy trước khi dọn khỏi bản bàn giao. [Báo cáo P12](docs/clinical-workflow/BAN_GIAO_P12.md) ghi lệnh/kết quả, ma trận AT01–AT34, checkpoint chứa source kiểm thử và giới hạn thiết bị thực. [Tiến độ](docs/clinical-workflow/TIEN_DO.md) và [kết quả các mốc](docs/clinical-workflow/KET_QUA_KIEM_THU.md) giữ lịch sử triển khai. Tài liệu trước đợt cải tổ nằm trong `docs/legacy` và có thể mô tả phiên bản cũ; dùng README này để cài/chạy hiện tại.
