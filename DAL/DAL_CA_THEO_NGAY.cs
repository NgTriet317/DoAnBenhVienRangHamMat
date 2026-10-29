using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập dữ liệu ngày, thực hiện truy vấn và kiểm tra trong phạm vi kết nối/giao dịch của dbconnect.
    /// </summary>
    public class DAL_CA_THEO_NGAY : dbconnect
    {
        /// <summary>
        /// Đọc các ca theo khoảng tuần được chọn để dựng lịch làm việc.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>connection.Query&lt;ET_CA_THEO_NGAY&gt;</c>, <c>dauTuan.Date.AddDays</c>.</remarks>
        /// <param name="dauTuan">Giá trị đầu tuần dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <returns>Tập dữ liệu kiểu List&lt;ET_CA_THEO_NGAY&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
        public List<ET_CA_THEO_NGAY> layCaTheoTuan(DateTime dauTuan)
        {
            return ThucHien((connection, transaction) => connection.Query<ET_CA_THEO_NGAY>(
                @"SELECT * FROM dbo.CA_THEO_NGAY WHERE NgayLam >= @TuNgay AND NgayLam < @DenNgay",
                new { TuNgay = dauTuan.Date, DenNgay = dauTuan.Date.AddDays(7) }, transaction).ToList());
        }

        /// <summary>
        /// Thêm cấu hình ca áp dụng cho ngày được chỉ định sau các kiểm tra dữ liệu. Các truy vấn có gợi ý khóa giữ bản dữ liệu liên quan trong phạm vi giao dịch.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>connection.Execute</c>.</remarks>
        /// <param name="ca">Giá trị ca dùng khi thực hiện thao tác; kiểu ET_CA_THEO_NGAY.</param>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        public bool themCaTheoNgay(ET_CA_THEO_NGAY ca)
        {
            return ThucHien((connection, transaction) => connection.Execute(
                @"INSERT INTO dbo.CA_THEO_NGAY (NgayLam, MaCa)
                  SELECT @NgayLam, @MaCa
                  WHERE NOT EXISTS (SELECT 1 FROM dbo.CA_THEO_NGAY WITH (UPDLOCK, HOLDLOCK)
                                    WHERE NgayLam = @NgayLam AND MaCa = @MaCa)",
                ca, transaction) > 0);
        }

        /// <summary>
        /// Xóa cấu hình ca/ngày theo mã và điều kiện được nghiệp vụ cho phép.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>connection.Execute</c>.</remarks>
        /// <param name="ngay">Giá trị ngày dùng khi thực hiện thao tác; kiểu DateTime.</param>
        /// <param name="maCa">Mã ca xác định đối tượng cần xử lý.</param>
        /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
        public bool xoaCaTheoNgay(DateTime ngay, string maCa)
        {
            return ThucHien((connection, transaction) => connection.Execute(
                @"DELETE FROM dbo.CA_THEO_NGAY
                  WHERE NgayLam = @NgayLam AND MaCa = @MaCa
                    AND NOT EXISTS (SELECT 1 FROM dbo.LICH_LAM_VIEC
                                    WHERE NgayLam = @NgayLam AND MaCa = @MaCa
                                    AND TrangThai IN (N'Chờ xác nhận', N'Đã xác nhận'))",
                new { NgayLam = ngay.Date, MaCa = maCa }, transaction) > 0);
        }
    }
}
