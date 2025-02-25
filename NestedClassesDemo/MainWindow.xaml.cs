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
                        localEnsureSubscribeItem(item, visited);
                        void localEnsureSubscribeItem(INotifyPropertyChanged item, HashSet<object> visited)
                        {
                            if (visited.Contains(item)) return;  // e.g. a property that points to `this`
                            visited.Add(item);
                            item.PropertyChanged += OnItemPropertyChanged;
                            foreach(
                                var pi in 
                                item
                                .GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                .Where(_ =>
                                    typeof(INotifyPropertyChanged).IsAssignableFrom(_.PropertyType) &&
                                    _.CanRead &&
                                    _.GetMethod?.IsStatic != true &&
                                    _.GetIndexParameters().Length == 0))
                            {
                                if (pi.GetValue(item) is INotifyPropertyChanged childINPC)
                                {
                                    localEnsureSubscribeItem(childINPC, visited);
                                }
                            }
                        }
                    }
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                break;
        }
        void localUnsubscribeItem(INotifyPropertyChanged item)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
            item.PropertyChanged += OnItemPropertyChanged;
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
public class ObservableBindablePropertyChangedEventArgs : PropertyChangedEventArgs
{
    public ObservableBindablePropertyChangedEventArgs(
        string? propertyName,
        object? changedItem
        ) : base(propertyName)
    {
        ChangedItem =
            (changedItem is INotifyPropertyChanged bindable)
            ? bindable
            : null;
        OriginalSenders.Add(changedItem);
    }
    public INotifyPropertyChanged? ChangedItem { get; }

    // The original sender is at the top of this list.
    public List<object?> OriginalSenders { get; } = new ();
}

public static class Extensions
{
    public static IEnumerable<PropertyInfo> GetINotifyPropertyChangedProperties(this Type type, HashSet<Type>? visitedTypes = null)
    {
        if (visitedTypes == null) visitedTypes = new HashSet<Type>();

        if (!visitedTypes.Add(type)) yield break;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (typeof(INotifyPropertyChanged).IsAssignableFrom(property.PropertyType))
            {
                yield return property;
            }
            foreach (var childProperty in property.PropertyType.GetINotifyPropertyChangedProperties(visitedTypes))
            {
                yield return childProperty;
            }
        }
    }
}
