using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.CoordinatorLayout.Widget;
using Com.Unity3d.Player;

namespace UnityUaalMaui;

[Activity(Label = "UnityActivity",
          MainLauncher = false,
          ConfigurationChanges = ConfigChanges.Mcc | ConfigChanges.Mnc | ConfigChanges.Locale | ConfigChanges.Touchscreen | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation | ConfigChanges.Orientation | ConfigChanges.ScreenLayout | ConfigChanges.UiMode | ConfigChanges.ScreenSize | ConfigChanges.SmallestScreenSize | ConfigChanges.FontScale | ConfigChanges.LayoutDirection | ConfigChanges.Density,
          //ScreenOrientation = ScreenOrientation.FullUser,
          ResizeableActivity = false,
          LaunchMode = LaunchMode.SingleTask)]
public class UnityActivity : Activity,
                             IUnityPlayerLifecycleEvents, IUnityPermissionRequestSupport, 
                             
                             /*IUnityPlayerSupport,*/ UnityUaalMaui.Unity.INativeUnityBridge
{
    private UnityPlayerForActivityOrService player;

    public void SendContent(string eventName, string eventContent)
    {
        var content = eventName + "|" + (eventContent ?? string.Empty);

        UnityPlayer.UnitySendMessage("Bridge", "ReceiveContent", content);
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        RequestWindowFeature(WindowFeatures.NoTitle);

        base.OnCreate(savedInstanceState);
        player = new UnityPlayerForActivityOrService(this, this);
    
        //CoordinatorLayout coordinatorLayout = new CoordinatorLayout(this);
        //coordinatorLayout.SetBackgroundColor(Android.Graphics.Color.Red);
        //coordinatorLayout.AddView(player.FrameLayout);

        this.SetContentView(player.FrameLayout);
        //player.FrameLayout.SetPadding(50, 25, 50, 50);
     
        player.FrameLayout?.RequestFocus();

        

        UnityUaalMaui.Unity.UnityBridge.RegisterNativeBridge(this);
    }

    public UnityPlayer? UnityPlayerConnection 
    {
		get { return player; }
    }

    // When Unity player unloaded move task to background
    public void OnUnityPlayerUnloaded()
    {
        Android.Util.Log.Info(GetType().Name, nameof(OnUnityPlayerUnloaded) + "|" + GetHashCode() + "|");
        MoveTaskToBack(true);
    }

    // Callback before Unity player process is killed
    public void OnUnityPlayerQuitted()
    {
        Android.Util.Log.Info(GetType().Name, nameof(OnUnityPlayerQuitted) + "|" + GetHashCode() + "|");
    }

    protected override void OnNewIntent(Android.Content.Intent? intent)
    {
        Android.Util.Log.Info(GetType().Name, nameof(OnNewIntent) + "|" + GetHashCode() + "|" + "Intent=" + intent.Action + "," + intent.Flags);
        Intent = intent;
        player.NewIntent(intent);
    }

    protected override void OnDestroy()
    {
        Android.Util.Log.Info(GetType().Name, nameof(OnDestroy) + "|" + GetHashCode() + "|");
        player.Destroy();
        base.OnDestroy();

        UnityUaalMaui.Unity.UnityBridge.RegisterNativeBridge(null);
    }

    protected override void OnStop()
    {
        Android.Util.Log.Info(GetType().Name, nameof(OnStop) + "|" + GetHashCode() + "|");
        base.OnStop();
        player.OnStop();
    }

    protected override void OnPause()
    {
        Android.Util.Log.Info(GetType().Name, nameof(OnPause) + "|" + GetHashCode() + "|");
        base.OnPause();
        player.OnPause();
    }

    protected override void OnStart()
    {
        Android.Util.Log.Info(GetType().Name, nameof(OnStart) + "|" + GetHashCode() + "|");
        base.OnStart();
        player.OnStart();
    }

    protected override void OnResume()
    {
        Android.Util.Log.Info(GetType().Name, nameof(OnResume) + "|" + GetHashCode() + "|");
        base.OnResume();
        player.OnResume();
    }

    public override void OnLowMemory()
    {
        base.OnLowMemory();
        player.OnTrimMemory(UnityPlayerForActivityOrService.MemoryUsage.Critical);
    }

    public override void OnTrimMemory([GeneratedEnum] TrimMemory level)
    {
        base.OnTrimMemory(level);
        switch (level)
        {
            case TrimMemory.RunningModerate:
                player.OnTrimMemory(UnityPlayerForActivityOrService.MemoryUsage.Medium);
                break;
            case TrimMemory.RunningLow:
                player.OnTrimMemory(UnityPlayerForActivityOrService.MemoryUsage.High);
                break;
            case TrimMemory.RunningCritical:
                player.OnTrimMemory(UnityPlayerForActivityOrService.MemoryUsage.Critical);
                break;
        }
    }

    // Configuration changes are used by Video playback logic in Unity
    public override void OnConfigurationChanged(Configuration newConfig)
    {
        Android.Util.Log.Info(GetType().Name, nameof(OnConfigurationChanged) + "|" + GetHashCode() + "|" + newConfig);
        base.OnConfigurationChanged(newConfig);
        player.ConfigurationChanged(newConfig);
    }

    // Notify Unity of the focus change.
    public override void OnWindowFocusChanged(bool hasFocus)
    {
        Android.Util.Log.Info(GetType().Name, nameof(OnWindowFocusChanged) + "|" + GetHashCode() + "|" + "hasFocus=" + hasFocus);
        base.OnWindowFocusChanged(hasFocus);
        player.WindowFocusChanged(hasFocus);
    }

    public override bool DispatchKeyEvent(KeyEvent? e)
    {
        Android.Util.Log.Info(GetType().Name, nameof(DispatchKeyEvent) + "|" + GetHashCode() + "|" + e.Action);
        if (e.Action == KeyEventActions.Multiple)
        {
            return player.InjectEvent(e);
        }

        return base.DispatchKeyEvent(e);
    }

    // Quit Unity
    public void RequestPermissions(PermissionRequest? request)
    {
        player.AddPermissionRequest(request);
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        player.PermissionResponse(this, requestCode, permissions, grantResults?.Select(gr => (int)gr)?.ToArray() ?? Array.Empty<int>());
    }

    // Pass any events not handled by (unfocused) views straight to UnityPlayer
    public override bool OnKeyUp(Keycode keyCode, KeyEvent? e)
    {
        Android.Util.Log.Info(GetType().Name, nameof(OnKeyUp));
        return player.FrameLayout.OnKeyUp(keyCode, e);
    }

    public override bool OnKeyDown(Keycode keyCode, KeyEvent? e)
    {
        Android.Util.Log.Info(GetType().Name, nameof(OnKeyDown));
        return player.FrameLayout.OnKeyDown(keyCode, e);
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        Android.Util.Log.Info(GetType().Name, nameof(OnTouchEvent));
        return player.FrameLayout.OnTouchEvent(e);
    }

    public override bool OnGenericMotionEvent(MotionEvent? e)
    {
        Android.Util.Log.Info(GetType().Name, nameof(OnGenericMotionEvent));
        return player.FrameLayout.OnGenericMotionEvent(e);
    }
}