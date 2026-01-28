Муфтонаворот (Bucking unit app)
Что это? Приложение для контроля производства на станке муфтонаворота. Позволяет операторам отслеживать свою продуктивность и простои за смену, месяц как индивидуально для каждого оператора, так и в целом для каждой производственной смены (А,Б,В,Г). Приложение строит графики крутящего момента в реальном времени и позволяет настраивать параметры самого станка.
Где находится? Компьютер на станке участка НОТ, муфтонаворот 3. Размножается при помощи ClickOnce.
Кому надо? Операторы станков с программным управлением, мастера.
Как работает?
	Интерфейс представляет с собой окно с личной и общей статистикой слева и графиком справа, элементами управления циклом обновления графика, его сохранения, остановки цикла. Кнопка «Показать параметры ПЛК» позволяет открыть окно для управления крутящим моментом самого станка. При этом перед началом каждой смены открывается приложение техосмотра оборудования.
 
Рис. 1 Пользовательский интерфейс после авторизации.
 
Рис. 2 Окно настройки ПЛК.

После проведения технического осмотра и отметке в программе техосмотра приложение ТО автоматически закрывается и приложение муфтонаворота возобновляет свою работу. Оператор вставляет свой пропуск в считыватель и приложение вытягивает данные по оператору либо из БД, либо из 1с, либо обновляет данные в БД на актуальные. Далее оператор начинает работать согласно плану, который указывается в статистике и приложение начинает учет накрученных муфт и простоя на станке.
Также можно зафиксировать себя до конца смены и не держать всю смену пропуск в считывателе что сильно облегчает жизнь операторам. При вставке другого пропуска в считыватель фиксация сбрасывается и авторизуется новый оператор либо слесарь.
Описание приложения:
Bucking_Unit_App — это desktop-приложение на базе WPF (Windows Presentation Foundation) для управления и мониторинга муфтонаворота в производственной среде. Приложение интегрируется с:
•	Siemens PLC (через библиотеку Sharp7) для чтения/записи параметров оборудования (например, крутящий момент, обороты).
•	COM-считывателем карт-пропусков для аутентификации операторов (SKUD-система).
•	1C-системой для получения данных о сотрудниках.
•	MS SQL Server (базами данных Pilot и Runtime) для хранения и извлечения статистики, сотрудников и исторических данных.
•	Графиками (используя LiveChartsCore) для визуализации данных о крутящем моменте (torque) и оборотах.
Цель приложения:
•	Аутентификация операторов через карты.
•	Мониторинг и обновление статистики (простои, муфты, планы).
•	Чтение/запись параметров ПЛК.
•	Построение графиков реального времени и исторических данных.
•	Генерация отчетов (PDF) и экспорт данных.
Структура проекта:
Bucking_Unit_App
├─ App.xaml / App.xaml.cs		← точка входа приложения
├─ appsettings.json			← настройки и строки подключения к БД и ПЛК
├─ Controllers			← контроллеры
│  └─ 1C_Controller			← контроллер 1с
│     ├─ Controller1C.cs
│     └─ employee_data.xml		← шаблон xml запроса в 1c.
├─ COM_Controller			← COM контроллер.
│  ├─ COMController.cs
│  └─ COMEventArgs.cs		← аргументы событий.
├─ Interfaces				← интерфейсы
│  ├─DataAccessLayer.cs		← класс для работы с БД
│  ├─ IEmployeeRepository.cs	← интерфейс для работы с данными сотрудника
│  └─ IStatsRepository.cs		← интерфейс для работы со статистикой
├─ Models
│  ├─ Employee1CModel.cs		← модель данных сотрудника для запроса в 1с
│  ├─ DbContext.cs                         ← контекст из моделей БД для работы EntityFramework
│  ├─ COMControllerParamsModel.cs	← модель параметров COM контроллера.
│  ├─ InspectionWorkModels	← модели для работы с ТО
├─ SiemensPLC			← Библиотека для работы с ПЛК
├─ Services				← сервисы
│  ├─ StatsService.cs		← сервис статистики
│  ├─ OperatorService.cs		← сервис для работы с сотрудниками
│  ├─ GraphService.cs		← сервис для построения графиков
│  ├─ ReadFromPLC.cs		← сервис для чтения из ПЛК
│  └─ WriteToPLC.cs		← сервис для записи в ПЛК
├─ Utilities
│  └─ TaskExtensions.cs		← расширение для таймаута задач
├─ Views
│  ├─ MainWindow.xaml / .cs	← главное окно приложения
│  └─ PLCDataWindow.xaml / .cs	← окно настройки параметров ПЛК
│  ├─ GradientBackgroundConverter.cs← градиент фона на основе разницы план/факт.
│  ├─ ComparePlanFactConverter.cs ← конвертер для сравнения план/факт (для UI)
│  └─ MaxPlanFactDifferenceConverter.cs←определение максимальной разницы план/факт.
└─ logs/                                (создаётся в runtime)
Приложение использует Dependency Injection (через Microsoft.Extensions.DependencyInjection), Entity Framework Core для работы с БД, и асинхронные задачи для реального времени обновлений.
Требования к окружению:
•	.NET (.NET 9+).
•	MS SQL Server (базы Pilot и Runtime).
•	Siemens PLC (IP: 192.168.0.200).
•	COM-порт для ридера карт (COM3 по умолчанию).
•	Библиотеки: Sharp7, LiveChartsCore, PdfSharpCore, etc.
Конфигурация в appsettings.json.
2. Архитектура
Приложение следует MVVM-подобной архитектуре с разделением на:
•	Модели (Models): Данные, такие как Employee1CModel, COMControllerParamsModel.
•	Сервисы (Services): Бизнес-логика, например, StatsService, OperatorService, GraphService.
•	Репозитории (Interfaces/Repositories): Доступ к данным (IEmployeeRepository, IStatsRepository), реализованные в DataAccessLayer.
•	Контроллеры (Controllers): Внешние интеграции, такие как Controller1C (для 1C), COMController (для COM-порта), ReadFromPLC/WriteToPLC (для PLC).
•	UI (Views): MainWindow.xaml.cs (основное окно), PLCDataWindow.xaml.cs (окно параметров PLC).
•	Утилиты (Utilities): TaskExtensions, конвертеры (ComparePlanFactConverter и др.).
•	Конфигурация: App.xaml.cs настраивает DI, логирование и запуск.
Поток данных:
1.	Запуск: App.xaml.cs парсит аргументы (operatorId), настраивает DI, запускает MainWindow.
2.	Аутентификация: COMController читает карту → OperatorService запрашивает 1C → Синхронизация с БД.
3.	Статистика: StatsService обновляет UI через вызовы IStatsRepository.
4.	PLC: ReadFromPLC/WriteToPLC подключаются к PLC, читают/пишут параметры.
5.	Графики: GraphService извлекает данные из Runtime БД и строит графики в MainWindow.
6.	Обновления: Таймеры и асинхронные задачи для реального времени (downtime, графики, статистика).
Логирование: Через ILogger, в консоль и debug.
3. Ключевые файлы и классы
3.1 Модели (Models)
Employee1CModel.cs
•	Описание: Модель сотрудника из 1C/SKUD.
•	Ключевые свойства:
o	CardNumber, PersonnelNumber, FullName, Department, Position, TORoleId.
o	ErrorCode/ErrorText для ошибок.
•	Логика: Enum ErrorCodes для статусов (EmployeeFound, NotFound и т.д.). Вложенные классы для статусов с текстом и цветом (для UI).
•	Методы: Нет методов, только свойства.
COMControllerParamsModel.cs
•	Описание: Параметры COM-порта для ридера карт.
•	Ключевые свойства: PortName, BaudRate, Parity, DataBits, StopBits.
•	Логика: Enum ErrorCodes и COMStates для состояний. Вложенные классы для статусов с текстом и цветом.
•	Конструктор: Инициализирует параметры порта.
SiemensPLCModels (в ReadFromPLC.cs и WriteToPLC.cs)
•	Описание: Модели адресов PLC (DBAddressModel, MAddressModel).
•	Логика: Классы для адресов (DbbDbwDbdAddress, DbxAddress и т.д.) с методами Read/Write для Sharp7.
3.2 Сервисы (Services)
StatsService.cs
•	Описание: Сервис для обновления статистики операторов.
•	Зависимости: IStatsRepository, IEmployeeRepository.
•	Ключевые методы:
o	UpdateStatsAsync(string personnelNumber, Action<...> updateUI): Получает operatorId, downtime, операции, планы. Вызывает updateUI с данными.
	Логика: Расчёт daily/monthly downtime, операций, планов. Учитывает смены (day/night).
