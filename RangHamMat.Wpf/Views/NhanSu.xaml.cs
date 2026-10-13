// AI bổ sung kiểm tra nhập liệu: tạo TryReadEmployee/TryReadPosition, chặn thông tin sai trước khi gọi BUS và phân biệt lỗi lưu với lỗi tải lại.
// PHẦN AI ĐÃ THỰC HIỆN: AI đã bổ sung chú thích cho các chức năng nhân sự hiện có: nạp danh sách qua BUS, thêm/sửa/xóa, đọc dòng được chọn và mở ChiTietNhanSu.
// AI hỗ trợ: bổ sung chú thích tiếng Việt cho file này ngày 13/10/2026.
// Nhãn này ghi nhận phần hỗ trợ/chú thích;
using BUS;
using ET;
using RangHamMat.Controls;
using System.Windows;
using System.Windows.Input;

namespace RangHamMat.Views
{
    /// <summary>
    /// Màn hình quản lý nhân viên/chức vụ: phần bố cục/binding nằm trong XAML, code-behind xử lý khởi tạo và các sự kiện của màn hình.
    /// </summary>
    public partial class NhanSu
    {        
        /// <summary>
        /// Khởi tạo quản lý nhân viên/chức vụ từ XAML. Đọc danh sách nhân viên, chức vụ và chuyên khoa qua BUS rồi gán cho bảng/ComboBox.
        /// </summary>
        public NhanSu()
        {
            InitializeComponent();
            dpNgaySinh.DisplayDateEnd=DateTime.Today;

            dgvNhanVien.ItemsSource = new BUS_NHANVIEN().layToanBoNhanVien();
            dgvChucVu.ItemsSource = new BUS_CHUCVU().layToanBoChucVu();
            cboChuyenKhoa.ItemsSource = new BUS_CHUYENKHOA().layToanBoChuyenKhoa();
            cboChucVu.ItemsSource = new BUS_CHUCVU().layToanBoChucVu();
        }

        /// <summary>
        /// Đọc ET_NHANVIEN của dòng được chọn và mở cửa sổ ChiTietNhanSu với Owner là cửa sổ chính; không có dòng hợp lệ thì bỏ qua.
        /// </summary>
        private void dgvNhanVien_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgvNhanVien.SelectedItem is not ET_NHANVIEN nhanVien)
                return;

