# Sprint 3 – Thanh Tân

**Bản cuối:** xem [Biên bản nghiệm thu](acceptance-report.md). Phần triển khai chức năng/database đã hoàn tất. Người dùng đã yêu cầu bàn giao toàn dự án lên nhánh mới `Sprint3_tan`.

## Phạm vi nhật ký đã chốt

Theo yêu cầu mới của người dùng, nhật ký chỉ ghi nhận thao tác làm thay đổi dữ liệu ở các phân hệ và LOGIN_SUCCESS, LOGIN_FAILED, LOGOUT. Xem/tra cứu không sinh log. Kết thúc phiên bất thường chỉ cập nhật trạng thái phiên nội bộ, không tạo sự kiện nhật ký. Màn hình chỉ còn danh sách sự kiện, không có tab phiên; KPI không tính log đọc/kỹ thuật. Lịch sử cũ không bị xóa nhưng được loại tại SQL trước COUNT/OFFSET. Bằng chứng mới: evidence/operation-scope-final.log (210 kiểm tra) và evidence/scope-clinical-regression.log.

## Database duy nhất

Toàn bộ schema, nâng cấp trạng thái danh mục, phiên đăng nhập, audit và chỉ mục nằm trong **`Database/QLBenhVienRangHamMat.sql`**. Không cần file migration SQL riêng. File này cài mới hoặc nâng cấp database hiện hữu và chạy lại được.

Ứng dụng kết nối database `QLBenhVienRangHamMat` trên SQL Server; file `.sql` là chương trình cài đặt/đồng bộ database, không phải dữ liệu được WPF đọc trực tiếp.

```powershell
sqlcmd -S . -E -C -b -f65001 -i Database/QLBenhVienRangHamMat.sql
```

Database chính cục bộ đã được cập nhật trong phiên triển khai. Trước khi cập nhật đã BACKUP COPY_ONLY/CHECKSUM và RESTORE VERIFYONLY thành công, vào thư mục backup của SQL Server ngoài dự án. Đối chiếu sau cập nhật: **60 bảng hiện hữu giữ nguyên số hàng**, tổng 62 bảng sau khi có `PHAN_QUYEN_VAI_TRO` và `PHIEN_DANG_NHAP`. Xem `evidence/main-preservation.txt`.

## Chức năng

- Danh mục 10 bảng P0/P1: danh sách, tìm/lọc, form thêm/sửa trong màn hình, ngừng dùng, khôi phục; giữ route `DichVu`, sử dụng bảng `PHONG` thực tế. Whitelist bảng/cột, kiểm tra giá trị, FK, trùng tên, bản gốc chống ghi đè, giữ khóa ghép.
- Trạng thái danh mục lâm sàng được giữ theo CHECK SQL hiện hữu. Không xóa vật lý danh mục; API xóa cũ của sáu danh mục báo yêu cầu ngừng dùng.
- Danh mục cha đang có con hoạt động hoặc phòng/dịch vụ có lịch/phiếu khám mở bị chặn ngừng dùng. Ngày nghỉ đã qua được bảo vệ bằng chính sách chặn sửa tại màn hình tổng hợp.
- Backend dùng chung kiểm tra FK mới/đổi phải đang hoạt động; giữ nguyên liên kết cũ để lịch sử vẫn sử dụng được. Bộ chọn Nhân sự/Hàng hóa lọc danh mục hoạt động và giữ tên/mã gốc khi xem bản ghi cũ. Ngày nghỉ ngừng dùng được loại khỏi module SQL tính công/lương mới; không tính lại chứng từ đã chốt.
- Nhật ký SQL: KPI, lọc ngày/giờ UTC+7, tài khoản/nhân viên, phân hệ, hành động, kết quả, đối tượng; SQL phân trang 25/50/100, danh sách sự kiện và chi tiết chỉ đọc. Payload hiển thị theo whitelist; không hiển thị mật khẩu/hash hay nội dung bệnh án đầy đủ.
- LOGIN_FAILED chỉ ghi với tài khoản có thật, giới hạn một lần/phút, không lưu mật khẩu hoặc tạo MaTK giả. Phiên được tạo sau xác thực thực sự; login/logout ghi đúng một lần; hủy Closing giữ phiên mở. Heartbeat mỗi phút, quá hạn 5 phút được đối soát ở lần đăng nhập sau; không gán giờ logout giả khi crash.
- Audit danh mục cùng transaction nghiệp vụ; tái sử dụng trigger lâm sàng cũ để tránh ghi trùng. Audit tại editor chung ghi tên trường thay đổi, không chép giá trị y tế cá nhân.
- Bổ sung 30 trigger audit cho các bảng Kho/Nhân sự/Lương/Tài khoản/Vai trò và các bảng liên quan chưa qua editor chung. Trigger chỉ ghi khi có phiên ứng dụng đã xác thực, kiểm tra lại phiên/tài khoản và ghi cùng transaction. Chỉ lưu PK/trạng thái và tên trường được đổi, không dump bản ghi. Có sự kiện ROLE_CHANGE, PERMISSION_CHANGE, ACCOUNT_LOCK/RESTORE và ánh xạ các trạng thái duyệt/xác nhận/chốt/hủy.
- Các luồng khám/tiếp nhận/thanh toán có audit sẵn nhận UTC/event/phiên qua DEFAULT và session context từ kết nối ứng dụng. Các bảng P2 giữ quy trình duyệt/phiên bản ở màn hình chuyên biệt, không mở CRUD tùy ý.

