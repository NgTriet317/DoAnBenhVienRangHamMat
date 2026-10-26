# Chạy phần nền đã triển khai

Đây là hướng dẫn P01–P07 đã triển khai. Năm module lâm sàng dùng quyền theo chức vụ; bản build hiện tại cần schema ClinicalWorkflow v2. Quy trình khám/thanh toán đầy đủ ở P08–P12 chưa hoàn tất. Database gốc chưa được nâng cấp tự động; chạy kiểm tra/ứng dụng với biến môi trường của bản sao đã nâng cấp.

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

Mode `--migration` chỉ đọc hợp đồng 30 kiểm tra, chạy được sau upgrade/cài mới. Mặc định ClinicalChecks hiện chạy 146 ca có fixture rollback; `--configuration`, `--new-data`, `--reception`, `--examination`, `--services`, `--prescriptions` chạy riêng từng nhóm. Nhóm khám/kế hoạch có 24 ca, dịch vụ 20 ca, đơn thuốc 22 ca.

Sáu lớp DAL/BUS mới được nhóm trong `DAL_CAUHINHLAMSANG.cs`/`BUS_CAUHINHLAMSANG.cs`. Năm lớp cấu hình cung cấp `LayDanhSach`, `LuuNhap(entity,nguoiDung,banGoc)`; nhận transaction tùy chọn, xác minh lại phiên/quyền, kiểm tra snapshot và audit cùng transaction. Endpoint nháp không nhận chữ ký client. P05 bổ sung duyệt mẫu điều trị qua aggregate khám bên dưới; cấu hình kê đơn thuộc P07. `BUS_BANGIAOTHUOC.LayTheoDon` đọc theo phạm vi đơn thuốc; chưa có API ghi bàn giao P10.

## Hợp đồng nền P02

- `BUS_QUYENNGHIEPVU.LayNguCanh(maNV)` chỉ lấy ngữ cảnh trùng phiên đã đăng nhập. Mỗi API đọc lại tài khoản/chức vụ đang hoạt động trong SQL, không tin các cờ quyền trên DTO.
- `PhienDangNhap.KhoiTaoDinhDanhNghiepVu(maTK)` được gọi tại điểm đăng nhập thành công hiện có, gắn ClaimsPrincipal cho API mới. Không thay cách kiểm tra mật khẩu hoặc vai trò của các module cũ. Khi xử lý nền, giữ ExecutionContext của phiên; thiếu principal phải bị từ chối, không tự gán người khác.
- `BUS_KHAMDIEUTRI.LayHangDoi` lấy phiếu theo bác sĩ phiên, mặc định hôm nay, trang tối đa 100. Bộ lọc không nhận bác sĩ khác.
- `LayChiTiet(maLS, nguoiDung)` và `BUS_DONTHUOC.LayChiTietCoQuyen` kiểm tra phạm vi ngay trong truy vấn. Bác sĩ chỉ đọc nháp mình, có thể đọc lịch sử đã xác nhận của BN mình được phân; Admin tra cứu có nhật ký mã nguồn.
- `KiemTraQuyenGhiLanKham` là guard cho lượt nháp thuộc chính bác sĩ; bắt buộc nhận transaction của bên lưu hoặc transaction môi trường. Guard **không phải API lưu**; khi tích hợp phải giữ cùng transaction đến khi lệnh ghi kết thúc để tránh đổi quyền/phân công giữa kiểm tra và lưu.
- Cấu hình ánh xạ ở `DAL/ClinicalWorkflowPermissions.json`, được nhúng khi build. CV004 có quyền thu, CV003 mặc định không thu. MainWindow đọc quyền này cho năm module lâm sàng; quyền module khác giữ nguyên. Quyền thu ở đây chưa phải giao dịch thu đủ P09.

Trong chế độ lâm sàng, `DuLieuGiaoDien` nạp snapshot đã lọc tại SQL; lưu/xóa gọi guard DAL cùng transaction với BUS/DAL thực hiện ghi. Đổi định danh xóa các collection cũ. Form bác sĩ và tiếp nhận điền người thực hiện từ phiên; điều dưỡng chỉ sửa kết quả/thời gian dòng được giao.

