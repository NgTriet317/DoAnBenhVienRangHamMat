# Chạy phần nền đã triển khai

Đây là hướng dẫn P01–P04 đã triển khai. Năm module lâm sàng dùng quyền theo chức vụ; bản build hiện tại cần schema ClinicalWorkflow v2. Quy trình khám/thanh toán đầy đủ ở P05–P12 chưa hoàn tất. Database gốc chưa được nâng cấp tự động; chạy kiểm tra/ứng dụng với biến môi trường của bản sao đã nâng cấp.

1. Dùng Windows, .NET 8 SDK và SQL Server. Tạo backup COPY_ONLY CHECKSUM của database đã xác nhận, kiểm tra backup rồi restore sang tên thử riêng; không dùng script cài mới trên dữ liệu đang có.
2. Đặt biến môi trường trong đúng terminal chạy kiểm tra. Ví dụ với xác thực Windows (không có mật khẩu):

```powershell
$env:RHM_CONNECTION_STRING = 'Data Source=.;Initial Catalog=RHM_ClinicalWorkflow_Test_20261025;Integrated Security=True;TrustServerCertificate=True;'
dotnet build .\BenhVienRangHamMat.sln -c Debug -p:NuGetAudit=false
dotnet run --project .\RangHamMat.ClinicalChecks -c Debug --no-restore -p:NuGetAudit=false
dotnet run --project .\RangHamMat.ClinicalUiChecks -c Debug -p:NuGetAudit=false
```

ClinicalChecks chỉ nhận database có tên `RHM_ClinicalWorkflow_Test_*`. Không có cấu hình, sai tên hoặc không kết nối được đều trả FAIL; không báo PASS/skip thay cho kiểm tra SQL. Dữ liệu mẫu gắn nhãn FIXTURE và rollback trong từng ca, không dùng làm mẫu chuyên môn thật.

Tiền kiểm chỉ đọc, tự chọn database bằng `-d`, cần đọc các bảng kết quả rỗng/bất thường và danh sách chứng từ cũ:

```powershell
sqlcmd -S . -d RHM_ClinicalWorkflow_Test_20261025 -E -C -b -f 65001 -i .\Database\Migrations\20261024_01_Preflight_ClinicalWorkflow.sql
```

3. Chạy hồi quy bằng các lệnh tại [BASELINE.md](BASELINE.md). SchemaChecks đã đạt sau P03; ba suite còn lỗi sẵn được ghi đúng tên trong kết quả kiểm thử. Chưa nghiệm thu toàn solution.

## SQL P03: chọn đúng đường cài

**Database đã có dữ liệu:** backup COPY_ONLY CHECKSUM, RESTORE VERIFYONLY, restore sang bản thử; chạy preflight và đọc danh sách chứng từ lịch sử. Sau đó chạy upgrade và verify trên chính tên bản thử:

```powershell
sqlcmd -S . -d RHM_ClinicalWorkflow_Test_20261025 -E -C -b -f 65001 -i .\Database\Migrations\20261024_02_ClinicalWorkflow.sql
sqlcmd -S . -d RHM_ClinicalWorkflow_Test_20261025 -E -C -b -f 65001 -i .\Database\Migrations\20261024_03_Verify_ClinicalWorkflow.sql
dotnet run --project .\RangHamMat.ClinicalChecks -c Debug --no-restore -- --migration
```

Upgrade chỉ bổ sung schema, khóa `RHM:ClinicalWorkflow:Schema`, ghi extended property `RHM.ClinicalWorkflow.Version=2`; chạy lại kiểm tra hợp đồng rồi thoát. Verify từ chối cột sai kiểu/nullable, thiếu đối tượng hoặc FK/CHECK/trigger bị tắt. Không tự sửa schema khác hợp đồng; không sửa lịch sử, chạy lại seed, trừ tồn hoặc chốt nợ. Hóa đơn cũ mặc định phiên bản 1, tiền đưa/thối vẫn null.

**Cài mới trống:** `Database/QLBenhVienRangHamMat_ClinicalWorkflow.sql` là file độc lập, gồm schema/dữ liệu baseline và phần bổ sung; không phụ thuộc `:r`. Mặc định tạo database riêng `QLBenhVienRangHamMat_ClinicalWorkflow`. Chọn tên mới trước khi chạy toàn file, không đổi sang tên database đang dùng. File từ chối database có bảng và đánh dấu hoàn tất baseline trong session trước khi cho phần nâng cấp chạy. Dùng `sqlcmd -b -f 65001` để dừng khi có lỗi. Giữ `Database/QLBenhVienRangHamMat.sql` làm baseline, không dùng file đó nâng cấp.

