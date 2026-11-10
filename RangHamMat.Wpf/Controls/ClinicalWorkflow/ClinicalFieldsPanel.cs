using System.Windows;
using System.Windows.Controls;

namespace RangHamMat.Controls.ClinicalWorkflow;

/// <summary>Đặt các trường khám cạnh nhau khi đủ rộng, xếp dọc khi khung nhập liệu hẹp.</summary>
public sealed class ClinicalFieldsPanel : Panel
{
    private const double TwoColumnWidth=620;
    private const double Gap=16;

    protected override Size MeasureOverride(Size availableSize)
    {
        var width=double.IsPositiveInfinity(availableSize.Width)?TwoColumnWidth:availableSize.Width;
        var columns=width>=TwoColumnWidth?2:1;
        var fieldWidth=Math.Max(0,(width-(columns-1)*Gap)/columns);
        var height=0d;
        for(var row=0;row<InternalChildren.Count;row+=columns)
        {
            var rowHeight=0d;
            for(var column=0;column<columns&&row+column<InternalChildren.Count;column++)
            {
                var child=InternalChildren[row+column];
                child.Measure(new Size(fieldWidth,double.PositiveInfinity));
                rowHeight=Math.Max(rowHeight,child.DesiredSize.Height);
            }
            height+=(row>0?Gap:0)+rowHeight;
        }
        return new Size(width,height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns=finalSize.Width>=TwoColumnWidth?2:1;
        var fieldWidth=Math.Max(0,(finalSize.Width-(columns-1)*Gap)/columns);
        var top=0d;
        for(var row=0;row<InternalChildren.Count;row+=columns)
        {
            var rowHeight=0d;
            for(var column=0;column<columns&&row+column<InternalChildren.Count;column++)
                rowHeight=Math.Max(rowHeight,InternalChildren[row+column].DesiredSize.Height);
            for(var column=0;column<columns&&row+column<InternalChildren.Count;column++)
                InternalChildren[row+column].Arrange(new Rect(column*(fieldWidth+Gap),top,fieldWidth,rowHeight));
            top+=rowHeight+Gap;
        }
        return finalSize;
    }
}