Hóa đơn lịch sử chỉ cho đối chiếu/in lại; các lệnh thu/hoàn/cấp thuốc cũ bị khóa trong chế độ này. BUS/DAL tài chính cũ vẫn giữ cho hồi quy và tích hợp ngoài luồng mới. Chưa có giao dịch thu đủ P09 hoặc bàn giao P10.

`ClinicalUiChecks` chạy control WPF thật trên bản thử, gồm quyền module, cache đổi phiên, nút chỉ đọc, gọi ghi trực tiếp bị từ chối, lịch nhúng và lịch độc lập. Các ca bổ sung P04/P05 bên dưới không thay thế thử DPI/kích thước/nội dung dài toàn bộ ở P11/P12.

## Sử dụng tiếp nhận P04

1. Đăng nhập tiếp nhận CV004 hoặc điều dưỡng CV003; chọn đúng bệnh nhân theo mã, ngày sinh và điện thoại. Danh sách tìm được ngày sinh dạng `dd/MM/yyyy` hoặc `yyyy-MM-dd`, hiển thị lần khám gần nhất trong phạm vi được đọc. Bốn tab hồ sơ giữ lịch sử theo mã; lịch hẹn trong hồ sơ chỉ xem, không sửa/hủy tại đây.
2. Chọn Tiếp nhận trong hồ sơ. Nếu chưa có hồ sơ bệnh án, hệ thống lập đúng một hồ sơ trong cùng transaction khi lưu; lỗi tiếp nhận không để lại hồ sơ rỗng. Chọn trực tiếp hoặc lịch còn hiệu lực hôm nay của đúng người bệnh.
3. Chọn dịch vụ khám có chuyên khoa; chọn bác sĩ đang làm việc và có tài khoản hoạt động. Danh sách đối chiếu ca/phòng, số đang chờ, lịch vắng và nghỉ đã duyệt. Đổi dịch vụ xóa lựa chọn bác sĩ/phòng cũ. Nguồn lịch hẹn giữ bác sĩ/phòng/dịch vụ đã có; muốn đổi nguồn thì xử lý lịch trước.
4. Có lịch làm việc thì phòng khớp lịch. Thiếu lịch hiển thị rõ; CV004 mặc định có `DieuPhoiNgoaiLich` được nhập lý do điều phối tối đa 500 ký tự, ghi nhật ký. CV003 chưa có quyền này; không vượt lịch xác định nghỉ/vắng, dù có quyền điều phối.
5. Nhập lý do đến khám, xác nhận. Nhân viên lấy từ phiên, giờ từ SQL và số theo ngày/phòng; không nhập tên đồng nghiệp/số thứ tự. Thành công báo bác sĩ nhận, số và mã phiếu; không tạo MaLS hoặc mở chẩn đoán. Cache chỉ tải lại sau commit. Bấm lại giữ biên nhận; cùng lịch hẹn hoặc cùng lượt trực tiếp đang hiệu lực không sinh phiếu thứ hai.

`BUS_TIEPNHAN.LayBacSi(maDV,nguoiDung)` và `TiepNhan(YeuCauTiepNhan,nguoiDung)` nhận transaction tùy chọn. API đọc lại quyền/tài khoản và giữ khóa nguồn, gồm cả khi transaction ngoài là ReadCommitted. Không truyền nhân viên, giờ, trạng thái hay số thứ tự trong DTO. Kết quả có MaBN/MaHS/MaPhieu/MaBS/SoThuTu/TrangThai; bác sĩ bắt đầu khám bằng P05 bên dưới.

Kiểm tra đồng thời cần dữ liệu commit để hai kết nối nhìn thấy. Các mode P04/P05 dưới chạy trên bản thử đã nâng cấp; mode concurrency và fixture tiếp nhận WPF **commit dữ liệu giả rồi dọn theo mã GUID trong finally**, khác với 124 ca SQL mặc định rollback. Ca ký khám/mẫu/dịch vụ WPF dùng transaction ngoài và rollback. Mode ký dịch vụ P06 có database riêng bên dưới. Không chạy trên database thật:

