using Microsoft.Extensions.DependencyInjection;

namespace PresenceEtudiant.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Services (un seul exemplaire pour toute l'appli)
        builder.Services.AddSingleton<Session>();
        builder.Services.AddSingleton<LocalDb>();
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<SyncService>();
        builder.Services.AddSingleton<AppelService>();

        // Écrans et leurs ViewModels (un nouvel exemplaire à chaque ouverture)
        builder.Services.AddTransient<ConnexionViewModel>();
        builder.Services.AddTransient<ConnexionPage>();
        builder.Services.AddTransient<InscriptionViewModel>();
        builder.Services.AddTransient<InscriptionPage>();
        builder.Services.AddTransient<ClasseViewModel>();
        builder.Services.AddTransient<ClassePage>();
        builder.Services.AddTransient<AppelViewModel>();
        builder.Services.AddTransient<AppelPage>();
        builder.Services.AddTransient<JustificationViewModel>();
        builder.Services.AddTransient<JustificationPage>();
        builder.Services.AddTransient<AssiduiteViewModel>();
        builder.Services.AddTransient<AssiduitePage>();
        builder.Services.AddTransient<HistoriqueViewModel>();
        builder.Services.AddTransient<HistoriquePage>();

        var app = builder.Build();

        // Dès que le réseau revient, on envoie les appels en attente
        var sync = app.Services.GetRequiredService<SyncService>();
        Connectivity.Current.ConnectivityChanged += async (_, _) => await sync.SynchroniserAsync();

#if WINDOWS
        // Sur PC : le serveur démarre tout seul avec l'application
        _ = ServeurLocal.DemarrerAsync();
#endif
        return app;
    }
}
