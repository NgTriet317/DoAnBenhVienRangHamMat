using System.Data;
using DAL;
using ET;

namespace BUS;

public sealed class BUS_ANHXQUANG(IDbTransaction? transaction=null,CauHinhKhoAnh? cauHinh=null)
{
    private KhoLuuAnhXquang Store()=>new(cauHinh??CauHinhKhoAnh.Doc());
    public IReadOnlyList<NhanVienChupAnh> LayKyThuatVien(string maLS,NguCanhNghiepVu user)
        =>XuLyLoi.ThucHien(()=>new DAL_ANHXQUANG(transaction).LayKyThuatVien(maLS,user));
    public string PhanCong(string maLS,string source,string maNVKyThuat,NguCanhNghiepVu user)
        =>XuLyLoi.ThucHien(()=>new DAL_ANHXQUANG(transaction).PhanCong(maLS,source,maNVKyThuat,user));
    public IReadOnlyList<LuotAnhDuocGiao> LayLuotDuocGiao(NguCanhNghiepVu user)
        =>XuLyLoi.ThucHien(()=>new DAL_ANHXQUANG(transaction).LayLuotDuocGiao(user));
    public DanhSachAnhXquang LayDanhSach(string maLS,bool hienAnhAn,NguCanhNghiepVu user)
        =>XuLyLoi.ThucHien(()=>new DAL_ANHXQUANG(transaction).LayDanhSach(maLS,hienAnhAn,user));
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
    public DuLieuAnhXquang MoAnh(string maLS,string maAnh,NguCanhNghiepVu user)=>XuLyLoi.ThucHien(()=>
    {
        if(transaction is not null)return Read(transaction,maLS,maAnh,user);
        using var tx=new DAL_ANHXQUANG().BatDauGiaoDich();using var connection=tx.Connection!;
        // Lỗi tệp được giữ lại tới sau commit audit, không làm rollback dấu vết lỗi.
        DuLieuAnhXquang? result=null;LoiAnhXquangException? error=null;
        try{result=Read(tx,maLS,maAnh,user);}catch(LoiAnhXquangException ex){error=ex;}
        tx.Commit();if(error is not null)throw error;return result!;
    });
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
    public bool AnAnh(string maLS,string maAnh,NguCanhNghiepVu user)
        =>XuLyLoi.ThucHien(()=>new DAL_ANHXQUANG(transaction).AnAnh(maLS,maAnh,user));
    public IReadOnlyList<SuCoKhoAnh> KiemTraKho(NguCanhNghiepVu user)
        =>XuLyLoi.ThucHien(()=>
        {
            if(transaction is not null){var dal=new DAL_ANHXQUANG(transaction);dal.KhoaKhoDocQuyen(user);return Store().KiemTra(dal.LayToanBoMetadata(user));}
            using var tx=new DAL_ANHXQUANG().BatDauGiaoDich();using var connection=tx.Connection!;var current=new DAL_ANHXQUANG(tx);
            current.KhoaKhoDocQuyen(user);var result=Store().KiemTra(current.LayToanBoMetadata(user));tx.Commit();return result;
        });
}
