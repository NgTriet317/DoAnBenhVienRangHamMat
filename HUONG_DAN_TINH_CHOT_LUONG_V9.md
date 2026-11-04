# Bổ sung nghiệp vụ Tính → Đối soát → Chốt lương (V9)

## Cài đặt

1. Sao lưu database đang dùng trước khi nâng cấp.
2. Mở **SQL Server Management Studio** và chọn đúng database ứng dụng sử dụng (mặc định theo `DAL/dbconnect.cs` là `QLBenhVienRangHamMat_TestLuong`).
3. Mở `SQL/07_NANG_CAP_TINH_DOI_SOAT_CHOT_LUONG_V9.sql`. Nếu DB của bạn không phải `QLBenhVienRangHamMat_TestLuong`, **đổi dòng `USE [...]`** rồi mới **F5**. Không chạy lại file tạo mới database gốc trên database đang có dữ liệu.
4. Kiểm tra `dbo.RHM_TinhLuongKy` và `dbo.RHM_ChotLuongNhanVien` được tạo. Mở solution WPF trên Windows/Visual Studio, Build/Run và đăng nhập tài khoản **Admin**.
5. Vào **Bảng lương & hoa hồng**, chọn tháng/năm, nhấn **Tính lương kỳ này**. Thao tác cập nhật chỉ các phiếu *Nháp*; kỳ mới có thể tự lập phiếu nháp cho nhân viên đang làm việc nếu có đúng một chính sách đã duyệt bao phủ cả tháng. Phiếu đã chốt được giữ nguyên.
6. Chọn từng nhân viên để xem công chờ duyệt, đơn nghỉ chờ duyệt, công trùng ngày phép, chứng từ chưa duyệt, nguồn hoa hồng, hiệu lực chính sách và ngày chốt. Xử lý lỗi trong phân hệ tương ứng; sau đó **Tính lương kỳ này** lại và xem tiền.
7. Chỉ khi không còn lỗi, thực nhận không âm và đã qua hạn quy định, nhấn **Chốt lương nhân viên này**, xác nhận trong hộp thoại. Đã chốt thì không được sửa/xóa.

## Quy tắc giữ từ SQL V9

- **0,5 công/ca hoàn tất đã xác nhận** — không phân biệt đủ 8 tiếng. Ca vắng được xác nhận có thể tính 0 công. Đi trễ 16–60 phút vẫn trừ riêng 0,25 công/ca như SQL V9; quy tắc đi trễ này không bị thay đổi.
- Công chuẩn: ngày thứ Hai–Bảy, trừ ngày nghỉ lễ có hưởng lương lưu trong `NGAY_NGHI_LE`. Ngày nghỉ *được duyệt* hưởng 1 công/ngày công chuẩn theo mô hình V9 hiện tại (không phân biệt loại nghỉ).
- Lương theo công = Lương cơ bản × Công thực tế / Công chuẩn. Thực nhận = Lương theo công + Hoa hồng + Phụ cấp + Tăng ca − Bảo hiểm − Các khoản trừ.
- Bảo hiểm tính theo tỷ lệ BHXH/BHYT/BHTN từ chính sách lương đã duyệt. Hoa hồng lấy từ `CHI_TIET_HOA_HONG_LUONG` và chỉ cho chốt khi hóa đơn thu đủ/dịch vụ xác nhận. Phụ cấp, làm thêm và khấu trừ lấy từ chứng từ `CHI_TIET_LUONG` đã có; **không tự phát sinh các chứng từ mới**.
- Các khoản *Điều chỉnh/Cộng* không có cột hạch toán riêng theo SQL V9 được cảnh báo để phân loại trước khi chốt (không âm thầm bỏ sót). Chi tiết lương chưa có người duyệt cũng chặn chốt.
- Chỉ được chốt **từ 5 ngày sau cuối tháng**. Ví dụ tháng 10/2026: **từ ngày 05/11/2026**, nên trong ngày 26/10/2026 chỉ tính và đối soát được, chưa chốt được.
- Chốt **từng nhân viên**, không chốt hàng loạt. SQL tự tính lại phiếu từ nguồn ngay trước khi chốt và so với số công, hoa hồng, thực nhận mà quản lý vừa xem; khác thì bắt tính/xem lại.
- Người chốt (`MaNVChot`), thời điểm chốt (`NgayChot`) được ghi trên `BANG_LUONG`. SQL trigger V9 chặn sửa phiếu đã chốt; phần duyệt chấm công V9 cũng khóa công thuộc kỳ đã chốt.

## Các file được thay đổi

- `RangHamMat.Wpf/Views/BangLuong.xaml` (hai nút tính/chốt và khu vực thông báo đối soát)
- `RangHamMat.Wpf/Views/BangLuong.xaml.cs` (luồng nhập, hiển thị, kiểm tra và xác nhận)
- `BUS/BUS_BANGLUONG.cs` (kiểm tra kỳ và gọi DAL)
- `DAL/DAL_BANGLUONG.cs` (truy vấn số liệu, lỗi đối soát và gọi stored procedure có tham số)
- `SQL/07_NANG_CAP_TINH_DOI_SOAT_CHOT_LUONG_V9.sql` (2 procedure và các khóa giao dịch)

## Lưu ý triển khai

Chưa thực thi Build .NET/WPF hoặc thử giao dịch thực trên SQL Server trong môi trường tạo bản sửa, nên cần chạy thử trên **database sao lưu** trước khi sử dụng. Các tình huống tối thiểu cần kiểm thử: kỳ chưa đủ ngày chốt, công chưa duyệt, nghỉ chờ duyệt, phiếu âm, chính sách hết hạn, hóa đơn chưa thu đủ, phiếu đã chốt, thao tác chốt trùng từ hai cửa sổ, thay đổi công/phụ cấp sau khi tính, kỳ không có phiếu.

Phiếu đã chốt không được sửa; việc điều chỉnh bảng lương kỳ đã chốt phải có quy trình phiếu điều chỉnh độc lập. Phần này **chưa triển khai**. Chưa thêm cơ chế chuyển khoản/chi tiền tự động: "Đã chốt" không đồng nghĩa "Đã thanh toán".
