using System;
using System.Windows.Input;

namespace SteamCloudSave.Windows;

public class AnonymousCommand : AnonymousCommand<object>
{
    public AnonymousCommand(Action execute)
        : base(p => execute())
    {
    }

    public AnonymousCommand(Action<object> execute)
        : base(execute)
    {
    }

    public AnonymousCommand(Action<object> execute, Predicate<object> canExecute)
        : base(execute, canExecute)
    {
    }
}

public class AnonymousCommand<T> : ICommand
{
    private readonly Action<T> execute;
    private readonly Predicate<T> canExecute;

    public AnonymousCommand(Action<T> execute)
        : this(execute, p => true)
    {
    }

    public AnonymousCommand(Action<T> execute, Predicate<T> canExecute)
    {
        ArgumentNullException.ThrowIfNull(execute);
        ArgumentNullException.ThrowIfNull(canExecute);

        this.execute = execute;
        this.canExecute = canExecute;
    }

    private bool isEnabled = true;
    public bool IsEnabled
    {
        get { return isEnabled; }
        set
        {
            if (isEnabled != value)
            {
                isEnabled = value;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool CanExecute(object parameter)
    {
        if (IsEnabled == false)
            return false;

        return canExecute((T)parameter);
    }

    public void Execute(object parameter)
    {
        execute((T)parameter);
    }

    public event EventHandler CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }
}
