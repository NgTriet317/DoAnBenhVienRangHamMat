# Kết nối lâm sàng và thu phí với database v8

Nguồn yêu cầu: `RangHamMat.Wpf/KeHoach.txt`. Nguồn schema: SQL Server `DESKTOP-C9VCE8A/QLBenhVienRangHamMat`, `Database/QLBenhVienRangHamMat.sql` và `Database/DB_Nhom4_v8.docx`.

Giữ WPF và luồng form gốc. Dữ liệu đi qua WPF → BUS → DAL → SQL Server; ET phản ánh khóa INT IDENTITY, khóa ngoại nullable, số lượng DECIMAL và ngày nullable đúng schema. Dùng Dapper/SqlClient đã có, không thêm framework. ClinicalData là cache hiển thị, không còn nguồn lưu độc lập. Nạp database khi mở phân hệ, bỏ nút/code nạp mẫu. Database sinh mã, tính cột computed và tổng chứng từ bằng trigger.

Lưu header và các dòng đơn thuốc/kế hoạch/dịch vụ trong một transaction; thất bại không giữ thay đổi trong cache. Xác nhận lần khám cập nhật trạng thái và ngày cùng lúc. Bản xác nhận giữ nguyên nội dung. Khóa và kiểm tra trạng thái lại ở SQL trong transaction để xử lý nhiều cửa sổ/quầy. Giữ patient/visit scope khi điều hướng.

Hóa đơn gắn MaLS, chỉ lấy dịch vụ Đã xác nhận và thuốc đã cấp đủ qua phiếu xuất Đã ghi sổ. Mỗi nguồn chỉ thu một lần. Giá dịch vụ lấy nguồn; giá thuốc chốt từ DonGiaBan. Không tính tiền dự toán, đơn chưa cấp hoặc thuốc mua ngoài. Cấp thuốc đủ cả đơn bằng các lô hợp lệ, kiểm tra tồn trong transaction. Thu/hoàn dương, có người thực hiện; hoàn cần lý do, không vượt thu ròng, thu không vượt số còn nợ. In hóa đơn có chi tiết và số đã thu/còn phải trả.

Form bệnh nhân giữ màu/typography hiện có, bổ sung MaBHYT, SDT dạng chuỗi, trường sức khỏe và kiểm tra nhập. Dịch vụ thực hiện dùng cùng nguồn và form khám, mở đúng hồ sơ/lần khám/đơn/hóa đơn. Thông báo lỗi kết nối/lưu rõ ràng, không chuyển sang dữ liệu giả.

Kiểm chứng: build solution; bộ kiểm tra hiện có; WPF smoke; kiểm tra schema ET; kiểm tra transaction SQL trên dữ liệu thử được rollback, gồm persistence, lỗi nhiều dòng, khóa xác nhận, phí không trùng, cấp thuốc đủ, giữ giá, thu/hoàn và vượt số dư. Không xóa dữ liệu hiện có, không chạy lại script tạo database.
