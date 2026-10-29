// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Text;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.SqlClient;

namespace DAL
{
    // Hạ tầng SQL; quy tắc nghiệp vụ thuộc DAL/BUS từng thực thể.
    /// <summary>
    /// Hạ tầng SQL dùng chung: mở kết nối/giao dịch, cấp mã có khóa, đọc metadata, chuẩn hóa ET và phát hiện sửa đồng thời. AI hỗ trợ chú thích, cấu hình và luồng hạ tầng trong phạm vi yêu cầu.
    /// </summary>
    public class dbconnect
    {
        /// <summary>
        /// Giao dịch được nơi gọi cấp cho đối tượng DAL; giữ nguyên kết nối và quyền commit/rollback của nơi gọi.
        /// </summary>
        private readonly IDbTransaction? transaction;
        /// <summary>
        /// Bộ nhớ đệm metadata cột theo tên bảng, chỉ sống trong đối tượng hiện tại để tránh đọc lại sys.columns mỗi lần kiểm tra ET.
        /// </summary>
        private readonly Dictionary<string, Column[]> columns = new();
        /// <summary>
        /// Giữ transaction tùy chọn của nơi gọi; các DAL kế thừa có thể dùng chung kết nối/giao dịch khi lưu nhiều thực thể.
        /// </summary>
        /// <param name = "transaction">Giao dịch đang mở do nơi gọi quản lý; null để ThucHien dùng giao dịch môi trường hoặc tự tạo giao dịch cục bộ.</param>
        public dbconnect(IDbTransaction? transaction = null)
        {
            this.transaction = transaction;
        }

        /// <summary>
        /// Tạo SqlConnection từ biến môi trường RHM_CONNECTION_STRING hoặc chuỗi mặc định; chỉ tạo đối tượng, chưa mở kết nối.
        /// </summary>
        /// <returns>Kết nối SQL chưa mở; nơi sử dụng chịu trách nhiệm mở và giải phóng.</returns>
        protected IDbConnection CreateConnection()
        {
            string? chuoiKetNoi = Environment.GetEnvironmentVariable("RHM_CONNECTION_STRING");
            if (chuoiKetNoi is null)
            {
                chuoiKetNoi = @"Data Source=DESKTOP-C9VCE8A;Initial Catalog=QLBenhVienRangHamMat;Integrated Security=True;TrustServerCertificate=True;";
            }

            return new SqlConnection(chuoiKetNoi);
        }

