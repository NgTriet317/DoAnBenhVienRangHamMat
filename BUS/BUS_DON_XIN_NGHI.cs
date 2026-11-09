using DAL;
using ET;
using System;
using System.Collections.Generic;

namespace BUS
{
    /// <summary>
    /// Tầng nghiệp vụ đơn xin nghỉ, nhận yêu cầu từ giao diện và điều phối lớp DAL tương ứng.
    /// </summary>
    public class BUS_DON_XIN_NGHI
    {
        /// <summary>
        /// Đối tượng truy cập dữ liệu đơn xin nghỉ phục vụ các thao tác của lớp.
        /// </summary>
        private DAL_DON_XIN_NGHI dal = new DAL_DON_XIN_NGHI();

        /// <summary>
        /// Kiểm tra và lưu đơn xin nghỉ để chuyển vào quy trình duyệt của quản lý. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>DateTime.Today.AddDays</c>, <c>dal.guiDon</c>.</remarks>
        /// <param name="don">Giá trị đơn dùng khi thực hiện thao tác; kiểu ET_DON_XIN_NGHI.</param>
        /// <returns>Chuỗi kết quả của thao tác gửi đơn.</returns>
        public string guiDon(ET_DON_XIN_NGHI don)
        {
            if (string.IsNullOrWhiteSpace(don.MaNV) || string.IsNullOrWhiteSpace(don.LyDo))
                throw new ArgumentException("Thiếu mã nhân viên hoặc lý do xin nghỉ.");
            if (don.TuNgay.Date < DateTime.Today.AddDays(7))
                throw new ArgumentException("Phải xin nghỉ trước ít nhất 7 ngày. Ngày bắt đầu nghỉ sớm nhất là " + DateTime.Today.AddDays(7).ToString("dd/MM/yyyy") + ".");
            if (don.DenNgay.Date < don.TuNgay.Date)
                throw new ArgumentException("Ngày kết thúc không được trước ngày bắt đầu.");
            don.NgayGui = DateTime.Now;
            don.TrangThaiDuyet = "Chờ duyệt";
            don.MaNVDuyet = null;
            don.NgayDuyet = null;
            return dal.guiDon(don);
        }

        /// <summary>
        /// Tạo đơn nghỉ khẩn cấp theo quyền quản lý và điều kiện riêng của loại đơn. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dal.taoDonKhanCap</c>.</remarks>
        /// <param name="don">Giá trị đơn dùng khi thực hiện thao tác; kiểu ET_DON_XIN_NGHI.</param>
        /// <param name="maQuanLy">Mã quản lý xác định đối tượng cần xử lý.</param>
        /// <returns>Chuỗi kết quả của thao tác tạo đơn khẩn cấp.</returns>
        public string taoDonKhanCap(ET_DON_XIN_NGHI don, string maQuanLy)
        {
            if (string.IsNullOrWhiteSpace(maQuanLy) || string.IsNullOrWhiteSpace(don.MaNV))
                throw new ArgumentException("Phải chọn nhân viên và người quản lý.");
            if (don.MaNV==maQuanLy) throw new ArgumentException("Quản lý không được tự lập đơn khẩn cấp đã duyệt cho chính mình.");
            if (string.IsNullOrWhiteSpace(don.LyDo)) throw new ArgumentException("Phải nêu lý do nghỉ khẩn cấp.");
            if (don.TuNgay.Date<DateTime.Today || don.DenNgay.Date<don.TuNgay.Date)
                throw new ArgumentException("Ngày nghỉ khẩn cấp phải từ hôm nay, ngày kết thúc không được trước ngày bắt đầu.");
            // Ngoại lệ 7 ngày CHỈ áp dụng cho đơn khẩn cấp do quản lý tạo.
            don.TuNgay=don.TuNgay.Date;
            don.DenNgay=don.DenNgay.Date;
            don.GhiChu="Loại đơn: Nghỉ phép\nĐơn khẩn cấp do quản lý tạo.\n"+don.GhiChu;
            return dal.taoDonKhanCap(don,maQuanLy);
        }

