using System.Windows;
using System.Windows.Controls;
using RangHamMat.Controls;
namespace RangHamMat.Views;
public partial class CuaSoBenhNhan : UserControl
{
    public CuaSoBenhNhan()
    {
        InitializeComponent();
        BirthDate.DisplayDateEnd = DateTime.Today;
        ClinicalUi.Observe(this, RefreshForm);
    }
    private void RefreshForm()
    {
        ClinicalForm.Prepare(this);
        var table=ClinicalForm.GetTable(this); var existing=ClinicalForm.GetRecord(this);
        var newPatient=table=="BENH_NHAN" && (existing is null || ClinicalData.Find(table,UiRecord.Get(existing,"MaBN")) is null);
        CreateMedical.Visibility=newPatient?Visibility.Visible:Visibility.Collapsed;
        EditorTitle.Text=table=="HO_SO_BENH_AN"?"Hồ sơ bệnh án":newPatient?"Thêm bệnh nhân":"Cập nhật thông tin bệnh nhân";
    }
}
