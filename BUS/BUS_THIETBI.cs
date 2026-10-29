// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ thiết bị, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_THIETBI. Các lời gọi DAL được
    /// bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_THIETBI
    {
        /// <summary>
        /// Đối tượng DAL_THIETBI thực hiện truy vấn/ghi thiết bị trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_THIETBI data;
        /// <summary>
        /// Khởi tạo DAL_THIETBI bằng transaction tùy chọn để các thao tác thiết bị dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_THIETBI(IDbTransaction? transaction = null) { data = new(transaction); }
        /// <summary>
        /// Gọi DAL_THIETBI.layToanBoThietBi để đọc toàn bộ thiết bị; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể rỗng khi
        /// chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        public IEnumerable<ET_THIETBI> layToanBoThietBi() => XuLyLoi.ThucHien(() => data.layToanBoThietBi());
        /// <summary>
        /// Gọi DAL_THIETBI.layThietBiTheoMa với ma để tra thiết bị theo khóa; trả ET tương ứng hoặc null nếu không tìm thấy.
        /// XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_THIETBI? layThietBiTheoMa(string ma) => XuLyLoi.ThucHien(() => data.layThietBiTheoMa(ma));
        /// <summary>
        /// Thêm thiết bị từ duLieu qua DAL_THIETBI.themThietBi; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ liệu rồi INSERT
        /// trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. XuLyLoi chuyển lỗi SQL sang thông báo
        /// tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themThietBi(ET_THIETBI duLieu) => XuLyLoi.ThucHien(() => data.themThietBi(duLieu));
        /// <summary>
        /// Cập nhật thiết bị qua DAL_THIETBI.suaThietBi; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn hóa dữ liệu và
        /// kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. XuLyLoi chuyển lỗi
        /// SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaThietBi(ET_THIETBI duLieu, ET_THIETBI? banGoc = null, bool chiDoiTrangThai = false)
            => XuLyLoi.ThucHien(() => data.suaThietBi(duLieu, banGoc, chiDoiTrangThai));
        /// <summary>
        /// Xóa thiết bị có mã ma qua DAL_THIETBI.xoaThietBi; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi DELETE. Trả true
        /// khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang thông báo
        /// tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaThietBi(string ma, ET_THIETBI? banGoc = null) => XuLyLoi.ThucHien(() => data.xoaThietBi(ma, banGoc));

        // Chỉ phân hoặc thu hồi thiết bị có sẵn; không tạo mới hồ sơ thiết bị.
        public bool phanThietBiVaoPhong(string maTB, string? maPhong, ET_THIETBI banGoc)
            => XuLyLoi.ThucHien(() => data.phanThietBiVaoPhong(maTB, maPhong, banGoc));

    }
}