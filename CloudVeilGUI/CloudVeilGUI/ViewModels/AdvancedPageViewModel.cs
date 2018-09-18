﻿using Filter.Platform.Common.Util;
using Citadel.IPC;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace CloudVeilGUI.ViewModels
{
    public class AdvancedPageViewModel
    {
        private Command deactivateCommand;
        public Command DeactivateCommand
        {
            get
            {
                if(deactivateCommand == null)
                {
                    deactivateCommand = new Command(() =>
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
                        catch(Exception ex)
                        {
                            LoggerUtil.RecursivelyLogException(LoggerUtil.GetAppWideLogger(), ex);
                        }
                    });
                }

                return deactivateCommand;
            }
        }
    }
}