```powershell
dotnet run --project .\RangHamMat.ClinicalChecks -c Debug --no-restore -- --reception
dotnet run --project .\RangHamMat.ClinicalChecks -c Debug --no-restore -- --reception-concurrency
dotnet run --project .\RangHamMat.ClinicalUiChecks -c Debug --no-restore
```

ClinicalUiChecks hiện có 35 ca, gồm tiếp nhận, khám, dịch vụ và đơn thuốc. Ảnh tiếp nhận trước/sau lưu ở `obj/ClinicalWorkflowP04/`; dữ liệu ảnh là FIXTURE. Kiểm tra này không thay thế thử toàn ứng dụng với DPI/SQL lớn ở P11/P12.

## Sử dụng khám và kế hoạch P05

1. Đăng nhập bác sĩ CV002, mở Khám điều trị → Bắt đầu khám. Chọn phiếu chờ thuộc mình; lọc ngày và bệnh nhân nếu cần. Bắt đầu lại cùng phiếu trả cùng MaLS; không cho bác sĩ khác lấy lượt dù biết mã. Header giữ bệnh nhân, dị ứng/tiền sử, phiếu và bác sĩ nguồn.
2. Nhập triệu chứng, chẩn đoán và hướng xử trí; tab Tình trạng bệnh cho chọn danh mục hoặc mô tả thủ công, ghi mức độ/vị trí răng. Lưu nháp chưa chuyển chờ thanh toán. Lỗi trường/ô số/ngày được giữ tại form; bản SQL đã đổi báo xung đột, không ghi đè. Tải lại là hành động bỏ thay đổi chưa lưu.
3. Lưu tình trạng rồi Gợi ý mẫu. Chỉ mẫu đã duyệt có danh mục/mức độ phù hợp được gợi ý, mẫu tổng quát đứng sau. Xem nội dung, nguồn, phiên bản và bác sĩ duyệt trước áp dụng từng mẫu; nhiều tình trạng không tự cộng bước. Không có mẫu thì lập kế hoạch thủ công. Áp lại cùng nguồn/mẫu dùng cùng kế hoạch, bản sao không đổi theo mẫu nguồn.
4. Lưu kế hoạch, kiểm tra lượng/giá dự kiến, buổi và ngày rồi duyệt; không sinh dịch vụ thực hiện hay khoản thu. Kế hoạch nháp có thể hủy và giữ lịch sử. Tình trạng đã được kế hoạch tham chiếu không xóa/đổi danh mục nguồn. Các bước buổi sau chưa thực hiện không chặn đóng buổi nếu kế hoạch đã duyệt.
5. Tổng kết: chọn Không kê thuốc và ghi lý do khi phù hợp, hoặc dùng đơn đã xác nhận. Hoàn tất chỉ nhận triệu chứng/chẩn đoán/xử trí và ít nhất một tình trạng, không còn đơn/dịch vụ/kế hoạch nháp. Xác nhận khóa nội dung/tình trạng và chuyển phiếu Chờ thanh toán. Soạn/ký đơn mới và thao tác thực hiện dịch vụ đầy đủ tiếp tục ở P06/P07; thu tiền ở P09.
6. Cấu hình hỗ trợ: bác sĩ hoặc Admin soạn danh mục/mẫu nháp; chỉ bác sĩ có quyền chuyên môn duyệt. Hệ thống ghi bác sĩ/thời gian từ phiên, audit thay đổi. Mẫu đã duyệt được ngừng áp dụng hoặc sao chép sang mã/phiên bản mới; không sửa nội dung bản đã duyệt. Không có mẫu điều trị tự duyệt sẵn.
7. Đổi tab giữ nháp. Đổi bộ lọc/bệnh nhân/lần khám, cache refresh hoặc quay lại bị chặn nếu đang nhập, kể cả ô bảng/ngày chưa commit và danh mục mới chưa lưu. Lịch sử lần khám trong hồ sơ chỉ xem; nhập liệu tại Khám điều trị. Guard đóng cửa sổ/chuyển phân hệ toàn ứng dụng thuộc P11.

