using System.Windows;
namespace RangHamMat.Controls;
public static class ClinicalNavigator
{
    public static void Open(DependencyObject origin, string module, string patientId, string? visitId = null, string? prescriptionId = null)
        => ClinicalUi.Workspace(origin)?.NavigateClinical(module, patientId, visitId, prescriptionId);
}
