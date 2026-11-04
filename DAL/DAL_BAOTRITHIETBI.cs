using System.Data;
using Dapper;
using ET;

namespace DAL;

/// <summary>Mỗi phiếu gắn một MaTB/serial; SQL đồng bộ tình trạng thiết bị trong cùng giao dịch.</summary>
public class DAL_BAOTRITHIETBI : dbconnect
{
    /// <summary>
    /// Khởi tạo bảo trì thiết bị. Chuyển giao dịch tùy chọn cho lớp cơ sở; nơi cấp giao dịch chịu trách nhiệm kết thúc giao dịch đó. Gán các giá trị đầu vào vào đối tượng để nơi gọi sử dụng cùng một bản dữ liệu.
    /// </summary>
    /// <param name="transaction">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    public DAL_BAOTRITHIETBI(IDbTransaction? transaction=null):base(transaction) { }
    /// <summary>
    /// Đọc lịch sử bảo trì theo mã thiết bị/serial và thứ tự được quy định trong truy vấn.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>db.Query&lt;ET_BAOTRITHIETBI&gt;</c>.</remarks>
    /// <returns>Tập dữ liệu kiểu IEnumerable&lt;ET_BAOTRITHIETBI&gt; theo điều kiện và phạm vi xử lý của hàm.</returns>
    public IEnumerable<ET_BAOTRITHIETBI> layToanBoBaoTriThietBi()=>ThucHien((db,tx)=>
        db.Query<ET_BAOTRITHIETBI>("SELECT * FROM dbo.BAO_TRI_THIET_BI ORDER BY NgayBatDau DESC,MaBT DESC",transaction:tx).ToArray());
    /// <summary>
    /// Đọc phiếu bảo trì theo mã để xem hoặc đối chiếu trước khi thao tác.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>db.QuerySingleOrDefault&lt;ET_BAOTRITHIETBI&gt;</c>.</remarks>
    /// <param name="ma">Mã định danh của bản ghi cần xử lý.</param>
    /// <returns>Đối tượng kiểu ET_BAOTRITHIETBI được tìm hoặc tính toán; có thể null theo nhánh xử lý trong hàm.</returns>
    public ET_BAOTRITHIETBI? layBaoTriThietBiTheoMa(string ma)=>ThucHien((db,tx)=>
        db.QuerySingleOrDefault<ET_BAOTRITHIETBI>("SELECT * FROM dbo.BAO_TRI_THIET_BI WHERE MaBT=@ma",new{ma},tx));

