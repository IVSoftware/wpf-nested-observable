Since your tag is WPF, I wanted to offer a more robust solution that will tolerate changes to the `C` property in `ClassB` as well as certain edge cases that would break or limit the scheme you show such as:

- A deeply nested class that has a non-INPC class as its parent.
- A property that is declared as `object` that later is assigned to an instance that implements INPC.
- A property that is intended to lazy initialize as a singleton, that would be inadvertently activated by the discovery shown.
- Nested properties that are themselves of type `ObservableCollection<T>`

This would be an alternative to subclassing `ObservableCollection<T>`. In other words, `FullyObservableCollection<T>` is no longer required. This solution employs a lightweight NuGet package for [XBoundObject](https://www.nuget.org/packages/IVSoftware.Portable.Xml.Linq.XBoundObject). But even if you decide to code this yourself, consider browsing the [source code](https://github.com/IVSoftware/IVSoftware.Portable.Xml.Linq.XBoundObject.git) to see how it performs the resursive discovery and keeps track of subscriptions to `PropertyChanged` delegates.

`<PackageReference Include="IVSoftware.Portable.Xml.Linq.XBoundObject" Version="1.3.0" />`

___

**Handling the Nested Property Changes**

In the snippet below, the aggregated traffic of all nested `INotifyPropertyChanged` descendants is routed to the designated `PropertyChangedEventHandler` delegate. Note that for the calculation of `SumOfBCost`, we not only have to respond to changes of the value of `C.Cost`, there is also a case where the `ClassB.C` is replaced by a different instance of `ClassC`. This scenario might be contributing to the problems you're describing, because this swap doesn't change the _collection_ of `ClassB` items. Therefore, trying to handle this scenario by responding to `NotifyCollectionChangedAction.Replace` isn't going to work.

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

The WPF sample in the [linked repo](https://github.com/IVSoftware/wpf-nested-observable.git) includes a button to replace the `ClassC` instances with new ones, to verify that the `INotifyPropertyChanged` is still subscribed with the new instances. As far as the critical element of testing is concerned, if you browse the repo for `XBoundObject` you'll see an `MSTest` project with more than a dozen detailed tests that ensure its reliable operation. [TestClass_Modeling.cs](https://github.com/IVSoftware/IVSoftware.Portable.Xml.Linq.XBoundObject/blob/master/MSTestProject/TestClass_Modeling.cs)

___

***XBoundObject***

The `WithNotifyOnDescendants(...)` leverages the ability of `XBoundObject` to bind delegates and instances to the runtime XML model.

`BCollection = new ObservableCollection<ClassB>.WithNotifyOnDescendants(out XElement model, OnPropertyChanged, onXO: OnXObjectEvent);`

For debugging, the `model` can be inspected in a Text Viewer or XML Viewer at any time to see exactly what's going on in the nested hierarchy below.

```xml
<model name="(Origin)ObservableCollection" instance="[ObservableCollection]" onpc="[OnPC]" context="[ModelingContext]">
  <member name="Count" pi="[Int32]" />
  <model name="(Origin)ClassB" instance="[ClassB]" onpc="[OnPC]">
    <member name="C" pi="[ClassC]" instance="[ClassC]" onpc="[OnPC]">
      <member name="Name" pi="[String]" />
      <member name="Cost" pi="[Int32]" />
      <member name="Currency" pi="[Int32]" />
    </member>
  </model>
  <model name="(Origin)ClassB" instance="[ClassB]" onpc="[OnPC]">
    <member name="C" pi="[ClassC]" instance="[ClassC]" onpc="[OnPC]">
      <member name="Name" pi="[String]" />
      <member name="Cost" pi="[Int32]" />
      <member name="Currency" pi="[Int32]" />
    </member>
  </model>
  <model name="(Origin)ClassB" instance="[ClassB]" onpc="[OnPC]">
    <member name="C" pi="[ClassC]" instance="[ClassC]" onpc="[OnPC]">
      <member name="Name" pi="[String]" />
      <member name="Cost" pi="[Int32]" />
      <member name="Currency" pi="[Int32]" />
    </member>
  </model>
</model>
```
___

##### Subscriptions and Unsubscriptions

The instance and the subscribed `PropertyChangedEventHandler` are registered on the same model node.

`<member name="C" pi="[ClassC]" instance="[ClassC]" onpc="[OnPC]">`

So, for example:

- If this `XElement` is removed from the model in response to changes in `BCollection`, this removal is detected by the `System.Linq.Xml.Changed` event and the bound attributes are employed to unsubscribe the handler.

- If a new instance of `ClassC` is added to the model, the delegate information contained in the root node's `ModelingContext` attribute is used to subscribe the new instance.



[![screenshot][1]][1]


  [1]: https://i.sstatic.net/65g1zRRB.png