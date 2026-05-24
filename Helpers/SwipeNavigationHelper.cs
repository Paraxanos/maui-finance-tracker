using Microsoft.Maui.Controls;

namespace FinanceTracker.Helpers;

public static class SwipeNavigationHelper
{
    private static SwipeTransition? PendingTransition;

    public static void AddSwipeGestures(ContentPage page, string prevRoute, string nextRoute)
    {
        if (page.Content is not View pageRoot)
        {
            throw new InvalidOperationException("Swipe gestures require the page to have a root view.");
        }

        page.Appearing += OnPageAppearing;

        // Recursively attach separate gesture recognizers to pageRoot and all scrollable/layout children
        AttachGesturesRecursively(pageRoot, pageRoot, prevRoute, nextRoute);
    }

    private static void AttachGesturesRecursively(Element element, View pageRoot, string prevRoute, string nextRoute)
    {
        if (element is View view)
        {
            // Attach to the page root, scrollable controls, and major layout containers
            if (view == pageRoot || view is ScrollView || view is CollectionView || view is Layout)
            {
                AttachSwipeToControl(view, pageRoot, prevRoute, nextRoute);
            }
        }

        foreach (var child in element.LogicalChildren)
        {
            AttachGesturesRecursively(child, pageRoot, prevRoute, nextRoute);
        }
    }

    private static void AttachSwipeToControl(View view, View pageRoot, string prevRoute, string nextRoute)
    {
        if (!string.IsNullOrEmpty(prevRoute))
        {
            var swipeRight = new SwipeGestureRecognizer { Direction = SwipeDirection.Right };
            swipeRight.Swiped += async (s, e) =>
            {
                try
                {
                    await AnimateAndNavigateAsync(pageRoot, $"//{prevRoute}", SwipeDirection.Right);
                }
                catch
                {
                    await Shell.Current.GoToAsync($"//{prevRoute}", animate: false);
                }
            };
            view.GestureRecognizers.Add(swipeRight);
        }

        if (!string.IsNullOrEmpty(nextRoute))
        {
            var swipeLeft = new SwipeGestureRecognizer { Direction = SwipeDirection.Left };
            swipeLeft.Swiped += async (s, e) =>
            {
                try
                {
                    await AnimateAndNavigateAsync(pageRoot, $"//{nextRoute}", SwipeDirection.Left);
                }
                catch
                {
                    await Shell.Current.GoToAsync($"//{nextRoute}", animate: false);
                }
            };
            view.GestureRecognizers.Add(swipeLeft);
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
        try
        {
            if (sender is not ContentPage page || page.Content is not View content)
            {
                return;
            }

            var transition = PendingTransition;
            PendingTransition = null;

            if (transition is null)
            {
                content.TranslationX = 0;
                content.Opacity = 1;
                return;
            }

            var distance = Math.Max(content.Width, 320);
            var startOffset = transition.Direction == SwipeDirection.Left ? distance : -distance;

            content.TranslationX = startOffset;
            content.Opacity = 0;

            await Task.WhenAll(
                content.TranslateToAsync(0, 0, 180, Easing.CubicOut),
                content.FadeToAsync(1, 180, Easing.CubicOut));
        }
        catch
        {
            if (sender is ContentPage page && page.Content is View content)
            {
                content.TranslationX = 0;
                content.Opacity = 1;
            }
        }
    }

    private sealed record SwipeTransition(SwipeDirection Direction);
}
