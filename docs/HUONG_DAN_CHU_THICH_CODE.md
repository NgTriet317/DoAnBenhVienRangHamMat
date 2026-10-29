# Hướng dẫn đọc mã nguồn và chú thích

Chú thích được bổ sung bằng tiếng Việt theo phong cách đang có trong dự án: XML `summary` mô tả vai trò, `param` giải thích đầu vào, `returns` nêu kết quả và `remarks` chỉ ra các lời gọi được phối hợp. Các đoạn XAML dùng `<!-- ... -->`; PowerShell dùng phần trợ giúp và comment `#`; batch dùng `rem`. Giữ nguyên những ghi chú đã có.

## Phạm vi cập nhật ngày 30/10/2026

- Đọc 305 file C# và mã kiểm chứng `.cs.txt` thuộc BUS, DAL, ET, WPF và tools. Bổ sung chú thích cho 2.976 khai báo còn thiếu trong 219 file, gồm kiểu dữ liệu, hàm khởi tạo, phương thức, thuộc tính, trường, sự kiện và lựa chọn enum.
- Bổ sung 1.206 chú thích trong 73 file XAML cho màn hình, tài nguyên dùng chung, vùng bố cục, trường dữ liệu và các sự kiện điều hướng sang code-behind.
- Giải thích cấu hình của bốn project, phần trợ giúp/các bước chạy của bảy script PowerShell và cách truyền đối số/mã thoát trong `KiemTra.bat`.
- Giữ nguyên các chú thích cũ trong SQL và metadata XML. Không chỉnh file SQL chính, dữ liệu cấu hình JSON, ảnh, tài liệu thiết kế/bằng chứng cũ hoặc mã sinh tự động trong bin/obj.

## Cách đọc theo tầng

| Khu vực | Vai trò và nội dung cần chú ý |
| --- | --- |
| ET | Thực thể ánh xạ SQL và hợp đồng dữ liệu giữa các bước. Mã định danh liên kết đúng bệnh nhân/lần khám/chứng từ; giá trị nullable thể hiện thông tin có thể thiếu. Constructor và setter không tự ghi SQL. |
| DAL | Đọc/ghi SQL, kiểm tra dữ liệu hiện tại, quyền truy cập và liên kết nghiệp vụ. Đọc kỹ giao dịch, các khóa, dấu vân tay và điều kiện trạng thái trước khi sửa/xóa/chốt. |
| BUS | Điểm gọi nghiệp vụ cho giao diện. Có lớp chuyển tiếp DAL và xử lý lỗi; các đầu mối thanh toán/bàn giao tự quản lý giao dịch theo quy tắc riêng. Không suy ra rằng mọi BUS đều giống một lớp CRUD. |
| WPF Views | XAML định nghĩa bố cục/binding/sự kiện; code-behind nhận thao tác, gọi nghiệp vụ và cập nhật dữ liệu đang hiển thị. Các cờ đang nạp/có thay đổi chưa lưu giúp xử lý đúng chuỗi sự kiện. |
| WPF Controls/Presentation | Chia sẻ nhập liệu, phân trang, quyền, chuyển ngữ cảnh, công việc chưa lưu, bản in và dữ liệu trình bày. Các phần `partial` cùng tham gia một lớp; đọc cùng file chính trong Views. |
| tools | Công cụ kiểm tra/xuất dữ liệu. Mã C# trong `.cs.txt` được script chép vào project tạm dưới obj để chạy; không thêm project sản phẩm. Đọc trợ giúp trước khi dùng chế độ tạo database hoặc cập nhật snapshot. |

## Những quy tắc quan trọng đã giải thích trong code

Nhân sự đồng bộ trạng thái theo ngày hiệu lực của đơn đã duyệt, kể cả khi ứng dụng không mở vào ngày đó. Cập nhật hồ sơ không ghi đè trạng thái vừa đồng bộ bằng giá trị cũ trên biểu mẫu. Khôi phục người đã nghỉ kết thúc hiệu lực đơn nghỉ việc cũ và yêu cầu duyệt lại các ca tương lai có liên quan.

Khám và kê đơn sử dụng phạm vi người dùng được xác thực lại ở DAL. Dấu vân tay là giá trị đối chiếu bản dữ liệu đã đọc với bản hiện tại; khi không khớp, người dùng cần tải lại để đối chiếu. `RxFingerprint` tạo giá trị băm, còn `RxHash` kiểm tra hai token đã có. Hàm `Route` trong DAL đơn thuốc tra lần khám từ mã đơn, phục vụ định tuyến tới đúng nguồn.

Các API lưu/xác nhận/hủy dịch vụ độc lập của `DAL_DICHVUTHUCHIEN` đã khóa và luôn phát ngoại lệ; các hàm BUS tương ứng cũng không trả kết quả thành công. Lịch sử dịch vụ chỉ dùng tra cứu. Việc xác nhận thực hiện diễn ra trong quy trình khám; tránh nhầm với API có tên tương tự ở `DAL_KHAMDIEUTRI`.

Thanh toán phối hợp hóa đơn, xuất thuốc, khoản thu và bàn giao theo một giao dịch. Mã yêu cầu và dấu vân tay hỗ trợ đối chiếu khi thử lại. Đọc phần kiểm nguồn phí/khóa dữ liệu để hiểu vì sao không thể thay giá, tồn hoặc đơn nguồn sau khi người dùng đã xem bảng tính mà không đối chiếu lại.

## Kiểm chứng việc chỉ thêm chú thích

Các file C# thay đổi được so token bằng Roslyn với bản trước khi thêm chú thích, bao gồm nguyên nội dung chuỗi SQL. Cấu trúc phần tử, thuộc tính và nội dung có ý nghĩa của XAML được đối chiếu sau khi bỏ comment. Các script PowerShell được phân tích cú pháp và so token ngoài comment/xuống dòng. Kiểm thêm build Debug/Release và bộ kiểm bàn giao/SQL chỉ đọc.

File `Database/QLBenhVienRangHamMat.sql` được bảo toàn theo SHA-256:

```text
8FFA08075C88C4534080A78BE8B9C8B3CE5CAA48483F62FF3270A501905F86D1
```

Các thay đổi SQL, bảo trì và công cụ xuất dữ liệu đã có trước đợt cập nhật được giữ để đưa vào commit bàn giao. File `Database/QLBenhVienRangHamMat_new.sql` từng được kéo từ nhánh `tan`; sau khi xác nhận các cập nhật nghiệp vụ đã có đầy đủ trong file chính, đã xóa file phụ theo yêu cầu của chủ dự án. Thư mục Database chỉ giữ một file SQL `QLBenhVienRangHamMat.sql`; chi tiết đối chiếu tại [HOP_NHAT_V9.md](database/HOP_NHAT_V9.md).
