﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Artemis.DAL;
using Artemis.Events;
using Artemis.Managers;
using Artemis.Services;
using Artemis.Settings;
using Artemis.Utilities;
using Caliburn.Micro;

namespace Artemis.ViewModels
{
    public class SystemTrayViewModel : Screen
    {
        private readonly ShellViewModel _shellViewModel;
        private readonly IWindowManager _windowManager;
        private string _activeIcon;
        private bool _checked;
        private bool _enabled;
        private string _toggleText;

        public SystemTrayViewModel(IWindowManager windowManager, MetroDialogService dialogService,
            ShellViewModel shellViewModel, MainManager mainManager)
        {
            _windowManager = windowManager;
            _shellViewModel = shellViewModel;

            DialogService = dialogService;
            MainManager = mainManager;

            MainManager.EnableProgram();
            MainManager.OnEnabledChangedEvent += MainManagerOnOnEnabledChangedEvent;

            var generalSettings = SettingsProvider.Load<GeneralSettings>();
            Enabled = !generalSettings.Suspended;
            if (generalSettings.ShowOnStartup)
                ShowWindow();
        }

        public MetroDialogService DialogService { get; set; }

        public MainManager MainManager { get; set; }

        public bool CanShowWindow => !_shellViewModel.IsActive;
        public bool CanHideWindow => _shellViewModel.IsActive && !MainManager.DeviceManager.ChangingKeyboard;
        public bool CanToggleEnabled => !MainManager.DeviceManager.ChangingKeyboard;

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value == _enabled) return;
                _enabled = value;

                ToggleText = _enabled ? "Disable Artemis" : "Enable Artemis";
                ActiveIcon = _enabled ? "../Resources/logo.ico" : "../Resources/logo-disabled.ico";
                NotifyOfPropertyChange(() => Enabled);
            }
        }

        public string ActiveIcon
        {
            get { return _activeIcon; }
            set
            {
                _activeIcon = value;
                NotifyOfPropertyChange();
            }
        }

        public string ToggleText
        {
            get { return _toggleText; }
            set
            {
                if (value == _toggleText) return;
                _toggleText = value;
                NotifyOfPropertyChange(() => ToggleText);
            }
        }

        public Mutex Mutex { get; set; }

        private void MainManagerOnOnEnabledChangedEvent(object sender, EnabledChangedEventArgs e)
        {
            Enabled = e.Enabled;
        }

        public void ToggleEnabled()
        {
            if (Enabled)
                MainManager.DisableProgram();
            else
                MainManager.EnableProgram();
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            NotifyOfPropertyChange(() => CanShowWindow);
            NotifyOfPropertyChange(() => CanHideWindow);
        }

        public void ShowWindow()
        {
            if (!CanShowWindow)
                return;

            // manually show the next window view-model
            _windowManager.ShowWindow(_shellViewModel);

            NotifyOfPropertyChange(() => CanShowWindow);
            NotifyOfPropertyChange(() => CanHideWindow);
            
            SettingsProvider.Load<GeneralSettings>().ApplyTheme();

            // Show certain dialogs if needed
            CheckKeyboardState();
            CheckDuplicateInstances();
            Updater.CheckChangelog(DialogService);
        }

        private void CheckDuplicateInstances()
        {
            if (_checked)
                return;
            _checked = true;

            bool aIsNewInstance;
            Mutex = new Mutex(true, "ArtemisMutex", out aIsNewInstance);
            if (aIsNewInstance)
                return;

            DialogService.ShowMessageBox("Multiple instances found",
                "It looks like there are multiple running instances of Artemis. " +
                "This can cause issues, especially with CS:GO and Dota2. " +
                "If so, please make sure Artemis isn't already running");
        }

        private async void CheckKeyboardState()
        {
            while (!_shellViewModel.IsActive)
                await Task.Delay(200);

            NotifyOfPropertyChange(() => CanHideWindow);
            NotifyOfPropertyChange(() => CanToggleEnabled);

            var dialog = await DialogService.ShowProgressDialog("Enabling keyboard",
                "Artemis is still busy trying to enable your last used keyboard. " +
                "Please wait while the process completes");
            dialog.SetIndeterminate();

            while (MainManager.DeviceManager.ChangingKeyboard)
                await Task.Delay(10);

            NotifyOfPropertyChange(() => CanHideWindow);
            NotifyOfPropertyChange(() => CanToggleEnabled);

            try
            {
                await dialog.CloseAsync();
            }
            catch (InvalidOperationException)
            {
                // Occurs when window is closed again, can't find a proper check for this
            }
        }

        public void HideWindow()
        {
            if (!CanHideWindow)
                return;

            _shellViewModel.TryClose();

            NotifyOfPropertyChange(() => CanShowWindow);
            NotifyOfPropertyChange(() => CanHideWindow);
        }

        public void ExitApplication()
        {
            MainManager.Dispose();
            Application.Current.Shutdown();
        }
    }
}