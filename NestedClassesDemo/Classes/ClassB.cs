using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NestedClassesDemo.Classes
{
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
}
