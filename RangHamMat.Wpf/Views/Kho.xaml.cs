using System;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using BUS;
using ET;

namespace RangHamMat.Views
{
    /// <summary>
    /// Phần xử lý kho của màn hình, phối hợp dữ liệu và các sự kiện khai báo trong XAML.
    /// </summary>
    public partial class Kho : UserControl
    {
        /// <summary>
        /// Đối tượng nghiệp vụ kho được màn hình dùng để đọc/ghi dữ liệu.
        /// </summary>
        private readonly BUS_KHO busKho = new BUS_KHO();
        /// <summary>
        /// Đối tượng nghiệp vụ nhân viên được màn hình dùng để đọc/ghi dữ liệu.
        /// </summary>
        private readonly BUS_NHANVIEN busNhanVien = new BUS_NHANVIEN();

        /// <summary>
        /// Danh sách tồn dùng trong kho được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private DataTable danhSachTon = new DataTable();
        /// <summary>
        /// Danh sách lô dùng trong kho được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private DataTable danhSachLo = new DataTable();

        /// <summary>
        /// Lọc tìm kiểm được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string locTimKiem = "";
        /// <summary>
        /// Lọc trạng thái được giữ trong phạm vi lớp để các thao tác dùng lại.
        /// </summary>
        private string locTrangThai = "";

        /// <summary>
        /// Cờ Đang tải; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
        /// </summary>
        private bool dangTai;
        /// <summary>
        /// Cờ Đã tải; các nhánh xử lý kiểm tra cờ này trước khi tiếp tục.
        /// </summary>
        private bool daTai;

        /// <summary>
        /// Khởi tạo kho. Nạp giao diện từ XAML trước khi thiết lập dữ liệu, quyền và sự kiện của cửa sổ/điều khiển.
        /// </summary>
        public Kho()
        {
            InitializeComponent();

            Loaded += (sender, e) => { if (!daTai) hienThiDuLieu(); };
        }


