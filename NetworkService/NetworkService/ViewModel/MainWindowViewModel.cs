using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using NetworkService.Commands;
using NetworkService.Model;
using NetworkService.Views;
using Notification.Wpf;

namespace NetworkService.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private object _currentViewModel;
        private readonly Stack<Action> _undoStack = new Stack<Action>();
        private readonly HomeViewModel _homeViewModel;
        private string _viewTitle = "DER MONITOR";
        private bool _showBackButton;
        private object _navBackVM;
        private string _navBackTitle;
        private bool _navBackShowBack;
        private NetworkEntitiesViewModel _networkEntitiesViewModel;
        private AddEntityViewModel _addEntityViewModel;
        private NetworkDisplayViewModel _networkDisplayViewModel;
        private MeasurementGraphViewModel _measurementGraphViewModel;
        private readonly NotificationManager _notificationManager = new NotificationManager();

        private const string SimulatorPath =
            @"C:\Users\tijana\OneDrive\Documents\GitHub\HCI\MeteringSimulator\MeteringSimulator\bin\Debug\MeteringSimulator.exe";

        private static void RestartMeteringSimulator(Action<string, string> showToast = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
                showToast?.Invoke("Simulator", "Restartuje se..."));

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    foreach (var proc in Process.GetProcessesByName("MeteringSimulator"))
                        try
                        {
                            proc.CloseMainWindow();
                            if (!proc.WaitForExit(2000))
                                proc.Kill();
                        }
                        catch { }
                }
                catch { }
                System.Threading.Thread.Sleep(1500);
                var logPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(SimulatorPath), "restart.log");
                try
                {
                    System.IO.File.AppendAllText(logPath,
                        $"[{DateTime.Now:HH:mm:ss}] Attempting launch. Path exists: {System.IO.File.Exists(SimulatorPath)}\n");
                    var args = $"/c start \"\" \"{SimulatorPath}\"";
                    var p = Process.Start(new ProcessStartInfo("cmd.exe", args)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                    System.IO.File.AppendAllText(logPath,
                        $"[{DateTime.Now:HH:mm:ss}] cmd.exe started: PID {p?.Id}\n");
                    Application.Current.Dispatcher.Invoke(() =>
                        showToast?.Invoke("Simulator", "Simulator je pokrenut"));
                }
                catch (Exception ex)
                {
                    System.IO.File.AppendAllText(logPath,
                        $"[{DateTime.Now:HH:mm:ss}] EXCEPTION: {ex.GetType().Name}: {ex.Message}\n");
                }
            });
        }

        public ObservableCollection<NetworkEntity> Entities { get; }
            = new ObservableCollection<NetworkEntity>();

        public object CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                _currentViewModel = value;
                OnPropertyChanged(nameof(CurrentViewModel));
                OnPropertyChanged(nameof(StatusBarBackground));
            }
        }

        public string StatusBarBackground
            => CurrentViewModel is NetworkEntitiesViewModel ? "#1E8449" : "#196F3D";

        public ICommand NavigateHomeCommand { get; }
        public ICommand NavigateToEntitiesCommand { get; }
        public ICommand NavigateToDisplayCommand { get; }
        public ICommand NavigateToGraphCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand NavigateBackCommand { get; }

        public string ViewTitle
        {
            get => _viewTitle;
            set { _viewTitle = value; OnPropertyChanged(nameof(ViewTitle)); }
        }

        public bool ShowBackButton
        {
            get => _showBackButton;
            set { _showBackButton = value; OnPropertyChanged(nameof(ShowBackButton)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindowViewModel()
        {
            _homeViewModel = new HomeViewModel();

            Action<string, string> showToast = (title, msg) =>
                _notificationManager.Show(
                    new NotificationContent { Title = title, Message = msg, Type = NotificationType.Success },
                    "WindowNotificationArea");

            Action<string, string> showError = (title, msg) =>
                _notificationManager.Show(
                    new NotificationContent { Title = title, Message = msg, Type = NotificationType.Error },
                    "WindowNotificationArea");

            Action<string, string> showInfo = (title, msg) =>
                _notificationManager.Show(
                    new NotificationContent { Title = title, Message = msg, Type = NotificationType.Information },
                    "WindowNotificationArea");

            Action<Action> pushUndoForEntities = undoAction => _undoStack.Push(() =>
            {
                CurrentViewModel = _networkEntitiesViewModel;
                ViewTitle = "Network Entities";
                ShowBackButton = true;
                undoAction();
            });

            Action<Action> pushUndoForDisplay = undoAction => _undoStack.Push(() =>
            {
                CurrentViewModel = _networkDisplayViewModel;
                ViewTitle = "Network Display";
                ShowBackButton = true;
                undoAction();
            });

            _addEntityViewModel = new AddEntityViewModel(Entities,
                () =>
                {
                    CurrentViewModel = _networkEntitiesViewModel;
                    ViewTitle = "Network Entities";
                    ShowBackButton = true;
                },
                showToast,
                () => RestartMeteringSimulator(showInfo),
                pushUndoForEntities);

            _networkEntitiesViewModel = new NetworkEntitiesViewModel(Entities,
                () =>
                {
                    CurrentViewModel = _addEntityViewModel;
                    ViewTitle = "Add Entity";
                    ShowBackButton = true;
                },
                entity =>
                {
                    _navBackVM = CurrentViewModel;
                    _navBackTitle = ViewTitle;
                    _navBackShowBack = ShowBackButton;
                    _measurementGraphViewModel.SelectedEntity = entity;
                    CurrentViewModel = _measurementGraphViewModel;
                    ViewTitle = "Measurement Graph";
                    ShowBackButton = true;
                },
                name =>
                {
                    var d = new ConfirmDeleteDialog(name);
                    d.Owner = Application.Current.MainWindow;
                    return d.ShowDialog() == true;
                },
                showError,
                () => RestartMeteringSimulator(showInfo),
                pushUndoForEntities);

            _networkDisplayViewModel = new NetworkDisplayViewModel(Entities, pushUndoForDisplay, showError);
            _networkEntitiesViewModel.ClearConnections = id => _networkDisplayViewModel.ClearConnectionsForEntity(id);
            _measurementGraphViewModel = new MeasurementGraphViewModel(Entities);
            Entities.Add(new NetworkEntity { ID = 1, Naziv = "Solarni panel 1",
                Tip = EntityType.SolarniPanel, LastValue = 3.2, LastValueValid = true });
            Entities.Add(new NetworkEntity { ID = 2, Naziv = "Vetrogenerator 1",
                Tip = EntityType.Vetrogenerator, LastValue = 4.8, LastValueValid = true });
            Entities.Add(new NetworkEntity { ID = 3, Naziv = "Solarni panel 2",
                Tip = EntityType.SolarniPanel, LastValue = 0.5, LastValueValid = false });

            NavigateHomeCommand = new RelayCommand(_ =>
            {
                CurrentViewModel = _homeViewModel;
                ViewTitle = "DER MONITOR";
                ShowBackButton = false;
            });
            NavigateToEntitiesCommand = new RelayCommand(_ =>
            {
                CurrentViewModel = _networkEntitiesViewModel;
                ViewTitle = "Network Entities";
                ShowBackButton = true;
            });
            NavigateToDisplayCommand = new RelayCommand(_ =>
            {
                CurrentViewModel = _networkDisplayViewModel;
                ViewTitle = "Network Display";
                ShowBackButton = true;
            });
            NavigateToGraphCommand = new RelayCommand(_ =>
            {
                _navBackVM = null;
                CurrentViewModel = _measurementGraphViewModel;
                ViewTitle = "Graph view";
                ShowBackButton = true;
            });
            UndoCommand = new RelayCommand(
                _  => { if (_undoStack.Count > 0) _undoStack.Pop()?.Invoke(); },
                _  => _undoStack.Count > 0);

            CurrentViewModel = _homeViewModel;
            NavigateBackCommand = new RelayCommand(_ =>
            {
                if (CurrentViewModel is AddEntityViewModel)
                {
                    CurrentViewModel = _networkEntitiesViewModel;
                    ViewTitle = "Network Entities";
                    ShowBackButton = true;
                }
                else if (CurrentViewModel is NetworkDisplayViewModel)
                {
                    CurrentViewModel = _homeViewModel;
                    ViewTitle = "DER MONITOR";
                    ShowBackButton = false;
                }
                else if (CurrentViewModel is MeasurementGraphViewModel)
                {
                    if (_navBackVM != null)
                    {
                        CurrentViewModel = _navBackVM;
                        ViewTitle = _navBackTitle;
                        ShowBackButton = _navBackShowBack;
                        _navBackVM = null;
                    }
                    else
                    {
                        CurrentViewModel = _homeViewModel;
                        ViewTitle = "DER MONITOR";
                        ShowBackButton = false;
                    }
                }
                else
                {
                    CurrentViewModel = _homeViewModel;
                    ViewTitle = "DER MONITOR";
                    ShowBackButton = false;
                }
            });
            createListener();
        }

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void createListener()
        {
            var tcp = new TcpListener(IPAddress.Any, 25675);
            tcp.Start();

            var listeningThread = new Thread(() =>
            {
                while (true)
                {
                    var tcpClient = tcp.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(param =>
                    {
                        NetworkStream stream = tcpClient.GetStream();
                        string incomming;
                        byte[] bytes = new byte[1024];
                        int i = stream.Read(bytes, 0, bytes.Length);
                        incomming = System.Text.Encoding.ASCII.GetString(bytes, 0, i);

                        if (incomming.Equals("Need object count"))
                        {
                            Byte[] data = System.Text.Encoding.ASCII.GetBytes(Entities.Count.ToString());
                            stream.Write(data, 0, data.Length);
                        }
                        else
                        {
                            try
                            {
                                var colonParts = incomming.Split(':');
                                int n = int.Parse(colonParts[0].Split('_')[1]);
                                double v = double.Parse(colonParts[1], CultureInfo.InvariantCulture);
                                if (n >= 0 && n < Entities.Count)
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        Entities[n].LastValue = v;
                                        Entities[n].LastValueValid = v >= 1.0 && v <= 5.0;
                                        var hist = Entities[n].MeasurementHistory;
                                        hist.Add(new NetworkEntity.MeasurementPoint
                                            { Value = v, Timestamp = DateTime.Now, IsValid = Entities[n].LastValueValid });
                                        if (hist.Count > 5) hist.RemoveAt(0);
                                    });
                                    File.AppendAllText("log.txt",
                                        $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}] Entitet_{n}: {v}{Environment.NewLine}");
                                }
                            }
                            catch { }
                        }
                    }, null);
                }
            });

            listeningThread.IsBackground = true;
            listeningThread.Start();
        }
    }
}
