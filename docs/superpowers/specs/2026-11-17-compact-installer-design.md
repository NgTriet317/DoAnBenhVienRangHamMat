# Thu gọn bộ cài Windows x64

Người dùng đã chọn triển khai phương án gom thư viện vào executable, giữ thư mục Database. Thực hiện sau khi đưa bản tan hiện tại lên master; mã mới chỉ nằm trên tan. Master đã cập nhật tới 8fe4016 trước khi thay đổi.

## Mục tiêu và ràng buộc

- Giảm số file/thư mục sau khi cài, giữ đầy đủ chức năng hiện có và runtime .NET 8 đi kèm trên Windows x64.
- Giữ `Database/QLBenhVienRangHamMat.sql`, `appsettings.json`, cấu hình/prompt AI, ảnh X-quang và nguồn/giấy phép ảnh ở các đường dẫn hiện hành.
- Giữ cờ cấu hình SQL sau cài đặt. Không chạy SQL, sửa database nghiệp vụ hoặc thay cấu hình SQL của người dùng khi thử bộ cài.
- Không trimming vì WPF, DICOM và các thư viện dùng reflection. Không đóng gói khóa AI cục bộ.

## Các phương án

1. Gom DLL/runtime vào executable của WPF và executable riêng của Kiosk. Người dùng đã yêu cầu thực hiện đề xuất này. Cần sửa bộ khởi động Kiosk để không bắt buộc DLL nằm ngoài; phải kiểm các thư viện native.
2. Giữ toàn bộ payload trong một thư mục con và thêm launcher: gốc nhìn gọn nhưng vẫn còn 923 file và thêm bước chạy gián tiếp.

## Thiết kế

Profile Installer và Folder dùng single-file self-contained, gom native runtime, không trimming. Các tài nguyên đọc theo đường dẫn vẫn nằm ngoài executable. Backend được publish riêng bằng cùng chế độ, ở thư mục trung gian riêng để tránh lẫn file publish cũ. Không đưa backend vào bundle WPF. Không phát hành PDB rời.

Quyết định sau kiểm runtime: `Dicom.Native.dll` của fo-dicom.Codecs 5.16.6 là DLL mixed-mode C++/CLI, bundler nhận là managed nên không giải nén các export native. Bốn kiểm JPEG/JPEG-LS/JPEG2000 thất bại khi gom DLL này. Giữ riêng đúng DLL codec cạnh mỗi executable bằng `Directory.Build.targets`; các thư viện còn lại vẫn gom. Payload cuối dự kiến 32 file, gốc có 3 file và 4 thư mục. Đây là điều chỉnh cần thiết để bảo toàn chức năng, không bỏ hỗ trợ DICOM nén.

KioskApiHost ưu tiên executable; chỉ yêu cầu DLL nếu cần fallback dotnet của bản phát triển. Giữ cổng loopback động, cấu hình SQL, quản lý tiến trình con và health check hiện có.

Tăng version WPF/API/MSI lên 1.0.2, đổi ProductCode/PackageCode, giữ UpgradeCode để nâng cấp bản cũ. Bộ kiểm MSI đối chiếu đúng toàn bộ đường dẫn/kích thước với publish mới, kiểm tài nguyên và nhận diện executable đã bundle thay vì yêu cầu DLL nằm ngoài.

.NET tự giải nén thư viện native vào cache của người dùng khi chạy; thư mục cài đặt vẫn gọn. Đây là giảm file hiển thị, không phải loại bỏ runtime hoặc dữ liệu. Xem [tài liệu Microsoft](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview).

## Nghiệm thu

Kiểm RED→GREEN cho Kiosk không có DLL rời; publish và Rebuild MSI Release; giải nén quản trị MSI vào obj; so manifest và hash tài nguyên; mở/đóng màn hình SQL từ chính payload với PATH không có dotnet; chạy Kiosk health/trang web và DICOM tổng hợp bằng bản helper single-file. Chạy lại bộ kiểm cấu trúc/build Debug/Release và cấu hình SQL. Không tuyên bố đã cài/gỡ thật hoặc kiểm trên máy khác nếu chưa làm.