        /// <summary>
        /// Kiểm tra nhân viên đã có đơn nghỉ được duyệt áp dụng tại ngày được chỉ định. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dal.daDuocDuyetNghi</c>.</remarks>
        /// <param name="maNV">Mã nhân viên xác định đối tượng cần xử lý.</param>
        /// <param name="ngay">Giá trị ngày dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        public bool daDuocDuyetNghi(string maNV, DateTime ngay)
        {
            return dal.daDuocDuyetNghi(maNV, ngay);
        }

        /// <summary>
        /// Đếm các đơn còn chờ duyệt để hiển thị thông báo cho quản lý. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dal.demDonChoDuyet</c>.</remarks>
        /// <returns>Giá trị số được đọc, đếm hoặc tính toán trong hàm.</returns>
        public int demDonChoDuyet()
        {
            return dal.demDonChoDuyet();
        }

        /// <summary>
        /// Đọc danh sách đơn xin nghỉ cùng dữ liệu hiển thị liên quan. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dal.layDanhSachDon</c>.</remarks>
        /// <returns>Tập dữ liệu kiểu List&lt;ET_DON_XIN_NGHI_HIENTHI&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
        public List<ET_DON_XIN_NGHI_HIENTHI> layDanhSachDon()
        {
            return dal.layDanhSachDon();
        }

        /// <summary>
        /// Đọc các ca bị ảnh hưởng bởi đơn nghỉ để quản lý lập phương án thay thế. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dal.layCaCanThayThe</c>.</remarks>
        /// <param name="maDon">Mã đơn xác định đối tượng cần xử lý.</param>
        /// <returns>Tập dữ liệu kiểu List&lt;ET_CA_CAN_THAY_THE&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
        public List<ET_CA_CAN_THAY_THE> layCaCanThayThe(string maDon)
        {
            return dal.layCaCanThayThe(maDon);
        }

        /// <summary>
        /// Đọc danh sách nhân viên phù hợp để thay ca của đơn nghỉ. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dal.layNhanVienThayThe</c>.</remarks>
        /// <param name="maDon">Mã đơn xác định đối tượng cần xử lý.</param>
        /// <returns>Tập dữ liệu kiểu List&lt;ET_NGUOI_THAY_THE&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
        public List<ET_NGUOI_THAY_THE> layNhanVienThayThe(string maDon)
        {
            return dal.layNhanVienThayThe(maDon);
        }

        /// <summary>
        /// Kiểm tra và lưu phương án nhân viên/phòng thay thế cho ca thuộc đơn nghỉ. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dal.phanCongThayThe</c>.</remarks>
        /// <param name="maDon">Mã đơn xác định đối tượng cần xử lý.</param>
        /// <param name="maLLV">Mã lịch làm việc xác định đối tượng cần xử lý.</param>
        /// <param name="maNVThay">Mã nhân viên thay xác định đối tượng cần xử lý.</param>
        /// <param name="maPhong">Mã phòng xác định đối tượng cần xử lý.</param>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        public bool phanCongThayThe(string maDon, string maLLV, string maNVThay, string maPhong)
        {
            if (string.IsNullOrWhiteSpace(maDon) || string.IsNullOrWhiteSpace(maLLV) ||
                string.IsNullOrWhiteSpace(maNVThay) || string.IsNullOrWhiteSpace(maPhong))
                throw new ArgumentException("Vui lòng chọn ca, nhân viên thay thế và phòng.");
            return dal.phanCongThayThe(maDon, maLLV, maNVThay, maPhong);
        }

