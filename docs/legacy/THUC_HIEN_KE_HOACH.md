# Kết quả thực hiện KeHoach.txt — 11/10/2026

Đã thực hiện các yêu cầu trong `RangHamMat.Wpf/KeHoach.txt` trên source hiện tại, giữ các thay đổi có sẵn và luồng form gốc. Không chạy script khởi tạo lại database.

Đã hoàn tất yêu cầu tiếp theo về ET/DAL/BUS từng bảng, mã chuỗi tự cấp và tên view tiếng Việt không dấu; xem [chi tiết thay đổi](CAP_NHAT_DAL_BUS_ET.md).

## Đối chiếu yêu cầu

| Yêu cầu | Thực hiện |
|---|---|
| Bỏ nạp dữ liệu mẫu, dùng database thật và 3 lớp | Tải lại đọc SQL. Bản sửa 13/10/2026 có đủ 39 bộ ET/DAL/BUS riêng, gọi các lớp thực thể liên quan và chia sẻ IDbTransaction. WPF dùng ET và dữ liệu SQL; xem CAP_NHAT_DAL_BUS_ET.md để tra kết quả mới nhất. |
| Hồ sơ, khám điều trị, đơn thuốc lưu hoàn chỉnh | Lưu/thêm/sửa/xóa theo ràng buộc; đơn/kế hoạch/dịch vụ nhiều dòng dùng transaction. Xác nhận lần khám lưu ngày và trạng thái đồng thời. |
| Dịch vụ liên kết hồ sơ, khám và đơn | Lọc theo bệnh nhân/lần khám; thêm/sửa/xác nhận; điều hướng hồ sơ, lần khám, đơn, hóa đơn giữ đúng khóa nguồn. |
| Form bệnh nhân hợp database | Bổ sung BHYT; điện thoại chuỗi, không mất số 0 đầu; giới tính/ngày sinh nullable; chỉ bắt buộc tên và trạng thái như SQL; độ dài trường theo v8; bố cục xanh, nhóm thông tin/sức khỏe giữ luồng tạo bệnh án. |
| Hóa đơn và thanh toán liên kết chặt | DAL/BUS riêng cho HOA_DON, CHI_TIET_HOA_DON, THANH_TOAN, PHIEU_XUAT, CHI_TIET_PHIEU_XUAT: cấp thuốc ghi sổ, lập/xác nhận/hủy nháp hóa đơn, thu/hoàn, nguồn dịch vụ/thuốc không tính trùng. |
| Chính xác với database | Đọc schema SQL thật, whitelist bảng/cột, SQL parameters, kiểm tra FK/trạng thái/precision; computed và tổng theo database/trigger; khóa giao dịch và chống form cũ ghi đè. |
| Sửa ET liên quan | 24 ET tương ứng 201 cột được đối chiếu SQL hiện tại: PK/FK chuỗi NVARCHAR(50), nullable, decimal, DATE/DATETIME2/TIME; computed/aggregate Write(false); private fields, constructor rỗng/đầy đủ, property get/set. |
| Giữ liên kết form gốc, tinh chỉnh UI | Giữ workspace/modal và 25 mục Home; bổ sung liên kết dịch vụ/hóa đơn ở hồ sơ/lần khám/đơn, form hóa đơn theo nguồn và bản xem trước phân trang. |

Database kiểm tra: SQL Server `DESKTOP-C9VCE8A`, `QLBenhVienRangHamMat`, Windows integrated authentication, schema v8 trong `Database/QLBenhVienRangHamMat.sql` và `Database/DB_Nhom4_v8.docx`. Kết nối có thể thay bằng biến môi trường `RHM_CONNECTION_STRING`; xem README.

## Nghiệp vụ đã triển khai

- Xác nhận và các thao tác nhiều dòng dùng transaction. SQL lỗi thì rollback cả SQL và cache; dữ liệu hiển thị chỉ cập nhật sau khi SQL chấp nhận.
- Form lưu bản chụp lúc mở; chặn cập nhật/xóa stale. Các bản xác nhận giữ nguyên nội dung và khóa liên kết cha.
- Số lượng cho phép dấu phẩy/chấm thập phân; không hiểu dấu phẩy là phân cách hàng nghìn. Từ chối vượt độ chính xác cột; thành tiền làm tròn từng dòng như SQL.
- Hóa đơn chỉ lập cho lần khám đã xác nhận, từ dịch vụ đã xác nhận và thuốc đã cấp/ghi sổ. Thuốc chốt giá khi lập; bỏ nguồn đã lập hóa đơn.
- Cấp toàn bộ đơn tại một kho đủ tồn, tự chia FIFO vào các lô còn hạn/đang dùng; thiếu một thuốc thì không ghi sổ phần còn lại.
- Thu không vượt dư phải thu; hoàn không vượt thu ròng và phải có lý do. Khóa hóa đơn/nguồn/tồn trong giao dịch để tránh hai quầy xử lý vượt giới hạn.
- Bản xem trước hóa đơn dùng FlowDocument, tự chia trang và chọn máy in/Microsoft Print to PDF qua hộp thoại in Windows.

