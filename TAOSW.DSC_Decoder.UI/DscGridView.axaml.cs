using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using TAOSW.DSC_Decoder.Core.Domain;
using TAOSW.DSC_Decoder.UI.Model;

namespace TAOSW.DSC_Decoder.UI;

public partial class DscGridView : UserControl
{
    private DataGrid? _dataGrid;
    private DscMessagesViewModel? _viewModel;

    public DscGridView(DscMessagesViewModel dataModel)
    {
        InitializeComponent();
        DataContext = dataModel;
        _viewModel = dataModel;
        SetupDataGridEventHandlers();
    }

    public DscGridView()
    {
        InitializeComponent();
        SetupDataGridEventHandlers();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _dataGrid = this.FindControl<DataGrid>("MessagesDataGrid");
    }

    private void SetupDataGridEventHandlers()
    {
        // Setup any additional event handlers if needed
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        if (DataContext is DscMessagesViewModel viewModel)
        {
            _viewModel = viewModel;
            // Subscribe to collection changes to update row styles when new messages are added
            viewModel.Messages.CollectionChanged += (s, args) =>
            {
                // Force refresh of the data grid when new items are added
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    // The Category column template will automatically update with new colors
                    // when new messages are added to the collection
                });
            };
        }
    }

    private static SolidColorBrush GetCategoryBrush(CategoryOfCall category)
    {
        return category switch
        {
            CategoryOfCall.Distress => new SolidColorBrush(Color.FromRgb(255, 102, 102)), // Light red
            CategoryOfCall.Urgency => new SolidColorBrush(Color.FromRgb(255, 165, 0)),   // Orange
            CategoryOfCall.Safety => new SolidColorBrush(Color.FromRgb(255, 255, 102)),  // Light yellow
            CategoryOfCall.Routine => new SolidColorBrush(Color.FromRgb(144, 238, 144)), // Light green
            CategoryOfCall.Error => new SolidColorBrush(Color.FromRgb(255, 182, 193)),   // Light pink
            _ => new SolidColorBrush(Colors.White)
        };
    }
}
