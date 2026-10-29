using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;
public partial class DichVuThucHien:UserControl,IPatientScoped
{
 private readonly ThucHienDichVuEditor history;
 public string? PatientId=>history.PatientId;
 public DichVuThucHien(){InitializeComponent();history=new();HistoryHost.Content=history;}
 public void SetPatient(string id)=>history.SetPatient(id);
 public void FocusContext(string? patientId,string? visitId=null,string? prescriptionId=null)=>history.FocusContext(patientId,visitId);
}