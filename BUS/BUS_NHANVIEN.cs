using DAL;
using ET;
using System.Collections.Generic;
using System.Data;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ nhân viên, nhận yêu cầu từ giao diện và điều phối lớp DAL tương ứng.
    /// </summary>
    public class BUS_NHANVIEN
    {
        /// <summary>
        /// Đối tượng truy cập dữ liệu nhân viên phục vụ các thao tác của lớp.
        /// </summary>
        private readonly DAL_NHANVIEN data;
        /// <summary>
        /// Khởi tạo nhân viên.
        /// </summary>
        /// <param name="transaction">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
        public BUS_NHANVIEN(IDbTransaction? transaction = null) => data = new(transaction);
        /// <summary>
        /// Đồng bộ trạng thái nhân viên theo ngày hiệu lực của đơn nghỉ đã duyệt; phân biệt nghỉ phép tạm thời và nghỉ việc. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>data.dongBoTrangThaiNghi</c>.</remarks>
        public void dongBoTrangThaiNghi()
        {
            data.dongBoTrangThaiNghi();
        }

        /// <summary>
        /// Đọc danh sách nhân viên hiện tại qua tầng dữ liệu, gồm bước đồng bộ trạng thái nghỉ tại DAL. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dalNhanVien.layToanBoNV</c>.</remarks>
        /// <returns>Tập dữ liệu kiểu IEnumerable&lt;ET_NHANVIEN&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
        public IEnumerable<ET_NHANVIEN> layToanBoNhanVien()
        {
            DAL_NHANVIEN dalNhanVien = data;
            return dalNhanVien.layToanBoNV();
        }

        /// <summary>
        /// Đọc nhân viên theo mã sau khi đồng bộ trạng thái nghỉ đến ngày hiện tại. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dalNhanVien.layNhanVienTheoMa</c>.</remarks>
        /// <param name="maNV">Mã nhân viên xác định đối tượng cần xử lý.</param>
        /// <returns>Kết quả kiểu ET_NHANVIEN dùng cho bước xử lý tiếp theo.</returns>
        public ET_NHANVIEN layNhanVienTheoMa(string maNV)
        {
            DAL_NHANVIEN dalNhanVien = data;
            return dalNhanVien.layNhanVienTheoMa(maNV);
        }

        /// <summary>
        /// Thêm hồ sơ nhân viên theo dữ liệu đầu vào và quy tắc kiểm tra của tầng dữ liệu. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dalNhanVien.themNhanVien</c>.</remarks>
        /// <param name="nhanVien">Giá trị nhân viên dùng khi thực hiện thao tác; kiểu ET_NHANVIEN.</param>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        public bool themNhanVien(ET_NHANVIEN nhanVien)
        {
            DAL_NHANVIEN dalNhanVien = data;
            return dalNhanVien.themNhanVien(nhanVien);
        }

        /// <summary>
        /// Cập nhật thông tin hồ sơ nhân viên; giữ trạng thái vừa đồng bộ từ đơn nghỉ và không ghi đè bằng dữ liệu cũ trên biểu mẫu. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dalNhanVien.capNhatNhanVien</c>.</remarks>
        /// <param name="nhanVien">Giá trị nhân viên dùng khi thực hiện thao tác; kiểu ET_NHANVIEN.</param>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        public bool capNhatNhanVien(ET_NHANVIEN nhanVien)
        {
            DAL_NHANVIEN dalNhanVien = data;
            return dalNhanVien.capNhatNhanVien(nhanVien);
        }

        /// <summary>
        /// Khôi phục nhân viên đã nghỉ, kết thúc hiệu lực đơn nghỉ việc cũ và đưa các ca tương lai liên quan về chờ xác nhận. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dalNhanVien.khoiPhucNhanVien</c>.</remarks>
        /// <param name="maNV">Mã nhân viên xác định đối tượng cần xử lý.</param>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        public bool khoiPhucNhanVien(string maNV)
        {
            DAL_NHANVIEN dalNhanVien = data;
            return dalNhanVien.khoiPhucNhanVien(maNV);
        }

        /// <summary>
        /// Chuyển nhân viên đang được chỉ định sang trạng thái Đã nghỉ nếu điều kiện cập nhật phù hợp. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dalNhanVien.nghiViecNhanVien</c>.</remarks>
        /// <param name="maNV">Mã nhân viên xác định đối tượng cần xử lý.</param>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        public bool nghiViecNhanVien(string maNV)
        {
            DAL_NHANVIEN dalNhanVien = data;
            return dalNhanVien.nghiViecNhanVien(maNV);
        }
    }
}
