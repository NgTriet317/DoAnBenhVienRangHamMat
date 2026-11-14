using System;

namespace ET;

public class ET_AI_DANHMUC
{
    public string Ma { get; set; } = "";
    public string Ten { get; set; } = "";
    public override string ToString() => Ten;
}

public class ET_AI_BENHNHANTIM
{
    public string MaBN { get; set; } = "";
    public string HoTenBN { get; set; } = "";
    public DateTime? NgaySinh { get; set; }
    public string? SDT { get; set; }
    // 2: khop du ho ten + ngay sinh + SDT; 1: co dau hieu nghi trung.
    public int MucDoKhop { get; set; }
    public string HienThi => $"{MaBN} - {HoTenBN} - {NgaySinh:dd/MM/yyyy} - {SDT}";
}

public class ET_AI_TAOPHIEU
{
    public Guid MaPhien { get; set; }
    public string MaNV { get; set; } = "";
    public string? MaBNHienCo { get; set; }
    public string HoTen { get; set; } = "";
    public DateTime NgaySinh { get; set; }
    public string SDT { get; set; } = "";
    public string LyDoKham { get; set; } = "";
    public string LoaiKham { get; set; } = "ThongThuong";
    public string MaPhong { get; set; } = "";
    public string? MaBS { get; set; }
    public string? MaDV { get; set; }
}

public class ET_AI_KETQUATAO
{
    public string MaBN { get; set; } = "";
    public string MaHS { get; set; } = "";
    public string MaPhieu { get; set; } = "";
    public int SoThuTu { get; set; }
}
