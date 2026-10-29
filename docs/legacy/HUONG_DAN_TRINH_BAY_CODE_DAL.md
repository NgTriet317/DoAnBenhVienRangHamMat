# Cách đọc và trình bày dbconnect, DAL

Sau lượt tinh chỉnh ngày 13/10/2026, code được viết theo từng bước với vòng lặp, biến rõ nghĩa và hàm có nhiệm vụ cụ thể. Các API đang dùng bởi BUS/giao diện và câu lệnh SQL giữ nguyên tác dụng.

## 1. Nên giới thiệu dự án theo luồng nào?

**XAML → code-behind → BUS → DAL → SQL Server.**

- XAML khai báo nút, ô nhập và bảng.
- Code-behind nhận thao tác, kiểm tra đầu vào và hiển thị thông báo.
- BUS cung cấp các hàm nghiệp vụ cho giao diện gọi.
- DAL đọc/ghi SQL và kiểm tra điều kiện khi lưu.
- ET chứa dữ liệu của từng bảng; Dapper đưa kết quả SQL vào ET.

Có thể trình bày: “Em chia chương trình thành các tầng. Khi thêm bệnh nhân, giao diện lấy thông tin, gọi BUS; BUS chuyển tới DAL. DAL kiểm tra và thực hiện INSERT có tham số. Khi thành công, giao diện tải lại danh sách.”

Phần phân quyền/đăng nhập và các form mẫu còn chưa nối lưu được ghi rõ trong [báo cáo nhập liệu](KIEM_TRA_NHAP_LIEU.md); không trình bày chúng như chức năng đã triển khai đầy đủ.

## 2. dbconnect làm những việc gì?

Đọc [dbconnect.cs](../../DAL/dbconnect.cs) theo các nhóm sau:

| Nhóm hàm | Nhiệm vụ | Cách giải thích |
|---|---|---|
| CreateConnection, MoKetNoi | Lấy chuỗi cấu hình, tạo và mở kết nối | “Chuỗi kết nối xác định SQL Server/database; Open bắt đầu kết nối.” |
| BatDauGiaoDich, ThucHien | Quản lý giao dịch chung hoặc giao dịch riêng | “Cùng một lần lưu, các lệnh dùng chung kết nối/giao dịch. Thành công thì commit, lỗi thì rollback.” |
| TaoMa, DinhDangMa | Cấp mã không trùng | “Lấy phần số lớn nhất cộng một, ghép với chữ đầu tên bảng; BENH_NHAN tạo BN01, BN02…” |
| DocThongTinCot | Đọc quy tắc cột SQL | “Lấy kiểu, độ dài và quyền để trống của cột để kiểm tra ET.” |
| ChuanHoaGiaTri và bốn hàm ChuanHoa… | Đưa giá trị về đúng kiểu/giới hạn | “Chuỗi được Trim; số kiểm tra phần lẻ; ngày/giờ theo độ chính xác SQL.” |
| GiaTri, KiemTra, ChoPhepGhi | Kiểm tra dữ liệu và bản gốc | “Duyệt từng thuộc tính ET, bỏ trường không ghi SQL, kiểm tra trước khi lưu và phát hiện bản ghi cũ.” |
| KiemTraChiDoiTrangThai | Bảo vệ thao tác chỉ đổi trạng thái | “Khi xác nhận/hủy, các trường nội dung phải giữ nguyên.” |

### ThucHien có ba trường hợp

1. DAL đã được truyền transaction: dùng transaction đó; nơi gọi quyết định commit/rollback.
2. Đang có TransactionScope: kết nối tự tham gia giao dịch của phạm vi đó.
3. Không có giao dịch chung: DAL mở transaction riêng, chạy đoạn SQL, commit khi thành công và rollback khi có ngoại lệ.

`Func<IDbConnection, IDbTransaction?, T>` là một hàm được truyền vào. Nó nhận kết nối và transaction rồi trả kết quả kiểu T. T có thể là bool, một ET, danh sách ET hoặc mã chứng từ. Những DAL có cùng cách mở/đóng kết nối dùng lại ThucHien.

Ví dụ trong DAL:

```csharp
return ThucHien((connection, transaction) =>
{
    // Chạy SQL bằng connection và transaction được hạ tầng cung cấp.
    // Trả kết quả cho nơi gọi.
    return true;
});
```

