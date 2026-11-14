using System.Data;
using Dapper;
using ET;

namespace DAL;

/// <summary>Đối soát vận hành chỉ SELECT. Một giao dịch Serializable giữ số đếm và chi tiết nhất quán.</summary>
public sealed partial class DAL_DOISOATDULIEU(IDbTransaction? transaction=null):dbconnect(transaction)
{
    /// <summary>
    /// Kiểm kê bảng và chạy các quy tắc khóa ngoại, lâm sàng, tài chính, kho trên schema thực tế.
    /// </summary>
    /// <remarks>Chỉ đọc dữ liệu trong giao dịch của ThucHien; các quy tắc tùy chọn chỉ chạy khi schema có cột tương ứng.</remarks>
    /// <param name="gioiHanMoiQuyTac">Số dòng chi tiết tối đa cho mỗi quy tắc, từ 1 đến 500; tổng vi phạm vẫn được đếm đầy đủ.</param>
    /// <returns>Số dòng từng bảng và các vi phạm phân theo lỗi xác định hoặc trường hợp cần đối chiếu.</returns>
    public KetQuaDoiSoatDuLieu KiemTra(int gioiHanMoiQuyTac=200)
    {
        if(gioiHanMoiQuyTac is <1 or >500)throw new ArgumentOutOfRangeException(nameof(gioiHanMoiQuyTac));
        return ThucHien((db,tx)=>
        {
            var start=DateTime.UtcNow;
            var tables=db.Query<(string Schema,string Name)>("SELECT SCHEMA_NAME(schema_id),name FROM sys.tables WHERE is_ms_shipped=0 ORDER BY schema_id,name",transaction:tx).ToArray();
            var columns=db.Query<string>("SELECT SCHEMA_NAME(t.schema_id)+N'.'+t.name+N'.'+c.name FROM sys.tables t JOIN sys.columns c ON c.object_id=t.object_id WHERE t.is_ms_shipped=0",transaction:tx).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if(!columns.Contains("dbo.LICH_SU_KHAM_BENH.MaLS")||!columns.Contains("dbo.HOA_DON.MaHD"))throw new InvalidOperationException("Database không có schema bệnh viện được hỗ trợ.");
            var counts=tables.Select(t=>new ThongKeBangDoiSoat(t.Schema,t.Name,db.ExecuteScalar<long>($"SELECT COUNT_BIG(*) FROM {Q(t.Schema)}.{Q(t.Name)}",transaction:tx))).ToArray();
            var rules=new List<QuyTacDoiSoat>();
            void Add(string ma,string mucDo,string moTa,string sql,object? parameters=null)=>AddRule(db,tx,rules,gioiHanMoiQuyTac,ma,mucDo,moTa,sql,parameters);
            bool Has(string name)=>columns.Contains("dbo."+name);
            Add("SCHEMA_PHIENBAN","CanDoiChieu","Schema P1/P4 chưa được cài; chỉ kiểm các cột/bảng hiện có, không tự nâng cấp.","""
                SELECT N'PHIEU_KHAM_BENH' Bang,N'LoaiKham/PhiPhuThuDichVu' Khoa,N'Sprint3Foundation1' LienQuan
                WHERE COL_LENGTH(N'dbo.PHIEU_KHAM_BENH',N'LoaiKham') IS NULL OR COL_LENGTH(N'dbo.PHIEU_KHAM_BENH',N'PhiPhuThuDichVu') IS NULL
                UNION ALL SELECT N'ANH_XQUANG_LAN_KHAM',N'MaAnh',N'Sprint3Foundation1' WHERE OBJECT_ID(N'dbo.ANH_XQUANG_LAN_KHAM',N'U') IS NULL
                UNION ALL SELECT N'PHAN_CONG_ANH_XQUANG',N'MaPC',N'XrayStorage1' WHERE OBJECT_ID(N'dbo.PHAN_CONG_ANH_XQUANG',N'U') IS NULL
                """);
            Add("SCHEMA_RANG_BUOC","SaiXacDinh","Ràng buộc bị tắt/chưa trusted hoặc trigger bị tắt; cần đối chiếu trước sửa dữ liệu.","""
                SELECT OBJECT_NAME(parent_object_id) Bang,name Khoa,N'FOREIGN KEY' LienQuan FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1
                UNION ALL SELECT OBJECT_NAME(parent_object_id),name,N'CHECK' FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1
                UNION ALL SELECT OBJECT_NAME(parent_id),name,N'TRIGGER' FROM sys.triggers WHERE parent_class=1 AND is_ms_shipped=0 AND is_disabled=1
                """);
            var keys=db.Query<FkColumn>("""
                SELECT fk.object_id Id,fk.name Ten,SCHEMA_NAME(child.schema_id) LuocDo,child.name Bang,cc.name Cot,
                    SCHEMA_NAME(parent.schema_id) LuocDoDich,parent.name BangDich,pc.name CotDich
                FROM sys.foreign_keys fk JOIN sys.foreign_key_columns k ON k.constraint_object_id=fk.object_id
                JOIN sys.tables child ON child.object_id=k.parent_object_id JOIN sys.tables parent ON parent.object_id=k.referenced_object_id
                JOIN sys.columns cc ON cc.object_id=k.parent_object_id AND cc.column_id=k.parent_column_id
                JOIN sys.columns pc ON pc.object_id=k.referenced_object_id AND pc.column_id=k.referenced_column_id
                WHERE child.is_ms_shipped=0 ORDER BY fk.object_id,k.constraint_column_id
                """,transaction:tx).GroupBy(x=>x.Id);
            foreach(var key in keys)
            {
                var f=key.First();var full=Q(f.LuocDo)+"."+Q(f.Bang);
                var pk=db.Query<string>("""
                    SELECT c.name FROM sys.indexes i JOIN sys.index_columns k ON k.object_id=i.object_id AND k.index_id=i.index_id
                    JOIN sys.columns c ON c.object_id=k.object_id AND c.column_id=k.column_id
                    WHERE i.object_id=OBJECT_ID(@full) AND i.is_primary_key=1 ORDER BY k.key_ordinal
                    """,new{full},tx).ToArray();
                // MATCH SIMPLE: FK ghép có bất kỳ cột NULL nào không phải bản ghi mồ côi.
                var nonNull=string.Join(" AND ",key.Select(k=>"c."+Q(k.Cot)+" IS NOT NULL"));
                var equal=string.Join(" AND ",key.Select(k=>"p."+Q(k.CotDich)+"=c."+Q(k.Cot)));
                var identity=pk.Length==0?"N'[Không có PK]'":"(SELECT "+string.Join(",",pk.Select(k=>"c."+Q(k)))+" FOR JSON PATH,WITHOUT_ARRAY_WRAPPER,INCLUDE_NULL_VALUES)";
                Add("FK:"+f.Ten,"SaiXacDinh","Tham chiếu mồ côi theo FK thực trong sys.foreign_keys.",
                    $"SELECT @bang Bang,{identity} Khoa,@dich LienQuan FROM {full} c WHERE {nonNull} AND NOT EXISTS(SELECT 1 FROM {Q(f.LuocDoDich)}.{Q(f.BangDich)} p WHERE {equal})",new{bang=f.LuocDo+"."+f.Bang,dich=f.LuocDoDich+"."+f.BangDich});
            }
            ClinicalRules(Add,Has);
            FinanceRules(Add,Has);
            InventoryRules(Add);
            return new KetQuaDoiSoatDuLieu(db.Database,start,DateTime.UtcNow,counts,rules);
        });
    }
    private delegate void AddCheck(string ma,string mucDo,string moTa,string sql,object? parameters=null);
    /// <summary>
    /// Đặt tên schema, bảng hoặc cột đọc từ metadata trong ngoặc vuông và escape dấu đóng ngoặc khi dựng SQL.
    /// </summary>
    private static string Q(string name)=>"["+name.Replace("]","]]")+"]";
    /// <summary>
    /// Đếm toàn bộ vi phạm và lấy chi tiết theo giới hạn trong cùng giao dịch đối soát.
    /// </summary>
    /// <remarks>Sắp xếp chi tiết theo bảng, khóa và liên quan để kết quả lấy mẫu ổn định; không sửa dữ liệu vi phạm.</remarks>
    private static void AddRule(IDbConnection db,IDbTransaction? tx,List<QuyTacDoiSoat> rules,int limit,string ma,string severity,string description,string sql,object? parameters)
    {
        var args=new DynamicParameters(parameters);args.Add("take",limit);
        var count=db.ExecuteScalar<long>($"SELECT COUNT_BIG(*) FROM ({sql}) q",args,tx,commandTimeout:60);
        var details=count==0?[]:db.Query<ChiTietDoiSoat>($"SELECT TOP(@take) Bang,Khoa,LienQuan FROM ({sql}) q ORDER BY Bang,Khoa,LienQuan",args,tx,commandTimeout:60).ToArray();
        rules.Add(new(ma,severity,description,count,details));
    }
    /// <summary>
    /// Ánh xạ một cặp cột khóa ngoại từ metadata SQL để ghép điều kiện đối chiếu cả khóa đơn và khóa ghép.
    /// </summary>
    private sealed class FkColumn
    {
        public int Id{get;set;}public string Ten{get;set;}="";public string LuocDo{get;set;}="";public string Bang{get;set;}="";public string Cot{get;set;}="";
        public string LuocDoDich{get;set;}="";public string BangDich{get;set;}="";public string CotDich{get;set;}="";
    }
}
