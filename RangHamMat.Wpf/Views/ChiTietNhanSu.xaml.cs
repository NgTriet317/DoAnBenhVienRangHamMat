// PHẦN AI ĐÃ THỰC HIỆN: AI đã bổ sung giải thích constructor nhận nhân viên và gán DataContext để XAML hiển thị thông tin nhân sự.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using ET;
using System.Windows;
using System.Windows.Controls;

namespace RangHamMat.Views;

/// <summary>
/// Màn hình chi tiết nhân viên: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình. File hiện chỉ khởi tạo giao diện, chưa có thao tác lưu SQL riêng.
/// </summary>
public partial class ChiTietNhanSu : Window
{    

    /// <summary>
    /// Khởi tạo chi tiết nhân viên từ XAML. Gán ET_NHANVIEN được truyền vào làm DataContext để hiện đúng nhân viên nguồn.
    /// </summary>
    public ChiTietNhanSu(ET_NHANVIEN nhanVien)
    {
        InitializeComponent();

        DataContext = nhanVien;
    }
}
