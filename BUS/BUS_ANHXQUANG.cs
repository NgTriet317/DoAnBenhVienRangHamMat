using System.Data;
using DAL;
using ET;

namespace BUS;

/// <summary>
/// Điều phối ảnh X-quang giữa quyền SQL, metadata và kho tệp; quản lý giao dịch khi tải hoặc mở ảnh.
/// </summary>
public sealed class BUS_ANHXQUANG(IDbTransaction? transaction=null,CauHinhKhoAnh? cauHinh=null)
{
    private KhoLuuAnhXquang Store()=>new(cauHinh??CauHinhKhoAnh.Doc());
    /// <summary>
    /// Lấy kỹ thuật viên đủ điều kiện nhận dịch vụ chụp ảnh của lần khám qua DAL.
    /// </summary>
    public IReadOnlyList<NhanVienChupAnh> LayKyThuatVien(string maLS,NguCanhNghiepVu user)
        =>XuLyLoi.ThucHien(()=>new DAL_ANHXQUANG(transaction).LayKyThuatVien(maLS,user));
    /// <summary>
    /// Giao dịch vụ chụp của lần khám cho kỹ thuật viên; DAL kiểm tra quyền và lưu lịch sử phân công.
    /// </summary>
    public string PhanCong(string maLS,string source,string maNVKyThuat,NguCanhNghiepVu user)
        =>XuLyLoi.ThucHien(()=>new DAL_ANHXQUANG(transaction).PhanCong(maLS,source,maNVKyThuat,user));
    /// <summary>
    /// Lấy các lượt còn mở trong phạm vi chụp ảnh được giao cho người dùng đã xác thực.
    /// </summary>
    public IReadOnlyList<LuotAnhDuocGiao> LayLuotDuocGiao(NguCanhNghiepVu user)
        =>XuLyLoi.ThucHien(()=>new DAL_ANHXQUANG(transaction).LayLuotDuocGiao(user));
    /// <summary>
    /// Nạp metadata, dịch vụ chụp và quyền thao tác ảnh của lần khám để hiển thị.
    /// </summary>
    public DanhSachAnhXquang LayDanhSach(string maLS,bool hienAnhAn,NguCanhNghiepVu user)
        =>XuLyLoi.ThucHien(()=>new DAL_ANHXQUANG(transaction).LayDanhSach(maLS,hienAnhAn,user));
    /// <summary>
    /// Kiểm tra quyền và nội dung ảnh, lưu tệp rồi ghi metadata trong giao dịch do đầu mối này quản lý.
    /// </summary>
    /// <remarks>Ảnh trùng cùng nguồn và người tải được dùng lại sau khi kiểm tra tệp. Chỉ xóa tệp mới khi chưa thử commit; nếu kết quả commit chưa rõ thì giữ tệp để bảo toàn liên kết SQL.</remarks>
    /// <param name="maLS">Mã lần khám đã lưu và còn cho phép cập nhật ảnh.</param>
    /// <param name="content">Luồng ảnh được đọc từ vị trí hiện tại để kiểm tra định dạng và dung lượng.</param>
    /// <param name="tenTepGoc">Tên tệp gốc dùng kiểm tra phần mở rộng và lưu thông tin tra cứu.</param>
    /// <param name="maDVThucHien">Mã dịch vụ thực hiện liên kết với ảnh; kỹ thuật viên phải chọn dịch vụ được giao.</param>
    /// <param name="ghiChu">Ghi chú tùy chọn, tối đa 500 ký tự.</param>
    /// <param name="user">Ngữ cảnh người tải được DAL xác thực lại trong giao dịch.</param>
    /// <returns>Metadata ảnh vừa lưu hoặc metadata ảnh trùng đã tồn tại.</returns>
    public ET_ANHXQUANGLANKHAM TaiLen(string maLS,Stream content,string tenTepGoc,string? maDVThucHien,string? ghiChu,NguCanhNghiepVu user)
    {
        if(transaction is not null)throw new LoiAnhXquangException("CanLuuLuot","Lưu lần khám trước khi tải ảnh vào hồ sơ.");
        return XuLyLoi.ThucHien(()=>
        {
            using var tx=new DAL_ANHXQUANG().BatDauGiaoDich();using var connection=tx.Connection!;
            var dal=new DAL_ANHXQUANG(tx);string? savedKey=null;bool commitAttempted=false;KhoLuuAnhXquang? store=null;
            try
            {
                var actor=dal.KiemTraTaiLen(maLS,maDVThucHien,user);
                if(ghiChu?.Length>500)throw new LoiAnhXquangException("GhiChu","Ghi chú ảnh tối đa 500 ký tự.");
                store=Store();store.ThuMucGoc();var image=store.KiemTraTep(content,tenTepGoc);
                var duplicate=dal.TimAnhTrung(maLS,image.Sha256,maDVThucHien,actor);
                if(duplicate is not null)
                {
                    try{store.DocTep(duplicate);}
                    catch(LoiAnhXquangException ex)
                    {dal.GhiSuKien(duplicate,ex.MaLoi=="TepKhongTonTai"?"XR_FILE_MISSING":"XR_FILE_INVALID","Thất bại",actor);commitAttempted=true;tx.Commit();throw;}
                    dal.GhiSuKien(duplicate,"XR_UPLOAD_DUPLICATE","Thành công",actor);commitAttempted=true;tx.Commit();return duplicate;
                }
                savedKey=store.LuuTep(image);
                var metadata=new ET_ANHXQUANGLANKHAM{MaAnh=Guid.NewGuid().ToString("N"),MaLS=maLS,MaDVThucHien=maDVThucHien,
                    TenTepGoc=tenTepGoc,TenTepLuu=savedKey,DuongDanTuongDoi=savedKey,MimeType=image.MimeType,
                    KichThuocBytes=image.NoiDung.Length,Sha256=image.Sha256,GhiChu=ghiChu,MaNVUpload=actor.MaNV};
                var saved=dal.LuuMetadata(metadata,actor);commitAttempted=true;tx.Commit();return saved;
            }
            catch
            {
                if(tx.Connection is not null){try{tx.Rollback();}catch(Exception ex) when(ex is InvalidOperationException or System.Data.Common.DbException){}}
                // Nếu commit chưa rõ kết quả, giữ tệp để tránh xóa ảnh có thể đã được SQL liên kết.
                if(savedKey is not null&&!commitAttempted){try{store!.XoaTepChuaLienKet(savedKey);}catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or LoiAnhXquangException){}}
                throw;
            }
        });
    }
    /// <summary>
    /// Đọc ảnh đã kiểm tra quyền và toàn vẹn, đồng thời ghi nhật ký mở ảnh hoặc lỗi tệp.
    /// </summary>
    /// <remarks>Khi tự mở giao dịch, commit nhật ký trước khi phát lại lỗi tệp. Giao dịch được truyền vào vẫn do nơi gọi kết thúc.</remarks>
    /// <returns>Metadata và nội dung ảnh đã được đối chiếu với kho tệp.</returns>
    public DuLieuAnhXquang MoAnh(string maLS,string maAnh,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>
    {
        if(transaction is not null)return Read(transaction,maLS,maAnh,user);
        using var tx=new DAL_ANHXQUANG().BatDauGiaoDich();using var connection=tx.Connection!;
        // Lỗi tệp được giữ lại tới sau commit audit, không làm rollback dấu vết lỗi.
        DuLieuAnhXquang? result=null;LoiAnhXquangException? error=null;
        try{result=Read(tx,maLS,maAnh,user);}catch(LoiAnhXquangException ex){error=ex;}
        tx.Commit();if(error is not null)throw error;return result!;
    });
    /// <summary>
    /// Đọc tệp theo metadata được DAL cho phép và ghi kết quả kiểm tra vào giao dịch đang dùng.
    /// </summary>
    private DuLieuAnhXquang Read(IDbTransaction tx,string maLS,string maAnh,NguCanhNghiepVu user)
    {
        var dal=new DAL_ANHXQUANG(tx);var metadata=dal.LayAnh(maLS,maAnh,user);
        try
        {
            var bytes=Store().DocTep(metadata);dal.GhiSuKien(metadata,"XR_VIEW","Thành công",user);return new(metadata,bytes);
        }
        catch(LoiAnhXquangException ex)
        {dal.GhiSuKien(metadata,ex.MaLoi=="TepKhongTonTai"?"XR_FILE_MISSING":"XR_FILE_INVALID","Thất bại",user);throw;}
    }
    /// <summary>
    /// Ẩn ảnh qua DAL sau khi kiểm tra lượt và quyền; giữ metadata cùng tệp trong lịch sử.
    /// </summary>
    public bool AnAnh(string maLS,string maAnh,NguCanhNghiepVu user)
        =>XuLyLoi.ThucHien(()=>new DAL_ANHXQUANG(transaction).AnAnh(maLS,maAnh,user));
    /// <summary>
    /// Giữ khóa kho độc quyền để đối chiếu toàn bộ metadata với tệp dưới quyền quản trị.
    /// </summary>
    /// <returns>Các tệp thiếu, sai toàn vẹn, chưa đăng ký hoặc còn ở thư mục tạm.</returns>
    public IReadOnlyList<SuCoKhoAnh> KiemTraKho(NguCanhNghiepVu user)
        =>XuLyLoi.ThucHien(()=>
        {
            if(transaction is not null){var dal=new DAL_ANHXQUANG(transaction);dal.KhoaKhoDocQuyen(user);return Store().KiemTra(dal.LayToanBoMetadata(user));}
            using var tx=new DAL_ANHXQUANG().BatDauGiaoDich();using var connection=tx.Connection!;var current=new DAL_ANHXQUANG(tx);
            current.KhoaKhoDocQuyen(user);var result=Store().KiemTra(current.LayToanBoMetadata(user));tx.Commit();return result;
        });
}