o	UpdateIdsAsync(string personnelNumber): Обновляет lastKnownRepId/DowntimeId, присваивает operatorId неassigned записям.
o	UpdateStatsForAllOperatorsAsync(Action<...> updateAllUI): Обновляет статистику по всем сменам (downtime, операции, планы).
	Логика: Агрегирует данные по сменам (Smena), использует Debug для логирования.
OperatorService.cs
•	Описание: Сервис для аутентификации и управления операторами.
•	Зависимости: IEmployeeRepository, Controller1C, IStatsRepository.
•	Ключевые свойства: CurrentOperator (с событием OnOperatorChanged).
•	Ключевые методы:
o	AuthenticateOperatorAsync(string cardNumber, bool isAuth, DateTime? authTime): Аутентифицирует/deauth оператора.
	Логика: Получает operatorId, обновляет exchange и sys_stat в БД. Сбрасывает CurrentOperator при deauth.
o	InitializeOperatorAsync(string cardNumber): Инициализирует оператора.
	Логика: Запрос к 1C → Синхронизация с БД → Установка CurrentOperator.
o	FetchAndSaveFrom1C(string cardNumber): Запрос к 1C и сохранение в БД.
GraphService.cs
•	Описание: Сервис для построения графиков torque/turns.
•	Зависимости: ConnectionString, PipeCounter.
•	Ключевые методы:
o	GetGraphData(DateTime start, DateTime end, bool isActive): Возвращает точки, оси и ошибку.
	Логика: Синхронизирует данные из Runtime БД (теги NOT_MN3_ACT_TURNS, NOT_MN3_ACT_TORQUE). Фильтрует по времени/pipeCounter. Строит оси с лимитами.
