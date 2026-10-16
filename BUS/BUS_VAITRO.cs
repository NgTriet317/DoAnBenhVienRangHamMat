// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ vai trò tài khoản, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_VAITRO. Các lời gọi
    /// DAL được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_VAITRO
    {
        /// <summary>
        /// Đối tượng DAL_VAITRO thực hiện truy vấn/ghi vai trò tài khoản trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_VAITRO data;
        /// <summary>
        /// Khởi tạo DAL_VAITRO bằng transaction tùy chọn để các thao tác vai trò tài khoản dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_VAITRO(IDbTransaction? transaction = null) { data = new(transaction); }
        /// <summary>
        /// Gọi DAL_VAITRO.layToanBoVaiTro để đọc toàn bộ vai trò tài khoản; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể
        /// rỗng khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        public IEnumerable<ET_VAITRO> layToanBoVaiTro() => XuLyLoi.ThucHien(() => data.layToanBoVaiTro());
        /// <summary>
        /// Gọi DAL_VAITRO.layVaiTroTheoMa với ma để tra vai trò tài khoản theo khóa; trả ET tương ứng hoặc null nếu không tìm
        /// thấy. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_VAITRO? layVaiTroTheoMa(string ma) => XuLyLoi.ThucHien(() => data.layVaiTroTheoMa(ma));
        /// <summary>
        /// Thêm vai trò tài khoản từ duLieu qua DAL_VAITRO.themVaiTro; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ liệu rồi
        /// INSERT trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. XuLyLoi chuyển lỗi SQL sang thông
        /// báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themVaiTro(ET_VAITRO duLieu) => XuLyLoi.ThucHien(() => data.themVaiTro(duLieu));
        /// <summary>
        /// Cập nhật vai trò tài khoản qua DAL_VAITRO.suaVaiTro; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn hóa dữ
        /// liệu và kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. XuLyLoi
        /// chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaVaiTro(ET_VAITRO duLieu, ET_VAITRO? banGoc = null, bool chiDoiTrangThai = false)
            => XuLyLoi.ThucHien(() => data.suaVaiTro(duLieu, banGoc, chiDoiTrangThai));
        /// <summary>
        /// Xóa vai trò tài khoản có mã ma qua DAL_VAITRO.xoaVaiTro; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi DELETE.
        /// Trả true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang
        /// thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaVaiTro(string ma, ET_VAITRO? banGoc = null) => XuLyLoi.ThucHien(() => data.xoaVaiTro(ma, banGoc));

    }
}