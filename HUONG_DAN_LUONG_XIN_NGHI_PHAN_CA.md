# Luồng xin nghỉ – duyệt – thay ca – đồng bộ lịch khám

## Tệp trong dự án

- `ET/ET_CA_CAN_THAY_THE.cs`: dữ liệu ca bị ảnh hưởng và người có thể thay thế.
- `DAL/DAL_DON_XIN_NGHI.cs`, `BUS/BUS_DON_XIN_NGHI.cs`: gửi đơn, kiểm tra trùng kỳ nghỉ, tìm ca bị ảnh hưởng, chọn/hủy phương án thay, duyệt/từ chối trong giao dịch SQL.
- `DAL/DAL_LICHLAMVIEC.cs`: chặn đăng ký, tạo, duyệt và cập nhật ca nếu nhân viên đang nghỉ được duyệt.
- `DAL/DAL_LICHKHAM.cs`, `BUS/BUS_LICHKHAM.cs`: chặn bác sĩ nghỉ, chống trùng lịch và tự thêm ca làm từ lịch khám (có chức năng đồng bộ dữ liệu cũ).
- `RangHamMat.Wpf/Views/DonXinNghiQuanLy.xaml` và `.xaml.cs`: xem ca bị ảnh hưởng; chọn/đổi người thay thế.
- `RangHamMat.Wpf/Views/CaLamViec.xaml` và `.xaml.cs`: nút đồng bộ các lịch khám cũ, tải lại ca làm khi quay về từ duyệt đơn.
- `RangHamMat.Wpf/Views/ThemLichKham.xaml.cs`: báo lỗi sớm ngay khi chọn bác sĩ đã được duyệt nghỉ.

## Thao tác

1. Nhân viên chọn **Xin nghỉ phép** hoặc **Xin nghỉ việc**, nhập ngày và lý do rồi **Gửi đơn**. BUS kiểm tra ngày bắt đầu không trong quá khứ; DAL không cho gửi kỳ nghỉ trùng đơn đang chờ/đã duyệt.
2. Quản lý mở **Ca làm** → **Thông báo đơn xin nghỉ**. Nếu lịch khám từ dữ liệu trước chưa tạo ca, bấm **Đồng bộ lịch khám** ở Ca làm trước.
3. Nhấp đúp vào đơn. Danh sách ca bị ảnh hưởng hiển thị theo ngày/ca/phòng, kèm trạng thái đã phân người thay hay chưa.
4. Chọn một ca → chọn nhân viên cùng **chức vụ và chuyên khoa**, chọn phòng → **Phân công người thay cho ca đã chọn**. Có thể bấm **Đổi người thay thế** để hủy phương án trước đó và chọn lại. Người thay không được nghỉ đã duyệt, không có ca/lịch khám trùng thời gian. Nếu đã có ca xác nhận đúng giờ/phòng, hệ thống tái sử dụng lịch đó, không tạo ca trùng.
5. Sau khi đủ người thay cho tất cả ca ảnh hưởng, chọn **Duyệt đơn**. Trong **một giao dịch**, hệ thống chuyển lịch khám tương lai đang hoạt động sang người thay, ghi lý do thay đổi, đánh dấu ca cũ **Nghỉ phép/Nghỉ việc**, xác nhận phương án thay, cuối cùng mới đánh dấu đơn **Đã duyệt**. Nếu có bất kỳ ca/lịch nào chưa thay hoặc xung đột lịch, giao dịch sẽ rollback, đơn giữ **Chờ duyệt**.
6. Nếu **Không duyệt**, bắt buộc nhập lý do. Các ca dự kiến thay sẽ bị hủy; ca nhân viên cũ giữ nguyên; đơn chuyển **Không duyệt** và giữ lịch sử.
7. Khi tạo/chỉnh sửa lịch khám trong tương lai, nếu bác sĩ nghỉ đã duyệt thì DAL từ chối. Nếu hợp lệ mà chưa có ca làm đúng ngày/ca/phòng, DAL tự thêm `CA_THEO_NGAY` và `LICH_LAM_VIEC` trạng thái **Đã xác nhận** cùng giao dịch tạo lịch khám. Nếu bác sĩ có đăng ký ca chưa duyệt, cần quản lý duyệt ca trước khi phân lịch khám, không tự vượt qua quy trình duyệt.