`BUS_KHAMDIEUTRI` bổ sung `BatDauKham`, `LayKhongGianKham`/`LayBanNhap`, `LuuKham`, `GoiYKeHoach`, `ApDungMau`, `LayKeHoach`, `LuuKeHoach`, `XacNhanKeHoach`, `HuyKeHoach`, `HoanTatKham`; cấu hình có `LuuMauDieuTri`, `LayMauDieuTri`, `DuyetMauDieuTri`, `NgungApDungMau`. Các DTO ghi chỉ nhận trường được phép và dấu vân tay bản gốc. API xác minh lại danh tính, khóa nguồn và kiểm tra snapshot. Khi không truyền transaction, API tự commit/rollback; khi truyền transaction ngoài, nơi gọi chịu trách nhiệm commit/rollback và chỉ tải cache sau commit. Form sản phẩm dùng transaction tự quản lý; transaction ngoài trong WPF phục vụ kiểm tra rollback.

```powershell
dotnet run --project .\RangHamMat.ClinicalChecks -c Debug --no-restore -- --examination
dotnet run --project .\RangHamMat.ClinicalChecks -c Debug --no-restore -- --examination-concurrency
dotnet run --project .\RangHamMat.ClinicalUiChecks -c Debug --no-restore
```

Mode đồng thời P05 kiểm tra hai kết nối bắt đầu cùng phiếu và lưu cùng snapshot; dữ liệu giả được commit rồi dọn. Các ca áp mẫu idempotent hiện kiểm tra retry tuần tự và ràng buộc/khóa SQL; chưa tuyên bố kiểm thử tải nhiều bác sĩ hay nghiệm thu toàn bộ AT01–AT34. Ảnh không gian khám với FIXTURE ở `obj/ClinicalWorkflowP05/kham-bat-dau.png`.

## Sử dụng dịch vụ thực hiện P06

1. Bác sĩ mở Dịch vụ thực hiện từ lượt đang khám hoặc chọn lượt trong bộ chọn. Nhận diện cố định gồm tên/mã BN, hồ sơ, lượt và bác sĩ phụ trách. Chọn lượt thông thường không làm mất các BN khác khỏi bộ chọn; điều hướng từ hồ sơ vẫn giữ phạm vi BN đã truyền.
2. Tab Dự kiến chỉ lấy bước đã duyệt cùng MaHS, gồm bước còn lại từ buổi trước. Chọn bước, bấm Thực hiện bước đang chọn: dịch vụ lấy từ bước và không được tự thay. Lượng còn lại đã trừ dòng xác nhận và nháp đang giao; không âm thầm tăng dự kiến.
3. Chọn người làm là bác sĩ/điều dưỡng có tài khoản hoạt động; nhập thời gian từ lúc bắt đầu lượt đến hiện tại, lượng/giá thực hiện và kết quả. Dịch vụ phát sinh ngoài kế hoạch cần lý do tối đa 1000 ký tự; lý do có nhật ký trong cùng transaction.
4. Lưu nháp rồi bác sĩ phụ trách kiểm tra, xác nhận riêng. Chỉ dòng xác nhận là nguồn tính phí; nháp/hủy và kế hoạch không thành khoản thu. Hai lần xác nhận cùng dòng dùng cùng nguồn, không ký/tính hai lần. Dòng xác nhận hoặc đã tính phí khóa sửa/hủy; muốn đổi nguồn của nháp phải hủy và lập dòng mới.
5. Điều dưỡng chỉ thấy dòng được giao, chỉ sửa kết quả/thời gian; không tự tạo, đổi dịch vụ/lượng/giá/người làm hoặc xác nhận. Thu ngân/tiếp nhận có quyền thu chỉ nhận thông tin nguồn, lượng, giá, trạng thái; kết quả và lý do chuyên môn bị loại tại DAL. Admin tra cứu được ghi nhật ký kể cả mở trực tiếp mã dòng.
6. Tab Thực tế hiển thị tên dịch vụ/người làm, thời gian, lượng, giá chốt, kết quả theo quyền, trạng thái và đã tính phí. Giá trị xác nhận tổng hợp đúng lượt hiện tại. Phát hành hóa đơn/thu đủ ở P08/P09; P06 không tự tạo hóa đơn.
7. Nháp được giữ khi đổi tab; đổi lượt/dòng bị chặn tới khi lưu hoặc Tải lại để bỏ thay đổi. Form chung/cache không được ghi/xóa dịch vụ để bỏ qua kiểm tra nguồn và lượng. Bảo vệ đóng/chuyển phân hệ toàn ứng dụng vẫn thuộc P11.

