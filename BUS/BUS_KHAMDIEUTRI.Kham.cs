using ET;
namespace BUS;
public sealed partial class BUS_KHAMDIEUTRI
{
    public DuLieuKhongGianKham LayKhongGianKham(string maLS,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dal.LayKhongGianKham(maLS,user));
    public string BatDauKham(string maPhieu,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dal.BatDauKham(maPhieu,user));
    public BanNhapKham LayBanNhap(string maLS,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dal.LayBanNhap(maLS,user));
    public BanNhapKham LuuKham(YeuCauLuuKham request,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dal.LuuKham(request,user));
    public BanNhapKeHoach LayKeHoach(string maKHDT,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dal.LayKeHoach(maKHDT,user));
    public BanNhapKeHoach LuuKeHoach(YeuCauLuuKeHoach request,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dal.LuuKeHoach(request,user));
    public BanNhapKeHoach XacNhanKeHoach(string maKHDT,string dauVanTay,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dal.XacNhanKeHoach(maKHDT,dauVanTay,user));
    public BanNhapKeHoach HuyKeHoach(string maKHDT,string dauVanTay,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dal.HuyKeHoach(maKHDT,dauVanTay,user));
    public BanNhapKham HoanTatKham(string maLS,string dauVanTay,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dal.HoanTatKham(maLS,dauVanTay,user));
}
