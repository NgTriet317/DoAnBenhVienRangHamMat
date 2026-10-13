// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ chuyên khoa, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_CHUYENKHOA. Các lời gọi DAL
    /// được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_CHUYENKHOA
    {
        /// <summary>
        /// Đối tượng DAL_CHUYENKHOA thực hiện truy vấn/ghi chuyên khoa trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_CHUYENKHOA data;
        /// <summary>
        /// Khởi tạo DAL_CHUYENKHOA bằng transaction tùy chọn để các thao tác chuyên khoa dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_CHUYENKHOA(IDbTransaction? transaction = null) { data = new(transaction); }
        /// <summary>
        /// Gọi DAL_CHUYENKHOA.layToanBoChuyenKhoa để đọc toàn bộ chuyên khoa; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể
        /// rỗng khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        public IEnumerable<ET_CHUYENKHOA> layToanBoChuyenKhoa() => XuLyLoi.ThucHien(() => data.layToanBoChuyenKhoa());
        /// <summary>
        /// Gọi DAL_CHUYENKHOA.layChuyenKhoaTheoMa với ma để tra chuyên khoa theo khóa; trả ET tương ứng hoặc null nếu không tìm
        /// thấy. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_CHUYENKHOA? layChuyenKhoaTheoMa(string ma) => XuLyLoi.ThucHien(() => data.layChuyenKhoaTheoMa(ma));
        /// <summary>
        /// Thêm chuyên khoa từ duLieu qua DAL_CHUYENKHOA.themChuyenKhoa; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ liệu rồi
        /// INSERT trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. XuLyLoi chuyển lỗi SQL sang thông
        /// báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themChuyenKhoa(ET_CHUYENKHOA duLieu) => XuLyLoi.ThucHien(() => data.themChuyenKhoa(duLieu));
        /// <summary>
        /// Cập nhật chuyên khoa qua DAL_CHUYENKHOA.suaChuyenKhoa; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn hóa dữ
        /// liệu và kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. XuLyLoi
        /// chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaChuyenKhoa(ET_CHUYENKHOA duLieu, ET_CHUYENKHOA? banGoc = null, bool chiDoiTrangThai = false)
            => XuLyLoi.ThucHien(() => data.suaChuyenKhoa(duLieu, banGoc, chiDoiTrangThai));
        /// <summary>
        /// Xóa chuyên khoa có mã ma qua DAL_CHUYENKHOA.xoaChuyenKhoa; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi DELETE.
        /// Trả true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang
        /// thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaChuyenKhoa(string ma, ET_CHUYENKHOA? banGoc = null) => XuLyLoi.ThucHien(() => data.xoaChuyenKhoa(ma, banGoc));
        /// <summary>
        /// Tên gọi chuyển tiếp đến suaChuyenKhoa(record); dùng luồng sửa chuyên khoa với banGoc null và chiDoiTrangThai false mặc
        /// định, trả kết quả bool của thao tác sửa.
        /// </summary>
        /// <param name="record">Bản ET được kiểm tra hoặc chuyển vào thao tác cập nhật.</param>
        public bool capNhatChuyenKhoa(ET_CHUYENKHOA record) => suaChuyenKhoa(record);
    }
}