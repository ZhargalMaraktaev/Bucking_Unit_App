using Bucking_Unit_App._1C_Controller;
using Bucking_Unit_App.COM_Controller;
using Bucking_Unit_App.Interfaces;
using Bucking_Unit_App.Models;
using Bucking_Unit_App.Services;
using Bucking_Unit_App.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO.Ports;
using System.Windows;

namespace Bucking_Unit_App
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;

        public App()
        {
            _serviceProvider = ConfigureServices();
        }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
            services.AddSingleton<IConfiguration>(configuration);

            // Логирование
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });

            // Регистрация COMControllerParamsModel
            services.AddSingleton<COMControllerParamsModel>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var logger = sp.GetService<ILogger<COMController>>();
                try
                {
                    var comConfig = config.GetSection("COMController");
                    var paramsModel = new COMControllerParamsModel(
                        portName: comConfig["PortName"] ?? "COM3",
                        baudRate: int.Parse(comConfig["BaudRate"] ?? "9600"),
                        parity: Enum.Parse<Parity>(comConfig["Parity"] ?? "None"),
                        dataBits: int.Parse(comConfig["DataBits"] ?? "8"),
                        stopBits: Enum.Parse<StopBits>(comConfig["StopBits"] ?? "One")
                    );
                    logger?.LogInformation("COMControllerParamsModel created with PortName: {PortName}, BaudRate: {BaudRate}", paramsModel.PortName, paramsModel.BaudRate);
                    return paramsModel;
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to create COMControllerParamsModel.");
                    throw;
                }
            });

            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? "Data Source=192.168.11.222,1433;Initial Catalog=Pilot;User ID=UserNotTrend;Password=NotTrend";

            services.AddDbContextFactory<YourDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddSingleton<DataAccessLayer>(sp => new DataAccessLayer(
                sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection"),
                sp.GetService<ILogger<DataAccessLayer>>()));
            services.AddSingleton<IStatsRepository>(sp => sp.GetRequiredService<DataAccessLayer>());
            services.AddSingleton<IEmployeeRepository>(sp => sp.GetRequiredService<DataAccessLayer>());

            services.AddSingleton<OperatorService>();
            services.AddSingleton<COMController>();
            services.AddSingleton<Controller1C>();
            services.AddSingleton<StatsService>();
            services.AddSingleton<MainWindow>();

            return services.BuildServiceProvider();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var logger = _serviceProvider.GetService<ILogger<App>>();
            logger?.LogInformation("Bucking_Unit_App starting up.");

            // Проверка подключения к базе данных
            try
            {
                using var db = _serviceProvider.GetService<IDbContextFactory<YourDbContext>>().CreateDbContext();
                await db.Database.CanConnectAsync();
                logger?.LogInformation("Database connection successful.");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to connect to database.");
                MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            // Проверяем аргументы командной строки
            string operatorId = null;
            if (e.Args.Length > 0)
            {
                foreach (var arg in e.Args)
                {
                    if (arg.StartsWith("operatorId=", StringComparison.OrdinalIgnoreCase))
                    {
                        operatorId = arg.Substring("operatorId=".Length);
                        logger?.LogInformation("Received operatorId from command line: {OperatorId}", operatorId);
                        break;
                    }
                }
            }

            var operatorService = _serviceProvider.GetService<OperatorService>();
            if (!string.IsNullOrEmpty(operatorId))
            {
                try
                {
                    using var db = _serviceProvider.GetService<IDbContextFactory<YourDbContext>>().CreateDbContext();
                    var employee = await db.dic_SKUD
                        .Where(s => s.TabNumber == operatorId)
                        .Select(s => new { s.IdCard, s.TORoleId })
                        .FirstOrDefaultAsync();

                    if (employee != null)
                    {
                        logger?.LogInformation("Found CardId: {CardId} for operatorId: {OperatorId}", employee.IdCard, operatorId);
                        await operatorService.AuthenticateOperatorAsync(employee.IdCard, true, DateTime.Now);
                    }
                    else
                    {
                        logger?.LogWarning("No employee found for operatorId: {OperatorId}. Check database or operatorId validity.", operatorId);
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Error retrieving employee data for operatorId: {OperatorId}", operatorId);
                    MessageBox.Show($"Ошибка при получении данных сотрудника: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                logger?.LogInformation("No operatorId provided. Waiting for card scan.");
            }

            try
            {
                var mainWindow = _serviceProvider.GetService<MainWindow>();
                if (mainWindow == null)
                {
                    logger?.LogError("Failed to resolve MainWindow from service provider.");
                    MessageBox.Show("Ошибка: не удалось инициализировать главное окно приложения.", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown();
                    return;
                }
                MainWindow = mainWindow;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error initializing or showing MainWindow.");
                MessageBox.Show($"Ошибка при запуске главного окна: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}