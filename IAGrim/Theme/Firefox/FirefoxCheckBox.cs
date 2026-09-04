using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using IAGrim.Services.ItemStats;
using Avalonia.LogicalTree;

namespace IAGrim.Theme;

public class FirefoxCheckBox : CheckBox
{
    public static readonly StyledProperty<bool> SupportsNumericFilterProperty = AvaloniaProperty.Register<FirefoxCheckBox, bool>(nameof(SupportsNumericFilter));
    public static readonly DirectProperty<FirefoxCheckBox, bool> HasFilterProperty = AvaloniaProperty.RegisterDirect<FirefoxCheckBox, bool>(nameof(HasFilter), o => o.HasFilter);

    /// <summary>
    /// When true, a small filter (funnel) button is drawn on the right edge of the checkbox while it is
    /// checked and hovered, letting the user attach a numeric "stat &gt;= n" comparison to this stat.
    /// Opt-in per panel; defaults to false so non-stat checkboxes (e.g. Classes) are unaffected.
    /// </summary>
    public bool SupportsNumericFilter
    {
        get => GetValue(SupportsNumericFilterProperty);
        set => SetValue(SupportsNumericFilterProperty, value);
    }
    /// <summary>The comparison operator the user picked, or null when no numeric filter is set.</summary>
    public StatValueFilter.Op? FilterOperator { get; private set; }
    /// <summary>The threshold the user typed, valid only when <see cref="FilterOperator"/> is set.</summary>
    public double FilterThreshold { get; private set; }
    /// <summary>True once a numeric filter has been configured for this checkbox.</summary>
    public bool HasFilter => FilterOperator != null;
    /// <summary>Raised when the numeric filter is set or removed via the filter dialog (not on cancel).</summary>
    public event EventHandler? FilterChanged;

    private TextBox? _filterTextBox;
    private ComboBox? _filterOperatorComboBox;

    // Prevent changes made programmatically from triggering
    // FilterChanged while the control is being updated.
    private bool _updatingFilter;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsCheckedProperty)
        {
            OnCheckedChanged();
        }
        if (change.Property == SupportsNumericFilterProperty)
        {
            UpdateFilterVisibility();
        }
    }

    public void ClearFilter()
    {
        FilterOperator = null;
        RaisePropertyChanged(HasFilterProperty, true, false);
        FilterThreshold = 0;

        _updatingFilter = true;

        if (_filterTextBox != null)
            _filterTextBox.Text = string.Empty;

        if (_filterOperatorComboBox != null)
            _filterOperatorComboBox.SelectedIndex = 1; // >=

        _updatingFilter = false;

        FilterChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Turns on the numeric-filter button for every FirefoxCheckBox under <paramref name="root"/>.</summary>
    public static void EnableNumericFilters(Control root)
    {
        foreach (var child in root.GetLogicalChildren())
        {
            if (child is FirefoxCheckBox cb)
                cb.SupportsNumericFilter = true;

            if (child is Control control)
                EnableNumericFilters(control);
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _filterTextBox = e.NameScope.Find<TextBox>("PART_FilterValue");
        _filterOperatorComboBox = e.NameScope.Find<ComboBox>("PART_FilterOperator");
        if (_filterTextBox != null)
        {
            _filterTextBox.TextChanged += FilterTextBox_TextChanged;
        }
        if (_filterOperatorComboBox != null)
        {
            _filterOperatorComboBox.SelectionChanged +=
                FilterOperator_SelectionChanged;
        }
        if (HasFilter)
        {
            _updatingFilter = true;

            if (_filterTextBox != null)
            {
                _filterTextBox.Text = FilterThreshold.ToString(System.Globalization.CultureInfo.CurrentCulture);
            }

            if (_filterOperatorComboBox != null)
            {
                _filterOperatorComboBox.SelectedIndex = OperatorToIndex(FilterOperator!.Value);
            }
            _updatingFilter = false;
        }
        UpdateFilterVisibility();
    }

    private void OnCheckedChanged()
    {
        // Unchecking the stat removes any numeric filter: "stat >= n" makes no sense without the stat.
        if (IsChecked != true && HasFilter)
        {
            FilterOperator = null;
            RaisePropertyChanged(HasFilterProperty, true, false);
            FilterThreshold = 0;

            _updatingFilter = true;

            if (_filterTextBox != null)
                _filterTextBox.Text = string.Empty;

            _updatingFilter = false;

            FilterChanged?.Invoke(this, EventArgs.Empty);
        }
        UpdateFilterVisibility();
    }

    private void UpdateFilterVisibility()
    {
        if (_filterTextBox == null || _filterOperatorComboBox == null)
            return;

        bool visible = SupportsNumericFilter && IsChecked == true;
        _filterTextBox.IsVisible = visible;
        _filterOperatorComboBox.IsVisible = visible;
    }

    private void FilterTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_updatingFilter)
            return;

        UpdateNumericFilter();
    }

    private void FilterOperator_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_updatingFilter)
            return;

        UpdateNumericFilter();
    }

    private void UpdateNumericFilter()
    {
        if (_filterTextBox == null || _filterOperatorComboBox == null)
            return;

        if (string.IsNullOrWhiteSpace(_filterTextBox.Text))
        {
            if (HasFilter)
            {
                FilterOperator = null;
                RaisePropertyChanged(HasFilterProperty, true, false);
                FilterThreshold = 0;

                FilterChanged?.Invoke(this, EventArgs.Empty);
            }

            return;
        }

        if (!double.TryParse(_filterTextBox.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out double value))
        {
            return;
        }

        StatValueFilter.Op? op = IndexToOperator(_filterOperatorComboBox.SelectedIndex);

        if (op == null)
            return;

        bool hadFilter = HasFilter;

        FilterOperator = op.Value;
        RaisePropertyChanged(HasFilterProperty, hadFilter, true);
        FilterThreshold = value;

        FilterChanged?.Invoke(this, EventArgs.Empty);
    }

    private static int OperatorToIndex(StatValueFilter.Op op)
    {
        return op switch
        {
            StatValueFilter.Op.GreaterThan => 0,
            StatValueFilter.Op.GreaterOrEqual => 1,
            StatValueFilter.Op.Equal => 2,
            StatValueFilter.Op.LessOrEqual => 3,
            StatValueFilter.Op.LessThan => 4,
            _ => 1
        };
    }

    private static StatValueFilter.Op? IndexToOperator(int index)
    {
        return index switch
        {
            0 => StatValueFilter.Op.GreaterThan,
            1 => StatValueFilter.Op.GreaterOrEqual,
            2 => StatValueFilter.Op.Equal,
            3 => StatValueFilter.Op.LessOrEqual,
            4 => StatValueFilter.Op.LessThan,
            _ => null
        };
    }

    public void SetNumericFilter(StatValueFilter.Op op, double threshold)
    {
        bool hadFilter = HasFilter;

        FilterOperator = op;
        RaisePropertyChanged(HasFilterProperty, hadFilter, true);
        FilterThreshold = threshold;

        _updatingFilter = true;

        if (_filterOperatorComboBox != null)
        {
            _filterOperatorComboBox.SelectedIndex = OperatorToIndex(op);
        }

        if (_filterTextBox != null)
        {
            _filterTextBox.Text = threshold.ToString(System.Globalization.CultureInfo.CurrentCulture);
        }

        _updatingFilter = false;

        FilterChanged?.Invoke(this, EventArgs.Empty);
    }

    public void NotifyFilterChanged()
    {
        FilterChanged?.Invoke(this, EventArgs.Empty);
    }
}
