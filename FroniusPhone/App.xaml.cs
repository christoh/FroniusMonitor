﻿using De.Hochstaetter.Fronius;
using De.Hochstaetter.Fronius.Models.Settings;
using Microsoft.Maui.Controls;

namespace FroniusPhone;

public partial class App
{
    private readonly Settings settings;
    private readonly IDataCollectionService dataCollectionService;
    private readonly IAesKeyProvider aesKeyProvider;
    private readonly AppShell shell;

    public static string PerUserDataDir => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    public static string SettingsFileName => Path.Combine(PerUserDataDir, "Settings.fms");


    public App(AppShell shell, Settings settings, IAesKeyProvider aesKeyProvider, IDataCollectionService dataCollectionService)
    {
            this.settings = settings;
            this.dataCollectionService = dataCollectionService;
            this.aesKeyProvider = aesKeyProvider;
            InitializeComponent();
            MainPage = this.shell = shell;
        }

    protected override void OnHandlerChanged()
    {
            try
            {
                if (Handler.MauiContext?.Services != null)
                {
                    IoC.Update(Handler.MauiContext.Services);
                }
            }
            finally
            {
                base.OnHandlerChanged();
            }
        }

    protected override async void OnStart()
    {
            try
            {
                WebConnection.Aes.Key = aesKeyProvider.GetAesKey();

                try
                {
                    await settings.LoadAsync().ConfigureAwait(false);
                }
                catch
                {
                    shell.NeedInitialSettings = true;
                }

                dataCollectionService.FroniusUpdateRate = settings.FroniusUpdateRate;
                await dataCollectionService.Start(settings.FroniusConnection, settings.FroniusConnection2, settings.FritzBoxConnection, settings.WattPilotConnection).ConfigureAwait(false);
            }
            finally
            {
                base.OnStart();
            }
        }

    protected override void OnSleep()
    {
        try
        {
            #if ANDROID || IOS
            dataCollectionService.Stop();
            #endif
        }
        finally
        {
            base.OnSleep();
        }
    }

    protected override void OnResume()
    {
        base.OnResume();
        #if ANDROID || IOS
        _ = dataCollectionService.Start(settings.FroniusConnection, settings.FroniusConnection2, settings.FritzBoxConnection, settings.WattPilotConnection);
        #endif
    }
}