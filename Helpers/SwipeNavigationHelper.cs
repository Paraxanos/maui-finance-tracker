using Microsoft.Maui.Controls;

namespace FinanceTracker.Helpers;

public static class SwipeNavigationHelper
{
    public static void AddSwipeGestures(ContentPage page, string prevRoute, string nextRoute)
    {
        if (!string.IsNullOrEmpty(prevRoute))
        {
            var swipeRight = new SwipeGestureRecognizer { Direction = SwipeDirection.Right };
            swipeRight.Swiped += async (s, e) =>
            {
                await Shell.Current.GoToAsync($"//{prevRoute}");
            };
            page.GestureRecognizers.Add(swipeRight);
        }

        if (!string.IsNullOrEmpty(nextRoute))
        {
            var swipeLeft = new SwipeGestureRecognizer { Direction = SwipeDirection.Left };
            swipeLeft.Swiped += async (s, e) =>
            {
                await Shell.Current.GoToAsync($"//{nextRoute}");
            };
            page.GestureRecognizers.Add(swipeLeft);
        }
    }
}
