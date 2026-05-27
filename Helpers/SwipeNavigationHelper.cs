using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace FinanceTracker.Helpers;

public static class SwipeNavigationHelper
{
    private static SwipeTransition? PendingTransition;

    // Store current active page states for global platform gesture detectors (e.g. Android MainActivity)
    private static ContentPage? CurrentActivePage;
    private static string? CurrentPrevRoute;
    private static string? CurrentNextRoute;

    public static void AddSwipeGestures(ContentPage page, string prevRoute, string nextRoute)
    {
        if (page.Content is not View pageRoot)
        {
            return;
        }

        // Track active page and routes when pages appear/disappear
        page.Appearing += (s, e) =>
        {
            CurrentActivePage = page;
            CurrentPrevRoute = prevRoute;
            CurrentNextRoute = nextRoute;
        };

        page.Disappearing += (s, e) =>
        {
            if (CurrentActivePage == page)
            {
                CurrentActivePage = null;
                CurrentPrevRoute = null;
                CurrentNextRoute = null;
            }
        };

        page.Appearing += OnPageAppearing;

        // On non-Android platforms, we attach standard SwipeGestureRecognizer to the pageRoot only
#if !ANDROID
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
                    if (Shell.Current is not null)
                    {
                        await Shell.Current.GoToAsync($"//{prevRoute}", animate: false);
                    }
                }
            };
            pageRoot.GestureRecognizers.Add(swipeRight);
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
                    if (Shell.Current is not null)
                    {
                        await Shell.Current.GoToAsync($"//{nextRoute}", animate: false);
                    }
                }
            };
            pageRoot.GestureRecognizers.Add(swipeLeft);
        }
#endif
    }

    /// <summary>
    /// Invoked by Android platform gesture interceptors to trigger swipe navigation.
    /// </summary>
    public static bool OnAndroidSwipe(SwipeDirection direction)
    {
        if (CurrentActivePage == null || CurrentActivePage.Content is not View pageRoot)
        {
            return false;
        }

        var route = direction == SwipeDirection.Left ? CurrentNextRoute : CurrentPrevRoute;
        if (string.IsNullOrEmpty(route))
        {
            return false;
        }

        if (Shell.Current is null)
        {
            return false;
        }

        // Always run UI transitions and navigation on the main thread
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (Shell.Current is null)
            {
                return;
            }

            try
            {
                await AnimateAndNavigateAsync(pageRoot, $"//{route}", direction);
            }
            catch
            {
                if (Shell.Current is not null)
                {
                    await Shell.Current.GoToAsync($"//{route}", animate: false);
                }
            }
        });

        return true;
    }

    private static async Task AnimateAndNavigateAsync(VisualElement content, string route, SwipeDirection direction)
    {
        if (Shell.Current is null)
        {
            return;
        }

        PendingTransition = new SwipeTransition(direction);

        var distance = Math.Max(content.Width, 320);
        var offset = direction == SwipeDirection.Left ? -distance : distance;

        await Task.WhenAll(
            content.TranslateToAsync(offset, 0, 180, Easing.CubicIn),
            content.FadeToAsync(0, 180, Easing.CubicIn));

        if (Shell.Current is not null)
        {
            await Shell.Current.GoToAsync(route, animate: false);
        }
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
