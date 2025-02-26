After staring at your question _a lot_, I think I've finally wrapped my head around how you want the recursion to work. So as I understand it we have `ClassC` which implements `INotifyProertyChanged` and the goal is that when `ClassB` instantiates it, it doesn't have to do anything at all in terms of bubbling property changed events up from members.

At the same time, if the `C` property is _changed_ as in replaced in sitio with a new instance, all that recursive binding that we initially set up will now break unless we run discovery again on the replacement object. This is the reason for making the `C` property itself a bindable instance.

```
class ClassB : INotifyPropertyChanged
{
    /// <summary>
    /// If C is replaced, then any new instance and
    /// its descendants will need to be rediscovered.
    /// </summary>
    public ClassC? C
    {
        get => _c;
        set
        {
            if (!Equals(_c, value))
            {
                _c = value;
                OnPropertyChanged();
            }
        }
    }
    ClassC? _c = null;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    public event PropertyChangedEventHandler? PropertyChanged;
}
```

___

**Enumerator**

This means that we need an enumerator extension for Type to get INPC implementers for self and all descendents resursively.

```
public static class Extensions
{
    public static IEnumerable<INotifyPropertyChanged> BindableDescendantsAndSelf(this object item)
    {
        if (item is INotifyPropertyChanged inpc)
        {
            yield return inpc;  // Yield the item itself if it's INPC
        }
        foreach (var pi in item.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(pi =>
                typeof(INotifyPropertyChanged).IsAssignableFrom(pi.PropertyType) &&
                pi.CanRead &&
                pi.GetMethod?.IsStatic != true &&
                pi.GetIndexParameters().Length == 0))
        {
            if (pi.GetValue(item) is INotifyPropertyChanged childINPC)
            {
                foreach (var descendant in childINPC.BindableDescendantsAndSelf())
                {
                    yield return descendant;
                }
            }
        }
    }
}
```

**Respond recursively to both _initial_ and to _changed_ instanced of INPC properties.**

```
public class ObservableBindableCollection<T> : ObservableCollection<T>, INotifyPropertyChanged
{
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnCollectionChanged(e);
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems != null)
                {
                    HashSet<object> visited = new();
                    foreach (var item in e.NewItems.OfType<INotifyPropertyChanged>())
                    {
                        foreach (var bindableInstance in item.BindableDescendantsAndSelf())
                        {
                            if(visited.Add(bindableInstance))
                            {
                                bindableInstance.PropertyChanged -= OnItemPropertyChanged;
                                bindableInstance.PropertyChanged += OnItemPropertyChanged;
                            }
                            else
                            {   /* G T K */
                                // e.g. A property that returns 'this'
                            }
                        }
                    }
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems != null)
                {
                    HashSet<object> visited = new();
                    foreach (var item in e.OldItems.OfType<INotifyPropertyChanged>())
                    {
                        foreach (var bindableInstance in item.BindableDescendantsAndSelf())
                        {
                            if (visited.Add(bindableInstance))
                            {
                                bindableInstance.PropertyChanged -= OnItemPropertyChanged;
                            }
                            else
                            {   /* G T K */
                                // e.g. A property that returns 'this'
                            }
                        }
                    }
                }
                break;
        }
    }

    protected override void ClearItems()
    {
        foreach (var item in this)
        {
            if (item is INotifyPropertyChanged bindable)
            {
                bindable.PropertyChanged -= OnItemPropertyChanged;
            }
        }
        base.ClearItems();
    }

    public new event PropertyChangedEventHandler? PropertyChanged
    {
        add => base.PropertyChanged += value;
        remove => base.PropertyChanged -= value;
    }
    public virtual void OnItemPropertyChanged(object? changedItem, PropertyChangedEventArgs e)
        => OnPropertyChanged(new ObservableBindablePropertyChangedEventArgs(e.PropertyName, changedItem));

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if(e is ObservableBindablePropertyChangedEventArgs ePlus)
        {             
            if( ePlus.ChangedItem?.GetType() is { } type &&
                type.GetProperty(ePlus.PropertyName) is { } pi &&
                typeof(INotifyPropertyChanged).IsAssignableFrom(pi.PropertyType))
            {
                if(pi.GetValue(ePlus.ChangedItem) is { } inpc)
                {
                    HashSet<object> visited = new();
                    foreach (var bindableInstance in inpc.BindableDescendantsAndSelf())
                    {
                        if (visited.Add(bindableInstance))
                        {
                            bindableInstance.PropertyChanged -= OnItemPropertyChanged;
                            bindableInstance.PropertyChanged += OnItemPropertyChanged;
                        }
                        // Refresh
                        foreach(var piRefresh in inpc.GetType().GetProperties().Where(_=>_.CanRead))
                        {
                            OnPropertyChanged(new PropertyChangedEventArgs(piRefresh.Name));
                        }
                    }
                }
            }
        }
        base.OnPropertyChanged(e);
    }
}
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

**USAGE**

Now use as intended.

```
public class ClassA
{
    public ObservableBindableCollection<ClassB> BCollection{ get; } = new ();
    public ClassA() 
    {
        BCollection.PropertyChanged += (sender, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(ClassC.Cost):
                    SumOfBCost = BCollection.Sum(_ => _.C?.Cost ?? 0);
                    break;
            }
        };
    }
}
```
[![screenshot][1]][1]
___

**NOTES**

`ObservableCollection` already has a `PropertyChanged` event, but for some very good reasons it's not `public`. What this version does is elevate that to a public property, and fire a derived event with information about the property instance of the collection whose property changed event is being responded to. If that info is of interest, then handle in in this manner instead;

___

```
BCollection.PropertyChanged += (sender, e) =>
{
    switch (e.PropertyName)
    {
        case nameof(ClassC.Cost):
            SumOfBCost = BCollection.Sum(_ => _.C?.Cost ?? 0);

            if(e is ObservableBindablePropertyChangedEventArgs ePlus)
            {
                // Obtain more information about the property instance.
            }
            break;
    }
};
```


  [1]: https://i.sstatic.net/65g1zRRB.png