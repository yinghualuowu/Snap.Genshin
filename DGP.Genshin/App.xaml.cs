﻿using DGP.Genshin.Common.Core.DependencyInjection;
using DGP.Genshin.Common.Exceptions;
using DGP.Genshin.Common.Extensions.System;
using DGP.Genshin.Helpers.Notifications;
using DGP.Genshin.Services;
using DGP.Genshin.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Uwp.Notifications;
using ModernWpf;
using System;
using System.IO;
using System.Reflection;
using System.Windows;

namespace DGP.Genshin
{
    public partial class App : Application
    {
        private readonly ToastNotificationHandler toastNotificationHandler = new();
        private readonly SingleInstanceChecker singleInstanceChecker = new("Snap.Genshin");
        //private IHost host;
        public App()
        {
            Services = ConfigureServices();
        }

        /// <summary>
        /// Gets the current <see cref="App"/> instance in use
        /// </summary>
        public new static App Current => (App)Application.Current;

        #region Dependency Injection
        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
        /// </summary>
        public IServiceProvider Services { get; }

        /// <summary>
        /// Configures the services for the application.
        /// </summary>
        private static IServiceProvider ConfigureServices()
        {
            ServiceCollection services = new();
            ScanServices(services, typeof(App));
            ScanServices(services, typeof(MiHoYoAPI.ScanEntry));
            ScanServices(services, typeof(YoungMoeAPI.ScanEntry));
            return services.BuildServiceProvider();
        }
        public static TService GetService<TService>()
        {
            return Current.Services.GetService<TService>() ?? throw new SnapGenshinInternalException("无法找到对应的服务");
        }
        public static TViewModel GetViewModel<TViewModel>()
        {
            return GetService<TViewModel>();
        }
        private static void ScanServices(ServiceCollection services, Type entryType)
        {
            foreach (Type type in entryType.Assembly.GetTypes())
            {
                if (type.GetCustomAttribute<ServiceAttribute>() is ServiceAttribute serviceAttr)
                {
                    _ = serviceAttr.ServiceType switch
                    {
                        ServiceType.Singleton => services.AddSingleton(type, serviceAttr.ImplmentationInterfaceType),
                        ServiceType.Transient => services.AddTransient(type, serviceAttr.ImplmentationInterfaceType),
                        _ => throw new SnapGenshinInternalException($"未知的服务类型{type}"),
                    };
                }
                if (type.GetCustomAttribute<ViewModelAttribute>() is ViewModelAttribute viewModelAttr)
                {
                    _ = viewModelAttr.ViewModelType switch
                    {
                        ViewModelType.Singleton => services.AddSingleton(type),
                        ViewModelType.Transient => services.AddTransient(type),
                        _ => throw new SnapGenshinInternalException($"未知的视图模型类型{type}"),
                    };
                }
            }
        }
        #endregion

        #region LifeCycle
        protected override void OnStartup(StartupEventArgs e)
        {
            EnsureWorkingPath();
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            //handle notification activation
            SetupToastNotificationHandling();
            singleInstanceChecker.Ensure(Current);
            //file operation starts
            this.Log($"Snap Genshin - {Assembly.GetExecutingAssembly().GetName().Version}");
            GetService<SettingService>().Initialize();
            //app theme
            SetAppTheme();
            //open main window
            base.OnStartup(e);
        }

        private void SetupToastNotificationHandling()
        {
            if (!ToastNotificationManagerCompat.WasCurrentProcessToastActivated())
            {
                //remove toast last time not cleared if it's manually launched
                ToastNotificationManagerCompat.History.Clear();
            }
            ToastNotificationManagerCompat.OnActivated += toastNotificationHandler.OnActivatedByNotification;
        }

        /// <summary>
        /// set working dir while launch by windows autorun
        /// </summary>
        private void EnsureWorkingPath()
        {
            string path = AppContext.BaseDirectory;
            string? workingPath = Path.GetDirectoryName(path);
            if (workingPath is not null)
            {
                Environment.CurrentDirectory = workingPath;
            }
        }
        private void SetAppTheme()
        {
            ThemeManager.Current.ApplicationTheme =
                GetService<SettingService>().GetOrDefault(Setting.AppTheme, null, Setting.ApplicationThemeConverter);
        }
        protected override void OnExit(ExitEventArgs e)
        {
            if (!singleInstanceChecker.IsExitDueToSingleInstanceRestriction)
            {
                GetService<SettingService>().UnInitialize();
                GetService<MetadataViewModel>().UnInitialize();
                this.Log($"Exit code:{e.ApplicationExitCode}");
            }
            base.OnExit(e);
        }
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (!singleInstanceChecker.IsEnsureingSingleInstance)
            {
                using (StreamWriter sw = new(File.Create($"{DateTime.Now:yyyy-MM-dd HH-mm-ss}-crash.log")))
                {
                    sw.WriteLine($"Snap Genshin - {Assembly.GetExecutingAssembly().GetName().Version}");
                    sw.Write(e.ExceptionObject);
                }
                //while exit with error OnExit will somehow not triggered
            }
        }
        #endregion
    }
}
