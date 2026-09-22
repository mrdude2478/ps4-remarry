using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace PS4Remarry.WinForms;

/// <summary>
/// A text column whose cells can paint their text in a per-row colour.
/// Reads the colour from a bound property (e.g. DigestColorArgb) on the
/// row's object.
/// </summary>
public sealed class DataGridViewColoredTextColumn : DataGridViewColumn
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [DefaultValue("")]
    public string ColorPropertyName { get; set; } = "";

    public DataGridViewColoredTextColumn()
    {
        CellTemplate = new DataGridViewColoredTextCell();
    }

    internal string ResolveColorPropertyName()
    {
        if (!string.IsNullOrEmpty(ColorPropertyName)) return ColorPropertyName;
        return string.IsNullOrEmpty(DataPropertyName) ? "" : DataPropertyName + "ColorArgb";
    }
}

internal sealed class DataGridViewColoredTextCell : DataGridViewTextBoxCell
{
    protected override void Paint(
        Graphics g,
        Rectangle clipBounds,
        Rectangle cellBounds,
        int rowIndex,
        DataGridViewElementStates cellState,
        object? value,
        object? formattedValue,
        string? errorText,
        DataGridViewCellStyle cellStyle,
        DataGridViewAdvancedBorderStyle advancedBorderStyle,
        DataGridViewPaintParts paintParts)
    {
        base.Paint(g, clipBounds, cellBounds, rowIndex, cellState,
                   value, "", errorText, cellStyle, advancedBorderStyle,
                   paintParts & ~DataGridViewPaintParts.ContentForeground);

        if (value is null) return;

        Color fore = cellStyle.ForeColor;

        bool isSelected = (cellState & DataGridViewElementStates.Selected) != 0;
        if (!isSelected && OwningColumn is DataGridViewColoredTextColumn col)
        {
            var propName = col.ResolveColorPropertyName();
            if (!string.IsNullOrEmpty(propName) &&
                DataGridView?.Rows[rowIndex].DataBoundItem is object item)
            {
                var prop = item.GetType().GetProperty(propName);
                if (prop?.GetValue(item) is int argb)
                    fore = Color.FromArgb(argb);
            }
        }

        var text = value.ToString() ?? "";
        TextRenderer.DrawText(g, text, cellStyle.Font ?? SystemFonts.DefaultFont,
            cellBounds, fore,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter
            | TextFormatFlags.EndEllipsis);
    }
}