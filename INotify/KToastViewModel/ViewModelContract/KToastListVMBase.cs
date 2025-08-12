using INotify.KToastView.Model;
using INotify.KToastView.View.ViewContract;
using INotifyLibrary.Model;
using INotifyLibrary.Model.Entity;
using INotifyLibrary.Util.Enums;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WinCommon.Util;

namespace INotify.KToastViewModel.ViewModelContract
{
    public abstract class KToastListVMBase : KToastViewModelBase
    {
        public IKToastListView View { get; set; }

        private ObservableCollection<KToastVObj> _kToastNotifications;
        private bool _isLoading;
        private bool _isLoadingMore;
        private int _currentPage = 0;
        private bool _hasMoreData = true;
        private const int PAGE_SIZE = 50;
        private string _totalCountText = "0 notifications";

        // Filter properties
        private string _searchKeyword = "";
        private string _selectedAppFilter = "";
        private DateTimeOffset? _selectedDate;
        private DateTimeOffset? _fromDate;
        private DateTimeOffset? _toDate;
        private bool _isFilterPanelVisible = false;
        private ObservableCollection<KPackageProfile> _availableApps = new();

        /// <summary>
        /// Collection of notifications displayed in incremental order
        /// </summary>
        public ObservableCollection<KToastVObj> KToastNotifications
        {
            get
            {
                if (_kToastNotifications == null)
                {
                    _kToastNotifications = new ObservableCollection<KToastVObj>();
                }
                return _kToastNotifications;
            }
            set
            {
                _kToastNotifications = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether initial data is currently being loaded
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set 
            { 
                SetIfDifferent(ref _isLoading, value);
                OnPropertyChanged(nameof(ShowLoadingIndicator));
            }
        }

        /// <summary>
        /// Whether additional data is being loaded (pagination)
        /// </summary>
        public bool IsLoadingMore
        {
            get => _isLoadingMore;
            set 
            { 
                SetIfDifferent(ref _isLoadingMore, value);
                OnPropertyChanged(nameof(ShowLoadingMoreIndicator));
            }
        }

        /// <summary>
        /// Whether to show the main loading indicator
        /// </summary>
        public bool ShowLoadingIndicator => IsLoading && KToastNotifications.Count == 0;

        /// <summary>
        /// Whether to show the loading more indicator at the bottom
        /// </summary>
        public bool ShowLoadingMoreIndicator => IsLoadingMore;

        /// <summary>
        /// Whether there is more data to load
        /// </summary>
        public bool HasMoreData
        {
            get => _hasMoreData;
            set => SetIfDifferent(ref _hasMoreData, value);
        }

        /// <summary>
        /// Current page for pagination
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set => SetIfDifferent(ref _currentPage, value);
        }

        /// <summary>
        /// Display text for total count
        /// </summary>
        public string TotalCountText
        {
            get => _totalCountText;
            set => SetIfDifferent(ref _totalCountText, value);
        }

        /// <summary>
        /// Total count of notifications currently displayed
        /// </summary>
        public int TotalCount => KToastNotifications?.Count ?? 0;

        #region Filter Properties

        /// <summary>
        /// Search keyword for filtering notifications
        /// </summary>
        public string SearchKeyword
        {
            get => _searchKeyword;
            set => SetIfDifferent(ref _searchKeyword, value);
        }

        /// <summary>
        /// Selected app for filtering notifications
        /// </summary>
        public string SelectedAppFilter
        {
            get => _selectedAppFilter;
            set => SetIfDifferent(ref _selectedAppFilter, value);
        }

        /// <summary>
        /// Selected specific date for filtering
        /// </summary>
        public DateTimeOffset? SelectedDate
        {
            get => _selectedDate;
            set => SetIfDifferent(ref _selectedDate, value);
        }

        /// <summary>
        /// From date for date range filtering
        /// </summary>
        public DateTimeOffset? FromDate
        {
            get => _fromDate;
            set => SetIfDifferent(ref _fromDate, value);
        }

        /// <summary>
        /// To date for date range filtering
        /// </summary>
        public DateTimeOffset? ToDate
        {
            get => _toDate;
            set => SetIfDifferent(ref _toDate, value);
        }

