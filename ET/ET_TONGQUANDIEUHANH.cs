namespace ET;

/// <summary>Các chỉ số độc lập theo ngày nghiệp vụ, không chứa định danh cá nhân.</summary>
public sealed class ThongKeDieuHanh
{
    public int SoBenhNhan { get; set; }
    public int TongLuotKham { get; set; }
    public int TongLichHen { get; set; }
    public decimal TienThu { get; set; }
    public decimal TienHoan { get; set; }
    public decimal TienThucThu => TienThu-TienHoan;
    public int SoGiaoDich { get; set; }
    public int SoPhieuTiepNhan { get; set; }
    public int LuotKhamNhap { get; set; }
    public int LuotChuaPhanLoai { get; set; }
    public decimal[] ChiSo => [SoBenhNhan,TongLuotKham,TienThucThu,TongLichHen];
    public bool CoPhatSinh => SoBenhNhan>0||TongLuotKham>0||TongLichHen>0||SoGiaoDich>0||SoPhieuTiepNhan>0||LuotKhamNhap>0;
}

/// <summary>Trạng thái hiện tại của các lịch có ngày hẹn trong kỳ đang xem.</summary>
public sealed class DongTrangThaiDieuHanh
{
    public string TrangThai { get; set; } = "";
    public int SoLuong { get; set; }
}

/// <summary>Số lượt khám khác nhau có dịch vụ được xác nhận trong kỳ thực hiện.</summary>
public sealed class DongDichVuDieuHanh
{
    public string MaDV { get; set; } = "";
    public string TenDV { get; set; } = "";
    public int SoLuotKham { get; set; }
}

/// <summary>Gói tổng quan nhiều kỳ lấy trên một giao dịch; các bảng phụ thuộc toàn kỳ cần xem.</summary>
public sealed record GoiTongQuanDieuHanh(IReadOnlyDictionary<KySoSanh,ThongKeDieuHanh> CacKy,KySoSanh KyHienTai,
    DongTrangThaiDieuHanh[] TrangThaiLichHen,DongDichVuDieuHanh[] DichVu,DateTime ThoiDiemTai)
{
    public ThongKeDieuHanh HienTai => CacKy[KyHienTai];
    public bool CoPhatSinh => HienTai.CoPhatSinh||DichVu.Length>0;
}
