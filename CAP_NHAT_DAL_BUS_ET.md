# Cập nhật ET–DAL–BUS — 13/10/2026

Đủ 39 ET hiện có, 39 DAL và 39 BUS cùng hậu tố. Mỗi lớp xử lý đúng thực thể tương ứng; đã bỏ `BUS_LAMSANG`, `DAL_GIAODICH` và helper kiểm tra liên kết chung của WPF.

BUS từng thực thể kiểm tra dữ liệu và gọi DAL tương ứng. DAL chứa SQL, kiểm tra phiên bản/trạng thái và gọi DAL liên quan khi cần kiểm tra hồ sơ, lịch hẹn, kế hoạch hoặc chi tiết. Các lớp nhận `IDbTransaction?` để dùng cùng connection khi lưu chứng từ nhiều dòng. Giao dịch lỗi sẽ rollback SQL và cache, kể cả khi SQL trigger hủy giao dịch hoặc kết nối bị ngắt.

`dbconnect` chỉ chứa hạ tầng kết nối, giao dịch SQL, cấp mã, chuẩn hóa cột và so sánh phiên bản. Không chứa danh sách bảng hay quy tắc nghiệp vụ riêng của thực thể. WPF chuyển dữ liệu form thành ET và gọi từng BUS trực tiếp; cache phục vụ hiển thị.

## Các bộ đã bổ sung

BAOTRITHIETBI, CHITIETPHIEUNHAP, CHUNGCHINHANVIEN, HOADONNHAP, KHOA, LICHLAMVIEC, LOAIHANGHOA, MAVAITRO, NHACUNGCAP, PHIEUNHAP, TAIKHOAN, THIETBI, VATTU.

Ví dụ: `ET_NHACUNGCAP.cs` → `DAL_NHACUNGCAP.cs` → `BUS_NHACUNGCAP.cs`. Hậu tố MAVAITRO giữ theo ET hiện có, ánh xạ bảng SQL VAI_TRO.

Các ET cũ đã được đối chiếu tên cột, kiểu dữ liệu, nullable, khóa và constructor với SQL hiện có. Tổng/computed do SQL tính được đánh dấu `Write(false)` và không ghi đè từ ET. Các DAL chức vụ/chuyên khoa đã sửa SQL theo cột hiện tại và dùng mã tự cấp.

Mã mới tiếp tục số lớn nhất đang có, dùng chữ đầu tên bảng và ít nhất hai chữ số: BN01, HSBA01, LSKB01. Khóa cấp mã và INSERT cùng giao dịch. Không đổi mã cũ, không chạy lại script tạo database.

## Tên giao diện và helper

| Trước | Hiện tại |
|---|---|
| TiepNhanLamSang.xaml / .xaml.cs | TiepNhanKham.xaml / .xaml.cs |
| ChiTietLamSang.xaml / .xaml.cs | ChiTietHoSo.xaml / .xaml.cs |
| ClinicalData.cs | DuLieuGiaoDien.cs |
| ClinicalSchema.cs | TruongDuLieu.cs |
| ClinicalRules.cs | QuyTacHienThi.cs |
| ClinicalNavigator.cs | DieuHuongHoSo.cs |
| ClinicalCards.cs | TheHoSo.cs |
| ClinicalVisitModels.cs | MoHinhLanKham.cs |
| ClinicalStyles.xaml | KieuHienThiHoSo.xaml |

BieuMauHoSo.cs và HoTroGiaoDien.cs tách riêng phần đọc form và hỗ trợ hiển thị. Các lớp, x:Class, resource, điều hướng và runner đều dùng tên mới.

## Kiểm chứng

- Build solution Release: 0 lỗi, 0 cảnh báo.
- SchemaChecks: 39 bảng ET, 303 cột, 0 sai lệch.
- Checks: đủ 39 bộ ET/DAL/BUS; 57 XAML hợp lệ, điều hướng/sự kiện/resource hợp lệ.
- PersistenceChecks: 16 ca đạt, gồm đọc typed qua tất cả 39 BUS, CRUD danh mục mới, tổng phiếu nhập, chống stale và rollback.
- FinanceChecks: 54 ca đạt về hóa đơn, thu/hoàn, FIFO, thiếu tồn, cạnh tranh và đọc dữ liệu chưa commit trong cùng giao dịch.
- WpfSmoke: hồ sơ, lịch hẹn, đơn nhiều dòng, kế hoạch/dịch vụ, hóa đơn/bản in, rollback SQL/cache, lỗi trigger và ngắt kết nối đều đạt.

Các runner SQL có cảnh báo NU1900 do không tải được dữ liệu kiểm tra lỗ hổng NuGet; kiểm tra nghiệp vụ vẫn đạt. Mọi dữ liệu SQL thử được rollback. Kiểm tra in giới hạn ở bản xem trước và phân trang.