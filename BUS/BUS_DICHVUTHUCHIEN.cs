// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ dịch vụ thực hiện, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_DICHVUTHUCHIEN. Các
    /// lời gọi DAL được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_DICHVUTHUCHIEN
    {
        /// <summary>
        /// Đối tượng DAL_DICHVUTHUCHIEN thực hiện truy vấn/ghi dịch vụ thực hiện trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_DICHVUTHUCHIEN dalDichVuThucHien;

        /// <summary>
        /// Khởi tạo DAL_DICHVUTHUCHIEN bằng transaction tùy chọn để các thao tác dịch vụ thực hiện dùng cùng giao dịch của nơi
        /// gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_DICHVUTHUCHIEN(IDbTransaction? transaction = null)
        {
            dalDichVuThucHien = new DAL_DICHVUTHUCHIEN(transaction);
        }

        /// <summary>
        /// Gọi DAL_DICHVUTHUCHIEN.layToanBoDichVuThucHien để đọc toàn bộ dịch vụ thực hiện; trả danh sách ET do DAL đã tải vào bộ
        /// nhớ, có thể rỗng khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong
        /// InnerException.
        /// </summary>
        public IEnumerable<ET_DICHVUTHUCHIEN> layToanBoDichVuThucHien()
        {
            return XuLyLoi.ThucHien(() => dalDichVuThucHien.layToanBoDichVuThucHien());
        }

        /// <summary>
        /// Gọi DAL_DICHVUTHUCHIEN.layDichVuThucHienTheoMa với ma để tra dịch vụ thực hiện theo khóa; trả ET tương ứng hoặc null
        /// nếu không tìm thấy. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_DICHVUTHUCHIEN? layDichVuThucHienTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalDichVuThucHien.layDichVuThucHienTheoMa(ma));
        }

        /// <summary>
        /// Thêm dịch vụ thực hiện từ duLieu qua DAL_DICHVUTHUCHIEN.themDichVuThucHien; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra
        /// dữ liệu rồi INSERT trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. Kiểm tra dữ liệu bắt
        /// buộc tại BUS trước khi gọi DAL. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themDichVuThucHien(ET_DICHVUTHUCHIEN duLieu)
        {
            KiemTraDuLieu(duLieu);
            return XuLyLoi.ThucHien(() => dalDichVuThucHien.themDichVuThucHien(duLieu));
        }

        /// <summary>
        /// Cập nhật dịch vụ thực hiện qua DAL_DICHVUTHUCHIEN.suaDichVuThucHien; DAL khóa bản hiện tại, đối chiếu banGoc nếu có,
        /// chuẩn hóa dữ liệu và kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi.
        /// Kiểm tra dữ liệu bắt buộc tại BUS trước khi gọi DAL. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc
        /// trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaDichVuThucHien(ET_DICHVUTHUCHIEN duLieu, ET_DICHVUTHUCHIEN? banGoc = null, bool chiDoiTrangThai = false)
        {
            KiemTraDuLieu(duLieu);
            return XuLyLoi.ThucHien(() => dalDichVuThucHien.suaDichVuThucHien(duLieu, banGoc, chiDoiTrangThai));
        }

        /// <summary>
        /// Xóa dịch vụ thực hiện có mã ma qua DAL_DICHVUTHUCHIEN.xoaDichVuThucHien; DAL khóa bản hiện tại, kiểm tra banGoc nếu có
        /// rồi DELETE. Trả true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi
        /// SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaDichVuThucHien(string ma, ET_DICHVUTHUCHIEN? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalDichVuThucHien.xoaDichVuThucHien(ma, banGoc));
        }
        /// <summary>
        /// Yêu cầu mã lần khám, mã dịch vụ và mã nhân viên thực hiện; số lượng phải dương, đơn giá không âm. Sai điều kiện sẽ ném
        /// InvalidOperationException ngay, ngăn thao tác thêm/sửa gọi DAL; không ghi dữ liệu.
        /// </summary>
        /// <param name="record">Bản ET được kiểm tra hoặc chuyển vào thao tác cập nhật.</param>
        private static void KiemTraDuLieu(ET_DICHVUTHUCHIEN record)
        {
            if (string.IsNullOrWhiteSpace(record.MaLS)) throw new InvalidOperationException("Vui lòng nhập/chọn MaLS.");
            if (string.IsNullOrWhiteSpace(record.MaDV)) throw new InvalidOperationException("Vui lòng nhập/chọn MaDV.");
            if (string.IsNullOrWhiteSpace(record.MaNVThucHien)) throw new InvalidOperationException("Vui lòng nhập/chọn MaNVThucHien.");
            if (record.SoLuong <= 0 || record.DonGia < 0) throw new InvalidOperationException("Số lượng phải dương, đơn giá không âm.");
        }
    }
}
