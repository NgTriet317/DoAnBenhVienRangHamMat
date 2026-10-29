using System;

namespace ET
{
    /// <summary>
    /// Dữ liệu nhận trao đổi giữa các tầng và ánh xạ với kết quả truy vấn SQL.
    /// </summary>
    public class ET_LICH_KHAM_CAN_XAC_NHAN
    {
        /// <summary>
        /// Mã lịch.
        /// </summary>
        public string MaLich { get; set; } = "";
        /// <summary>
        /// Tên bệnh nhân.
        /// </summary>
        public string TenBenhNhan { get; set; } = "";
        /// <summary>
        /// Ngày giờ khám.
        /// </summary>
        public DateTime NgayGioKham { get; set; }
        /// <summary>
        /// Thời lượng dự kiến.
        /// </summary>
        public int ThoiLuongDuKien { get; set; }
        /// <summary>
        /// Mã ca làm việc.
        /// </summary>
        public string MaCa { get; set; } = "";
        /// <summary>
        /// Mã bác sĩ cũ.
        /// </summary>
        public string MaBSCu { get; set; } = "";
        /// <summary>
        /// Tên bác sĩ cũ.
        /// </summary>
        public string TenBSCu { get; set; } = "";
        /// <summary>
        /// Tên phòng cũ.
        /// </summary>
        public string TenPhongCu { get; set; } = "";
        /// <summary>
        /// Mã phòng cũ.
        /// </summary>
        public string MaPhongCu { get; set; } = "";
        /// <summary>
        /// Mã bác sĩ mới.
        /// </summary>
        public string MaBSMoi { get; set; } = "";
        /// <summary>
        /// Tên bác sĩ mới.
        /// </summary>
        public string TenBSMoi { get; set; } = "";
        /// <summary>
        /// Mã phòng mới.
        /// </summary>
        public string MaPhongMoi { get; set; } = "";
        /// <summary>
        /// Tên phòng mới.
        /// </summary>
        public string TenPhongMoi { get; set; } = "";
        /// <summary>
        /// Mã lịch làm việc thay thế.
        /// </summary>
        public string MaLLVThayThe { get; set; } = "";
        /// <summary>
        /// Mã bác sĩ dự bị. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public string? MaBSDuBi { get; set; }
        /// <summary>
        /// Tên bác sĩ dự bị.
        /// </summary>
        public string TenBSDuBi { get; set; } = "";
        /// <summary>
        /// Đã xác nhận.
        /// </summary>
        public bool DaXacNhan { get; set; }
        /// <summary>
        /// Mã nhân viên xác nhận.
        /// </summary>
        public string MaNVXacNhan { get; set; } = "";
        /// <summary>
        /// Tên người xác nhận.
        /// </summary>
        public string TenNguoiXacNhan { get; set; } = "";
        /// <summary>
        /// Thời điểm xác nhận; dữ liệu nháp hoặc chưa xác nhận có thể chưa có giá trị. Có thể để null khi dữ liệu chưa được cung cấp.
        /// </summary>
        public DateTime? NgayXacNhan { get; set; }
        /// <summary>
        /// Trạng thái lưu.
        /// </summary>
        public string TrangThaiLuu { get; set; } = "";

        /// <summary>
        /// Giá trị Trạng thái xác nhận; được xác định bởi phần getter/setter của thuộc tính.
        /// </summary>
        public string TrangThaiXacNhan
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(TrangThaiLuu)) return TrangThaiLuu;
                if (DaXacNhan) return "Đã xác nhận";
                if (string.IsNullOrWhiteSpace(MaLLVThayThe)) return "Chưa có ca thay";
                return "Chưa xác nhận";
            }
        }
    }
}