            var frm = new ChiTietNhanSu(nhanVien)
            {
                Owner = System.Windows.Application.Current?.MainWindow
            };
            frm.ShowDialog();
        }

        /// <summary>
        /// Tạo ET_CHUCVU từ các ô nhập, gọi BUS thêm chức vụ, thông báo kết quả và tải lại bảng chức vụ.
        /// </summary>
        private void btnThemChucVu_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!TryReadPosition(out var position, out var error)) return;
            try
            {
                var bus=new BUS_CHUCVU();
                if (!bus.themChucVu(position!)) { FormError.Text="Không thể thêm chức vụ. Nội dung nhập được giữ để thử lại."; return; }
                FormError.Text="Đã thêm chức vụ thành công.";
                try { dgvChucVu.ItemsSource=bus.layToanBoChucVu(); cboChucVu.ItemsSource=bus.layToanBoChucVu(); }
                catch(Exception ex) { FormError.Text="Chức vụ đã được lưu; chưa tải lại được danh sách: "+ex.Message; }
            }
            catch(Exception ex) { FormError.Text="Không thể thêm chức vụ: "+ex.Message; }
        }
        /// <summary>AI tạo: đọc và kiểm tra tên/mô tả chức vụ trước khi gọi BUS; mã chức vụ được tầng lưu tự cấp.</summary>
        public bool TryReadPosition(out ET_CHUCVU? position,out string error)
        {
            position=null;
            if(!KiemTraNhapLieu.PrepareSave(this,out error)) return false;
            try
            {
                var values=new Dictionary<string,object?> { ["TenChucVu"]=txtTenChucVu.Text.Trim(), ["MoTa"]=txtMoTaChucVu.Text.Trim() };
                KiemTraNhapLieu.ValidateValues("CHUC_VU",values);
                position=new ET_CHUCVU("",txtTenChucVu.Text.Trim(),txtMoTaChucVu.Text.Trim());
                FormError.Text=""; return true;
            }
            catch(LoiNhapLieu ex) { KiemTraNhapLieu.MarkError(this,ex); error=ex.Message; return false; }
        }
        /// <summary>
        /// Xóa ba ô mã, tên và mô tả chức vụ để nhập một chức vụ mới.
        /// </summary>
        public void clearTextBoxes()
        {
            txtMaChucVu.Text = string.Empty;
            txtTenChucVu.Text = string.Empty;
            txtMoTaChucVu.Text = string.Empty;
        }

        /// <summary>
        /// Xóa nội dung form chức vụ và nạp lại bảng chức vụ từ BUS.
        /// </summary>
        private void btnLamMoiChucVu_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            clearTextBoxes();
            dgvChucVu.ItemsSource = new BUS_CHUCVU().layToanBoChucVu();
        }

        /// <summary>
        /// Kiểm tra chức vụ/ngày vào làm, tạo ET_NHANVIEN từ form rồi gọi BUS thêm với mã trống để tự cấp mã; tải lại bảng nhân viên khi thành công, báo lỗi nếu có.
        /// </summary>
        private void btnThemNhanVien_Click(object sender, RoutedEventArgs e)
        {
            if(!TryReadEmployee(out var employee,out _)) return;
            try
            {
                BUS_NHANVIEN busNhanVien = new BUS_NHANVIEN();
                if (busNhanVien.themNhanVien(employee!))
                {
                    FormError.Text="Đã thêm nhân viên thành công.";
                    try { dgvNhanVien.ItemsSource = busNhanVien.layToanBoNhanVien(); }
                    catch(Exception ex) { FormError.Text="Nhân viên đã được lưu; chưa tải lại được danh sách: "+ex.Message; }
                }
                else
                {
                    FormError.Text="Không thể thêm nhân viên. Nội dung nhập được giữ để thử lại.";
                }
            }
            catch (Exception ex)
            {
                FormError.Text="Không thể thêm nhân viên: "+ex.Message;
            }
        }
        /// <summary>AI tạo: chặn tên trống, điện thoại/ngày sai, chuỗi quá dài và thiếu chức vụ/trạng thái; giữ nguyên form khi lỗi.</summary>
        public bool TryReadEmployee(out ET_NHANVIEN? employee,out string error)
        {
            employee=null;
            if(!KiemTraNhapLieu.PrepareSave(this,out error)) return false;
            try
            {
                var born=KiemTraNhapLieu.ReadDate(dpNgaySinh);
                var joined=KiemTraNhapLieu.ReadDate(dpNgayVaoLam);
                var values=new Dictionary<string,object?>
                {
                    ["HoTen"]=txtHoTen.Text.Trim(), ["GioiTinh"]=cboGioiTinh.Text,
                    ["NgaySinh"]=born, ["SDT"]=txtSDT.Text.Trim(), ["DiaChi"]=txtDiaChi.Text.Trim(),
                    ["MaChucVu"]=cboChucVu.SelectedValue, ["MaCK"]=cboChuyenKhoa.SelectedValue,
                    ["BangCap"]=txtBangCap.Text.Trim(), ["NgayVaoLam"]=joined, ["TrangThai"]=cboTrangThai.Text
                };
                KiemTraNhapLieu.ValidateValues("NHAN_VIEN",values);
                employee=new ET_NHANVIEN("",txtHoTen.Text.Trim(),cboGioiTinh.Text,born,txtSDT.Text.Trim(),
                    txtDiaChi.Text.Trim(),cboChucVu.SelectedValue!.ToString()!,cboChuyenKhoa.SelectedValue?.ToString(),
                    txtBangCap.Text.Trim(),joined!.Value,cboTrangThai.Text);
                FormError.Text=""; return true;
            }
            catch(LoiNhapLieu ex) { KiemTraNhapLieu.MarkError(this,ex); error=ex.Message; return false; }
        }
    }
}