## Kiểm chứng

| Kiểm tra | Bằng chứng |
|---|---|
| Cài mới bằng file SQL chính, rồi chạy lại, giữ số hàng | `evidence/unified-fresh.log` |
| 198 kiểm tra SQL/WPF, installer và nhiều tiến trình | `evidence/acceptance-final.log` |
| 47 ca hồi quy hiện hữu trên schema hợp nhất | `evidence/unified-regression.log` |
| Schema/bàn giao chỉ đọc trên DB chính | `evidence/unified-handover.log` |
| DB chính: không mất hàng của 60 bảng cũ | `evidence/main-preservation.txt`, `main-counts-before.txt`, `main-counts-after.txt` |
| Debug/Release và hợp đồng ET | `evidence/build-debug-final.log`, `build-release-final.log`, `build-and-contracts.log` |
| Ảnh hai màn hình đọc DB fixture SQL thật | `evidence/catalog-*.png`, `audit-*.png` |

Kiểm thử dùng DB cách ly, đọc file SQL chính và chỉ đổi tên database/loại snapshot trong bộ nhớ; không tạo file database SQL phụ trong dự án. Fixture chứa dữ liệu `S3_*`, không chèn fixture vào DB chính.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraSprint3.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File tools/KiemTraSprint3Sql.ps1 -CaiDat
```

Các log `integration.log`, `regression.log`, `migration-preservation.log` là bằng chứng đợt trước; log `unified-*` là kiểm chứng sau hợp nhất. Nội dung log cài đặt DB chính được rút gọn để không xuất các truy vấn nghiệp vụ thật.

## Kết luận nghiệm thu

Đã bổ sung kiểm thử nút Lưu/Ngừng dùng/Khôi phục trên cả 10 danh mục, hai tiến trình sửa đồng thời, kill tiến trình thật, LOGIN_FAILED có kiểm soát và RESTORE database fixture. Hồi quy khám: 47 ca; công/lương: 32 ca; bảo trì: 58 ca, đều đạt. Chi tiết các giới hạn môi trường và bàn giao Git nằm trong [biên bản nghiệm thu](acceptance-report.md).

## Làm mới nhật ký và tiếng Việt

Theo yêu cầu người dùng đã xóa một lần 631 hàng NHAT_KY_THAO_TAC ở database chính, xác nhận còn 0 hàng trong giao dịch (evidence/log-reset-result.txt). Không thêm lệnh xóa tự động khi mở ứng dụng hoặc chạy lại schema. Hành động dùng chung mapping ET/HanhDongNhatKy.cs: combobox, danh sách và chi tiết hiển thị tiếng Việt; lọc SQL giữ mã ổn định. Kiểm chứng 218 kiểm tra đạt trong evidence/vietnamese-actions-final.log, gồm chọn Cập nhật thông tin lọc đúng UPDATE và Xóa lọc về Tất cả hành động.

## Thiết kế lại chi tiết nhật ký

Panel chi tiết dùng thẻ trắng, tiêu đề hành động tiếng Việt và nhãn kết quả; nhóm người thực hiện, tài khoản, thời gian, phân hệ/đối tượng. Giá trị trước–sau được trình bày thành từng thẻ có nhãn tiếng Việt thay cho JSON thô. Mã nhật ký/phiên nằm trong mục tham chiếu thu gọn. Có trạng thái chưa chọn và nút bỏ chọn; tải lại không giữ chi tiết dòng cũ. Kiểm tra trên DB fixture, không ghi dữ liệu vào DB chính. Ảnh: evidence/audit-detail-1280.png, audit-detail-1024.png và audit-detail-changes-*.png. Bằng chứng kiểm thử: evidence/detail-ui-final.log.

## Bàn giao GitHub

Nhánh bàn giao: Sprint3_tan, từ nhánh tan tại commit 9d43b87fe6576b15b0fbad8d90dfb56181e1086a. Kho mã: https://github.com/NgTriet317/DoAnBenhVienRangHamMat/tree/Sprint3_tan. Bản bàn giao bao gồm mã nguồn, SQL duy nhất, kế hoạch Word, công cụ kiểm thử và bằng chứng; không bao gồm bin/obj, PhatHanh hoặc backup/database vật lý.
