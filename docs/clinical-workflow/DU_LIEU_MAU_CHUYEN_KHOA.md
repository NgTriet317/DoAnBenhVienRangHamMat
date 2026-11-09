> LỊCH SỬ: dữ liệu demo và các bước Migrations dưới đây đã ngừng sử dụng từ29/10/2026. Dùng [README hiện tại](../../README.md) và duy nhất QLBenhVienRangHamMat.sql; tài liệu này chỉ lưu bằng chứng mốc trước.

# Dữ liệu demo chuyên khoa

[Migration 07](../../Database/Migrations/20261028_07_DemoSpecialties.sql) tạo dữ liệu **hư cấu** để thử tiếp nhận, hàng chờ và nhập tình trạng bệnh. Nội dung chưa kết luận chẩn đoán, không chứa liều thuốc hay hướng dẫn điều trị. Giá dịch vụ chỉ minh họa.

Chọn database demo đã sao lưu và chạy sau migration `20261028_06`. File không chuyển database, không thêm bảng; tổng schema vẫn 52 bảng. Nên kiểm tra trước trên database fixture riêng. Không chạy bản cài mới lên database đang sử dụng.

```powershell
sqlcmd -S TEN_MAY_CHU -d TEN_DATABASE_DEMO -E -C -b -f65001 -i Database/Migrations/20261028_07_DemoSpecialties.sql
```

## Bác sĩ và dịch vụ

Mật khẩu ban đầu của tài khoản mới là `123456`; mỗi tài khoản có salt ngẫu nhiên riêng, PBKDF2-HMAC-SHA256 210.000 vòng, salt 16 byte/hash 32 byte. Vai trò `VTRO002` (User), chức vụ `CV002` (Bác sĩ), không giới hạn khoa/phòng trên tài khoản. Đổi mật khẩu sau khi dùng thử; chạy lại không đặt lại mật khẩu.

| Chuyên khoa | Tài khoản ca ngày | Tài khoản ca đêm | Dịch vụ dùng thử |
|---|---|---|---|
| CK001 – Tổng quát | `user002` (NV002, có sẵn) | `user003` (NV003, có sẵn) | DV001 |
| CK002 – Bảo tồn | `demo_ck002_ngay` | `demo_ck002_dem` | DEMO28_DV_CK002 – Khám tư vấn điều trị bảo tồn |
| CK003 – Phẫu thuật hàm mặt | `demo_ck003_ngay` | `demo_ck003_dem` | DEMO28_DV_CK003 – Khám tư vấn phẫu thuật hàm mặt |
| CK004 – Hình ảnh | `demo_ck004_ngay` | `demo_ck004_dem` | DV006 – Chụp X-quang hàm mặt |
| CK005 – Xét nghiệm | `demo_ck005_ngay` | `demo_ck005_dem` | DEMO28_DV_CK005 – Khám tư vấn xét nghiệm |
| CK006 – Gây mê hồi sức | `demo_ck006_ngay` | `demo_ck006_dem` | DEMO28_DV_CK006 – Khám đánh giá trước gây mê |
| CK007 – Phục hồi | `demo_ck007_ngay` | `demo_ck007_dem` | DV008 – Chăm sóc vùng da bị tổn thương |

Mã nhân viên mới: `DEMO28_NV_CK002_ngay`…`DEMO28_NV_CK007_dem`; mã tài khoản thay `NV` bằng `TK`. Tên hiển thị bắt đầu bằng `BS Hư Cấu`. Tài khoản gốc `user002`, `user003`, `user005` có mật khẩu `123456` trong bản cài mẫu; migration không thay mật khẩu, nhân thân, vai trò hoặc chuyên khoa của những nhân viên này. `user005` là nhân viên tiếp nhận; `admin001` xem được toàn bộ hàng chờ.

Mỗi chuyên khoa có phòng `DEMO28_PH_CK001`…`DEMO28_PH_CK007`, loại `Phòng khám chuyên khoa`, đúng khoa của chuyên khoa. Phòng riêng giúp tiếp nhận đáp ứng điều kiện `LoaiPhong LIKE N'Phòng khám%'` và giữ nguyên loại phòng cũ. Bốn dịch vụ mới được thêm cho các chuyên khoa chưa có dịch vụ gốc; chuyên khoa và lịch sử của DV001…DV010 được giữ nguyên.

## Lịch và hồ sơ

