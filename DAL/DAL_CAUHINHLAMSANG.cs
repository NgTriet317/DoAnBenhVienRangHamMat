using System.Data;
using System.Reflection;
using Dapper;
using Dapper.Contrib.Extensions;
using ET;
using Microsoft.Data.SqlClient;

namespace DAL;

/// <summary>Đọc/lưu cấu hình hỗ trợ trong cùng transaction với kiểm tra định danh và audit SQL.</summary>
public abstract class DAL_CAUHINHLAMSANG<T> : dbconnect where T : class, new()
{
    private static readonly Type[] Allowed = [typeof(ET_DMTINHTRANGBENH),typeof(ET_MAUKEHOACHDIEUTRI),typeof(ET_CHITIETMAUKEHOACH),typeof(ET_NHOMTHUOCKEDON),typeof(ET_CAUHINHTHUOCKEDON)];
    private static readonly string Table = Allowed.Contains(typeof(T)) ? typeof(T).GetCustomAttribute<TableAttribute>()!.Name : throw new InvalidOperationException("Entity ngoài hợp đồng cấu hình.");
    private static readonly PropertyInfo[] Columns = typeof(T).GetProperties();
    private static readonly PropertyInfo Key = Columns.Single(p => p.GetCustomAttribute<ExplicitKeyAttribute>() is not null);
    protected DAL_CAUHINHLAMSANG(IDbTransaction? transaction) : base(transaction) { }

    public IReadOnlyList<T> LayDanhSach(NguCanhNghiepVu nguoiDung) => ThucHien((db,tx) =>
    {
        var user=DAL_QUYENNGHIEPVU.XacThuc(db,tx,nguoiDung);
        if(!user.CoQuyen(QuyenLamViec.CauHinhHoTro) && !user.LaAdmin) throw DAL_QUYENNGHIEPVU.TuChoi();
        return (IReadOnlyList<T>)db.Query<T>($"SELECT * FROM dbo.[{Table}] ORDER BY [{Key.Name}]",transaction:tx).ToArray();
    });

    /// <summary>Chỉ lưu nội dung nháp. Phê duyệt mẫu/hướng dẫn phải qua đầu mối chuyên môn P05/P07.</summary>
    public bool LuuNhap(T duLieu,NguCanhNghiepVu nguoiDung,T? banGoc=null) => ThucHien((db,tx) =>
    {
        ArgumentNullException.ThrowIfNull(duLieu);
        var user=DAL_QUYENNGHIEPVU.XacThuc(db,tx,nguoiDung);
        if(!user.CoQuyen(QuyenLamViec.CauHinhHoTro) && !user.LaAdmin) throw DAL_QUYENNGHIEPVU.TuChoi();
        if(duLieu is ET_MAUKEHOACHDIEUTRI m && (m.TrangThai!="Nháp" || m.MaBSDuyet is not null || m.NgayDuyet is not null)
            || duLieu is ET_CAUHINHTHUOCKEDON c && (c.TrangThai!="Nháp" || c.MaNguoiDuyet is not null || c.NgayDuyet is not null))
            throw new InvalidOperationException("Chỉ lưu cấu hình nháp; phê duyệt qua thao tác chuyên môn riêng.");
        var id=(string?)Key.GetValue(duLieu);
        var current=string.IsNullOrWhiteSpace(id) ? null : db.QuerySingleOrDefault<T>($"SELECT * FROM dbo.[{Table}] WITH(UPDLOCK,HOLDLOCK) WHERE [{Key.Name}]=@id",new{id},tx);
        if(current is null)
        {
            if(typeof(T)==typeof(ET_CAUHINHTHUOCKEDON)) DAL_QUYENNGHIEPVU.KiemTraMa(id);
            else
            {
                if(!string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                Key.SetValue(duLieu,TaoMa(db,tx,Table,Key.Name));
            }
            if(banGoc is not null) throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
        }
        KiemTra(db,tx,Table,duLieu,current,banGoc);
        var sql=current is null
            ? $"INSERT dbo.[{Table}] ({string.Join(',',Columns.Select(p=>$"[{p.Name}]"))}) VALUES ({string.Join(',',Columns.Select(p=>"@"+p.Name))})"
            : $"UPDATE dbo.[{Table}] SET {string.Join(',',Columns.Where(p=>p!=Key).Select(p=>$"[{p.Name}]=@{p.Name}"))} WHERE [{Key.Name}]=@{Key.Name}";
        var previous=db.ExecuteScalar<string?>("SELECT CONVERT(nvarchar(128),SESSION_CONTEXT(N'RHM.MaTK'))",transaction:tx);
        var succeeded=false;
        try
        {
            db.Execute("EXEC sys.sp_set_session_context @key=N'RHM.MaTK',@value=@actor",new{actor=user.MaTK},tx);
            db.Execute(sql,duLieu,tx);
            succeeded=true;
            return true;
        }
        finally
        {
            try { db.Execute("EXEC sys.sp_set_session_context @key=N'RHM.MaTK',@value=@actor",new{actor=previous},tx?.Connection is null?null:tx); }
            catch
            {
                // Không để actor cũ lọt sang lệnh tiếp theo nếu kết nối/giao dịch bị hỏng.
                if(db is SqlConnection connection) { SqlConnection.ClearPool(connection); connection.Close(); }
                if(succeeded) throw;
            }
        }
    });
}

public sealed class DAL_DMTINHTRANGBENH(IDbTransaction? transaction=null) : DAL_CAUHINHLAMSANG<ET_DMTINHTRANGBENH>(transaction);
public sealed class DAL_MAUKEHOACHDIEUTRI(IDbTransaction? transaction=null) : DAL_CAUHINHLAMSANG<ET_MAUKEHOACHDIEUTRI>(transaction);
public sealed class DAL_CHITIETMAUKEHOACH(IDbTransaction? transaction=null) : DAL_CAUHINHLAMSANG<ET_CHITIETMAUKEHOACH>(transaction);
public sealed class DAL_NHOMTHUOCKEDON(IDbTransaction? transaction=null) : DAL_CAUHINHLAMSANG<ET_NHOMTHUOCKEDON>(transaction);
public sealed class DAL_CAUHINHTHUOCKEDON(IDbTransaction? transaction=null) : DAL_CAUHINHLAMSANG<ET_CAUHINHTHUOCKEDON>(transaction);

/// <summary>Bàn giao chỉ đọc trong P03; ghi nhận giao thuốc được triển khai ở P10.</summary>
public sealed class DAL_BANGIAOTHUOC(IDbTransaction? transaction=null) : dbconnect(transaction)
{
    public ET_BANGIAOTHUOC? LayTheoDon(string maDT,NguCanhNghiepVu nguoiDung) => ThucHien((db,tx) =>
    {
        new DAL_KHAMDIEUTRI(tx).LayDonThuoc(maDT,nguoiDung);
        return db.QuerySingleOrDefault<ET_BANGIAOTHUOC>("SELECT * FROM dbo.BAN_GIAO_THUOC WHERE MaDT=@maDT",new{maDT},tx);
    });
}
