using System.Data;
using Dapper;
using ET;
using System.Globalization;

namespace DAL;

public sealed partial class DAL_QUYENNGHIEPVU
{
    private static readonly IReadOnlyDictionary<string, (string Key, string Parent)> ClinicalTables =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            ["BENH_NHAN"] = ("MaBN", ""), ["HO_SO_BENH_AN"] = ("MaHS", "MaBN"),
            ["PHIEU_KHAM_BENH"] = ("MaPhieu", "MaHS"), ["LICH_SU_KHAM_BENH"] = ("MaLS", "MaPhieu"),
            ["TINH_TRANG_BENH"] = ("MaTTB", "MaLS"), ["KE_HOACH_DIEU_TRI"] = ("MaKHDT", "MaLS"),
            ["CHI_TIET_KE_HOACH"] = ("MaCTKH", "MaKHDT"), ["DICH_VU_THUC_HIEN"] = ("MaDVThucHien", "MaLS"),
            ["DON_THUOC"] = ("MaDT", "MaLS"), ["CHI_TIET_DON_THUOC"] = ("MaCTDT", "MaDT")
        };

    public void KiemTraGhiHoSo(string bang, IDictionary<string, object?> giaTri, string? ma, bool xoa, NguCanhNghiepVu nguoiDung)
    {
        if (callerTransaction is null && System.Transactions.Transaction.Current is null)
            throw new InvalidOperationException("Guard ghi hồ sơ phải dùng cùng giao dịch với lệnh ghi.");
        ThucHien((db, tx) =>
        {
            var user = XacThuc(db, tx, nguoiDung);
            if (!ClinicalTables.TryGetValue(bang, out var metadata)) throw TuChoi();
            if (giaTri is null || (xoa && ma is null)) throw TuChoi();
            var original = ma is null ? null : ReadRow(db, tx, bang, ma);
            if (ma is not null && original is null) throw TuChoi();
            if (original is not null && metadata.Parent.Length > 0 && giaTri.TryGetValue(metadata.Parent, out var parent)
                && Text(parent) != Text(original.GetValueOrDefault(metadata.Parent))) throw TuChoi();
            if (original is not null && giaTri.TryGetValue(metadata.Key, out var suppliedId) && Text(suppliedId) != ma) throw TuChoi();
            var values = original is null ? new Dictionary<string, object?>() : new Dictionary<string, object?>(original);
            foreach (var pair in giaTri) values[pair.Key] = pair.Value;

            if (bang is "BENH_NHAN" or "HO_SO_BENH_AN" or "PHIEU_KHAM_BENH")
            {
                if (!user.CoQuyen(QuyenLamViec.TiepNhan)) throw TuChoi();
                if (bang == "PHIEU_KHAM_BENH")
                {
                    if (original is not null && db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.LICH_SU_KHAM_BENH WITH(UPDLOCK,HOLDLOCK) WHERE MaPhieu=@ma", new { ma }, tx) != 0) throw TuChoi();
                    if (values.TryGetValue("MaNVTiepNhan", out var receiver) && Text(receiver) != user.MaNV) throw TuChoi();
                }
                return true;
            }

            string visit;
            if (bang == "LICH_SU_KHAM_BENH")
            {
                var receipt = ReadRow(db, tx, "PHIEU_KHAM_BENH", Text(values.GetValueOrDefault("MaPhieu"))) ?? throw TuChoi();
                if (!user.CoQuyen(QuyenLamViec.Kham) || Text(receipt.GetValueOrDefault("MaBS")) != user.MaNV ||
                    Text(values.GetValueOrDefault("MaBS")) != user.MaNV || Text(receipt.GetValueOrDefault("TrangThai")) is not ("Chờ khám" or "Đang khám")) throw TuChoi();
                if (original is not null && Text(original.GetValueOrDefault("TrangThai")) != "Nháp") throw TuChoi();
                return true;
            }
            if (bang is "CHI_TIET_DON_THUOC" or "CHI_TIET_KE_HOACH")
            {
                var parentTable = bang == "CHI_TIET_DON_THUOC" ? "DON_THUOC" : "KE_HOACH_DIEU_TRI";
                var header = ReadRow(db, tx, parentTable, Text(values.GetValueOrDefault(metadata.Parent))) ?? throw TuChoi();
                if (Text(header.GetValueOrDefault("TrangThai")) != "Nháp" || Text(header.GetValueOrDefault("MaBS")) != user.MaNV) throw TuChoi();
                visit = Text(header.GetValueOrDefault("MaLS"));
            }
            else visit = Text(values.GetValueOrDefault("MaLS"));

            if (bang == "DICH_VU_THUC_HIEN" && user.CoQuyen(QuyenLamViec.CapNhatKetQuaDuocGiao) && !user.CoQuyen(QuyenLamViec.Kham))
            {
                if (original is null || xoa || Text(original.GetValueOrDefault("MaNVThucHien")) != user.MaNV || Text(original.GetValueOrDefault("TrangThai")) != "Nháp") throw TuChoi();
                foreach (var pair in giaTri)
                    if (pair.Key is not ("KetQua" or "ThoiGianThucHien") && !SameStoredValue(pair.Value, original.GetValueOrDefault(pair.Key))) throw TuChoi();
                var parentVisit = ReadRow(db, tx, "LICH_SU_KHAM_BENH", visit) ?? throw TuChoi();
                if (Text(parentVisit.GetValueOrDefault("TrangThai")) != "Nháp") throw TuChoi();
                return true;
            }
            KiemTraQuyenGhiLanKham(visit, user);
            if (original is not null && original.TryGetValue("TrangThai", out var state) && Text(state) != "Nháp") throw TuChoi();
            if (bang is "DON_THUOC" or "KE_HOACH_DIEU_TRI")
                if (Text(values.GetValueOrDefault("MaBS")) != user.MaNV) throw TuChoi();
            return true;
        });
    }

    private static Dictionary<string, object?>? ReadRow(IDbConnection db, IDbTransaction? tx, string table, string id)
    {
        KiemTraMa(id);
        if (!ClinicalTables.TryGetValue(table, out var metadata)) throw TuChoi();
        var row = db.QuerySingleOrDefault($"SELECT * FROM dbo.[{table}] WITH(UPDLOCK,HOLDLOCK) WHERE [{metadata.Key}]=@id", new { id }, tx);
        return row is null ? null : ((IDictionary<string, object>)row).ToDictionary(p => p.Key, p => p.Value is DBNull ? null : (object?)p.Value);
    }
    private static string Text(object? value) => Convert.ToString(value) ?? "";

    private static bool SameStoredValue(object? input, object? stored)
    {
        if (input is DBNull) input = null;
        if (stored is null) return input is null || input is string text && string.IsNullOrWhiteSpace(text);
        if (input is string value)
        {
            if (stored is decimal number) return decimal.TryParse(value.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsed) && parsed == number;
            if (stored is int integer) return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed == integer;
            if (stored is string original) return value.Trim() == original;
        }
        return Equals(input, stored);
    }
}
