// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ kế hoạch điều trị, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_KEHOACHDIEUTRI. Các
    /// lời gọi DAL được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_KEHOACHDIEUTRI
    {
        /// <summary>
        /// Đối tượng DAL_KEHOACHDIEUTRI thực hiện truy vấn/ghi kế hoạch điều trị trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_KEHOACHDIEUTRI dalKeHoachDieuTri;

        /// <summary>
        /// Khởi tạo DAL_KEHOACHDIEUTRI bằng transaction tùy chọn để các thao tác kế hoạch điều trị dùng cùng giao dịch của nơi
        /// gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_KEHOACHDIEUTRI(IDbTransaction? transaction = null)
        {
            dalKeHoachDieuTri = new DAL_KEHOACHDIEUTRI(transaction);
        }

        /// <summary>
        /// Gọi DAL_KEHOACHDIEUTRI.layToanBoKeHoachDieuTri để đọc toàn bộ kế hoạch điều trị; trả danh sách ET do DAL đã tải vào bộ
        /// nhớ, có thể rỗng khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong
        /// InnerException.
        /// </summary>
        public IEnumerable<ET_KEHOACHDIEUTRI> layToanBoKeHoachDieuTri()
        {
            return XuLyLoi.ThucHien(() => dalKeHoachDieuTri.layToanBoKeHoachDieuTri());
        }

        /// <summary>
        /// Gọi DAL_KEHOACHDIEUTRI.layKeHoachDieuTriTheoMa với ma để tra kế hoạch điều trị theo khóa; trả ET tương ứng hoặc null
        /// nếu không tìm thấy. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_KEHOACHDIEUTRI? layKeHoachDieuTriTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalKeHoachDieuTri.layKeHoachDieuTriTheoMa(ma));
        }

        /// <summary>
        /// Thêm kế hoạch điều trị từ duLieu qua DAL_KEHOACHDIEUTRI.themKeHoachDieuTri; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra
        /// dữ liệu rồi INSERT trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. Kiểm tra dữ liệu bắt
        /// buộc tại BUS trước khi gọi DAL. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themKeHoachDieuTri(ET_KEHOACHDIEUTRI duLieu)
        {
            KiemTraDuLieu(duLieu);
            return XuLyLoi.ThucHien(() => dalKeHoachDieuTri.themKeHoachDieuTri(duLieu));
        }

        /// <summary>
        /// Cập nhật kế hoạch điều trị qua DAL_KEHOACHDIEUTRI.suaKeHoachDieuTri; DAL khóa bản hiện tại, đối chiếu banGoc nếu có,
        /// chuẩn hóa dữ liệu và kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi.
        /// Kiểm tra dữ liệu bắt buộc tại BUS trước khi gọi DAL. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc
        /// trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaKeHoachDieuTri(ET_KEHOACHDIEUTRI duLieu, ET_KEHOACHDIEUTRI? banGoc = null, bool chiDoiTrangThai = false)
        {
            KiemTraDuLieu(duLieu);
            return XuLyLoi.ThucHien(() => dalKeHoachDieuTri.suaKeHoachDieuTri(duLieu, banGoc, chiDoiTrangThai));
        }

        /// <summary>
        /// Xóa kế hoạch điều trị có mã ma qua DAL_KEHOACHDIEUTRI.xoaKeHoachDieuTri; DAL khóa bản hiện tại, kiểm tra banGoc nếu có
        /// rồi DELETE. Trả true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi
        /// SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaKeHoachDieuTri(string ma, ET_KEHOACHDIEUTRI? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalKeHoachDieuTri.xoaKeHoachDieuTri(ma, banGoc));
        }
        /// <summary>
        /// Yêu cầu mã lần khám và mã bác sĩ lập kế hoạch. Sai điều kiện sẽ ném InvalidOperationException ngay, ngăn thao tác
        /// thêm/sửa gọi DAL; không ghi dữ liệu.
        /// </summary>
        /// <param name="record">Bản ET được kiểm tra hoặc chuyển vào thao tác cập nhật.</param>
        private static void KiemTraDuLieu(ET_KEHOACHDIEUTRI record)
        {
            if (string.IsNullOrWhiteSpace(record.MaLS)) throw new InvalidOperationException("Vui lòng nhập/chọn MaLS.");
            if (string.IsNullOrWhiteSpace(record.MaBS)) throw new InvalidOperationException("Vui lòng nhập/chọn MaBS.");
        }
    }
}
