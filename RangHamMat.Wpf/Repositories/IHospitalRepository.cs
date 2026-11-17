using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RangHamMat.Models;

namespace RangHamMat.Repositories;

public interface IHospitalRepository
{
    Task<IReadOnlyList<DepartmentRoom>> GetRoomsAsync(DepartmentGroup group, CancellationToken ct);
    Task<string> LookupAsync(HospitalQueryType queryType, CancellationToken ct);
    // Phải lưu phiếu và tăng STT trong cùng transaction ở triển khai SQL thật.
    Task<GuidanceSlip> CreateGuidanceSlipAsync(SlipRequest request, CancellationToken ct);
}
