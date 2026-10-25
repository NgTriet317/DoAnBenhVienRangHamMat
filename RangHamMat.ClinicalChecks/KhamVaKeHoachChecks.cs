using BUS;
using Dapper;
using ET;
using Microsoft.Data.SqlClient;
namespace RangHamMat.ClinicalChecks;

internal static class KhamVaKeHoachChecks
{
    internal static void Need(bool ok,string message){if(!ok)throw new Exception(message);}
    internal static void Denied(Action run){try{run();}catch(TuChoiQuyenNghiepVuException){return;}throw new Exception("Phải từ chối quyền");}
    internal static void Invalid(Action run,string field){try{run();}catch(LoiKhamException ex)when(ex.Truong==field){return;}throw new Exception("Phải chặn "+field);}
    internal static void Conflict(Action run){try{run();}catch(XungDotDuLieuKhamException){return;}throw new Exception("Phải báo xung đột snapshot");}
    internal static YeuCauLuuKham Request(BanNhapKham s)=>new(){MaLS=s.LanKham.MaLS,DauVanTay=s.DauVanTay,TrieuChung="FIXTURE triệu chứng",ChanDoan="FIXTURE chẩn đoán",KeHoachXuLy="FIXTURE xử trí",TinhTrang=[new(){LoaiTinhTrang="FIXTURE",MoTaTinhTrang="FIXTURE mô tả",MucDo="FIXTURE mức độ"}]};
    internal static BanNhapMauDieuTri Template(Fixture f,BUS_KHAMDIEUTRI bus,NguCanhNghiepVu user,bool approved=true,string? category=null,string? severity="FIXTURE mức độ")
    {
        var cat=new ET_DMTINHTRANGBENH {TenTinhTrang="FIXTURE không dùng thật",TrangThai="Đang áp dụng"};
        if(category is null)new BUS_DMTINHTRANGBENH(f.Transaction).LuuNhap(cat,user);else cat.MaDMTT=category;
        var m=bus.LuuMauDieuTri(new(){MaDMTT=cat.MaDMTT,TenMau="FIXTURE không dùng thật",MucDo=severity,CacBuoc=[new(){MaDV=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU WHERE TrangThai=N'Đang hoạt động' ORDER BY MaDV"),NoiDungBuoc="FIXTURE nội dung không dùng thật",SoLuongDuKien=1.125m,LanThu=1}]},user);
        return approved?bus.DuyetMauDieuTri(m.Mau.MaMauKH,m.DauVanTay,user):m;
    }
    internal static IEnumerable<(string,Action<Fixture>)> Cases()
    {
        foreach (var change in new[]
        {
            (Name:"SQL chặn sửa tình trạng sau hoàn tất qua API khám",Sql:"UPDATE dbo.TINH_TRANG_BENH SET MoTaTinhTrang=N'FIXTURE ghi đè' WHERE MaTTB=@id"),
            (Name:"SQL chặn xóa tình trạng sau hoàn tất qua API khám",Sql:"DELETE dbo.TINH_TRANG_BENH WHERE MaTTB=@id")
        })
            yield return(change.Name,f=>
            {
                f.Execute("DELETE dbo.CHI_TIET_DON_THUOC WHERE MaDT=@id; DELETE dbo.DON_THUOC WHERE MaDT=@id",new{id=f.Id("DTA")});
                var u=f.User("A");var b=new BUS_KHAMDIEUTRI(f.Transaction);
                var s=b.LuuKham(Request(b.LayBanNhap(f.Id("LSA"),u)) with {KhongKeThuoc=true,LyDoKhongKeThuoc="FIXTURE"},u);
                b.HoanTatKham(f.Id("LSA"),s.DauVanTay,u);
                try {f.Execute(change.Sql,new{id=s.TinhTrang.Single().MaTTB});}
                catch(SqlException ex) when(ex.Errors.Cast<SqlError>().Any(e=>e.Number==52111)){return;}
                throw new Exception("SQL không giữ tình trạng đã ký bằng lỗi 52111.");
            });
        yield return("Danh mục ngừng áp dụng không khóa ghi chú tình trạng đã ghi trước đó",f=>
        {
            var u=f.User("A");var b=new BUS_KHAMDIEUTRI(f.Transaction);var m=Template(f,b,u,false);var r=Request(b.LayBanNhap(f.Id("LSA"),u));r.TinhTrang[0].MaDMTT=m.Mau.MaDMTT;var s=b.LuuKham(r,u);
            new BUS_DMTINHTRANGBENH(f.Transaction).LuuNhap(new(){MaDMTT=m.Mau.MaDMTT,TenTinhTrang="FIXTURE không dùng thật",TrangThai="Ngừng áp dụng"},u);
            var old=s.TinhTrang[0];var saved=b.LuuKham(r with {DauVanTay=s.DauVanTay,TinhTrang=[new(){MaTTB=old.MaTTB,MaDMTT=old.MaDMTT,LoaiTinhTrang=old.LoaiTinhTrang,MoTaTinhTrang=old.MoTaTinhTrang,MucDo=old.MucDo,GhiChu="FIXTURE bổ sung trước ký"}]},u);
            Need(saved.TinhTrang[0].GhiChu=="FIXTURE bổ sung trước ký","Danh mục nghỉ khóa hồ sơ đang khám");
        });
        yield return("Kế hoạch nháp phải xử lý, kế hoạch tương lai đã duyệt cho đóng buổi",f=>
        {
            f.Execute("DELETE dbo.CHI_TIET_DON_THUOC WHERE MaDT=@id;DELETE dbo.DON_THUOC WHERE MaDT=@id",new{id=f.Id("DTA")});var u=f.User("A");var b=new BUS_KHAMDIEUTRI(f.Transaction);var s=b.LuuKham(Request(b.LayBanNhap(f.Id("LSA"),u)) with {KhongKeThuoc=true,LyDoKhongKeThuoc="FIXTURE"},u);
            var p=b.LuuKeHoach(new(){MaLS=f.Id("LSA"),CacBuoc=[new(){MaDV=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU ORDER BY MaDV"),NoiDungBuoc="FIXTURE buổi tương lai",LanThu=2}]},u);Invalid(()=>b.HoanTatKham(f.Id("LSA"),s.DauVanTay,u),"KeHoach");
            b.XacNhanKeHoach(p.KeHoach.MaKHDT,p.DauVanTay,u);Need(b.HoanTatKham(f.Id("LSA"),s.DauVanTay,u).LanKham.TrangThai=="Đã xác nhận","Buộc thực hiện hết kế hoạch tương lai");
        });
        yield return("Kế hoạch nháp rỗng có thể hủy, không xóa lịch sử hoặc chặn buổi mãi",f=>
        {
            var u=f.User("A");var b=new BUS_KHAMDIEUTRI(f.Transaction);var p=b.LuuKeHoach(new(){MaLS=f.Id("LSA")},u);var result=b.HuyKeHoach(p.KeHoach.MaKHDT,p.DauVanTay,u);
            Need(result.KeHoach.TrangThai=="Hủy" && f.Scalar<int>("SELECT COUNT(*) FROM dbo.KE_HOACH_DIEU_TRI WHERE MaKHDT=@id",new{id=p.KeHoach.MaKHDT})==1,"Hủy làm mất kế hoạch");
        });
        yield return("Hai cửa sổ kế hoạch không ký bản đã bị sửa",f=>
        {
            var u=f.User("A");var b=new BUS_KHAMDIEUTRI(f.Transaction);var p=b.LuuKeHoach(new(){MaLS=f.Id("LSA")},u);b.LuuKeHoach(new(){MaLS=f.Id("LSA"),MaKHDT=p.KeHoach.MaKHDT,DauVanTay=p.DauVanTay,GhiChu="FIXTURE mới"},u);
            Conflict(()=>b.XacNhanKeHoach(p.KeHoach.MaKHDT,p.DauVanTay,u));
        });
        yield return("Còn dịch vụ nháp thì không đóng chuyên môn",f=>
        {
            f.Execute("DELETE dbo.CHI_TIET_DON_THUOC WHERE MaDT=@id;DELETE dbo.DON_THUOC WHERE MaDT=@id",new{id=f.Id("DTA")});var u=f.User("A");var b=new BUS_KHAMDIEUTRI(f.Transaction);var s=b.LuuKham(Request(b.LayBanNhap(f.Id("LSA"),u)) with {KhongKeThuoc=true,LyDoKhongKeThuoc="FIXTURE"},u);
            f.Execute("INSERT dbo.DICH_VU_THUC_HIEN(MaDVThucHien,MaLS,MaDV,MaNVThucHien,ThoiGianThucHien,SoLuong,DonGia,KetQua,TrangThai) VALUES(@id,@ls,(SELECT TOP(1) MaDV FROM dbo.DICH_VU ORDER BY MaDV),@nv,SYSDATETIME(),1,1,N'FIXTURE',N'Nháp')",new{id=f.Id("DVP5"),ls=f.Id("LSA"),nv=u.MaNV});
            Invalid(()=>b.HoanTatKham(f.Id("LSA"),s.DauVanTay,u),"DichVu");
        });
        yield return("Lỗi dòng tình trạng thứ hai không để lại dòng đầu trong transaction ngoài",f=>
        {
            var u=f.User("A");var b=new BUS_KHAMDIEUTRI(f.Transaction);var r=Request(b.LayBanNhap(f.Id("LSA"),u));
            Invalid(()=>b.LuuKham(r with {TinhTrang=[r.TinhTrang[0],new(){LoaiTinhTrang="FIXTURE",MoTaTinhTrang="FIXTURE",MucDo=""}]},u),"TinhTrang");
            Need(f.Scalar<int>("SELECT COUNT(*) FROM dbo.TINH_TRANG_BENH WHERE MaLS=@id",new{id=f.Id("LSA")})==0,"Lưu dở dòng tình trạng đầu");
        });
        yield return("Gợi ý và áp mẫu dùng cùng quy tắc so mức độ của SQL",f=>
        {
            var u=f.User("A");var b=new BUS_KHAMDIEUTRI(f.Transaction);var m=Template(f,b,u);var r=Request(b.LayBanNhap(f.Id("LSA"),u));r.TinhTrang[0].MaDMTT=m.Mau.MaDMTT;r.TinhTrang[0].MucDo="fixture MỨC ĐỘ";var s=b.LuuKham(r,u);
            Need(b.GoiYKeHoach(f.Id("LSA"),u).Any(g=>g.Mau.MaMauKH==m.Mau.MaMauKH),"SQL không nhận mức độ hợp lệ");
            Need(b.ApDungMau(f.Id("LSA"),s.TinhTrang[0].MaTTB,m.Mau.MaMauKH,u).CacBuoc.Count==1,"Gợi ý được nhưng không áp được");
        });
        yield return("Bản sao giữ nội dung khi mẫu nguồn ngừng áp dụng và có phiên bản mới",f=>
        {
            var u=f.User("A");var b=new BUS_KHAMDIEUTRI(f.Transaction);var m=Template(f,b,u);var r=Request(b.LayBanNhap(f.Id("LSA"),u));r.TinhTrang[0].MaDMTT=m.Mau.MaDMTT;var s=b.LuuKham(r,u);var p=b.ApDungMau(f.Id("LSA"),s.TinhTrang[0].MaTTB,m.Mau.MaMauKH,u);
            b.NgungApDungMau(m.Mau.MaMauKH,m.DauVanTay,u);
            var next=b.LuuMauDieuTri(new(){MaDMTT=m.Mau.MaDMTT,TenMau=m.Mau.TenMau,PhienBan=2,MucDo=m.Mau.MucDo,CacBuoc=[new(){MaDV=m.CacBuoc[0].MaDV,NoiDungBuoc="FIXTURE phiên bản mới",SoLuongDuKien=3}]},u);b.DuyetMauDieuTri(next.Mau.MaMauKH,next.DauVanTay,u);
            var old=b.LayKeHoach(p.KeHoach.MaKHDT,u);Need(old.CacBuoc[0].NoiDungBuoc==m.CacBuoc[0].NoiDung && old.KeHoach.PhienBanMau==1,"Mẫu mới đổi bản đã sao chép");
            Need(b.ApDungMau(f.Id("LSA"),s.TinhTrang[0].MaTTB,m.Mau.MaMauKH,u).KeHoach.MaKHDT==p.KeHoach.MaKHDT,"Retry mẫu đã ngừng không giữ bản cũ");
        });
        yield return("Nhiều tình trạng không tự áp tất cả mẫu hoặc sinh dịch vụ",f=>
        {
            var u=f.User("A");var b=new BUS_KHAMDIEUTRI(f.Transaction);var m=Template(f,b,u);var r=Request(b.LayBanNhap(f.Id("LSA"),u));var a=r.TinhTrang[0];a.MaDMTT=m.Mau.MaDMTT;b.LuuKham(r with {TinhTrang=[a,new(){MaDMTT=m.Mau.MaDMTT,MucDo=a.MucDo,LoaiTinhTrang="FIXTURE",MoTaTinhTrang="FIXTURE khác"}]},u);
            Need(b.GoiYKeHoach(f.Id("LSA"),u).Count==2 && f.Scalar<int>("SELECT COUNT(*) FROM dbo.KE_HOACH_DIEU_TRI WHERE MaLS=@ls",new{ls=f.Id("LSA")})==0,"Tự gộp hoặc áp kế hoạch");
        });
        yield return("Bắt đầu phiếu hai lần chỉ có một MaLS và chuyển Đang khám",f=>
        {
            var u=f.User("A");var b=new BUS_KHAMDIEUTRI(f.Transaction);var id=f.Id("PHNEW");
            f.Execute("INSERT dbo.PHIEU_KHAM_BENH(MaPhieu,MaHS,MaPhong,MaBS,MaNVTiepNhan,SoThuTu,ThoiGianTiepNhan,LyDoKham,TrangThai) SELECT @id,MaHS,MaPhong,MaBS,MaNVTiepNhan,5,SYSDATETIME(),N'FIXTURE',N'Chờ khám' FROM dbo.PHIEU_KHAM_BENH WHERE MaPhieu=@source",new{id,source=f.Id("PHA")});
            var a=b.BatDauKham(id,u);var c=b.BatDauKham(id,u);
            Need(a==c && f.Scalar<int>("SELECT COUNT(*) FROM dbo.LICH_SU_KHAM_BENH WHERE MaPhieu=@id",new{id})==1,"Trùng MaLS");
            Need(f.Scalar<string>("SELECT TrangThai FROM dbo.PHIEU_KHAM_BENH WHERE MaPhieu=@id",new{id})=="Đang khám","Chưa đổi phiếu");
        });
        yield return("Bác sĩ khác biết MaPhieu vẫn không bắt đầu khám",f=>Denied(()=>new BUS_KHAMDIEUTRI(f.Transaction).BatDauKham(f.Id("PHA"),f.User("B"))));
        yield return("Lưu nháp khám và nhiều tình trạng không chuyển chờ thanh toán",f=>
        {
            var u=f.User("A");var b=new BUS_KHAMDIEUTRI(f.Transaction);var s=b.LayBanNhap(f.Id("LSA"),u);var saved=b.LuuKham(Request(s),u);
            Need(saved.LanKham.TrangThai=="Nháp" && saved.TinhTrang.Count==1 && saved.LanKham.ChanDoan=="FIXTURE chẩn đoán","Lưu thiếu nội dung");
            Need(f.Scalar<string>("SELECT TrangThai FROM dbo.PHIEU_KHAM_BENH WHERE MaPhieu=@id",new{id=f.Id("PHA")})!="Chờ thanh toán","Tự chuyển chờ thu");
        });
        yield return("Hai cửa sổ lưu khám cùng snapshot báo xung đột",f=>
        {var u=f.User("A");var b=new BUS_KHAMDIEUTRI(f.Transaction);var r=Request(b.LayBanNhap(f.Id("LSA"),u));b.LuuKham(r,u);Conflict(()=>b.LuuKham(r with {ChanDoan="FIXTURE cửa sổ cũ"},u));});
        yield return("API lưu khám từ chối bác sĩ khác và Admin giả quyền",f=>
        {var b=new BUS_KHAMDIEUTRI(f.Transaction);var a=f.User("A");var r=Request(b.LayBanNhap(f.Id("LSA"),a));Denied(()=>b.LuuKham(r,f.User("B")));Denied(()=>b.LuuKham(r,f.User("X") with {Quyen=(QuyenLamViec)int.MaxValue}));});
        yield return("Mẫu nháp không gợi ý; mẫu khớp mức độ đứng trước tổng quát",f=>
        {
            var u=f.User("A");var b=new BUS_KHAMDIEUTRI(f.Transaction);var draft=Template(f,b,u,false);var exact=Template(f,b,u,true,draft.Mau.MaDMTT);var general=Template(f,b,u,true,draft.Mau.MaDMTT,null);
            var r=Request(b.LayBanNhap(f.Id("LSA"),u));r.TinhTrang[0].MaDMTT=draft.Mau.MaDMTT;b.LuuKham(r,u);
            var rows=b.GoiYKeHoach(f.Id("LSA"),u);Need(rows.Count==2 && rows[0].Mau.MaMauKH==exact.Mau.MaMauKH && rows[1].Mau.MaMauKH==general.Mau.MaMauKH,"Gợi ý sai duyệt/mức độ");
        });
        yield return("Áp mẫu cùng nguồn hai lần không nhân kế hoạch hoặc dịch vụ thu phí",f=>
        {
            var u=f.User("A");var b=new BUS_KHAMDIEUTRI(f.Transaction);var m=Template(f,b,u);var r=Request(b.LayBanNhap(f.Id("LSA"),u));r.TinhTrang[0].MaDMTT=m.Mau.MaDMTT;var s=b.LuuKham(r,u);
            var a=b.ApDungMau(f.Id("LSA"),s.TinhTrang[0].MaTTB,m.Mau.MaMauKH,u);var c=b.ApDungMau(f.Id("LSA"),s.TinhTrang[0].MaTTB,m.Mau.MaMauKH,u);
            Need(a.KeHoach.MaKHDT==c.KeHoach.MaKHDT && a.CacBuoc.Count==1 && a.KeHoach.PhienBanMau==1 && a.CacBuoc[0].NoiDungBuoc==m.CacBuoc[0].NoiDung,"Mất snapshot/nhân bản");
            b.XacNhanKeHoach(a.KeHoach.MaKHDT,a.DauVanTay,u);Need(f.Scalar<int>("SELECT COUNT(*) FROM dbo.DICH_VU_THUC_HIEN WHERE MaLS=@ls",new{ls=f.Id("LSA")})==0,"Duyệt kế hoạch sinh dịch vụ tính phí");
        });
        yield return("Không có mẫu vẫn lưu và duyệt kế hoạch thủ công",f=>
        {
            var u=f.User("A");var b=new BUS_KHAMDIEUTRI(f.Transaction);var p=b.LuuKeHoach(new(){MaLS=f.Id("LSA"),CacBuoc=[new(){MaDV=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU ORDER BY MaDV"),NoiDungBuoc="FIXTURE thủ công",DonGiaDuKien=10000,SoLuongDuKien=1.125m}]},u);
            Need(p.KeHoach.MaMauKH is null && p.KeHoach.TongTienDuKien==11250,"Kế hoạch thủ công/giá derived sai");
            var signed=b.XacNhanKeHoach(p.KeHoach.MaKHDT,p.DauVanTay,u);Need(signed.KeHoach.TrangThai=="Đã xác nhận","Không duyệt được");
        });
        yield return("Admin soạn nháp nhưng không ký mẫu thay bác sĩ",f=>
        {var x=f.User("X");var b=new BUS_KHAMDIEUTRI(f.Transaction);var m=Template(f,b,x,false);Denied(()=>b.DuyetMauDieuTri(m.Mau.MaMauKH,m.DauVanTay,x));});
        yield return("Duyệt mẫu ghi bác sĩ phiên và nhật ký, không tin chữ ký client",f=>
        {var u=f.User("A");var b=new BUS_KHAMDIEUTRI(f.Transaction);var m=Template(f,b,u);Need(m.Mau.MaBSDuyet==u.MaNV && m.Mau.NgayDuyet is not null,"Sai người duyệt");Need(f.Scalar<int>("SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC WHERE MaTK=@MaTK AND MaDoiTuong=@id",new{u.MaTK,id=m.Mau.MaMauKH})>=2,"Thiếu audit");});
        yield return("Hoàn tất khám thiếu nội dung chuyên môn bị chặn",f=>
        {var u=f.User("A");var b=new BUS_KHAMDIEUTRI(f.Transaction);var s=b.LayBanNhap(f.Id("LSA"),u);Invalid(()=>b.HoanTatKham(f.Id("LSA"),s.DauVanTay,u),"TinhTrang");});
        yield return("Đơn nháp chưa xử lý chặn hoàn tất chuyên môn",f=>
        {var u=f.User("A");var b=new BUS_KHAMDIEUTRI(f.Transaction);var s=b.LuuKham(Request(b.LayBanNhap(f.Id("LSA"),u)),u);Invalid(()=>b.HoanTatKham(f.Id("LSA"),s.DauVanTay,u),"DonThuoc");});
        yield return("Không kê thuốc có lý do cho hoàn tất, rồi khóa nội dung/tình trạng",f=>
        {
            f.Execute("DELETE dbo.CHI_TIET_DON_THUOC WHERE MaDT=@id; DELETE dbo.DON_THUOC WHERE MaDT=@id",new{id=f.Id("DTA")});
            var u=f.User("A");var b=new BUS_KHAMDIEUTRI(f.Transaction);var r=Request(b.LayBanNhap(f.Id("LSA"),u)) with {KhongKeThuoc=true,LyDoKhongKeThuoc="FIXTURE không chỉ định"};var s=b.LuuKham(r,u);var final=b.HoanTatKham(f.Id("LSA"),s.DauVanTay,u);
            Need(final.LanKham.TrangThai=="Đã xác nhận" && !final.DuocSua && f.Scalar<string>("SELECT TrangThai FROM dbo.PHIEU_KHAM_BENH WHERE MaPhieu=@id",new{id=f.Id("PHA")})=="Chờ thanh toán","Chưa đóng chuyên môn");
            Denied(()=>b.LuuKham(r with {DauVanTay=final.DauVanTay,ChanDoan="Ghi đè"},u));
        });
    }
}
