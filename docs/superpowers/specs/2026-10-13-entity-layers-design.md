# ET, DAL và BUS theo từng thực thể

Yêu cầu: mỗi ET hiện có có một DAL và một BUS cùng hậu tố. Bỏ lớp BUS điều phối nhiều bảng và DAL giao dịch nghiệp vụ chung. Tên giao diện và các helper giao diện phản ánh chức năng, không dùng tiền tố Clinical/LamSang.

DAL từng thực thể chứa SQL và kiểm tra trạng thái/liên kết của thực thể đó, gọi DAL của thực thể liên quan khi cần. BUS từng thực thể gọi DAL tương ứng và BUS liên quan. `dbconnect` chỉ cung cấp kết nối, giao dịch SQL cục bộ, chuẩn hóa cột và phát hiện phiên bản cũ; không chứa danh sách bảng hoặc quy tắc nghiệp vụ.

Các BUS/DAL nhận `IDbTransaction?` để cùng đọc/ghi một giao dịch. Cache giao diện gọi BUS từng thực thể trực tiếp và chuyển dữ liệu form thành ET, không thông qua một BUS tổng. Giữ rollback SQL/cache, khóa chứng từ xác nhận, kiểm tra số tiền, tồn kho và cấp mã cạnh tranh.

Bổ sung DAL/BUS cho những ET còn thiếu; cập nhật ET cũ theo schema SQL hiện có. Giữ hậu tố MAVAITRO hiện có, ánh xạ tới bảng VAI_TRO. Không chạy lại script tạo database.

Đổi TiepNhanLamSang thành TiepNhanKham, ChiTietLamSang thành ChiTietHoSo; đổi helper giao diện Clinical* thành tên tiếng Việt theo trách nhiệm. Cập nhật file, lớp, x:Class, resource, navigation, project links và runner kiểm tra.

Kiểm chứng: build Release; kiểm tra một-một ET/DAL/BUS và schema toàn bộ ET; kiểm tra XAML/navigation; PersistenceChecks, FinanceChecks và WpfSmoke chạy SQL thật với dữ liệu thử rollback.
