﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Artemis.DAL;
using Artemis.DeviceProviders;
using Artemis.Events;
using Artemis.Services;
using Artemis.Settings;
using MahApps.Metro.Controls.Dialogs;
using Ninject;
using Ninject.Extensions.Logging;

namespace Artemis.Managers
{
    /// <summary>
    ///     Manages the keyboard providers
    /// </summary>
    public class DeviceManager
    {
        private readonly GeneralSettings _generalSettings;
        private readonly ILogger _logger;

        public DeviceManager(ILogger logger, List<DeviceProvider> deviceProviders)
        {
            _logger = logger;
            _generalSettings = SettingsProvider.Load<GeneralSettings>();

            KeyboardProviders = deviceProviders.Where(d => d.Type == DeviceType.Keyboard)
                .Cast<KeyboardProvider>().ToList();
            MiceProviders = deviceProviders.Where(d => d.Type == DeviceType.Mouse).ToList();
            HeadsetProviders = deviceProviders.Where(d => d.Type == DeviceType.Headset).ToList();
            GenericProviders = deviceProviders.Where(d => d.Type == DeviceType.Generic).ToList();
            MousematProviders = deviceProviders.Where(d => d.Type == DeviceType.Mousemat).ToList();

            _logger.Info("Intialized DeviceManager with {0} device providers", deviceProviders.Count);
        }

        public List<DeviceProvider> MiceProviders { get; set; }
        public List<DeviceProvider> HeadsetProviders { get; set; }
        public List<DeviceProvider> GenericProviders { get; set; }
        public List<DeviceProvider> MousematProviders { get; set; }

        [Inject]
        public MetroDialogService DialogService { get; set; }

        public List<KeyboardProvider> KeyboardProviders { get; set; }

        public KeyboardProvider ActiveKeyboard { get; set; }

        public bool ChangingKeyboard { get; private set; }
        

        public event EventHandler<KeyboardChangedEventArgs> OnKeyboardChangedEvent;

        /// <summary>
        ///     Enables the last keyboard according to the settings file
        /// </summary>
        public void EnableLastKeyboard()
        {
            _logger.Debug("Getting last keyboard: {0}", _generalSettings.LastKeyboard);
            if (string.IsNullOrEmpty(_generalSettings.LastKeyboard))
                return;

            var keyboard = KeyboardProviders.FirstOrDefault(k => k.Name == _generalSettings.LastKeyboard);
            EnableKeyboard(keyboard);
        }

        /// <summary>
        ///     Enables the given keyboard
        /// </summary>
        /// <param name="keyboardProvider"></param>
        public async void EnableKeyboard(KeyboardProvider keyboardProvider)
        {
            if (keyboardProvider == null)
                throw new ArgumentNullException(nameof(keyboardProvider));

            if (ChangingKeyboard || (ActiveKeyboard?.Name == keyboardProvider.Name))
                return;

            _logger.Debug("Trying to enable keyboard: {0}", keyboardProvider.Name);
            ChangingKeyboard = true;

            // Store the old keyboard so it can be used in the event we're raising later
            var oldKeyboard = ActiveKeyboard;

            // Release the current keyboard
            ReleaseActiveKeyboard();

            // Create a dialog to let the user know Artemis hasn't frozen
            ProgressDialogController dialog = null;
            if (DialogService.GetActiveWindow() != null)
            {
                dialog = await DialogService.ShowProgressDialog("Enabling keyboard",
                    $"Checking if keyboard '{keyboardProvider.Name}' can be enabled...", true);

                // May seem a bit cheesy, but it's tidier to have the animation finish
                await Task.Delay(500);
            }
            dialog?.SetIndeterminate();

            var canEnable = await keyboardProvider.CanEnableAsync(dialog);
            if (!canEnable)
            {
                if (dialog != null)
                    await dialog.CloseAsync();

                DialogService.ShowErrorMessageBox(keyboardProvider.CantEnableText);
                ActiveKeyboard = null;
                _generalSettings.LastKeyboard = null;
                _generalSettings.Save();
                _logger.Warn("Failed enabling keyboard: {0}", keyboardProvider.Name);
                ChangingKeyboard = false;
                return;
            }

            dialog?.SetMessage($"Enabling keyboard: {keyboardProvider.Name}...");

            // Setup the new keyboard
            ActiveKeyboard = keyboardProvider;
            await ActiveKeyboard.EnableAsync(dialog);
            EnableUsableDevices();

            _generalSettings.LastKeyboard = ActiveKeyboard.Name;
            _generalSettings.Save();

            RaiseKeyboardChangedEvent(new KeyboardChangedEventArgs(oldKeyboard, ActiveKeyboard));
            _logger.Debug("Enabled keyboard: {0}", keyboardProvider.Name);

            if (dialog != null)
                await dialog.CloseAsync();

            ChangingKeyboard = false;
        }

        private void EnableUsableDevices()
        {
            foreach (var mouseProvider in MiceProviders)
                mouseProvider.TryEnableAsync();
            foreach (var headsetProvider in HeadsetProviders)
                headsetProvider.TryEnableAsync();
            foreach (var genericProvider in GenericProviders)
                genericProvider.TryEnableAsync();
            foreach (var mousematProviders in MousematProviders)
                mousematProviders.TryEnableAsync();
        }

        /// <summary>
        ///     Releases the active keyboard
        /// </summary>
        /// <param name="save">Whether to save the LastKeyboard (making it null)</param>
        public void ReleaseActiveKeyboard(bool save = false)
        {
            lock (this)
            {
                if (ActiveKeyboard == null)
                    return;

                // Store the old keyboard so it can be used in the event we're raising later
                var oldKeyboard = ActiveKeyboard;

                var releaseName = ActiveKeyboard.Name;
                ActiveKeyboard.Disable();
                ActiveKeyboard = null;

                if (save)
                {
                    _generalSettings.LastKeyboard = null;
                    _generalSettings.Save();
                }

                RaiseKeyboardChangedEvent(new KeyboardChangedEventArgs(oldKeyboard, null));
                _logger.Debug("Released keyboard: {0}", releaseName);
            }
        }

        protected virtual void RaiseKeyboardChangedEvent(KeyboardChangedEventArgs e)
        {
            // I do this in all to avoid a possible race condition
            // https://msdn.microsoft.com/en-us/library/w369ty8x.aspx
            var handler = OnKeyboardChangedEvent;
            handler?.Invoke(this, e);
        }
    }
}