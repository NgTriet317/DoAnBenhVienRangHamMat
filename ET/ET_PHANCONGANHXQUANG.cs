using Dapper.Contrib.Extensions;
namespace ET;

[Table("PHAN_CONG_ANH_XQUANG")]
public sealed class ET_PHANCONGANHXQUANG
{
    [ExplicitKey] public string MaPC {get;set;}="";
    public string MaDVThucHien {get;set;}="";
    public string MaNVKyThuat {get;set;}="";
    public string MaNVPhanCong {get;set;}="";
    public DateTime NgayPhanCong {get;set;}
    public string TrangThai {get;set;}="Đang áp dụng";
    public DateTime? NgayHuy {get;set;}
}
