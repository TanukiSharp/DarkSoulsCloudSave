using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SteamCloudSave.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    protected bool SetValue<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        return true;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        PropertyChanged?.Invoke(this, e);
    }
}
