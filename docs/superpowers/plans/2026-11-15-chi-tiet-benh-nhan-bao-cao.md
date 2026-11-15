# Bổ sung bệnh nhân đến khám và làm rõ bốn ô tổng quan

**Yêu cầu:** Đổi nhãn bệnh nhân duy nhất thành số bệnh nhân đến khám, thay các ô kiểm chất lượng dữ liệu bằng thông tin nghiệp vụ, thêm bảng bệnh nhân/ngày giờ/chẩn đoán/tình trạng bên cạnh so sánh tháng đã có.

**Phương án:** Giữ chuyên đề và bộ lọc hiện tại; dùng bốn ô Số bệnh nhân đến khám, Tổng lượt khám, Số loại bệnh/tình trạng, Tình trạng gặp nhiều nhất. Bảng chi tiết đặt trước phần đối chiếu; mỗi lượt một dòng, gộp các tình trạng của lượt đó, không nhân dòng theo tình trạng hay dịch vụ. Tìm nhanh trong tập đã tải. Giữ thông báo thiếu mã ở phần phụ. Chẩn đoán hiển thị nguyên văn đã ghi nhận; không suy mã từ chữ.

**Ràng buộc:** Dùng quyền xem dữ liệu lâm sàng đang có cho bảng định danh bệnh nhân; DAL kiểm lại phiên/quyền. Quyền tổng hợp vẫn độc lập. Xuất chi tiết yêu cầu lại quyền chi tiết. Xóa cả dữ liệu định danh khi đổi bộ lọc/phiên. Không sửa schema hay dữ liệu khám. Thực hiện tiếp trên workspace hiện tại, không commit.

**Triển khai / kiểm tra:**

1. [x] Thêm kiểm tra thất bại cho nhãn bốn ô, bảng chi tiết và quyền lâm sàng trong `tools/ReportChecks.cs.txt`.
2. [x] Thêm DTO chi tiết ở ET, truy vấn một hàng/lượt trong `DAL_BAOCAO.BenhLy.cs`, điểm gọi BUS và chính sách chi tiết chuyên đề20. Kiểm SQL: đúng thời gian, bộ lọc, mã/tên/chẩn đoán/tình trạng, nhiều tình trạng không làm lặp lượt.
3. [x] Nối kết quả vào luồng tải, WPF, tìm nhanh, trạng thái trống/chưa tải/không quyền, xóa cache và CSV. Đổi bốn ô và giữ so sánh tháng. Kiểm giao diện 1024/1366/1920.
4. [x] Chạy bộ kiểm SQL/WPF, rà soát độc lập, lưu ảnh/hướng dẫn và kết quả.

Kết quả: build WPF Release thành công; 54/54 kiểm SQL/WPF đạt. Rà soát độc lập phát hiện tổng và chi tiết đọc tách giao dịch; đã thay bằng GoiBaoCaoBenhLy trên một kết nối/giao dịch, thêm kiểm hồi quy cho Admin và bác sĩ. Ảnh và log lưu trong XemTruoc/benh-ly-so-sanh.
