// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ hóa đơn nhập hàng, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_HOADONNHAP. Các lời
    /// gọi DAL được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_HOADONNHAP
    {
        /// <summary>
        /// Đối tượng DAL_HOADONNHAP thực hiện truy vấn/ghi hóa đơn nhập hàng trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_HOADONNHAP data;
        /// <summary>
        /// Khởi tạo DAL_HOADONNHAP bằng transaction tùy chọn để các thao tác hóa đơn nhập hàng dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_HOADONNHAP(IDbTransaction? transaction = null) { data = new(transaction); }
        /// <summary>
        /// Gọi DAL_HOADONNHAP.layToanBoHoaDonNhap để đọc toàn bộ hóa đơn nhập hàng; trả danh sách ET do DAL đã tải vào bộ nhớ, có
        /// thể rỗng khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        public IEnumerable<ET_HOADONNHAP> layToanBoHoaDonNhap() => XuLyLoi.ThucHien(() => data.layToanBoHoaDonNhap());
        /// <summary>
        /// Gọi DAL_HOADONNHAP.layHoaDonNhapTheoMa với ma để tra hóa đơn nhập hàng theo khóa; trả ET tương ứng hoặc null nếu không
        /// tìm thấy. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_HOADONNHAP? layHoaDonNhapTheoMa(string ma) => XuLyLoi.ThucHien(() => data.layHoaDonNhapTheoMa(ma));
        /// <summary>
        /// Thêm hóa đơn nhập hàng từ duLieu qua DAL_HOADONNHAP.themHoaDonNhap; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ liệu
        /// rồi INSERT trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. XuLyLoi chuyển lỗi SQL sang
        /// thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themHoaDonNhap(ET_HOADONNHAP duLieu) => XuLyLoi.ThucHien(() => data.themHoaDonNhap(duLieu));
        /// <summary>
        /// Cập nhật hóa đơn nhập hàng qua DAL_HOADONNHAP.suaHoaDonNhap; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn hóa
        /// dữ liệu và kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. XuLyLoi
        /// chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaHoaDonNhap(ET_HOADONNHAP duLieu, ET_HOADONNHAP? banGoc = null, bool chiDoiTrangThai = false)
            => XuLyLoi.ThucHien(() => data.suaHoaDonNhap(duLieu, banGoc, chiDoiTrangThai));
        /// <summary>
        /// Xóa hóa đơn nhập hàng có mã ma qua DAL_HOADONNHAP.xoaHoaDonNhap; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi
        /// DELETE. Trả true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL
        /// sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaHoaDonNhap(string ma, ET_HOADONNHAP? banGoc = null) => XuLyLoi.ThucHien(() => data.xoaHoaDonNhap(ma, banGoc));

    }
}