using System.ComponentModel;
using System.Data;
using System.Globalization;
using Dapper;

namespace DAL
{
    // Các DAL từng bảng dùng chung kết nối khi lưu một chứng từ nhiều dòng.
    public sealed class DAL_GIAODICH : dbconnect, IDisposable
    {
        public static readonly IReadOnlyDictionary<string, string> Tables = new Dictionary<string, string>
        {
            ["BENH_NHAN"]="MaBN", ["HO_SO_BENH_AN"]="MaHS", ["PHIEU_KHAM_BENH"]="MaPhieu",
            ["LICH_KHAM"]="MaLich", ["LICH_SU_KHAM_BENH"]="MaLS", ["TINH_TRANG_BENH"]="MaTTB",
            ["KE_HOACH_DIEU_TRI"]="MaKHDT", ["CHI_TIET_KE_HOACH"]="MaCTKH", ["DICH_VU_THUC_HIEN"]="MaDVThucHien",
            ["DON_THUOC"]="MaDT", ["CHI_TIET_DON_THUOC"]="MaCTDT", ["NHAN_VIEN"]="MaNV",
            ["PHONG"]="MaPhong", ["CA_LAM"]="MaCa", ["DICH_VU"]="MaDV", ["THUOC"]="MaThuoc", ["HANG_HOA"]="MaHH",
            ["HOA_DON"]="MaHD", ["CHI_TIET_HOA_DON"]="MaCTHD", ["THANH_TOAN"]="MaTT",
            ["PHIEU_XUAT"]="MaPX", ["CHI_TIET_PHIEU_XUAT"]="MaCTPX", ["KHO"]="MaKho", ["LO_HANG_HOA"]="MaLo"
        };
        private static readonly HashSet<string> ChungTu = new()
        { "PHIEU_KHAM_BENH", "LICH_KHAM", "LICH_SU_KHAM_BENH", "TINH_TRANG_BENH", "KE_HOACH_DIEU_TRI",
          "CHI_TIET_KE_HOACH", "DICH_VU_THUC_HIEN", "DON_THUOC", "CHI_TIET_DON_THUOC" };
        private readonly Dictionary<string, Column[]> columns = new();
        private IDbConnection? connection;
        private IDbTransaction? transaction;
        public bool DangGiaoDich => transaction is not null;
        private sealed class Column
        {
            public string Name { get; set; } = "";
            public string Type { get; set; } = "";
            public bool Nullable { get; set; }
            public bool Computed { get; set; }
            public int Length { get; set; }
            public int Scale { get; set; }
        }

        public void Begin()
        {
            if (connection is not null) throw new InvalidOperationException("Đang có giao dịch lưu dữ liệu.");
            connection = CreateConnection();
            try { connection.Open(); transaction = connection.BeginTransaction(IsolationLevel.Serializable); }
            catch { connection.Dispose(); connection=null; throw; }
        }
        public void Commit() { try { transaction?.Commit(); } finally { Dispose(); } }
        public void Rollback() { try { if(transaction?.Connection is not null) transaction.Rollback(); } finally { Dispose(); } }
        public void Dispose() { transaction?.Dispose(); transaction=null; connection?.Dispose(); connection=null; }

        public T ThucHien<T>(Func<IDbConnection, IDbTransaction?, T> action)
        {
            if (connection is not null) return action(connection, transaction);
            if (System.Transactions.Transaction.Current is not null)
            {
                using var current=CreateConnection(); current.Open(); return action(current, null);
            }
            Begin();
            try { var result=action(connection!,transaction); Commit(); return result; }
            catch { Rollback(); throw; }
        }

