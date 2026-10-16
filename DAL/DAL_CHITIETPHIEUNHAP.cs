// AI hỗ trợ: bổ sung chú thích tiếng Việt;
using System.Data;
using Dapper;
using ET;

namespace DAL
{
    /// <summary>
    /// Tầng truy cập SQL cho dòng nhập kho trong bảng CHI_TIET_PHIEU_NHAP. Lưu hàng hóa, lô, số lượng và đơn giá; chỉ cho chỉnh nội dung khi phiếu nhập cha còn nháp. Dapper ánh xạ dòng SQL sang ET; ThucHien dùng giao dịch được truyền vào hoặc tự quản lý giao dịch cục bộ.
    /// </summary>
    public class DAL_CHITIETPHIEUNHAP : dbconnect
    {
        /// <summary>
        /// Tạo DAL quản lý dòng nhập kho, chuyển giao dịch tùy chọn cho dbconnect để nhiều thao tác có thể dùng chung kết nối và phạm vi lưu.
        /// </summary>
        /// <param name = "transaction">Giao dịch của nơi gọi; null để hạ tầng tham gia giao dịch môi trường hoặc tự mở giao dịch cục bộ. Nếu truyền vào, nơi gọi quản lý commit/rollback.</param>
        public DAL_CHITIETPHIEUNHAP(IDbTransaction? transaction = null) : base(transaction)
        {
        }

