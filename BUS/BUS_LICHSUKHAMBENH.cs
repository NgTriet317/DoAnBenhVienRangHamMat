// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ lịch sử khám bệnh, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_LICHSUKHAMBENH. Các
    /// lời gọi DAL được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_LICHSUKHAMBENH
    {
        /// <summary>
        /// Đối tượng DAL_LICHSUKHAMBENH thực hiện truy vấn/ghi lịch sử khám bệnh trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_LICHSUKHAMBENH dalLichSuKhamBenh;

        /// <summary>
        /// Khởi tạo DAL_LICHSUKHAMBENH bằng transaction tùy chọn để các thao tác lịch sử khám bệnh dùng cùng giao dịch của nơi
        /// gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_LICHSUKHAMBENH(IDbTransaction? transaction = null)
        {
            dalLichSuKhamBenh = new DAL_LICHSUKHAMBENH(transaction);
        }

        /// <summary>
        /// Gọi DAL_LICHSUKHAMBENH.layToanBoLichSuKhamBenh để đọc toàn bộ lịch sử khám bệnh; trả danh sách ET do DAL đã tải vào bộ
        /// nhớ, có thể rỗng khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong
        /// InnerException.
        /// </summary>
        public IEnumerable<ET_LICHSUKHAMBENH> layToanBoLichSuKhamBenh()
        {
            return XuLyLoi.ThucHien(() => dalLichSuKhamBenh.layToanBoLichSuKhamBenh());
        }

        /// <summary>
        /// Gọi DAL_LICHSUKHAMBENH.layLichSuKhamBenhTheoMa với ma để tra lịch sử khám bệnh theo khóa; trả ET tương ứng hoặc null
        /// nếu không tìm thấy. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_LICHSUKHAMBENH? layLichSuKhamBenhTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalLichSuKhamBenh.layLichSuKhamBenhTheoMa(ma));
        }

        /// <summary>
        /// Thêm lịch sử khám bệnh từ duLieu qua DAL_LICHSUKHAMBENH.themLichSuKhamBenh; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra
        /// dữ liệu rồi INSERT trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. Kiểm tra dữ liệu bắt
        /// buộc tại BUS trước khi gọi DAL. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themLichSuKhamBenh(ET_LICHSUKHAMBENH duLieu)
        {
            KiemTraDuLieu(duLieu);
            return XuLyLoi.ThucHien(() => dalLichSuKhamBenh.themLichSuKhamBenh(duLieu));
        }

        /// <summary>
        /// Cập nhật lịch sử khám bệnh qua DAL_LICHSUKHAMBENH.suaLichSuKhamBenh; DAL khóa bản hiện tại, đối chiếu banGoc nếu có,
        /// chuẩn hóa dữ liệu và kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi.
        /// Kiểm tra dữ liệu bắt buộc tại BUS trước khi gọi DAL. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc
        /// trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaLichSuKhamBenh(ET_LICHSUKHAMBENH duLieu, ET_LICHSUKHAMBENH? banGoc = null, bool chiDoiTrangThai = false)
        {
            KiemTraDuLieu(duLieu);
            return XuLyLoi.ThucHien(() => dalLichSuKhamBenh.suaLichSuKhamBenh(duLieu, banGoc, chiDoiTrangThai));
        }

        /// <summary>
        /// Xóa lịch sử khám bệnh có mã ma qua DAL_LICHSUKHAMBENH.xoaLichSuKhamBenh; DAL khóa bản hiện tại, kiểm tra banGoc nếu có
        /// rồi DELETE. Trả true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi
        /// SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaLichSuKhamBenh(string ma, ET_LICHSUKHAMBENH? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalLichSuKhamBenh.xoaLichSuKhamBenh(ma, banGoc));
        }
        /// <summary>
        /// Yêu cầu mã phiếu khám, mã bác sĩ và mã phòng. Sai điều kiện sẽ ném InvalidOperationException ngay, ngăn thao tác
        /// thêm/sửa gọi DAL; không ghi dữ liệu.
        /// </summary>
        /// <param name="record">Bản ET được kiểm tra hoặc chuyển vào thao tác cập nhật.</param>
        private static void KiemTraDuLieu(ET_LICHSUKHAMBENH record)
        {
            if (string.IsNullOrWhiteSpace(record.MaPhieu)) throw new InvalidOperationException("Vui lòng nhập/chọn MaPhieu.");
            if (string.IsNullOrWhiteSpace(record.MaBS)) throw new InvalidOperationException("Vui lòng nhập/chọn MaBS.");
            if (string.IsNullOrWhiteSpace(record.MaPhong)) throw new InvalidOperationException("Vui lòng nhập/chọn MaPhong.");
        }
    }
}
