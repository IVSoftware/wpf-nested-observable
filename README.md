After staring at your question _a lot_, I think I've finally wrapped my head around how you want the recursion to work. So as I understand it we have `ClassC` which implements `INotifyProertyChanged` and the goal is that when `ClassB` instantiates it, it doesn't have to do anything at all in terms of bubbling property changed events up from members.

At the same time, if the `C` property is set to a new instance, all that recursive binding that we initially set up will now break unless we run discovery again on the replacement object. This is the reason for making the `C` property itself a bindable instance.

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
