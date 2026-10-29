using ET;
namespace BUS;
public partial class BUS_DONTHUOC
{
    public IReadOnlyList<ThuocKeDon> TimThuoc(BoLocThuoc filter,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.TimThuoc(filter,user));
    public IReadOnlyList<LuotKeDon> LayLuotKeDon(NguCanhNghiepVu user,string? patient=null)=>XuLyLoi.ThucHien(()=>dalDonThuoc.LayLuotKeDon(user,patient));
    public KhongGianKeDon LayKhongGianKeDon(string maLS,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.LayKhongGianKeDon(maLS,user));
    public BanNhapDon LayBanNhapDon(string id,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.LayBanNhapDon(id,user));
    public BanNhapDon LuuDon(YeuCauLuuDon r,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.LuuDon(r,user));
    public BanNhapDon XacNhanDon(string id,string token,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.XacNhanDon(id,token,user));
    public BanNhapDon HuyDonNhap(string id,string token,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.HuyDonNhap(id,token,user));
    public BanInDonThuoc LayBanInDon(string id,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.LayBanInDon(id,user));
    public bool KiemTraDonChoThu(string maLS,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.KiemTraDonChoThu(maLS,user));
    public BanNhapHuongDanThuoc? LayHuongDan(string drug,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.LayHuongDan(drug,user));
    public BanNhapHuongDanThuoc LuuHuongDan(ET_CAUHINHTHUOCKEDON config,string? token,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.LuuHuongDan(config,token,user));
    public BanNhapHuongDanThuoc DuyetHuongDan(string drug,string token,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>dalDonThuoc.DuyetHuongDan(drug,token,user));
}
