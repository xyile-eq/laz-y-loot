using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LazLootIni
{
    public class InlineCommand : ICommand
    {
        public InlineCommand(Action<object> commandAction)
        {
            CommandAction = commandAction;
        }

        public Action<object> CommandAction { get; set; }

        public event EventHandler CanExecuteChanged = (o, e) => { };
        public event EventHandler CommandExecuted;

        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            CommandAction(parameter);
            CommandExecuted?.Invoke(this, new EventArgs());
        }
    }
}
