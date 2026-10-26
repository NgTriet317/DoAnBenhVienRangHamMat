# Baseline ngày 25/10/2026

Đã đọc cả hai bản kế hoạch tại gốc dự án. Bản Word có 844 đoạn có nội dung; khác biệt đối chiếu là trang bìa, tiêu đề và ký tự xuống dòng/zero-width trong bảng, không thấy quyết định nghiệp vụ mâu thuẫn. Giữ nguyên file Word.

- Solution thực tế: `BenhVienRangHamMat.sln`, Git `7470bf1` trên nhánh `tan`.
- Ban đầu không có sửa source, chỉ hai file kế hoạch chưa theo dõi trong Git.
- Nhánh triển khai: `cai-to-quy-trinh-kham-6-chuc-nang`.
- WPF `net8.0-windows`, ET/BUS/DAL `net8.0`, .NET SDK 8.0.400.
- Database đang dùng có 46 bảng. Không chạy script cài mới hoặc migration trên database này.
- SHA256 đầu vào Markdown: `8CFFE38A784C3CB96D891463242F5175FA32936802D6EEF20D18F6451E1AB849`.
- SHA256 Word: `7BAA235C67555AA80B4FF07BDE40BEF4A35336424B56B6E5F12FEEE9F5A630AA`.

## Điểm quay lại SQL

Đã thực hiện `BACKUP DATABASE ... WITH COPY_ONLY, CHECKSUM`, `RESTORE VERIFYONLY ... WITH CHECKSUM` và restore thành công sang `RHM_ClinicalWorkflow_Test_20261025`. File backup: `RHM_ClinicalBaseline_20261025.bak`, trong thư mục Backup mặc định của SQL Server trên máy này. Đây là bản sao dữ liệu thật, giữ ở máy SQL, không đưa vào Git.

Mọi bộ kiểm tra có ghi dữ liệu dùng `RHM_CONNECTION_STRING` trỏ bản thử. Không đổi cấu hình mặc định của ứng dụng. Fixture ClinicalChecks còn kiểm tra tên database bắt đầu bằng `RHM_ClinicalWorkflow_Test_` và rollback toàn bộ.

## Kết quả chạy trước khi sửa source

| Lệnh (từ gốc dự án) | Kết quả thực tế |
| --- | --- |
| `dotnet build BenhVienRangHamMat.sln -c Debug --no-restore -p:NuGetAudit=false` | Exit 0, 0 lỗi, 57 cảnh báo có sẵn |
| `dotnet run --project RangHamMat.Checks -c Debug -p:NuGetAudit=false -- .` | Exit 1; `KiemTraGiaoDien.cs:25`, trùng khóa `MaCa` khi tạo dictionary |
| `dotnet run --project RangHamMat.SchemaChecks -c Debug -p:NuGetAudit=false` | Exit 1; `Each ET must map to exactly one SQL table`, 39 bảng / 303 cột / 1 lỗi |
| `dotnet run --project RangHamMat.PersistenceChecks -c Debug -p:NuGetAudit=false` | Exit 1; `PersistenceFixture.cs:49`, CS1729: `BUS_NHANVIEN` không có constructor nhận transaction |
| `dotnet run --project RangHamMat.FinanceChecks -c Debug -p:NuGetAudit=false` | Exit 0; 54 ca transaction SQL, rollback |
| `dotnet run --project RangHamMat.SupplierChecks -c Debug -p:NuGetAudit=false` | Exit 0; SQL, nút lưu, validation và WPF 1366/1024; rollback |
| `dotnet run --project RangHamMat.MaintenanceChecks -c Debug -p:NuGetAudit=false` | Exit 0; SQL, thao tác WPF, lịch sử và hạn bảo trì; rollback |
| `dotnet run --project RangHamMat.ReportChecks -c Debug -p:NuGetAudit=false` | Exit 0; 18 chuyên đề / 4 nhóm; chạy mặc định, chưa chạy `--sql` hoặc `--render` |
| `dotnet run --project RangHamMat.WpfSmoke -c Debug -p:NuGetAudit=false` | Exit 1 khi biên dịch; `NhapLieuChecks.cs:139,140,143,147`, CS1061 thiếu `NhanSu.TryReadPosition/TryReadEmployee`; chưa chạy được smoke |

Các lần thử `--no-build` ban đầu không có executable Debug; một số project cũng chưa có `project.assets.json`. Đã chạy lại có restore/build để lấy kết quả thật trong bảng. Kết nối SqlClient bị sandbox chặn đã được chạy lại với quyền được cấp; không sửa connection hay thuật toán để né lỗi.

Log thô tại `obj/ClinicalWorkflowBaseline/` trên máy này; tài liệu trên là kết quả được lưu trong Git. Các ảnh do Supplier/MaintenanceChecks sinh đã được khôi phục về baseline.

## Tiền kiểm và đối chiếu

Đã chạy `Database/Migrations/20261024_01_Preflight_ClinicalWorkflow.sql` bằng sqlcmd với `-f 65001` trên bản thử, exit 0. Không thiếu bảng/trigger/view được kiểm tra, không có FK/trigger bị vô hiệu hóa, không phát hiện hai hồ sơ/BN, hai lần khám/phiếu, sai liên kết phiếu–lần khám, trùng lịch tiếp nhận đang hiệu lực hoặc trùng nguồn hóa đơn trong các truy vấn tiền kiểm.

| Đại lượng | Giá trị trên bản thử trước thay đổi |
| --- | ---: |
| Bệnh nhân | 10 |
| Hóa đơn | 10 |
| Tổng `HOA_DON.TongTien` | 732.500,00 |
| Tổng `SoTienPhaiThu` | 717.500,00 |
| Thu đã xác nhận | 7 giao dịch, 487.500,00 |
| Hoàn đã xác nhận | 1 giao dịch, 20.000,00 |
| Thu nháp | 1 giao dịch, 1.000,00 |
| Thu hủy | 1 giao dịch, 20.000,00 |
| Vị trí hàng–lô–kho trong view tồn | 10 |
| Tổng lượng tồn (chỉ dùng đối chiếu kỹ thuật, nhiều đơn vị) | 4.014,000 |

Các chứng từ cũ cần giữ nguyên: HD004/LS004 nháp còn 100.000; HD005/LS005 nháp còn 150.000; HD006–HD010 nháp tổng 0; PX001–PX008 đã ghi sổ và liên kết DT001–DT008 qua chi tiết xuất/chi tiết đơn. Không tự xác nhận, xóa, thu bù hoặc xuất lại các nguồn này.

Đã xác minh ánh xạ thực tế: CV002 bác sĩ (2), CV003 điều dưỡng (1), CV004 tiếp nhận (1), CV005 dược sĩ (1), CV007 thu ngân (1); vai trò hệ thống giữ Admin/User. Nhân viên/điện thoại/bệnh án không được xuất ra báo cáo này.

Đối chiếu sau P08 (26/10/2026): bản thử vẫn 52 bảng v2, 10 BN/HS/phiếu/HD/đơn/chi tiết đơn; tổng hóa đơn 732.500, thu ròng 467.500 và tồn 4.014. Không còn fixture CW hoặc cấu hình điều trị thử. Database P08Concurrency mới đã dọn sau kiểm chứng; không đổi database gốc/chứng từ lịch sử. Xem log obj/ClinicalWorkflowP08/data-final.log và kết quả kiểm thử P08.