## Kết quả kiểm tra thực tế

Tất cả lệnh dưới đây kết thúc với exit code 0 trên Windows/.NET 8:

| Lệnh | Kết quả |
|---|---|
| `dotnet build BenhVienRangHamMat.sln -c Release --no-restore -m:1 -v minimal -p:NuGetAudit=false` | Build thành công, 0 lỗi, 0 cảnh báo. |
| `dotnet run --project RangHamMat.Checks -c Release --no-restore -v quiet -p:WarningLevel=0` | PASS: 4 module lâm sàng, 10 bảng, 57 XAML; trường v8, sự kiện, resources và quy tắc lịch/đơn thuốc. |
| `dotnet run --project RangHamMat.SchemaChecks -c Release --no-restore -v quiet -p:WarningLevel=0` | PASS: 24 bảng, 201 cột, 0 sai lệch; kiểm tra cả private fields, constructor và property. |
| `dotnet run --project RangHamMat.PersistenceChecks -c Release --no-restore -v quiet -p:WarningLevel=0` | PASS: 12 kiểm tra SQL; thêm kiểm tra BUS typed, mã tự cấp, chuẩn hóa ngày giờ và khóa nội dung khi đổi trạng thái. |
| `dotnet run --project RangHamMat.FinanceChecks/RangHamMat.FinanceChecks.csproj --no-restore -v quiet -p:WarningLevel=0` | PASS: 54 kiểm tra tài chính, gồm nguồn hợp lệ/trùng, hủy nháp, FIFO/lô hết hạn/thiếu tồn, giá chốt, giới hạn thu/hoàn, khóa với kết nối SQL thứ hai và đọc qua DAL/BUS từng bảng trong giao dịch đang lưu. |
| `dotnet run --project RangHamMat.WpfSmoke -c Release --no-restore -v quiet -p:WarningLevel=0` | PASS: chạy Window thật và SQL thật; hồ sơ/lịch sử, lịch theo bệnh nhân, phạm vi dịch vụ/hóa đơn, form v8, lưu/sửa/xóa dòng thuốc, lưu kế hoạch/dịch vụ, tổng tiền, stale form, rollback SQL/cache và phân trang hóa đơn dài. |

Các kiểm tra integration tạo fixture trong transaction và rollback. Không để lại bệnh nhân/đơn/hóa đơn/thanh toán thử trong database. Schema hiện tại dùng mã chuỗi, không có IDENTITY. Hai runner SQL có cảnh báo NU1900 vì không tải được dữ liệu vulnerability từ NuGet; cảnh báo này không làm kiểm tra thất bại.

Kiểm tra WPF dùng cửa sổ 1366×768 ở DPI hiện tại và bản in thử 120 dòng trong bộ nhớ. Các lỗi phát hiện bằng kiểm tra đã sửa: dấu phẩy bị hiểu sai thành hàng nghìn, form cũ ghi đè, xóa stale, làm tròn tổng nháp khác SQL và cột nội dung bản in bị co về 0.

## Ảnh kiểm tra WPF

Ảnh được tạo từ giao diện WPF đang chạy, không phải mockup HTML:

- [Form bệnh nhân](../../XemTruoc/verification-v8/benh-nhan-form.png).
- [Form dịch vụ, tiền có phần lẻ](../../XemTruoc/verification-v8/dich-vu-form.png) — fixture rollback.
- [Hóa đơn từ SQL](../../XemTruoc/verification-v8/hoa-don.png).
- [Bản xem trước nhiều dòng](../../XemTruoc/verification-v8/xem-truoc-hoa-don.png) — nhân bản dòng trong bộ nhớ để kiểm tra phân trang, không lưu vào SQL.

## Hướng dẫn và giới hạn kiểm chứng

Mở solution, đặt WPF làm Startup Project, F5; dùng vai trò Quản trị để thử toàn bộ luồng liên phân hệ. Chọn nhân viên tại Hóa đơn trước khi cấp thuốc/lập hóa đơn/thu tiền. Xem `HUONG_DAN_GIAO_DIEN_BENH_VIEN.md` cho luồng từ tiếp nhận đến thanh toán.

Đã kiểm tra bản xem trước và paginator; chưa gửi lệnh tới máy in vật lý hoặc nghiệm thu file từ trình điều khiển Microsoft Print to PDF. DPI 125%/150% và mọi cấu hình máy in chưa được thử. Đăng nhập/vai trò giữ cơ chế source gốc; các phân hệ ngoài phạm vi KeHoach.txt giữ hiện trạng. Các tài liệu v7/báo cáo kiểm tra cũ chỉ có giá trị lịch sử; tài liệu này và README mô tả trạng thái hiện tại.

Chạy `KiemTra.bat` cho build/schema; `KiemTraDatabase.bat` cho SQL/WPF rollback; `KiemTraWpf.bat` cho riêng WPF. Các kiểm tra SQL cần database v8 có danh mục thực tế.
