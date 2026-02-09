using System.Windows.Input;
namespace PortfelStudenta.Behaviors;
public class SwitchToggledBehavior : Behavior<Switch>
{
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(SwitchToggledBehavior));
    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(SwitchToggledBehavior));
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }
    protected override void OnAttachedTo(Switch bindable)
    {
        base.OnAttachedTo(bindable);
        bindable.Toggled += OnSwitchToggled;
    }
    protected override void OnDetachingFrom(Switch bindable)
    {
        base.OnDetachingFrom(bindable);
        bindable.Toggled -= OnSwitchToggled;
    }
    private void OnSwitchToggled(object? sender, ToggledEventArgs e)
    {
        if (Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }
    }
}