        /// <summary>
        /// Whether the filter panel is visible
        /// </summary>
        public bool IsFilterPanelVisible
        {
            get => _isFilterPanelVisible;
            set
            {
                _isFilterPanelVisible = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Available apps for filtering
        /// </summary>
        public ObservableCollection<KPackageProfile> AvailableApps
        {
            get => _availableApps;
            set => SetIfDifferent(ref _availableApps, value);
        }

        /// <summary>
        /// Whether any filters are active
        /// </summary>
        public bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(SearchKeyword) ||
            !string.IsNullOrWhiteSpace(SelectedAppFilter) ||
            SelectedDate.HasValue ||
            FromDate.HasValue ||
            ToDate.HasValue;

        #endregion

        #region Commands

        /// <summary>
        /// Command to refresh notifications
        /// </summary>
        public ICommand RefreshCommand { get; protected set; }

        /// <summary>
        /// Command to load more notifications
        /// </summary>
        public ICommand LoadMoreCommand { get; protected set; }

        /// <summary>
        /// Command to apply filters
        /// </summary>
        public ICommand ApplyFiltersCommand { get; protected set; }

        /// <summary>
        /// Command to clear all filters
        /// </summary>
        public ICommand ClearFiltersCommand { get; protected set; }

        /// <summary>
        /// Command to toggle filter panel visibility
        /// </summary>
        public ICommand ToggleFilterPanelCommand { get; protected set; }

        /// <summary>
        /// Command to search notifications
        /// </summary>
        public ICommand SearchCommand { get; protected set; }

        #endregion

        #region Abstract Methods
        /// <summary>
        /// Load initial notifications (first page)
        /// </summary>
        public abstract void LoadInitialNotifications();

        /// <summary>
        /// Load next page of notifications
        /// </summary>
        public abstract void LoadMoreNotifications();

        /// <summary>
        /// Refresh notifications (reload from beginning)
        /// </summary>
        public abstract void RefreshNotifications();

        /// <summary>
        /// Add a single notification to the collection
        /// </summary>
        public abstract void AddNotification(KToastVObj notification);

        /// <summary>
        /// Handle scroll to bottom event for infinite scrolling
        /// </summary>
        public abstract void OnScrollToBottom();

        /// <summary>
        /// Apply current filters to notifications
        /// </summary>
        public abstract void ApplyFilters();

        /// <summary>
        /// Clear all active filters
        /// </summary>
        public abstract void ClearFilters();

        /// <summary>
        /// Load available apps for filtering
        /// </summary>
        public abstract void LoadAvailableApps();
        #endregion

        #region Helper Methods
        /// <summary>
        /// Update the total count text display
        /// </summary>
        protected void UpdateTotalCountText()
        {
            var baseText = TotalCount switch
            {
                0 => "No notifications",
                1 => "1 notification",
                _ => $"{TotalCount} notifications"
            };

            if (HasActiveFilters)
            {
                baseText += " (filtered)";
            }

            TotalCountText = baseText;
        }

        /// <summary>
        /// Reset pagination state
        /// </summary>
        protected void ResetPagination()
        {
            CurrentPage = 0;
            HasMoreData = true;
        }

        /// <summary>
        /// Get pagination parameters for current page
        /// </summary>
        protected (int skip, int take) GetPaginationParams()
        {
            return (CurrentPage * PAGE_SIZE, PAGE_SIZE);
        }

        /// <summary>
        /// Toggle filter panel visibility
        /// </summary>
        protected void ToggleFilterPanel()
        {
            IsFilterPanelVisible = !IsFilterPanelVisible;
        }

        /// <summary>
        /// Clear all filter values
        /// </summary>
        protected void ClearFilterValues()
        {
            SearchKeyword = "";
            SelectedAppFilter = "";
            SelectedDate = null;
            FromDate = null;
            ToDate = null;
        }
        #endregion

        public KToastListVMBase()
        {
            RefreshCommand = new RelayCommand(RefreshNotifications);
            LoadMoreCommand = new RelayCommand(LoadMoreNotifications, () => HasMoreData && !IsLoadingMore);
            ApplyFiltersCommand = new RelayCommand(ApplyFilters);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            ToggleFilterPanelCommand = new RelayCommand(ToggleFilterPanel);
            SearchCommand = new RelayCommand(ApplyFilters);
        }

        protected void HandlePropertyChanged(string propertyName)
        {
            // Update dependent properties when KToastNotifications changes
            if (propertyName == nameof(KToastNotifications))
            {
                UpdateTotalCountText();
                OnPropertyChanged(nameof(TotalCount));
            }
            
            // Update filter-related properties
            if (propertyName == nameof(SearchKeyword) || 
                propertyName == nameof(SelectedAppFilter) || 
                propertyName == nameof(SelectedDate) || 
                propertyName == nameof(FromDate) || 
                propertyName == nameof(ToDate))
            {
                OnPropertyChanged(nameof(HasActiveFilters));
                UpdateTotalCountText();
            }
        }
    }

    /// <summary>
    /// Simple RelayCommand implementation for commands
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