o	FetchSynchronizedData(...): Извлекает и синхронизирует данные по времени.
	Логика: SQL-запросы с INTERPOLATE для заполнения пробелов. Обработка ошибок.
o	GetAvailableTags(): Для debug, возвращает доступные теги.
ReadFromPLC.cs
•	Описание: Сервис для чтения из PLC.
•	Зависимости: S7Client.
•	События: ValueRead (при чтении), ConnectionStateChanged.
•	Ключевые методы:
o	Connect/Disconnect: Подключение к PLC.
o	AddAddress(string key, object address): Добавляет адрес для чтения.
o	Read<T>(string key): Чтение значения по адресу.
	Логика: Поддержка разных типов адресов (DB, M). Логирование ошибок.
o	ReadAsync<T>(...): Асинхронное чтение с ретраями.
o	ReadMultipleAsync(...): Множественное чтение.
o	StartPeriodicReadAsync(TimeSpan interval, ...): Периодическое чтение.
WriteToPLC.cs
•	Описание: Сервис для записи в PLC (аналогично ReadFromPLC).
•	Ключевые методы:
o	Write<T>(string key, T value): Запись значения.
	Логика: Поддержка типов, логирование.
o	WriteAsync<T>(...): Асинхронная запись с ретраями.
3.3 Репозитории (Interfaces/Repositories)
IEmployeeRepository.cs и IStatsRepository.cs
•	Описание: Интерфейсы для доступа к данным сотрудников и статистики.
•	Реализация: DataAccessLayer.cs (использует SqlConnection для запросов).
•	Ключевые методы в DataAccessLayer:
o	GetEmployeeAsync(string cardNumber): Получает сотрудника из dic_SKUD, определяет роль.
o	SaveEmployeeAsync(Employee1CModel): Сохраняет/обновляет в БД.
o	SyncEmployeeAsync(...): Синхронизирует с БД, определяет роль.
o	Статистические методы: GetDailyDowntimeAsync, GetMonthlyDowntimeAsync, etc.
	Логика: SQL-запросы для агрегации (SUM downtime, COUNT операций). Учёт смен, планов (CalculatePlansAsync с временными таблицами).