        /// <summary>
        /// Mở kết nối SQL và trả cho nơi gọi quản lý; nếu Open lỗi thì Dispose trước khi phát lại lỗi.
        /// </summary>
        /// <returns>Kết nối đã mở; nơi gọi cần Dispose sau khi dùng xong.</returns>
        public IDbConnection MoKetNoi()
        {
            var connection = CreateConnection();
            try
            {
                connection.Open();
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Mở kết nối rồi tạo giao dịch Serializable; nơi gọi chịu trách nhiệm commit/rollback và giải phóng transaction/kết nối.
        /// </summary>
        /// <returns>Giao dịch mới trên kết nối riêng; nếu mở/tạo thất bại thì giải phóng kết nối và phát lại ngoại lệ.</returns>
        public IDbTransaction BatDauGiaoDich()
        {
            var connection = MoKetNoi();
            try
            {
                return connection.BeginTransaction(IsolationLevel.Serializable);
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Dùng transaction được truyền vào nếu có; nếu không thì mở kết nối, tham gia TransactionScope hiện có hoặc tạo giao dịch Serializable cục bộ với commit/rollback tự động.
        /// </summary>
        /// <typeparam name = "T">Kiểu kết quả mà thao tác SQL cần trả.</typeparam>
        /// <param name = "action">Hàm nhận kết nối đang mở và giao dịch cục bộ/được truyền vào; transaction null khi dùng giao dịch môi trường.</param>
        /// <returns>Kết quả của action; lỗi được phát lại, giao dịch cục bộ được rollback còn giao dịch ngoài do nơi gọi quyết định.</returns>
        protected T ThucHien<T>(Func<IDbConnection, IDbTransaction?, T> action)
        {
            // Trường hợp 1: nơi gọi đã mở giao dịch để lưu nhiều bảng cùng nhau.
            if (transaction is not null)
            {
                var ketNoiChung = transaction.Connection;
                if (ketNoiChung is null)
                {
                    throw new InvalidOperationException("Giao dịch đã kết thúc.");
                }

                return action(ketNoiChung, transaction);
            }

            using var connection = MoKetNoi();
            // Trường hợp 2: có TransactionScope; kết nối đã tự tham gia giao dịch đó.
            if (System.Transactions.Transaction.Current is not null)
            {
                return action(connection, null);
            }

            // Trường hợp 3: DAL tự mở giao dịch và tự chịu trách nhiệm lưu/hủy.
            using var local = connection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                T ketQua = action(connection, local);
                local.Commit();
                return ketQua;
            }
            catch
            {
                if (local.Connection is not null)
                {
                    local.Rollback();
                }

                throw;
            }
        }

        /// <summary>
        /// Chỉ chấp nhận tên SQL không rỗng gồm chữ/số ASCII và dấu gạch dưới trước khi ghép tên bảng/cột vào câu lệnh.
        /// </summary>
        /// <param name = "name">Tên bảng hoặc cột nội bộ được ghép vào SQL; không phải giá trị dữ liệu người dùng.</param>
        /// <returns>true nếu tên đáp ứng tập ký tự cho phép; false nếu rỗng hoặc có ký tự khác.</returns>
        private static bool LaTenSql(string name)
        {
            if (name.Length == 0)
            {
                return false;
            }

            foreach (char kyTu in name)
            {
                if (!char.IsAsciiLetterOrDigit(kyTu) && kyTu != '_')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Metadata một cột SQL phục vụ kiểm tra kiểu, nullable, computed, độ dài và scale trước khi ghi ET.
        /// </summary>
        private sealed class Column
        {
            /// <summary>
            /// Tên cột SQL trong metadata dùng để kiểm tra và lấy property cùng tên của ET.
            /// </summary>
            public string Name { get; set; } = "";
            /// <summary>
            /// Tên kiểu SQL của cột, như nvarchar, decimal hoặc datetime2, dùng chọn cách chuẩn hóa giá trị.
            /// </summary>
            public string Type { get; set; } = "";
            /// <summary>
            /// Cột SQL có cho phép null hay không, dùng kiểm tra trường bắt buộc.
            /// </summary>
            public bool Nullable { get; set; }
            /// <summary>
            /// Cột do SQL tính toán, không nhận giá trị ghi trực tiếp từ ET.
            /// </summary>
            public bool Computed { get; set; }
            /// <summary>
            /// Độ dài tối đa cột SQL theo byte; nvarchar/nchar chia 2 khi kiểm tra số ký tự.
            /// </summary>
            public int Length { get; set; }
            /// <summary>
            /// Số chữ số thập phân của decimal/time/datetime2 dùng kiểm tra hoặc làm tròn giá trị.
            /// </summary>
            public int Scale { get; set; }
        }

        /// <summary>
        /// Yêu cầu có giao dịch, lấy khóa ứng dụng riêng từng bảng và UPDLOCK/HOLDLOCK, tìm hậu tố số lớn nhất rồi cấp số tiếp theo; tránh trùng mã khi lưu đồng thời.
        /// </summary>
        /// <param name = "db">Kết nối đang mở và dùng cùng giao dịch với thao tác INSERT sau đó.</param>
        /// <param name = "tx">Giao dịch giữ khóa cấp mã; null chỉ khi kết nối tham gia giao dịch môi trường.</param>
        /// <param name = "table">Tên bảng nội bộ hợp lệ, dùng cho phạm vi khóa ứng dụng và tiền tố mã.</param>
        /// <param name = "key">Tên cột khóa để tìm hậu tố số lớn nhất đang có.</param>
        /// <returns>Mã mới được định dạng từ số lớn nhất cộng một; không chèn bản ghi tại đây.</returns>
        public static string TaoMa(IDbConnection db, IDbTransaction? tx, string table, string key)
        {
            if (!LaTenSql(table) || !LaTenSql(key))
            {
                throw new InvalidOperationException("Bảng hoặc cột cấp mã không hợp lệ.");
            }

            if (tx is null && System.Transactions.Transaction.Current is null)
            {
                throw new InvalidOperationException("Cấp mã phải nằm trong giao dịch lưu bản ghi.");
            }

            var locked = db.ExecuteScalar<int>("""
                DECLARE @result int;
                EXEC @result=sys.sp_getapplock @Resource=@Resource,@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
                SELECT @result;
                """, new { Resource = "RHM:Ma:" + table }, tx);
            if (locked < 0)
            {
                throw new InvalidOperationException("Bảng đang được cấp mã ở cửa sổ khác. Vui lòng thử lại.");
            }

            // TRY_CONVERT bỏ qua hậu tố không chuyển được thành số; COALESCE lấy 0 nếu bảng chưa có số hợp lệ.
            // Khóa cùng giao dịch với INSERT; hỗ trợ mã cũ BN001 và mã mới BN01.
            var highest = db.ExecuteScalar<long>($"SELECT COALESCE(MAX(TRY_CONVERT(bigint,SUBSTRING([{key}],PATINDEX('%[0-9]%',[{key}]+'0'),50))),0) FROM dbo.[{table}] WITH(UPDLOCK,HOLDLOCK)", transaction: tx);
            if (highest == long.MaxValue)
            {
                throw new InvalidOperationException("Đã hết phạm vi số của mã dữ liệu.");
            }

            return DinhDangMa(table, highest + 1);
        }

        /// <summary>
        /// Ghép chữ đầu các từ của tên bảng với số thứ tự có ít nhất hai chữ số, ví dụ mã bệnh nhân mới là BN01; không đổi các mã cũ trong database.
        /// </summary>
        /// <param name = "table">Tên bảng nội bộ gồm các phần phân tách bởi dấu gạch dưới.</param>
        /// <param name = "number">Số thứ tự dương đã cấp; D2 bổ sung số 0 khi cần, không cắt số dài hơn hai chữ số.</param>
        /// <returns>Chuỗi tiền tố chữ đầu cộng số thứ tự, dùng định dạng số không phụ thuộc ngôn ngữ máy.</returns>
        public static string DinhDangMa(string table, long number)
        {
            if (!LaTenSql(table) || number <= 0)
            {
                throw new InvalidOperationException("Bảng hoặc số thứ tự mã không hợp lệ.");
            }

            var tienTo = new StringBuilder();
            foreach (string tu in table.Split('_'))
            {
                tienTo.Append(tu[0]);
            }

            return tienTo.ToString() + number.ToString("D3", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Đọc metadata cột từ sys.columns/sys.types gồm kiểu, nullable, computed, độ dài, số lẻ; cache theo bảng trong đối tượng kết nối để dùng kiểm tra dữ liệu.
        /// </summary>
        /// <param name = "db">Kết nối SQL đang mở để đọc danh mục hệ thống.</param>
        /// <param name = "tx">Giao dịch hiện tại nếu có.</param>
        /// <param name = "table">Tên bảng nội bộ được kiểm tra trước khi truy vấn metadata.</param>
        /// <returns>Mảng metadata theo thứ tự cột; không tìm thấy bảng thì ném lỗi.</returns>
        private Column[] DocThongTinCot(IDbConnection db, IDbTransaction? tx, string table)
        {
            if (!LaTenSql(table))
            {
                throw new InvalidOperationException("Bảng dữ liệu không được phép truy cập.");
            }

            if (!columns.TryGetValue(table, out var result))
            {
                result = db.Query<Column>("""
                    SELECT c.name AS Name,t.name AS Type,c.is_nullable AS Nullable,c.is_computed AS Computed,
                        c.max_length AS Length,c.scale AS Scale
                    FROM sys.columns c JOIN sys.types t ON t.user_type_id=c.user_type_id
                    WHERE c.object_id=OBJECT_ID(@Table) ORDER BY c.column_id
                    """, new { Table = "dbo." + table }, tx).ToArray();
                if (result.Length == 0)
                {
                    throw new InvalidOperationException("Không tìm thấy bảng " + table + " trong database.");
                }

                columns[table] = result;
            }

            return result;
        }

        /// <summary>
        /// Chuẩn hóa DBNull/chuỗi trống và đổi đúng kiểu SQL; kiểm tra bắt buộc, độ dài, số chữ số lẻ và phạm vi giờ, làm tròn thời gian theo scale của cột.
        /// </summary>
        /// <param name = "column">Metadata quyết định kiểu, nullable, độ dài và độ chính xác thời gian/số thập phân.</param>
        /// <param name = "value">Giá trị ET đầu vào; chuỗi được Trim, chuỗi rỗng của cột nullable và DBNull chuyển thành null.</param>
        /// <returns>Giá trị đã chuẩn hóa tương thích kiểu cột; lỗi định dạng/phạm vi được đổi thành thông báo InvalidOperationException.</returns>
        private static object? ChuanHoaGiaTri(Column column, object? value)
        {
            if (value is DBNull)
            {
                value = null;
            }

            if (value is string text)
            {
                text = text.Trim();
                if (text.Length == 0 && column.Nullable)
                {
                    return null;
                }

                value = text;
            }

            if (value is null)
            {
                if (!column.Nullable)
                {
                    throw new InvalidOperationException("Vui lòng nhập " + column.Name + ".");
                }

                return null;
            }

            try
            {
                // Mỗi nhánh xử lý đúng một kiểu dữ liệu SQL.
                switch (column.Type)
                {
                    case "int":
                        return Convert.ToInt32(value, CultureInfo.CurrentCulture);
                    case "decimal":
                    case "numeric":
                        return ChuanHoaSo(column, value);
                    case "date":
                        return Convert.ToDateTime(value, CultureInfo.CurrentCulture).Date;
                    case "datetime2":
                    case "datetime":
                        return ChuanHoaNgayGio(column, value);
                    case "time":
                        return ChuanHoaGio(column, value);
                    case "nvarchar":
                    case "varchar":
                    case "nchar":
                    case "char":
                        return ChuanHoaChuoi(column, value);
                    default:
                        return value;
                }
            }
            catch (Exception ex)when (ex is FormatException or OverflowException or InvalidCastException or ArgumentOutOfRangeException)
            {
                throw new InvalidOperationException("Giá trị " + column.Name + " không đúng kiểu dữ liệu.", ex);
            }
        }

        /// <summary>Đọc số thập phân, nhận dấu phẩy/chấm và kiểm tra số chữ số lẻ theo cột SQL.</summary>
        private static decimal ChuanHoaSo(Column column, object value)
        {
            decimal so;
            if (value is string chuoiSo)
            {
                chuoiSo = chuoiSo.Replace(',', '.');
                so = decimal.Parse(chuoiSo, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            }
            else
            {
                so = Convert.ToDecimal(value, CultureInfo.CurrentCulture);
            }

            if (decimal.Round(so, column.Scale) != so)
            {
                throw new InvalidOperationException(column.Name + " có quá nhiều chữ số thập phân.");
            }

            return so;
        }

        /// <summary>Đọc ngày giờ và làm tròn phần giây theo độ chính xác của cột, giữ DateTimeKind.</summary>
        private static DateTime ChuanHoaNgayGio(Column column, object value)
        {
            DateTime ngayGio = Convert.ToDateTime(value, CultureInfo.CurrentCulture);
            long ticks = LamTronTicks(ngayGio.Ticks, column.Scale);
            return new DateTime(ticks, ngayGio.Kind);
        }

        /// <summary>Đọc giờ trong một ngày; chặn âm/24 giờ và làm tròn theo cột SQL.</summary>
        private static TimeSpan ChuanHoaGio(Column column, object value)
        {
            TimeSpan gio;
            if (value is TimeSpan giaTriGio)
            {
                gio = giaTriGio;
            }
            else
            {
                gio = TimeSpan.Parse(Convert.ToString(value, CultureInfo.CurrentCulture)!, CultureInfo.CurrentCulture);
            }

            if (gio < TimeSpan.Zero || gio >= TimeSpan.FromDays(1))
            {
                throw new InvalidOperationException("Giờ phải từ 00:00 đến trước 24:00.");
            }

            long ticks = LamTronTicks(gio.Ticks, column.Scale);
            return new TimeSpan(ticks % TimeSpan.TicksPerDay);
        }

        /// <summary>Đọc chuỗi và kiểm tra độ dài; cột Unicode tính hai byte mỗi ký tự, cột MAX không giới hạn tại đây.</summary>
        private static string ChuanHoaChuoi(Column column, object value)
        {
            string chuoi = Convert.ToString(value, CultureInfo.CurrentCulture) ?? "";
            int gioiHan = column.Length;
            // Unicode dùng 2 byte/ký tự. Length=-1 là cột MAX, không giới hạn tại đây.
            if (column.Type.StartsWith('n'))
            {
                gioiHan /= 2;
            }

            if (column.Length > 0 && chuoi.Length > gioiHan)
            {
                throw new InvalidOperationException($"{column.Name} tối đa {gioiHan} ký tự.");
            }

            return chuoi;
        }

        /// <summary>
        /// Làm tròn thời gian theo số chữ số lẻ của SQL. Một giây có 10.000.000 ticks;
        /// scale=0 làm tròn tới giây, scale=3 tới mili giây, scale=7 giữ nguyên ticks.
        /// </summary>
        private static long LamTronTicks(long ticks, int soChuSoLe)
        {
            long donViLamTron = 1;
            for (int i = soChuSoLe; i < 7; i++)
            {
                donViLamTron *= 10;
            }

            long ticksCongNuaDonVi = ticks + donViLamTron / 2;
            long soDonVi = ticksCongNuaDonVi / donViLamTron;
            return soDonVi * donViLamTron;
        }

        /// <summary>
        /// Dùng TypeDescriptor lấy các property của ET thành dictionary tên cột/giá trị để kiểm tra và đối chiếu dữ liệu.
        /// </summary>
        /// <param name = "record">Đối tượng ET cần đọc property, gồm cả các property không được ghi SQL.</param>
        /// <returns>Dictionary tên property tới giá trị hiện tại, không thay đổi đối tượng.</returns>
        public static Dictionary<string, object?> GiaTri(object record)
        {
            var giaTri = new Dictionary<string, object?>();
            foreach (PropertyDescriptor thuocTinh in TypeDescriptor.GetProperties(record))
            {
                giaTri.Add(thuocTinh.Name, thuocTinh.GetValue(record));
            }

            return giaTri;
        }

        /// <summary>
        /// Duyệt các cột được phép ghi theo metadata/WriteAttribute, chuẩn hóa lại giá trị ET và so dữ liệu expected với bản SQL gốc để chặn ghi đè khi sửa đồng thời.
        /// </summary>
        /// <typeparam name = "T">Kiểu ET tham chiếu có property tương ứng tên cột SQL.</typeparam>
        /// <param name = "db">Kết nối SQL đang mở để đọc metadata nếu chưa có trong cache.</param>
        /// <param name = "tx">Giao dịch hiện tại chứa cả việc kiểm tra và thao tác ghi sau đó.</param>
        /// <param name = "table">Bảng nội bộ dùng lấy metadata cột.</param>
        /// <param name = "record">ET sắp ghi; property được gán lại giá trị chuẩn hóa ngay trên đối tượng.</param>
        /// <param name = "original">Bản SQL hiện tại được đọc trước đó; null khi thêm mới.</param>
        /// <param name = "expected">Bản người dùng đã đọc ban đầu; có cùng original để phát hiện sửa đồng thời, null để bỏ đối chiếu.</param>
        protected void KiemTra<T>(IDbConnection db, IDbTransaction? tx, string table, T record, T? original, T? expected)
            where T : class
        {
            var cacCot = DocThongTinCot(db, tx, table);
            var giaTriMoi = GiaTri(record);
            var giaTriHienTai = original is null ? null : GiaTri(original);
            var giaTriBanGoc = expected is null ? null : GiaTri(expected);
            var cacThuocTinh = TypeDescriptor.GetProperties(record);
            var cacQuyTacGhi = TypeDescriptor.GetProperties(typeof(T));
            foreach (Column cot in cacCot)
            {
                // Bỏ cột SQL tự tính và thuộc tính không được phép ghi.
                if (cot.Computed || !giaTriMoi.ContainsKey(cot.Name))
                {
                    continue;
                }

                PropertyDescriptor thuocTinh = cacQuyTacGhi[cot.Name]!;
                if (!ChoPhepGhi(thuocTinh))
                {
                    continue;
                }

                object? giaTri = ChuanHoaGiaTri(cot, giaTriMoi[cot.Name]);
                cacThuocTinh[cot.Name]!.SetValue(record, giaTri);
                // Thêm mới hoặc không có bản gốc thì không cần kiểm tra ghi đè.
                if (giaTriHienTai is null || giaTriBanGoc is null)
                {
                    continue;
                }

                object? giaTriDaDoc = ChuanHoaGiaTri(cot, giaTriBanGoc[cot.Name]);
                if (!Equals(giaTriDaDoc, giaTriHienTai[cot.Name]))
                {
                    throw new InvalidOperationException("Bản ghi đã thay đổi ở cửa sổ khác. Hãy tải lại trước khi lưu hoặc xóa.");
                }
            }
        }

        /// <summary>
        /// Kiểm tra WriteAttribute của thuộc tính ET: có Write(false) thì bỏ qua,
        /// không có thuộc tính này hoặc tất cả đều cho ghi thì được lưu.
        /// </summary>
        private static bool ChoPhepGhi(PropertyDescriptor thuocTinh)
        {
            foreach (Attribute quyTac in thuocTinh.Attributes)
            {
                if (quyTac is WriteAttribute quyTacGhi && !quyTacGhi.Write)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Dùng chung khi DAL chỉ đổi trạng thái: đối chiếu từng trường với bản SQL hiện tại.
        /// Chỉ cho đổi TrangThai, NgayXacNhan, NgayCapNhat và LyDoThayDoi; các trường khác phải giữ nguyên.
        /// </summary>
        protected static void KiemTraChiDoiTrangThai(object duLieu, object hienTai)
        {
            var giaTriMoi = GiaTri(duLieu);
            var giaTriCu = GiaTri(hienTai);
            foreach (var truong in giaTriMoi)
            {
                string ten = truong.Key;
                if (ten == "TrangThai" || ten == "NgayXacNhan" || ten == "NgayCapNhat" || ten == "LyDoThayDoi")
                {
                    continue;
                }

                if (!Equals(truong.Value, giaTriCu.GetValueOrDefault(ten)))
                {
                    throw new InvalidOperationException("Thao tác đổi trạng thái không được thay đổi nội dung bản ghi.");
                }
            }
        }
    }
}
