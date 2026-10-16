// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ phiếu nhập, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_PHIEUNHAP. Các lời gọi DAL
    /// được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_PHIEUNHAP
    {
        /// <summary>
        /// Đối tượng DAL_PHIEUNHAP thực hiện truy vấn/ghi phiếu nhập trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_PHIEUNHAP data;
        /// <summary>
        /// Khởi tạo DAL_PHIEUNHAP bằng transaction tùy chọn để các thao tác phiếu nhập dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_PHIEUNHAP(IDbTransaction? transaction = null) { data = new(transaction); }
        /// <summary>
        /// Gọi DAL_PHIEUNHAP.layToanBoPhieuNhap để đọc toàn bộ phiếu nhập; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể rỗng
        /// khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        public IEnumerable<ET_PHIEUNHAP> layToanBoPhieuNhap() => XuLyLoi.ThucHien(() => data.layToanBoPhieuNhap());
        /// <summary>
        /// Gọi DAL_PHIEUNHAP.layPhieuNhapTheoMa với ma để tra phiếu nhập theo khóa; trả ET tương ứng hoặc null nếu không tìm
        /// thấy. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_PHIEUNHAP? layPhieuNhapTheoMa(string ma) => XuLyLoi.ThucHien(() => data.layPhieuNhapTheoMa(ma));
        /// <summary>
        /// Thêm phiếu nhập từ duLieu qua DAL_PHIEUNHAP.themPhieuNhap; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ liệu rồi
        /// INSERT trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. XuLyLoi chuyển lỗi SQL sang thông
        /// báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themPhieuNhap(ET_PHIEUNHAP duLieu) => XuLyLoi.ThucHien(() => data.themPhieuNhap(duLieu));
        /// <summary>
        /// Cập nhật phiếu nhập qua DAL_PHIEUNHAP.suaPhieuNhap; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn hóa dữ liệu
        /// và kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. XuLyLoi chuyển lỗi
        /// SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaPhieuNhap(ET_PHIEUNHAP duLieu, ET_PHIEUNHAP? banGoc = null, bool chiDoiTrangThai = false)
            => XuLyLoi.ThucHien(() => data.suaPhieuNhap(duLieu, banGoc, chiDoiTrangThai));
        /// <summary>
        /// Xóa phiếu nhập có mã ma qua DAL_PHIEUNHAP.xoaPhieuNhap; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi DELETE. Trả
        /// true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang thông
        /// báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaPhieuNhap(string ma, ET_PHIEUNHAP? banGoc = null) => XuLyLoi.ThucHien(() => data.xoaPhieuNhap(ma, banGoc));

    }
}