        public static string TaoMa(IDbConnection db, IDbTransaction? tx, string table, string key)
        {
            if (!Tables.TryGetValue(table,out var expectedKey) || expectedKey!=key)
                throw new InvalidOperationException("Bảng hoặc cột cấp mã không hợp lệ.");
            if (tx is null && System.Transactions.Transaction.Current is null)
                throw new InvalidOperationException("Cấp mã phải nằm trong giao dịch lưu bản ghi.");
            var locked=db.ExecuteScalar<int>("""
                DECLARE @result int;
                EXEC @result=sys.sp_getapplock @Resource=@Resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
                SELECT @result;
                """,new { Resource="RHM:Ma:"+table },tx);
            if (locked<0) throw new InvalidOperationException("Bảng đang được cấp mã ở cửa sổ khác. Vui lòng thử lại.");
            // Khóa cùng giao dịch với INSERT; hỗ trợ mã cũ BN001 và mã mới BN01.
            var highest=db.ExecuteScalar<long>($"SELECT COALESCE(MAX(TRY_CONVERT(bigint,SUBSTRING([{key}],PATINDEX('%[0-9]%',[{key}]+'0'),50))),0) FROM dbo.[{table}] WITH(UPDLOCK,HOLDLOCK)",transaction:tx);
            if (highest==long.MaxValue) throw new InvalidOperationException("Đã hết phạm vi số của mã dữ liệu.");
            return DinhDangMa(table,highest+1);
        }

        public static string DinhDangMa(string table,long number)
        {
            if(!Tables.ContainsKey(table) || number<=0) throw new InvalidOperationException("Bảng hoặc số thứ tự mã không hợp lệ.");
            return string.Concat(table.Split('_').Select(word=>word[0]))+number.ToString("D2",CultureInfo.InvariantCulture);
        }

        private Column[] Schema(IDbConnection db, IDbTransaction? tx, string table)
        {
            if (!Tables.ContainsKey(table)) throw new InvalidOperationException("Bảng dữ liệu không được phép truy cập.");
            if (!columns.TryGetValue(table,out var result))
            {
                result=db.Query<Column>("""
                    SELECT c.name AS Name,t.name AS Type,c.is_nullable AS Nullable,c.is_computed AS Computed,
                        c.max_length AS Length,c.scale AS Scale
                    FROM sys.columns c JOIN sys.types t ON t.user_type_id=c.user_type_id
                    WHERE c.object_id=OBJECT_ID(@Table) ORDER BY c.column_id
                    """,new {Table="dbo."+table},tx).ToArray();
                if (result.Length==0) throw new InvalidOperationException("Không tìm thấy bảng "+table+" trong database.");
                columns[table]=result;
            }
            return result;
        }
        private static object? Normalize(Column column, object? value)
        {
            if (value is DBNull) value=null;
            if (value is string text)
            {
                text=text.Trim(); if (text.Length==0 && column.Nullable) return null; value=text;
            }
            if (value is null)
            {
                if (!column.Nullable) throw new InvalidOperationException("Vui lòng nhập "+column.Name+".");
                return null;
            }
            try
            {
                if (column.Type=="int") return Convert.ToInt32(value,CultureInfo.CurrentCulture);
                if (column.Type is "decimal" or "numeric")
                {
                    var number=value is string numericText
                        ? decimal.Parse(numericText.Replace(',','.'),NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture)
                        : Convert.ToDecimal(value,CultureInfo.CurrentCulture);
                    if (decimal.Round(number,column.Scale)!=number) throw new InvalidOperationException(column.Name+" có quá nhiều chữ số thập phân.");
                    return number;
                }
                if (column.Type=="date") return Convert.ToDateTime(value,CultureInfo.CurrentCulture).Date;
                if (column.Type is "datetime2" or "datetime")
                {
                    var date=Convert.ToDateTime(value,CultureInfo.CurrentCulture);
                    var quantum=(long)Math.Pow(10,7-column.Scale);
                    return new DateTime((date.Ticks+quantum/2)/quantum*quantum,date.Kind);
                }
                if (column.Type=="time")
                {
                    var time=value is TimeSpan span?span:TimeSpan.Parse(Convert.ToString(value,CultureInfo.CurrentCulture)!,CultureInfo.CurrentCulture);
                    if(time<TimeSpan.Zero || time>=TimeSpan.FromDays(1)) throw new InvalidOperationException("Giờ phải từ 00:00 đến trước 24:00.");
                    var quantum=(long)Math.Pow(10,7-column.Scale);
                    return new TimeSpan((time.Ticks+quantum/2)/quantum*quantum%TimeSpan.TicksPerDay);
                }
                if (column.Type is "nvarchar" or "varchar" or "nchar" or "char")
                {
                    var stringValue=Convert.ToString(value,CultureInfo.CurrentCulture)??"";
                    var limit=column.Type.StartsWith('n')?column.Length/2:column.Length;
                    if (column.Length>0 && stringValue.Length>limit) throw new InvalidOperationException($"{column.Name} tối đa {limit} ký tự.");
                    return stringValue;
                }
                return value;
            }
            catch (Exception ex) when(ex is FormatException or OverflowException or InvalidCastException or ArgumentOutOfRangeException)
            { throw new InvalidOperationException("Giá trị "+column.Name+" không đúng kiểu dữ liệu.",ex); }
        }
        public static Dictionary<string,object?> GiaTri(object record) => TypeDescriptor.GetProperties(record)
            .Cast<PropertyDescriptor>().ToDictionary(property=>property.Name,property=>property.GetValue(record));

