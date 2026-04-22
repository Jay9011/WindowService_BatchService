using System.Windows;
using System.Windows.Controls;

namespace SettingsUI.Behaviors;

/// <summary>
/// Attached property that enables two-way binding of <see cref="PasswordBox.Password"/>.
/// PasswordBox intentionally does not expose a bindable DependencyProperty; this
/// behavior bridges to a ViewModel string while the actual value remains only in
/// memory on the PasswordBox until the ViewModel reads it.
///
/// Usage: &lt;PasswordBox behaviors:PasswordBoxBindingBehavior.BoundPassword="{Binding Password, Mode=TwoWay}" /&gt;
/// </summary>
public static class PasswordBoxBindingBehavior
{
    /// <summary>
    /// Attached property that enables two-way binding of <see cref="PasswordBox.Password"/>.
    /// (Register Attached Property)
    /// Bind the OnBoundPasswordChanged event to FrameworkPropertyMetadata.
    /// </summary>
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached(
            "BoundPassword",
            typeof(string),
            typeof(PasswordBoxBindingBehavior),
            new FrameworkPropertyMetadata(null, OnBoundPasswordChanged));

    /// <summary>
    /// Check flag to subscribe to the PasswordChanged event exactly once, 
    /// even if the BoundPassword changes multiple times in the same PasswordBox instance.
    /// </summary>
    private static readonly DependencyProperty _attachedProperty =
        DependencyProperty.RegisterAttached(
            "_Attached",
            typeof(bool),
            typeof(PasswordBoxBindingBehavior),
            new PropertyMetadata(false));

    /// <summary>
    /// Get the bound password.
    /// </summary>
    /// <param name="obj">The dependency object.</param>
    /// <returns>The bound password.</returns>
    public static string GetBoundPassword(DependencyObject obj) => (string)obj.GetValue(BoundPasswordProperty);

    /// <summary>
    /// Set the bound password.
    /// </summary>
    /// <param name="obj">The dependency object.</param>
    /// <param name="value">The value to set.</param>
    public static void SetBoundPassword(DependencyObject obj, string value) => obj.SetValue(BoundPasswordProperty, value);

    #region Private Methods

    /// <summary>
    /// Event handler for the BoundPasswordChanged event.
    /// </summary>
    /// <param name="dependencyObj">The dependency object.</param>
    /// <param name="propChangedEventArgs">The event arguments.</param>
    private static void OnBoundPasswordChanged(DependencyObject dependencyObj, DependencyPropertyChangedEventArgs propChangedEventArgs)
    {
        if (dependencyObj is not PasswordBox passwordBox)
        {
            return;
        }

        // Check if the PasswordChanged event is already subscribed.
        if (!(bool)passwordBox.GetValue(_attachedProperty))
        {
            passwordBox.SetValue(_attachedProperty, true);
            passwordBox.PasswordChanged += OnPasswordChanged;
        }

        var newValue = propChangedEventArgs.NewValue as string ?? string.Empty;
        if (passwordBox.Password != newValue)
        {
            passwordBox.Password = newValue;
        }
    }

    /// <summary>
    /// Event handler for the PasswordChanged event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="routedEventArgs">The event arguments.</param>
    private static void OnPasswordChanged(object sender, RoutedEventArgs routedEventArgs)
    {
        if (sender is PasswordBox passwordBox)
        {
            SetBoundPassword(passwordBox, passwordBox.Password);
        }
    }

    #endregion
}