`BUS_DICHVUTHUCHIEN` thêm `LayLuotThucHien` (tối đa 100 lượt có quyền), `LayKhongGianThucHien`, `LayThucHien`, `LuuThucHien`, `XacNhanThucHien`, `HuyThucHien`. DTO ghi có nguồn, người làm, giờ, lượng/giá, kết quả, lý do khi phát sinh và dấu vân tay. Nhận transaction ngoài thì bên gọi chịu trách nhiệm commit/rollback và chỉ tải cache sau commit; mặc định API tự quản lý transaction. CRUD cũ giữ cho tương thích ngoài luồng mới.

```powershell
dotnet run --project .\RangHamMat.ClinicalChecks -c Debug --no-restore -- --services
dotnet run --project .\RangHamMat.ClinicalUiChecks -c Debug --no-restore
```

Mode `--services-concurrency` **commit fixture có chữ ký**, chỉ chấp nhận database mới riêng có tiền tố `RHM_ClinicalWorkflow_Test_P06Concurrency_`, chưa có fixture CW. Tạo từ SQL cài mới v2 vào tên mới (không ghi đè), đặt `RHM_CONNECTION_STRING` trỏ đúng database đó rồi chạy mode. Mode kiểm tra hai kết nối xác nhận cùng dòng, hai bác sĩ/hai lượt tranh lượng cuối và ReadCommitted giữ khóa nguồn. Sau kiểm tra giữ log và dọn database thử tạm đã tự tạo; không tắt trigger để xóa chứng từ đã ký trên bản thử chính. Lượt triển khai đã tạo/dọn `RHM_ClinicalWorkflow_Test_P06Concurrency_20261026`, kết quả ở `obj/ClinicalWorkflowP06/services-concurrency-final.log` / `concurrency-cleanup.log`.

## Sử dụng kê đơn và hướng dẫn thuốc P07

