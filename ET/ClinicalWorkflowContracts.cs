namespace ET;

/// <summary>Phân biệt từ chối quyền với lỗi SQL/kết nối để không coi lỗi hệ thống là ca quyền đạt.</summary>
public sealed class TuChoiQuyenNghiepVuException : InvalidOperationException
{
    public TuChoiQuyenNghiepVuException()
        : base("Không tìm thấy dữ liệu trong phạm vi được phép hoặc tài khoản không còn quyền thao tác.") { }
}

public static class DinhDanhNghiepVu
{
    public const string KieuXacThuc = "RHM.Desktop";
    public const string MaNhanVienClaim = "RHM.MaNV";
}

/// <summary>Quyền nghiệp vụ cho sáu miền lâm sàng; độc lập với vai trò hệ thống Admin/User.</summary>
[Flags]
public enum QuyenLamViec
{
    Khong = 0,
    XemHoSo = 1,
    TiepNhan = 2,
    Kham = 4,
    CapNhatKetQuaDuocGiao = 8,
    ThuTien = 16,
    GiaoThuoc = 32,
    CauHinhHoTro = 64,
    DuyetMauDieuTri = 128,
    DuyetHuongDanThuoc = 256
}

/// <summary>Snapshot phục vụ UI. Endpoint phải xác minh lại bằng MaNV/MaTK trong SQL.</summary>
public sealed record NguCanhNghiepVu
{
    public string MaNV { get; init; } = "";
    public string MaTK { get; init; } = "";
    public string MaChucVu { get; init; } = "";
    public string? MaCK { get; init; }
    public bool LaAdmin { get; init; }
    public QuyenLamViec Quyen { get; init; }
    public bool CoQuyen(QuyenLamViec quyen) => (Quyen & quyen) == quyen;
}

public sealed record BoLocHangDoi
{
    public DateTime? TuNgay { get; init; }
    public DateTime? DenNgay { get; init; }
    public string? TrangThai { get; init; }
    public string? TuKhoa { get; init; }
    public int Trang { get; init; } = 1;
    public int KichThuocTrang { get; init; } = 50;
}

public sealed class HangDoiKhamItem
{
    public string MaPhieu { get; set; } = "";
    public string? MaLS { get; set; }
    public string MaHS { get; set; } = "";
    public string MaBN { get; set; } = "";
    public string HoTenBN { get; set; } = "";
    public DateTime? NgaySinh { get; set; }
    public string MaBS { get; set; } = "";
    public string MaPhong { get; set; } = "";
    public string? MaDV { get; set; }
    public int SoThuTu { get; set; }
    public DateTime ThoiGianTiepNhan { get; set; }
    public string TrangThai { get; set; } = "";
}

/// <summary>Chi tiết đã qua kiểm tra phạm vi, không phải entity bảng SQL.</summary>
public sealed record LanKhamChiTiet(ET_LICHSUKHAMBENH LanKham, string MaBN, string MaHS, bool DuocSua);

public sealed record DonThuocChiTiet(ET_DONTHUOC DonThuoc, IReadOnlyList<ET_CHITIETDONTHUOC> CacDong, string MaBN, bool DuocSua);

public sealed record DuLieuHoSoTheoQuyen(NguCanhNghiepVu NguoiDung, IReadOnlyDictionary<string, object[]> CacBang);
