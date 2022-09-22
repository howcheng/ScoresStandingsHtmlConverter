using System.Reflection;
using CommandLine;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using GoogleOAuthCliClient;
using GoogleSheetsHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Omu.ValueInjecter;
using ScoresStandingsHtmlConverter.Services;

namespace ScoresStandingsHtmlConverter.Console
{
	public class Program
	{
		private static IConfigurationRoot? Configuration;
		private static GoogleCredential? GoogleCredential;

		public static async Task Main(string[] args)
		{
			string? outputPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().Location).LocalPath);
			Configuration = new ConfigurationBuilder()
				.SetBasePath(outputPath)
				.AddJsonFile("appSettings.json")
				.Build();

			DisplayCommandLineArguments(args);
			if (args.Length > 0)
				await Parser.Default.ParseArguments<Arguments>(args).WithParsedAsync(Run);
			else
				await Run(new Arguments());
		}

		private static void DisplayCommandLineArguments(string[] args)
		{
			System.Console.WriteLine("Arguments:");
			System.Console.WriteLine("--------------");
			for (int index = 0; index < args.Length; index++)
			{
				System.Console.WriteLine($"{index}\t\t\t{args[index]}");
			}
			System.Console.WriteLine("--------------");
		}

		private static IServiceProvider BuildSharedServices(AppSettings settings)
		{
			IServiceCollection services = new ServiceCollection();
			services.AddLogging(builder =>
			{
				builder.ClearProviders();
				builder.AddDebug();
				builder.AddConsole();
			});
			services.AddSingleton(settings);
			services.AddSingleton(GoogleCredential!);
			services.AddSingleton<ISheetsClient>(provider => ActivatorUtilities.CreateInstance<SheetsClient>(provider, settings.SheetId!));
			return services.BuildServiceProvider();
		}

		private static IServiceProvider BuildServiceProvider(IServiceProvider provider, string division)
		{
			AppSettings settings = provider.GetRequiredService<AppSettings>();

			IServiceCollection services = new ServiceCollection();
			services.AddLogging(builder =>
			{
				builder.ClearProviders();
				builder.AddDebug();
				builder.AddConsole();
			});
			services.AddSingleton(settings);
			services.AddSingleton(provider.GetRequiredService<ISheetsClient>());

			services.AddTransient<IFileWriter, FileWriter>();

			if (!settings.NoScores)
			{
				services.AddTransient<IScoresExtractor, ScoresExtractor>();
				services.AddTransient<IScoresHtmlWriter, ScoresHtmlWriter>();
				services.AddTransient<IService>(p => ActivatorUtilities.CreateInstance<ScoresService>(p, division));
			}
			if (!settings.NoStandings)
			{
				services.AddTransient<IStandingsExtractor, StandingsExtractor>();
				services.AddTransient<IStandingsHtmlWriter, StandingsHtmlWriter>();
				services.AddTransient<IService>(p => ActivatorUtilities.CreateInstance<StandingsService>(p, division));
			}

			return services.BuildServiceProvider();
		}

		private static async Task Run(Arguments args)
		{
			if (args.NoScores && args.NoStandings)
			{
				System.Console.WriteLine("You started me but told me not to do scores or standings. What was the point? Press any key to exit.");
				System.Console.ReadKey();
				return;
			}

			AppSettings settings = new AppSettings();
			settings.InjectFrom(args);
			// do OAuth first -- this has to be in a separate service collection because we need to get the GoogleCredential
			Configuration!.GetSection(nameof(AppSettings)).Bind(settings);

			IServiceCollection oauthServices = new ServiceCollection();
			oauthServices.AddOptions();
			oauthServices.AddOAuthChecker(options =>
			{
				options.SecretsPath = settings.ClientSecretsPath;
				options.BrowserArguments = Configuration!.GetSection("OAuth")[nameof(OAuthCheckerOptions.BrowserArguments)];
				options.Scopes.Add(SheetsService.ScopeConstants.DriveFile);
			});
			IServiceProvider oauthProvider = oauthServices.BuildServiceProvider();
			IOAuthChecker checker = oauthProvider.GetRequiredService<IOAuthChecker>();
			if (await checker.IsOAuthRequired())
				await checker.DoOAuth();

			// load the credential
			GoogleCredential = GoogleCredential.FromAccessToken(checker.AccessToken);

			IServiceProvider sharedProvider = BuildSharedServices(settings);

			ILogger<Program> log = sharedProvider.GetRequiredService<ILogger<Program>>();
			log.LogInformation("Creating {0}{1}{2} for {3:M/d} for the following divisions: {4}"
				, !settings.NoScores ? "scores" : string.Empty
				, !settings.NoScores && !settings.NoStandings ? " and " : string.Empty
				, !settings.NoScores ? "standings" : string.Empty
				, settings.DateOfRound
				, settings.Divisions!.Aggregate((s1, s2) => $"{s1}, {s2}")
				);

			foreach (string division in settings.Divisions!)
			{
				IServiceProvider provider = BuildServiceProvider(sharedProvider, division);
				IEnumerable<IService> services = provider.GetServices<IService>();
				foreach (IService service in services)
				{
					await service.Execute();
				}
			}

			log.LogInformation("All done!");
		}
	}
}
