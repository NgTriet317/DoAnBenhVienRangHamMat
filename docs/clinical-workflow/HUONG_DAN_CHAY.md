# Hướng dẫn chạy hiện tại — HospitalWorkflow3

Dùng [README](../../README.md) cho cấu hình và các lệnh cài đặt. File SQL duy nhất là Database/QLBenhVienRangHamMat.sql; chạy toàn bộ file để cài database trống hoặc đồng bộ bản v2/v3 được hỗ trợ. Không chạy lại các lệnh Migrations trong tài liệu lịch sử.

- Sao lưu trước đồng bộ. Database đang dùng giữ hồ sơ/chứng từ/mật khẩu/giá; bộ DEMO28 chưa được xử lý mới được dọn tự động theo các kiểm tra sở hữu/FK. Trường hợp đã phát sinh nghiệp vụ bị chặn để đối chiếu.
- Cài sạch chỉ có Admin hệ thống và danh mục thực tế có nguồn. Nhập nhân sự, phòng, lịch, thuốc và tồn thực tế qua các chức năng của dự án; không có người bệnh/bác sĩ giả để thao tác trên dữ liệu thật.
- Admin khởi tạo admin001 / 123456; đồng bộ không đặt lại mật khẩu tài khoản đã có. Mọi User theo chức vụ và phạm vi.
- Hoàn tất khám xác nhận toàn bộ dịch vụ trong kế hoạch đã làm, chốt nguồn và lập hóa đơn chờ thu ngay. Thu tiền sau đó dùng lại hóa đơn; không phát sinh thêm dịch vụ.
- Dịch vụ thực hiện chỉ tra cứu các lần khám đã hoàn tất; không còn editor ghi hay API thêm ngoài luồng.
- Chọn nhanh tình trạng có47 mục theo nhóm bệnh viện RHM; dịch vụ gợi ý lấy từ SQL. Nguồn tại [DANH_MUC_RHM](DANH_MUC_RHM.md).
- Giữ toàn bộ thư mục PhatHanh, sửa appsettings.json cạnh exe cho SQL Server của máy. Đóng bản chạy cũ trước khi cập nhật DLL/schema.

Kiểm nguồn: KiemTra.bat -KiemTraSql. Kiểm nghiệp vụ fixture: tools/KiemTraQuyTrinhBenhVien.ps1 -TaoDatabaseKiemThu. Chi tiết [luồng hoàn tất và SQL](CHOT_KHAM_VA_SQL_THONG_NHAT.md).