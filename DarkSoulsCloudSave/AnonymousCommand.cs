using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SteamCloudSave
{
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
        private Action<T> execute;
        private Predicate<T> canExecute;

        public AnonymousCommand(Action<T> execute)
            : this(execute, p => true)
        {
        }

        public AnonymousCommand(Action<T> execute, Predicate<T> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");
            if (canExecute == null)
                throw new ArgumentNullException("canExecute");

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
}
