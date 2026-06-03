using System;
using UnityEngine.UIElements;

public class RelayCommand
{
    private readonly Action execute;
    private readonly Func<bool> canExecute;

    public RelayCommand(Action execute, Func<bool> canExecute = null)
    {
        this.execute = execute;
        this.canExecute = canExecute;
    }

    public void Execute() => execute?.Invoke();
    public bool CanExecute() => canExecute == null || canExecute();
}

public class RelayCommand<T>
{
    private readonly Action<T> execute;
    public RelayCommand(Action<T> execute) => this.execute = execute;
    public void Execute(T param) => execute?.Invoke(param);
}