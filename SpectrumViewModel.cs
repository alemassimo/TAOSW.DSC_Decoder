public class SpectrumViewModel : INotifyPropertyChanged
{
    public ObservableCollection<SpectrumDataPoint> SpectrumData { get; set; }
    public string Title { get; set; }
    
    // Metodi per gestire i dati dello spettro
}