using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace FinanceTracker;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private float _startX;
    private float _startY;

    public override bool DispatchTouchEvent(MotionEvent? ev)
    {
        if (ev == null)
        {
            return base.DispatchTouchEvent(ev);
        }

        switch (ev.Action)
        {
            case MotionEventActions.Down:
                _startX = ev.GetX();
                _startY = ev.GetY();
                break;

            case MotionEventActions.Up:
                float diffX = ev.GetX() - _startX;
                float diffY = ev.GetY() - _startY;

                // Detect horizontal flings: distance > 150px and horizontal component is at least twice the vertical component
                if (Math.Abs(diffX) > 150 && Math.Abs(diffX) > Math.Abs(diffY) * 2)
                {
                    var direction = diffX > 0 ? Microsoft.Maui.SwipeDirection.Right : Microsoft.Maui.SwipeDirection.Left;
                    if (FinanceTracker.Helpers.SwipeNavigationHelper.OnAndroidSwipe(direction))
                    {
                        return true; // Consume touch event so child views do not receive the Up action
                    }
                }
                break;
        }

        return base.DispatchTouchEvent(ev);
    }
}
