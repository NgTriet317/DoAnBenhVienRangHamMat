// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ chi tiết kế hoạch điều trị, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến
    /// DAL_CHITIETKEHOACH. Các lời gọi DAL được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi
    /// nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_CHITIETKEHOACH
    {
        /// <summary>
        /// Đối tượng DAL_CHITIETKEHOACH thực hiện truy vấn/ghi chi tiết kế hoạch điều trị trong giao dịch được truyền vào
        /// constructor.
        /// </summary>
        private readonly DAL_CHITIETKEHOACH dalChiTietKeHoach;

        /// <summary>
        /// Khởi tạo DAL_CHITIETKEHOACH bằng transaction tùy chọn để các thao tác chi tiết kế hoạch điều trị dùng cùng giao dịch
        /// của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_CHITIETKEHOACH(IDbTransaction? transaction = null)
        {
            dalChiTietKeHoach = new DAL_CHITIETKEHOACH(transaction);
        }

        /// <summary>
        /// Gọi DAL_CHITIETKEHOACH.layToanBoChiTietKeHoach để đọc toàn bộ chi tiết kế hoạch điều trị; trả danh sách ET do DAL đã
        /// tải vào bộ nhớ, có thể rỗng khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong
        /// InnerException.
        /// </summary>
        public IEnumerable<ET_CHITIETKEHOACH> layToanBoChiTietKeHoach()
        {
            return XuLyLoi.ThucHien(() => dalChiTietKeHoach.layToanBoChiTietKeHoach());
        }

        /// <summary>
        /// Gọi DAL_CHITIETKEHOACH.layChiTietKeHoachTheoMa với ma để tra chi tiết kế hoạch điều trị theo khóa; trả ET tương ứng
        /// hoặc null nếu không tìm thấy. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_CHITIETKEHOACH? layChiTietKeHoachTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalChiTietKeHoach.layChiTietKeHoachTheoMa(ma));
        }

        /// <summary>
        /// Thêm chi tiết kế hoạch điều trị từ duLieu qua DAL_CHITIETKEHOACH.themChiTietKeHoach; DAL cấp mã mới vào ET, chuẩn
        /// hóa/kiểm tra dữ liệu rồi INSERT trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. Kiểm tra
        /// dữ liệu bắt buộc tại BUS trước khi gọi DAL. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong
        /// InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themChiTietKeHoach(ET_CHITIETKEHOACH duLieu)
        {
            KiemTraDuLieu(duLieu);
            return XuLyLoi.ThucHien(() => dalChiTietKeHoach.themChiTietKeHoach(duLieu));
        }

        /// <summary>
        /// Cập nhật chi tiết kế hoạch điều trị qua DAL_CHITIETKEHOACH.suaChiTietKeHoach; DAL khóa bản hiện tại, đối chiếu banGoc
        /// nếu có, chuẩn hóa dữ liệu và kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ
        /// phát lỗi. Kiểm tra dữ liệu bắt buộc tại BUS trước khi gọi DAL. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ
        /// lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaChiTietKeHoach(ET_CHITIETKEHOACH duLieu, ET_CHITIETKEHOACH? banGoc = null, bool chiDoiTrangThai = false)
        {
            KiemTraDuLieu(duLieu);
            return XuLyLoi.ThucHien(() => dalChiTietKeHoach.suaChiTietKeHoach(duLieu, banGoc, chiDoiTrangThai));
        }

        /// <summary>
        /// Xóa chi tiết kế hoạch điều trị có mã ma qua DAL_CHITIETKEHOACH.xoaChiTietKeHoach; DAL khóa bản hiện tại, kiểm tra
        /// banGoc nếu có rồi DELETE. Trả true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi.
        /// XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaChiTietKeHoach(string ma, ET_CHITIETKEHOACH? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalChiTietKeHoach.xoaChiTietKeHoach(ma, banGoc));
        }
        /// <summary>
        /// Yêu cầu mã kế hoạch và mã dịch vụ; số lượng dự kiến phải dương, đơn giá dự kiến không âm. Sai điều kiện sẽ ném
        /// InvalidOperationException ngay, ngăn thao tác thêm/sửa gọi DAL; không ghi dữ liệu.
        /// </summary>
        /// <param name="record">Bản ET được kiểm tra hoặc chuyển vào thao tác cập nhật.</param>
        private static void KiemTraDuLieu(ET_CHITIETKEHOACH record)
        {
            if (string.IsNullOrWhiteSpace(record.MaKHDT)) throw new InvalidOperationException("Vui lòng nhập/chọn MaKHDT.");
            if (string.IsNullOrWhiteSpace(record.MaDV)) throw new InvalidOperationException("Vui lòng nhập/chọn MaDV.");
            if (record.SoLuongDuKien <= 0 || record.DonGiaDuKien < 0) throw new InvalidOperationException("Số lượng phải dương, đơn giá không âm.");
        }
    }
}
