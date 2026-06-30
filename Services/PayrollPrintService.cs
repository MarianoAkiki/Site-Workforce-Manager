using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Site_Workforce_Manager.ViewModels;

namespace Site_Workforce_Manager.Services;

public static class PayrollPrintService
{
    private static readonly FontFamily PrintFont     = new("Arial");
    private static readonly Brush DarkHeaderBrush   = new SolidColorBrush(Color.FromRgb(210, 210, 210));
    private static readonly Brush GroupHeaderBg     = new SolidColorBrush(Color.FromRgb(225, 225, 225));
    private static readonly Brush GroupHeaderFg     = Brushes.Black;
    private static readonly Brush SubtotalBg        = new SolidColorBrush(Color.FromRgb(240, 240, 240));
    private static readonly Brush GrandTotalBg      = new SolidColorBrush(Color.FromRgb(210, 210, 210));
    private static readonly Brush GridLineBrush     = new SolidColorBrush(Color.FromRgb(140, 140, 140));
    private static readonly Brush AltRowBrush       = new SolidColorBrush(Color.FromRgb(250, 250, 250));
    private static readonly Thickness CellBorder    = new(1);

    public static void Print(
        DateTime weekStart,
        DateTime weekEnd,
        IList<PayrollTradeGroup> groups,
        decimal grandTotalBalance,
        decimal grandTotalPayment)
    {
        var dlg = new PrintDialog();
        dlg.PrintTicket.PageOrientation = PageOrientation.Portrait;
        dlg.PrintTicket.PageMediaSize   = new PageMediaSize(PageMediaSizeName.ISOA4);

        if (dlg.ShowDialog() != true) return;

        var doc = BuildDocument(weekStart, weekEnd, groups,
                                grandTotalBalance, grandTotalPayment, dlg);
        dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator,
                          $"Payroll {weekStart:dd-MM-yyyy}");
    }

    private static FlowDocument BuildDocument(
        DateTime weekStart,
        DateTime weekEnd,
        IList<PayrollTradeGroup> groups,
        decimal grandTotalBalance,
        decimal grandTotalPayment,
        PrintDialog dlg)
    {
        var pageWidth  = dlg.PrintableAreaWidth;
        var pageHeight = dlg.PrintableAreaHeight;
        const double hPad = 50;
        const double vPad = 40;

        var doc = new FlowDocument
        {
            PageWidth   = pageWidth,
            PageHeight  = pageHeight,
            PagePadding = new Thickness(hPad, vPad, hPad, vPad),
            ColumnWidth = pageWidth - hPad * 2,
            FontFamily  = PrintFont,
            FontSize    = 12
        };

        // Title
        var title = new Paragraph { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 18) };
        title.Inlines.Add(new Run("Payroll Summary") { FontSize = 16, FontWeight = FontWeights.Bold });
        title.Inlines.Add(new LineBreak());
        title.Inlines.Add(new Run($"{weekStart:dd/MM/yyyy}  —  {weekEnd:dd/MM/yyyy}")
            { FontSize = 10, Foreground = Brushes.DimGray });
        doc.Blocks.Add(title);

        var available = pageWidth - hPad * 2;
        doc.Blocks.Add(BuildTable(groups, grandTotalBalance, grandTotalPayment, available));
        return doc;
    }

    private static Table BuildTable(
        IList<PayrollTradeGroup> groups,
        decimal grandTotalBalance,
        decimal grandTotalPayment,
        double available)
    {
        const double idCol      = 55;
        const double balanceCol = 145;
        const double paymentCol = 150;
        var nameCol = available - idCol - balanceCol - paymentCol;

        var table = new Table
        {
            CellSpacing     = 0,
            BorderBrush     = GridLineBrush,
            BorderThickness = new Thickness(1),
            FontFamily      = PrintFont
        };
        table.Columns.Add(new TableColumn { Width = new GridLength(idCol) });
        table.Columns.Add(new TableColumn { Width = new GridLength(nameCol) });
        table.Columns.Add(new TableColumn { Width = new GridLength(balanceCol) });
        table.Columns.Add(new TableColumn { Width = new GridLength(paymentCol) });

        var rg = new TableRowGroup();

        // Column header row
        var hdr = new TableRow();
        hdr.Cells.Add(Cell("ID",                 DarkHeaderBrush, Brushes.Black, bold: true));
        hdr.Cells.Add(Cell("Worker",             DarkHeaderBrush, Brushes.Black, bold: true));
        hdr.Cells.Add(Cell("Balance",            DarkHeaderBrush, Brushes.Black, bold: true, align: TextAlignment.Right));
        hdr.Cells.Add(Cell("Payment This Week",  DarkHeaderBrush, Brushes.Black, bold: true, align: TextAlignment.Right));
        rg.Rows.Add(hdr);

        foreach (var group in groups)
        {
            // Trade group header (spans all 4 cols)
            var groupHdrRow  = new TableRow();
            var groupHdrCell = new TableCell(Para(group.TradeName, bold: true, fg: GroupHeaderFg))
            {
                ColumnSpan      = 4,
                Background      = GroupHeaderBg,
                BorderBrush     = GridLineBrush,
                BorderThickness = CellBorder,
                Padding         = new Thickness(8, 8, 8, 8)
            };
            groupHdrRow.Cells.Add(groupHdrCell);
            rg.Rows.Add(groupHdrRow);

            // Worker rows
            var rowIndex = 0;
            foreach (var row in group.Rows)
            {
                var bg  = rowIndex++ % 2 == 1 ? AltRowBrush : Brushes.White;
                var tr  = new TableRow();
                var pay = string.IsNullOrWhiteSpace(row.PaymentAmountText)
                    ? string.Empty
                    : Fmt(row.PaymentAmount);

                tr.Cells.Add(Cell(row.WorkerId.ToString(),  bg, null));
                tr.Cells.Add(Cell(row.WorkerName,           bg, null));
                tr.Cells.Add(Cell(row.BalanceDisplay,       bg, null, align: TextAlignment.Right));
                tr.Cells.Add(Cell(pay,                      bg, null, align: TextAlignment.Right));
                rg.Rows.Add(tr);
            }

            // Category subtotal
            var sub = new TableRow();
            sub.Cells.Add(Cell(string.Empty,              SubtotalBg, null));
            sub.Cells.Add(Cell("Category Total",          SubtotalBg, null, bold: true));
            sub.Cells.Add(Cell(group.TotalBalanceDisplay, SubtotalBg, null, bold: true, align: TextAlignment.Right));
            sub.Cells.Add(Cell(group.TotalPaymentDisplay, SubtotalBg, null, bold: true, align: TextAlignment.Right));
            rg.Rows.Add(sub);
        }

        // Grand total
        var grand = new TableRow();
        grand.Cells.Add(Cell(string.Empty,                  GrandTotalBg, null));
        grand.Cells.Add(Cell("Grand Total",                 GrandTotalBg, GroupHeaderFg, bold: true, size: 13));
        grand.Cells.Add(Cell(Fmt(grandTotalBalance),        GrandTotalBg, GroupHeaderFg, bold: true, size: 13, align: TextAlignment.Right));
        grand.Cells.Add(Cell(Fmt(grandTotalPayment),        GrandTotalBg, GroupHeaderFg, bold: true, size: 13, align: TextAlignment.Right));
        rg.Rows.Add(grand);

        table.RowGroups.Add(rg);
        return table;
    }

    private static TableCell Cell(
        string        text,
        Brush?        bg    = null,
        Brush?        fg    = null,
        bool          bold  = false,
        double        size  = 12,
        TextAlignment align = TextAlignment.Left)
    {
        return new TableCell(Para(text, bold: bold, size: size, fg: fg, align: align))
        {
            BorderBrush     = GridLineBrush,
            BorderThickness = CellBorder,
            Background      = bg ?? Brushes.White,
            Padding         = new Thickness(8, 9, 8, 9)
        };
    }

    private static string Fmt(decimal value) =>
        value < 0 ? $"({Math.Abs(value):C})" : value.ToString("C");

    private static Paragraph Para(
        string        text,
        bool          bold  = false,
        double        size  = 10,
        Brush?        fg    = null,
        TextAlignment align = TextAlignment.Left)
    {
        return new Paragraph(new Run(text)
        {
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            FontSize   = size,
            Foreground = fg ?? Brushes.Black
        })
        {
            Margin        = new Thickness(0),
            TextAlignment = align,
            LineHeight    = 14
        };
    }
}
