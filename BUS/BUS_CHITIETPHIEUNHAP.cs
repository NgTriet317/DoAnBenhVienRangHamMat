// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ chi tiết phiếu nhập, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_CHITIETPHIEUNHAP.
    /// Các lời gọi DAL được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ
    /// nguyên.
    /// </summary>
    public class BUS_CHITIETPHIEUNHAP
    {
        /// <summary>
        /// Đối tượng DAL_CHITIETPHIEUNHAP thực hiện truy vấn/ghi chi tiết phiếu nhập trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_CHITIETPHIEUNHAP data;
        /// <summary>
        /// Khởi tạo DAL_CHITIETPHIEUNHAP bằng transaction tùy chọn để các thao tác chi tiết phiếu nhập dùng cùng giao dịch của
        /// nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_CHITIETPHIEUNHAP(IDbTransaction? transaction = null) { data = new(transaction); }
        /// <summary>
        /// Gọi DAL_CHITIETPHIEUNHAP.layToanBoChiTietPhieuNhap để đọc toàn bộ chi tiết phiếu nhập; trả danh sách ET do DAL đã tải
        /// vào bộ nhớ, có thể rỗng khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong
        /// InnerException.
        /// </summary>
        public IEnumerable<ET_CHITIETPHIEUNHAP> layToanBoChiTietPhieuNhap() => XuLyLoi.ThucHien(() => data.layToanBoChiTietPhieuNhap());
        /// <summary>
        /// Gọi DAL_CHITIETPHIEUNHAP.layChiTietPhieuNhapTheoMa với ma để tra chi tiết phiếu nhập theo khóa; trả ET tương ứng hoặc
        /// null nếu không tìm thấy. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_CHITIETPHIEUNHAP? layChiTietPhieuNhapTheoMa(string ma) => XuLyLoi.ThucHien(() => data.layChiTietPhieuNhapTheoMa(ma));
        /// <summary>
        /// Thêm chi tiết phiếu nhập từ duLieu qua DAL_CHITIETPHIEUNHAP.themChiTietPhieuNhap; DAL cấp mã mới vào ET, chuẩn
        /// hóa/kiểm tra dữ liệu rồi INSERT trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. XuLyLoi
        /// chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themChiTietPhieuNhap(ET_CHITIETPHIEUNHAP duLieu) => XuLyLoi.ThucHien(() => data.themChiTietPhieuNhap(duLieu));
        /// <summary>
        /// Cập nhật chi tiết phiếu nhập qua DAL_CHITIETPHIEUNHAP.suaChiTietPhieuNhap; DAL khóa bản hiện tại, đối chiếu banGoc nếu
        /// có, chuẩn hóa dữ liệu và kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát
        /// lỗi. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaChiTietPhieuNhap(ET_CHITIETPHIEUNHAP duLieu, ET_CHITIETPHIEUNHAP? banGoc = null, bool chiDoiTrangThai = false)
            => XuLyLoi.ThucHien(() => data.suaChiTietPhieuNhap(duLieu, banGoc, chiDoiTrangThai));
        /// <summary>
        /// Xóa chi tiết phiếu nhập có mã ma qua DAL_CHITIETPHIEUNHAP.xoaChiTietPhieuNhap; DAL khóa bản hiện tại, kiểm tra banGoc
        /// nếu có rồi DELETE. Trả true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi
        /// chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaChiTietPhieuNhap(string ma, ET_CHITIETPHIEUNHAP? banGoc = null) => XuLyLoi.ThucHien(() => data.xoaChiTietPhieuNhap(ma, banGoc));

    }
}