        /// <summary>
        /// Hủy phương án thay ca đang được chọn sau khi kiểm tra điều kiện dữ liệu. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dal.huyPhanCongThayThe</c>.</remarks>
        /// <param name="maDon">Mã đơn xác định đối tượng cần xử lý.</param>
        /// <param name="maLLVGoc">Mã lịch làm việc gốc xác định đối tượng cần xử lý.</param>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        public bool huyPhanCongThayThe(string maDon, string maLLVGoc)
        {
            if (string.IsNullOrWhiteSpace(maDon) || string.IsNullOrWhiteSpace(maLLVGoc))
                throw new ArgumentException("Vui lòng chọn đơn và ca cần hủy phương án thay.");
            return dal.huyPhanCongThayThe(maDon, maLLVGoc);
        }

        /// <summary>
        /// Đọc các lịch khám bị ảnh hưởng bởi đơn nghỉ và cần quản lý xác nhận xử lý. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dal.layLichKhamCanXacNhan</c>.</remarks>
        /// <param name="maDon">Mã đơn xác định đối tượng cần xử lý.</param>
        /// <returns>Tập dữ liệu kiểu List&lt;ET_LICH_KHAM_CAN_XAC_NHAN&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
        public List<ET_LICH_KHAM_CAN_XAC_NHAN> layLichKhamCanXacNhan(string maDon)
        {
            if (string.IsNullOrWhiteSpace(maDon))
                throw new ArgumentException("Thiếu mã đơn xin nghỉ.");
            return dal.layLichKhamCanXacNhan(maDon);
        }

        /// <summary>
        /// Kiểm tra và xác nhận phương án xử lý của từng lịch khám thuộc đơn nghỉ. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dal.xacNhanTungLichKham</c>.</remarks>
        /// <param name="maDon">Mã đơn xác định đối tượng cần xử lý.</param>
        /// <param name="maLich">Mã lịch xác định đối tượng cần xử lý.</param>
        /// <param name="maNguoiXacNhan">Mã người xác nhận xác định đối tượng cần xử lý.</param>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        public bool xacNhanTungLichKham(string maDon, string maLich, string maNguoiXacNhan)
        {
            if (string.IsNullOrWhiteSpace(maDon) || string.IsNullOrWhiteSpace(maLich) ||
                string.IsNullOrWhiteSpace(maNguoiXacNhan))
                throw new ArgumentException("Thiếu thông tin lịch khám hoặc người xác nhận.");
            return dal.xacNhanTungLichKham(maDon, maLich, maNguoiXacNhan);
        }

        /// <summary>
        /// Kiểm tra người duyệt, phương án ca/lịch và ghi kết quả duyệt hoặc từ chối đơn nghỉ. Chuyển thao tác dữ liệu đến DAL; giữ kết quả trả về cho nơi gọi.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>dal.xuLyDon</c>, <c>lyDoKhongDuyet.Trim</c>.</remarks>
        /// <param name="maDon">Mã đơn xác định đối tượng cần xử lý.</param>
        /// <param name="maNguoiDuyet">Mã người duyệt xác định đối tượng cần xử lý.</param>
        /// <param name="duyet">Giá trị duyệt dùng khi thực hiện thao tác; kiểu bool.</param>
        /// <param name="lyDoKhongDuyet">Giá trị lý do không duyệt dùng khi thực hiện thao tác; kiểu string.</param>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        public bool xuLyDon(string maDon, string maNguoiDuyet, bool duyet, string lyDoKhongDuyet)
        {
            if (string.IsNullOrWhiteSpace(maDon) || string.IsNullOrWhiteSpace(maNguoiDuyet))
                throw new ArgumentException("Thiếu mã đơn hoặc người duyệt.");
            if (!duyet && string.IsNullOrWhiteSpace(lyDoKhongDuyet))
                throw new ArgumentException("Phải nhập lý do không duyệt đơn.");
            if (lyDoKhongDuyet.Length > 1000)
                throw new ArgumentException("Lý do không duyệt tối đa 1000 ký tự.");
            return dal.xuLyDon(maDon, maNguoiDuyet, duyet, lyDoKhongDuyet.Trim());
        }
    }
}
