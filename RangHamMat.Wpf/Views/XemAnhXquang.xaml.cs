using System.Windows;
using ET;

namespace RangHamMat.Views;

/// <summary>
/// Cửa sổ xem ảnh lớn từ nội dung đã được tầng nghiệp vụ kiểm tra quyền và ghi nhận truy cập.
/// </summary>
public partial class XemAnhXquang : Window
{
    /// <summary>
    /// Nạp ảnh đã đọc và hiển thị tên tệp, lần khám, thời điểm tạo để đối chiếu.
    /// </summary>
    /// <param name="source">Nội dung ảnh cùng metadata do nghiệp vụ trả về sau khi kiểm tra quyền.</param>
    public XemAnhXquang(DuLieuAnhXquang source)
    {
        InitializeComponent();Caption.Text=$"{source.Anh.TenTepGoc} · Lần khám {source.Anh.MaLS} · {source.Anh.NgayTao:dd/MM/yyyy HH:mm:ss}";
        FullImage.Source=AnhXquangLanKham.Decode(source.NoiDung);
    }
}
