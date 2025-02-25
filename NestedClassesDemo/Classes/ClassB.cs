using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NestedClassesDemo.Classes
{
    class ClassB : INotifyPropertyChanged
    {
        public ClassC C
        {
            get
            {
                if (_c is null)
                {
                    _c = new ClassC();
                    _c.PropertyChanged += OnPropertyChangedC;
                }
                return _c;
            }
            set
            {
                if(value != null)
                {
                    if(!Equals(_c, value))
                    {
                        C.PropertyChanged -= OnPropertyChangedC;
                        _c = value;
                        C.PropertyChanged += OnPropertyChangedC;
                    }
                }
            }
        }
        ClassC? _c = default;

        void OnPropertyChangedC(object? sender, PropertyChangedEventArgs e)
        {
            if(!(e is ObservableBindablePropertyChangedEventArgs))
            {
                e = new ObservableBindablePropertyChangedEventArgs(e.PropertyName, sender);
            }
            PropertyChanged?.Invoke(this, e);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
