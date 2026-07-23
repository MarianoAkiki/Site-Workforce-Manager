using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Site_Workforce_Manager.ViewModels;

namespace Site_Workforce_Manager.Services;

public static class WeeklyPrintService
{
    private static readonly FontFamily PrintFont    = new FontFamily("Arial");
    private static readonly Brush DarkHeaderBrush  = new SolidColorBrush(Color.FromRgb(210, 210, 210));
    private static readonly Brush SubHeaderBrush   = new SolidColorBrush(Color.FromRgb(235, 235, 235));
    private static readonly Brush DateFgBrush      = new SolidColorBrush(Color.FromRgb(90, 90, 90));
    private static readonly Brush GridLineBrush    = new SolidColorBrush(Color.FromRgb(60, 60, 60));

    public static void PrintWeeklyView(
        string tradeName,
        DateTime weekStart,
        DateTime weekEnd,
        IList<WeeklyWorkerRow> rows)
    {
        var dlg = new PrintDialog();
        dlg.PrintTicket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4);

        if (dlg.ShowDialog() != true) return;

        dlg.PrintTicket.PageOrientation = PageOrientation.Landscape;

        var doc = BuildDocument(tradeName, weekStart, weekEnd, rows, dlg);
        dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator,
                          $"Weekly Work Entry - {tradeName}");
    }

    private static FlowDocument BuildDocument(
        string tradeName,
        DateTime weekStart,
        DateTime weekEnd,
        IList<WeeklyWorkerRow> rows,
        PrintDialog dlg)
    {
        var pageWidth  = dlg.PrintableAreaWidth;
        var pageHeight = dlg.PrintableAreaHeight;
        const double hPad = 40;
        const double vPad = 32;

        var doc = new FlowDocument
        {
            PageWidth    = pageWidth,
            PageHeight   = pageHeight,
            PagePadding  = new Thickness(hPad, vPad, hPad, vPad),
            ColumnWidth  = pageWidth - hPad * 2,
            FontFamily   = PrintFont,
            FontSize     = 10
        };

        // Title block
        var titlePara = new Paragraph { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 14) };
        titlePara.Inlines.Add(new Run($"سجل الحضور الأسبوعي  —  {tradeName}")
        {
            FontSize   = 15,
            FontWeight = FontWeights.Bold
        });
        titlePara.Inlines.Add(new LineBreak());
        titlePara.Inlines.Add(new Run($"{weekStart:dd/MM/yyyy}   —   {weekEnd:dd/MM/yyyy}")
        {
            FontSize   = 10,
            Foreground = Brushes.DimGray
        });
        doc.Blocks.Add(titlePara);

        // 16 columns → 17 gaps × CellSpacing(1) + 2 × BorderThickness(1) = 19px overhead
        var available  = pageWidth - hPad * 2 - 19;
        doc.Blocks.Add(BuildTable(weekStart, rows, available));
        return doc;
    }

    private static Table BuildTable(DateTime weekStart, IList<WeeklyWorkerRow> rows, double available)
    {
        // Proportional weights — sum exactly to available width, no rounding drift
        const double idW     = 0.5;
        const double workerW = 1.5;
        const double hoursW  = 1.0;
        const double siteW   = 1.9;
        const double totalW  = idW + workerW + 7 * (hoursW + siteW); // 2.0 + 20.3 = 22.3
        double unit      = available / totalW;
        double idColW     = idW     * unit;
        double workerColW = workerW * unit;
        double hoursColW  = hoursW  * unit;
        double siteColW   = siteW   * unit;

        var table = new Table
        {
            CellSpacing     = 1,
            Background      = GridLineBrush,
            BorderBrush     = GridLineBrush,
            BorderThickness = new Thickness(1),
            FontFamily      = PrintFont
        };

        table.Columns.Add(new TableColumn { Width = new GridLength(idColW) });
        table.Columns.Add(new TableColumn { Width = new GridLength(workerColW) });
        for (int i = 0; i < 7; i++)
        {
            table.Columns.Add(new TableColumn { Width = new GridLength(hoursColW) });
            table.Columns.Add(new TableColumn { Width = new GridLength(siteColW) });
        }

        // ── Header row 1: day names ────────────────────────────────
        var headerGroup = new TableRowGroup();
        var dayRow      = new TableRow();
        dayRow.Cells.Add(MakeCell("#",       colspan: 1, bg: DarkHeaderBrush, fg: Brushes.Black, bold: true, size: 10));
        dayRow.Cells.Add(MakeCell("العامل", colspan: 1, bg: DarkHeaderBrush, fg: Brushes.Black, rtl: true, bold: true, size: 11));

        for (int d = 0; d < 7; d++)
        {
            var date    = weekStart.AddDays(d);
            var dayCell = new TableCell
            {
                ColumnSpan = 2,
                Background = DarkHeaderBrush,
                Padding    = new Thickness(4, 8, 4, 8)
            };
            var para = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                Margin        = new Thickness(0),
                LineHeight    = 16,
                FlowDirection = FlowDirection.RightToLeft
            };
            para.Inlines.Add(new Run(GetArabicDayName(date.DayOfWeek))
            {
                FontWeight = FontWeights.Bold,
                FontSize   = 11,
                Foreground = Brushes.Black
            });
            para.Inlines.Add(new LineBreak());
            para.Inlines.Add(new Run(date.ToString("dd/MM"))
            {
                FontSize   = 9,
                Foreground = DateFgBrush
            });
            dayCell.Blocks.Add(para);
            dayRow.Cells.Add(dayCell);
        }
        headerGroup.Rows.Add(dayRow);

        // ── Header row 2: sub-labels (ساعات / ورشة) ───────────────
        var subRow = new TableRow();
        subRow.Cells.Add(MakeCell(string.Empty, colspan: 1, bg: SubHeaderBrush));
        subRow.Cells.Add(MakeCell(string.Empty, colspan: 1, bg: SubHeaderBrush));
        for (int d = 0; d < 7; d++)
        {
            subRow.Cells.Add(MakeCell("ساعات", colspan: 1, bg: SubHeaderBrush, rtl: true, bold: true, size: 9));
            subRow.Cells.Add(MakeCell("ورشة",  colspan: 1, bg: SubHeaderBrush, rtl: true, bold: true, size: 9));
        }
        headerGroup.Rows.Add(subRow);
        table.RowGroups.Add(headerGroup);

        // ── Data rows ──────────────────────────────────────────────
        var dataGroup = new TableRowGroup();

        foreach (var workerRow in rows)
        {
            var tr = new TableRow();
            tr.Cells.Add(MakeDataCell(workerRow.WorkerId.ToString(), center: true));
            tr.Cells.Add(MakeCell(workerRow.WorkerName, colspan: 1));
            foreach (var cell in workerRow.Cells)
            {
                tr.Cells.Add(MakeDataCell(cell.DurationHoursText, center: true));
                tr.Cells.Add(MakeDataCell(string.Empty));  // blank — fill manually
            }
            dataGroup.Rows.Add(tr);
        }

        table.RowGroups.Add(dataGroup);
        return table;
    }

    private static TableCell MakeCell(
        string text,
        int    colspan,
        Brush? bg   = null,
        Brush? fg   = null,
        bool   rtl  = false,
        bool   bold = false,
        double size = 10)
    {
        var para = new Paragraph(new Run(text)
        {
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            FontSize   = size,
            Foreground = fg ?? Brushes.Black
        })
        {
            Margin        = new Thickness(0),
            TextAlignment = rtl ? TextAlignment.Center : TextAlignment.Left,
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            LineHeight    = 14
        };

        return new TableCell(para)
        {
            ColumnSpan = colspan,
            Background = bg ?? Brushes.White,
            Padding    = new Thickness(5, 7, 5, 7)
        };
    }

    private static TableCell MakeDataCell(string text, bool center = false)
    {
        var para = new Paragraph(new Run(text) { FontSize = 10 })
        {
            Margin        = new Thickness(0),
            TextAlignment = center ? TextAlignment.Center : TextAlignment.Left,
            LineHeight    = 14
        };

        return new TableCell(para)
        {
            Background = Brushes.White,
            Padding    = new Thickness(5, 9, 5, 9)
        };
    }

    private static string GetArabicDayName(DayOfWeek day) => day switch
    {
        DayOfWeek.Thursday  => "الخميس",
        DayOfWeek.Friday    => "الجمعة",
        DayOfWeek.Saturday  => "السبت",
        DayOfWeek.Sunday    => "الأحد",
        DayOfWeek.Monday    => "الاثنين",
        DayOfWeek.Tuesday   => "الثلاثاء",
        DayOfWeek.Wednesday => "الأربعاء",
        _                   => day.ToString()
    };
}