### Các bước của KiemTra

1. Lấy thông tin các cột từ SQL.
2. Lấy giá trị mới, giá trị hiện tại và bản gốc đã đọc.
3. Duyệt từng cột bằng foreach.
4. Bỏ cột SQL tự tính hoặc thuộc tính có Write(false).
5. Chuẩn hóa giá trị và điền lại vào ET.
6. Khi sửa/xóa có bản gốc, đối chiếu với dữ liệu hiện tại; khác nhau thì yêu cầu tải lại.

`TypeDescriptor.GetProperties` cung cấp thông tin thuộc tính ET: tên, giá trị và các attribute. Code hiện dùng PropertyDescriptor thống nhất để đọc/ghi giá trị và kiểm tra WriteAttribute, thay cho tra thuộc tính bằng hai API khác nhau.

Các thành phần metadata Column:

| Thuộc tính | Ý nghĩa |
|---|---|
| Name | Tên cột, tương ứng tên property ET |
| Type | Kiểu SQL: int, decimal, date, nvarchar… |
| Nullable | Có được bỏ trống hay không |
| Computed | SQL tự tính giá trị |
| Length | Độ dài theo byte; Unicode dùng 2 byte/ký tự |
| Scale | Số chữ số lẻ cho số/thời gian |

LamTronTicks giữ độ chính xác thời gian giống SQL: scale 0 tới giây, scale 3 tới mili giây. Một giây có 10.000.000 ticks. Việc làm tròn này giúp đọc lại dữ liệu không bị báo nhầm là bản ghi đã thay đổi.

## 3. Đọc một DAL cụ thể

Bắt đầu bằng [DAL_BENHNHAN.cs](../../DAL/DAL_BENHNHAN.cs), rồi đối chiếu một DAL khác.

**Đọc:** chạy SELECT → Dapper chuyển thành ET → trả kết quả.

**Thêm:** cấp mã → kiểm tra ET → INSERT → trả thành công.

**Sửa:** đọc bản hiện tại có khóa → báo lỗi nếu không còn → kiểm tra bản gốc/dữ liệu mới → kiểm tra chỉ đổi trạng thái nếu cần → UPDATE.

**Xóa:** đọc bản hiện tại có khóa → kiểm tra bản gốc/nghiệp vụ → DELETE. Nếu có liên kết không được xóa, ràng buộc SQL sẽ chặn.

Câu lệnh dùng `@Ma`, `@HoTenBN`… là tham số. Dapper lấy giá trị từ đối tượng được truyền vào. Dữ liệu nhập như `O'Brien` được giữ đúng và không phải nối vào câu lệnh SQL.

`banGoc` là bản người dùng đã đọc lúc mở form. `hienTai` là bản DAL vừa lấy lại từ SQL. So sánh hai bản giúp chặn cửa sổ cũ ghi đè thay đổi ở cửa sổ khác.

Các hàm private KiemTraNghiepVu đã bỏ tham số không dùng và dùng tên rõ nghĩa như duLieu, banHienTai, trangThaiCu, laBanNhap. Phần điều kiện vẫn được giữ vì mỗi chứng từ có trạng thái/liên kết riêng.

## 4. Cách trình bày hóa đơn, thanh toán, cấp thuốc

### Hóa đơn

Trong [DAL_HOADON.cs](../../DAL/DAL_HOADON.cs):

1. Kiểm tra nhân viên và lần khám đã xác nhận.
2. Khóa lần khám, lấy khoản thu chưa đưa vào hóa đơn.
3. Kiểm tra giảm giá.
4. Tạo đầu hóa đơn Nháp.
5. Thêm các dòng chi tiết.
6. Áp dụng giảm giá rồi hoàn tất giao dịch.

### Thu/hoàn tiền

Trong [DAL_THANHTOAN.cs](../../DAL/DAL_THANHTOAN.cs):

1. Kiểm tra nhân viên và khóa hóa đơn.
2. Kiểm tra hóa đơn đã xác nhận, số tiền, loại giao dịch, phương thức, lý do.
3. Tính thu ròng = tổng Thu đã xác nhận − tổng Hoàn đã xác nhận.
4. Nếu Thu: không vượt phần hóa đơn còn phải trả.
5. Nếu Hoàn: không vượt tiền thu ròng.
6. Ghi giao dịch trong cùng transaction.