        public T ChuyenDoi<T>(IDictionary<string,object?> values) where T:new()
        {
            var record=new T();
            foreach(var pair in values)
            {
                var property=TypeDescriptor.GetProperties(record)[pair.Key]??throw new InvalidOperationException("Trường không có trong ET: "+pair.Key);
                var value=pair.Value is DBNull?null:pair.Value;
                var type=Nullable.GetUnderlyingType(property.PropertyType)??property.PropertyType;
                if (value is string empty && empty.Length==0 && Nullable.GetUnderlyingType(property.PropertyType) is not null) value=null;
                try
                {
                    if(value is not null && !type.IsInstanceOfType(value))
                        value=type==typeof(decimal) && value is string text
                            ?decimal.Parse(text.Replace(',','.'),NumberStyles.AllowDecimalPoint|NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture)
                            :type==typeof(TimeSpan)?TimeSpan.Parse(Convert.ToString(value)!,CultureInfo.CurrentCulture):Convert.ChangeType(value,type,CultureInfo.CurrentCulture);
                    property.SetValue(record,value);
                }
                catch(Exception ex) when(ex is FormatException or OverflowException or InvalidCastException)
                {throw new InvalidOperationException("Giá trị "+pair.Key+" không đúng kiểu dữ liệu.",ex);}
            }
            return record;
        }

        public void KiemTra<T>(IDbConnection db,IDbTransaction? tx,string table,T record,T? original,T? expected,bool statusOnly=false) where T:class
        {
            if(table=="LICH_SU_KHAM_BENH")
            {
                var fields=TypeDescriptor.GetProperties(record);
                var confirmed=Convert.ToString(fields["TrangThai"]!.GetValue(record))=="Đã xác nhận";
                fields["NgayXacNhan"]!.SetValue(record,confirmed?fields["NgayXacNhan"]!.GetValue(record)??DateTime.Now:null);
            }
            var schema=Schema(db,tx,table);var values=GiaTri(record);var prior=original is null?null:GiaTri(original);
            foreach(var column in schema.Where(c=>!c.Computed && c.Name!="TongTienDuKien" && values.ContainsKey(c.Name)))
            {
                var normalized=Normalize(column,values[column.Name]);values[column.Name]=normalized;
                TypeDescriptor.GetProperties(record)[column.Name]!.SetValue(record,normalized);
                if(prior is not null && expected is not null && !Equals(Normalize(column,GiaTri(expected)[column.Name]),prior[column.Name]))
                    throw new InvalidOperationException("Bản ghi đã thay đổi ở cửa sổ khác. Hãy tải lại trước khi lưu hoặc xóa.");
            }
            CheckState(db,tx,table,values,prior,statusOnly);
        }
        private static Dictionary<string,object?>? Get(IDbConnection db,IDbTransaction? tx,string table,string id)
            =>db.Query($"SELECT * FROM dbo.[{table}] WITH(UPDLOCK,HOLDLOCK) WHERE [{Tables[table]}]=@Id",new{Id=id},tx)
                .Select(row=>new Dictionary<string,object?>((IDictionary<string,object>)row)).SingleOrDefault();

