// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ dịch vụ, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_DICHVU. Các lời gọi DAL được
    /// bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_DICHVU
    {
        /// <summary>
        /// Đối tượng DAL_DICHVU thực hiện truy vấn/ghi dịch vụ trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_DICHVU dalDichVu;

        /// <summary>
        /// Khởi tạo DAL_DICHVU bằng transaction tùy chọn để các thao tác dịch vụ dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_DICHVU(IDbTransaction? transaction = null)
        {
            dalDichVu = new DAL_DICHVU(transaction);
        }

        /// <summary>
        /// Gọi DAL_DICHVU.layToanBoDichVu để đọc toàn bộ dịch vụ; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể rỗng khi chưa
        /// có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        public IEnumerable<ET_DICHVU> layToanBoDichVu()
        {
            return XuLyLoi.ThucHien(() => dalDichVu.layToanBoDichVu());
        }

        /// <summary>
        /// Gọi DAL_DICHVU.layDichVuTheoMa với ma để tra dịch vụ theo khóa; trả ET tương ứng hoặc null nếu không tìm thấy. XuLyLoi
        /// chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_DICHVU? layDichVuTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalDichVu.layDichVuTheoMa(ma));
        }

        /// <summary>
        /// Thêm dịch vụ từ duLieu qua DAL_DICHVU.themDichVu; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ liệu rồi INSERT trong
        /// giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. XuLyLoi chuyển lỗi SQL sang thông báo tiếng
        /// Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themDichVu(ET_DICHVU duLieu)
        {
            return XuLyLoi.ThucHien(() => dalDichVu.themDichVu(duLieu));
        }

        /// <summary>
        /// Cập nhật dịch vụ qua DAL_DICHVU.suaDichVu; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn hóa dữ liệu và kiểm
        /// tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. XuLyLoi chuyển lỗi SQL
        /// sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaDichVu(ET_DICHVU duLieu, ET_DICHVU? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalDichVu.suaDichVu(duLieu, banGoc, chiDoiTrangThai));
        }

        /// <summary>
        /// Xóa dịch vụ có mã ma qua DAL_DICHVU.xoaDichVu; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi DELETE. Trả true khi
        /// hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang thông báo tiếng
        /// Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaDichVu(string ma, ET_DICHVU? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalDichVu.xoaDichVu(ma, banGoc));
        }
    }
}
