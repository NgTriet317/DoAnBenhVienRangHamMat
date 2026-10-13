// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ lô hàng hóa, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_LOHANGHOA. Các lời gọi DAL
    /// được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_LOHANGHOA
    {
        /// <summary>
        /// Đối tượng DAL_LOHANGHOA thực hiện truy vấn/ghi lô hàng hóa trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_LOHANGHOA dalLoHangHoa;

        /// <summary>
        /// Khởi tạo DAL_LOHANGHOA bằng transaction tùy chọn để các thao tác lô hàng hóa dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_LOHANGHOA(IDbTransaction? transaction = null)
        {
            dalLoHangHoa = new DAL_LOHANGHOA(transaction);
        }

        /// <summary>
        /// Gọi DAL_LOHANGHOA.layToanBoLoHangHoa để đọc toàn bộ lô hàng hóa; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể
        /// rỗng khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        public IEnumerable<ET_LOHANGHOA> layToanBoLoHangHoa()
        {
            return XuLyLoi.ThucHien(() => dalLoHangHoa.layToanBoLoHangHoa());
        }

        /// <summary>
        /// Gọi DAL_LOHANGHOA.layLoHangHoaTheoMa với ma để tra lô hàng hóa theo khóa; trả ET tương ứng hoặc null nếu không tìm
        /// thấy. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_LOHANGHOA? layLoHangHoaTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalLoHangHoa.layLoHangHoaTheoMa(ma));
        }

        /// <summary>
        /// Thêm lô hàng hóa từ duLieu qua DAL_LOHANGHOA.themLoHangHoa; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ liệu rồi
        /// INSERT trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. XuLyLoi chuyển lỗi SQL sang thông
        /// báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themLoHangHoa(ET_LOHANGHOA duLieu)
        {
            return XuLyLoi.ThucHien(() => dalLoHangHoa.themLoHangHoa(duLieu));
        }

        /// <summary>
        /// Cập nhật lô hàng hóa qua DAL_LOHANGHOA.suaLoHangHoa; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn hóa dữ liệu
        /// và kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. XuLyLoi chuyển lỗi
        /// SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaLoHangHoa(ET_LOHANGHOA duLieu, ET_LOHANGHOA? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalLoHangHoa.suaLoHangHoa(duLieu, banGoc, chiDoiTrangThai));
        }

        /// <summary>
        /// Xóa lô hàng hóa có mã ma qua DAL_LOHANGHOA.xoaLoHangHoa; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi DELETE. Trả
        /// true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang thông
        /// báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaLoHangHoa(string ma, ET_LOHANGHOA? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalLoHangHoa.xoaLoHangHoa(ma, banGoc));
        }
    }
}
