# Chạy phần nền đã triển khai

Đây là hướng dẫn P01 và nền quyền P02. Chưa có quy trình khám/thanh toán mới đầy đủ để sử dụng qua giao diện.

1. Dùng Windows, .NET 8 SDK và SQL Server. Tạo backup COPY_ONLY CHECKSUM của database đã xác nhận, kiểm tra backup rồi restore sang tên thử riêng; không dùng script cài mới trên dữ liệu đang có.
2. Đặt biến môi trường trong đúng terminal chạy kiểm tra. Ví dụ với xác thực Windows (không có mật khẩu):

```powershell
$env:RHM_CONNECTION_STRING = 'Data Source=.;Initial Catalog=RHM_ClinicalWorkflow_Test_20261025;Integrated Security=True;TrustServerCertificate=True;'
dotnet build .\BenhVienRangHamMat.sln -c Debug -p:NuGetAudit=false
dotnet run --project .\RangHamMat.ClinicalChecks -c Debug --no-restore -p:NuGetAudit=false
```

ClinicalChecks chỉ nhận database có tên `RHM_ClinicalWorkflow_Test_*`. Không có cấu hình, sai tên hoặc không kết nối được đều trả FAIL; không báo PASS/skip thay cho kiểm tra SQL. Dữ liệu mẫu gắn nhãn FIXTURE và rollback trong từng ca, không dùng làm mẫu chuyên môn thật.

Tiền kiểm chỉ đọc, tự chọn database bằng `-d`, cần đọc các bảng kết quả rỗng/bất thường và danh sách chứng từ cũ:

```powershell
sqlcmd -S . -d RHM_ClinicalWorkflow_Test_20261025 -E -C -b -f 65001 -i .\Database\Migrations\20261024_01_Preflight_ClinicalWorkflow.sql
```

3. Chạy hồi quy bằng các lệnh tại [BASELINE.md](BASELINE.md). Bốn suite cũ đang lỗi sẵn được ghi đúng tên; không thể kết luận toàn solution đã nghiệm thu.

## Hợp đồng nền P02

- `BUS_QUYENNGHIEPVU.LayNguCanh(maNV)` chỉ lấy ngữ cảnh trùng phiên đã đăng nhập. Mỗi API đọc lại tài khoản/chức vụ đang hoạt động trong SQL, không tin các cờ quyền trên DTO.
- `PhienDangNhap.KhoiTaoDinhDanhNghiepVu(maTK)` được gọi tại điểm đăng nhập thành công hiện có, gắn ClaimsPrincipal cho API mới. Không thay cách kiểm tra mật khẩu hoặc vai trò của các module cũ. Khi xử lý nền, giữ ExecutionContext của phiên; thiếu principal phải bị từ chối, không tự gán người khác.
- `BUS_KHAMDIEUTRI.LayHangDoi` lấy phiếu theo bác sĩ phiên, mặc định hôm nay, trang tối đa 100. Bộ lọc không nhận bác sĩ khác.
- `LayChiTiet(maLS, nguoiDung)` và `BUS_DONTHUOC.LayChiTietCoQuyen` kiểm tra phạm vi ngay trong truy vấn. Bác sĩ chỉ đọc nháp mình, có thể đọc lịch sử đã xác nhận của BN mình được phân; Admin tra cứu có nhật ký mã nguồn.
- `KiemTraQuyenGhiLanKham` là guard cho lượt nháp thuộc chính bác sĩ; bắt buộc nhận transaction của bên lưu hoặc transaction môi trường. Guard **không phải API lưu**; khi tích hợp phải giữ cùng transaction đến khi lệnh ghi kết thúc để tránh đổi quyền/phân công giữa kiểm tra và lưu.
- Cấu hình ánh xạ ở `DAL/ClinicalWorkflowPermissions.json`, được nhúng khi build. CV004 có quyền thu, CV003 mặc định không thu. Đây là cấu hình nền, chưa cấp quyền mở các màn hình cũ hoặc hiện thực giao dịch thu đủ P09.

Các BUS/DAL cũ còn phục vụ màn hình và hồi quy cũ. Không dùng chúng để tuyên bố đã có kiểm soát phạm vi mới. Việc thay nguồn đọc/ghi, cache theo phiên, điều hướng và trạng thái nút sẽ hoàn tất tiếp trong P02/P04–P11.
