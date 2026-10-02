> Tài liệu yêu cầu gốc để tham khảo; không phải danh sách chức năng đã triển khai. Bản hiện tại chỉ có giao diện và điều hướng. Phạm vi trường dữ liệu xem DOI_CHIEU_DATABASE.md; cách dùng xem README.md.

> Cập nhật 28/09/2026: bản hiện hành dùng một cửa sổ toàn màn hình và trường dữ liệu theo SQL 34 bảng. Xem `README.md` và `DOI_CHIEU_DATABASE.md` để biết cấu trúc hiện tại. Các đối chiếu frontend bên dưới là lịch sử tham khảo.

# Đối chiếu yêu cầu và giao diện chuyển đổi

Nguồn đối chiếu là nội dung thân tài liệu `Nhom4.docx`, không dùng số mục trong mục lục vì mục lục chưa đồng bộ: thân tài liệu có Nhà cung cấp ở 2.5, Ca làm ở 2.6, Lịch khám ở 2.7, Tài chính ở 2.8 và Trí tuệ nhân tạo hỗ trợ ở 2.9.

## Nhom4.docx và các màn hình WPF

| Mục tài liệu | Màn hình và thao tác đã có | Giới hạn của bản giao diện |
|---|---|---|
| 2.1 Hồ sơ bệnh nhân | Hành chính, liên hệ, lý do khám, tiền sử, dị ứng; tìm kiếm; từng lần khám liên kết bệnh nhân; chẩn đoán, kế hoạch, răng/vùng, ngày tái khám; đơn thuốc và hóa đơn liên kết lần khám | Lịch sử xem ở Khám và điều trị bằng cách tìm tên/mã bệnh nhân. Chưa quản lý ảnh X-quang hoặc tệp đính kèm. Sơ đồ chỉ chọn răng vĩnh viễn; vùng khác/răng sữa nhập văn bản |
| 2.2 Bác sĩ và nhân sự | Thông tin cá nhân, khoa/phòng, chuyên môn, bằng cấp, chứng chỉ, nơi/ngày cấp, ngày vào làm, trạng thái; liên kết hồ sơ tài khoản | Chưa có kiểm tra giấy phép trực tuyến hoặc xác thực tài khoản |
| 2.3 Trang thiết bị | Mã, tên, nhóm, số lượng, phòng, nhà cung cấp, tình trạng, hạn bảo trì; phân hệ lịch sử bảo trì/sửa chữa và chi phí | Bảo trì và trạng thái thiết bị được cập nhật thủ công tại các màn hình tương ứng |
| 2.4 Vật tư | Tồn đầu, nhập, xuất, tồn tự tính; mỗi mã gắn một lô; hạn dùng, cảnh báo tồn thấp và gần hết hạn 30 ngày; chặn xuất âm kho | Chưa có kiểm kê, chuyển kho, nhiều kho hoặc tự trừ vật tư theo dịch vụ; hạn dùng là cảnh báo, chưa tự chặn xuất hàng hết hạn |
| 2.5 Nhà cung cấp | Mã, tên, địa chỉ, điện thoại, email, người liên hệ, trạng thái; tìm và xuất danh sách | Chưa có đơn đặt mua hay công nợ nhà cung cấp |
| 2.6 Ca làm việc | Lịch ngày/tuần/tháng, bác sĩ, phòng, giờ, trạng thái, điểm danh, lý do xin nghỉ/đổi ca; chặn trùng bác sĩ hoặc phòng | Xin nghỉ/đổi ca là trạng thái và ghi chú, chưa có quy trình duyệt nhiều bước |
| 2.7 Lịch khám | Liên kết bệnh nhân, bác sĩ, phòng, dịch vụ; lịch ngày/tuần/tháng; bảng lọc; các trạng thái đã đặt/chờ/đang khám/hoàn tất/dời/hủy; kiểm tra trùng giờ | Chưa có nhắc lịch; chưa bắt buộc lịch hẹn nằm trong ca bác sĩ; lịch ngày hiển thị danh sách thẻ theo giờ, không có trục kéo thả |
| 2.8 Tài chính | Hóa đơn, giảm giá, số đã thu, công nợ, hình thức thu; tổng hợp khoảng ngày và doanh thu dịch vụ; lương và hoa hồng dự tính | Mỗi hóa đơn một dịch vụ; số đã thu lũy kế, chưa có sổ giao dịch tiền, hoàn tiền hay báo cáo lãi/lỗ; không tự tính lương từ ca làm |
| 2.9 Trí tuệ nhân tạo hỗ trợ | Màn hình Phiếu hướng dẫn: thông tin người bệnh, nội dung đã ghi nhận, khoa, phòng, ghi chú; xem trước/in/lưu PDF | Mới đáp ứng phần giao diện và xuất phiếu. Khoa do nhân viên chọn; ánh xạ phòng theo danh mục cố định. Chưa gọi mô hình AI hoặc tự chẩn đoán |
| 3.1 Người dùng | 5 vai trò đúng nhóm trong tài liệu; menu lọc theo vai trò và kiểm tra quyền ghi trong DataStore | Quyền ở mức phân hệ trong bản demo; có thể đổi vai trò mà không xác thực. Nhật ký local không chống sửa |
| 3.2 Phạm vi | Ứng dụng Windows, dữ liệu giả, quản lý và báo cáo đơn giản | Không tích hợp BHYT, thanh toán điện tử, chữ ký số, thiết bị y tế hay hệ thống ngoài |

