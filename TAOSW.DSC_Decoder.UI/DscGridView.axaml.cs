using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TAOSW.DSC_Decoder.UI.Model;

namespace TAOSW.DSC_Decoder.UI;

public partial class DscGridView : UserControl
{
    public DscGridView(DscMessagesViewModel dataModel)
    {
        InitializeComponent(); // Ensure this method is defined in the corresponding XAML file
        DataContext = dataModel;
       
    }

    public DscGridView()
    {
        InitializeComponent(); // Default constructor for design-time or runtime instantiation without a data model
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this); // Load the XAML file associated with this UserControl
    }

}
