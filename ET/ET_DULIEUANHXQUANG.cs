namespace ET;

public sealed class LoiAnhXquangException(string maLoi,string message,Exception? inner=null)
    : InvalidOperationException(message,inner)
{
    public string MaLoi { get; } = maLoi;
}

public sealed record DichVuAnhXquang(string MaDVThucHien,string TenDV,string MaNVThucHien);
public sealed record LuotAnhDuocGiao(string MaLS,string MaBN,string HoTenBN,DateTime ThoiGianKham);
public sealed record AnhXquangHienThi(ET_ANHXQUANGLANKHAM Anh,string NguoiTai,bool DuocAn);
public sealed record DanhSachAnhXquang(string MaLS,string MaBN,string HoTenBN,bool DuocTaiLen,
    IReadOnlyList<DichVuAnhXquang> DichVu,IReadOnlyList<AnhXquangHienThi> CacAnh,bool DuocPhanCong=false);
public sealed record NhanVienChupAnh(string MaNV,string HoTen);
public sealed record DuLieuAnhXquang(ET_ANHXQUANGLANKHAM Anh,byte[] NoiDung);
public sealed record AnhDaKiemTra(byte[] NoiDung,string MimeType,string PhanMoRong,string Sha256,int ChieuRong,int ChieuCao);
public sealed record SuCoKhoAnh(string Loai,string TenTep,string? MaAnh);
