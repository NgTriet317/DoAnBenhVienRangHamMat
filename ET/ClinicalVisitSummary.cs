namespace ET;

public sealed record DichVuTongKetKham(string MaDVThucHien,string MaDV,string TenDV,
    decimal SoLuong,decimal DonGia,decimal ThanhTien,DateTime ThoiGianThucHien,string? KetQua);
public sealed record ThuocTongKetKham(string MaDT,string MaCTDT,string MaThuoc,string TenThuoc,
    string HamLuong,string DonViTinh,decimal SoLuong,string LieuDung,string? DuongDung,
    int SoNgayDung,string HuongDan,decimal DonGia)
{
    public decimal ThanhTien=>decimal.Round(SoLuong*DonGia,2,MidpointRounding.AwayFromZero);
}
public sealed record TongKetKham(IReadOnlyList<DichVuTongKetKham> DichVu,
    IReadOnlyList<ThuocTongKetKham> Thuoc,bool DaThucHien,int SoDonNhap,string DauVanTay)
{
    public decimal TienDichVu=>DichVu.Sum(x=>x.ThanhTien);
    public decimal TienThuoc=>Thuoc.Sum(x=>x.ThanhTien);
}
