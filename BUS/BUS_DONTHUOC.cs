// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ đơn thuốc, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_DONTHUOC. Các lời gọi DAL
    /// được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public partial class BUS_DONTHUOC
    {
        public DonThuocChiTiet LayChiTietCoQuyen(string maDT, NguCanhNghiepVu nguoiDung)
            => XuLyLoi.ThucHien(() => clinical.LayDonThuoc(maDT, nguoiDung));

        private readonly DAL_KHAMDIEUTRI clinical;

        /// <summary>
        /// Đối tượng DAL_DONTHUOC thực hiện truy vấn/ghi đơn thuốc trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_DONTHUOC dalDonThuoc;

        /// <summary>
        /// Khởi tạo DAL_DONTHUOC bằng transaction tùy chọn để các thao tác đơn thuốc dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_DONTHUOC(IDbTransaction? transaction = null)
        {
            dalDonThuoc = new DAL_DONTHUOC(transaction);
            clinical = new DAL_KHAMDIEUTRI(transaction);
        }

        /// <summary>
        /// Gọi DAL_DONTHUOC.layToanBoDonThuoc để đọc toàn bộ đơn thuốc; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể rỗng
        /// khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        public IEnumerable<ET_DONTHUOC> layToanBoDonThuoc()
        {
            return XuLyLoi.ThucHien(() => dalDonThuoc.layToanBoDonThuoc());
        }

        /// <summary>
        /// Gọi DAL_DONTHUOC.layDonThuocTheoMa với ma để tra đơn thuốc theo khóa; trả ET tương ứng hoặc null nếu không tìm thấy.
        /// XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_DONTHUOC? layDonThuocTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalDonThuoc.layDonThuocTheoMa(ma));
        }

        /// <summary>
        /// Thêm đơn thuốc từ duLieu qua DAL_DONTHUOC.themDonThuoc; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ liệu rồi INSERT
        /// trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. Kiểm tra dữ liệu bắt buộc tại BUS trước
        /// khi gọi DAL. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themDonThuoc(ET_DONTHUOC duLieu)
        {
            KiemTraDuLieu(duLieu);
            return XuLyLoi.ThucHien(() => dalDonThuoc.themDonThuoc(duLieu));
        }

        /// <summary>
        /// Cập nhật đơn thuốc qua DAL_DONTHUOC.suaDonThuoc; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn hóa dữ liệu và
        /// kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. Kiểm tra dữ liệu bắt
        /// buộc tại BUS trước khi gọi DAL. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaDonThuoc(ET_DONTHUOC duLieu, ET_DONTHUOC? banGoc = null, bool chiDoiTrangThai = false)
        {
            KiemTraDuLieu(duLieu);
            return XuLyLoi.ThucHien(() => dalDonThuoc.suaDonThuoc(duLieu, banGoc, chiDoiTrangThai));
        }

        /// <summary>
        /// Xóa đơn thuốc có mã ma qua DAL_DONTHUOC.xoaDonThuoc; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi DELETE. Trả
        /// true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang thông
        /// báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaDonThuoc(string ma, ET_DONTHUOC? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalDonThuoc.xoaDonThuoc(ma, banGoc));
        }
        /// <summary>
        /// Gọi DAL_DONTHUOC.BatDauGiaoDich để mở kết nối và tạo giao dịch Serializable dùng lưu đơn cùng chi tiết; trả
        /// IDbTransaction để nơi gọi truyền cho các BUS, tự commit/rollback và giải phóng cả giao dịch lẫn kết nối. Lỗi SQL khi
        /// mở được XuLyLoi chuyển sang thông báo tiếng Việt.
        /// </summary>
        public IDbTransaction BatDauGiaoDich() => XuLyLoi.ThucHien(() => dalDonThuoc.BatDauGiaoDich());
        /// <summary>
        /// Yêu cầu mã lần khám và mã bác sĩ kê đơn. Sai điều kiện sẽ ném InvalidOperationException ngay, ngăn thao tác thêm/sửa
        /// gọi DAL; không ghi dữ liệu.
        /// </summary>
        /// <param name="record">Bản ET được kiểm tra hoặc chuyển vào thao tác cập nhật.</param>
        private static void KiemTraDuLieu(ET_DONTHUOC record)
        {
            if (string.IsNullOrWhiteSpace(record.MaLS)) throw new InvalidOperationException("Vui lòng nhập/chọn MaLS.");
            if (string.IsNullOrWhiteSpace(record.MaBS)) throw new InvalidOperationException("Vui lòng nhập/chọn MaBS.");
        }
    }
}
