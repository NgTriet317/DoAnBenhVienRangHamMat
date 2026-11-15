using System.Windows;
using ET;

namespace RangHamMat.Views;

/// <summary>
/// Cửa sổ xem ảnh lớn từ nội dung đã được tầng nghiệp vụ kiểm tra quyền và ghi nhận truy cập.
/// </summary>
public partial class XemAnhXquang : Window
{
    /// <summary>
    /// Nạp ảnh đã đọc, hiển thị lần khám và giữ ghi chú nguồn/ảnh tham khảo khi xem lớn.
    /// </summary>
    /// <param name="source">Nội dung ảnh cùng metadata do nghiệp vụ trả về sau khi kiểm tra quyền.</param>
    public XemAnhXquang(DuLieuAnhXquang source)
    {
        InitializeComponent();Caption.Text=$"{source.Anh.TenTepGoc} · Lần khám {source.Anh.MaLS} · {source.Anh.NgayTao:dd/MM/yyyy HH:mm:ss}";
        if(!string.IsNullOrWhiteSpace(source.Anh.GhiChu))Caption.Text+="\n"+source.Anh.GhiChu;
        FullImage.Source=AnhXquangLanKham.Decode(source.NoiDung);
    }
}
