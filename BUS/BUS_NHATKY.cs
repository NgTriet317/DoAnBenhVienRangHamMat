using DAL;
using ET;

namespace BUS;

public sealed class BUS_NHATKY
{
    private readonly DAL_NHATKY dal;
    public BUS_NHATKY(System.Data.IDbTransaction? transaction=null)=>dal=new(transaction);
    public void GhiDangNhapThatBai(string account)=>dal.GhiDangNhapThatBai(account);
    public void GhiThayDoi(string table,string id,string action,IEnumerable<string> fields,NguCanhNghiepVu actor,Guid session)
        =>dal.GhiThayDoi(table,id,action,fields,actor,session);
    public Guid BatDauPhien(NguCanhNghiepVu actor)=>dal.BatDauPhien(actor);
    public void Heartbeat(Guid session,NguCanhNghiepVu actor)=>dal.Heartbeat(session,actor);
    public void KetThucPhien(Guid session)=>dal.KetThucPhien(session);
    public TrangNhatKy LayTrang(BoLocNhatKy filter,NguCanhNghiepVu actor,bool sessions=false)=>dal.LayTrang(filter,actor,sessions);
    public (int DangNhap,int DangNhapThatBai,int ThayDoi) ThongKe(NguCanhNghiepVu actor)=>dal.ThongKe(actor);
}
