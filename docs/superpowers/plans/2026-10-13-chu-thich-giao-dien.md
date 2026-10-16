# Đồng đều ô thu/hoàn tiền và chú thích tiếng Việt

**Yêu cầu:** chỉnh ô Số tiền/Lý do; giải thích chức năng, cách hoạt động của code liên quan; ghi nhận hỗ trợ AI ở giao diện và hạ tầng dbconnect/XuLyLoi. Không sửa các DAL/BUS khác.

**Cách làm:** giữ workspace hiện tại vì có nhiều thay đổi đang làm của người dùng. Chỉ sửa kích thước hai ô, bổ sung comment C#/XAML và tài liệu tra cứu; không thay đổi xử lý nghiệp vụ. Nhãn AI mô tả hỗ trợ/chỉnh sửa và chú thích của AI, không khẳng định tác giả của code gốc khi không có bằng chứng.

- [x] Đo chiều cao thực tế của các ô WPF; áp dụng style hiện có để hai ô đồng đều với ComboBox.
- [x] Chú thích các lớp/hàm giao diện, model hiển thị, ET và chương trình kiểm tra bằng tiếng Việt; ghi chú luồng quan trọng bên trong các hàm lưu/giao dịch.
- [x] Giải thích dbconnect và XuLyLoi; giữ nguyên nội dung DAL/BUS còn lại so với đầu lượt làm việc.
- [x] Chú thích bố cục, binding/style XAML; viết tài liệu giải thích kiến trúc, chức năng, luồng lưu và phạm vi hỗ trợ AI; liên kết từ README.
- [x] Build Release, chạy kiểm tra hiện có và kiểm tra WPF thực tế; đối chiếu token C# và cấu trúc XAML để bảo đảm chú thích không đổi hành vi.

**Kiểm chứng:** `dotnet build BenhVienRangHamMat.sln -c Release --no-restore -p:NuGetAudit=false`; các chương trình Checks, SchemaChecks, FinanceChecks và WpfSmoke hiện có. Lưu bằng chứng chiều cao trước/sau cùng ảnh trong XemTruoc. Kiểm tra mã nguồn bằng Roslyn và XML, bỏ qua comment khi so sánh.

**Giới hạn:** không gán tác giả cho DAL/BUS được bỏ ngoài phạm vi; chức năng đăng nhập/vai trò hiện có là luồng xem giao diện, không mô tả thành cơ chế xác thực SQL.
