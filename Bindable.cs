using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;


namespace FFBitrateViewer
{
    // Source: https://timoch.com/blog/2013/08/annoyed-with-inotifypropertychange/ (with some modifications)

    /// <summary>
    /// Base class to implement object that can be bound to
    /// </summary>
    public class Bindable : INotifyPropertyChanged
    {
        private readonly Dictionary<string, object?> _properties = new();


        /// <summary>
        /// Gets the value of a property
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        protected T? Get<T>([CallerMemberName] string? name = null)
        {
            Debug.Assert(name != null, "name != null");
            if (_properties.TryGetValue(name, out object? value)) return value == null ? default : (T)value;
            return default;
        }


        /// <summary>
        /// Sets the value of a property
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="name"></param>
        protected void Set<T>(T value, [CallerMemberName] string? name = null)
        {
            Debug.Assert(name != null, "name != null");
            if (Equals(value, Get<T>(name))) return;
            _properties[name] = value;
            OnPropertyChanged(name);
        }


        public event PropertyChangedEventHandler? PropertyChanged;


        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        protected virtual void OnPropertyChanged<T>(Expression<Func<T>> raiser)
        {
            var propertyName = ((MemberExpression)raiser.Body).Member.Name;
            OnPropertyChanged(propertyName);
        }
    }
}
