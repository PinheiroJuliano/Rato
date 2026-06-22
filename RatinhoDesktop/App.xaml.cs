using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RatinhoDesktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private static EventWaitHandle? _eventWaitHandle;
    private const string EventName = "RatinhoDesktop_ShowInstance_Event";

    protected override void OnStartup(StartupEventArgs e)
    {
        _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, EventName, out bool isNewInstance);
        
        if (!isNewInstance)
        {
            // Signal the running instance to show itself
            _eventWaitHandle.Set();
            
            // Shut down this instance immediately
            Shutdown();
            return;
        }

        // Start listening for activation signals from other instances
        StartSignalListener();

        base.OnStartup(e);
    }

    private void StartSignalListener()
    {
        Task.Run(() =>
        {
            try
            {
                while (_eventWaitHandle != null)
                {
                    if (_eventWaitHandle.WaitOne())
                    {
                        // Signal received! Show the window on the main dispatcher thread.
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (System.Windows.Application.Current.MainWindow is MainWindow mainWin)
                            {
                                mainWin.Show();
                                if (mainWin.WindowState == WindowState.Minimized)
                                {
                                    mainWin.WindowState = WindowState.Normal;
                                }
                                mainWin.Activate();
                            }
                        }));
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Event handle was disposed, exit loop
            }
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _eventWaitHandle?.Close();
        _eventWaitHandle = null;
        base.OnExit(e);
    }
}

