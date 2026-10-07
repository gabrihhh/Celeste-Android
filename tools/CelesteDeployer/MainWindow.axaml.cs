using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CelesteDeployer.Services;

namespace CelesteDeployer;

public partial class MainWindow : Window
{
    private readonly AdbService _adb = new();
    private readonly DeviceService _devices;
    private readonly DeployRunner _runner;
    private List<AndroidDevice> _deviceList = new();

    public MainWindow()
    {
        InitializeComponent();
        _devices = new DeviceService(_adb);
        _runner = new DeployRunner(_adb, new ApkFetcher());
        BrowseBtn.Click += OnBrowse;
        RefreshBtn.Click += async (_, _) => await RefreshDevices();
        InstallBtn.Click += async (_, _) => await RunDeploy();
        CloseBtn.Click += (_, _) => Close();
        RetryBtn.Click += (_, _) => ShowSelect();
        DeviceCombo.SelectionChanged += (_, _) => Validate();
        Opened += async (_, _) => await EnsureAdbThenRefresh();
    }

    private async void OnBrowse(object? s, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new() { Title = "Pasta do Celeste", AllowMultiple = false });
        if (folders.Count == 0) return;
        GamePathBox.Text = folders[0].Path.LocalPath;
        Validate();
    }

    private void Validate()
    {
        var v = GameValidator.Validate(GamePathBox.Text);
        HintText.Text = v switch
        {
            GameValidation.MissingGameFiles => "Essa pasta não parece ser o Celeste (faltam Celeste.exe/Content).",
            GameValidation.XnaBuild => "Essa é a versão XNA. Ative o beta 'opengl' na Steam ou use o .zip da itch.io.",
            _ => ""
        };
        InstallBtn.IsEnabled = v == GameValidation.Valid && DeviceCombo.SelectedItem is AndroidDevice d && d.Authorized;
    }

    private async Task EnsureAdbThenRefresh()
    {
        try { await _adb.EnsureAsync(null, default); await RefreshDevices(); }
        catch (DeployException ex) { HintText.Text = ex.Message; }
    }

    private async Task RefreshDevices()
    {
        _deviceList = await _devices.ListAsync(default);
        DeviceCombo.ItemsSource = _deviceList;
        DeviceCombo.SelectedItem = _deviceList.FirstOrDefault(d => d.Authorized) ?? _deviceList.FirstOrDefault();
        Validate();
    }

    private async Task RunDeploy()
    {
        if (DeviceCombo.SelectedItem is not AndroidDevice dev) return;
        ShowProgress();
        var progress = new Progress<string>(msg => StepText.Text = msg);
        try
        {
            await _runner.RunAsync(new DeployInput(GamePathBox.Text!, dev.Serial), progress, default);
            ShowDone("✓ Pronto! O Celeste abriu no celular e está finalizando a preparação do jogo " +
                     "(acompanhe a barra no próprio celular).", isError: false);
        }
        catch (DeployException ex) { ShowDone(ex.Message, isError: true); }
        catch (Exception ex) { ShowDone("Algo deu errado: " + ex.Message, isError: true); }
    }

    private void ShowSelect() { SelectPanel.IsVisible = true; ProgressPanel.IsVisible = false; DonePanel.IsVisible = false; }
    private void ShowProgress() { SelectPanel.IsVisible = false; ProgressPanel.IsVisible = true; DonePanel.IsVisible = false; }

    private void ShowDone(string text, bool isError)
    {
        SelectPanel.IsVisible = false;
        ProgressPanel.IsVisible = false;
        DonePanel.IsVisible = true;
        DoneText.Text = text;
        RetryBtn.IsVisible = isError;
    }
}
