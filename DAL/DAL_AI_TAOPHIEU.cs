using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using ET;

namespace DAL;

// Tất cả câu SQL có tham số. Không ghi dữ liệu bệnh án từ nội dung AI mà chưa được duyệt.
public class DAL_AI_TAOPHIEU : dbconnect
{
    public List<ET_AI_DANHMUC> layPhong() => ThucHien((db, tx) => db.Query<ET_AI_DANHMUC>("""
        SELECT MaPhong AS Ma,TenPhong AS Ten
        FROM dbo.PHONG WHERE TrangThai=N'Đang hoạt động'
        ORDER BY TenPhong
        """, transaction: tx).ToList());

    public List<ET_AI_DANHMUC> layBacSi(string maPhong) => ThucHien((db, tx) => db.Query<ET_AI_DANHMUC>("""
        SELECT DISTINCT nv.MaNV AS Ma,nv.HoTen AS Ten
        FROM dbo.NHAN_VIEN nv
        JOIN dbo.CHUC_VU cv ON cv.MaChucVu=nv.MaChucVu
        JOIN dbo.LICH_LAM_VIEC llv ON llv.MaNV=nv.MaNV
        WHERE llv.MaPhong=@MaPhong AND llv.NgayLam=CONVERT(date,SYSDATETIME())
          AND llv.TrangThai=N'Đã xác nhận' AND nv.TrangThai=N'Đang làm việc'
          AND cv.TenChucVu LIKE N'%Bác sĩ%'
        ORDER BY nv.HoTen
        """, new { MaPhong = maPhong }, tx).ToList());

    public List<ET_AI_DANHMUC> layDichVu(string maPhong, string? maBS) => ThucHien((db, tx) => db.Query<ET_AI_DANHMUC>("""
        SELECT dv.MaDV AS Ma,dv.TenDV AS Ten
        FROM dbo.DICH_VU dv
        JOIN dbo.CHUYEN_KHOA ck ON ck.MaCK=dv.MaCK
        JOIN dbo.PHONG ph ON ph.MaKhoa=ck.MaKhoa
        WHERE ph.MaPhong=@MaPhong AND dv.TrangThai=N'Đang hoạt động'
          AND dv.NgayApDung<=CONVERT(date,SYSDATETIME())
          AND (@MaBS IS NULL OR EXISTS(SELECT 1 FROM dbo.NHAN_VIEN nv
               WHERE nv.MaNV=@MaBS AND nv.MaCK=dv.MaCK))
        ORDER BY dv.TenDV
        """, new { MaPhong = maPhong, MaBS = maBS }, tx).ToList());

    public List<ET_AI_BENHNHANTIM> timBenhNhan(string tim) => ThucHien((db, tx) => db.Query<ET_AI_BENHNHANTIM>("""
        SELECT TOP (25) MaBN,HoTenBN,NgaySinh,SDT
        FROM dbo.BENH_NHAN
        WHERE HoTenBN LIKE N'%' + @Tim + N'%' OR SDT LIKE N'%' + @Tim + N'%'
              OR MaBN=@Tim
        ORDER BY HoTenBN, MaBN
        """, new { Tim = tim }, tx).ToList());

    // Tự đối chiếu: không dùng câu trả lời "đã từng khám" làm điều kiện tạo hồ sơ.
    // Có dữ liệu nghi trùng thì không tự chọn nhầm bệnh nhân.
    public List<ET_AI_BENHNHANTIM> doiChieuBenhNhan(string hoTen, DateTime ngaySinh, string sdt)
        => ThucHien((db, tx) => db.Query<ET_AI_BENHNHANTIM>("""
            DECLARE @SDTChuan nvarchar(50) = REPLACE(REPLACE(REPLACE(REPLACE(
                ISNULL(@SDT,N''),N' ',N''),N'-',N''),N'.',N''),N'(',N'');
            SET @SDTChuan=REPLACE(@SDTChuan,N')',N'');
            SELECT TOP (30) b.MaBN,b.HoTenBN,b.NgaySinh,b.SDT,
                   CASE WHEN LTRIM(RTRIM(b.HoTenBN))=@HoTen AND b.NgaySinh=@NgaySinh
                         AND REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(b.SDT,N''),
                             N' ',N''),N'-',N''),N'.',N''),N'(',N''),N')',N'')=@SDTChuan
                        THEN 2 ELSE 1 END AS MucDoKhop
            FROM dbo.BENH_NHAN AS b
            WHERE (LTRIM(RTRIM(b.HoTenBN))=@HoTen AND b.NgaySinh=@NgaySinh)
               OR (@SDTChuan<>N'' AND REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(ISNULL(b.SDT,N''),
                   N' ',N''),N'-',N''),N'.',N''),N'(',N''),N')',N'')=@SDTChuan)
            ORDER BY MucDoKhop DESC,b.MaBN;
            """,new { HoTen=hoTen.Trim(), NgaySinh=ngaySinh.Date, SDT=sdt.Trim() },tx).ToList());

    public ET_AI_KETQUATAO taoPhieu(ET_AI_TAOPHIEU m)
    {
        // Procedure tự quản lý transaction (không lồng transaction của ThucHien).
        // Duyệt lại cùng mã AI sẽ trả cùng MaPhieu thay vì tạo thêm lượt khám.
        using var db = MoKetNoi();
        var args = new DynamicParameters();
        args.Add("MaPhien", m.MaPhien);
        args.Add("MaNV", m.MaNV);
        args.Add("MaBNHienCo", m.MaBNHienCo);
        args.Add("HoTen", m.HoTen.Trim());
        args.Add("NgaySinh", m.NgaySinh.Date, DbType.Date);
        args.Add("SDT", m.SDT.Trim());
        args.Add("LyDoKham", m.LyDoKham.Trim());
        args.Add("LoaiKham", m.LoaiKham);
        args.Add("MaPhong", m.MaPhong);
        args.Add("MaBS", m.MaBS);
        args.Add("MaDV", m.MaDV);
        args.Add("MaBNOut", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
        args.Add("MaHSOut", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
        args.Add("MaPhieuOut", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
        args.Add("SoThuTuOut", dbType: DbType.Int32, direction: ParameterDirection.Output);
        db.Execute("dbo.AI_TaoPhieuTuBanKhai",args,
            commandType: CommandType.StoredProcedure, commandTimeout: 30);
        return new ET_AI_KETQUATAO
        {
            MaBN = args.Get<string>("MaBNOut") ?? "",
            MaHS = args.Get<string>("MaHSOut") ?? "",
            MaPhieu = args.Get<string>("MaPhieuOut") ?? "",
            SoThuTu = args.Get<int>("SoThuTuOut")
        };
    }
}
