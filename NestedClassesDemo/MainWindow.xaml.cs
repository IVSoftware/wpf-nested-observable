using IVSoftware.Portable.Xml.Linq.XBoundObject.Modeling;
using NestedClassesDemo.Classes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace NestedClassesDemo;
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        dataGridClassC.AutoGeneratingColumn += (sender, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(ClassC.Name):
                    e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
                    break;
            }
        };
    }
    /// <summary>
    /// Replace the C objects and make sure the new ones are still responsive.
    /// </summary>
    private void OnTestReplaceCObjects(object sender, RoutedEventArgs e)
    {
        foreach(ClassB classB in DataContext.BCollection)
        {
            classB.C = new ClassC { Name = classB.C?.Name?.Replace("Item C", "Replace C") ?? "Error" };
        }
    }
    new MainWindowViewModel DataContext => (MainWindowViewModel)base.DataContext;
}
class MainWindowViewModel : INotifyPropertyChanged
{
    public MainWindowViewModel() 
    {
        BCollection = new ObservableCollection<ClassB>
        {
            new ClassB{C = new ClassC{Name = $"Item C{_autoIncrement++}"} },
            new ClassB{C = new ClassC{Name = $"Item C{_autoIncrement++}"} },
            new ClassB{C = new ClassC{Name = $"Item C{_autoIncrement++}"} },
        }.WithNotifyOnDescendants(OnPropertyChanged);
    }
    public ObservableCollection<ClassB> BCollection { get; }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ClassC.Cost):
            case nameof(ClassB.C):
                SumOfBCost = BCollection.Sum(_ => _.C?.Cost ?? 0);
                break;
            default:
                break;
        }
    }

    static int _autoIncrement = 1;

    public int SumOfBCost
    {
        get => _sumOfBCost;
        set
        {
            if (!Equals(_sumOfBCost, value))
            {
                _sumOfBCost = value;
                OnPropertyChanged();
            }
        }
    }
    int _sumOfBCost = default;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    public event PropertyChangedEventHandler? PropertyChanged;
}