## Quy tắc và giả định

- `GhiChu` của `DON_XIN_NGHI` nhận diện **Nghỉ việc** từ chuỗi `Loại đơn: Nghỉ việc`. Sau khi nghỉ việc được duyệt, mọi ngày từ `TuNgay` trở đi đều không được phân ca/lịch khám. Không tự đổi trạng thái hồ sơ nhân viên sang **Đã nghỉ**.
- Đơn **Nghỉ phép** chỉ chặn từ `TuNgay` đến `DenNgay` (bao gồm cả hai ngày).
- Cùng phòng có thể có nhiều nhân viên làm ca; mỗi nhân viên không được phân các ca giao nhau.
- Lịch khám còn hiệu lực được tự chuyển bác sĩ/phòng khi duyệt đơn, nhưng cần quản lý kiểm tra và thông báo cho bệnh nhân theo quy trình thực tế. Lịch khám đã hoàn thành/hủy không chuyển.
- Đơn nghỉ có lịch khám nhưng chưa có ca trong `LICH_LAM_VIEC` sẽ không thể duyệt cho đến khi đồng bộ ca và phân thay.
- Phần loại đơn/phân công sử dụng bảng `DON_XIN_NGHI`, `PHAN_CONG_THAY_THE`, `LICH_LAM_VIEC` đã có trong SQL. Không cần DDL mới. Phương án thay được lưu trong `PHAN_CONG_THAY_THE.GhiChu` theo mẫu `Thay ca <MaLLV>`.
- Chỉ tài khoản quản lý (Admin) thao tác thay thế và duyệt; không tự duyệt đơn của mình.
- Không tự động gửi SMS/email cho bệnh nhân hoặc thay đổi các phiếu khám, điều trị, chứng từ đã phát sinh.

## Kiểm thử cần chạy trên Windows / SQL Server

1. Gửi 1 đơn nghỉ hợp lệ, kiểm tra trạng thái `Chờ duyệt` và thông báo ở Ca làm.
2. Gửi lần hai có ngày trùng đơn đang chờ → phải bị từ chối.
3. Đơn không có ca và không có lịch khám, duyệt → thành công, bác sĩ bị chặn đặt lịch trong kỳ nghỉ.
4. Đơn trùng ca xác nhận, duyệt ngay → phải bị chặn; phân người thay hợp lệ mới duyệt được.
5. Phân người thay đã nghỉ, khác chức vụ/chuyên khoa hoặc trùng ca/lịch khám → bị chặn.
6. Phân người thay hợp lệ, duyệt → `PHAN_CONG_THAY_THE` và `LICH_LAM_VIEC` được cập nhật; lịch khám chưa kết thúc chuyển sang người thay.
7. Phân người thay rồi không duyệt kèm lý do → ca dự kiến bị hủy, ca gốc không đổi.
8. Lưu lịch khám có bác sĩ nghỉ đã duyệt → bị từ chối ngay cả khi bypass UI.
9. Lưu lịch khám hợp lệ khi bác sĩ chưa có ca → tự thêm ca và lịch làm, không thêm trùng khi tạo lịch khám thứ hai cùng ca/phòng.
10. Lịch khám cũ chưa được đồng bộ → bấm nút Đồng bộ lịch khám, kiểm tra số lượng ca được tạo.
11. Hai quản lý cùng duyệt một đơn → chỉ một thao tác thành công.
12. Kiểm tra các đơn nghỉ việc: ngày sau `TuNgay` vẫn bị chặn phân lịch/ca.

**Môi trường biên soạn không có .NET SDK/Windows/SQL Server.** Đã kiểm tra cú pháp XML XAML và liên kết tên event của các view đã sửa; cần build và chạy 12 trường hợp trên bằng Visual Studio 2022 cùng SQL Server thật.