        private static void CheckState(IDbConnection db,IDbTransaction? tx,string table,Dictionary<string,object?> values,Dictionary<string,object?>? original,bool statusOnly)
        {
            if(statusOnly && original is not null)
            {
                foreach(var pair in values)
                    if(pair.Key is not "TrangThai" and not "NgayXacNhan" and not "NgayCapNhat" and not "LyDoThayDoi"
                        && !Equals(pair.Value,original.GetValueOrDefault(pair.Key)))
                        throw new InvalidOperationException("Thao tác đổi trạng thái không được thay đổi nội dung bản ghi.");
            }
            var state=Convert.ToString(original?.GetValueOrDefault("TrangThai"))??"";
            if(original is not null && ChungTu.Contains(table))
            {
                if(table=="DICH_VU_THUC_HIEN" && state=="Đã xác nhận") throw new InvalidOperationException("Dịch vụ đã xác nhận được giữ nguyên.");
                var draft=state is "" or "Nháp" or "Chưa xác nhận" or "Chờ xác nhận" or "Đang lập" or "Dự kiến" or "Chờ thực hiện";
                if(!draft && (!statusOnly || table=="LICH_SU_KHAM_BENH")) throw new InvalidOperationException("Bản ghi đã xác nhận hoặc đã hủy; hãy lập bản mới.");
                if(!draft && Convert.ToString(values.GetValueOrDefault("TrangThai"))=="Nháp") throw new InvalidOperationException("Không chuyển chứng từ đã xác nhận về nháp.");
                if(table=="LICH_KHAM" && state is "Hủy" or "Đã đổi" or "Hoàn thành") throw new InvalidOperationException("Lịch đã kết thúc, hãy lập lịch mới.");
                if(table=="CHI_TIET_KE_HOACH" && db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.DICH_VU_THUC_HIEN WHERE MaCTKH=@Id AND TrangThai=N'Đã xác nhận'",new{Id=original["MaCTKH"]},tx)>0)
                    throw new InvalidOperationException("Bước kế hoạch đã có dịch vụ xác nhận, không thể thay đổi.");
            }
            var parent=table switch {"CHI_TIET_DON_THUOC"=>("DON_THUOC","MaDT"),"CHI_TIET_KE_HOACH"=>("KE_HOACH_DIEU_TRI","MaKHDT"),"TINH_TRANG_BENH"=>("LICH_SU_KHAM_BENH","MaLS"),_=>("","")};
            if(parent.Item1.Length>0)
            {
                var row=Get(db,tx,parent.Item1,Convert.ToString(values.GetValueOrDefault(parent.Item2))??"");
                if(row is null || Convert.ToString(row.GetValueOrDefault("TrangThai"))!="Nháp") throw new InvalidOperationException("Chỉ sửa nội dung trong bản nháp có liên kết hợp lệ.");
            }
            var owner=table switch {"HO_SO_BENH_AN"=>"MaBN","PHIEU_KHAM_BENH"=>"MaHS","LICH_SU_KHAM_BENH"=>"MaPhieu","DON_THUOC" or "KE_HOACH_DIEU_TRI" or "TINH_TRANG_BENH" or "DICH_VU_THUC_HIEN"=>"MaLS","CHI_TIET_DON_THUOC"=>"MaDT","CHI_TIET_KE_HOACH"=>"MaKHDT",_=>""};
            if(original is not null && owner.Length>0 && !Equals(values.GetValueOrDefault(owner),original.GetValueOrDefault(owner))) throw new InvalidOperationException("Không chuyển bản ghi đã lưu sang hồ sơ khác.");
        }
    }
}
