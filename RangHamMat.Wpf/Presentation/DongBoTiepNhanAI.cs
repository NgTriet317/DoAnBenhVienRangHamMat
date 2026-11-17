using BUS;
using ET;
using RangHamMat.Services;

namespace RangHamMat.Views;

public partial class TiepNhanAI
{
    private bool dongBoDangDienDanhSach,dongBoBanKhaiXungDot;
    private void DangKyDongBoBanKhai()=>DongBoManHinh.RegisterPreparedWindow(this,
        ["AI_PHIEN_TIEP_NHAN","AI_TIEP_NHAN_NHAP","AI_KET_QUA_TAO_PHIEU","PHIEU_KHAM_BENH"],
        token=>DongBoChuanBi.Doc(()=>new BUS_AITIEPNHAN().layDanhSachChoDuyet(),ApplyDongBoBanKhai,token),
        ()=>dangTai||dangDoiChieu);

    private void ApplyDongBoBanKhai(List<ET_AITIEPNHAN> rows)
    {
        var id=dangChon?.MaPhien;
        var latest=rows.FirstOrDefault(x=>x.MaPhien==id);
        // Giữ nguyên đối tượng gốc và mọi ô đang sửa. Chỉ thay nguồn của danh sách bên cạnh.
        dongBoDangDienDanhSach=true;
        try{danhSach=rows;gridPhien.ItemsSource=rows;gridPhien.SelectedItem=latest;}
        finally{dongBoDangDienDanhSach=false;}
        dongBoBanKhaiXungDot=dangChon is not null&&(latest is null||
            latest.TrangThai!=dangChon.TrangThai||latest.MaPhieu!=dangChon.MaPhieu||
            latest.HoTen!=dangChon.HoTen||latest.NgaySinh!=dangChon.NgaySinh||
            latest.SoDienThoai!=dangChon.SoDienThoai||latest.LyDoKham!=dangChon.LyDoKham||
            latest.LoaiKham!=dangChon.LoaiKham||latest.TrieuChung!=dangChon.TrieuChung||latest.CanHoTroGap!=dangChon.CanHoTroGap);
        btnTaoPhieu.IsEnabled=!dongBoBanKhaiXungDot;
        lblThongBao.Text=dongBoBanKhaiXungDot
            ?"Bản khai đang đối chiếu đã thay đổi hoặc được xử lý ở máy khác. Nội dung nhập được giữ; chọn lại bản khai hoặc bấm Làm mới để đối chiếu trước khi tiếp tục."
            :$"Có {rows.Count} bản khai AI · Danh sách đã cập nhật; nội dung đang đối chiếu được giữ nguyên.";
    }
}
