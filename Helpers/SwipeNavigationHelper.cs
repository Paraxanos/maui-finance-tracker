using Microsoft.Maui.Controls;

namespace FinanceTracker.Helpers;

public static class SwipeNavigationHelper
{
    public static void AddSwipeGestures(ContentPage page, string prevRoute, string nextRoute)
    {
        if (page.Content is not View content)
        {
            throw new InvalidOperationException("Swipe gestures require the page to have a root view.");
        }
        
        if (!string.IsNullOrEmpty(prevRoute))
        {
            var swipeRight = new SwipeGestureRecognizer { Direction = SwipeDirection.Right };
            swipeRight.Swiped += async (s, e) =>
            {
                await Shell.Current.GoToAsync($"//{prevRoute}");
            };
            content.GestureRecognizers.Add(swipeRight);
        }

        if (!string.IsNullOrEmpty(nextRoute))
        {
            var swipeLeft = new SwipeGestureRecognizer { Direction = SwipeDirection.Left };
            swipeLeft.Swiped += async (s, e) =>
            {
                await Shell.Current.GoToAsync($"//{nextRoute}");
            };
            content.GestureRecognizers.Add(swipeLeft);
        }
    }
}
