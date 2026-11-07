namespace ET;

public enum CachSoSanh { Khong, ThangTruoc, CungKyNamTruoc, NamDuocChon }

/// <summary>Kỳ nửa mở, dùng thống nhất cho tất cả truy vấn thống kê.</summary>
public sealed record KySoSanh(DateTime TuNgay, DateTime DenExclusive)
{
    public string Nhan => $"{TuNgay:dd/MM/yyyy} – {DenExclusive.AddDays(-1):dd/MM/yyyy}";
}

public sealed record BoLocThongKe
{
    public DateTime TuThang { get; init; }
    public DateTime DenThang { get; init; }
    public CachSoSanh SoSanh { get; init; } = CachSoSanh.ThangTruoc;
    public bool LuyKeThang { get; init; }
    public int NamGoc { get; init; } = DateTime.Today.Year - 1;
    public string? MaCK { get; init; }
    public string? MaDV { get; init; }
}

public sealed record CapKyThongKe(KySoSanh HienTai, KySoSanh? KyGoc, bool LaLuyKe, string GhiChu);

public sealed record KetQuaSoSanh(decimal? HienTai, decimal? KyGoc, decimal? ChenhLech, decimal? PhanTram)
{
    public string DienGiai => HienTai is null || KyGoc is null ? "Không đủ dữ liệu"
        : KyGoc == 0 ? $"Δ {ChenhLech:+#,##0.##;-#,##0.##;0}; không tính % do kỳ gốc = 0"
        : $"{ChenhLech:+#,##0.##;-#,##0.##;0} ({PhanTram:+0.##;-0.##;0}%)";
}

/// <summary>SQL trả số tổng hợp; không có định danh bệnh nhân.</summary>
public sealed class DongThongKeBenh
{
    public string MaDMTT { get; set; } = "";
    public string TenTinhTrang { get; set; } = "";
    public int? SoLuotKham { get; set; }
    public int? SoBenhNhan { get; set; }
    public decimal? TyTrong { get; set; }
    public bool DaLamMo { get; set; }
    public KetQuaSoSanh? BienDong { get; set; }
    public string HienThiLuot => DaLamMo ? "Ẩn nhóm nhỏ" : SoLuotKham?.ToString("N0") ?? "—";
    public string HienThiBenhNhan => DaLamMo ? "Ẩn nhóm nhỏ" : SoBenhNhan?.ToString("N0") ?? "—";
    public string HienThiBienDong => BienDong?.DienGiai ?? "Không đủ dữ liệu";
}

public sealed class ChatLuongMaBenh
{
    public int TongLuot { get; set; }
    public int LuotCoMa { get; set; }
    public int LuotChuaPhanLoai { get; set; }
    public int DongChuaGanMa { get; set; }
    public int DongTrungMa { get; set; }
    public int SoBenhNhan { get; set; }
    public decimal? DoBaoPhu => TongLuot == 0 ? null : 100m * LuotCoMa / TongLuot;
    public string CanhBao => TongLuot == 0 ? "Không phát sinh lượt khám xác nhận."
        : LuotCoMa < TongLuot || DongChuaGanMa > 0 ? "Dữ liệu bệnh lý chưa đầy đủ"
        : "Các lượt khám đều có mã; danh mục chưa phân loại riêng bệnh/triệu chứng.";
    public string ThongBaoXepHang => TongLuot==0 ? "Không có lượt khám đã xác nhận trong kỳ."
        : LuotCoMa==0 ? $"Chưa thể xác định bệnh/tình trạng nào được khám nhiều nhất: cả {TongLuot:N0} lượt khám đều chưa được gắn mã danh mục. Đây là dữ liệu chưa phân loại, không phải không có bệnh."
        : LuotCoMa<TongLuot ? $"Xếp hạng chỉ phản ánh {LuotCoMa:N0}/{TongLuot:N0} lượt đã gắn mã; {LuotChuaPhanLoai:N0} lượt chưa phân loại có thể làm thay đổi thứ hạng."
        : "Xếp hạng theo mã đã được ghi nhận; một bệnh nhân có thể được tính ở nhiều tình trạng. Đây không phải tỷ lệ mắc trong cộng đồng.";
}

public sealed record KetQuaBenhLy(ChatLuongMaBenh ChatLuong, DongThongKeBenh[] CacBenh);
public sealed record LuaChonThongKe(string Ma,string Ten);

[Flags]
public enum QuyenBaoCao { Khong = 0, XemTongHop = 1, XemTaiChinhChiTiet = 2, XemLuong = 4, Xuat = 8, XemDuLieuLamSang = 16, XemKhoChiTiet = 32 }

/// <summary>Chính sách chuyên đề; DAL luôn xác thực lại chức vụ và vai trò trong SQL.</summary>
public static class ChinhSachBaoCao
{
    public static QuyenBaoCao TuVaiTro(NguCanhNghiepVu actor)
    {
        if (string.IsNullOrWhiteSpace(actor.MaTK) || string.IsNullOrWhiteSpace(actor.MaNV)) return QuyenBaoCao.Khong;
        if (actor.LaAdmin) return (QuyenBaoCao)63;
        var rights = QuyenBaoCao.XemTongHop | QuyenBaoCao.Xuat;
        if (actor.MaChucVu is "CV007" or "CV010") rights |= QuyenBaoCao.XemTaiChinhChiTiet;
        if (actor.MaChucVu == "CV010") rights |= QuyenBaoCao.XemLuong;
        if (actor.MaChucVu is "CV002" or "CV003") rights |= QuyenBaoCao.XemDuLieuLamSang;
        if (actor.MaChucVu is "CV005" or "CV006" or "CV010") rights |= QuyenBaoCao.XemKhoChiTiet;
        return rights;
    }
    public static bool ChoPhep(string ma, QuyenBaoCao rights) => ma switch
    {
        "11" or "18" => rights.HasFlag(QuyenBaoCao.XemLuong),
        _ => rights.HasFlag(QuyenBaoCao.XemTongHop)
    };
    public static bool ChoPhepChiTiet(string ma,QuyenBaoCao rights) => ChoPhep(ma,rights) && (ma switch
    {
        "09" or "10" => rights.HasFlag(QuyenBaoCao.XemTaiChinhChiTiet),
        "11" or "18" => rights.HasFlag(QuyenBaoCao.XemLuong),
        "13" or "14" => rights.HasFlag(QuyenBaoCao.XemKhoChiTiet),
        _ => false
    });
}
