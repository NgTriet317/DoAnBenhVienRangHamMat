using ET;
using System;
using System.Windows;

namespace RangHamMat.Views
{
    public partial class ChiTietLichKham : Window
    {
        private ET_LICHKHAM lichHienTai;

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

        private void btnDong_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