**Rollback P03:** `20261024_04_Rollback_ClinicalWorkflow.sql` chỉ hạ schema khi cả sáu bảng bổ sung rỗng và mọi cột mới còn null/default lịch sử. Script giữ transaction và khóa bảng từ lúc kiểm tra đến DDL; gặp dữ liệu mới trả lỗi 52104 và dừng. Đã có dữ liệu thì cần sửa tiến hoặc phục hồi backup có đối chiếu, không xóa dữ liệu để ép rollback.

Hai mode kiểm tra dưới đây **thực thi DDL**, chỉ dùng bản restore thử 46 bảng chưa nâng cấp, với `RHM_CONNECTION_STRING` có tiền tố `RHM_ClinicalWorkflow_Test_`. Chúng so sánh hash tất cả cột/dòng cũ và định nghĩa trigger/view, kiểm tra nâng cấp hai lần; mode rollback thêm hạ/nâng lại và hai ca từ chối có dữ liệu. Không restore hoặc drop database:

```powershell
dotnet run --project .\RangHamMat.ClinicalChecks -c Debug --no-restore -- --migration-upgrade
# Chỉ trên một bản restore baseline khác:
dotnet run --project .\RangHamMat.ClinicalChecks -c Debug --no-restore -- --migration-rollback
```

Mode `--migration` chỉ đọc hợp đồng 30 kiểm tra, chạy được sau upgrade/cài mới. Mặc định ClinicalChecks hiện chạy 80 ca có fixture rollback; `--configuration`, `--new-data`, `--reception` chạy riêng từng nhóm.

Sáu lớp DAL/BUS mới được nhóm trong `DAL_CAUHINHLAMSANG.cs`/`BUS_CAUHINHLAMSANG.cs`. Năm lớp cấu hình cung cấp `LayDanhSach`, `LuuNhap(entity,nguoiDung,banGoc)`; nhận transaction tùy chọn, xác minh lại phiên/quyền, kiểm tra snapshot và audit cùng transaction. Endpoint nháp không nhận chữ ký client. `BUS_BANGIAOTHUOC.LayTheoDon` đọc theo phạm vi đơn thuốc; chưa có API ghi bàn giao. Phê duyệt và giao diện cấu hình thuộc P05/P07/P10.

## Hợp đồng nền P02

- `BUS_QUYENNGHIEPVU.LayNguCanh(maNV)` chỉ lấy ngữ cảnh trùng phiên đã đăng nhập. Mỗi API đọc lại tài khoản/chức vụ đang hoạt động trong SQL, không tin các cờ quyền trên DTO.
- `PhienDangNhap.KhoiTaoDinhDanhNghiepVu(maTK)` được gọi tại điểm đăng nhập thành công hiện có, gắn ClaimsPrincipal cho API mới. Không thay cách kiểm tra mật khẩu hoặc vai trò của các module cũ. Khi xử lý nền, giữ ExecutionContext của phiên; thiếu principal phải bị từ chối, không tự gán người khác.
- `BUS_KHAMDIEUTRI.LayHangDoi` lấy phiếu theo bác sĩ phiên, mặc định hôm nay, trang tối đa 100. Bộ lọc không nhận bác sĩ khác.
- `LayChiTiet(maLS, nguoiDung)` và `BUS_DONTHUOC.LayChiTietCoQuyen` kiểm tra phạm vi ngay trong truy vấn. Bác sĩ chỉ đọc nháp mình, có thể đọc lịch sử đã xác nhận của BN mình được phân; Admin tra cứu có nhật ký mã nguồn.
- `KiemTraQuyenGhiLanKham` là guard cho lượt nháp thuộc chính bác sĩ; bắt buộc nhận transaction của bên lưu hoặc transaction môi trường. Guard **không phải API lưu**; khi tích hợp phải giữ cùng transaction đến khi lệnh ghi kết thúc để tránh đổi quyền/phân công giữa kiểm tra và lưu.
- Cấu hình ánh xạ ở `DAL/ClinicalWorkflowPermissions.json`, được nhúng khi build. CV004 có quyền thu, CV003 mặc định không thu. MainWindow đọc quyền này cho năm module lâm sàng; quyền module khác giữ nguyên. Quyền thu ở đây chưa phải giao dịch thu đủ P09.

Trong chế độ lâm sàng, `DuLieuGiaoDien` nạp snapshot đã lọc tại SQL; lưu/xóa gọi guard DAL cùng transaction với BUS/DAL thực hiện ghi. Đổi định danh xóa các collection cũ. Form bác sĩ và tiếp nhận điền người thực hiện từ phiên; điều dưỡng chỉ sửa kết quả/thời gian dòng được giao.

