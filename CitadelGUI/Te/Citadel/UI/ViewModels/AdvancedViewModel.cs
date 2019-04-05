﻿/*
* Copyright © 2019 Cloudveil Technology Inc.  
* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/
using Citadel.Core.Windows.Util.Update;
using Citadel.IPC;
using Citadel.IPC.Messages;
using CloudVeil.Windows;
using Filter.Platform.Common.Data.Models;
using Filter.Platform.Common.IPC.Messages;
using Filter.Platform.Common.Types;
using Filter.Platform.Common.Util;
using GalaSoft.MvvmLight.CommandWpf;
using MahApps.Metro.IconPacks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Te.Citadel.UI.Views;

namespace Te.Citadel.UI.ViewModels
{
    public class AdvancedViewModel : BaseCitadelViewModel
    {

        public AdvancedViewModel() : base()
        {
        }

        /// <summary>
        /// Private data member for the public DeactivateCommand property.
        /// </summary>
        private RelayCommand m_deactivationCommand;

        /// <summary>
        /// Private data member for the public ViewLogsCommand property.
        /// </summary>
        private RelayCommand m_viewLogsCommand;

        private RelayCommand m_viewSslExemptionsCommand;

        private string updateText;
        public string UpdateText
        {
            get => updateText;
            set
            {
                updateText = value;
                RaisePropertyChanged(nameof(UpdateText));
            }
        }

        private bool checkingForUpdates;
        public bool CheckingForUpdates
        {
            get => checkingForUpdates;
            set
            {
                checkingForUpdates = value;
                RaisePropertyChanged(nameof(CheckingForUpdates));
                RaisePropertyChanged(nameof(NotCheckingForUpdates));
            }
        }

        public bool NotCheckingForUpdates => !CheckingForUpdates;

        private PackIconFontAwesomeKind updateIcon;
        public PackIconFontAwesomeKind UpdateIcon
        {
            get => updateIcon;
            set
            {
                updateIcon = value;
                RaisePropertyChanged(nameof(UpdateIcon));
            }
        }

        private string updateButtonText = "Check for Updates";
        public string UpdateButtonText
        {
            get => updateButtonText;
            set
            {
                updateButtonText = value;
                RaisePropertyChanged(nameof(UpdateButtonText));
            }
        }

        private Brush updateIconForeground;
        public Brush UpdateIconForeground
        {
            get => updateIconForeground;
            set
            {
                updateIconForeground = value;
                RaisePropertyChanged(nameof(UpdateIconForeground));
            }
        }

        private string updateLastCheckedText;
        public string UpdateLastCheckedText
        {
            get => updateLastCheckedText;
            set
            {
                updateLastCheckedText = value;
                RaisePropertyChanged(nameof(UpdateLastCheckedText));
            }
        }

        private string m_errorText = "";
        public string ErrorText
        {
            get { return m_errorText; }
            set
            {
                m_errorText = value;
                RaisePropertyChanged(nameof(ErrorText));
            }
        }

        private bool shouldUpdateButtonInstall = false;

        private bool m_isUpdateButtonEnabled = true;
        public bool IsUpdateButtonEnabled
        {
            get { return m_isUpdateButtonEnabled; }
            set
            {
                m_isUpdateButtonEnabled = value;
                RaisePropertyChanged(nameof(IsUpdateButtonEnabled));
            }
        }

        private RelayCommand checkForUpdatesCommand;
        public RelayCommand CheckForUpdatesCommand
        {
            get
            {
                if(checkForUpdatesCommand == null)
                {
                    checkForUpdatesCommand = new RelayCommand(() =>
                    {
                        CheckingForUpdates = true;
                        ErrorText = "";

                        if(shouldUpdateButtonInstall)
                        {
                            Task.Run(() =>
                            {
                                IPCClient.Default.Request<object, ApplicationUpdate>(IpcCall.Update, null).OnReply((h, msg) =>
                                {
                                    (CitadelApp.Current as CitadelApp).BeginUpdateRequest(msg.Data);
                                    return true;
                                });
                            });
                        }
                        else
                        {
                            Task.Run(() =>
                            {
                                IPCClient.Default.Request(IpcCall.CheckForUpdates).OnReply((h, msg) => OnCheckForUpdates(msg.As<UpdateCheckInfo>()));
                            });
                        }
                    });
                }

                return checkForUpdatesCommand;
            }
        }

        private static string dateTimeToString(DateTime? dt)
        {
            if(dt == null)
            {
                return "N/A";
            }
            else
            {
                return $"{dt.Value.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern + " " + CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern)}";
            }
        }

        private static string lastChecked(DateTime? dt)
        {
            return $"Last Checked: {dateTimeToString(dt)}";
        }

        public bool OnCheckForUpdates(IpcMessage<UpdateCheckInfo> msg)
        {
            CheckingForUpdates = false;

            switch(msg.Data.CheckResult)
            {
                case null:
                case UpdateCheckResult.CheckFailed:
                    UpdateIcon = PackIconFontAwesomeKind.TimesSolid;
                    UpdateIconForeground = Brushes.Red;
                    UpdateText = "Update Check Failed";
                    UpdateLastCheckedText = lastChecked(msg.Data.LastChecked);
                    UpdateButtonText = "Check for Updates";
                    shouldUpdateButtonInstall = false;
                    break;

                case UpdateCheckResult.UpToDate:
                    UpdateIcon = PackIconFontAwesomeKind.CheckSolid;
                    UpdateIconForeground = Brushes.DarkGreen;
                    UpdateText = "You're up to date";
                    UpdateLastCheckedText = lastChecked(msg.Data.LastChecked);
                    UpdateButtonText = "Check for Updates";
                    shouldUpdateButtonInstall = false;
                    break;

                case UpdateCheckResult.UpdateAvailable:
                    UpdateIcon = PackIconFontAwesomeKind.ExclamationCircleSolid;
                    UpdateIconForeground = Brushes.Red;
                    UpdateText = "Update Available";
                    UpdateLastCheckedText = lastChecked(msg.Data.LastChecked);
                    UpdateButtonText = "Install";
                    shouldUpdateButtonInstall = true;
                    break;

                case UpdateCheckResult.UpdateFailed:
                    UpdateIcon = PackIconFontAwesomeKind.TimesSolid;
                    UpdateIconForeground = Brushes.Red;
                    UpdateText = "Update Failed";
                    UpdateLastCheckedText = $"Last attempt: {dateTimeToString(msg.Data.LastChecked)}";
                    UpdateButtonText = "Check for Updates";
                    shouldUpdateButtonInstall = false;
                    break;
            }

            return true;
        }

        public bool OnSettingsSynchronized(IpcMessage<ConfigCheckInfo> msg)
        {
            SynchronizingSettings = false;

            switch(msg.Data.CheckResult)
            {

                case ConfigUpdateResult.ErrorOccurred:
                case ConfigUpdateResult.NoInternet:
                case null:
                    SyncSettingsIcon = PackIconFontAwesomeKind.TimesSolid;
                    SyncSettingsIconForeground = Brushes.Red;
                    SyncSettingsText = "Error occurred";
                    SettingsLastCheckedText = lastChecked(msg.Data.LastChecked);
                    break;

                case ConfigUpdateResult.Updated:
                case ConfigUpdateResult.UpToDate:
                    SyncSettingsIcon = PackIconFontAwesomeKind.CheckSolid;
                    SyncSettingsIconForeground = Brushes.DarkGreen;
                    SyncSettingsText = "Settings up to date";
                    SettingsLastCheckedText = lastChecked(msg.Data.LastChecked);
                    break;
            }

            return true;
        }

        private string syncSettingsText;
        public string SyncSettingsText
        {
            get => syncSettingsText;
            set
            {
                syncSettingsText = value;
                RaisePropertyChanged(nameof(SyncSettingsText));
            }
        }

        private bool synchronizingSettings;
        public bool SynchronizingSettings
        {
            get => synchronizingSettings;
            set
            {
                synchronizingSettings = value;
                RaisePropertyChanged(nameof(SynchronizingSettings));
                RaisePropertyChanged(nameof(NotSynchronizingSettings));
            }
        }

        public bool NotSynchronizingSettings => !SynchronizingSettings;

        private PackIconFontAwesomeKind syncSettingsIcon;
        public PackIconFontAwesomeKind SyncSettingsIcon
        {
            get => syncSettingsIcon;
            set
            {
                syncSettingsIcon = value;
                RaisePropertyChanged(nameof(SyncSettingsIcon));
            }
        }

        private Brush syncSettingsIconForeground;
        public Brush SyncSettingsIconForeground
        {
            get => syncSettingsIconForeground;
            set
            {
                syncSettingsIconForeground = value;
                RaisePropertyChanged(nameof(SyncSettingsIconForeground));
            }
        }

        private string settingsLastCheckedText;
        public string SettingsLastCheckedText
        {
            get => settingsLastCheckedText;
            set
            {
                settingsLastCheckedText = value;
                RaisePropertyChanged(nameof(SettingsLastCheckedText));
            }
        }

        private string syncErrorText;
        public string SyncErrorText
        {
            get => syncErrorText;
            set
            {
                syncErrorText = value;
                RaisePropertyChanged(nameof(SyncErrorText));
            }
        }

        private RelayCommand syncSettingsCommand;
        public RelayCommand SyncSettingsCommand
        {
            get
            {
                if(syncSettingsCommand == null)
                {
                    syncSettingsCommand = new RelayCommand(() =>
                    {
                        SynchronizingSettings = true;
                        SyncErrorText = "";

                        Task.Run(() =>
                        {
                            IPCClient.Default.Request(IpcCall.SynchronizeSettings).OnReply((h, msg) => OnSettingsSynchronized(msg.As<ConfigCheckInfo>()));
                        });
                    });
                }

                return syncSettingsCommand;
            }
        }

        public RelayCommand ViewLogsCommand
        {
            get
            {
                if (m_viewLogsCommand == null)
                {
                    m_viewLogsCommand = new RelayCommand(() =>
                    {
                        // Scan all Nlog log targets
                        var logDir = string.Empty;

                        var targets = NLog.LogManager.Configuration.AllTargets;

                        foreach (var target in targets)
                        {
                            if (target is NLog.Targets.FileTarget)
                            {
                                var fTarget = (NLog.Targets.FileTarget)target;
                                var logEventInfo = new NLog.LogEventInfo { TimeStamp = DateTime.Now };
                                var fName = fTarget.FileName.Render(logEventInfo);

                                if (!string.IsNullOrEmpty(fName) && !string.IsNullOrWhiteSpace(fName))
                                {
                                    logDir = Directory.GetParent(fName).FullName;
                                    break;
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(logDir) || string.IsNullOrWhiteSpace(logDir))
                        {
                            // Fallback, just in case.
                            logDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        }

                        // Call process start with the dir path, explorer will handle it.
                        Process.Start(logDir);
                    });
                }

                return m_viewLogsCommand;
            }
        }

        /// <summary>
        /// Command to run a deactivation request for the current authenticated user.
        /// </summary>
        public RelayCommand RequestDeactivateCommand
        {
            get
            {
                if (m_deactivationCommand == null)
                {
                    m_deactivationCommand = new RelayCommand((Action)(() =>
                    {
                        try
                        {
                            Task.Run(() =>
                            {
                                using (var ipcClient = new IPCClient())
                                {
                                    ipcClient.ConnectedToServer = () =>
                                    {
                                        ipcClient.RequestDeactivation();
                                    };

                                    ipcClient.WaitForConnection();
                                    Task.Delay(3000).Wait();
                                }
                            });
                        }
                        catch (Exception e)
                        {
                            LoggerUtil.RecursivelyLogException(m_logger, e);
                        }
                    }));
                }

                return m_deactivationCommand;
            }
        }
    }
}