## Chuyển đổi từ mã Vue gốc

| Nguồn trong frontend_admin.zip | Thành phần WPF tương ứng | Thay đổi theo bệnh viện |
|---|---|---|
| `Sidebar.vue`, `Topbar.vue`, `AdminLayout.vue` | `MainWindow.xaml`, `Theme.xaml` | Cập nhật 27/09/2026: Home dạng lưới app, tông xanh dương, app và form mở trong cửa sổ chính toàn màn hình qua AppWorkspace; giữ bộ chọn vai trò mô phỏng |
| `Dashboard.vue`, `StatCard.vue`, các biểu đồ | `DashboardView`, `ReportsView` | KPI điều phối lượt khám, cảnh báo kho/bảo trì và công nợ. Số liệu tính từ dữ liệu local, không gắn số cứng trong UI |
| `CustomerManagement.vue` | `RecordsView` cấu hình `patients`, `visits`, `prescriptions` | Khách hàng → Bệnh nhân; ưu tiên thông tin y tế, lần khám và răng/vùng điều trị |
| `AppointmentManagement.vue`, `CalendarBoard.vue`, các modal | `ScheduleView`, `RecordsView`, `EditorWindow` | Lịch ngày/tuần/tháng; cập nhật qua biểu mẫu; kiểm tra trùng bác sĩ/phòng/bệnh nhân |
| `StaffManagement.vue`, `EmployeeDetail.vue`, `EmployeeFormModal.vue` | `RecordsView` cấu hình `staff` | Nhân sự nội bộ, chứng chỉ hành nghề và khoa/bộ phận |
| `ShiftManagement.vue`, `ShiftCard.vue` | `ScheduleView` cấu hình `shifts` | Phân ca bác sĩ theo phòng khám; trạng thái và điểm danh |
| Các `src/services/*.js` gọi API | `Core/DataStore.cs` | Thay bằng dữ liệu giả + JSON local. Không dùng lại URL API hoặc phụ thuộc backend web gốc |
| Menu gốc chưa có route thực hiện cho thu/chi, kho, báo cáo... | Vật tư, nhập xuất, thiết bị, bảo trì, NCC, hóa đơn, lương, báo cáo, vai trò, phiếu | Bổ sung màn hình theo nội dung Nhom4.docx |

## Quyền mô phỏng

| Vai trò | Xem | Thêm hoặc sửa |
|---|---|---|
| Quản trị hệ thống | Tất cả | Tất cả phân hệ dữ liệu; chứng từ kho đã ghi sổ không được sửa |
| Ban quản lý / Kế toán | Bệnh nhân, lịch, nhân sự, ca, kho, thiết bị, NCC, tài chính, dịch vụ, báo cáo | Nhân sự, ca làm, hóa đơn, lương |
| Người hướng dẫn bệnh nhân | Bệnh nhân, lịch, ca làm, hóa đơn, dịch vụ, phiếu hướng dẫn | Bệnh nhân, lịch khám, hóa đơn; tạo phiếu |
| Bác sĩ | Bệnh nhân, lịch, ca làm, khám điều trị, đơn thuốc, dịch vụ, phiếu hướng dẫn | Khám điều trị, đơn thuốc; tạo phiếu |
| Nhân viên kho / thiết bị | Vật tư, nhập xuất, thiết bị, bảo trì, nhà cung cấp | Các phân hệ kho và thiết bị |

Ma trận này là một cách triển khai đề xuất từ phạm vi người dùng của tài liệu. Nhóm có thể điều chỉnh `ReadRoles` và `WriteRoles` trong `Core/Modules.json`.
