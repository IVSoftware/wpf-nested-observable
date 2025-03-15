So as I understand it your example states:

- The `BCollection` contains items of `ClassB`.
- `ClassB` has a member named `C` that is a `ClassC`.
- `ClassC` implements `INotifyPropertyChanged` and in particular will raise this event when `C.Cost` changes.

And finally, we have `ClassA` which contains the `BCollection` and has a `SumOfBCost` that should be updated whenever:

1. `C.Cost` changes
2. `ClassB.C` instance is replaced with a new instance of `ClassC`. (Note that this is 'not' a collection changed event when this occurs!)
3. `BCollection` undergoes changes of `ClassB` items that are added, removed or replaced. (All of which _are_ collection changed events!)

___

As an alternative to subclassing `ObservableCollection<T>`, the snippet below (and the GitHub [repo that I used to test this answer]()) employs the `WithNotifyOnDescendants(...)` extension that is a part of the [XBoundObject NuGet package](https://www.nuget.org/packages/IVSoftware.Portable.Xml.Linq.XBoundObject) whose source code can be found [here](https://github.com/IVSoftware/IVSoftware.Portable.Xml.Linq.XBoundObject.git).

Here, the aggregated traffic of all nested `INotifyPropertyChanged` descendants is routed to the designated `PropertyChangedEventHandler` delegate. Note that for the calculation of `SumOfBCost`, we not only have to respond to changes of the value of `C.Cost`, there is also a case where the `ClassB.C` is replaced by a different instance of `ClassC`. This scenario might be contributing to the problems you're describing, because this swap doesn't change the _collection_ of `ClassB` items. Therefore, trying to handle this scenario by responding to `NotifyCollectionChangedAction.Replace` isn't going to work.

```
class ClassA : INotifyPropertyChanged
{
    public ClassA() 
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
```

___

**Testing Off-List Swaps**

In order to verify that `ClassA` continues to respond to new instances of `ClassC` that are swapped into `ClassB.C` we can devise this simple test.

```
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
    new ClassA DataContext => (ClassA)base.DataContext;
}
```



[![screenshot][1]][1]


  [1]: https://i.sstatic.net/65g1zRRB.png