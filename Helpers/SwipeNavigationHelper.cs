using Microsoft.Maui.Controls;

namespace FinanceTracker.Helpers;

public static class SwipeNavigationHelper
{
    private static SwipeTransition? PendingTransition;

    public static void AddSwipeGestures(ContentPage page, string prevRoute, string nextRoute)
    {
        if (page.Content is not View content)
        {
            throw new InvalidOperationException("Swipe gestures require the page to have a root view.");
        }

        page.Appearing += OnPageAppearing;

        if (!string.IsNullOrEmpty(prevRoute))
        {
            var swipeRight = new SwipeGestureRecognizer { Direction = SwipeDirection.Right };
            swipeRight.Swiped += async (s, e) =>
            {
                await AnimateAndNavigateAsync(content, $"//{prevRoute}", SwipeDirection.Right);
            };
            content.GestureRecognizers.Add(swipeRight);
        }

        if (!string.IsNullOrEmpty(nextRoute))
        {
            var swipeLeft = new SwipeGestureRecognizer { Direction = SwipeDirection.Left };
            swipeLeft.Swiped += async (s, e) =>
            {
                await AnimateAndNavigateAsync(content, $"//{nextRoute}", SwipeDirection.Left);
            };
            content.GestureRecognizers.Add(swipeLeft);
        }
    }

    private static async Task AnimateAndNavigateAsync(VisualElement content, string route, SwipeDirection direction)
    {
        PendingTransition = new SwipeTransition(direction);

        var distance = Math.Max(content.Width, 320);
        var offset = direction == SwipeDirection.Left ? -distance : distance;

        await Task.WhenAll(
            content.TranslateToAsync(offset, 0, 180, Easing.CubicIn),
            content.FadeToAsync(0, 180, Easing.CubicIn));

        await Shell.Current.GoToAsync(route, animate: false);
    }

    private static async void OnPageAppearing(object? sender, EventArgs e)
    {
        if (sender is not ContentPage page || page.Content is not View content)
        {
            return;
        }

        var transition = PendingTransition;
        PendingTransition = null;

        if (transition is null)
        {
            await content.TranslateToAsync(0, 0, 0);
            await content.FadeToAsync(1, 0);
            return;
        }

        var distance = Math.Max(content.Width, 320);
        var startOffset = transition.Direction == SwipeDirection.Left ? distance : -distance;

        await content.TranslateToAsync(startOffset, 0, 0);
        await content.FadeToAsync(0, 0);

        await Task.WhenAll(
            content.TranslateToAsync(0, 0, 180, Easing.CubicOut),
            content.FadeToAsync(1, 180, Easing.CubicOut));
    }

    private sealed record SwipeTransition(SwipeDirection Direction);
}
