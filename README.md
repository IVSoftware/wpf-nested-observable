When the classes are _not_ nested it's often possible to use `BindingList<T>` as a drop in replacement for `ObservableCollection<T>` and use its `ListChangedEvent` checking for `ListChangedType== ListChangedType.ItemChanged`. However, it sometimes behaves quite badly in cases like the one you describe. 

So as a possible alternative I wanted to share a class I wrote when I needed something similar in the hope that this could be something you can use or adapt. The thing is, `ObservableCollection` already has a `PropertyChanged` event, it's just (for a lot of good reasons) not `public`. So we're going to elevate it, and at the same time make a specialized class derived from `PropertyChangedEventArgs` that provides information about the source item that raised the property changed event.
___
```
public class ObservableBindableCollection<T> : ObservableCollection<T>, INotifyPropertyChanged
{
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnCollectionChanged(e);
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if(e.NewItems != null)
                {
                    foreach (var item in e.NewItems.OfType<INotifyPropertyChanged>())
                    {
                        // Remove any existing.
                        item.PropertyChanged -= OnItemPropertyChanged;
                        // Add new.
                        item.PropertyChanged += OnItemPropertyChanged;
                    }
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                break;
        }        
    }
    protected override void ClearItems()
    {
        base.ClearItems();
        foreach (var item in this)
        {
            if (item is INotifyPropertyChanged bindable)
            {
                bindable.PropertyChanged -= OnItemPropertyChanged;
            }
            base.ClearItems();
        }
    }
    public new event PropertyChangedEventHandler? PropertyChanged
    {
        add => base.PropertyChanged += value;
        remove => base.PropertyChanged -= value;
    }
    public virtual void OnItemPropertyChanged(object? changedItem, PropertyChangedEventArgs e)
        => OnPropertyChanged(new ObservableBindablePropertyChangedEventArgs(e.PropertyName, changedItem));
}
```
___

It fires a derivative class of PropertyChangedEventArgs.

```
public class ObservableBindablePropertyChangedEventArgs : PropertyChangedEventArgs
{
    public ObservableBindablePropertyChangedEventArgs(
        string? propertyName,
        object? changedItem
        ) : base(propertyName)
        => ChangedItem =
        (changedItem is INotifyPropertyChanged bindable)
        ? bindable
        : null;
    public INotifyPropertyChanged? ChangedItem { get; }
}
```

___

So, in your case you might do something like this:

```

public ObservableBindableCollection<ClassB> BCollection{ get; } = new ();

BCollection.PropertyChanged += (sender, e) =>
{
    if(e is ObservableBindablePropertyChangedEventArgs ePlus)
    {
        switch (e.PropertyName)
        {
            case nameof(ClassC.Cost):
                SumOfBCost = BCollection.Sum(_ => _.C.Cost);
                break;
        }
    }
};
```



[![wpf MRE][1]][1]


  [1]: https://i.sstatic.net/UwlSDxED.png