using System.Windows.Input;
namespace Raytracer;

public class RelayCommand( Action<object?> execute, Func<object?, bool>? canExecute ) : ICommand
{
	public event EventHandler? CanExecuteChanged;

	public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke( this, EventArgs.Empty );

	public bool CanExecute( object? parameter ) => canExecute?.Invoke( parameter ) ?? true;

	public void Execute( object? parameter ) => execute.Invoke( parameter );
}