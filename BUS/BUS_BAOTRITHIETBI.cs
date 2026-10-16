// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ bảo trì thiết bị, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_BAOTRITHIETBI. Các lời
    /// gọi DAL được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_BAOTRITHIETBI
    {
        /// <summary>
        /// Đối tượng DAL_BAOTRITHIETBI thực hiện truy vấn/ghi bảo trì thiết bị trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_BAOTRITHIETBI data;
        /// <summary>
        /// Khởi tạo DAL_BAOTRITHIETBI bằng transaction tùy chọn để các thao tác bảo trì thiết bị dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_BAOTRITHIETBI(IDbTransaction? transaction = null) { data = new(transaction); }
        /// <summary>
        /// Gọi DAL_BAOTRITHIETBI.layToanBoBaoTriThietBi để đọc toàn bộ bảo trì thiết bị; trả danh sách ET do DAL đã tải vào bộ
        /// nhớ, có thể rỗng khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong
        /// InnerException.
        /// </summary>
        public IEnumerable<ET_BAOTRITHIETBI> layToanBoBaoTriThietBi() => XuLyLoi.ThucHien(() => data.layToanBoBaoTriThietBi());
        /// <summary>
        /// Gọi DAL_BAOTRITHIETBI.layBaoTriThietBiTheoMa với ma để tra bảo trì thiết bị theo khóa; trả ET tương ứng hoặc null nếu
        /// không tìm thấy. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_BAOTRITHIETBI? layBaoTriThietBiTheoMa(string ma) => XuLyLoi.ThucHien(() => data.layBaoTriThietBiTheoMa(ma));
        /// <summary>
        /// Thêm bảo trì thiết bị từ duLieu qua DAL_BAOTRITHIETBI.themBaoTriThietBi; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ
        /// liệu rồi INSERT trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. XuLyLoi chuyển lỗi SQL
        /// sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themBaoTriThietBi(ET_BAOTRITHIETBI duLieu) => XuLyLoi.ThucHien(() => data.themBaoTriThietBi(duLieu));
        /// <summary>
        /// Cập nhật bảo trì thiết bị qua DAL_BAOTRITHIETBI.suaBaoTriThietBi; DAL khóa bản hiện tại, đối chiếu banGoc nếu có,
        /// chuẩn hóa dữ liệu và kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi.
        /// XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaBaoTriThietBi(ET_BAOTRITHIETBI duLieu, ET_BAOTRITHIETBI? banGoc = null, bool chiDoiTrangThai = false)
            => XuLyLoi.ThucHien(() => data.suaBaoTriThietBi(duLieu, banGoc, chiDoiTrangThai));
        /// <summary>
        /// Xóa bảo trì thiết bị có mã ma qua DAL_BAOTRITHIETBI.xoaBaoTriThietBi; DAL khóa bản hiện tại, kiểm tra banGoc nếu có
        /// rồi DELETE. Trả true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi
        /// SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaBaoTriThietBi(string ma, ET_BAOTRITHIETBI? banGoc = null) => XuLyLoi.ThucHien(() => data.xoaBaoTriThietBi(ma, banGoc));

    }
}