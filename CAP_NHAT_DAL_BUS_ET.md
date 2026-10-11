# Cập nhật DAL/BUS/ET — 11/10/2026

Đã viết lại các lớp phục vụ toàn bộ chức năng trong `RangHamMat.Wpf/KeHoach.txt` theo mẫu dự án. Nguồn chuẩn là `Database/QLBenhVienRangHamMat.sql` hiện tại; PK/FK là chuỗi NVARCHAR(50), không phải INT IDENTITY. Không khởi tạo lại database hoặc đổi các khóa đã có.

## ET và lớp riêng từng bảng

24 ET có private fields, constructor không tham số, constructor đầy đủ các cột và property get/set. Kiểu dữ liệu/nullability khớp 201 cột SQL. Cột computed và tổng do trigger cập nhật được đánh dấu `Write(false)`; không đưa vào câu INSERT/UPDATE.

Mỗi dòng dưới đây có đủ ba file `ET_<hậu tố>.cs`, `DAL_<hậu tố>.cs`, `BUS_<hậu tố>.cs` trong thư mục tương ứng:

| Bảng SQL | Hậu tố lớp | Mã mới |
|---|---|---|
| BENH_NHAN | BENHNHAN | BN01 |
| HO_SO_BENH_AN | HOSOBENHAN | HSBA01 |
| PHIEU_KHAM_BENH | PHIEUKHAMBENH | PKB01 |
| LICH_KHAM | LICHKHAM | LK01 |
| LICH_SU_KHAM_BENH | LICHSUKHAMBENH | LSKB01 |
| TINH_TRANG_BENH | TINHTRANGBENH | TTB01 |
| KE_HOACH_DIEU_TRI | KEHOACHDIEUTRI | KHDT01 |
| CHI_TIET_KE_HOACH | CHITIETKEHOACH | CTKH01 |
| DICH_VU_THUC_HIEN | DICHVUTHUCHIEN | DVTH01 |
| DON_THUOC | DONTHUOC | DT01 |
| CHI_TIET_DON_THUOC | CHITIETDONTHUOC | CTDT01 |
| NHAN_VIEN | NHANVIEN | NV01 |
| PHONG | PHONG | P01 |
| CA_LAM | CALAM | CL01 |
| DICH_VU | DICHVU | DV01 |
| THUOC | THUOC | T01 |
| HANG_HOA | HANGHOA | HH01 |
| HOA_DON | HOADON | HD01 |
| CHI_TIET_HOA_DON | CHITIETHOADON | CTHD01 |
| THANH_TOAN | THANHTOAN | TT01 |
| PHIEU_XUAT | PHIEUXUAT | PX01 |
| CHI_TIET_PHIEU_XUAT | CHITIETPHIEUXUAT | CTPX01 |
| KHO | KHO | K01 |
| LO_HANG_HOA | LOHANGHOA | LHH01 |

DAL từng bảng chứa SQL tham số và trả về ET của đúng bảng. BUS từng bảng gọi DAL tương ứng. Các bảng lâm sàng/danh mục có các hàm lấy danh sách, lấy theo mã, thêm, sửa, xóa với tham số ET; ví dụ `BUS_BENHNHAN.themBenhNhan(ET_BENHNHAN)` và `layBenhNhanTheoMa(string)`. Sau khi thêm thành công, ET nhận mã thực tế đã lưu.

Hóa đơn, thanh toán và xuất thuốc dùng các hàm nghiệp vụ lập/xác nhận/hủy nháp, thu/hoàn, cấp thuốc ghi sổ để giữ ràng buộc chứng từ. Chi tiết được xử lý ở DAL/BUS của bảng chi tiết tương ứng. Không cho phép sửa tùy ý chứng từ đã xác nhận.