3.4 Контроллеры (Controllers)
Controller1C.cs
•	Описание: Интеграция с 1C для получения данных сотрудников.
•	Ключевые методы:
o	GetResp1CSKUD(string cardNumber): POST-запрос SOAP к 1C.
	Логика: Чтение XML-шаблона, замена плейсхолдера, парсинг ответа (XML → JSON → Employee1CModel).
COMController.cs
•	Описание: Управление COM-ридером карт.
•	События: StateChanged (при обнаружении/удалении карты).
•	Ключевые свойства: IsReading (стартует/стопит чтение).
•	Логика: Асинхронное чтение из порта, очередь сообщений, обработка шаблонов (Regex для ID карты). Ретраи подключения.
3.5 UI (Views)
MainWindow.xaml.cs
•	Описание: Основное окно с UI для статистики, графиков, аутентификации.
•	Зависимости: Множество сервисов (OperatorService, StatsService, etc.).
•	Ключевые методы:
o	InitializeComponent(): Настройка UI, таймеров, графиков.
o	UpdateOperatorStats(): Асинхронное обновление статистики оператора.
o	UpdateAllStats(): Обновление статистики по сменам.
o	UpdateCurrentPipeData(): Обновление данных текущей трубы.
o	UpdateGraphData(): Построение графиков.
o	OnClosing(): Очистка ресурсов (cts.Cancel, Dispose).
•	Логика: Таймеры для обновлений (каждые 5-60 сек). Обработка событий (карта, PLC). Генерация PDF.
PLCDataWindow.xaml.cs
•	Описание: Окно для параметров PLC.
•	Зависимости: ReadFromPLC, WriteToPLC.
•	Ключевые методы:
o	SaveParametersToXml/LoadParametersFromXml: Сохранение/загрузка в XML.
o	PlcReader_ValueRead: Обновление UI при чтении.
o	UpdateConnectionStatus(): Обновление статуса подключения.
3.6 Утилиты и Конвертеры
•	TaskExtensions.cs: Расширение для таймаута задач.
•	ComparePlanFactConverter.cs: Конвертер для сравнения план/факт (для UI).
•	MaxPlanFactDifferenceConverter.cs: Определение максимальной разницы план/факт.
•	GradientBackgroundConverter.cs: Градиент фона на основе разницы план/факт.
3.7 Конфигурация
appsettings.json
•	Описание: JSON с соединениями (SQL, PLC IP), параметрами COM.
App.xaml.cs
•	Описание: Точка входа, настройка DI.
•	Ключевые методы:
o	ConfigureServices(): Регистрация сервисов, логгинга.
o	OnStartup(): Парсинг аргументов, аутентификация, запуск MainWindow.
DbContext.cs
•	Описание: EF Core контекст для БД Pilot.
•	Логика: Настройка таблиц, связей (Sector, Role, Skud, etc.).
4. Важная логика приложения
•	Аутентификация: Карта → COMController → OperatorService.InitializeOperatorAsync → Controller1C.GetResp1CSKUD → Sync с БД → Update статистики.
•	PLC Интеграция: Connect в Read/WriteToPLC → Периодическое чтение → UI обновления.
•	Статистика: Расчёты в StatsService с SQL-запросами (downtime в минутах, планы на смену/месяц).
•	Графики: Синхронизация данных по времени, интерполяция пробелов, лимиты осей.
•	Ошибки: Логирование в Debug, обработка в catch (TimeoutException, SqlException).
•	Асинхронность: Все обновления асинхронны с CTS для отмены при закрытии.
•	Безопасность: Credentials для 1C, TrustServerCertificate в SQL.
5. Рекомендации
•	Тестирование: Проверить на реальном оборудовании (PLC, ридер).
•	Оптимизация: SQL-запросы могут быть тяжелыми; добавить индексы.
•	Расширение: Добавить больше ролей, уведомлений.
Эта документация охватывает основные аспекты. Для деталей смотрите код

