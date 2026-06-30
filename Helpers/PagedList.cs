using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Site_Workforce_Manager.Helpers;

public static class PageSizeOptions
{
    public static readonly int[] Values = { 25, 50, 100 };
}

public class PagedList<T> : ObservableObject
{
    private List<T> source = new();
    private int pageSize;
    private int currentPage = 1;
    private int totalPages = 1;
    private int totalCount;

    public PagedList(int pageSize = 25)
    {
        this.pageSize = pageSize;
        PreviousPageCommand = new RelayCommand(() => CurrentPage--, () => CanGoPrevious);
        NextPageCommand = new RelayCommand(() => CurrentPage++, () => CanGoNext);
    }

    public ObservableCollection<T> Items { get; } = new();

    public int PageSize
    {
        get => pageSize;
        set
        {
            if (value <= 0 || value == pageSize)
                return;

            pageSize = value;
            OnPropertyChanged();
            RecomputePaging();
        }
    }

    public int CurrentPage
    {
        get => currentPage;
        private set
        {
            if (SetProperty(ref currentPage, value))
                OnPageChanged();
        }
    }

    public int TotalPages
    {
        get => totalPages;
        private set => SetProperty(ref totalPages, value);
    }

    public int TotalCount
    {
        get => totalCount;
        private set
        {
            if (SetProperty(ref totalCount, value))
                OnPropertyChanged(nameof(PageInfoText));
        }
    }

    public bool CanGoPrevious => CurrentPage > 1;
    public bool CanGoNext => CurrentPage < TotalPages;

    public string PageInfoText => TotalCount == 0
        ? "No results"
        : $"Page {CurrentPage} of {TotalPages} ({TotalCount} total)";

    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }

    public void SetSource(IEnumerable<T> items)
    {
        source = items.ToList();
        RecomputePaging();
    }

    private void RecomputePaging()
    {
        TotalCount = source.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)pageSize));

        if (currentPage > TotalPages)
            CurrentPage = TotalPages;
        else
            RefreshPage();

        RaiseAll();
    }

    private void OnPageChanged()
    {
        RefreshPage();
        RaiseAll();
    }

    private void RaiseAll()
    {
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(PageInfoText));
        (PreviousPageCommand as RelayCommand)?.NotifyCanExecuteChanged();
        (NextPageCommand as RelayCommand)?.NotifyCanExecuteChanged();
    }

    private void RefreshPage()
    {
        Items.Clear();
        foreach (var item in source.Skip((currentPage - 1) * pageSize).Take(pageSize))
            Items.Add(item);
    }
}
