// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ chi tiết đơn thuốc, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_CHITIETDONTHUOC. Các
    /// lời gọi DAL được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_CHITIETDONTHUOC
    {
        /// <summary>
        /// Đối tượng DAL_CHITIETDONTHUOC thực hiện truy vấn/ghi chi tiết đơn thuốc trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_CHITIETDONTHUOC dalChiTietDonThuoc;

        /// <summary>
        /// Khởi tạo DAL_CHITIETDONTHUOC bằng transaction tùy chọn để các thao tác chi tiết đơn thuốc dùng cùng giao dịch của nơi
        /// gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_CHITIETDONTHUOC(IDbTransaction? transaction = null)
        {
            dalChiTietDonThuoc = new DAL_CHITIETDONTHUOC(transaction);
        }

        /// <summary>
        /// Gọi DAL_CHITIETDONTHUOC.layToanBoChiTietDonThuoc để đọc toàn bộ chi tiết đơn thuốc; trả danh sách ET do DAL đã tải vào
        /// bộ nhớ, có thể rỗng khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong
        /// InnerException.
        /// </summary>
        public IEnumerable<ET_CHITIETDONTHUOC> layToanBoChiTietDonThuoc()
        {
            return XuLyLoi.ThucHien(() => dalChiTietDonThuoc.layToanBoChiTietDonThuoc());
        }

        /// <summary>
        /// Gọi DAL_CHITIETDONTHUOC.layChiTietDonThuocTheoMa với ma để tra chi tiết đơn thuốc theo khóa; trả ET tương ứng hoặc
        /// null nếu không tìm thấy. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_CHITIETDONTHUOC? layChiTietDonThuocTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalChiTietDonThuoc.layChiTietDonThuocTheoMa(ma));
        }

        /// <summary>
        /// Thêm chi tiết đơn thuốc từ duLieu qua DAL_CHITIETDONTHUOC.themChiTietDonThuoc; DAL cấp mã mới vào ET, chuẩn hóa/kiểm
        /// tra dữ liệu rồi INSERT trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. Kiểm tra dữ liệu
        /// bắt buộc tại BUS trước khi gọi DAL. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong
        /// InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themChiTietDonThuoc(ET_CHITIETDONTHUOC duLieu)
        {
            KiemTraDuLieu(duLieu);
            return XuLyLoi.ThucHien(() => dalChiTietDonThuoc.themChiTietDonThuoc(duLieu));
        }

        /// <summary>
        /// Cập nhật chi tiết đơn thuốc qua DAL_CHITIETDONTHUOC.suaChiTietDonThuoc; DAL khóa bản hiện tại, đối chiếu banGoc nếu
        /// có, chuẩn hóa dữ liệu và kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát
        /// lỗi. Kiểm tra dữ liệu bắt buộc tại BUS trước khi gọi DAL. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi
        /// gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaChiTietDonThuoc(ET_CHITIETDONTHUOC duLieu, ET_CHITIETDONTHUOC? banGoc = null, bool chiDoiTrangThai = false)
        {
            KiemTraDuLieu(duLieu);
            return XuLyLoi.ThucHien(() => dalChiTietDonThuoc.suaChiTietDonThuoc(duLieu, banGoc, chiDoiTrangThai));
        }

        /// <summary>
        /// Xóa chi tiết đơn thuốc có mã ma qua DAL_CHITIETDONTHUOC.xoaChiTietDonThuoc; DAL khóa bản hiện tại, kiểm tra banGoc nếu
        /// có rồi DELETE. Trả true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển
        /// lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaChiTietDonThuoc(string ma, ET_CHITIETDONTHUOC? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalChiTietDonThuoc.xoaChiTietDonThuoc(ma, banGoc));
        }
        /// <summary>
        /// Yêu cầu mã đơn thuốc, mã thuốc, liều dùng và hướng dẫn; số lượng và số ngày dùng phải lớn hơn 0. Sai điều kiện sẽ ném
        /// InvalidOperationException ngay, ngăn thao tác thêm/sửa gọi DAL; không ghi dữ liệu.
        /// </summary>
        /// <param name="record">Bản ET được kiểm tra hoặc chuyển vào thao tác cập nhật.</param>
        private static void KiemTraDuLieu(ET_CHITIETDONTHUOC record)
        {
            if (string.IsNullOrWhiteSpace(record.MaDT)) throw new InvalidOperationException("Vui lòng nhập/chọn MaDT.");
            if (string.IsNullOrWhiteSpace(record.MaThuoc)) throw new InvalidOperationException("Vui lòng nhập/chọn MaThuoc.");
            if (string.IsNullOrWhiteSpace(record.LieuDung)) throw new InvalidOperationException("Vui lòng nhập/chọn LieuDung.");
            if (string.IsNullOrWhiteSpace(record.HuongDan)) throw new InvalidOperationException("Vui lòng nhập/chọn HuongDan.");
            if (record.SoLuong <= 0 || record.SoNgayDung <= 0) throw new InvalidOperationException("Số lượng và số ngày dùng phải dương.");
        }
    }
}
