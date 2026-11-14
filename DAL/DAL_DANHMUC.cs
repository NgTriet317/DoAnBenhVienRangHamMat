using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using ET;

namespace DAL;

/// <summary>
/// Đọc và cập nhật danh mục theo whitelist, kiểm quyền quản trị, phiên và xung đột trong giao dịch.
/// </summary>
public sealed class DAL_DANHMUC(IDbTransaction? transaction=null):dbconnect(transaction)
{
    private static string Cols(DinhNghiaDanhMuc d)=>string.Join(",",d.Khoa.Concat(d.Truong.Select(f=>f.Cot)).Append("TrangThai").Distinct().Select(c=>$"[{c}]"));
    private static string Where(DinhNghiaDanhMuc d)=>string.Join(" AND ",d.Khoa.Select(c=>$"[{c}]=@{c}"));
    private static Dictionary<string,object?> Copy(object row)=>((IDictionary<string,object>)row).ToDictionary(p=>p.Key,p=>(object?)p.Value);
    /// <summary>
    /// Băm dữ liệu theo thứ tự tên cột ổn định, bỏ trường DauVanTay để đối chiếu bản đã đọc với SQL.
    /// </summary>
    private static string Fingerprint(IDictionary<string,object?> row)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(row.Where(p=>p.Key!="DauVanTay").OrderBy(p=>p.Key).ToDictionary(p=>p.Key,p=>p.Value)))));

    /// <summary>
    /// Kiểm quyền quản trị rồi tìm theo khóa hoặc tên trên bảng được phép, gắn dấu vân tay cho mỗi bản ghi.
    /// </summary>
    public IReadOnlyList<Dictionary<string,object?>> Lay(string bang,string search,string status,NguCanhNghiepVu actor)=>ThucHien((db,tx)=>
    {
        DAL_NHATKY.Admin(db,tx,actor);
        var d=DanhMucHeThong.Lay(bang);
        var names=d.Truong.Where(f=>f.Cot.StartsWith("Ten",StringComparison.Ordinal)).Select(f=>f.Cot).Concat(d.Khoa);
        var match=string.Join(" OR ",names.Select(n=>$"[{n}] LIKE @search"));
        return (IReadOnlyList<Dictionary<string,object?>>)db.Query($"SELECT {Cols(d)} FROM dbo.[{d.Bang}] WHERE (@status=N'' OR TrangThai=@status) AND ({match}) ORDER BY {string.Join(",",d.Khoa.Select(k=>$"[{k}]"))}",new{search="%"+search+"%",status},tx)
            .Select(r=>{var row=Copy((object)r);row["DauVanTay"]=Fingerprint(row);return row;}).ToArray();
    });

    /// <summary>
    /// Chuẩn hóa và lưu danh mục dưới quyền quản trị, giữ khóa bản hiện tại và đối chiếu dấu vân tay trước khi sửa.
    /// </summary>
    /// <remarks>Đổi trạng thái chỉ dùng nội dung đang lưu trong SQL. Kiểm tra danh mục cha/con và chứng từ còn mở; tái sử dụng nhật ký trigger khi bảng đã có audit để tránh ghi trùng.</remarks>
    /// <param name="bang">Tên bảng thuộc whitelist DanhMucHeThong.</param>
    /// <param name="values">Dữ liệu nhập theo các trường được định nghĩa cho danh mục.</param>
    /// <param name="original">Bản gốc kèm DauVanTay khi cập nhật; null khi tạo mới.</param>
    /// <param name="actor">Ngữ cảnh cần xác thực lại với quyền quản trị.</param>
    /// <param name="session">Phiên còn hiệu lực của tài khoản thực hiện.</param>
    /// <param name="active">true để dùng lại, false để ngừng dùng, null để lưu nội dung.</param>
    /// <returns>Bản SQL sau khi lưu, có DauVanTay mới cho lần cập nhật kế tiếp.</returns>
    public Dictionary<string,object?> Luu(string bang,IDictionary<string,object?> values,IDictionary<string,object?>? original,
        NguCanhNghiepVu actor,Guid session,bool? active=null)=>ThucHien((db,tx)=>
    {
        var user=DAL_NHATKY.Admin(db,tx,actor);
        DAL_NHATKY.KiemTraPhien(db,tx,user.MaTK,session);
        var d=DanhMucHeThong.Lay(bang);
        var data=d.ChuanHoa(values);
        Dictionary<string,object?>? current=null;
        if(original is not null)
        {
            var keys=new DynamicParameters();foreach(var k in d.Khoa)keys.Add(k,original[k]);
            var row=db.QuerySingleOrDefault($"SELECT {Cols(d)} FROM dbo.[{d.Bang}] WITH(UPDLOCK,HOLDLOCK) WHERE {Where(d)}",keys,tx);
            if(row is null)throw new InvalidOperationException("Bản ghi không còn tồn tại. Hãy tải lại.");
            current=Copy((object)row);
            if(!original.TryGetValue("DauVanTay",out var expected)||Fingerprint(current)!=Convert.ToString(expected))
                throw new InvalidOperationException("Bản ghi đã được thay đổi bởi người khác. Hãy tải lại trước khi sửa.");
            foreach(var k in d.Khoa)
            {
                if(data.ContainsKey(k)&&!Equals(data[k],current[k]))throw new ArgumentException("Không được đổi khóa của bản ghi.");
                data[k]=current[k];
            }
            if(active.HasValue)
            {
                // Nút trạng thái không được vô tình lưu những thay đổi chưa lưu trên form.
                foreach(var f in d.Truong)data[f.Cot]=current[f.Cot];
            }
        }
        else
        {
            if(active.HasValue)throw new ArgumentException("Chọn bản ghi trước khi đổi trạng thái.");
            if(d.Khoa.Length==1)data[d.Khoa[0]]=TaoMa(db,tx,d.Bang,d.Khoa[0]);
        }
        data["TrangThai"]=active.HasValue ? (active.Value?d.DangDung:d.NgungDung) : current?.GetValueOrDefault("TrangThai")??d.DangDung;
        if(active.HasValue&&current is not null&&Equals(current["TrangThai"],data["TrangThai"]))
            throw new InvalidOperationException("Bản ghi đã ở trạng thái được yêu cầu.");
        bool isActive=Equals(data["TrangThai"],d.DangDung);
        foreach(var field in d.Truong.Where(f=>f.BangCha is not null))
        {
            var parent= DanhMucHeThong.Lay(field.BangCha!);
            if(db.ExecuteScalar<int>($"SELECT COUNT(*) FROM dbo.[{parent.Bang}] WITH(HOLDLOCK) WHERE [{parent.Khoa[0]}]=@id AND (@isActive=0 OR TrangThai=@status)",new{id=data[field.Cot],isActive,status=parent.DangDung},tx)!=1)
                throw new ArgumentException($"{field.Nhan}: danh mục cha không tồn tại hoặc đã ngừng sử dụng.");
        }
        // Không vô hiệu hóa cha đang có danh mục con còn dùng.
        if(active==false)
        {
            foreach(var child in DanhMucHeThong.TatCa)
            foreach(var fk in child.Truong.Where(f=>f.BangCha==d.Bang))
                if(db.ExecuteScalar<int>($"SELECT COUNT(*) FROM dbo.[{child.Bang}] WITH(UPDLOCK,HOLDLOCK) WHERE [{fk.Cot}]=@id AND TrangThai=@status",new{id=data[d.Khoa[0]],status=child.DangDung},tx)>0)
                    throw new InvalidOperationException($"Còn {child.Ten} đang sử dụng danh mục này. Hãy xử lý các mục con trước.");
            if(d.Bang is "PHONG" or "DICH_VU")
            {
                string key=d.Bang=="PHONG"?"MaPhong":"MaDV";
                if(db.ExecuteScalar<int>($"SELECT COUNT(*) FROM dbo.LICH_KHAM WITH(UPDLOCK,HOLDLOCK) WHERE [{key}]=@id AND TrangThai NOT IN(N'Đã hủy',N'Hủy',N'Đã đổi',N'Đã khám',N'Đã hoàn thành',N'Hoàn thành')",new{id=data[key]},tx)>0
                    ||db.ExecuteScalar<int>($"SELECT COUNT(*) FROM dbo.PHIEU_KHAM_BENH WITH(UPDLOCK,HOLDLOCK) WHERE [{key}]=@id AND TrangThai IN(N'Chờ khám',N'Đang khám',N'Chờ thanh toán',N'Chờ nhận thuốc')",new{id=data[key]},tx)>0)
                    throw new InvalidOperationException("Danh mục còn lịch hoặc phiếu khám chưa kết thúc.");
            }
        }
        if(d.Bang=="NGAY_NGHI_LE" && current is not null && Convert.ToDateTime(current["NgayNghi"])<=db.ExecuteScalar<DateTime>("SELECT CONVERT(date,SYSDATETIME())",transaction:tx))
            throw new InvalidOperationException("Ngày nghỉ đã qua cần được đối soát lương tại phân hệ chuyên trách trước khi thay đổi.");
        var name=d.Truong.FirstOrDefault(f=>f.Cot.StartsWith("Ten",StringComparison.Ordinal));
        if(name is not null)
        {
            var p=new DynamicParameters();p.Add("name",data[name.Cot]);
            string scope="";
            foreach(var f in d.Truong.Where(f=>f.BangCha is not null)){scope+=$" AND [{f.Cot}]=@{f.Cot}";p.Add(f.Cot,data[f.Cot]);}
            string exclude="";
            if(current is not null){exclude=" AND NOT ("+Where(d)+")";foreach(var k in d.Khoa)p.Add(k,data[k]);}
            if(db.ExecuteScalar<int>($"SELECT COUNT(*) FROM dbo.[{d.Bang}] WITH(UPDLOCK,HOLDLOCK) WHERE [{name.Cot}]=@name{scope}{exclude}",p,tx)>0)
                throw new ArgumentException("Tên danh mục đã tồn tại trong phạm vi này.");
        }
        var parameters=new DynamicParameters();foreach(var item in data)parameters.Add(item.Key,item.Value);
        string id=string.Join("/",d.Khoa.Select(k=>Convert.ToString(data[k],CultureInfo.InvariantCulture)));
        // Khóa ghép dài hơn 50 ký tự được định danh ổn định bằng SHA256 rút gọn; khóa thật vẫn nằm trong diff whitelist.
        if(id.Length>50)id=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id)))[..50];
        string action=current is null?"CREATE":active.HasValue?(active.Value?"REACTIVATE":"DEACTIVATE"):"UPDATE";
        // Hai danh mục đã có trigger audit: tái sử dụng hàng trigger vừa tạo thay vì thêm log trùng.
        bool triggerAudit=d.Bang is "DM_TINH_TRANG_BENH" or "NHOM_THUOC_KE_DON";
        var previousActor=db.ExecuteScalar<string?>("SELECT CONVERT(nvarchar(128),SESSION_CONTEXT(N'RHM.MaTK'))",transaction:tx);
        var priorLogs=triggerAudit?db.Query<string>("SELECT MaNK FROM dbo.NHAT_KY_THAO_TAC WITH(UPDLOCK,HOLDLOCK) WHERE LoaiDoiTuong=@table AND MaDoiTuong=@id",new{table=d.Bang,id},tx).ToHashSet():new HashSet<string>();
        try
        {
            if(triggerAudit)db.Execute("EXEC sys.sp_set_session_context @key=N'RHM.MaTK',@value=@account",new{account=user.MaTK},tx);
            if(current is null)
                db.Execute($"INSERT dbo.[{d.Bang}] ({string.Join(",",data.Keys.Select(k=>$"[{k}]"))}) VALUES({string.Join(",",data.Keys.Select(k=>$"@{k}"))})",parameters,tx);
            else
                db.Execute($"UPDATE dbo.[{d.Bang}] SET {string.Join(",",data.Keys.Except(d.Khoa).Select(k=>$"[{k}]=@{k}"))} WHERE {Where(d)}",parameters,tx);
            if(triggerAudit)
            {
                var log=db.Query<string>("SELECT MaNK FROM dbo.NHAT_KY_THAO_TAC WHERE LoaiDoiTuong=@table AND MaDoiTuong=@id",new{table=d.Bang,id},tx).Except(priorLogs).Single();
                db.Execute("UPDATE dbo.NHAT_KY_THAO_TAC SET MaPhien=@session,ThoiGianUtc=SYSUTCDATETIME(),MaSuKien=NEWID(),PhanHe=N'Quản lý danh mục hệ thống',HanhDong=@action WHERE MaNK=@log",new{session,action,log},tx);
            }
            else DAL_NHATKY.Ghi(db,tx,user.MaTK,session,action,"Quản lý danh mục hệ thống",d.Bang,id,
                current is null?null:JsonSerializer.Serialize(current),JsonSerializer.Serialize(data));
        }
        finally
        {
            if(triggerAudit)
            {
                try{db.Execute("EXEC sys.sp_set_session_context @key=N'RHM.MaTK',@value=@account",new{account=previousActor},tx?.Connection is null?null:tx);}
                catch{if(db is Microsoft.Data.SqlClient.SqlConnection c){Microsoft.Data.SqlClient.SqlConnection.ClearPool(c);c.Close();}throw;}
            }
        }
        var saved=Copy((object)db.QuerySingle($"SELECT {Cols(d)} FROM dbo.[{d.Bang}] WHERE {Where(d)}",parameters,tx));
        saved["DauVanTay"]=Fingerprint(saved);
        return saved;
    });
}