1. Bác sĩ mở Đơn thuốc theo đúng lượt, đối chiếu tên/mã BN, ngày sinh/tuổi, dị ứng, tiền sử và chẩn đoán ở đầu màn hình. Thông tin chưa có được ghi rõ cần đối chiếu, không suy là không dị ứng. Nhắc dị ứng tự do không phải hệ thống phát hiện tương tác/chống chỉ định đầy đủ.
2. Tìm theo nhóm, tên/mã, hàm lượng, đơn vị/dạng cấp phát. Bộ lọc dạng dùng DonViTinh có sẵn, không có danh mục dạng bào chế riêng. Chọn nhiều thuốc, Thêm các thuốc đã chọn; thêm lại focus dòng đã có. Giá và tồn là tham khảo, chưa giữ hàng.
3. Nhập đủ liều, đường dùng, hướng dẫn, ngày nguyên dương và lượng dương ngay khi lưu nháp. Không suy lượng từ chuỗi liều. Mẫu chỉ áp bằng nút riêng khi đã duyệt, đúng phiên bản và bác sĩ xác nhận phù hợp đối tượng; chưa có mẫu thì nhập thủ công. Có thể Bỏ mẫu, giữ chỉ định nhập để tự ghi theo đánh giá của bác sĩ.
4. Lưu nháp rồi ký riêng sau khi kiểm tra toàn bộ dòng. Nháp đã lưu giữ tên/hàm lượng/đơn vị tham chiếu; danh mục đổi trước ký sẽ báo xung đột. Lưu lại, đối chiếu thông tin mới rồi ký; không tự đổi liều. Tên/hàm lượng/đơn vị và mọi chỉ định ở bản ký giữ nguyên.
5. Chọn đơn ký → Soạn đơn thay thế, ghi lý do, sửa và lưu. Chỉ một nháp thay cho cùng nguồn; đơn cũ vẫn hiệu lực tới khi ký mới. Sau hoàn tất chuyên môn vẫn được thay khi chưa có hóa đơn/xuất thuốc; không mở lại nội dung khám đã ký. Đơn bổ sung độc lập cần lý do cả lúc lưu sửa và ký. Hủy nháp không ngừng đơn nguồn.
6. Nguồn đã lập hóa đơn hoặc có xuất phụ thuộc không cho thay; gồm hóa đơn hủy hoặc dòng xuất nháp đang giữ FK. Bước P08/P09 phải gọi kiểm tra nháp trong cùng transaction trước tính phí/thu; API KiemTraDonChoThu từ chối cả đơn thay và dịch vụ còn nháp. P07 chưa dựng bảng phí hoặc thu tiền.
7. Cấu hình hướng dẫn: bác sĩ/Admin/dược sĩ có quyền soạn nhóm và nháp; chỉ người có DuyetHuongDanThuoc mới duyệt (mặc định bác sĩ CV002). Người/giờ duyệt do server ghi. Tìm thuốc theo tên/mã để truy cập ngoài 100 kết quả đầu; không tự duyệt hoặc seed liều. Cấu hình đã duyệt cần Soạn phiên bản nháp tiếp theo, lưu và duyệt lại; bản trước có trong audit, không sửa chỉ định đã sao chép.
8. Chỉ bản ký được xem/in; preview giữ cố định mã đơn dù editor đổi nguồn, đọc lại hiệu lực/quyền ngay trước gửi in. Bản in có ngày sinh và thông tin chốt. Đơn cũ thiếu cột chốt dùng lịch sử chỉ đọc theo dữ liệu cũ; bản mới không dùng tên/hàm lượng hiện tại để thay snapshot. In máy thật/PDF phụ thuộc thiết bị/hộp thoại Windows; bộ kiểm tra xác minh nguồn và document, không tự gửi lệnh in.
9. Ô bảng đang nhập được tính là nháp; đổi nguồn bị chặn tới khi lưu hoặc Tải lại. Cấu hình mới giữ đúng mã thuốc khi chặn đổi chọn, Tải lại bỏ được nháp. Lịch sử trong hồ sơ chỉ xem, có Mở khu kê đơn; form chung không ghi/xóa riêng đầu hoặc dòng đơn. Đóng/chuyển module toàn ứng dụng thuộc P11.

BUS_DONTHUOC thêm TimThuoc, LayLuotKeDon, LayKhongGianKeDon, LayBanNhapDon, LuuDon, XacNhanDon, HuyDonNhap, LayBanInDon, KiemTraDonChoThu; cấu hình có LayHuongDan, LuuHuongDan, DuyetHuongDan. Mỗi API xác thực lại từ SQL, khóa nguồn và so snapshot; nhận transaction ngoài thì bên gọi chịu trách nhiệm commit/rollback, chỉ tải lại dữ liệu sau commit. Lưu/ký/thay đơn không trừ kho. CRUD legacy ngoài clinical vẫn giữ để tương thích.

```powershell
dotnet run --project .\RangHamMat.ClinicalChecks -c Debug --no-restore -- --prescriptions
dotnet run --project .\RangHamMat.ClinicalUiChecks -c Debug --no-restore
```

