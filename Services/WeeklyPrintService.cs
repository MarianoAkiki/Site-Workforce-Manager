using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Site_Workforce_Manager.ViewModels;

namespace Site_Workforce_Manager.Services;

public static class WeeklyPrintService
{
    private static readonly FontFamily PrintFont = new FontFamily("Arial");
    private static readonly Brush DarkHeaderBrush = new SolidColorBrush(Color.FromRgb(30, 41, 59));
    private static readonly Brush SubHeaderBrush = new SolidColorBrush(Color.FromRgb(241, 245, 249));
    private static readonly Brush DateForegroundBrush = new SolidColorBrush(Color.FromRgb(148, 163, 184));
    private static readonly Thickness CellBorder = new Thickness(0.5);

    public static void PrintWeeklyView(
        string tradeName,
        DateTime weekStart,
        DateTime weekEnd,
        IList<WeeklyWorkerRow> rows)
    {
        var printDialog = new PrintDialog();
        printDialog.PrintTicket.PageOrientation = PageOrientation.Landscape;

        if (printDialog.ShowDialog() != true) return;

        var doc = BuildDocument(tradeName, weekStart, weekEnd, rows, printDialog);

        printDialog.PrintDocument(
            ((IDocumentPaginatorSource)doc).DocumentPaginator,
            $"Weekly Work Entry - {tradeName}");
    }

    private static FlowDocument BuildDocument(
        string tradeName,
        DateTime weekStart,
        DateTime weekEnd,
        IList<WeeklyWorkerRow> rows,
        PrintDialog printDialog)
    {
        var pageWidth = printDialog.PrintableAreaWidth;
        var pageHeight = printDialog.PrintableAreaHeight;
        const double padding = 36;

        var doc = new FlowDocument
        {
            PageWidth = pageWidth,
            PageHeight = pageHeight,
            PagePadding = new Thickness(padding, 28, padding, 28),
            ColumnWidth = pageWidth - padding * 2,
            FontFamily = PrintFont,
            FontSize = 9
        };

        // Title
        var titlePara = new Paragraph { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 10) };
        titlePara.Inlines.Add(new Run($"سجل الحضور الأسبوعي  —  {tradeName}")
        {
            FontSize = 12,
            FontWeight = FontWeights.Bold
        });
        titlePara.Inlines.Add(new LineBreak());
        titlePara.Inlines.Add(new Run($"{weekStart:dd/MM/yyyy}   —   {weekEnd:dd/MM/yyyy}")
        {
            FontSize = 9,
            Foreground = Brushes.DimGray
        });
        doc.Blocks.Add(titlePara);

        doc.Blocks.Add(BuildTable(weekStart, rows));

        return doc;
    }

    private static Table BuildTable(DateTime weekStart, IList<WeeklyWorkerRow> rows)
    {
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1),
            FontFamily = PrintFont
        };

        // Column 0: worker name
        table.Columns.Add(new TableColumn { Width = new GridLength(135) });
        // Columns 1-14: 7 days × (hours col + site col)
        for (int i = 0; i < 7; i++)
        {
            table.Columns.Add(new TableColumn { Width = new GridLength(42) }); // hours
            table.Columns.Add(new TableColumn { Width = new GridLength(66) }); // site
        }

        // ── Header row group ─────────────────────────────────────
        var headerGroup = new TableRowGroup();

        // Row 1: Worker label | day name per day (each spans 2 cols)
        var dayRow = new TableRow();
        dayRow.Cells.Add(MakeCell("العامل", colspan: 1,
            bg: DarkHeaderBrush, fg: Brushes.White, rtl: true, bold: true, size: 9));

        for (int d = 0; d < 7; d++)
        {
            var date = weekStart.AddDays(d);
            var dayCell = new TableCell
            {
                ColumnSpan = 2,
                Background = DarkHeaderBrush,
                BorderBrush = Brushes.Black,
                BorderThickness = CellBorder,
                Padding = new Thickness(3, 4, 3, 4)
            };
            var dayPara = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0),
                LineHeight = 13,
                FlowDirection = FlowDirection.RightToLeft
            };
            dayPara.Inlines.Add(new Run(GetArabicDayName(date.DayOfWeek))
            {
                FontWeight = FontWeights.Bold,
                FontSize = 9,
                Foreground = Brushes.White
            });
            dayPara.Inlines.Add(new LineBreak());
            dayPara.Inlines.Add(new Run(date.ToString("dd/MM"))
            {
                FontSize = 8,
                Foreground = DateForegroundBrush
            });
            dayCell.Blocks.Add(dayPara);
            dayRow.Cells.Add(dayCell);
        }
        headerGroup.Rows.Add(dayRow);

        // Row 2: empty | ساعات | الموقع × 7
        var subRow = new TableRow();
        subRow.Cells.Add(MakeCell(string.Empty, colspan: 1, bg: SubHeaderBrush));
        for (int d = 0; d < 7; d++)
        {
            subRow.Cells.Add(MakeCell("ساعات", colspan: 1,
                bg: SubHeaderBrush, rtl: true, bold: true, size: 7));
            subRow.Cells.Add(MakeCell("الموقع", colspan: 1,
                bg: SubHeaderBrush, rtl: true, bold: true, size: 7));
        }
        headerGroup.Rows.Add(subRow);
        table.RowGroups.Add(headerGroup);

        // ── Data row group ────────────────────────────────────────
        var dataGroup = new TableRowGroup();

        foreach (var workerRow in rows)
        {
            var tr = new TableRow();
            tr.Cells.Add(MakeCell(workerRow.WorkerName, colspan: 1));
            foreach (var cell in workerRow.Cells)
            {
                tr.Cells.Add(MakeDataCell(cell.DurationHoursText, center: true));
                tr.Cells.Add(MakeDataCell(cell.SelectedConstructionSiteOption?.Name ?? string.Empty));
            }
            dataGroup.Rows.Add(tr);
        }

        // Extra blank rows for manual additions
        for (int i = 0; i < 3; i++)
        {
            var tr = new TableRow();
            tr.Cells.Add(MakeDataCell(string.Empty));
            for (int d = 0; d < 14; d++)
                tr.Cells.Add(MakeDataCell(string.Empty));
            dataGroup.Rows.Add(tr);
        }

        table.RowGroups.Add(dataGroup);
        return table;
    }

    private static TableCell MakeCell(
        string text,
        int colspan,
        Brush? bg = null,
        Brush? fg = null,
        bool rtl = false,
        bool bold = false,
        double size = 8.5)
    {
        var para = new Paragraph(new Run(text)
        {
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            FontSize = size,
            Foreground = fg ?? Brushes.Black
        })
        {
            Margin = new Thickness(0),
            TextAlignment = rtl ? TextAlignment.Center : TextAlignment.Left,
            FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            LineHeight = 12
        };

        return new TableCell(para)
        {
            ColumnSpan = colspan,
            BorderBrush = Brushes.Black,
            BorderThickness = CellBorder,
            Background = bg ?? Brushes.White,
            Padding = new Thickness(3, 4, 3, 4)
        };
    }

    private static TableCell MakeDataCell(string text, bool center = false)
    {
        var para = new Paragraph(new Run(text) { FontSize = 8.5 })
        {
            Margin = new Thickness(0),
            TextAlignment = center ? TextAlignment.Center : TextAlignment.Left,
            LineHeight = 12
        };

        return new TableCell(para)
        {
            BorderBrush = Brushes.Black,
            BorderThickness = CellBorder,
            Padding = new Thickness(3, 5, 3, 5)
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
        _ => day.ToString()
    };
}
