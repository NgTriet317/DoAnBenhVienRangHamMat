using ET;
namespace BUS;
public partial class BUS_DICHVUTHUCHIEN
{
    public IReadOnlyList<LuotThucHien> LayLuotThucHien(NguCanhNghiepVu user,string? maBN=null)=>XuLyLoi.ThucHien(()=>dalDichVuThucHien.LayLuotThucHien(user,maBN));
    public KhongGianThucHien LayKhongGianThucHien(string maLS,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDichVuThucHien.LayKhongGianThucHien(maLS,user));
    public BanNhapThucHien LayThucHien(string id,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDichVuThucHien.LayThucHien(id,user));
    public BanNhapThucHien LuuThucHien(YeuCauThucHienDichVu request,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDichVuThucHien.LuuThucHien(request,user));
    public BanNhapThucHien XacNhanThucHien(string id,string dauVanTay,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDichVuThucHien.XacNhanThucHien(id,dauVanTay,user));
    public BanNhapThucHien HuyThucHien(string id,string dauVanTay,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDichVuThucHien.HuyThucHien(id,dauVanTay,user));
}
