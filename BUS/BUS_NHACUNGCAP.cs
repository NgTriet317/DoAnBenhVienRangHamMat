// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ nhà cung cấp, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_NHACUNGCAP. Các lời gọi
    /// DAL được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_NHACUNGCAP
    {
        /// <summary>
        /// Đối tượng DAL_NHACUNGCAP thực hiện truy vấn/ghi nhà cung cấp trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_NHACUNGCAP data;
        /// <summary>
        /// Khởi tạo DAL_NHACUNGCAP bằng transaction tùy chọn để các thao tác nhà cung cấp dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_NHACUNGCAP(IDbTransaction? transaction = null) { data = new(transaction); }
        /// <summary>
        /// Gọi DAL_NHACUNGCAP.layToanBoNhaCungCap để đọc toàn bộ nhà cung cấp; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể
        /// rỗng khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        public IEnumerable<ET_NHACUNGCAP> layToanBoNhaCungCap() => XuLyLoi.ThucHien(() => data.layToanBoNhaCungCap());
        /// <summary>
        /// Gọi DAL_NHACUNGCAP.layNhaCungCapTheoMa với ma để tra nhà cung cấp theo khóa; trả ET tương ứng hoặc null nếu không tìm
        /// thấy. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_NHACUNGCAP? layNhaCungCapTheoMa(string ma) => XuLyLoi.ThucHien(() => data.layNhaCungCapTheoMa(ma));
        /// <summary>
        /// Thêm nhà cung cấp từ duLieu qua DAL_NHACUNGCAP.themNhaCungCap; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ liệu rồi
        /// INSERT trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. XuLyLoi chuyển lỗi SQL sang thông
        /// báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themNhaCungCap(ET_NHACUNGCAP duLieu) => XuLyLoi.ThucHien(() => data.themNhaCungCap(duLieu));
        /// <summary>
        /// Cập nhật nhà cung cấp qua DAL_NHACUNGCAP.suaNhaCungCap; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn hóa dữ
        /// liệu và kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. XuLyLoi
        /// chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaNhaCungCap(ET_NHACUNGCAP duLieu, ET_NHACUNGCAP? banGoc = null, bool chiDoiTrangThai = false)
            => XuLyLoi.ThucHien(() => data.suaNhaCungCap(duLieu, banGoc, chiDoiTrangThai));
        /// <summary>
        /// Xóa nhà cung cấp có mã ma qua DAL_NHACUNGCAP.xoaNhaCungCap; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi DELETE.
        /// Trả true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang
        /// thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaNhaCungCap(string ma, ET_NHACUNGCAP? banGoc = null) => XuLyLoi.ThucHien(() => data.xoaNhaCungCap(ma, banGoc));

    }
}