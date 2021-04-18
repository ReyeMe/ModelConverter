namespace ModelConverter.Utilities
{
    using System;
    using System.Windows.Input;

    /// <summary>
    /// Implementation of ICommand that allows use of lambdas in constructor
    /// </summary>
    public class ActionCommand : ICommand
    {
        /// <summary>
        /// Action that should be executed by this command
        /// </summary>
        private Action<object> actionToExecute;

        /// <summary>
        /// Function to validate parameter
        /// </summary>
        private Func<object, bool> validateParameter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionCommand"/> class
        /// </summary>
        /// <param name="action">Action to execute</param>
        /// <param name="validateParameter">Validate action parameter</param>
        public ActionCommand(Action action, Func<object, bool> validateParameter = null)
        {
            this.actionToExecute = new Action<object>(parameter => action());
            this.validateParameter = validateParameter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionCommand"/> class
        /// </summary>
        /// <param name="action">Action to execute</param>
        /// <param name="validateParameter">Validate action parameter</param>
        public ActionCommand(Action<object> action, Func<object, bool> validateParameter = null)
        {
            this.actionToExecute = action;
            this.validateParameter = validateParameter;
        }

        /// <summary>
        /// Can execute status changed event
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Check if command can be executed
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        /// <returns>True if can be executed</returns>
        public bool CanExecute(object parameter)
        {
            bool valid = true;

            if (this.validateParameter != null)
            {
                valid = this.validateParameter(parameter);
            }

            return this.actionToExecute != null && valid;
        }

        /// <summary>
        /// Execute command
        /// </summary>
        /// <param name="parameter">Command parameter</param>
        public void Execute(object parameter)
        {
            this.actionToExecute(parameter);
        }
    }
}