    /// <summary>
    /// Thêm phiếu bảo trì cho đúng thiết bị sau khi kiểm tra trạng thái và nội dung nhập. Các truy vấn có gợi ý khóa giữ bản dữ liệu liên quan trong phạm vi giao dịch.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>KiemTraSchema</c>, <c>KiemTraNhap</c>, <c>db.QuerySingleOrDefault&lt;ET_THIETBI&gt;</c>.</remarks>
    /// <param name="row">Giá trị bảo trì thiết bị được truyền vào để đọc, kiểm tra hoặc cập nhật trong thao tác này.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public bool themBaoTriThietBi(ET_BAOTRITHIETBI row)=>ThucHien((db,tx)=>{
        KiemTraSchema(db,tx);
        if(row.TrangThai is not ("Chưa xác nhận" or "Đã xác nhận"))throw new InvalidOperationException("Phiếu mới chỉ được lưu Chưa xác nhận hoặc Đã xác nhận.");
        KiemTraNhap(row);
        var asset=db.QuerySingleOrDefault<ET_THIETBI>("SELECT * FROM dbo.THIET_BI WITH(UPDLOCK,HOLDLOCK) WHERE MaTB=@ma",new{ma=row.MaTB},tx);
        if(asset is null || string.IsNullOrWhiteSpace(asset.SoSerial) || asset.TinhTrang!="Hư hại" || asset.TrangThai is "Ngưng sử dụng" or "Ngừng sử dụng")
            throw new InvalidOperationException("Chỉ lập phiếu cho serial đang Hư hại và chưa ngưng sử dụng.");
        row.MaBT=TaoMa(db,tx,"BAO_TRI_THIET_BI","MaBT");
        KiemTra(db,tx,"BAO_TRI_THIET_BI",row,null,null);
        db.Execute("""
            INSERT dbo.BAO_TRI_THIET_BI(MaBT,MaTB,MaNV,NgayBatDau,NoiDung,ChiPhi,NgayBaoTriTiepTheo,TrangThai,GhiChu)
            VALUES(@MaBT,@MaTB,@MaNV,@NgayBatDau,@NoiDung,@ChiPhi,@NgayBaoTriTiepTheo,@TrangThai,@GhiChu)
            """,row,tx);
        return true;
    });
    /// <summary>
    /// Sửa phiếu bảo trì theo bản gốc và giới hạn trường được phép thay đổi.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>KiemTraSchema</c>, <c>DocKhoa</c>, <c>KiemTra</c>.</remarks>
    /// <param name="row">Giá trị bảo trì thiết bị được truyền vào để đọc, kiểm tra hoặc cập nhật trong thao tác này.</param>
    /// <param name="banGoc">Bản dữ liệu trước khi chỉnh sửa để đối chiếu với giá trị hiện tại.</param>
    /// <param name="chiDoiTrangThai">Giá trị chỉ đổi trạng thái dùng khi thực hiện thao tác; kiểu bool.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public bool suaBaoTriThietBi(ET_BAOTRITHIETBI row,ET_BAOTRITHIETBI? banGoc=null,bool chiDoiTrangThai=false)=>ThucHien((db,tx)=>{
        KiemTraSchema(db,tx);
        var current=DocKhoa(db,tx,row.MaBT);
        KiemTra(db,tx,"BAO_TRI_THIET_BI",row,current,banGoc);
        if(chiDoiTrangThai){
            if(!CungNoiDung(row,current))throw new InvalidOperationException("Chuyển trạng thái không được sửa nội dung phiếu.");
            Chuyen(db,tx,current,row.TrangThai);return true;
        }
        if(current.TrangThai!="Chưa xác nhận")throw new InvalidOperationException("Chỉ phiếu Chưa xác nhận được chỉnh sửa; phiếu đã xác nhận/kết thúc được giữ nguyên.");
        if(row.MaTB!=current.MaTB)throw new InvalidOperationException("Không đổi serial của phiếu; hãy hủy phiếu chưa xác nhận và lập phiếu mới.");
        if(row.TrangThai is not ("Chưa xác nhận" or "Đã xác nhận"))throw new InvalidOperationException("Dùng nút chuyển trạng thái của phiếu bảo trì.");
        if(row.NgayXacNhan!=current.NgayXacNhan || row.NgayThucHien!=current.NgayThucHien)throw new InvalidOperationException("Thời điểm xác nhận/thực hiện do hệ thống ghi.");
        KiemTraNhap(row);
        db.Execute("""
            UPDATE dbo.BAO_TRI_THIET_BI SET MaNV=@MaNV,NgayBatDau=@NgayBatDau,NoiDung=@NoiDung,
                ChiPhi=@ChiPhi,NgayBaoTriTiepTheo=@NgayBaoTriTiepTheo,TrangThai=@TrangThai,GhiChu=@GhiChu
            WHERE MaBT=@MaBT
            """,row,tx);
        return true;
    });
    /// <summary>
    /// Chuyển trạng thái phiếu bảo trì và đồng bộ tình trạng thiết bị liên quan trong cùng giao dịch.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>ThucHien</c>, <c>KiemTraSchema</c>, <c>ArgumentNullException.ThrowIfNull</c>, <c>DocKhoa</c>.</remarks>
    /// <param name="maBT">Mã bảo trì xác định đối tượng cần xử lý.</param>
    /// <param name="trangThaiMoi">Giá trị trạng thái mới dùng khi thực hiện thao tác; kiểu string.</param>
    /// <param name="banGoc">Bản dữ liệu trước khi chỉnh sửa để đối chiếu với giá trị hiện tại.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    public bool chuyenTrangThaiBaoTri(string maBT,string trangThaiMoi,ET_BAOTRITHIETBI banGoc)=>ThucHien((db,tx)=>{
        KiemTraSchema(db,tx);ArgumentNullException.ThrowIfNull(banGoc);
        var current=DocKhoa(db,tx,maBT);KiemTra(db,tx,"BAO_TRI_THIET_BI",current,current,banGoc);
        Chuyen(db,tx,current,trangThaiMoi);return true;
    });
    /// <summary>
    /// Từ chối xóa lịch sử bảo trì; việc hủy được thực hiện qua thao tác chuyển trạng thái.
    /// </summary>
    /// <param name="ma">Mã định danh của bản ghi cần xử lý.</param>
    /// <param name="banGoc">Bản dữ liệu trước khi chỉnh sửa để đối chiếu với giá trị hiện tại.</param>
    /// <returns>Không trả kết quả; luôn phát ngoại lệ theo quy tắc của API này.</returns>
    public bool xoaBaoTriThietBi(string ma,ET_BAOTRITHIETBI? banGoc=null)
        =>throw new InvalidOperationException("Không xóa lịch sử bảo trì. Chỉ phiếu Chưa xác nhận được chuyển sang Hủy.");
    /// <summary>
    /// Kiểm tra cấu trúc SQL cần thiết trước khi thực hiện nghiệp vụ.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>db.Execute</c>.</remarks>
    /// <param name="db">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    private static void KiemTraSchema(IDbConnection db,IDbTransaction? tx)=>db.Execute("""
        IF OBJECT_ID(N'dbo.usp_RHM_KiemTraBaoTriSerial',N'P') IS NULL
         THROW 52600,N'Chạy toàn bộ Database/QLBenhVienRangHamMat.sql để đồng bộ bảo trì theo serial.',1;
        EXEC dbo.usp_RHM_KiemTraBaoTriSerial;
        """,transaction:tx);
    /// <summary>
    /// Đọc bản dữ liệu có khóa phục vụ đối chiếu và cập nhật. Các truy vấn có gợi ý khóa giữ bản dữ liệu liên quan trong phạm vi giao dịch.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>db.QuerySingleOrDefault&lt;ET_BAOTRITHIETBI&gt;</c>.</remarks>
    /// <param name="db">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    /// <param name="ma">Mã định danh của bản ghi cần xử lý.</param>
    /// <returns>Kết quả kiểu ET_BAOTRITHIETBI dùng cho bước xử lý tiếp theo.</returns>
    private static ET_BAOTRITHIETBI DocKhoa(IDbConnection db,IDbTransaction? tx,string ma)=>
        db.QuerySingleOrDefault<ET_BAOTRITHIETBI>("SELECT * FROM dbo.BAO_TRI_THIET_BI WITH(UPDLOCK,HOLDLOCK) WHERE MaBT=@ma",new{ma},tx)
        ??throw new InvalidOperationException("Phiếu không còn tồn tại. Vui lòng tải lại.");
    /// <summary>
    /// Kiểm tra và thực hiện chuyển trạng thái bảo trì theo các giá trị đầu vào và bản hiện tại.
    /// </summary>
    /// <remarks>Các bước được phối hợp qua <c>db.Execute</c>.</remarks>
    /// <param name="db">Kết nối SQL đang dùng; các lệnh trong thao tác này thực thi trên kết nối đó.</param>
    /// <param name="tx">Giao dịch dùng chung; null nếu nơi gọi không truyền giao dịch. Quyền commit/rollback thuộc nơi quản lý giao dịch.</param>
    /// <param name="current">Giá trị hiện tại dùng khi thực hiện thao tác; kiểu ET_BAOTRITHIETBI.</param>
    /// <param name="target">Giá trị đích dùng khi thực hiện thao tác; kiểu string.</param>
    private static void Chuyen(IDbConnection db,IDbTransaction? tx,ET_BAOTRITHIETBI current,string target){
        bool valid=(current.TrangThai,target) is ("Chưa xác nhận","Đã xác nhận") or ("Chưa xác nhận","Hủy") or ("Đã xác nhận","Đang thực hiện") or ("Đang thực hiện","Đã hoàn thành");
        if(!valid)throw new InvalidOperationException("Không thể chuyển phiếu từ "+current.TrangThai+" sang "+target+". Hãy tải lại và chọn thao tác phù hợp.");
        db.Execute("UPDATE dbo.BAO_TRI_THIET_BI SET TrangThai=@target WHERE MaBT=@ma",new{target,ma=current.MaBT},tx);
    }
    /// <summary>
    /// So sánh các trường nội dung để nhận diện thay đổi ngoài trạng thái.
    /// </summary>
    /// <param name="a">Giá trị a dùng khi thực hiện thao tác; kiểu ET_BAOTRITHIETBI.</param>
    /// <param name="b">Giá trị b dùng khi thực hiện thao tác; kiểu ET_BAOTRITHIETBI.</param>
    /// <returns>Kết quả đúng/sai của điều kiện hoặc thao tác được biểu diễn trong thân hàm; lỗi có thể được phát ra bằng ngoại lệ.</returns>
    private static bool CungNoiDung(ET_BAOTRITHIETBI a,ET_BAOTRITHIETBI b)=>a.MaTB==b.MaTB && a.MaNV==b.MaNV && a.NgayBatDau==b.NgayBatDau && a.NoiDung==b.NoiDung && a.ChiPhi==b.ChiPhi
        && a.NgayBaoTriTiepTheo==b.NgayBaoTriTiepTheo && a.GhiChu==b.GhiChu && a.NgayKetThuc==b.NgayKetThuc && a.NgayXacNhan==b.NgayXacNhan && a.NgayThucHien==b.NgayThucHien;
    /// <summary>
    /// Kiểm tra giá trị nhập trước khi ghi chứng từ nghiệp vụ.
    /// </summary>
    /// <param name="row">Giá trị bảo trì thiết bị được truyền vào để đọc, kiểm tra hoặc cập nhật trong thao tác này.</param>
    private static void KiemTraNhap(ET_BAOTRITHIETBI row){
        if(string.IsNullOrWhiteSpace(row.MaTB)||string.IsNullOrWhiteSpace(row.MaNV)||string.IsNullOrWhiteSpace(row.NoiDung))throw new InvalidOperationException("Chọn serial, nhân viên phụ trách và nhập nội dung bảo trì.");
        if(row.ChiPhi<0)throw new InvalidOperationException("Chi phí bảo trì không được âm.");
        if(row.NgayKetThuc is not null)throw new InvalidOperationException("Ngày kết thúc do hệ thống ghi khi bấm Hoàn thành.");
        if(row.NgayBaoTriTiepTheo?.Date<=row.NgayBatDau.Date)throw new InvalidOperationException("Lịch bảo trì tiếp theo phải sau ngày dự kiến bắt đầu.");
    }
}
