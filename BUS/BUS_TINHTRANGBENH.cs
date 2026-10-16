// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ tình trạng bệnh, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_TINHTRANGBENH. Các lời
    /// gọi DAL được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_TINHTRANGBENH
    {
        /// <summary>
        /// Đối tượng DAL_TINHTRANGBENH thực hiện truy vấn/ghi tình trạng bệnh trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_TINHTRANGBENH dalTinhTrangBenh;

        /// <summary>
        /// Khởi tạo DAL_TINHTRANGBENH bằng transaction tùy chọn để các thao tác tình trạng bệnh dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_TINHTRANGBENH(IDbTransaction? transaction = null)
        {
            dalTinhTrangBenh = new DAL_TINHTRANGBENH(transaction);
        }

        /// <summary>
        /// Gọi DAL_TINHTRANGBENH.layToanBoTinhTrangBenh để đọc toàn bộ tình trạng bệnh; trả danh sách ET do DAL đã tải vào bộ
        /// nhớ, có thể rỗng khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong
        /// InnerException.
        /// </summary>
        public IEnumerable<ET_TINHTRANGBENH> layToanBoTinhTrangBenh()
        {
            return XuLyLoi.ThucHien(() => dalTinhTrangBenh.layToanBoTinhTrangBenh());
        }

        /// <summary>
        /// Gọi DAL_TINHTRANGBENH.layTinhTrangBenhTheoMa với ma để tra tình trạng bệnh theo khóa; trả ET tương ứng hoặc null nếu
        /// không tìm thấy. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_TINHTRANGBENH? layTinhTrangBenhTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalTinhTrangBenh.layTinhTrangBenhTheoMa(ma));
        }

        /// <summary>
        /// Thêm tình trạng bệnh từ duLieu qua DAL_TINHTRANGBENH.themTinhTrangBenh; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ
        /// liệu rồi INSERT trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. Kiểm tra dữ liệu bắt buộc
        /// tại BUS trước khi gọi DAL. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themTinhTrangBenh(ET_TINHTRANGBENH duLieu)
        {
            KiemTraDuLieu(duLieu);
            return XuLyLoi.ThucHien(() => dalTinhTrangBenh.themTinhTrangBenh(duLieu));
        }

        /// <summary>
        /// Cập nhật tình trạng bệnh qua DAL_TINHTRANGBENH.suaTinhTrangBenh; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn
        /// hóa dữ liệu và kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. Kiểm
        /// tra dữ liệu bắt buộc tại BUS trước khi gọi DAL. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong
        /// InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaTinhTrangBenh(ET_TINHTRANGBENH duLieu, ET_TINHTRANGBENH? banGoc = null, bool chiDoiTrangThai = false)
        {
            KiemTraDuLieu(duLieu);
            return XuLyLoi.ThucHien(() => dalTinhTrangBenh.suaTinhTrangBenh(duLieu, banGoc, chiDoiTrangThai));
        }

        /// <summary>
        /// Xóa tình trạng bệnh có mã ma qua DAL_TINHTRANGBENH.xoaTinhTrangBenh; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi
        /// DELETE. Trả true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL
        /// sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaTinhTrangBenh(string ma, ET_TINHTRANGBENH? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalTinhTrangBenh.xoaTinhTrangBenh(ma, banGoc));
        }
        /// <summary>
        /// Yêu cầu mã lần khám, loại tình trạng và mô tả tình trạng bệnh. Sai điều kiện sẽ ném InvalidOperationException ngay,
        /// ngăn thao tác thêm/sửa gọi DAL; không ghi dữ liệu.
        /// </summary>
        /// <param name="record">Bản ET được kiểm tra hoặc chuyển vào thao tác cập nhật.</param>
        private static void KiemTraDuLieu(ET_TINHTRANGBENH record)
        {
            if (string.IsNullOrWhiteSpace(record.MaLS)) throw new InvalidOperationException("Vui lòng nhập/chọn MaLS.");
            if (string.IsNullOrWhiteSpace(record.LoaiTinhTrang)) throw new InvalidOperationException("Vui lòng nhập/chọn LoaiTinhTrang.");
            if (string.IsNullOrWhiteSpace(record.MoTaTinhTrang)) throw new InvalidOperationException("Vui lòng nhập/chọn MoTaTinhTrang.");
        }
    }
}