Mode --prescriptions-concurrency commit chữ ký trên database mới riêng có tiền tố RHM_ClinicalWorkflow_Test_P07Concurrency_, chưa có fixture CW; từ chối bản thử chính. Tạo từ SQL cài mới v2 vào tên mới có guard không ghi đè rồi đặt RHM_CONNECTION_STRING đúng tên trước chạy. Kiểm tra hai kết nối ký thay cùng đơn, xung đột sửa nháp, một nháp thay cho nguồn, khóa ReadCommitted và tồn không đổi. Lượt triển khai đã tạo/dọn RHM_ClinicalWorkflow_Test_P07Concurrency_20261026, không tắt trigger để xóa lịch sử ký. Log/ảnh ở obj/ClinicalWorkflowP07/.

## P08 — Xem bảng tính phí

1. Bác sĩ phụ trách hoàn tất chuyên môn, ký dịch vụ và tất cả đơn cần dùng; hủy nháp không dùng. Phiếu phải Chờ thanh toán. Đơn thay thế còn nháp chặn tính phí cho tới khi ký hoặc hủy.
2. Mở Hóa đơn → chọn bệnh nhân và đúng lần khám → Bảng tính phí. Chọn kho hoạt động đủ toàn bộ thuốc nếu có đơn; lượt chỉ dịch vụ dùng Không dùng kho. Nhấn Xem bảng tính. Bác sĩ chỉ đọc lượt phụ trách; tiếp nhận/thu ngân có quyền thu được đọc nguồn phí, Admin có audit. Dược sĩ xem lịch sử đã có tại tab hóa đơn.
3. Bảng tính gồm dịch vụ xác nhận, tất cả đơn ký còn hiệu lực và các phần lô dự kiến. Kho dùng FIFO theo ngày nhập rồi hạn/mã lô, giống xuất kho hiện có. Một đơn ngừng/hủy không được cộng; cùng lượng tồn không được dùng lại cho hai đơn.
4. Số lẻ đồng giữ nguyên để đối chiếu, không cho thu mới bằng cách tự làm tròn. Nếu đã có hóa đơn/xuất hoặc đơn cũ thiếu snapshot, xem hướng dẫn đối chiếu và tab Hóa đơn / thu tiền; không tạo lần thu mới. Tiền đã thu ròng và còn phải thu luôn hiển thị riêng, giữ phần lẻ cũ.
5. Đổi lượt/kho hoặc tính lỗi xóa bảng cũ. Đây là bảng tính, chưa thu tiền hoặc giữ chỗ thuốc. P09 sẽ kiểm tra lại giá/tồn/nguồn và dấu vân tay trong cùng giao dịch trước ghi. Các nút ghi tài chính cũ trong clinical vẫn bị khóa.
6. In lại hóa đơn lấy nội dung/giá/thành tiền chốt; các phần lô cùng dòng đơn và giá được gộp nhưng mã chi tiết xuất vẫn nằm trên bản in. Cộng thành tiền từng phần đã chốt, không lấy tổng lượng nhân giá lại. Đã kiểm tra phân trang visual; chưa gửi lệnh in máy thật.

API BUS_THANHTOANTRONGOI.LapBangTinh(maLS, maKhoCapThuoc, nguoiDung) nhận transaction ngoài nếu có; trả BangTinhPhi với token, danh sách dịch vụ/thuốc/lô hoặc hóa đơn cần đối chiếu. Không nhận tổng tiền từ UI và không gọi CreateInvoice/DispensePrescription/RecordPayment. Quyền được đọc lại từ SQL theo principal phiên.

```powershell
dotnet run --project .\RangHamMat.ClinicalChecks -c Debug --no-restore -- --pricing
dotnet run --project .\RangHamMat.ClinicalUiChecks -c Debug --no-restore
```

Mode --pricing-concurrency chỉ nhận database mới riêng tiền tố RHM_ClinicalWorkflow_Test_P08Concurrency_, chưa có fixture CW. Cài script v2 vào tên mới có guard không ghi đè, dùng sqlcmd -f 65001 để giữ Unicode constraint; đặt RHM_CONNECTION_STRING đúng tên. Lượt triển khai đã tạo/dọn RHM_ClinicalWorkflow_Test_P08Concurrency_20261026. Không dùng mode này trên database gốc hoặc bản thử chính. Log/ảnh obj/ClinicalWorkflowP08/.