Hóa đơn lịch sử chỉ cho đối chiếu/in lại; các lệnh thu/hoàn/cấp thuốc cũ bị khóa trong chế độ này. BUS/DAL tài chính cũ vẫn giữ cho hồi quy và tích hợp ngoài luồng mới. Chưa có giao dịch thu đủ P09 hoặc bàn giao P10.

`ClinicalUiChecks` chạy control WPF thật trên bản thử, gồm quyền module, cache đổi phiên, nút chỉ đọc, gọi ghi trực tiếp bị từ chối, lịch nhúng và lịch độc lập. Kết quả 8 ca không thay thế thử DPI/kích thước/nội dung dài toàn bộ ở P11/P12.

## Sử dụng tiếp nhận P04

1. Đăng nhập tiếp nhận CV004 hoặc điều dưỡng CV003; chọn đúng bệnh nhân theo mã, ngày sinh và điện thoại. Danh sách tìm được ngày sinh dạng `dd/MM/yyyy` hoặc `yyyy-MM-dd`, hiển thị lần khám gần nhất trong phạm vi được đọc. Bốn tab hồ sơ giữ lịch sử theo mã; lịch hẹn trong hồ sơ chỉ xem, không sửa/hủy tại đây.
2. Chọn Tiếp nhận trong hồ sơ. Nếu chưa có hồ sơ bệnh án, hệ thống lập đúng một hồ sơ trong cùng transaction khi lưu; lỗi tiếp nhận không để lại hồ sơ rỗng. Chọn trực tiếp hoặc lịch còn hiệu lực hôm nay của đúng người bệnh.
3. Chọn dịch vụ khám có chuyên khoa; chọn bác sĩ đang làm việc và có tài khoản hoạt động. Danh sách đối chiếu ca/phòng, số đang chờ, lịch vắng và nghỉ đã duyệt. Đổi dịch vụ xóa lựa chọn bác sĩ/phòng cũ. Nguồn lịch hẹn giữ bác sĩ/phòng/dịch vụ đã có; muốn đổi nguồn thì xử lý lịch trước.
4. Có lịch làm việc thì phòng khớp lịch. Thiếu lịch hiển thị rõ; CV004 mặc định có `DieuPhoiNgoaiLich` được nhập lý do điều phối tối đa 500 ký tự, ghi nhật ký. CV003 chưa có quyền này; không vượt lịch xác định nghỉ/vắng, dù có quyền điều phối.
5. Nhập lý do đến khám, xác nhận. Nhân viên lấy từ phiên, giờ từ SQL và số theo ngày/phòng; không nhập tên đồng nghiệp/số thứ tự. Thành công báo bác sĩ nhận, số và mã phiếu; không tạo MaLS hoặc mở chẩn đoán. Cache chỉ tải lại sau commit. Bấm lại giữ biên nhận; cùng lịch hẹn hoặc cùng lượt trực tiếp đang hiệu lực không sinh phiếu thứ hai.

`BUS_TIEPNHAN.LayBacSi(maDV,nguoiDung)` và `TiepNhan(YeuCauTiepNhan,nguoiDung)` nhận transaction tùy chọn. API đọc lại quyền/tài khoản và giữ khóa nguồn, gồm cả khi transaction ngoài là ReadCommitted. Không truyền nhân viên, giờ, trạng thái hay số thứ tự trong DTO. Kết quả có MaBN/MaHS/MaPhieu/MaBS/SoThuTu/TrangThai; bác sĩ bắt đầu khám bằng P05 sau này.

Kiểm tra đồng thời cần dữ liệu commit để hai kết nối nhìn thấy. Hai mode dưới chạy trên bản thử đã nâng cấp; mode concurrency và fixture WPF **commit dữ liệu giả rồi dọn theo mã GUID trong finally**, khác với 80 ca SQL mặc định rollback. Không chạy trên database thật:

```powershell
dotnet run --project .\RangHamMat.ClinicalChecks -c Debug --no-restore -- --reception
dotnet run --project .\RangHamMat.ClinicalChecks -c Debug --no-restore -- --reception-concurrency
dotnet run --project .\RangHamMat.ClinicalUiChecks -c Debug --no-restore
```

ClinicalUiChecks hiện có 14 ca, gồm lưu form thật, tải cache sau commit, lọc dịch vụ/bác sĩ, lịch sử chỉ đọc và phép tính 1.000 BN/10.000 lượt trong bộ nhớ. Ảnh form trước/sau lưu ở `obj/ClinicalWorkflowP04/`; dữ liệu ảnh là FIXTURE. Kiểm tra này không thay thế thử toàn ứng dụng với DPI/SQL lớn ở P11/P12.
