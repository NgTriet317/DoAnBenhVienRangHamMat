// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ vật tư, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_VATTU. Các lời gọi DAL được bọc
    /// XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_VATTU
    {
        /// <summary>
        /// Đối tượng DAL_VATTU thực hiện truy vấn/ghi vật tư trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_VATTU data;
        /// <summary>
        /// Khởi tạo DAL_VATTU bằng transaction tùy chọn để các thao tác vật tư dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_VATTU(IDbTransaction? transaction = null) { data = new(transaction); }
        /// <summary>
        /// Gọi DAL_VATTU.layToanBoVatTu để đọc toàn bộ vật tư; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể rỗng khi chưa có
        /// dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        public IEnumerable<ET_VATTU> layToanBoVatTu() => XuLyLoi.ThucHien(() => data.layToanBoVatTu());
        /// <summary>
        /// Gọi DAL_VATTU.layVatTuTheoMa với ma để tra vật tư theo khóa; trả ET tương ứng hoặc null nếu không tìm thấy. XuLyLoi
        /// chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_VATTU? layVatTuTheoMa(string ma) => XuLyLoi.ThucHien(() => data.layVatTuTheoMa(ma));
        /// <summary>
        /// Thêm vật tư từ duLieu qua DAL_VATTU.themVatTu; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ liệu rồi INSERT trong giao
        /// dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và
        /// giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themVatTu(ET_VATTU duLieu) => XuLyLoi.ThucHien(() => data.themVatTu(duLieu));
        /// <summary>
        /// Cập nhật vật tư qua DAL_VATTU.suaVatTu; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn hóa dữ liệu và kiểm tra
        /// chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang
        /// thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaVatTu(ET_VATTU duLieu, ET_VATTU? banGoc = null, bool chiDoiTrangThai = false)
            => XuLyLoi.ThucHien(() => data.suaVatTu(duLieu, banGoc, chiDoiTrangThai));
        /// <summary>
        /// Xóa vật tư có mã ma qua DAL_VATTU.xoaVatTu; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi DELETE. Trả true khi
        /// hoàn tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang thông báo tiếng
        /// Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaVatTu(string ma, ET_VATTU? banGoc = null) => XuLyLoi.ThucHien(() => data.xoaVatTu(ma, banGoc));

    }
}