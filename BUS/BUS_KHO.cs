// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using DAL;
using ET;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ kho, cung cấp thao tác cho giao diện và chuyển truy cập dữ liệu đến DAL_KHO. Các lời gọi DAL được bọc
    /// XuLyLoi để đổi SqlException sang InvalidOperationException tiếng Việt; lỗi nghiệp vụ khác giữ nguyên.
    /// </summary>
    public class BUS_KHO
    {
        /// <summary>
        /// Đối tượng DAL_KHO thực hiện truy vấn/ghi kho trong giao dịch được truyền vào constructor.
        /// </summary>
        private readonly DAL_KHO dalKho;

        /// <summary>
        /// Khởi tạo DAL_KHO bằng transaction tùy chọn để các thao tác kho dùng cùng giao dịch của nơi gọi.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung được truyền cho DAL; null để hạ tầng DAL tự quản lý kết nối/giao dịch.</param>
        public BUS_KHO(IDbTransaction? transaction = null)
        {
            dalKho = new DAL_KHO(transaction);
        }

        /// <summary>
        /// Gọi DAL_KHO.layToanBoKho để đọc toàn bộ kho; trả danh sách ET do DAL đã tải vào bộ nhớ, có thể rỗng khi chưa có dữ
        /// liệu. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        public IEnumerable<ET_KHO> layToanBoKho()
        {
            return XuLyLoi.ThucHien(() => dalKho.layToanBoKho());
        }

        /// <summary>
        /// Gọi DAL_KHO.layKhoTheoMa với ma để tra kho theo khóa; trả ET tương ứng hoặc null nếu không tìm thấy. XuLyLoi chuyển
        /// lỗi SQL sang thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        public ET_KHO? layKhoTheoMa(string ma)
        {
            return XuLyLoi.ThucHien(() => dalKho.layKhoTheoMa(ma));
        }

        /// <summary>
        /// Thêm kho từ duLieu qua DAL_KHO.themKho; DAL cấp mã mới vào ET, chuẩn hóa/kiểm tra dữ liệu rồi INSERT trong giao dịch.
        /// Trả true khi hoàn tất; lỗi được phát lên thay vì trả false. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và giữ
        /// lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        public bool themKho(ET_KHO duLieu)
        {
            return XuLyLoi.ThucHien(() => dalKho.themKho(duLieu));
        }

        /// <summary>
        /// Cập nhật kho qua DAL_KHO.suaKho; DAL khóa bản hiện tại, đối chiếu banGoc nếu có, chuẩn hóa dữ liệu và kiểm tra
        /// chiDoiTrangThai trước UPDATE. Trả true khi hoàn tất; bản mất hoặc xung đột sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang
        /// thông báo tiếng Việt và giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="duLieu">ET chứa giá trị cần lưu; DAL có thể chuẩn hóa giá trị và gán mã khi thêm.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        /// <param name="chiDoiTrangThai">True yêu cầu DAL từ chối thay đổi nội dung ngoài trạng thái và các trường thời điểm/lý do được phép.</param>
        public bool suaKho(ET_KHO duLieu, ET_KHO? banGoc = null, bool chiDoiTrangThai = false)
        {
            return XuLyLoi.ThucHien(() => dalKho.suaKho(duLieu, banGoc, chiDoiTrangThai));
        }

        /// <summary>
        /// Xóa kho có mã ma qua DAL_KHO.xoaKho; DAL khóa bản hiện tại, kiểm tra banGoc nếu có rồi DELETE. Trả true khi hoàn tất;
        /// bản không tồn tại, xung đột hoặc ràng buộc liên kết sẽ phát lỗi. XuLyLoi chuyển lỗi SQL sang thông báo tiếng Việt và
        /// giữ lỗi gốc trong InnerException.
        /// </summary>
        /// <param name="ma">Mã định danh bản ghi cần tra cứu hoặc xóa.</param>
        /// <param name="banGoc">Bản ET đọc trước khi sửa/xóa để DAL phát hiện thay đổi đồng thời; null bỏ đối chiếu bản dự kiến.</param>
        public bool xoaKho(string ma, ET_KHO? banGoc = null)
        {
            return XuLyLoi.ThucHien(() => dalKho.xoaKho(ma, banGoc));
        }
    }
}
