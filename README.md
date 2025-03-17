Since your tag is WPF, I wanted to offer a more robust solution that will tolerate changes to the `C` property in `ClassB` as well as certain edge cases that would break or limit the scheme you show.

- A deeply nested class that has a non-INPC class as its parent.
- A property that is declared as `object` that later is assigned to an instance that implements INPC.
- A property that is intended to lazy initialize as a singleton, that would be inadvertently activated by the discovery shown.
- Nested properties that are themselves of type `ObservableCollection<T>`

___

As an alternative to subclassing `ObservableCollection<T>`, the snippet below (and the GitHub [repo that I used to test this answer]()) employs the `WithNotifyOnDescendants(...)` extension that is a part of the [XBoundObject NuGet package](https://www.nuget.org/packages/IVSoftware.Portable.Xml.Linq.XBoundObject) whose source code can be found [here](https://github.com/IVSoftware/IVSoftware.Portable.Xml.Linq.XBoundObject.git).

Here, the aggregated traffic of all nested `INotifyPropertyChanged` descendants is routed to the designated `PropertyChangedEventHandler` delegate. Note that for the calculation of `SumOfBCost`, we not only have to respond to changes of the value of `C.Cost`, there is also a case where the `ClassB.C` is replaced by a different instance of `ClassC`. This scenario might be contributing to the problems you're describing, because this swap doesn't change the _collection_ of `ClassB` items. Therefore, trying to handle this scenario by responding to `NotifyCollectionChangedAction.Replace` isn't going to work.

```

using IVSoftware.Portable.Xml.Linq.XBoundObject.Modeling;

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

The WPF sample in the [linked repo]() includes a button to replace the `ClassC` instances with new ones, to verify that the `INotifyPropertyChanged` is still subscribed with the new instances. As far as the critical element of testing is concerned, if you browse the repo for [XBoundObject](https://github.com/IVSoftware/IVSoftware.Portable.Xml.Linq.XBoundObject.git) you will see an `MSTest` project with more than a dozen detailed tests that ensure its reliable operation.





[![screenshot][1]][1]


  [1]: https://i.sstatic.net/65g1zRRB.png