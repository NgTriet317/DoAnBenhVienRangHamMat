# Hướng dẫn chạy hiện tại — HospitalWorkflow3 / PrescriptionFlow1

Dùng [README](../../README.md) cho cấu hình và các lệnh cài đặt. File SQL duy nhất là Database/QLBenhVienRangHamMat.sql; chạy toàn bộ file để cài database trống hoặc đồng bộ bản v2/v3 được hỗ trợ. Không chạy lại các lệnh Migrations trong tài liệu lịch sử.

- Sao lưu trước đồng bộ. Database đang dùng giữ hồ sơ/chứng từ/mật khẩu/giá; bộ DEMO28 chưa được xử lý mới được dọn tự động theo các kiểm tra sở hữu/FK. Trường hợp đã phát sinh nghiệp vụ bị chặn để đối chiếu.
- Cài sạch chỉ có Admin hệ thống và danh mục thực tế có nguồn. Nhập nhân sự, phòng, lịch, thuốc và tồn thực tế qua các chức năng của dự án; không có người bệnh/bác sĩ giả để thao tác trên dữ liệu thật.
- Admin khởi tạo admin001 / 123456; đồng bộ không đặt lại mật khẩu tài khoản đã có. Mọi User theo chức vụ và phạm vi.
- Trong khám: ghi tình trạng → lập/duyệt kế hoạch → xác nhận **Thực hiện** → **Kê thuốc** và ký đơn hoặc lưu lý do không kê → **Tổng kết** dịch vụ thực tế/thuốc đã ký → lập hóa đơn. Hoàn tất không tự thực hiện kế hoạch còn thiếu. Thu tiền dùng lại hóa đơn; không phát sinh thêm dịch vụ.
- Đơn thuốc bên ngoài chỉ xem/in đơn bác sĩ đã kê và bàn giao sau thu đủ; không soạn đơn hoặc mở cấu hình mẫu. Kê thuốc không còn Mẫu v, áp hướng dẫn hay nút Tải lại bỏ nháp.
- Thuốc mới có52 sản phẩm/20 nhóm; database đang dùng có62 thuốc và không còn thuốc chưa phân nhóm. Có40 giá tham khảo,12 giá chưa thiết lập; cập nhật giá trước lập hóa đơn và nhập kho thực tế trước xuất/giao. Không sinh tồn kho giả. Nguồn tại [DANH_MUC_THUOC_RHM](DANH_MUC_THUOC_RHM.md).
- Dịch vụ thực hiện chỉ tra cứu các lần khám đã hoàn tất; không còn editor ghi hay API thêm ngoài luồng.
- Chọn nhanh tình trạng có47 mục theo nhóm bệnh viện RHM; dịch vụ gợi ý lấy từ SQL. Nguồn tại [DANH_MUC_RHM](DANH_MUC_RHM.md).
- Giữ toàn bộ thư mục PhatHanh, sửa appsettings.json cạnh exe cho SQL Server của máy. Đóng bản chạy cũ trước khi cập nhật DLL/schema.

Kiểm nguồn: KiemTra.bat -KiemTraSql. Kiểm nghiệp vụ fixture: tools/KiemTraQuyTrinhBenhVien.ps1 -TaoDatabaseKiemThu. Chi tiết [luồng kê thuốc hiện tại](KE_THUOC_TRONG_KHAM.md).
