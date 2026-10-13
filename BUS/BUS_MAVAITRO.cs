// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ vai trò tài khoản, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_MAVAITRO. Các lời gọi
    /// DAL được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_MAVAITRO
    {
        /// <summary>
        /// Đối tượng DAL_MAVAITRO thực hiện truy vấn/ghi vai trò tài khoản trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_MAVAITRO data;
        /// <summary>
        /// Khởi tạo DAL_MAVAITRO bằng transaction tùy chọn để các thao tác vai trò tài khoản dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_MAVAITRO(IDbTransaction? transaction = null) { data = new(transaction); }
        /// <summary>
        /// Gọi DAL_MAVAITRO.layToanBoMaVaiTro để đọc toàn bộ vai trò tài khoản; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể
        /// rỗng khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        public IEnumerable<ET_MAVAITRO> layToanBoMaVaiTro() => XuLyLoi.ThucHien(() => data.layToanBoMaVaiTro());
        /// <summary>
        /// Gọi DAL_MAVAITRO.layMaVaiTroTheoMa với ma để tra vai trò tài khoản theo khóa; trả ET tương ứng hoặc null nếu không tìm
        /// thấy. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_MAVAITRO? layMaVaiTroTheoMa(string ma) => XuLyLoi.ThucHien(() => data.layMaVaiTroTheoMa(ma));
        /// <summary>
        /// Thêm vai trò tài khoản từ duLieu qua DAL_MAVAITRO.themMaVaiTro; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ liệu rồi
        /// INSERT trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. XuLyLoi chuyển lỗi SQL sang thông
        /// báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themMaVaiTro(ET_MAVAITRO duLieu) => XuLyLoi.ThucHien(() => data.themMaVaiTro(duLieu));
        /// <summary>
        /// Cập nhật vai trò tài khoản qua DAL_MAVAITRO.suaMaVaiTro; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn hóa dữ
        /// liệu và kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. XuLyLoi
        /// chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaMaVaiTro(ET_MAVAITRO duLieu, ET_MAVAITRO? banGoc = null, bool chiDoiTrangThai = false)
            => XuLyLoi.ThucHien(() => data.suaMaVaiTro(duLieu, banGoc, chiDoiTrangThai));
        /// <summary>
        /// Xóa vai trò tài khoản có mã ma qua DAL_MAVAITRO.xoaMaVaiTro; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi DELETE.
        /// Trả true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang
        /// thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaMaVaiTro(string ma, ET_MAVAITRO? banGoc = null) => XuLyLoi.ThucHien(() => data.xoaMaVaiTro(ma, banGoc));

    }
}