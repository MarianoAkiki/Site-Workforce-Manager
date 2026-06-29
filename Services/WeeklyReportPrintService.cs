using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Site_Workforce_Manager.ViewModels;

namespace Site_Workforce_Manager.Services;

public static class WeeklyReportPrintService
{
    private static readonly FontFamily ArabicFont   = new("Arial");
    private static readonly Brush HeaderBg          = new SolidColorBrush(Color.FromRgb(210, 210, 210));
    private static readonly Brush TotalsBg          = new SolidColorBrush(Color.FromRgb(225, 225, 225));
    private static readonly Brush AltRowBrush       = new SolidColorBrush(Color.FromRgb(250, 250, 250));
    private static readonly Brush GridLineBrush     = new SolidColorBrush(Color.FromRgb(60, 60, 60));
    private static readonly Thickness CellBorder    = new(0.8);

    private static readonly Dictionary<DayOfWeek, string> ArabicDayNames = new()
    {
        { DayOfWeek.Saturday,  "السبت"    },
        { DayOfWeek.Sunday,    "الأحد"    },
        { DayOfWeek.Monday,    "الإثنين"  },
        { DayOfWeek.Tuesday,   "الثلاثاء" },
        { DayOfWeek.Wednesday, "الأربعاء" },
        { DayOfWeek.Thursday,  "الخميس"   },
        { DayOfWeek.Friday,    "الجمعة"   },
    };

    public static void Print(DateTime weekStart, DateTime weekEnd, IList<WeeklyReportRow> rows)
    {
        var dlg = new PrintDialog();
        dlg.PrintTicket.PageOrientation = PageOrientation.Landscape;
        dlg.PrintTicket.PageMediaSize   = new PageMediaSize(PageMediaSizeName.ISOA4);

        if (dlg.ShowDialog() != true) return;

        var doc = BuildDocument(weekStart, weekEnd, rows, dlg);
        dlg.PrintDocument(
            ((IDocumentPaginatorSource)doc).DocumentPaginator,
            $"كشف أسبوعي {weekStart:dd-MM-yyyy}");
    }

    private static FlowDocument BuildDocument(
        DateTime weekStart, DateTime weekEnd,
        IList<WeeklyReportRow> rows, PrintDialog dlg)
    {
        const double hPad = 40;
        const double vPad = 36;

        var doc = new FlowDocument
        {
            PageWidth     = dlg.PrintableAreaWidth,
            PageHeight    = dlg.PrintableAreaHeight,
            PagePadding   = new Thickness(hPad, vPad, hPad, vPad),
            ColumnWidth   = dlg.PrintableAreaWidth,
            FontFamily    = ArabicFont,
            FontSize      = 9,
            FlowDirection = FlowDirection.RightToLeft,
        };

        // Title
        var title = new Paragraph { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 14) };
        title.Inlines.Add(new Run("كشف أسبوعي") { FontSize = 16, FontWeight = FontWeights.Bold });
        title.Inlines.Add(new LineBreak());
        title.Inlines.Add(new Run($"{weekStart:dd/MM/yyyy}  —  {weekEnd:dd/MM/yyyy}")
            { FontSize = 9, Foreground = Brushes.DimGray });
        doc.Blocks.Add(title);

