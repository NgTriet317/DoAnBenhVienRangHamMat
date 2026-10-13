// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using Dapper.Contrib.Extensions;
using System;

namespace ET
{
    /// <summary>
    /// Thực thể ánh xạ bảng TINH_TRANG_BENH qua Dapper.Contrib; chứa các cột đúng kiểu SQL để truyền giữa DAL/BUS và giao diện, không tự mở kết nối hoặc xử lý nghiệp vụ.
    /// </summary>
    [Table("TINH_TRANG_BENH")]
    public class ET_TINHTRANGBENH
    {
        private string maTTB = string.Empty;
        private string maLS = string.Empty;
        private string loaiTinhTrang = string.Empty;
        private string moTaTinhTrang = string.Empty;
        private string mucDo = string.Empty;
        private string? ghiChu;

        /// <summary>
        /// Khởi tạo thực thể rỗng để Dapper hoặc cơ chế chuyển dictionary điền các cột; các giá trị mặc định chưa phải một bản ghi đã lưu.
        /// </summary>
        public ET_TINHTRANGBENH() { }

        /// <summary>
        /// Nhận các giá trị cột qua tham số và gán cho thực thể; việc gọi constructor không ghi dữ liệu xuống SQL.
        /// </summary>
        public ET_TINHTRANGBENH(string maTTB, string maLS, string loaiTinhTrang, string moTaTinhTrang, string mucDo, string? ghiChu)
        {
            this.maTTB = maTTB;
            this.maLS = maLS;
            this.loaiTinhTrang = loaiTinhTrang;
            this.moTaTinhTrang = moTaTinhTrang;
            this.mucDo = mucDo;
            this.ghiChu = ghiChu;
        }

        /// <summary>
        /// Mã bản ghi tình trạng bệnh; ánh xạ cột MaTTB của bảng trong TableAttribute. Khóa chính dạng mã chuỗi; thao tác tạo mới cấp mã trong tầng lưu.
        /// </summary>
        [ExplicitKey]
        public string MaTTB { get => maTTB; set => maTTB = value; }
        /// <summary>
        /// Mã lần khám trong lịch sử khám bệnh; ánh xạ cột MaLS của bảng trong TableAttribute.
        /// </summary>
        public string MaLS { get => maLS; set => maLS = value; }
        /// <summary>
        /// Loại tình trạng bệnh; ánh xạ cột LoaiTinhTrang của bảng trong TableAttribute.
        /// </summary>
        public string LoaiTinhTrang { get => loaiTinhTrang; set => loaiTinhTrang = value; }
        /// <summary>
        /// Mô tả tình trạng bệnh ghi nhận khi khám; ánh xạ cột MoTaTinhTrang của bảng trong TableAttribute.
        /// </summary>
        public string MoTaTinhTrang { get => moTaTinhTrang; set => moTaTinhTrang = value; }
        /// <summary>
        /// Mức độ tình trạng bệnh; ánh xạ cột MucDo của bảng trong TableAttribute.
        /// </summary>
        public string MucDo { get => mucDo; set => mucDo = value; }
        /// <summary>
        /// Ghi chú bổ sung; ánh xạ cột GhiChu của bảng trong TableAttribute. Cho phép null khi thông tin chưa có theo schema.
        /// </summary>
        public string? GhiChu { get => ghiChu; set => ghiChu = value; }
    }
}
