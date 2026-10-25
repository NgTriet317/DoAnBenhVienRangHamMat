using BUS;
using DAL;
using Dapper;
using ET;
using Microsoft.Data.SqlClient;
using static RangHamMat.ClinicalChecks.CauHinhChecks;

namespace RangHamMat.ClinicalChecks;

internal static class DuLieuMoiChecks
{
    internal static IEnumerable<(string,Action<Fixture>)> Cases()
    {
        yield return ("Năm bảng cấu hình roundtrip đúng kiểu/nullable qua BUS",f =>
        {
            var user=f.User("X");
            var condition=new ET_DMTINHTRANGBENH { TenTinhTrang="FIXTURE không dùng thật",TrangThai="Đang áp dụng" };
            new BUS_DMTINHTRANGBENH(f.Transaction).LuuNhap(condition,user);
            var template=new ET_MAUKEHOACHDIEUTRI { MaDMTT=condition.MaDMTT,TenMau="FIXTURE không dùng thật",PhienBan=1,TrangThai="Nháp" };
            new BUS_MAUKEHOACHDIEUTRI(f.Transaction).LuuNhap(template,user);
            var step=new ET_CHITIETMAUKEHOACH { MaMauKH=template.MaMauKH,MaDV=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU ORDER BY MaDV"),ThuTu=1,LanThu=1,SoLuongDuKien=1.125m,NoiDung="FIXTURE không dùng thật" };
            new BUS_CHITIETMAUKEHOACH(f.Transaction).LuuNhap(step,user);
            Need(new BUS_CHITIETMAUKEHOACH(f.Transaction).LayDanhSach(user).Single(v=>v.MaCTMau==step.MaCTMau).SoLuongDuKien==1.125m,"Lượng mất precision");
            var group=new ET_NHOMTHUOCKEDON { TenNhom="FIXTURE không dùng thật",TrangThai="Đang áp dụng" };
            new BUS_NHOMTHUOCKEDON(f.Transaction).LuuNhap(group,user);
            var config=new ET_CAUHINHTHUOCKEDON { MaThuoc=f.Scalar<string>("SELECT TOP(1) MaThuoc FROM dbo.THUOC ORDER BY MaThuoc"),MaNhomThuoc=group.MaNhomThuoc,PhienBan=1,TrangThai="Nháp" };
            new BUS_CAUHINHTHUOCKEDON(f.Transaction).LuuNhap(config,user);
            var saved=new BUS_CAUHINHTHUOCKEDON(f.Transaction).LayDanhSach(user).Single(v=>v.MaThuoc==config.MaThuoc);
            Need(saved.MaNhomThuoc==group.MaNhomThuoc && saved.NgayDuyet is null && saved.HuongDanGoiY is null,"Nullable/config roundtrip sai");
            Need(f.Scalar<int>("SELECT COUNT(*) FROM dbo.NHAT_KY_THAO_TAC WHERE MaTK=@MaTK AND HanhDong=N'Cập nhật cấu hình hỗ trợ'",new{user.MaTK})==5,"Thiếu audit một trong năm bảng");
        });
        yield return ("Cột tình trạng/kế hoạch/chi tiết lưu qua DAL và giữ tiền derived",f =>
        {
            var user=f.User("A");
            var cat=new ET_DMTINHTRANGBENH { TenTinhTrang="FIXTURE",TrangThai="Đang áp dụng" };
            new BUS_DMTINHTRANGBENH(f.Transaction).LuuNhap(cat,user);
            var condition=new ET_TINHTRANGBENH { MaLS=f.Id("LSA"),LoaiTinhTrang="FIXTURE",MoTaTinhTrang="FIXTURE",MaDMTT=cat.MaDMTT,ViTriRang="FIXTURE" };
            var dalCondition=new DAL_TINHTRANGBENH(f.Transaction); dalCondition.themTinhTrangBenh(condition);
            var savedCondition=dalCondition.layTinhTrangBenhTheoMa(condition.MaTTB)!;
            Need(savedCondition.MaDMTT==cat.MaDMTT && savedCondition.ViTriRang=="FIXTURE","Mất cột tình trạng");
            var plan=new ET_KEHOACHDIEUTRI { MaLS=f.Id("LSA"),MaBS=user.MaNV,NgayLap=f.Scalar<DateTime>("SELECT SYSDATETIME()"),TrangThai="Nháp",MaTTBNguon=condition.MaTTB };
            var dalPlan=new DAL_KEHOACHDIEUTRI(f.Transaction); dalPlan.themKeHoachDieuTri(plan);
            var step=new ET_CHITIETKEHOACH { MaKHDT=plan.MaKHDT,MaDV=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU ORDER BY MaDV"),LanThu=1,SoLuongDuKien=1.125m,DonGiaDuKien=12000m,TrangThai="Dự kiến",NoiDungBuoc="FIXTURE" };
            var dalStep=new DAL_CHITIETKEHOACH(f.Transaction); dalStep.themChiTietKeHoach(step);
            Need(dalPlan.layKeHoachDieuTriTheoMa(plan.MaKHDT)!.MaTTBNguon==condition.MaTTB,"Mất nguồn tình trạng");
            var savedStep=dalStep.layChiTietKeHoachTheoMa(step.MaCTKH)!;
            Need(savedStep.NoiDungBuoc=="FIXTURE" && savedStep.ThanhTienDuKien==13500m,"Mất nội dung/tiền derived");
        });
        yield return ("Snapshot đơn thuốc lấy đủ cột mới và giới hạn hướng dẫn theo quyền",f =>
        {
            var user=f.User("A");
            var dal=new DAL_CHITIETDONTHUOC(f.Transaction);
            var item=dal.layChiTietDonThuocTheoMa(f.Id("CTA"))!;
            item.DuongDung="FIXTURE đường dùng"; item.TenThuocChot="FIXTURE tên";item.HamLuongChot="FIXTURE hàm lượng";item.DonViTinhChot="FIXTURE đơn vị";item.PhienBanHuongDan=3;
            dal.suaChiTietDonThuoc(item);
            var saved=dal.layChiTietDonThuocTheoMa(item.MaCTDT)!;
            Need(saved.DuongDung==item.DuongDung && saved.PhienBanHuongDan==3,"DAL mất cột mới");
            var snapshot=new DAL_KHAMDIEUTRI(f.Transaction).LayDuLieuHoSo(user);
            var line=snapshot.CacBang["CHI_TIET_DON_THUOC"].Cast<ET_CHITIETDONTHUOC>().Single(v=>v.MaCTDT==item.MaCTDT);
            Need(line.DuongDung==item.DuongDung && line.TenThuocChot==item.TenThuocChot && line.HamLuongChot==item.HamLuongChot && line.DonViTinhChot==item.DonViTinhChot && line.PhienBanHuongDan==3,"Snapshot mất dữ liệu mới");
            f.Execute("UPDATE dbo.DON_THUOC SET TrangThai=N'Đã xác nhận' WHERE MaDT=@dt; UPDATE dbo.LICH_SU_KHAM_BENH SET TrangThai=N'Đã xác nhận',NgayXacNhan=SYSDATETIME() WHERE MaLS=@ls",new{dt=f.Id("DTA"),ls=f.Id("LSA")});
            var cashier=f.User("C");
            line=new DAL_KHAMDIEUTRI(f.Transaction).LayDuLieuHoSo(cashier).CacBang["CHI_TIET_DON_THUOC"].Cast<ET_CHITIETDONTHUOC>().Single(v=>v.MaCTDT==item.MaCTDT);
            Need(line.TenThuocChot==item.TenThuocChot && line.DuongDung is null && line.PhienBanHuongDan is null,"Thu ngân lộ hướng dẫn hoặc mất tên chốt");
        });
        yield return ("Snapshot lần khám lấy cột không kê thuốc trong đúng phạm vi",f =>
        {
            var user=f.User("A");
            var dal=new DAL_LICHSUKHAMBENH(f.Transaction);var visit=dal.layLichSuKhamBenhTheoMa(f.Id("LSA"))!;
            visit.LyDoKhongKeThuoc="FIXTURE lý do"; dal.suaLichSuKhamBenh(visit);
            Need(dal.layLichSuKhamBenhTheoMa(visit.MaLS)!.LyDoKhongKeThuoc==visit.LyDoKhongKeThuoc,"DAL mất lý do");
            var row=new DAL_KHAMDIEUTRI(f.Transaction).LayDuLieuHoSo(user).CacBang["LICH_SU_KHAM_BENH"].Cast<ET_LICHSUKHAMBENH>().Single(v=>v.MaLS==visit.MaLS);
            Need(row.LyDoKhongKeThuoc==visit.LyDoKhongKeThuoc,"Snapshot mất lý do");
        });
        yield return ("SQL giữ nội dung lần khám đã ký",f=>SqlError(f,"UPDATE dbo.LICH_SU_KHAM_BENH SET GhiChu=N'FIXTURE sửa trái phép' WHERE MaLS=@id",52110,new{id=f.Id("LSH")}));
        yield return ("SQL giữ snapshot dòng đơn thuốc đã ký",f=>SqlError(f,"UPDATE dbo.CHI_TIET_DON_THUOC SET TenThuocChot=N'FIXTURE sửa trái phép' WHERE MaCTDT=@id",51012,new{id=f.Id("CTH")}));
        yield return ("SQL chặn tình trạng bổ sung vào lần khám đã ký",f=>SqlError(f,"INSERT dbo.TINH_TRANG_BENH(MaTTB,MaLS,LoaiTinhTrang,MoTaTinhTrang,MucDo) VALUES(@id,@ls,N'FIXTURE',N'FIXTURE',N'FIXTURE')",52111,new{id=f.Id("TT"),ls=f.Id("LSH")}));
        yield return ("SQL không kê thuốc cần lý do",f=>SqlError(f,"UPDATE dbo.LICH_SU_KHAM_BENH SET KhongKeThuoc=1 WHERE MaLS=@id",547,new{id=f.Id("LSA")},"CK_CW_LS_NoRx"));
        yield return ("SQL không kê thuốc không đồng thời có đơn nháp",f=>SqlError(f,"UPDATE dbo.LICH_SU_KHAM_BENH SET KhongKeThuoc=1,LyDoKhongKeThuoc=N'FIXTURE' WHERE MaLS=@id",52110,new{id=f.Id("LSA")}));
        yield return ("SQL giữ FK danh mục tình trạng",f=>SqlError(f,"INSERT dbo.TINH_TRANG_BENH(MaTTB,MaLS,LoaiTinhTrang,MoTaTinhTrang,MucDo,MaDMTT) VALUES(@id,@ls,N'FIXTURE',N'FIXTURE',N'FIXTURE',N'CW_NOT_EXISTS')",547,new{id=f.Id("TT"),ls=f.Id("LSA")},"FK_CW_TTB_DMTT"));
        yield return ("SQL khóa yêu cầu v2 phải là UUID chuẩn và hash đủ 64 hex",f=>SqlError(f,"INSERT dbo.HOA_DON(MaHD,MaLS,MaNV,NgayLap,TongTien,GiamGia,TrangThai,PhienBanQuyTrinh,MaYeuCau,DauVanTayYeuCau) VALUES(@id,@ls,@nv,SYSDATETIME(),0,0,N'Nháp',2,N'bad-uuid',REPLICATE(N'A',64))",547,new{id=f.Id("HD"),ls=f.Id("LSH"),nv=f.Id("C")},"CK_CW_HD_Request"));
        yield return ("SQL cấu hình yêu cầu actor audit",f=>SqlError(f,"INSERT dbo.DM_TINH_TRANG_BENH(MaDMTT,TenTinhTrang,TrangThai) VALUES(@id,N'FIXTURE',N'Đang áp dụng')",52120,new{id=f.Id("CAT")}));
        yield return ("Verify phát hiện trigger mới bị vô hiệu hóa",f=>
        {
            f.Execute("DISABLE TRIGGER dbo.TR_CW_LS_BaoVe ON dbo.LICH_SU_KHAM_BENH");
            SqlError(f,File.ReadAllText("Database/Migrations/20261024_03_Verify_ClinicalWorkflow.sql"),52102);
        });
        yield return ("Kế hoạch đã duyệt không trở về nháp tại SQL",f=>
        {
            var id=f.Scalar<string>("SELECT TOP(1) MaKHDT FROM dbo.KE_HOACH_DIEU_TRI WHERE TrangThai<>N'Nháp' ORDER BY MaKHDT");
            SqlError(f,"UPDATE dbo.KE_HOACH_DIEU_TRI SET TrangThai=N'Nháp' WHERE MaKHDT=@id",52113,new{id});
        });
        yield return ("Bước đã thực hiện không chuyển sang kế hoạch của bệnh nhân khác",f=>
        {
            var user=f.User("B");
            var target=new ET_KEHOACHDIEUTRI { MaLS=f.Id("LSB"),MaBS=user.MaNV,NgayLap=f.Scalar<DateTime>("SELECT SYSDATETIME()"),TrangThai="Nháp" };
            new DAL_KEHOACHDIEUTRI(f.Transaction).themKeHoachDieuTri(target);
            var id=f.Scalar<string>("SELECT TOP(1) s.MaCTKH FROM dbo.DICH_VU_THUC_HIEN s JOIN dbo.CHI_TIET_KE_HOACH c ON c.MaCTKH=s.MaCTKH JOIN dbo.KE_HOACH_DIEU_TRI k ON k.MaKHDT=c.MaKHDT WHERE s.TrangThai=N'Đã xác nhận' AND k.TrangThai<>N'Nháp' ORDER BY s.MaCTKH");
            Need(!string.IsNullOrEmpty(id),"Fixture cần nguồn thực hiện baseline");
            SqlError(f,"UPDATE dbo.CHI_TIET_KE_HOACH SET MaKHDT=@target WHERE MaCTKH=@id",52114,new{id,target=target.MaKHDT});
        });
        yield return ("Kế hoạch có sẵn không gắn mẫu chưa duyệt",f=>
        {
            var (plan,template)=TemplateFixture(f,false);
            SqlError(f,"UPDATE dbo.KE_HOACH_DIEU_TRI SET MaMauKH=@template,PhienBanMau=1 WHERE MaKHDT=@id",52113,new{id=plan.MaKHDT,template=template.MaMauKH});
        });
        yield return ("Kế hoạch có mẫu bắt buộc phiên bản khác null",f=>
        {
            var (plan,template)=TemplateFixture(f,true);
            SqlError(f,"UPDATE dbo.KE_HOACH_DIEU_TRI SET MaMauKH=@template,PhienBanMau=NULL WHERE MaKHDT=@id",547,new{id=plan.MaKHDT,template=template.MaMauKH},"CK_CW_KH_Source");
        });
        yield return ("Mẫu ngừng áp dụng không chặn cập nhật tiến độ kế hoạch đã sao chép",f=>
        {
            var (plan,template)=TemplateFixture(f,true);
            f.Execute("UPDATE dbo.KE_HOACH_DIEU_TRI SET MaMauKH=@template,PhienBanMau=1 WHERE MaKHDT=@id; UPDATE dbo.MAU_KE_HOACH_DIEU_TRI SET TrangThai=N'Ngừng áp dụng' WHERE MaMauKH=@template; UPDATE dbo.KE_HOACH_DIEU_TRI SET TrangThai=N'Đã duyệt' WHERE MaKHDT=@id",new{id=plan.MaKHDT,template=template.MaMauKH});
            Need(f.Scalar<string>("SELECT TrangThai FROM dbo.KE_HOACH_DIEU_TRI WHERE MaKHDT=@id",new{id=plan.MaKHDT})=="Đã duyệt","Không cập nhật được tiến độ");
        });
        yield return ("Bàn giao rỗng đọc qua quyền đơn, không cho đọc mã người khác",f=>
        {
            var user=f.User("A"); var service=new BUS_BANGIAOTHUOC(f.Transaction);
            Need(service.LayTheoDon(f.Id("DTA"),user) is null,"Có bàn giao giả");
            try {service.LayTheoDon(f.Id("DTB"),user);}catch(TuChoiQuyenNghiepVuException){return;}
            throw new Exception("Đọc bàn giao ngoài phạm vi");
        });
    }

    private static (ET_KEHOACHDIEUTRI,ET_MAUKEHOACHDIEUTRI) TemplateFixture(Fixture f,bool approved)
    {
        var user=f.User("A");
        var category=new ET_DMTINHTRANGBENH { TenTinhTrang="FIXTURE không dùng thật",TrangThai="Đang áp dụng" };
        new BUS_DMTINHTRANGBENH(f.Transaction).LuuNhap(category,user);
        var template=new ET_MAUKEHOACHDIEUTRI { MaDMTT=category.MaDMTT,TenMau="FIXTURE không dùng thật",PhienBan=1,TrangThai="Nháp" };
        new BUS_MAUKEHOACHDIEUTRI(f.Transaction).LuuNhap(template,user);
        var condition=new ET_TINHTRANGBENH { MaLS=f.Id("LSA"),LoaiTinhTrang="FIXTURE",MoTaTinhTrang="FIXTURE",MucDo="FIXTURE",MaDMTT=category.MaDMTT };
        new DAL_TINHTRANGBENH(f.Transaction).themTinhTrangBenh(condition);
        var plan=new ET_KEHOACHDIEUTRI { MaLS=f.Id("LSA"),MaBS=user.MaNV,NgayLap=f.Scalar<DateTime>("SELECT SYSDATETIME()"),TrangThai="Nháp",MaTTBNguon=condition.MaTTB };
        new DAL_KEHOACHDIEUTRI(f.Transaction).themKeHoachDieuTri(plan);
        // Fixture giả lập thao tác duyệt chuyên môn trong SQL để kiểm tra constraint P03; không phải API phê duyệt P05.
        if(approved)
        {
            new BUS_CHITIETMAUKEHOACH(f.Transaction).LuuNhap(new ET_CHITIETMAUKEHOACH { MaMauKH=template.MaMauKH,MaDV=f.Scalar<string>("SELECT TOP(1) MaDV FROM dbo.DICH_VU ORDER BY MaDV"),ThuTu=1,LanThu=1,SoLuongDuKien=1,NoiDung="FIXTURE không dùng thật" },user);
            f.Execute("EXEC sys.sp_set_session_context @key=N'RHM.MaTK',@value=@actor; UPDATE dbo.MAU_KE_HOACH_DIEU_TRI SET TrangThai=N'Đã duyệt',MaBSDuyet=@doctor,NgayDuyet=SYSDATETIME() WHERE MaMauKH=@id",new{id=template.MaMauKH,actor=user.MaTK,doctor=user.MaNV});
        }
        return (plan,template);
    }

    private static void SqlError(Fixture f,string sql,int number,object? args=null,string? constraint=null)
    {
        try { f.Execute(sql,args); }
        catch(SqlException ex) when(ex.Errors.Cast<SqlError>().Any(e=>e.Number==number) && (constraint is null || ex.Message.Contains(constraint))) {return;}
        throw new Exception("SQL không chặn bằng lỗi "+number);
    }
}
