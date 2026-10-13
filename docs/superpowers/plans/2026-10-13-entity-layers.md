# Kế hoạch sửa ET–DAL–BUS

> Thực hiện trực tiếp trong workspace người dùng; không commit/push. Dùng kiểm chứng trước khi báo hoàn tất.

**Goal:** Mỗi ET có DAL/BUS riêng, không có lớp nghiệp vụ điều phối chung.
**Architecture:** DAL sở hữu SQL/quy tắc từng thực thể; BUS gọi DAL/BUS liên quan. IDbTransaction dùng chung cho chứng từ nhiều dòng; dbconnect chỉ cung cấp hạ tầng SQL.
**Tech Stack:** C# .NET 8, WPF, Dapper, SQL Server.
**Spec:** ../specs/2026-10-13-entity-layers-design.md

## Ràng buộc

- Giữ thay đổi SQL sẵn có của người dùng.
- Mỗi ET hiện có có đúng DAL/BUS cùng hậu tố; MAVAITRO ánh xạ VAI_TRO.
- Không thay lớp điều phối cũ bằng lớp điều phối tên mới.
- Giữ giao dịch, rollback, kiểm tra phiên bản, khóa chứng từ và mã tự cấp.

## Công việc

- [x] 1. Thêm kiểm tra kiến trúc vào RangHamMat.Checks; đã thấy thiếu DAL/BUS và lớp tổng còn tồn tại. Chuyển hạ tầng kỹ thuật vào dbconnect, nhận IDbTransaction; chuyển trạng thái/liên kết về DAL thực thể, cập nhật cache WPF và runner.
- [x] 2. Bổ sung DAL/BUS còn thiếu và ET cũ theo schema. SchemaChecks kiểm tra đủ 39 ET và 303 cột, không có sai lệch.
- [x] 3. Đổi tên giao diện/helper/resource và mọi tham chiếu; build, Checks, PersistenceChecks, FinanceChecks, WpfSmoke đã đạt; cập nhật tài liệu cấu trúc hiện tại.

## Rà soát và quyết định thực hiện

- Thực hiện tại workspace hiện tại để giữ kết quả sửa trực tiếp cho người dùng; không commit/push và không sửa thay đổi SQL sẵn có.
- Giữ bộ chuyển dữ liệu form trong cache WPF: chuyển từ dictionary sang ET và gọi BUS tương ứng, không chứa SQL/quy tắc liên kết nghiệp vụ. Không tạo BUS điều phối thay thế.
- Rà soát độc lập phát hiện cleanup có thể bị bỏ qua khi Rollback ném lỗi. Đã tái hiện lỗi cache bị kẹt bằng ngắt connection của giao dịch thử, sửa cleanup bằng finally và guard giao dịch đã kết thúc; WpfSmoke sau sửa đạt.
- SQL metadata xác nhận tổng PHIEU_NHAP do trigger tính. Đã kiểm tra hồi quy thất bại với thiếu Write(false), rồi sửa ET/DAL và xác minh tổng không bị ghi đè qua CRUD typed.
- Mọi dữ liệu kiểm tra SQL được rollback; cảnh báo NU1900 của runner do nguồn dữ liệu kiểm tra lỗ hổng NuGet không truy cập được.

## Các điểm cần rà soát

- Lỗi dòng thứ hai rollback toàn bộ chứng từ và cache.
- Truy vấn cùng giao dịch đọc được dữ liệu chưa commit.
- Chặn sửa/xóa phiên bản cũ và chứng từ đã xác nhận.
- ET cũ phải có cột/nullability khớp SQL, cột computed không ghi.
- Điều hướng, x:Class và liên kết project vẫn tìm đúng file sau đổi tên.
