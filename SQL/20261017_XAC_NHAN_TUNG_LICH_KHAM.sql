-- Chạy một lần trên database QLBenhVienRangHamMat hiện có.
-- Lưu xác nhận TỪNG lịch khám trước khi duyệt đơn xin nghỉ.
IF OBJECT_ID(N'dbo.XAC_NHAN_DOI_LICH_KHAM',N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[XAC_NHAN_DOI_LICH_KHAM] (
        [MaDonNghi] NVARCHAR(50) NOT NULL,
        [MaLich] NVARCHAR(50) NOT NULL,
        [MaBSCu] NVARCHAR(50) NOT NULL,
        [MaPhongCu] NVARCHAR(50) NOT NULL,
        [MaBSMoi] NVARCHAR(50) NOT NULL,
        [MaPhongMoi] NVARCHAR(50) NOT NULL,
        [MaLLVThayThe] NVARCHAR(50) NOT NULL,
        [MaCa] NVARCHAR(50) NOT NULL,
        [NgayGioKham] DATETIME2(0) NOT NULL,
        [ThoiLuongDuKien] INT NOT NULL,
        [MaNVXacNhan] NVARCHAR(50) NOT NULL,
        [NgayXacNhan] DATETIME2(0) NOT NULL,
        [TrangThai] NVARCHAR(30) NOT NULL,
        CONSTRAINT [PK_XAC_NHAN_DOI_LICH_KHAM] PRIMARY KEY ([MaDonNghi],[MaLich]),
        CONSTRAINT [FK_XNDLK_Don] FOREIGN KEY ([MaDonNghi]) REFERENCES dbo.DON_XIN_NGHI([MaDonNghi]),
        CONSTRAINT [FK_XNDLK_NhanVien] FOREIGN KEY ([MaNVXacNhan]) REFERENCES dbo.NHAN_VIEN([MaNV]),
        CONSTRAINT [CK_XNDLK_TrangThai] CHECK ([TrangThai] IN (N'Đã xác nhận',N'Đã hủy'))
    );
END;
GO