SoTien của cả Thu và Hoàn đều lưu là số dương; LoaiGiaoDich quyết định cộng hay trừ khi tính số dư.

### Cấp thuốc

Trong [DAL_PHIEUXUAT.cs](../../DAL/DAL_PHIEUXUAT.cs):

1. Kiểm tra kho, nhân viên, đơn đã xác nhận và chưa cấp.
2. Lấy thuốc cần cấp và các lô còn hạn/còn tồn theo thứ tự SQL.
3. LapPhuongAnCapThuoc duyệt từng thuốc, rồi từng lô phù hợp.
4. Lượng lấy = nhỏ hơn giữa lượng còn thiếu và tồn của lô.
5. Giảm tồn tạm; đủ thuốc thì chuyển sang dòng tiếp theo.
6. Thiếu một thuốc thì báo lỗi trước khi ghi phiếu.
7. Đủ toàn bộ đơn mới tạo đầu phiếu, các dòng và ghi sổ.

PhanBoThuoc là lớp dữ liệu nhỏ có MaCTDT, MaHH, MaLo, SoLuong. Nó giúp đọc phương án cấp thuốc trực tiếp qua tên trường giống ET.

## 5. Những câu hỏi thầy có thể hỏi

| Câu hỏi | Ý chính để trả lời |
|---|---|
| Vì sao có transaction? | Đơn/phiếu nhiều dòng phải lưu đủ; lỗi một dòng thì hủy toàn bộ, không để chứng từ dở dang. |
| Vì sao dùng khóa dữ liệu? | Hai quầy có thể cùng cấp mã, cùng thu/hoàn hoặc cùng lấy thuốc; khóa giữ dữ liệu ổn định trong lần xử lý. |
| UPDLOCK/HOLDLOCK có tác dụng gì? | Giữ khóa bản đang đọc trong transaction để thao tác ghi tiếp theo không bị xen ngang. |
| Vì sao có khóa ứng dụng khi cấp mã? | Hai thao tác cùng bảng không được lấy cùng phần số kế tiếp. |
| Vì sao kiểm tra bản gốc? | Để form mở trước không ghi đè dữ liệu vừa được sửa ở nơi khác. |
| Vì sao SQL còn có CHECK/FOREIGN KEY/trigger? | Dữ liệu phải nhất quán dù có nhiều nơi gọi lưu. Trigger xử lý tính tổng/khóa chứng từ ở SQL. |
| Vì sao có Write(false)? | Một số trường do SQL tính/cập nhật; ET nhận giá trị khi đọc, nhưng không tự ghi đè. |
| Vì sao dùng generic T? | Các lớp ET khác nhau dùng chung quy trình; T giữ kiểu kết quả/đối tượng và giúp tránh truyền lẫn thực thể. |
| Vì sao thông báo lỗi bằng ngoại lệ? | Dừng luồng đang ghi, rollback khi cần, rồi giao diện báo lỗi và giữ form để người dùng sửa. |

## 6. Phần đã đơn giản hóa và kiểm chứng

- Rà và chỉnh 40 file DAL, gồm dbconnect và 39 DAL thực thể.
- Gom 34 đoạn kiểm tra chỉ đổi trạng thái về một hàm dùng chung.
- Bỏ 8 hàm bọc chỉ chuyển tiếp và 25 tham số private không dùng.
- Đổi 48 tên tham số/biến private sang tên rõ nghĩa; giữ API đang được BUS/giao diện gọi.
- Tách xử lý số, chuỗi, ngày giờ và giờ thành các hàm nhỏ.
- Viết SQL dài trên nhiều dòng; tách truy vấn bản ghi khỏi nhánh báo không tìm thấy.
- Cấp thuốc dùng lớp dữ liệu và vòng lặp; thu/hoàn dùng hai nhánh rõ ràng.

Kiểm chứng: 26.566 ca chuyển kiểu/định dạng mã khớp bản trước; quy tắc Write của 303 thuộc tính ET khớp; SQL và API hiện có của 40 file được đối chiếu. Persistence 16, Finance 54 và WPF/531 kiểm tra nhập liệu đạt, các bản ghi thử rollback. Build còn 6 cảnh báo đã có từ trước, không phát sinh cảnh báo mới.
