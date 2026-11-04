# Hướng dẫn chạy hiện tại — HospitalWorkflow3 / PrescriptionFlow1

Dùng [README](../../README.md) cho cấu hình và các lệnh cài đặt. File SQL duy nhất là Database/QLBenhVienRangHamMat.sql; chạy toàn bộ file để cài database trống hoặc đồng bộ bản v2/v3 được hỗ trợ. Không chạy lại các lệnh Migrations trong tài liệu lịch sử.

Bản hiện tại đã hợp nhất V9 với60 bảng, gồm lịch khám có bác sĩ dự bị, duyệt chấm công và tính/chốt lương. Không cần chạy thêm SQL/07 hoặc script duyệt công rời. Tiền các phiếu lương cũ được giữ nguyên; chỉ tính lại Nháp khi người dùng chủ động chọn. Chi tiết tại [HOP_NHAT_V9](../database/HOP_NHAT_V9.md).

- Sao lưu trước đồng bộ. Database đang dùng giữ hồ sơ/chứng từ/mật khẩu/giá; không tự dọn DEMO28 hoặc nạp đè dữ liệu đã có.
- Cài mới nạp bản dữ liệu phục hồi đã đóng gói:60 bảng,1.418 dòng (gồm8 nhật ký truy cập phát sinh khi kiểm giao diện), gồm11 bệnh nhân/11 hóa đơn/10 thiết bị/10 lịch sử bảo trì. Nguồn backup lúc15:32 ngày29/10/2026; xem [phục hồi và cập nhật snapshot](../database/PHUC_HOI_VA_DU_LIEU_SQL.md).
- Admin khởi tạo admin001 / 123456; đồng bộ không đặt lại mật khẩu tài khoản đã có. Mọi User theo chức vụ và phạm vi.
- Trong khám: ghi tình trạng → lập/duyệt kế hoạch → xác nhận **Thực hiện** → bấm **Kê thuốc** để mở cửa sổ lọc nhóm/chọn thuốc/nhập chỉ định → **Xác nhận đơn thuốc** đưa nháp về danh sách trong khám → mở sửa nếu cần → **Tổng kết** → **Chốt và lập hóa đơn** để ký nháp và lập hóa đơn cùng giao dịch. Lỗi không để lại chữ ký/hóa đơn dở. Nếu không kê thì lưu lý do. Thu tiền dùng lại hóa đơn; không phát sinh dịch vụ thêm.
- Đơn thuốc bên ngoài chỉ xem/in đơn bác sĩ đã kê và bàn giao sau thu đủ; không soạn đơn hoặc mở cấu hình mẫu. Kê thuốc không còn Mẫu v, áp hướng dẫn hay nút Tải lại bỏ nháp.
- Thuốc mới có52 sản phẩm/20 nhóm; database đang dùng có62 thuốc và không còn thuốc chưa phân nhóm. Có40 giá tham khảo,12 giá chưa thiết lập; cập nhật giá trước lập hóa đơn và nhập kho thực tế trước xuất/giao. Không sinh tồn kho giả. Nguồn tại [DANH_MUC_THUOC_RHM](DANH_MUC_THUOC_RHM.md).
- Dịch vụ thực hiện chỉ tra cứu các lần khám đã hoàn tất; không còn editor ghi hay API thêm ngoài luồng.
- Chọn nhanh tình trạng có47 mục theo nhóm bệnh viện RHM; dịch vụ gợi ý lấy từ SQL. Nguồn tại [DANH_MUC_RHM](DANH_MUC_RHM.md).
- Giữ toàn bộ thư mục PhatHanh, sửa appsettings.json cạnh exe cho SQL Server của máy. Đóng bản chạy cũ trước khi cập nhật DLL/schema.

Kiểm nguồn: KiemTra.bat -KiemTraSql. Kiểm nghiệp vụ fixture: tools/KiemTraQuyTrinhBenhVien.ps1 -TaoDatabaseKiemThu. Chi tiết [cửa sổ kê thuốc và chốt](CUA_SO_KE_THUOC.md).
