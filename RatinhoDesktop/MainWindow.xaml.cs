using System;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace RatinhoDesktop;

public partial class MainWindow : Window
{
    private DispatcherTimer? _dvdTimer;
    private double _vx = 4.0;
    private double _vy = 4.0;
    private bool _isDvdMode = false;

    private string? _squeakPath;
    private string? _melodyPath;
    private SoundPlayer? _squeakPlayer;
    private MediaPlayer? _backgroundMusic;

    private bool _squeakEnabled = true;
    private bool _musicEnabled = false;
    private double _currentOpacity = 1.0;

    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    private HwndSource? _source;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int HOTKEY_ID = 9000;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint VK_R = 0x52; // Key 'R'

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Posicionar o ratinho no centro da tela inicialmente
        double screenWidth = SystemParameters.PrimaryScreenWidth;
        double screenHeight = SystemParameters.PrimaryScreenHeight;
        this.Left = (screenWidth - this.Width) / 2;
        this.Top = (screenHeight - this.Height) / 2;

        // Gerar os sons sintetizados na pasta de execução local
        string assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
        _squeakPath = SoundGenerator.GenerateSqueakFile(assetsPath);
        _melodyPath = SoundGenerator.GenerateMelodyFile(assetsPath);

        // Inicializar SoundPlayer para o squeak
        if (File.Exists(_squeakPath))
        {
            _squeakPlayer = new SoundPlayer(_squeakPath);
            _squeakPlayer.Load();
        }

        // Inicializar MediaPlayer para a música de fundo
        if (File.Exists(_melodyPath))
        {
            _backgroundMusic = new MediaPlayer();
            _backgroundMusic.Open(new Uri(_melodyPath));
            _backgroundMusic.Volume = 0.35; // volume confortável
            
            // Loop da música
            _backgroundMusic.MediaEnded += (s, ev) =>
            {
                if (_musicEnabled && _backgroundMusic != null)
                {
                    _backgroundMusic.Position = TimeSpan.Zero;
                    _backgroundMusic.Play();
                }
            };
        }

        // Configurar timer para o Modo DVD (aprox. 60 FPS)
        _dvdTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _dvdTimer.Tick += DvdTimer_Tick;