`BUS_LAMSANG` điều phối các BUS riêng để cung cấp dữ liệu cho WPF và lưu liên bảng. `DAL_GIAODICH` dùng chung connection/transaction, kiểm tra schema/trạng thái/phiên bản và cấp mã. SQL đọc và ghi trong một giao dịch sử dụng cùng connection, kể cả các DAL/BUS tài chính.

## Cấp mã

Mã lấy ký tự đầu mỗi từ trong tên bảng SQL, ghép với số tiếp theo có ít nhất hai chữ số: BN01…BN09, BN10…BN99, BN100. Mã này được lưu thực tế vào PK và dùng trong FK.

Nếu bảng đã có BN001…BN010 thì mã mới là BN11; mã bệnh án cũ HS001 vẫn hợp lệ, mã mới dùng HSBA và tiếp tục hậu tố số lớn nhất. Không đổi các mã cũ hoặc đánh lại số dữ liệu đang có.

Cấp mã và INSERT trong cùng transaction, dùng `sp_getapplock` theo bảng và khóa dữ liệu khi lấy hậu tố lớn nhất để tránh hai cửa sổ cấp cùng mã. Khi lưu nhiều dòng bị lỗi, toàn bộ giao dịch được rollback. Form nhân sự cũng dùng mã NV tự cấp, không ghép số điện thoại/chức vụ vào mã.

## Tên view đã đổi

Đã đổi cả file XAML, code-behind, tên lớp, `x:Class` và mọi tham chiếu/test:

| Tên trước | Tên hiện tại |
|---|---|
| Views/ClinicalVisitDetails | Views/ChiTietKhamDieuTri |
| Views/ClinicalPrescriptionDetails | Views/ChiTietDonThuoc |
| Views/InvoicePrintPreview | Views/XemTruocHoaDon |
| Views/PrescriptionPrintPreview | Views/XemTruocDonThuoc |
| Controls/AppointmentCalendar | Controls/LichHenKham |

## Kiểm chứng

Các lệnh và hướng dẫn chạy nằm trong [báo cáo KeHoach.txt](THUC_HIEN_KE_HOACH.md). Kết quả kiểm tra cuối:

- Build solution Release: 0 lỗi, 0 cảnh báo.
- SchemaChecks: 24 bảng, 201 cột, 0 sai lệch; đủ fields/constructor/property.
- Checks: 4 module, 10 bảng, 57 XAML; sự kiện, resources và liên kết form hợp lệ.
- PersistenceChecks: 12 kiểm tra SQL đạt; gồm mã tự cấp, CRUD typed, nullable, chống ghi đè/xóa dữ liệu cũ, khóa chứng từ và rollback nhiều dòng.
- FinanceChecks: 54 kiểm tra đạt; nguồn thu không trùng, giá chốt, FIFO, thiếu tồn, thu/hoàn, khóa cạnh tranh và đọc các bảng trong giao dịch chưa commit.
- WpfSmoke: chạy cửa sổ và SQL thật; hồ sơ/lịch sử, lịch theo bệnh nhân, lưu/sửa/xóa đơn nhiều dòng, kế hoạch, dịch vụ, hóa đơn, bản in phân trang và rollback SQL/cache đều đạt.

Rà soát cuối đã tái hiện và sửa ba lỗi: DAL tài chính tự chờ dữ liệu đang ghi trên connection khác; cờ đổi trạng thái cho phép sửa nội dung kế hoạch xác nhận; DATE/DATETIME2 làm tròn khiến BUS typed báo nhầm dữ liệu cũ. Đã bổ sung kiểm tra hồi quy cho các trường hợp này.

Mọi dữ liệu thử SQL được rollback. Hai runner SQL có cảnh báo NU1900 do dữ liệu kiểm tra lỗ hổng NuGet không tải được; các kiểm tra vẫn kết thúc exit code 0. Đã kiểm tra bản xem trước và phân trang; chưa kiểm tra máy in vật lý, đầu ra trình điều khiển PDF hoặc DPI khác. Các phân hệ ngoài phạm vi KeHoach.txt giữ source hiện có.
