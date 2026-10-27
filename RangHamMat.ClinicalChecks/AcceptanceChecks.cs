using BUS;
using Dapper;
using ET;

namespace RangHamMat.ClinicalChecks;

internal static class AcceptanceChecks
{
    internal static int Run()
    {
        using(var db=MigrationChecks.Open())
            if(!db.Database.Contains("_P12",StringComparison.Ordinal))throw new Exception("Acceptance chỉ dùng DB P12 riêng.");
        var passed=0;
        foreach(var sample in new[]{(Drugs:true,Second:false,Price:400000m),(Drugs:true,Second:true,Price:400000m),(Drugs:false,Second:false,Price:0m)})
        {
            using var p=new PaymentChecks.Prepared(sample.Drugs,sample.Second,sample.Price);
            var request=p.Request with{SoTienKhachDua=p.Fee.TongTien==0?0:500000};
            var paid=p.Pay(request);
            using var db=MigrationChecks.Open();
            string Counts()=>db.ExecuteScalar<string>("SELECT CONCAT((SELECT COUNT(*) FROM dbo.HOA_DON),':',(SELECT COUNT(*) FROM dbo.THANH_TOAN),':',(SELECT COUNT(*) FROM dbo.PHIEU_XUAT),':',(SELECT COUNT(*) FROM dbo.CHI_TIET_PHIEU_XUAT),':',(SELECT SUM(SoLuongTon) FROM dbo.VW_TON_KHO))")!;
            var financial=Counts();
            if(paid.SoTienThu!=p.Fee.TongTien||paid.CacDon.Count!=(sample.Drugs?(sample.Second?2:1):0))throw new Exception("Thu không đủ mọi nguồn.");
            var pharmacist=HandoverChecks.User(p,"P");
            foreach(var order in paid.CacDon)
            {
                var print=new BUS_DONTHUOC().LayBanInDon(order.MaDT,pharmacist);
                if(print.Nguon.Don.MaLS!=paid.MaLS||print.Nguon.MaBN!=p.Fee.MaBN)throw new Exception("In đơn khác BN/lượt.");
                var service=new BUS_BANGIAOTHUOC();
                service.XacNhanGiao(order.MaDT,pharmacist);service.XacNhanGiao(order.MaDT,pharmacist);
            }
            if(Counts()!=financial)throw new Exception("Giao/in lại thay đổi tiền hoặc xuất/tồn.");
            p.F.ActAs("C");
            var retry=p.Pay(request);var invoice=new BUS_THANHTOANTRONGOI().LayBanInHoaDon(paid.MaHD,p.Actor);
            if(retry.MaHD!=paid.MaHD||!retry.DaTonTai||invoice.HoaDon.MaHD!=paid.MaHD||Counts()!=financial)throw new Exception("Retry/in thu hoặc xuất lại.");
            if(db.ExecuteScalar<string>("SELECT TrangThai FROM dbo.PHIEU_KHAM_BENH WHERE MaPhieu=@id",new{id=p.F.Id("PHA")})!="Hoàn tất")throw new Exception("Phiếu chưa hoàn tất sau giao hết.");
            if(p.Fee.TongTien==0&&db.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.THANH_TOAN WHERE MaHD=@MaHD",paid)!=0)throw new Exception("Tạo Thu0.");
            Console.WriteLine($"PASS: P12 liên tục ký khám/đơn →thu →giao →in/retry; thuốc={sample.Drugs},bổ sung={sample.Second},phải thu={paid.SoTienThu}; đúng một bộ chứng từ.");passed++;
        }
        Console.WriteLine($"AcceptanceChecks: {passed} PASS, 0 FAIL; commit chỉ trên DB P12 riêng.");return 0;
    }
}