        // Inicializar o ícone na bandeja do sistema (System Tray)
        InitializeTrayIcon();
    }

    private void RatoImage_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            if (e.ClickCount == 2)
            {
                ToggleDvdMode();
            }
            else
            {
                PlaySqueak();
                try
                {
                    DragMove();
                }
                catch
                {
                    // Ignora erros se o botão esquerdo for solto rapidamente
                }
            }
        }
    }

    private void PlaySqueak()
    {
        if (_squeakEnabled && _squeakPlayer != null)
        {
            try
            {
                _squeakPlayer.Play();
            }
            catch
            {
                // Ignora se der erro de concorrência ou bloqueio
            }
        }
    }

    private void ToggleDvdMode()
    {
        _isDvdMode = !_isDvdMode;
        MenuDvdMode.IsChecked = _isDvdMode;

        if (_isDvdMode)
        {
            // Começa com velocidade aleatória
            Random rand = new Random();
            _vx = (rand.Next(0, 2) == 0 ? -1 : 1) * (rand.NextDouble() * 3.0 + 3.0);
            _vy = (rand.Next(0, 2) == 0 ? -1 : 1) * (rand.NextDouble() * 3.0 + 3.0);
            _dvdTimer?.Start();
        }
        else
        {
            _dvdTimer?.Stop();
        }
    }

    private void DvdTimer_Tick(object? sender, EventArgs e)
    {
        double screenWidth = SystemParameters.PrimaryScreenWidth;
        double screenHeight = SystemParameters.PrimaryScreenHeight;

        double left = this.Left;
        double top = this.Top;
        double width = this.ActualWidth;
        double height = this.ActualHeight;

        left += _vx;
        top += _vy;

        bool bounced = false;

        // Limites horizontais
        if (left <= 0)
        {
            left = 0;
            _vx = -_vx;
            bounced = true;
        }
        else if (left + width >= screenWidth)
        {
            left = screenWidth - width;
            _vx = -_vx;
            bounced = true;
        }

        // Limites verticais
        if (top <= 0)
        {
            top = 0;
            _vy = -_vy;
            bounced = true;
        }
        else if (top + height >= screenHeight)
        {
            top = screenHeight - height;
            _vy = -_vy;
            bounced = true;
        }

        // Atualizar rotação/espelhamento horizontal com base na direção do movimento
        // Se estiver indo para a esquerda (vx < 0), espelha horizontalmente.
        if (RatoImage.RenderTransform is ScaleTransform scaleTransform)
        {
            scaleTransform.ScaleX = _vx < 0 ? -1 : 1;
        }
        else
        {
            // Cria caso não exista (inicialmente configuramos RotateTransform, vamos substituir por ScaleTransform)
            RatoImage.RenderTransform = new ScaleTransform(_vx < 0 ? -1 : 1, 1);
        }

        this.Left = left;
        this.Top = top;

        if (bounced)
        {
            PlaySqueak();
            
            // Efeito visual de colisão (mudar levemente a opacidade temporariamente)
            this.Opacity = _currentOpacity * 0.8;
            DispatcherTimer opacityRestoreTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            opacityRestoreTimer.Tick += (s, ev) =>
            {
                this.Opacity = _currentOpacity;
                opacityRestoreTimer.Stop();
            };
            opacityRestoreTimer.Start();
        }
    }

    // --- Ações do Menu de Contexto ---

    private void Size_Pequeno_Click(object sender, RoutedEventArgs e)
    {
        SetSize(100);
        UpdateSizeMenuChecked(MenuSizeSmall);
    }

    private void Size_Medio_Click(object sender, RoutedEventArgs e)
    {
        SetSize(200);
        UpdateSizeMenuChecked(MenuSizeMedium);
    }

    private void Size_Grande_Click(object sender, RoutedEventArgs e)
    {
        SetSize(320);
        UpdateSizeMenuChecked(MenuSizeLarge);
    }

    private void SetSize(double size)
    {
        RatoImage.Width = size;
        RatoImage.Height = size;
        this.Width = size + 10; // adiciona padding
        this.Height = size + 10;
    }

    private void UpdateSizeMenuChecked(System.Windows.Controls.MenuItem checkedItem)
    {
        MenuSizeSmall.IsChecked = false;
        MenuSizeMedium.IsChecked = false;
        MenuSizeLarge.IsChecked = false;
        checkedItem.IsChecked = true;
    }

    private void DvdMode_Click(object sender, RoutedEventArgs e)
    {
        ToggleDvdMode();
    }

    private void SoundClick_Click(object sender, RoutedEventArgs e)
    {
        _squeakEnabled = MenuSoundClick.IsChecked;
    }

    private void Music_Click(object sender, RoutedEventArgs e)
    {
        _musicEnabled = MenuMusic.IsChecked;
        if (_backgroundMusic != null)
        {
            if (_musicEnabled)
            {
                _backgroundMusic.Position = TimeSpan.Zero;
                _backgroundMusic.Play();
            }
            else
            {
                _backgroundMusic.Pause();
            }
        }
    }

    private void Opacity_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.MenuItem item && double.TryParse(item.Tag as string, out double val))
        {
            _currentOpacity = val;
            this.Opacity = val;
        }
    }

    private void Topmost_Click(object sender, RoutedEventArgs e)
    {
        this.Topmost = MenuTopmost.IsChecked;
    }

    private void Sair_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Ocultar_Click(object sender, RoutedEventArgs e)
    {
        this.Hide();
    }

    private void InitializeTrayIcon()
    {
        _notifyIcon = new System.Windows.Forms.NotifyIcon();
        try
        {
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
            if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
            {
                _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
            }
            else
            {
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }
        }
        catch
        {
            _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
        }

        _notifyIcon.Text = "Ratinho Desktop";
        _notifyIcon.Visible = true;

        // Double click on tray icon toggles window visibility
        _notifyIcon.DoubleClick += (s, e) => ToggleWindowVisibility();

        // Context menu for the tray icon
        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        
        var showHideItem = new System.Windows.Forms.ToolStripMenuItem("Mostrar / Ocultar");
        showHideItem.Click += (s, e) => ToggleWindowVisibility();
        contextMenu.Items.Add(showHideItem);
        
        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        
        var exitItem = new System.Windows.Forms.ToolStripMenuItem("Sair");
        exitItem.Click += (s, e) => Close();
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    private void ToggleWindowVisibility()
    {
        if (this.Visibility == Visibility.Visible)
        {
            this.Hide();
        }
        else
        {
            this.Show();
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }
            this.Activate();
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var helper = new WindowInteropHelper(this);
        _source = HwndSource.FromHwnd(helper.Handle);
        if (_source != null)
        {
            _source.AddHook(HwndHook);
            RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_R);
        }
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            ToggleWindowVisibility();
            handled = true;
        }
        return IntPtr.Zero;
    }

    protected override void OnClosed(EventArgs e)
    {
        // Stop music
        if (_backgroundMusic != null)
        {
            _backgroundMusic.Stop();
            _backgroundMusic.Close();
        }

        // Unregister global hotkey
        if (_source != null)
        {
            _source.RemoveHook(HwndHook);
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
        }

        // Dispose system tray icon
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        base.OnClosed(e);
    }
}