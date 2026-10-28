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
    DuyetHuongDanThuoc = 256,
    DieuPhoiNgoaiLich = 512
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
    public string? MaBN { get; init; }
    public string? MaBS { get; init; }
    public string? MaPhieu { get; init; }
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
    public string TenBS { get; set; } = "";
    public string TenPhong { get; set; } = "";
    public string TenDV { get; set; } = "";
    public string LyDoKham { get; set; } = "";
    public string ThaoTac => MaLS is null ? "Bắt đầu khám" : "Khám tiếp";
}

/// <summary>Chi tiết đã qua kiểm tra phạm vi, không phải entity bảng SQL.</summary>
public sealed record LanKhamChiTiet(ET_LICHSUKHAMBENH LanKham, string MaBN, string MaHS, bool DuocSua);

public sealed record DonThuocChiTiet(ET_DONTHUOC DonThuoc, IReadOnlyList<ET_CHITIETDONTHUOC> CacDong, string MaBN, bool DuocSua);

public sealed record DuLieuHoSoTheoQuyen(NguCanhNghiepVu NguoiDung, IReadOnlyDictionary<string, object[]> CacBang);

public sealed class LoiTiepNhanException(string truong,string thongBao) : InvalidOperationException(thongBao)
{
    public string Truong { get; } = truong;
}

public sealed record YeuCauTiepNhan
{
    public string MaBN { get; init; } = "";
    public string MaDV { get; init; } = "";
    public string MaBS { get; init; } = "";
    public string MaPhong { get; init; } = "";
    public string? MaLich { get; init; }
    public string LyDoKham { get; init; } = "";
    public string? GhiChu { get; init; }
    public string? LyDoDieuPhoiNgoaiLich { get; init; }
}

public sealed class BacSiTiepNhan
{
    public string MaNV { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string MaCK { get; set; } = "";
    public string MaKhoa { get; set; } = "";
    public string? MaPhong { get; set; }
    public string? TenPhong { get; set; }
    public string? MaCa { get; set; }
    public int SoDangCho { get; set; }
    public bool ThieuLich { get; set; }
    public string TinhTrangLich => ThieuLich ? "Chưa có lịch làm việc để đối chiếu" : "Có lịch làm việc phù hợp";
    public string Label => $"{HoTen} ({MaNV}) · {TenPhong ?? "chọn phòng"} · {SoDangCho} đang chờ · {TinhTrangLich}";
}

public sealed record KetQuaTiepNhan(string MaBN,string MaHS,string MaPhieu,string MaBS,int SoThuTu,string TrangThai);
