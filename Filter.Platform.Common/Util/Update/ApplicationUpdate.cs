﻿/*
* Copyright © 2017-2018 Cloudveil Technology Inc.  
* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Filter.Platform.Common;

namespace Citadel.Core.Windows.Util.Update
{
    public enum UpdateKind
    {
        MsiInstaller,
        ZipFile,
        ExePackage
    }

    public class ApplicationUpdate
    {
        public DateTime DatePublished
        {
            get;
            private set;
        }

        public string Title
        {
            get;
            private set;
        }

        public string HtmlBody
        {
            get;
            private set;
        }

        public Version CurrentVersion
        {
            get;
            private set;
        }

        public Version UpdateVersion
        {
            get;
            private set;
        }

        private Uri DownloadLink
        {
            get;
            set;
        }

        public UpdateKind Kind
        {
            get;
            private set;
        }

        public string UpdaterArguments
        {
            get;
            private set;
        }

        public string UpdateFileLocalPath
        {
            get;
            private set;
        }

        public bool IsRestartRequired
        {
            get;
            private set;
        }

        private IPathProvider paths;

        public ApplicationUpdate(DateTime datePublished, string title, string htmlBody, Version currentVersion, Version updateVersion, Uri downloadLink, UpdateKind kind, string updaterArguments, bool isRestartRequired)
        {
            paths = PlatformTypes.New<IPathProvider>();

            DatePublished = datePublished;
            Title = title = title != null ? title : string.Empty;
            HtmlBody = htmlBody = htmlBody != null ? htmlBody : string.Empty;
            CurrentVersion = currentVersion;
            UpdateVersion = updateVersion;
            DownloadLink = downloadLink;
            Kind = kind;
            UpdaterArguments = updaterArguments != null ? updaterArguments : string.Empty;
            IsRestartRequired = isRestartRequired;

            var targetDir = Path.Combine(paths.ApplicationDataFolder, "updates");

            if(!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // XXX TODO - Problems on non-windows plats? Do we care?
            UpdateFileLocalPath = Path.Combine(targetDir, Path.GetFileName(DownloadLink.LocalPath));
        }

        public async Task DownloadUpdate()
        {
            using(var cli = new WebClient())
            {
                await cli.DownloadFileTaskAsync(DownloadLink, UpdateFileLocalPath);
            }
        }

        /// <summary>
        /// Begins the external installation after a N second delay specified. 
        /// </summary>
        /// <param name="secondDelay">
        /// The number of seconds to wait before starting the actual update.
        /// </param>
        /// <exception cref="Exception">
        /// If the file designated at UpdateFileLocalPath does not exist at the time of this call,
        /// this method will throw.
        /// </exception>
        public void BeginInstallUpdateDelayed(int secondDelay = 5, bool restartApplication = true)
        {
            // TODO: Implement cross platform stuff.

            if(!File.Exists(UpdateFileLocalPath))
            {
                throw new Exception("Target update installer does not exist at the expected location.");
            }

            ProcessStartInfo updaterStartupInfo;

            var systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);

            if(restartApplication)
            {
                var executingProcess = Process.GetCurrentProcess().MainModule.FileName;
                var args = string.Format("\"{0}\\cmd.exe\" /C TIMEOUT {1} && \"{2}\\msiexec\" /I \"{3}\" {4} && \"{5}\"", systemFolder, secondDelay, systemFolder, UpdateFileLocalPath, UpdaterArguments, executingProcess);
                Console.WriteLine(args);
                updaterStartupInfo = new ProcessStartInfo(args);
            }
            else
            {
                var args = string.Format("\"{0}\\cmd.exe\" /C TIMEOUT {1} && \"{2}\\msiexec\" /I \"{3}\" {4}", systemFolder, secondDelay, systemFolder, UpdateFileLocalPath, UpdaterArguments);
                Console.WriteLine(args);
                updaterStartupInfo = new ProcessStartInfo(args);
            }

            updaterStartupInfo.UseShellExecute = false;
            //updaterStartupInfo.WindowStyle = ProcessWindowStyle.Hidden;
            updaterStartupInfo.CreateNoWindow = true;
            updaterStartupInfo.Arguments = UpdaterArguments;
            Process.Start(updaterStartupInfo);
        }
    }
}