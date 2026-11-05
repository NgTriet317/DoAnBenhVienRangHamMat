using DAL;
using ET;

namespace BUS;

public sealed class BUS_DANHMUC
{
    private readonly DAL_DANHMUC dal=new();
    public IReadOnlyList<Dictionary<string,object?>> Lay(string bang,string search,string status,NguCanhNghiepVu actor)
        =>dal.Lay(bang,search,status,actor);
    public Dictionary<string,object?> Luu(string bang,IDictionary<string,object?> values,IDictionary<string,object?>? original,NguCanhNghiepVu actor,Guid session,bool? active=null)
        =>dal.Luu(bang,values,original,actor,session,active);
}
