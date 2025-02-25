﻿using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NestedClassesDemo.Classes
{
    public class ClassC : INotifyPropertyChanged
    {
        public string Name
        {
            get => _name;
            set
            {
                if (!Equals(_name, value))
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }
        string _name = string.Empty;

        public int Cost
        {
            get => _cost;
            set
            {
                if (!Equals(_cost, value))
                {
                    _cost = value;
                    OnPropertyChanged();
                }
            }
        }
        int _cost = default;
        public int Currency
        {
            get => _currency;
            set
            {
                if (!Equals(_currency, value))
                {
                    _currency = value;
                    OnPropertyChanged();
                }
            }
        }
        int _currency = default;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
