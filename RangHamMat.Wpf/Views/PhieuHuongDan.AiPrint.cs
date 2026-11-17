using System;
using System.Data;
using System.Linq;
using System.Windows;

namespace RangHamMat.Views;

public partial class PhieuHuongDan
{
    /// <summary>
    /// Dùng lại thao tác in hiện có theo mã phiếu đã lưu trong SQL; không cung cấp danh sách
    /// phiếu và thông tin người bệnh của màn hình nhân viên ra giao diện kiosk.
    /// </summary>
    public void InPhieuTheoMaTuAI(string maPhieu)
    {
        if (string.IsNullOrWhiteSpace(maPhieu))
            throw new ArgumentException("Thiếu mã phiếu để in.", nameof(maPhieu));

        ChonPhieu(maPhieu);
        if (!daTai) hienThiDuLieu();

        // Khi được gọi từ AI, PhieuHuongDan có thể chưa được Loaded vào cây WPF.
        // Không dựa hoàn toàn vào SelectionChanged của DataGrid chưa render.
        if (phieuDangChon is null ||
            !string.Equals(phieuDangChon["MaPhieu"]?.ToString(), maPhieu, StringComparison.OrdinalIgnoreCase))
        {
            phieuDangChon = danhSachPhieu.DefaultView.Cast<DataRowView>().FirstOrDefault(r =>
                string.Equals(r["MaPhieu"]?.ToString(), maPhieu, StringComparison.OrdinalIgnoreCase));
        }
        if (phieuDangChon is null)
            throw new InvalidOperationException("Không tìm thấy phiếu hoặc tài khoản không có quyền xem phiếu này.");
        string trangThai = phieuDangChon["TrangThai"]?.ToString() ?? "";
        if (trangThai.Contains("hủy", StringComparison.CurrentCultureIgnoreCase) ||
            trangThai.Contains("huy", StringComparison.CurrentCultureIgnoreCase))
            throw new InvalidOperationException("Phiếu đã hủy, không thể in.");
        btnIn.IsEnabled = true;

        // Tái sử dụng toàn bộ quy trình A5 + PrintDialog + PrintDocument có sẵn.
        In_Click(this, new RoutedEventArgs());
    }
}
