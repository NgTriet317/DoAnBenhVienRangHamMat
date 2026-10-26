using System.Data;
using DAL;
using ET;
namespace BUS;

public sealed class BUS_THANHTOANTRONGOI
{
    private readonly DAL_THANHTOANTRONGOI dal;
    private readonly IDbTransaction? callerTransaction;
    public BUS_THANHTOANTRONGOI(IDbTransaction? transaction = null)
    { dal = new(transaction); callerTransaction = transaction; }
    public BangTinhPhi LapBangTinh(string maLS, string? maKhoCapThuoc, NguCanhNghiepVu nguoiDung)
        => dal.LapBangTinh(maLS, maKhoCapThuoc, nguoiDung);
    public KetQuaThanhToan ThanhToan(YeuCauThanhToan yeuCau, NguCanhNghiepVu nguoiDung)
    {
        if (callerTransaction is not null || System.Transactions.Transaction.Current is not null)
            throw new LoiKhamException("ThanhToan", "Thu trọn gói phải tự quản lý giao dịch để chỉ trả kết quả sau commit.");
        try
        {
            using var tx = dal.BatDauGiaoDich(); using var connection = tx.Connection!;
            try
            {
                var result = new DAL_THANHTOANTRONGOI(tx).ThanhToan(yeuCau, nguoiDung);
                tx.Commit(); return result;
            }
            catch
            {
                try { if (tx.Connection is not null) tx.Rollback(); } catch { /* Mất kết nối: thử lại cùng mã. */ }
                throw;
            }
        }
        catch (System.Data.Common.DbException ex)
        {
            throw new LoiKhamException("ThanhToan", "Chưa xác nhận được kết quả thanh toán. Thử lại cùng yêu cầu để đối chiếu kết quả đã lưu; không lập yêu cầu khác.", ex);
        }
    }
}
