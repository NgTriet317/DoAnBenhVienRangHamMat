// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ phiếu khám bệnh, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_PHIEUKHAMBENH. Các lời
    /// gọi DAL được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_PHIEUKHAMBENH
    {
        /// <summary>
        /// Đối tượng DAL_PHIEUKHAMBENH thực hiện truy vấn/ghi phiếu khám bệnh trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_PHIEUKHAMBENH dalPhieuKhamBenh;

        /// <summary>
        /// Khởi tạo DAL_PHIEUKHAMBENH bằng transaction tùy chọn để các thao tác phiếu khám bệnh dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_PHIEUKHAMBENH(IDbTransaction? transaction = null)
        {
            dalPhieuKhamBenh = new DAL_PHIEUKHAMBENH(transaction);
        }

        /// <summary>
        /// Gọi DAL_PHIEUKHAMBENH.layToanBoPhieuKhamBenh để đọc toàn bộ phiếu khám bệnh; trả danh sách ET do DAL đã tải vào bộ
        /// nhớ, có thể rỗng khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong
        /// InnerException.
        /// </summary>
        public IEnumerable<ET_PHIEUKHAMBENH> layToanBoPhieuKhamBenh()
        {
            return XuLyLoi.ThucHien(() => dalPhieuKhamBenh.layToanBoPhieuKhamBenh());
        }

        /// <summary>
        /// Gọi DAL_PHIEUKHAMBENH.layPhieuKhamBenhTheoMa với ma để tra phiếu khám bệnh theo khóa; trả ET tương ứng hoặc null nếu
        /// không tìm thấy. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_PHIEUKHAMBENH? layPhieuKhamBenhTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalPhieuKhamBenh.layPhieuKhamBenhTheoMa(ma));
        }

        /// <summary>
        /// Thêm phiếu khám bệnh từ duLieu qua DAL_PHIEUKHAMBENH.themPhieuKhamBenh; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ
        /// liệu rồi INSERT trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. Kiểm tra dữ liệu bắt buộc
        /// tại BUS trước khi gọi DAL. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themPhieuKhamBenh(ET_PHIEUKHAMBENH duLieu)
        {
            KiemTraDuLieu(duLieu);
            return XuLyLoi.ThucHien(() => dalPhieuKhamBenh.themPhieuKhamBenh(duLieu));
        }

        /// <summary>
        /// Cập nhật phiếu khám bệnh qua DAL_PHIEUKHAMBENH.suaPhieuKhamBenh; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn
        /// hóa dữ liệu và kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. Kiểm
        /// tra dữ liệu bắt buộc tại BUS trước khi gọi DAL. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong
        /// InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaPhieuKhamBenh(ET_PHIEUKHAMBENH duLieu, ET_PHIEUKHAMBENH? banGoc = null, bool chiDoiTrangThai = false)
        {
            KiemTraDuLieu(duLieu);
            return XuLyLoi.ThucHien(() => dalPhieuKhamBenh.suaPhieuKhamBenh(duLieu, banGoc, chiDoiTrangThai));
        }

        /// <summary>
        /// Xóa phiếu khám bệnh có mã ma qua DAL_PHIEUKHAMBENH.xoaPhieuKhamBenh; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi
        /// DELETE. Trả true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL
        /// sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaPhieuKhamBenh(string ma, ET_PHIEUKHAMBENH? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalPhieuKhamBenh.xoaPhieuKhamBenh(ma, banGoc));
        }
        /// <summary>
        /// Yêu cầu mã hồ sơ, mã phòng, mã nhân viên tiếp nhận và lý do khám. Sai điều kiện sẽ ném InvalidOperationException ngay,
        /// ngăn thao tác thêm/sửa gọi DAL; không ghi dữ liệu.
        /// </summary>
        /// <param name="record">Bản ET được kiểm tra hoặc chuyển vào thao tác cập nhật.</param>
        private static void KiemTraDuLieu(ET_PHIEUKHAMBENH record)
        {
            if (string.IsNullOrWhiteSpace(record.MaHS)) throw new InvalidOperationException("Vui lòng nhập/chọn MaHS.");
            if (string.IsNullOrWhiteSpace(record.MaPhong)) throw new InvalidOperationException("Vui lòng nhập/chọn MaPhong.");
            if (string.IsNullOrWhiteSpace(record.MaNVTiepNhan)) throw new InvalidOperationException("Vui lòng nhập/chọn MaNVTiepNhan.");
            if (string.IsNullOrWhiteSpace(record.LyDoKham)) throw new InvalidOperationException("Vui lòng nhập/chọn LyDoKham.");
        }
    }
}
