using System.Data;
using DAL;
using ET;
namespace BUS;

public sealed class BUS_BANGIAOTHUOC
{
    private readonly DAL_BANGIAOTHUOC dal;
    private readonly IDbTransaction? callerTransaction;
    public BUS_BANGIAOTHUOC(IDbTransaction? transaction=null){dal=new(transaction);callerTransaction=transaction;}
    public ET_BANGIAOTHUOC? LayTheoDon(string maDT,NguCanhNghiepVu nguoiDung)=>XuLyLoi.ThucHien(()=>dal.LayTheoDon(maDT,nguoiDung));
    public HangDoiBanGiao LayHangDoi(BoLocBanGiao filter,NguCanhNghiepVu user)=>dal.LayHangDoi(filter,user);
    public ChiTietBanGiao LayChiTiet(string maDT,NguCanhNghiepVu user)=>dal.LayChiTiet(maDT,user);
    public KetQuaBanGiao XacNhanGiao(string maDT,NguCanhNghiepVu user)
    {
        if(callerTransaction is not null || System.Transactions.Transaction.Current is not null)
            throw new LoiKhamException("BanGiao","Bàn giao phải tự quản lý giao dịch và chỉ trả kết quả sau commit.");
        try
        {
            using var tx=dal.BatDauGiaoDich();using var connection=tx.Connection!;
            try{var result=new DAL_BANGIAOTHUOC(tx).XacNhanGiao(maDT,user);tx.Commit();return result;}
            catch{try{if(tx.Connection is not null)tx.Rollback();}catch{}throw;}
        }
        catch(System.Data.Common.DbException ex){throw new LoiKhamException("BanGiao","Chưa xác nhận được kết quả giao. Thử lại đúng đơn để đối chiếu; không xuất thuốc lần nữa.",ex);}
    }
}
