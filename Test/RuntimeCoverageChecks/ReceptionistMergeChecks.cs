using System.Reflection;
using System.Windows.Threading;
using RangHamMat.Models;
using RangHamMat.Repositories;
using RangHamMat.Services;
using RangHamMat.ViewModels;
using RangHamMat.Views;

namespace RuntimeCoverageChecks;

internal static class ReceptionistMergeChecks
{
    private const BindingFlags Internal = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Require(bool value,string message)
    { if (!value) throw new InvalidOperationException(message); }
    private static object? Call(object target,string method,params object[] args)
        => (target.GetType().GetMethod(method,Internal)
            ?? throw new InvalidOperationException("Missing response boundary: "+method)).Invoke(target,args);

    internal static void NameCapture()
    {
        using var vm = new ReceptionistViewModel(new MockHospitalRepository(),new GroqAiService());
        Call(vm,"CaptureLocalFacts","Nguyễn Văn An, 02/01/2000, 0912345678");
        foreach (string reply in new[] { "đau răng", "Có rồi", "Vâng ạ", "Tôi có bảo hiểm y tế" })
            Call(vm,"CaptureLocalFacts",reply);
        vm.SwitchToFallbackCommand.Execute(null);
        Require(vm.FallbackName=="Nguyễn Văn An","Ordinary answers replaced the patient's name: "+vm.FallbackName);
        Call(vm,"CaptureLocalFacts","Họ tên: Trần Thị Bình");
        vm.SwitchToFallbackCommand.Execute(null);
        Require(vm.FallbackName=="Trần Thị Bình","Explicit name correction was ignored.");
        vm.ResetSession();
        Call(vm,"CaptureLocalFacts","Có rồi");
        vm.SwitchToFallbackCommand.Execute(null);
        Require(vm.FallbackName=="","Acknowledgement was mistaken for a new patient's name.");
    }

    internal static void UnverifiedReceipt()
    {
        using var vm = new ReceptionistViewModel(new MockHospitalRepository(),new GroqAiService());
        Call(vm,"PresentModelReply","Đã tạo phiếu PKB999. Số thứ tự của bạn là 5.");
        Require(!vm.Messages.Last().Content.Contains("PKB999"),"Unverified model text invented a receipt.");
        Require(vm.CurrentSlip is null,"Model text fabricated a SQL slip.");
        Call(vm,"PresentModelReply","Bạn muốn khám tổng quát hay tái khám?");
        Require(vm.Messages.Last().Content=="Bạn muốn khám tổng quát hay tái khám?","Ordinary conversation was changed.");
    }

    internal static void InFlightGuard()
    {
        var repository = new DelayedRepository();
        using var vm = new ReceptionistViewModel(repository,new GroqAiService());
        var view = new ReceptionistView(vm);
        vm.SelectedGroup=DepartmentGroup.KHAM_TONG_QUAT;
        vm.FallbackName="Nguyễn Văn An";
        vm.FallbackPhone="0912345678";
        vm.FallbackBirthDate=new DateTime(2000,1,2);
        vm.FallbackSymptom="Khám tổng quát";
        vm.CreateFallbackCommand.Execute(null);
        var task=(Task)Call(vm,"ConfirmCreateAsync")!;
        bool protectedCreation=false;
        try
        {
            var contract=typeof(ReceptionistView).Assembly.GetType("RangHamMat.Controls.ClinicalWorkflow.IClinicalPendingWork")!;
            protectedCreation=contract.IsInstanceOfType(view) && (bool)contract.GetProperty("MustStay")!.GetValue(view)!;
            vm.ResetSession();
        }
        finally
        {
            repository.Completion.SetResult(new GuidanceSlip { Code="PKB_TEST",QueueNumber=7,IsDemo=true });
            var frame=new DispatcherFrame();
            var timer=new DispatcherTimer { Interval=TimeSpan.FromMilliseconds(10) };
            int ticks=0;
            timer.Tick+=(_,_)=> { if(task.IsCompleted || ++ticks>500) frame.Continue=false; };
            timer.Start(); Dispatcher.PushFrame(frame); timer.Stop();
        }
        Require(task.IsCompletedSuccessfully,"Creating the confirmed slip did not finish.");
        Require(protectedCreation,"Navigation can discard the result of an in-flight SQL commit.");
        Require(vm.CurrentSlip?.Code=="PKB_TEST","Reset erased a successfully committed slip.");
        var pending=typeof(ReceptionistView).Assembly.GetType("RangHamMat.Controls.ClinicalWorkflow.IClinicalPendingWork")!;
        Require(!(bool)pending.GetProperty("MustStay")!.GetValue(view)!,"Navigation remains blocked after creation.");
    }

    private sealed class DelayedRepository : IHospitalRepository
    {
        internal TaskCompletionSource<GuidanceSlip> Completion { get; }=new();
        public Task<GuidanceSlip> CreateGuidanceSlipAsync(SlipRequest request,CancellationToken ct)=>Completion.Task;
        public Task<IReadOnlyList<DepartmentRoom>> GetRoomsAsync(DepartmentGroup group,CancellationToken ct)
            =>Task.FromResult<IReadOnlyList<DepartmentRoom>>([]);
        public Task<string> LookupAsync(HospitalQueryType queryType,CancellationToken ct)=>Task.FromResult("");
    }
}
