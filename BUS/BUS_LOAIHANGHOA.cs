// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ loại hàng hóa, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_LOAIHANGHOA. Các lời gọi
    /// DAL được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_LOAIHANGHOA
    {
        /// <summary>
        /// Đối tượng DAL_LOAIHANGHOA thực hiện truy vấn/ghi loại hàng hóa trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_LOAIHANGHOA data;
        /// <summary>
        /// Khởi tạo DAL_LOAIHANGHOA bằng transaction tùy chọn để các thao tác loại hàng hóa dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_LOAIHANGHOA(IDbTransaction? transaction = null) { data = new(transaction); }
        /// <summary>
        /// Gọi DAL_LOAIHANGHOA.layToanBoLoaiHangHoa để đọc toàn bộ loại hàng hóa; trả danh sách ET do DAL đã tải vào bộ nhớ, có
        /// thể rỗng khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        public IEnumerable<ET_LOAIHANGHOA> layToanBoLoaiHangHoa() => XuLyLoi.ThucHien(() => data.layToanBoLoaiHangHoa());
        /// <summary>
        /// Gọi DAL_LOAIHANGHOA.layLoaiHangHoaTheoMa với ma để tra loại hàng hóa theo khóa; trả ET tương ứng hoặc null nếu không
        /// tìm thấy. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_LOAIHANGHOA? layLoaiHangHoaTheoMa(string ma) => XuLyLoi.ThucHien(() => data.layLoaiHangHoaTheoMa(ma));
        /// <summary>
        /// Thêm loại hàng hóa từ duLieu qua DAL_LOAIHANGHOA.themLoaiHangHoa; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ liệu
        /// rồi INSERT trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. XuLyLoi chuyển lỗi SQL sang
        /// thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themLoaiHangHoa(ET_LOAIHANGHOA duLieu) => XuLyLoi.ThucHien(() => data.themLoaiHangHoa(duLieu));
        /// <summary>
        /// Cập nhật loại hàng hóa qua DAL_LOAIHANGHOA.suaLoaiHangHoa; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn hóa
        /// dữ liệu và kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. XuLyLoi
        /// chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaLoaiHangHoa(ET_LOAIHANGHOA duLieu, ET_LOAIHANGHOA? banGoc = null, bool chiDoiTrangThai = false)
            => XuLyLoi.ThucHien(() => data.suaLoaiHangHoa(duLieu, banGoc, chiDoiTrangThai));
        /// <summary>
        /// Xóa loại hàng hóa có mã ma qua DAL_LOAIHANGHOA.xoaLoaiHangHoa; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi
        /// DELETE. Trả true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL
        /// sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaLoaiHangHoa(string ma, ET_LOAIHANGHOA? banGoc = null) => XuLyLoi.ThucHien(() => data.xoaLoaiHangHoa(ma, banGoc));

    }
}