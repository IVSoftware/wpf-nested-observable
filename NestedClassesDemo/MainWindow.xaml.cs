using NestedClassesDemo.Classes;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
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
}
class MainWindowViewModel : INotifyPropertyChanged
{
    public MainWindowViewModel() 
    {
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
    }
    static int _autoIncrement = 1;
    public ObservableBindableCollection<ClassB> BCollection{ get; } = new ObservableBindableCollection<ClassB>
    {
        new ClassB{C = new ClassC{Name = $"Item C{_autoIncrement++}"} },
        new ClassB{C = new ClassC{Name = $"Item C{_autoIncrement++}"} },
        new ClassB{C = new ClassC{Name = $"Item C{_autoIncrement++}"} },
    };

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

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if(e is ObservableBindablePropertyChangedEventArgs ePlus)
        {             
            if( ePlus.ChangedItem?.GetType() is { } type &&
                type.GetProperty(e.PropertyName) is { } pi &&
                typeof(INotifyPropertyChanged).IsAssignableFrom(pi.PropertyType))
            {

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
