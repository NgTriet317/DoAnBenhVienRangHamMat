# Bổ sung: xác nhận RIÊNG từng lịch khám và xin nghỉ trước tối thiểu 7 ngày

## 1. Cập nhật database SQL Server đang sử dụng (bắt buộc)

1. Sao lưu database đang dùng.
2. Mở SQL Server Management Studio, chọn database `QLBenhVienRangHamMat`.
3. Chạy `SQL/20261017_XAC_NHAN_TUNG_LICH_KHAM.sql` **một lần** (script có `IF OBJECT_ID` để chạy lại không tạo trùng bảng).
4. Kiểm tra bảng mới `dbo.XAC_NHAN_DOI_LICH_KHAM`.

**Không chạy lại toàn bộ** `Database/QLBenhVienRangHamMat.sql` vào database đang sử dụng: file này dành cho trường hợp tạo database mới. File này cũng đã được cập nhật định nghĩa bảng mới.

## 2. Quy tắc 7 ngày

- Trên hai tab `XinNghiViec.xaml`, DatePicker chỉ cho phép chọn ngày bắt đầu từ `DateTime.Today.AddDays(7)`.
- Ngày kết thúc nghỉ phép phải bằng/sau ngày bắt đầu.
- `XinNghiViec.xaml.cs` kiểm tra khi nhấn Gửi; `BUS_DON_XIN_NGHI.guiDon()` kiểm tra ở BUS; `DAL_DON_XIN_NGHI.guiDon()` còn kiểm tra theo thời gian trên SQL Server. Dữ liệu nhập thủ công/bỏ qua giao diện không vượt qua được tầng lưu.
- Ví dụ: nếu hôm nay là **17/10/2026** thì ngày bắt đầu được chọn sớm nhất là **24/10/2026**. Quy tắc áp dụng cho **cả nghỉ phép và nghỉ việc**.
- Quy tắc tính tại lúc **gửi đơn**, không yêu cầu ngày duyệt phải còn cách ngày nghỉ ít nhất 7 ngày. Các đơn cũ đã gửi vẫn có thể tiếp tục được xử lý.

## 3. Bắt buộc xác nhận riêng từng lịch khám

1. Quản lý mở **Ca làm → thông báo đơn xin nghỉ → nhấp đúp vào đơn**.
2. Phân người thay cho **từng ca** bị ảnh hưởng trước tiên (chức năng hiện có).
3. Xem bảng **Xác nhận chuyển từng lịch khám**: mã lịch, bệnh nhân, ngày khám, bác sĩ thay và trạng thái xác nhận.
4. Chọn **một lịch khám**; xem bác sĩ/phòng cũ và mới, rồi bấm **Xác nhận riêng lịch khám đã chọn**. Hộp thoại yêu cầu xác nhận riêng lịch đó.
5. Lặp lại cho mọi lịch có trạng thái **Chưa xác nhận**. Sau đó mới bấm **Duyệt đơn**.
6. Khi bấm duyệt, DAL truy vấn lại tất cả lịch khám bị ảnh hưởng, đối chiếu dữ liệu xác nhận với chính lịch và phương án thay ca hiện tại trong cùng giao dịch SQL. Nếu còn thiếu hoặc đã thay đổi, toàn bộ duyệt sẽ bị rollback và hiển thị mã lịch lỗi.

Mỗi dòng xác nhận lưu `MaDonNghi`, `MaLich`, bác sĩ/phòng cũ-mới, ca thay, ca khám, ngày giờ, thời lượng, mã quản lý xác nhận và thời gian xác nhận. Xác nhận được lưu trong SQL, không mất khi đóng view.

- Nếu **đổi người thay một ca**, xác nhận các lịch thuộc ca đó bị đánh dấu hủy; quản lý bắt buộc xác nhận lại.
- Nếu **lịch khám bị sửa** (bác sĩ/phòng, giờ, ca, thời lượng) sau lúc xác nhận, xác nhận sẽ không còn khớp và cần xác nhận lại.
- Nếu đơn **không duyệt**, xác nhận từng lịch được đánh dấu hủy cùng giao dịch từ chối; lịch khám không tự đổi.
- Nếu đơn **đã duyệt**, các xác nhận còn được hiển thị trong lịch sử chi tiết đơn; bác sĩ/phòng các lịch được chuyển và ca làm cập nhật trong cùng giao dịch.
- Nếu đơn không ảnh hưởng đến lịch khám nào thì không cần xác nhận lịch khám. **Phân công người thay cho các ca bị ảnh hưởng vẫn là điều kiện bắt buộc riêng**.

## 4. Các file chính

- `ET/ET_LICH_KHAM_CAN_XAC_NHAN.cs` (mới)
- `BUS/BUS_DON_XIN_NGHI.cs` (sửa)
- `DAL/DAL_DON_XIN_NGHI.cs` (sửa)
- `RangHamMat.Wpf/Views/XinNghiViec.xaml` và `.xaml.cs` (sửa)
- `RangHamMat.Wpf/Views/DonXinNghiQuanLy.xaml` và `.xaml.cs` (sửa)
- `SQL/20261017_XAC_NHAN_TUNG_LICH_KHAM.sql` (mới; dùng để nâng cấp DB cũ)
- `Database/QLBenhVienRangHamMat.sql` (sửa; cho trường hợp tạo DB mới)

## 5. Kiểm thử đề xuất trên Windows + SQL Server

1. Chọn ngày bắt đầu là ngày hiện tại + 6 ngày: không chọn được / gửi bị từ chối.
2. Chọn ngày bắt đầu hiện tại + 7 ngày, ngày kết thúc hợp lệ: gửi thành công.
3. Xin nghỉ việc bắt đầu ngày hiện tại + 6: chặn; + 7: cho phép nếu các điều kiện khác hợp lệ.
4. Đơn chờ duyệt có 2 lịch khám, chỉ xác nhận 1 lịch: duyệt phải thất bại, không chuyển lịch nào.
5. Xác nhận đủ 2 lịch: duyệt thành công khi các ca đều có người thay hợp lệ và không có lịch trùng.
6. Xác nhận rồi đổi người thay ca: lịch thuộc ca đó chuyển về Chưa xác nhận; duyệt bị chặn.
7. Xác nhận rồi sửa giờ/phòng của lịch khám: xác nhận cũ không còn hợp lệ; duyệt bị chặn.
8. Không duyệt đơn: đơn lưu lý do, lịch khám không đổi, xác nhận còn lưu với trạng thái Đã hủy.
9. Thêm lịch khám mới bị ảnh hưởng sau khi xác nhận những lịch cũ: lịch mới bắt buộc xác nhận riêng.
10. Đồng thời hai máy xác nhận/duyệt/sửa lịch: dữ liệu được kiểm tra lại ở DAL; không chấp nhận dựa vào trạng thái cũ của giao diện.

**Giới hạn kiểm tra tại đây:** đã kiểm tra XML XAML, tên sự kiện và gói ZIP. Chưa build/chạy ứng dụng WPF với SQL Server thật trong môi trường này.
