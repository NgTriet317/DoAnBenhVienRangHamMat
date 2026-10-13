// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ phòng, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_PHONG. Các lời gọi DAL được bọc
    /// XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_PHONG
    {
        /// <summary>
        /// Đối tượng DAL_PHONG thực hiện truy vấn/ghi phòng trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_PHONG dalPhong;

        /// <summary>
        /// Khởi tạo DAL_PHONG bằng transaction tùy chọn để các thao tác phòng dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_PHONG(IDbTransaction? transaction = null)
        {
            dalPhong = new DAL_PHONG(transaction);
        }

        /// <summary>
        /// Gọi DAL_PHONG.layToanBoPhong để đọc toàn bộ phòng; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể rỗng khi chưa có
        /// dữ liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        public IEnumerable<ET_PHONG> layToanBoPhong()
        {
            return XuLyLoi.ThucHien(() => dalPhong.layToanBoPhong());
        }

        /// <summary>
        /// Gọi DAL_PHONG.layPhongTheoMa với ma để tra phòng theo khóa; trả ET tương ứng hoặc null nếu không tìm thấy. XuLyLoi
        /// chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_PHONG? layPhongTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalPhong.layPhongTheoMa(ma));
        }

        /// <summary>
        /// Thêm phòng từ duLieu qua DAL_PHONG.themPhong; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ liệu rồi INSERT trong giao
        /// dịch. Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và
        /// giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themPhong(ET_PHONG duLieu)
        {
            return XuLyLoi.ThucHien(() => dalPhong.themPhong(duLieu));
        }

        /// <summary>
        /// Cập nhật phòng qua DAL_PHONG.suaPhong; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn hóa dữ liệu và kiểm tra
        /// chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang
        /// thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaPhong(ET_PHONG duLieu, ET_PHONG? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalPhong.suaPhong(duLieu, banGoc, chiDoiTrangThai));
        }

        /// <summary>
        /// Xóa phòng có mã ma qua DAL_PHONG.xoaPhong; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi DELETE. Trả true khi hoàn
        /// tất; bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt
        /// và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaPhong(string ma, ET_PHONG? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalPhong.xoaPhong(ma, banGoc));
        }
    }
}