        //Hàm load dữ liệu lên cbo
        public void hienThiDuLieu()
        {
            if (dangTai) return;

            dangTai = true;
            try
            {
                string? maKhoCu = cboKho.SelectedValue as string;
                var cacKho = busKho.layToanBoKho().ToList();

                cboKho.ItemsSource = cacKho;
                cboKho.SelectedValue = maKhoCu;

                if (cboKho.SelectedIndex < 0 && cacKho.Count > 0)
                {
                    cboKho.SelectedIndex = 0; 
                }

                daTai = true;
            }
            catch (Exception ex)
            {
                daTai = false;
                MessageBox.Show("Không thể tải danh sách kho: " + ex.Message,
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                dangTai = false;
            }

            if (daTai) TaiTonKho();
        }

        //hàm load dữ liệu lên gridview có kèm bộ lọc và tìm kiếm
        private void TaiTonKho()
        {
            if (dangTai) return;

            dangTai = true;
            try
            {
                var kho = cboKho.SelectedItem as ET_KHO;
                lblTenKho.Text = kho?.TenKho ?? "Danh sách tồn kho";
                lblNguoiPhuTrach.Text = "Người phụ trách: Chưa xác định";

                if(kho == null)
                {
                    lblMoTaKho.Text = "Chưa có kho";
                    danhSachLo = new DataTable();
                    danhSachTon = new DataTable();
                }
                else
                {
                    lblMoTaKho.Text = "Loại kho: " + kho.LoaiKho + " • " + kho.TrangThai;
                    var nguoiPhuTrach = busNhanVien.layNhanVienTheoMa(kho.MaNV);
                    lblNguoiPhuTrach.Text = "Người phụ trách: " + (nguoiPhuTrach?.HoTen ?? "Chưa xác định");
                    danhSachLo = busKho.layLoTonTheoKho(kho.MaKho);
                    danhSachTon = busKho.layTonKhoTheoKho(kho.MaKho);
                }
                               
                lblLoSapHetHan.Text = danhSachLo.Columns.Count == 0 ? "0" : danhSachLo.AsEnumerable()
                    .Count(r => r.Field<string>("CanhBaoHan") == "Sắp hết hạn").ToString();

                //đếm số lượng mặt hàng
                lblTongMatHang.Text = danhSachTon.AsEnumerable()
                    .Count(r => r.Field<decimal>("SoLuongTon") > 0).ToString();

                //đếm sl cần bổ sung
                lblCanBoSung.Text = danhSachTon.AsEnumerable()
                    .Count(r => r.Field<decimal>("SoLuongTon") < r.Field<decimal>("MucTonToiThieu")).ToString();

                gridTonKho.ItemsSource = null;

                if (danhSachTon.Columns.Count > 0)
                {
                    //search kết hợp text và cbo
                    string dieuKien = locTimKiem;
                    if (locTrangThai != "")
                    {
                        if (dieuKien != "")
                            dieuKien += " AND ";

                        dieuKien += locTrangThai;
                    }
                    danhSachTon.DefaultView.RowFilter = dieuKien;

                    //sort theo tên HH bằng hàm của datatable
                    danhSachTon.DefaultView.Sort = "TenHH ASC";

                    gridTonKho.ItemsSource = danhSachTon.DefaultView;
                    if (danhSachTon.DefaultView.Count > 0)
                    {
                        gridTonKho.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                danhSachTon = new DataTable();
                danhSachLo = new DataTable();
                gridTonKho.ItemsSource = null;
                lblTongMatHang.Text = "0";
                lblCanBoSung.Text = "0";
                lblLoSapHetHan.Text = "0";
                lblNguoiPhuTrach.Text = "Người phụ trách: Chưa xác định";

                MessageBox.Show("Không thể tải tồn kho: " + ex.Message,
                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                dangTai = false;
            }
        }

        /// <summary>
        /// Phản ánh lựa chọn hộp chọn kho vào dữ liệu và phần hiển thị liên quan.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>TaiTonKho</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void cboKho_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (daTai && !dangTai) TaiTonKho();
        }

        /// <summary>
        /// Xử lý thao tác làm mới khi người dùng bấm nút tương ứng trên màn hình.
        /// </summary>
        /// <remarks>Các bước được phối hợp qua <c>hienThiDuLieu</c>.</remarks>
        /// <param name="sender">Đối tượng phát sinh sự kiện giao diện.</param>
        /// <param name="e">Thông tin sự kiện do WPF cung cấp cho lần gọi này.</param>
        private void LamMoi_Click(object sender, RoutedEventArgs e)
        {
            hienThiDuLieu();
        }

        //tìm kiếm theo tên hàng hóa
        private void txtTimKiem_TextChanged(object sender, TextChangedEventArgs e)
        {
            //clone nội dung ra thành 1 bản đã đc lọc toàn bộ kí tự đặc biệt
            string noiDung = Regex.Replace(txtTimKiem.Text, @"[^\p{L}\p{M}\p{N} ]", "");

            //check xem trc và sau lọc có trùng nhau không
            if (txtTimKiem.Text != noiDung)
            {
                int viTri = txtTimKiem.SelectionStart;
                //cập nhật lại text tìm kiếm = nội dung đã lọc ra
                txtTimKiem.Text = noiDung;
                txtTimKiem.SelectionStart = Math.Min(viTri, noiDung.Length);//đặt lại con trỏ chuột vào text
                return;
            }

            string timKiem = noiDung.Trim();
            locTimKiem = timKiem == "" ? "" : $"TenHH LIKE '%{timKiem}%'";

            //ko có dữ liệu thì ko tìm
            if (danhSachTon.Columns.Count == 0) return;

            //Tìm kiếm kết hợp text và trạng thái
            string dieuKien = locTimKiem;

            if (locTrangThai != "")
            {
                if (dieuKien != "")
                {
                    dieuKien += " AND ";
                }

                dieuKien += locTrangThai;
            }

            danhSachTon.DefaultView.RowFilter = dieuKien;//tìm kiếm bằng rf của datatable, không truy db

            //bind selected vào row đầu tiên
            if (gridTonKho.SelectedItem == null && danhSachTon.DefaultView.Count > 0)
            {
                gridTonKho.SelectedIndex = 0; 
            }
        }

        //Lọc danh sách theo cbo trạng thái
        private void cboTrangThai_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string trangThai = (cboTrangThai.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            
            //empty hoặc all thì ko lọc
            if(trangThai == "" || trangThai == "Tất cả tình trạng")
            {
                locTrangThai = "";
            }
            else
            {
                locTrangThai = $"TrangThaiTon = '{trangThai}'";
            }
                            
            if (danhSachTon.Columns.Count == 0) return;//dữ liệu ko có thì back về

            //Tìm kiếm kết hợp text và trạng thái
            string dieuKien = locTimKiem;

            if (locTrangThai != "")
            {
                if (dieuKien != "")
                {
                    dieuKien += " AND ";
                }

                dieuKien += locTrangThai;
            }

            danhSachTon.DefaultView.RowFilter = dieuKien;//tìm kiếm bằng rf của datatable, không truy db

            //bind seleted row về hàng đầu tiên
            if (gridTonKho.SelectedItem == null && danhSachTon.DefaultView.Count > 0)
            {
                gridTonKho.SelectedIndex = 0; 
            }
        }

        //click vào row để lấy dữ liệu
        private void gridTonKho_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridTonKho.SelectedItem is DataRowView hang)
            {
                lblTenHang.Text = hang["TenHH"].ToString();
                lblSoLuong.Text = Convert.ToDecimal(hang["SoLuongTon"]).ToString("N3");
                lblMucToiThieu.Text = Convert.ToDecimal(hang["MucTonToiThieu"]).ToString("N3");

                //lấy thông tin lô hàng từ hàng hóa show lên grid
                if (danhSachLo.Columns.Count > 0)
                {
                    string maHH = hang["MaHH"].ToString()!;
                    danhSachLo.DefaultView.RowFilter = $"MaHH = '{maHH}'";
                    gridLoHang.ItemsSource = danhSachLo.DefaultView;
                }
                else
                {
                    gridLoHang.ItemsSource = null;
                }
            }
            else
            {
                lblTenHang.Text = "Chưa chọn mặt hàng";
                lblSoLuong.Text = "—";
                lblMucToiThieu.Text = "—";
                gridLoHang.ItemsSource = null;
            }
        }
    }
}
