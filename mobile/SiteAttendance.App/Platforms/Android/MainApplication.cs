using Android.App;
using Android.Runtime;
using Shiny;

namespace SiteAttendance.App;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    public override void OnCreate()
    {
        base.OnCreate();
        
        // Initialize Shiny for Android background services
        this.ShinyOnCreate();
    }
}
