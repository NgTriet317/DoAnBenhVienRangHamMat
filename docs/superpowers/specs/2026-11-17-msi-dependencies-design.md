# Sửa bộ cài thiếu DLL và thư mục phát hành

Người dùng yêu cầu sửa ứng dụng không mở sau cài trên máy này/máy khác và không tự tạo Release-CauHinhDatabase. Ảnh LoiCauHinh.jpg báo thiếu DAL; Event Log 1026 trên máy báo thiếu System.Configuration.ConfigurationManager. Output group trước sửa chỉ có hai DLL đó dưới KioskServer, thiếu tại gốc.

Nguyên nhân: target thêm Kiosk vào ResolvedFileToPublish trước ComputeResolvedFilesToPublishList. SDK ResolveOverlappingItemGroupConflicts loại bản DLL gốc khi thấy trùng với backend. Chuyển target sang AfterTargets để chỉ thêm backend sau khi SDK lọc xong. Kiểm tra MSI phải xét đường dẫn cài đặt, không chỉ tên file.

Đưa output MSI Release về RHM/Release; bổ sung profile folder riêng cho MSI win-x64, không dùng ClickOnce/single-file. Đóng gói runtime cho WPF và backend để không phụ thuộc .NET đã cài trên máy đích; backend ưu tiên chạy apphost đi kèm. Bỏ prerequisite .NET Framework 4.7.2 không đúng loại ứng dụng. Giữ cờ yêu cầu cấu hình SQL ở lần cài mới/cài lại; không sửa database.

Đây là các lựa chọn triển khai của người thực hiện trong yêu cầu sửa bộ cài đã được cho phép. Chỉ chép thêm một DLL không giải quyết các phụ thuộc khác; buộc người dùng cài SDK hoặc chép bin thủ công không giải quyết bộ cài.

Kiểm chứng: RED bằng MSI lỗi hiện có; GREEN so đủ đường dẫn/kích thước file publish với MSI, giải nén MSI vào obj và mở chính executable đã giải nén, xác nhận màn hình SQL rồi thoát không lưu. Chạy health của backend khi PATH không có dotnet. Không tuyên bố đã thử máy khác hay gỡ/cài thực nếu chỉ giải nén và chạy.
