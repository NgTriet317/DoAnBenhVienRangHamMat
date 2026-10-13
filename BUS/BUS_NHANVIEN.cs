// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ nhân viên, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_NHANVIEN. Các lời gọi DAL
    /// được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_NHANVIEN
    {
        /// <summary>
        /// Đối tượng DAL_NHANVIEN thực hiện truy vấn/ghi nhân viên trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_NHANVIEN dalNhanVien;

        /// <summary>
        /// Khởi tạo DAL_NHANVIEN bằng transaction tùy chọn để các thao tác nhân viên dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_NHANVIEN(IDbTransaction? transaction = null)
        {
            dalNhanVien = new DAL_NHANVIEN(transaction);
        }

        /// <summary>
        /// Gọi DAL_NHANVIEN.layToanBoNhanVien để đọc toàn bộ nhân viên; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể rỗng
        /// khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        public IEnumerable<ET_NHANVIEN> layToanBoNhanVien()
        {
            return XuLyLoi.ThucHien(() => dalNhanVien.layToanBoNhanVien());
        }

        /// <summary>
        /// Gọi DAL_NHANVIEN.layNhanVienTheoMa với ma để tra nhân viên theo khóa; trả ET tương ứng hoặc null nếu không tìm thấy.
        /// XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_NHANVIEN? layNhanVienTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalNhanVien.layNhanVienTheoMa(ma));
        }

        /// <summary>
        /// Thêm nhân viên từ duLieu qua DAL_NHANVIEN.themNhanVien; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ liệu rồi INSERT
        /// trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. XuLyLoi chuyển lỗi SQL sang thông báo
        /// tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themNhanVien(ET_NHANVIEN duLieu)
        {
            return XuLyLoi.ThucHien(() => dalNhanVien.themNhanVien(duLieu));
        }

        /// <summary>
        /// Cập nhật nhân viên qua DAL_NHANVIEN.suaNhanVien; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn hóa dữ liệu và
        /// kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. XuLyLoi chuyển lỗi
        /// SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaNhanVien(ET_NHANVIEN duLieu, ET_NHANVIEN? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalNhanVien.suaNhanVien(duLieu, banGoc, chiDoiTrangThai));
        }

        /// <summary>
        /// Xóa nhân viên có mã ma qua DAL_NHANVIEN.xoaNhanVien; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi DELETE. Trả
        /// true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang thông
        /// báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaNhanVien(string ma, ET_NHANVIEN? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalNhanVien.xoaNhanVien(ma, banGoc));
        }
    }
}
