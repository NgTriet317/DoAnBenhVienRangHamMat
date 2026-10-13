// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ khoa, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_KHOA. Các lời gọi DAL được bọc
    /// XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_KHOA
    {
        /// <summary>
        /// Đối tượng DAL_KHOA thực hiện truy vấn/ghi khoa trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_KHOA data;
        /// <summary>
        /// Khởi tạo DAL_KHOA bằng transaction tùy chọn để các thao tác khoa dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_KHOA(IDbTransaction? transaction = null) { data = new(transaction); }
        /// <summary>
        /// Gọi DAL_KHOA.layToanBoKhoa để đọc toàn bộ khoa; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể rỗng khi chưa có dữ
        /// liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        public IEnumerable<ET_KHOA> layToanBoKhoa() => XuLyLoi.ThucHien(() => data.layToanBoKhoa());
        /// <summary>
        /// Gọi DAL_KHOA.layKhoaTheoMa với ma để tra khoa theo khóa; trả ET tương ứng hoặc null nếu không tìm thấy. XuLyLoi chuyển
        /// lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_KHOA? layKhoaTheoMa(string ma) => XuLyLoi.ThucHien(() => data.layKhoaTheoMa(ma));
        /// <summary>
        /// Thêm khoa từ duLieu qua DAL_KHOA.themKhoa; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ liệu rồi INSERT trong giao
        /// dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và
        /// giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themKhoa(ET_KHOA duLieu) => XuLyLoi.ThucHien(() => data.themKhoa(duLieu));
        /// <summary>
        /// Cập nhật khoa qua DAL_KHOA.suaKhoa; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn hóa dữ liệu và kiểm tra
        /// chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang
        /// thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaKhoa(ET_KHOA duLieu, ET_KHOA? banGoc = null, bool chiDoiTrangThai = false)
            => XuLyLoi.ThucHien(() => data.suaKhoa(duLieu, banGoc, chiDoiTrangThai));
        /// <summary>
        /// Xóa khoa có mã ma qua DAL_KHOA.xoaKhoa; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi DELETE. Trả true khi hoàn
        /// tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt
        /// và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaKhoa(string ma, ET_KHOA? banGoc = null) => XuLyLoi.ThucHien(() => data.xoaKhoa(ma, banGoc));

    }
}