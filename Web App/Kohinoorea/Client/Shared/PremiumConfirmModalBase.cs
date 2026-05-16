using Microsoft.AspNetCore.Components;

public class PremiumConfirmModalBase : ComponentBase
{
    [Parameter] public string Title { get; set; } = "Confirm Action";
    [Parameter] public string Message { get; set; } = "Are you sure?";
    [Parameter] public EventCallback<bool> OnResult { get; set; }
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
}
