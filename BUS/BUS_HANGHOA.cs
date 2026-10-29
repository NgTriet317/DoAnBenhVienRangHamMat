// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ hàng hóa, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_HANGHOA. Các lời gọi DAL được
    /// bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_HANGHOA
    {
        /// <summary>
        /// Đối tượng DAL_HANGHOA thực hiện truy vấn/ghi hàng hóa trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_HANGHOA dalHangHoa;

        /// <summary>
        /// Khởi tạo DAL_HANGHOA bằng transaction tùy chọn để các thao tác hàng hóa dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_HANGHOA(IDbTransaction? transaction = null)
        {
            dalHangHoa = new DAL_HANGHOA(transaction);
        }

        /// <summary>
        /// Gọi DAL_HANGHOA.layToanBoHangHoa để đọc toàn bộ hàng hóa; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể rỗng khi
        /// chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        public IEnumerable<ET_HANGHOA> layToanBoHangHoa()
        {
            return XuLyLoi.ThucHien(() => dalHangHoa.layToanBoHangHoa());
        }

        public DataTable layDanhMucChiTiet()
        {
            return XuLyLoi.ThucHien(() => dalHangHoa.layDanhMucChiTiet());
        }

        public bool luuDanhMucChiTiet(ET_HANGHOA duLieu, ET_HANGHOA? banGoc,
            string nhom, decimal mucTon, string hamLuong, decimal giaBan, bool dangThem)
        {
            return XuLyLoi.ThucHien(() => dalHangHoa.luuDanhMucChiTiet(
                duLieu, banGoc, nhom, mucTon, hamLuong, giaBan, dangThem));
        }

        public bool xoaDanhMucChiTiet(string ma, ET_HANGHOA? banGoc)
        {
            return XuLyLoi.ThucHien(() => dalHangHoa.xoaDanhMucChiTiet(ma, banGoc));
        }

        /// <summary>
        /// Gọi DAL_HANGHOA.layHangHoaTheoMa với ma để tra hàng hóa theo khóa; trả ET tương ứng hoặc null nếu không tìm thấy.
        /// XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_HANGHOA? layHangHoaTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalHangHoa.layHangHoaTheoMa(ma));
        }

        /// <summary>
        /// Thêm hàng hóa từ duLieu qua DAL_HANGHOA.themHangHoa; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ liệu rồi INSERT
        /// trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. XuLyLoi chuyển lỗi SQL sang thông báo
        /// tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themHangHoa(ET_HANGHOA duLieu)
        {
            return XuLyLoi.ThucHien(() => dalHangHoa.themHangHoa(duLieu));
        }

        /// <summary>
        /// Cập nhật hàng hóa qua DAL_HANGHOA.suaHangHoa; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn hóa dữ liệu và
        /// kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. XuLyLoi chuyển lỗi
        /// SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaHangHoa(ET_HANGHOA duLieu, ET_HANGHOA? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalHangHoa.suaHangHoa(duLieu, banGoc, chiDoiTrangThai));
        }

        /// <summary>
        /// Xóa hàng hóa có mã ma qua DAL_HANGHOA.xoaHangHoa; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi DELETE. Trả true
        /// khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang thông báo
        /// tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaHangHoa(string ma, ET_HANGHOA? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalHangHoa.xoaHangHoa(ma, banGoc));
        }
    }
}