        var available = dlg.PrintableAreaWidth - hPad * 2;
        doc.Blocks.Add(BuildTable(weekStart, rows, available));
        return doc;
    }

    private static Table BuildTable(DateTime weekStart, IList<WeeklyReportRow> rows, double available)
    {
        // Fixed widths for day + stat columns; Name gets the rest
        const double idCol       = 34;
        const double dayCol      = 50;
        const double hrsCol      = 42;
        const double daysCol     = 36;
        const double rateCol     = 62;
        const double balCol      = 65;
        const double earnCol     = 65;
        const double totEarnCol  = 70;
        const double totPaidCol  = 65;
        const double owedCol     = 70;
        const double sigCol      = 72;
        double fixedWidth = idCol + 7 * dayCol + hrsCol + daysCol + rateCol + balCol + earnCol + totEarnCol + totPaidCol + owedCol + sigCol;
        double nameCol = Math.Max(90, available - fixedWidth);

        var table = new Table
        {
            CellSpacing     = 0,
            BorderBrush     = GridLineBrush,
            BorderThickness = new Thickness(1.2),
            FontFamily      = ArabicFont,
            FlowDirection   = FlowDirection.RightToLeft,
        };

        table.Columns.Add(new TableColumn { Width = new GridLength(idCol) });
        table.Columns.Add(new TableColumn { Width = new GridLength(nameCol) });
        for (var i = 0; i < 7; i++)
            table.Columns.Add(new TableColumn { Width = new GridLength(dayCol) });
        table.Columns.Add(new TableColumn { Width = new GridLength(hrsCol) });
        table.Columns.Add(new TableColumn { Width = new GridLength(daysCol) });
        table.Columns.Add(new TableColumn { Width = new GridLength(rateCol) });
        table.Columns.Add(new TableColumn { Width = new GridLength(balCol) });
        table.Columns.Add(new TableColumn { Width = new GridLength(earnCol) });
        table.Columns.Add(new TableColumn { Width = new GridLength(totEarnCol) });
        table.Columns.Add(new TableColumn { Width = new GridLength(totPaidCol) });
        table.Columns.Add(new TableColumn { Width = new GridLength(owedCol) });
        table.Columns.Add(new TableColumn { Width = new GridLength(sigCol) });

        var rg = new TableRowGroup();

        // Header row
        var hdr = new TableRow();
        hdr.Cells.Add(ColHeader("رقم",          idCol));
        hdr.Cells.Add(ColHeader("الاسم",        nameCol, TextAlignment.Right));
        for (var i = 0; i < 7; i++)
        {
            var day = weekStart.AddDays(i);
            var dayName = ArabicDayNames.TryGetValue(day.DayOfWeek, out var n) ? n : day.ToString("ddd");
            hdr.Cells.Add(DayHeader(dayName, day.ToString("dd/MM"), dayCol));
        }
        hdr.Cells.Add(ColHeader("ساعات",           hrsCol));
        hdr.Cells.Add(ColHeader("أيام",            daysCol));
        hdr.Cells.Add(ColHeader("الأجر",           rateCol));
        hdr.Cells.Add(ColHeader("رصيد سابق",       balCol));
        hdr.Cells.Add(ColHeader("مكتسب",           earnCol));
        hdr.Cells.Add(ColHeader("إجمالي الكسب",    totEarnCol));
        hdr.Cells.Add(ColHeader("إجمالي المدفوع",  totPaidCol));
        hdr.Cells.Add(ColHeader("المستحق",         owedCol));
        hdr.Cells.Add(ColHeader("التوقيع",         sigCol));
        rg.Rows.Add(hdr);

        // Data rows (skip the totals row at the end — we add our own)
        var dataRows = rows.Where(r => !r.IsTotalsRow).ToList();
        var rowIndex = 0;
        foreach (var row in dataRows)
        {
            var bg = rowIndex++ % 2 == 1 ? AltRowBrush : Brushes.White;
            var tr = new TableRow();
            tr.Cells.Add(Cell(row.WorkerId.ToString(), bg));
            tr.Cells.Add(Cell(row.WorkerName,          bg, TextAlignment.Right));
            tr.Cells.Add(Cell(row.Day0Display,         bg));
            tr.Cells.Add(Cell(row.Day1Display,         bg));
            tr.Cells.Add(Cell(row.Day2Display,         bg));
            tr.Cells.Add(Cell(row.Day3Display,         bg));
            tr.Cells.Add(Cell(row.Day4Display,         bg));
            tr.Cells.Add(Cell(row.Day5Display,         bg));
            tr.Cells.Add(Cell(row.Day6Display,         bg));
            tr.Cells.Add(Cell(row.TotalHoursDisplay,   bg));
            tr.Cells.Add(Cell(row.NumberOfDays > 0 ? row.NumberOfDays.ToString() : string.Empty, bg));
            tr.Cells.Add(Cell(row.DailyRateDisplay,    bg));
            tr.Cells.Add(Cell(row.BalanceBeforeWeekDisplay, bg));
            tr.Cells.Add(Cell(row.WeekEarningsDisplay, bg));
            tr.Cells.Add(Cell(row.TotalEarnedDisplay,  bg));
            tr.Cells.Add(Cell(row.TotalPaidDisplay,    bg));
            tr.Cells.Add(Cell(row.TotalBalanceDisplay, bg));
            tr.Cells.Add(Cell(string.Empty,            bg));  // signature
            rg.Rows.Add(tr);
        }

        // Totals row
        if (rows.FirstOrDefault(r => r.IsTotalsRow) is { } totals)
        {
            var tot = new TableRow();
            tot.Cells.Add(Cell(string.Empty,                  TotalsBg, bold: true));
            tot.Cells.Add(Cell("الإجمالي",                    TotalsBg, TextAlignment.Right, bold: true));
            tot.Cells.Add(Cell(totals.Day0Display,            TotalsBg, bold: true));
            tot.Cells.Add(Cell(totals.Day1Display,            TotalsBg, bold: true));
            tot.Cells.Add(Cell(totals.Day2Display,            TotalsBg, bold: true));
            tot.Cells.Add(Cell(totals.Day3Display,            TotalsBg, bold: true));
            tot.Cells.Add(Cell(totals.Day4Display,            TotalsBg, bold: true));
            tot.Cells.Add(Cell(totals.Day5Display,            TotalsBg, bold: true));
            tot.Cells.Add(Cell(totals.Day6Display,            TotalsBg, bold: true));
            tot.Cells.Add(Cell(totals.TotalHoursDisplay,      TotalsBg, bold: true));
            tot.Cells.Add(Cell(string.Empty,                  TotalsBg));
            tot.Cells.Add(Cell(string.Empty,                  TotalsBg));
            tot.Cells.Add(Cell(totals.BalanceBeforeWeekDisplay, TotalsBg, bold: true));
            tot.Cells.Add(Cell(totals.WeekEarningsDisplay,    TotalsBg, bold: true));
            tot.Cells.Add(Cell(totals.TotalEarnedDisplay,     TotalsBg, bold: true));
            tot.Cells.Add(Cell(totals.TotalPaidDisplay,       TotalsBg, bold: true));
            tot.Cells.Add(Cell(totals.TotalBalanceDisplay,    TotalsBg, bold: true));
            tot.Cells.Add(Cell(string.Empty,                  TotalsBg));  // signature
            rg.Rows.Add(tot);
        }

        table.RowGroups.Add(rg);
        return table;
    }

    private static TableCell ColHeader(string text, double width, TextAlignment align = TextAlignment.Center)
    {
        return new TableCell(Para(text, bold: true, align: align))
        {
            BorderBrush     = GridLineBrush,
            BorderThickness = CellBorder,
            Background      = HeaderBg,
            Padding         = new Thickness(4, 7, 4, 7),
        };
    }

    private static TableCell DayHeader(string dayName, string date, double width)
    {
        var para = new Paragraph { TextAlignment = TextAlignment.Center, Margin = new Thickness(0), LineHeight = 13 };
        para.Inlines.Add(new Run(dayName) { FontWeight = FontWeights.Bold, FontSize = 9 });
        para.Inlines.Add(new LineBreak());
        para.Inlines.Add(new Run(date) { FontSize = 8, Foreground = Brushes.DimGray });

        return new TableCell(para)
        {
            BorderBrush     = GridLineBrush,
            BorderThickness = CellBorder,
            Background      = HeaderBg,
            Padding         = new Thickness(2, 5, 2, 5),
        };
    }

    private static TableCell Cell(string text, Brush? bg = null, TextAlignment align = TextAlignment.Center, bool bold = false)
    {
        return new TableCell(Para(text, bold: bold, align: align))
        {
            BorderBrush     = GridLineBrush,
            BorderThickness = CellBorder,
            Background      = bg ?? Brushes.White,
            Padding         = new Thickness(3, 6, 3, 6),
        };
    }

    private static Paragraph Para(string text, bool bold = false, double size = 9, TextAlignment align = TextAlignment.Center)
    {
        return new Paragraph(new Run(text)
        {
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            FontSize   = size,
        })
        {
            Margin        = new Thickness(0),
            TextAlignment = align,
            LineHeight    = 13,
        };
    }
}
