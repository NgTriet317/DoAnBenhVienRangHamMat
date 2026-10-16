// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ thuốc, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_THUOC. Các lời gọi DAL được bọc
    /// XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_THUOC
    {
        /// <summary>
        /// Đối tượng DAL_THUOC thực hiện truy vấn/ghi thuốc trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_THUOC dalThuoc;

        /// <summary>
        /// Khởi tạo DAL_THUOC bằng transaction tùy chọn để các thao tác thuốc dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_THUOC(IDbTransaction? transaction = null)
        {
            dalThuoc = new DAL_THUOC(transaction);
        }

        /// <summary>
        /// Gọi DAL_THUOC.layToanBoThuoc để đọc toàn bộ thuốc; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể rỗng khi chưa có
        /// dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        public IEnumerable<ET_THUOC> layToanBoThuoc()
        {
            return XuLyLoi.ThucHien(() => dalThuoc.layToanBoThuoc());
        }

        /// <summary>
        /// Gọi DAL_THUOC.layThuocTheoMa với ma để tra thuốc theo khóa; trả ET tương ứng hoặc null nếu không tìm thấy. XuLyLoi
        /// chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_THUOC? layThuocTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalThuoc.layThuocTheoMa(ma));
        }

        /// <summary>
        /// Thêm thuốc từ duLieu qua DAL_THUOC.themThuoc; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ liệu rồi INSERT trong giao
        /// dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và
        /// giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themThuoc(ET_THUOC duLieu)
        {
            return XuLyLoi.ThucHien(() => dalThuoc.themThuoc(duLieu));
        }

        /// <summary>
        /// Cập nhật thuốc qua DAL_THUOC.suaThuoc; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn hóa dữ liệu và kiểm tra
        /// chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang
        /// thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaThuoc(ET_THUOC duLieu, ET_THUOC? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalThuoc.suaThuoc(duLieu, banGoc, chiDoiTrangThai));
        }

        /// <summary>
        /// Xóa thuốc có mã ma qua DAL_THUOC.xoaThuoc; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi DELETE. Trả true khi hoàn
        /// tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt
        /// và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaThuoc(string ma, ET_THUOC? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalThuoc.xoaThuoc(ma, banGoc));
        }
    }
}
