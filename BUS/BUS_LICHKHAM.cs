// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ lịch khám, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_LICHKHAM. Các lời gọi DAL
    /// được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_LICHKHAM
    {
        /// <summary>
        /// Đối tượng DAL_LICHKHAM thực hiện truy vấn/ghi lịch khám trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_LICHKHAM dalLichKham;

        /// <summary>
        /// Khởi tạo DAL_LICHKHAM bằng transaction tùy chọn để các thao tác lịch khám dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_LICHKHAM(IDbTransaction? transaction = null)
        {
            dalLichKham = new DAL_LICHKHAM(transaction);
        }

        /// <summary>
        /// Gọi DAL_LICHKHAM.layToanBoLichKham để đọc toàn bộ lịch khám; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể rỗng
        /// khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        public IEnumerable<ET_LICHKHAM> layToanBoLichKham()
        {
            return XuLyLoi.ThucHien(() => dalLichKham.layToanBoLichKham());
        }

        /// <summary>
        /// Gọi DAL_LICHKHAM.layLichKhamTheoMa với ma để tra lịch khám theo khóa; trả ET tương ứng hoặc null nếu không tìm thấy.
        /// XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_LICHKHAM? layLichKhamTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalLichKham.layLichKhamTheoMa(ma));
        }

        public IEnumerable<ET_LICHKHAM_HIENTHI> layLichKhamHienThi()
        {
            return XuLyLoi.ThucHien(() => dalLichKham.layLichKhamHienThi());
        }

        public ET_LICHKHAM_HIENTHI? layLichKhamHienThiTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalLichKham.layLichKhamHienThiTheoMa(ma));
        }

        /// <summary>
        /// Thêm lịch khám từ duLieu qua DAL_LICHKHAM.themLichKham; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ liệu rồi INSERT
        /// trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. Kiểm tra dữ liệu bắt buộc tại BUS trước
        /// khi gọi DAL. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themLichKham(ET_LICHKHAM duLieu)
        {
            KiemTraDuLieu(duLieu);
            return XuLyLoi.ThucHien(() => dalLichKham.themLichKham(duLieu));
        }

        /// <summary>
        /// Cập nhật lịch khám qua DAL_LICHKHAM.suaLichKham; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn hóa dữ liệu và
        /// kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. Kiểm tra dữ liệu bắt
        /// buộc tại BUS trước khi gọi DAL. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaLichKham(ET_LICHKHAM duLieu, ET_LICHKHAM? banGoc = null, bool chiDoiTrangThai = false)
        {
            KiemTraDuLieu(duLieu);
            return XuLyLoi.ThucHien(() => dalLichKham.suaLichKham(duLieu, banGoc, chiDoiTrangThai));
        }

        /// <summary>
        /// Xóa lịch khám có mã ma qua DAL_LICHKHAM.xoaLichKham; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi DELETE. Trả
        /// true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang thông
        /// báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaLichKham(string ma, ET_LICHKHAM? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalLichKham.xoaLichKham(ma, banGoc));
        }
        public int dongBoLichKhamVaoCaLam()
        {
            return dalLichKham.dongBoLichKhamVaoCaLam();
        }

        /// <summary>
        /// Yêu cầu mã bệnh nhân, bác sĩ, phòng và ca khám. Sai điều kiện sẽ ném InvalidOperationException ngay, ngăn thao tác
        /// thêm/sửa gọi DAL; không ghi dữ liệu.
        /// </summary>
        /// <param name="record">Bản ET được kiểm tra hoặc chuyển vào thao tác cập nhật.</param>
        private static void KiemTraDuLieu(ET_LICHKHAM record)
        {
            if (string.IsNullOrWhiteSpace(record.MaBN)) throw new InvalidOperationException("Vui lòng nhập/chọn MaBN.");
            if (string.IsNullOrWhiteSpace(record.MaBS)) throw new InvalidOperationException("Vui lòng nhập/chọn MaBS.");
            if (string.IsNullOrWhiteSpace(record.MaPhong)) throw new InvalidOperationException("Vui lòng nhập/chọn MaPhong.");
            if (string.IsNullOrWhiteSpace(record.MaCa)) throw new InvalidOperationException("Vui lòng nhập/chọn MaCa.");
            if (record.TrangThai != "Hủy" && record.TrangThai != "Đã hủy" &&
                record.TrangThai != "Hoàn thành" && record.TrangThai != "Đã khám" &&
                record.TrangThai != "Đã đổi" && record.MucDoNghiemTrong=="Cao" &&
                string.IsNullOrWhiteSpace(record.MaBSDuBi))
                throw new InvalidOperationException("Lịch khám mức Cao bắt buộc có bác sĩ dự bị.");
        }
    }
}
