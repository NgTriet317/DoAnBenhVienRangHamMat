using ET;
using System;
using System.Windows;

namespace RangHamMat.Views
{
    /// <summary>
    /// Phần xử lý chi tiết lịch khám của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
    /// </summary>
    public partial class ChiTietLichKham : Window
    {
        /// <summary>
        /// Lịch hiển thị tải được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private ET_LICHKHAM lichHienTai;

        /// <summary>
        /// Khởi tạo chi tiết lịch khám. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
        /// </summary>
        /// <param name="lich">Giá trị lịch dùng khi thực hiện thao tác; kiểu ET_LICHKHAM.</param>
        public ChiTietLichKham(ET_LICHKHAM lich)
        {
            InitializeComponent();
            lichHienTai = lich;
            ET_LICHKHAM lichHienThi = lich;
            if (lich is not ET_LICHKHAM_HIENTHI)
            {
                ET_LICHKHAM_HIENTHI? tenHienThi = new BUS.BUS_LICHKHAM().layLichKhamHienThiTheoMa(lich.MaLich);
                if (tenHienThi != null) lichHienThi = tenHienThi;
            }
            DataContext = lichHienThi;

            if (lich.TrangThai is not ("Nháp" or "Chờ xác nhận" or "Chưa xác nhận"))
            {
                btnSua.IsEnabled = false;
                txtGhiChuSua.Text = "Lịch đã xác nhận/hoàn tất: giữ nguyên dữ liệu; nếu cần đổi lịch, tạo lịch hẹn mới.";
            }
            else
            {
                txtGhiChuSua.Text = "Bấm Chỉnh sửa để thay đổi thông tin ngay trong cửa sổ này.";
            }
        }

        /// <summary>
        /// Xử lý thao tác nút sửa khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void btnSua_Click(object sender, RoutedEventArgs e)
        {
            if (lichHienTai == null) return;
            if (lichHienTai.TrangThai is not ("Nháp" or "Chờ xác nhận" or "Chưa xác nhận")) return;

            // Chuyển từ chế độ xem sang chế độ sửa trong cùng cửa sổ.
            ThemLichKham formSua = new ThemLichKham(lichHienTai);
            Title = "Chỉnh sửa lịch khám - " + lichHienTai.MaLich;
            MaxWidth = Math.Max(660, SystemParameters.WorkArea.Width - 50);
            MaxHeight = Math.Max(490, SystemParameters.WorkArea.Height - 50);
            Width = Math.Min(1040, MaxWidth);
            Height = Math.Min(750, MaxHeight);
            MinWidth = Math.Min(800, MaxWidth);
            MinHeight = Math.Min(530, MaxHeight);
            Content = formSua;
        }

        /// <summary>
        /// Xử lý thao tác nút dòng khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>Close</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void btnDong_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
