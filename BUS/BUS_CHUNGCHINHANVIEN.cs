// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ chứng chỉ nhân viên, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_CHUNGCHINHANVIEN.
    /// Các lời gọi DAL được bọc XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ
    /// nguyên.
    /// </summary>
    public class BUS_CHUNGCHINHANVIEN
    {
        /// <summary>
        /// Đối tượng DAL_CHUNGCHINHANVIEN thực hiện truy vấn/ghi chứng chỉ nhân viên trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_CHUNGCHINHANVIEN data;
        /// <summary>
        /// Khởi tạo DAL_CHUNGCHINHANVIEN bằng transaction tùy chọn để các thao tác chứng chỉ nhân viên dùng cùng giao dịch của
        /// nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_CHUNGCHINHANVIEN(IDbTransaction? transaction = null) { data = new(transaction); }
        /// <summary>
        /// Gọi DAL_CHUNGCHINHANVIEN.layToanBoChungChiNhanVien để đọc toàn bộ chứng chỉ nhân viên; trả danh sách ET do DAL đã tải
        /// vào bộ nhớ, có thể rỗng khi chưa có dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong
        /// InnerException.
        /// </summary>
        public IEnumerable<ET_CHUNGCHINHANVIEN> layToanBoChungChiNhanVien() => XuLyLoi.ThucHien(() => data.layToanBoChungChiNhanVien());
        /// <summary>
        /// Gọi DAL_CHUNGCHINHANVIEN.layChungChiNhanVienTheoMa với ma để tra chứng chỉ nhân viên theo khóa; trả ET tương ứng hoặc
        /// null nếu không tìm thấy. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_CHUNGCHINHANVIEN? layChungChiNhanVienTheoMa(string ma) => XuLyLoi.ThucHien(() => data.layChungChiNhanVienTheoMa(ma));
        /// <summary>
        /// Thêm chứng chỉ nhân viên từ duLieu qua DAL_CHUNGCHINHANVIEN.themChungChiNhanVien; DAL cấp mã mới vào ET, chuẩn
        /// hóa/kiểm tra dữ liệu rồi INSERT trong giao dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. XuLyLoi
        /// chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themChungChiNhanVien(ET_CHUNGCHINHANVIEN duLieu) => XuLyLoi.ThucHien(() => data.themChungChiNhanVien(duLieu));
        /// <summary>
        /// Cập nhật chứng chỉ nhân viên qua DAL_CHUNGCHINHANVIEN.suaChungChiNhanVien; DAL khóa bản hiện tại, đối chiếu banGoc nếu
        /// có, chuẩn hóa dữ liệu và kiểm tra chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát
        /// lỗi. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaChungChiNhanVien(ET_CHUNGCHINHANVIEN duLieu, ET_CHUNGCHINHANVIEN? banGoc = null, bool chiDoiTrangThai = false)
            => XuLyLoi.ThucHien(() => data.suaChungChiNhanVien(duLieu, banGoc, chiDoiTrangThai));
        /// <summary>
        /// Xóa chứng chỉ nhân viên có mã ma qua DAL_CHUNGCHINHANVIEN.xoaChungChiNhanVien; DAL khóa bản hiện tại, kiểm tra banGoc
        /// nếu có rồi DELETE. Trả true khi hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi
        /// chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaChungChiNhanVien(string ma, ET_CHUNGCHINHANVIEN? banGoc = null) => XuLyLoi.ThucHien(() => data.xoaChungChiNhanVien(ma, banGoc));

    }
}