// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ bệnh nhân, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_BENHNHAN. Các lời gọi DAL
    /// được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_BENHNHAN
    {
        /// <summary>
        /// Đối tượng DAL_BENHNHAN thực hiện truy vấn/ghi bệnh nhân trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_BENHNHAN dalBenhNhan;

        /// <summary>
        /// Khởi tạo DAL_BENHNHAN bằng transaction tùy chọn để các thao tác bệnh nhân dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_BENHNHAN(IDbTransaction? transaction = null)
        {
            dalBenhNhan = new DAL_BENHNHAN(transaction);
        }

        /// <summary>
        /// Gọi DAL_BENHNHAN.layToanBoBenhNhan để đọc toàn bộ bệnh nhân; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể rỗng
        /// khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        public IEnumerable<ET_BENHNHAN> layToanBoBenhNhan()
        {
            return XuLyLoi.ThucHien(() => dalBenhNhan.layToanBoBenhNhan());
        }

        /// <summary>
        /// Gọi DAL_BENHNHAN.layBenhNhanTheoMa với ma để tra bệnh nhân theo khóa; trả ET tương ứng hoặc null nếu không tìm thấy.
        /// XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_BENHNHAN? layBenhNhanTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalBenhNhan.layBenhNhanTheoMa(ma));
        }

        /// <summary>
        /// Thêm bệnh nhân từ duLieu qua DAL_BENHNHAN.themBenhNhan; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ liệu rồi INSERT
        /// trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. Kiểm tra dữ liệu bắt buộc tại BUS trước
        /// khi gọi DAL. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themBenhNhan(ET_BENHNHAN duLieu)
        {
            KiemTraDuLieu(duLieu);
            return XuLyLoi.ThucHien(() => dalBenhNhan.themBenhNhan(duLieu));
        }

        /// <summary>
        /// Cập nhật bệnh nhân qua DAL_BENHNHAN.suaBenhNhan; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn hóa dữ liệu và
        /// kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. Kiểm tra dữ liệu bắt
        /// buộc tại BUS trước khi gọi DAL. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaBenhNhan(ET_BENHNHAN duLieu, ET_BENHNHAN? banGoc = null, bool chiDoiTrangThai = false)
        {
            KiemTraDuLieu(duLieu);
            return XuLyLoi.ThucHien(() => dalBenhNhan.suaBenhNhan(duLieu, banGoc, chiDoiTrangThai));
        }

        /// <summary>
        /// Xóa bệnh nhân có mã ma qua DAL_BENHNHAN.xoaBenhNhan; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi DELETE. Trả
        /// true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang thông
        /// báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaBenhNhan(string ma, ET_BENHNHAN? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalBenhNhan.xoaBenhNhan(ma, banGoc));
        }
        /// <summary>
        /// Yêu cầu họ tên và trạng thái; ngày sinh không được sau hôm nay. Sai điều kiện sẽ ném InvalidOperationException ngay,
        /// ngăn thao tác thêm/sửa gọi DAL; không ghi dữ liệu.
        /// </summary>
        /// <param name="record">Bản ET được kiểm tra hoặc chuyển vào thao tác cập nhật.</param>
        private static void KiemTraDuLieu(ET_BENHNHAN record)
        {
            if (string.IsNullOrWhiteSpace(record.HoTenBN)) throw new InvalidOperationException("Vui lòng nhập/chọn HoTenBN.");
            if (string.IsNullOrWhiteSpace(record.TrangThai)) throw new InvalidOperationException("Vui lòng nhập/chọn TrangThai.");
            if (record.NgaySinh > DateTime.Today) throw new InvalidOperationException("Ngày sinh không thể sau hôm nay.");
        }
    }
}
