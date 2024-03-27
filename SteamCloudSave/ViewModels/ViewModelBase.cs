using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ReactiveUI;

namespace SteamCloudSave.ViewModels;

public class ViewModelBase : ReactiveObject
{
    protected bool SetValue<T>(ref T? field, T? value, [CallerMemberName] string? propertyName = null)
    {
        ArgumentNullException.ThrowIfNull(propertyName);

        if (EqualityComparer<T?>.Default.Equals(field, value))
        {
            return false;
        }

        this.RaisePropertyChanging(propertyName);
        field = value;
        this.RaisePropertyChanged(propertyName);

        return true;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        this.RaisePropertyChanged(propertyName);
    }
}
