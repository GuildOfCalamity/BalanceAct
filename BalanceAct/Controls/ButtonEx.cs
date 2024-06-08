using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BalanceAct.Controls;

public class ButtonEx : Button
{
    public ButtonEx()
    {
        this.CornerRadius = new CornerRadius(4);
        this.BorderThickness = new Thickness(1);

        this.PointerEntered += OnPointerEntered;
        this.PointerExited += OnPointerExited;
        //this.Click += OnClick;
    }

    #region [Events]
    void OnPointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) => this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
    
    void OnPointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e) => this.ProtectedCursor = null;

    async void OnClick(object sender, RoutedEventArgs e)
    {
        // Set the cursor to some new type.
        this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Wait);
        // Pause for effect.
        await Task.Delay(TimeSpan.FromSeconds(1));
        // Put back to original
        this.ProtectedCursor = null;
    }
    #endregion

    #region [for more depth use this]
    //public static readonly DependencyProperty EnableCursorOverrideProperty = DependencyProperty.Register(
    //    nameof(EnableCursorOverride),
    //    typeof(bool),
    //    typeof(ButtonEx),
    //    new PropertyMetadata(default, (d, _) => (d as ButtonEx)?.UpdateCursor()));
    //
    //public static readonly DependencyProperty InputSystemCursorShapeProperty = DependencyProperty.Register(
    //    nameof(InputSystemCursorShape),
    //    typeof(InputSystemCursorShape),
    //    typeof(ButtonEx),
    //    new PropertyMetadata(default, (d, _) => (d as ButtonEx)?.UpdateCursor()));
    //
    //static ButtonEx()
    //{
    //    CursorOptions = ImmutableArray.Create(Enum.GetValues<InputSystemCursorShape>());
    //}
    //
    //public static ImmutableArray<InputSystemCursorShape> CursorOptions { get; }
    //
    //public InputSystemCursorShape InputSystemCursorShape
    //{
    //    get => (InputSystemCursorShape)GetValue(InputSystemCursorShapeProperty);
    //    set => SetValue(InputSystemCursorShapeProperty, value);
    //}
    //
    //public bool EnableCursorOverride
    //{
    //    get => (bool)GetValue(EnableCursorOverrideProperty);
    //    set => SetValue(EnableCursorOverrideProperty, value);
    //}
    //
    //void UpdateCursor() => ProtectedCursor = EnableCursorOverride is true && InputSystemCursor.Create(InputSystemCursorShape) is InputCursor inputCursor ? inputCursor : null;
    #endregion
}