- Ca ngày `DEMO28_CA_NGAY`: 07:00–19:00; ca đêm `DEMO28_CA_DEM`: 19:00–07:00 hôm sau. Cửa sổ từ hôm qua đến 14 ngày sau ngày chạy, theo `SYSDATETIME()` của SQL Server. Ca đêm hôm qua bao phủ thời điểm trước 07:00 hôm nay.
- Tối đa 224 lịch mới, đã xác nhận/có mặt, cùng các ca theo ngày tương ứng. Không tạo chấm công giờ vào/ra. NV002 nhận ca ngày, NV003 nhận ca đêm khi còn đúng chức vụ/chuyên khoa gốc và đang hoạt động.
- Lịch trùng khoảng giờ với bất kỳ lịch hiện có của bác sĩ **hoặc phòng** đều bị bỏ qua, kể cả lịch nháp/hủy. Đơn nghỉ đã duyệt được xét cả hai ngày của ca đêm. Không cập nhật lịch hoặc đơn nghỉ của người dùng.
- Mỗi CK001…CK007 có một bệnh nhân `DEMO28_BN_CK001_CHO` và một `DEMO28_BN_CK001_NHAP` (thay CK tương ứng): tổng 14 bệnh nhân/hồ sơ/phiếu. Phiếu `CHO` ở trạng thái `Chờ khám`; phiếu `NHAP` ở `Đang khám`, có một lần khám `Nháp`, chưa ký và một dòng tình trạng đơn giản. Không có mức độ; `MaDMTT=NULL`, vị trí răng chỉ nhập khi phù hợp.
- Bác sĩ/phòng được chọn từ lịch hợp lệ bao phủ **đúng giờ tiếp nhận**, cùng tiêu chí của `DAL_TIEPNHAN.Candidates`. Bảng kết quả cuối script cho biết tài khoản thực tế cần đăng nhập. Lượt chờ và nháp thường thuộc bác sĩ ca hiện tại khi nạp lần đầu. Qua ca khác, Admin vẫn xem được toàn bộ, bác sĩ chỉ xem lượt được giao.

Migration không tạo lịch hẹn, kế hoạch điều trị, đơn thuốc, thực hiện dịch vụ, hóa đơn, thanh toán hay xuất kho; không thêm mẫu hỗ trợ khám hoặc cấu hình kê đơn legacy.

## Chạy lại và lỗi

Chạy lại bổ sung lịch còn thiếu trong cửa sổ mới. Bệnh nhân/hồ sơ/phiếu có mã cố định được giữ nguyên; không chuyển về hàng chờ, không đổi bác sĩ/ngày tiếp nhận, không tái tạo lần khám hay tình trạng đã sửa/xóa. Lượt đã ký/thu tiền vẫn giữ trạng thái. Vì vậy chạy lại vào ngày khác không tạo bộ bệnh nhân mới.

Các mã `DEMO28_*` được dành riêng: script kiểm tra dấu nhận diện và liên kết nguồn trước khi ghi. Mã trùng hoặc hồ sơ bị xóa một phần làm script dừng để đối chiếu. Đổi trường nhận diện demo/neo nguồn có thể khiến lần chạy sau dừng; script không ghi đè. Mật khẩu và trạng thái nghiệp vụ có thể thay đổi bình thường.

Toàn bộ lần chạy nằm trong transaction với `XACT_ABORT`, khóa ứng dụng và `TRY/CATCH`; lỗi rollback toàn bộ thay đổi của lần chạy. Nếu không tìm được bác sĩ/phòng hợp lệ cho một phiếu mới do lịch, nghỉ phép hoặc trạng thái hoạt động hiện có, script báo lỗi 52142; đối chiếu lịch trước khi chạy lại. Không tự bỏ chặn lịch hoặc bật lại tài khoản/dịch vụ.

## Kiểm tra trên fixture

Sau khi nạp lần đầu trên bản cài mẫu sạch, bảng bộ đếm phải cho: bác sĩ 12, phòng 7, dịch vụ 4, lịch 224 nếu không bị lịch/nghỉ phép chặn, bệnh nhân 14, phiếu 14, lần khám nháp 7, tình trạng 7. Lần hai cùng ngày: tất cả số thêm bằng 0. Kết quả cuối luôn liệt kê 14 phiếu cùng trạng thái thực tế.

```sql
SELECT COUNT(*) AS SoBang FROM sys.tables WHERE is_ms_shipped=0; -- 52
SELECT p.MaDV,n.MaCK,COUNT(*) AS SoPhieu
FROM dbo.PHIEU_KHAM_BENH p
JOIN dbo.NHAN_VIEN n ON n.MaNV=p.MaBS
WHERE p.MaPhieu LIKE N'DEMO28[_]PK[_]%'
GROUP BY p.MaDV,n.MaCK; -- 7 nhóm, mỗi nhóm 2 phiếu
SELECT COUNT(*) AS SoTinhTrang
FROM dbo.TINH_TRANG_BENH WHERE MaTTB LIKE N'DEMO28[_]TTB[_]%'; -- 7 ban đầu
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;
```

Thử qua ứng dụng: đăng nhập `user005`, chọn dịch vụ từng chuyên khoa và kiểm tra có bác sĩ đúng ca/phòng; tiếp nhận một bệnh nhân demo vào dịch vụ khác nếu cần thử phiếu mới. Đăng nhập bác sĩ ghi trong kết quả, mở phiếu chờ/bản nháp, nhập hoặc sửa tình trạng, lưu nháp. Chạy lại migration và kiểm tra nội dung/trạng thái vừa lưu còn nguyên. Trên fixture riêng, thử cả lượt đã ký; không thực hiện thu tiền/xuất kho chỉ để kiểm tra dữ liệu này.

Kiểm tra lỗi bằng fixture thứ hai: tạo trước một nhân viên có mã `DEMO28_NV_CK002_ngay` nhưng khác chuyên khoa/dấu nhận diện rồi chạy script; phải lỗi 52141 và không để lại các bác sĩ/phòng/phiếu mới. Không thử tạo xung đột trên database đang sử dụng.