        /// <summary>
        /// Đọc toàn bộ dòng nhập kho từ dbo.CHI_TIET_PHIEU_NHAP, Dapper ánh xạ từng dòng sang ET, sắp theo mã; ToArray nạp hết dữ liệu trước khi hạ tầng kết thúc phạm vi kết nối/giao dịch.
        /// </summary>
        /// <returns>Danh sách ET đã vật hóa; không có bản ghi thì trả danh sách rỗng.</returns>
        public IEnumerable<ET_CHITIETPHIEUNHAP> layToanBoChiTietPhieuNhap()
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.Query<ET_CHITIETPHIEUNHAP>("SELECT * FROM dbo.[CHI_TIET_PHIEU_NHAP] ORDER BY [MaCTPN]", transaction: transaction).ToArray();
            });
        }

        /// <summary>
        /// Tìm dòng nhập kho theo khóa trong dbo.CHI_TIET_PHIEU_NHAP; mã được truyền bằng tham số SQL và QuerySingleOrDefault ánh xạ tối đa một bản ghi sang ET.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <returns>ET của bản ghi tìm thấy, hoặc null nếu mã không tồn tại.</returns>
        public ET_CHITIETPHIEUNHAP? layChiTietPhieuNhapTheoMa(string ma)
        {
            return ThucHien((connection, transaction) =>
            {
                return connection.QuerySingleOrDefault<ET_CHITIETPHIEUNHAP>("SELECT * FROM dbo.[CHI_TIET_PHIEU_NHAP] WHERE [MaCTPN]=@Ma", new { Ma = ma }, transaction);
            });
        }

        /// <summary>
        /// Thêm dòng nhập kho: cấp mã có khóa ngay vào duLieu, kiểm tra nghiệp vụ, chuẩn hóa và kiểm tra ET theo metadata SQL rồi INSERT có tham số vào dbo.CHI_TIET_PHIEU_NHAP trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <returns>true sau khi INSERT thành công; lỗi dữ liệu/SQL được phát ra bằng ngoại lệ. Giao dịch ngoài chỉ commit khi nơi gọi quyết định.</returns>
        public bool themChiTietPhieuNhap(ET_CHITIETPHIEUNHAP duLieu)
        {
            return ThucHien((connection, transaction) =>
            {
                duLieu.MaCTPN = TaoMa(connection, transaction, "CHI_TIET_PHIEU_NHAP", "MaCTPN");
                KiemTraNghiepVu(transaction, duLieu, null);
                KiemTra<ET_CHITIETPHIEUNHAP>(connection, transaction, "CHI_TIET_PHIEU_NHAP", duLieu, null, null);
                connection.Execute("INSERT INTO dbo.[CHI_TIET_PHIEU_NHAP] ([MaCTPN],[MaPN],[MaHH],[MaLo],[SoLuong],[DonGia],[GhiChu]) VALUES (@MaCTPN,@MaPN,@MaHH,@MaLo,@SoLuong,@DonGia,@GhiChu)", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Sửa dòng nhập kho: đọc và giữ khóa UPDLOCK/HOLDLOCK, báo lỗi nếu bản không còn, kiểm tra nghiệp vụ, chuẩn hóa ET và đối chiếu banGoc để phát hiện sửa đồng thời. Khi chỉ đổi trạng thái, chặn thay đổi các trường ngoài TrangThai/NgayXacNhan/NgayCapNhat/LyDoThayDoi, sau đó UPDATE có tham số trong cùng giao dịch.
        /// </summary>
        /// <param name = "duLieu">ET chứa dữ liệu cần lưu; thêm mới sẽ được gán mã, và kiểm tra metadata có thể chuẩn hóa lại các giá trị ngay trên đối tượng.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <param name = "chiDoiTrangThai">true để cấm thay đổi trường nội dung ngoài nhóm trường trạng thái cho phép; không tự đảm bảo mọi chuyển trạng thái đều hợp lệ.</param>
        /// <returns>true khi UPDATE chạy thành công; bản bị mất/thay đổi hoặc dữ liệu sai gây ngoại lệ. Không trả false để biểu thị lỗi.</returns>
        public bool suaChiTietPhieuNhap(ET_CHITIETPHIEUNHAP duLieu, ET_CHITIETPHIEUNHAP? banGoc = null, bool chiDoiTrangThai = false)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_CHITIETPHIEUNHAP>("SELECT * FROM dbo.[CHI_TIET_PHIEU_NHAP] WITH(UPDLOCK,HOLDLOCK) WHERE [MaCTPN]=@Ma", new { Ma = duLieu.MaCTPN }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTraNghiepVu(transaction, duLieu, hienTai);
                KiemTra(connection, transaction, "CHI_TIET_PHIEU_NHAP", duLieu, hienTai, banGoc);
                if (chiDoiTrangThai)
                {
                    KiemTraChiDoiTrangThai(duLieu, hienTai);
                }

                connection.Execute("UPDATE dbo.[CHI_TIET_PHIEU_NHAP] SET [MaPN]=@MaPN,[MaHH]=@MaHH,[MaLo]=@MaLo,[SoLuong]=@SoLuong,[DonGia]=@DonGia,[GhiChu]=@GhiChu WHERE [MaCTPN]=@MaCTPN", duLieu, transaction);
                return true;
            });
        }

        /// <summary>
        /// Xóa dòng nhập kho: đọc bản hiện tại với UPDLOCK/HOLDLOCK, báo lỗi nếu đã mất, kiểm tra nghiệp vụ, so banGoc khi có rồi DELETE theo mã trong dbo.CHI_TIET_PHIEU_NHAP; toàn bộ thao tác giữ cùng giao dịch và lỗi ràng buộc SQL được phát ra.
        /// </summary>
        /// <param name = "ma">Mã khóa chính của bản ghi cần đọc, sửa hoặc xóa; truyền bằng tham số SQL.</param>
        /// <param name = "banGoc">Bản ET đã đọc trước khi thao tác; có giá trị để so với SQL và chặn ghi đè/xóa khi bản đã thay đổi, null để bỏ đối chiếu bản gốc.</param>
        /// <returns>true khi DELETE chạy thành công; không tồn tại, xung đột hoặc vi phạm SQL gây ngoại lệ.</returns>
        public bool xoaChiTietPhieuNhap(string ma, ET_CHITIETPHIEUNHAP? banGoc = null)
        {
            return ThucHien((connection, transaction) =>
            {
                var hienTai = connection.QuerySingleOrDefault<ET_CHITIETPHIEUNHAP>("SELECT * FROM dbo.[CHI_TIET_PHIEU_NHAP] WITH(UPDLOCK,HOLDLOCK) WHERE [MaCTPN]=@Ma", new { Ma = ma }, transaction);
                if (hienTai is null)
                {
                    throw new InvalidOperationException("Bản ghi không còn tồn tại. Vui lòng tải lại.");
                }

                KiemTraNghiepVu(transaction, hienTai, hienTai);
                KiemTra(connection, transaction, "CHI_TIET_PHIEU_NHAP", hienTai, hienTai, banGoc);
                connection.Execute("DELETE FROM dbo.[CHI_TIET_PHIEU_NHAP] WHERE [MaCTPN]=@Ma", new { Ma = ma }, transaction);
                return true;
            });
        }

        /// <summary>
        /// Đọc phiếu nhập cha trong giao dịch, yêu cầu trạng thái Nháp và cấm chuyển chi tiết đã lưu sang phiếu khác.
        /// </summary>
        /// <param name = "giaoDich">Giao dịch đang dùng để đọc/ghi và giữ khóa đồng bộ; được truyền tiếp cho các DAL liên quan nếu có.</param>
        /// <param name = "duLieu">ET cần kiểm tra hoặc cập nhật, chứa các mã liên kết và trạng thái mới.</param>
        /// <param name = "banHienTai">Bản hiện tại đã đọc từ SQL; null khi thêm mới, dùng để bảo vệ nội dung/trạng thái và liên kết đã lưu.</param>
        private void KiemTraNghiepVu(IDbTransaction? giaoDich, ET_CHITIETPHIEUNHAP duLieu, ET_CHITIETPHIEUNHAP? banHienTai)
        {
            var parent = new DAL_PHIEUNHAP(giaoDich).layPhieuNhapTheoMa(duLieu.MaPN);
            if (parent is null || parent.TrangThai != "Nháp")
            {
                throw new InvalidOperationException("Chỉ sửa chi tiết phiếu nhập nháp.");
            }

            if (banHienTai is not null && banHienTai.MaPN != duLieu.MaPN)
            {
                throw new InvalidOperationException("Không chuyển chi tiết sang phiếu nhập khác.");
            }
        }
    }
}
