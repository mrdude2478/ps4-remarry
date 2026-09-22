using System.Drawing;
using System.Windows.Forms;

namespace PS4Remarry.WinForms;

public sealed class DataGridViewProgressColumn : DataGridViewColumn
{
    public DataGridViewProgressColumn()
    {
        CellTemplate = new DataGridViewProgressCell();
    }
}

internal sealed class DataGridViewProgressCell : DataGridViewTextBoxCell
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
                   value, "", errorText, cellStyle,
                   advancedBorderStyle,
                   paintParts & ~DataGridViewPaintParts.ContentForeground);

        int pct = 0;
        if (value is int i) pct = i;
        else if (value is not null && int.TryParse(value.ToString(), out var p)) pct = p;
        pct = Math.Clamp(pct, 0, 100);

        var barRect = new Rectangle(
            cellBounds.X + 2, cellBounds.Y + 2,
            cellBounds.Width - 4, cellBounds.Height - 4);

        using var back = new SolidBrush(Color.FromArgb(60, 60, 60));
        g.FillRectangle(back, barRect);

        int filled = (int)(barRect.Width * (pct / 100.0));
        if (filled > 0)
        {
            using var fore = new SolidBrush(Color.FromArgb(60, 160, 60));
            g.FillRectangle(fore, new Rectangle(barRect.X, barRect.Y, filled, barRect.Height));
        }

        var text = $"{pct}%";
        TextRenderer.DrawText(g, text, cellStyle.Font ?? SystemFonts.DefaultFont,
            barRect, Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}