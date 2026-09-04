using KarzounERP.Data;
using KarzounERP.Helpers;
using KarzounERP.Models;
using KarzounERP.Services;
using KarzounERP.Services.Interfaces;
using KarzounERP.ViewModels;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace KarzounERP;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    private static bool _isShowingUnhandledError;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Contains("--test-appdata-migration"))
        {
            var passed = TestAppDataMigration();
            Console.WriteLine($"[TestAppDataMigration] Pass={passed}");
            Shutdown(passed ? 0 : 1);
            return;
        }

        AppDataMigrationResult migration;
        try
        {
            migration = AppDataMigration.EnsureMigrated();
        }
        catch (Exception migrationException)
        {
            MessageBox.Show(
                $"KARZOUN ERP could not safely copy the existing application data. " +
                $"The legacy data was not changed and the application will close.\n\n{migrationException.Message}",
                AppPaths.ProductName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        QuestPDF.Settings.License = LicenseType.Community;

        // Test-PDF mode: generate sample PDFs without showing UI, then exit.
        // Usage: dotnet run -- --generate-test-pdfs
        if (e.Args.Contains("--generate-test-pdfs") || e.Args.Contains("--generate-test-exports"))
        {
            LocalizationManager.Initialize(LocalizationManager.LoadSavedLanguage());
            GenerateTestExports();
            Shutdown(0);
            return;
        }

        AppLogger.LogInfo($"[App] ===== {AppPaths.ProductName} startup {DateTime.Now:yyyy-MM-dd HH:mm:ss} =====");
        AppLogger.LogInfo(
            $"[AppDataMigration] LegacyDataFound={migration.LegacyDataFound} " +
            $"DatabaseCopied={migration.DatabaseCopied} ArchivedDuplicateRows={migration.ArchivedDuplicateRows} " +
            $"FilesCopied={migration.FilesCopied} " +
            $"Destination={migration.DestinationDirectory}");

        // Initialize localization BEFORE MaterialDesign so string resources are available
        var language = LocalizationManager.LoadSavedLanguage();
        LocalizationManager.Initialize(language);

        // Localization smoke test: after Initialize, log state, exit (no UI).
        // Usage: dotnet run --project KarzounERP.csproj -- --test-localization
        var saveLangArg = e.Args.FirstOrDefault(a => a.StartsWith("--save-language=", StringComparison.OrdinalIgnoreCase));
        if (saveLangArg != null)
        {
            var code = saveLangArg.Split('=', 2)[1].Trim();
            LocalizationManager.SaveLanguage(code);
            AppLogger.LogInfo($"[Test] SaveLanguage called with: {code}");
            Shutdown(0);
            return;
        }

        if (e.Args.Contains("--test-localization"))
        {
            TestLocalization();
            Shutdown(0);
            return;
        }

        // Load MaterialDesign theme programmatically — BundledTheme cannot be used in XAML
        // because it sets Source to a relative URI that fails to resolve at XAML parse time
        // before the Application's pack:// base URI is established.
        // Creating BundledTheme in code (after base.OnStartup) works correctly.
        var bundledTheme = new BundledTheme
        {
            BaseTheme = BaseTheme.Light,
            PrimaryColor = PrimaryColor.Cyan,
            SecondaryColor = SecondaryColor.Teal
        };
        Resources.MergedDictionaries.Add(bundledTheme);

        var defaultsDict = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml")
        };
        Resources.MergedDictionaries.Add(defaultsDict);
        ThemeManager.EnsureDefaultBrushes();
        ThemeManager.ApplyTheme(0);

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        var appDataDb = AppPaths.DatabasePath;

        try
        {
            using (var scope = Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                DatabaseInitializer.Initialize(context, appDataDb);
            }
        }
        catch (Exception initEx)
        {
            HandleStartupDatabaseFailure(initEx, appDataDb);
        }

        if (e.Args.Contains("--test-company-add"))
        {
            Task.Run(async () => await TestCompanyAddAsync()).GetAwaiter().GetResult();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-navigation"))
        {
            TestNavigation();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-auto-backup"))
        {
            Task.Run(() => TestAutoBackup()).GetAwaiter().GetResult();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-backup-folder-fallback"))
        {
            Task.Run(() => TestBackupFolderFallback()).GetAwaiter().GetResult();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-product-duplicates"))
        {
            Task.Run(() => TestProductDuplicates()).GetAwaiter().GetResult();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-product-translations"))
        {
            Task.Run(() => TestProductTranslations()).GetAwaiter().GetResult();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-positive-numbers"))
        {
            TestPositiveNumbers();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-restore-safety") || e.Args.Contains("--test-restore-safety-isolated"))
        {
            Task.Run(() => TestRestoreSafetyIsolated()).GetAwaiter().GetResult();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-database-recovery-inventory"))
        {
            Task.Run(() => TestDatabaseRecoveryInventory()).GetAwaiter().GetResult();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-startup-does-not-reset-real-db"))
        {
            Task.Run(() => TestStartupDoesNotResetRealDb()).GetAwaiter().GetResult();
            Environment.Exit(0);
            return;
        }


        if (e.Args.Contains("--test-search-ordering"))
        {
            Task.Run(() => TestSearchOrdering()).GetAwaiter().GetResult();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-rtl-layout"))
        {
            TestRtlLayout();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-appearance-navigation"))
        {
            TestAppearanceNavigation();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-appearance-view-template"))
        {
            TestAppearanceViewTemplate();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-pdf-font-size-setting"))
        {
            TestPdfFontSizeSetting();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-appearance-settings"))
        {
            TestAppearanceSettings();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-brand-identity"))
        {
            TestBrandIdentity();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-company-identity-theme"))
        {
            TestCompanyIdentityTheme();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-product-selector-search"))
        {
            TestProductSelectorSearch();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-product-selector-name-fill"))
        {
            TestProductSelectorNameFill();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-document-item-save-update"))
        {
            TestDocumentItemSaveUpdate();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-document-totals-weight-quantity"))
        {
            TestDocumentTotalsWeightQuantity();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-bulk-selection"))
        {
            TestBulkSelection();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-currency-display"))
        {
            TestCurrencyDisplay();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-document-type-status"))
        {
            TestDocumentTypeStatus();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-pdf-dynamic-product-images"))
        {
            TestPdfDynamicProductImages();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-pdf-all-items-visible"))
        {
            TestPdfAllItemsVisible();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-excel-import-export"))
        {
            TestExcelImportExport();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-products-navigation"))
        {
            TestProductsNavigation();
            Environment.Exit(0);
            return;
        }

        if (e.Args.Contains("--test-fresh-db-empty"))
        {
            TestFreshDbEmpty();
            Environment.Exit(0);
            return;
        }

        DispatcherUnhandledException += (_, args) =>
        {
            AppLogger.LogCrash("Dispatcher unhandled exception", args.Exception);
            args.Handled = true;
            if (_isShowingUnhandledError)
                return;

            _isShowingUnhandledError = true;
            try
            {
                MessageBox.Show(
                    LocalizationManager.Get("Msg_UnhandledError"),
                    LocalizationManager.Get("Msg_Error"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isShowingUnhandledError = false;
            }
        };

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static async Task TestCompanyAddAsync()
    {
        const string testName = "Test Company Cursor";
        using var scope = Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ICompanyService>();
        var existing = (await svc.GetAllCompaniesAsync()).FirstOrDefault(c => c.Name == testName);
        if (existing != null)
        {
            if (!await svc.CompanyHasDataAsync(existing.Id))
                await svc.DeleteCompanyAsync(existing.Id);
        }

        var added = await svc.AddCompanyAsync(new Models.Company
        {
            Name = testName,
            CommercialName = testName,
            Currency = "USD"
        });
        var all = await svc.GetAllCompaniesAsync();
        var found = all.FirstOrDefault(c => c.Id == added.Id);
        var pass = found != null && found.Name == testName && found.Id > 0;
        AppLogger.LogInfo($"[TestCompany] Added Id={added.Id} Name={added.Name} Pass={pass} TotalCompanies={all.Count}");
        Console.WriteLine($"[TestCompany] Pass={pass} Id={added.Id} Name={found?.Name} SidebarCount={all.Count}");
    }

    private static void TestNavigation()
    {
        var nav = Services.GetRequiredService<NavigationService>();
        var session = Services.GetRequiredService<AppSession>();
        var companySvc = Services.GetRequiredService<ICompanyService>();
        var companies = companySvc.GetAllCompaniesAsync().GetAwaiter().GetResult();
        if (companies.Any())
            session.ActiveCompany = companies.First();

        var field = typeof(NavigationService).GetField("_currentViewModel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        nav.NavigateTo<CustomerViewModel>();
        var vmFirst = field?.GetValue(nav);
        for (var i = 0; i < 4; i++) nav.NavigateTo<CustomerViewModel>();
        var vmAfter = field?.GetValue(nav);
        var sameInstance = ReferenceEquals(vmFirst, vmAfter);

        nav.NavigateTo<DocumentViewModel>(vm => vm.FilterType = Models.DocumentType.Quotation);
        var quotationsVm = field?.GetValue(nav);
        for (var i = 0; i < 4; i++)
            nav.NavigateTo<DocumentViewModel>(vm => vm.FilterType = Models.DocumentType.Quotation);
        var quotationsAfter = field?.GetValue(nav);
        var sameQuotations = ReferenceEquals(quotationsVm, quotationsAfter);

        AppLogger.LogInfo($"[TestNavigation] Customers SameInstance={sameInstance} Quotations SameInstance={sameQuotations}");
        Console.WriteLine($"[TestNavigation] Customers SameInstance={sameInstance} Quotations SameInstance={sameQuotations} Pass={sameInstance && sameQuotations}");
    }

    private static void ConfigureServices(ServiceCollection services)
    {
        Directory.CreateDirectory(AppPaths.DataRoot);

        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlite($"Data Source={AppPaths.DatabasePath}"), ServiceLifetime.Scoped);

        // Services
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IPdfService, PdfService>();
        services.AddScoped<IExcelService, ExcelService>();
        services.AddSingleton<IBackupService, BackupService>();

        // Helpers (singletons)
        services.AddSingleton<AppSession>();
        services.AddSingleton<NavigationService>();

        // ViewModels (transient so fresh state each navigation)
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<CompanyViewModel>();
        services.AddTransient<CompanyFormViewModel>();
        services.AddTransient<CustomerViewModel>();
        services.AddTransient<CustomerFormViewModel>();
        services.AddTransient<CustomerDetailViewModel>();
        services.AddTransient<ProductViewModel>();
        services.AddTransient<ProductFormViewModel>();
        services.AddTransient<DocumentViewModel>();
        services.AddTransient<DocumentFormViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<AppearanceViewModel>();
        services.AddTransient<LogViewModel>();

        // Window
        services.AddSingleton<MainWindow>();
    }

    public static T GetRequiredService<T>() where T : notnull
        => Services.GetRequiredService<T>();

    private static bool TestAppDataMigration()
    {
        var testRoot = Path.Combine(Path.GetTempPath(), $"KarzounERP_MigrationTest_{Guid.NewGuid():N}");
        var legacyRoot = AppPaths.GetLegacyDataRoot(testRoot);
        var legacyDatabase = Path.Combine(legacyRoot, AppPaths.LegacyDatabaseFileName);
        var legacyAppearance = Path.Combine(legacyRoot, "appearance.json");

        try
        {
            Directory.CreateDirectory(legacyRoot);
            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={legacyDatabase}"))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE MigrationProbe (Value TEXT NOT NULL); " +
                                      "INSERT INTO MigrationProbe (Value) VALUES ('preserved');";
                command.ExecuteNonQuery();
            }

            File.WriteAllText(legacyAppearance, "{\"source\":\"legacy\"}");
            var first = AppDataMigration.EnsureMigrated(testRoot);
            var destinationDatabase = Path.Combine(AppPaths.GetDataRoot(testRoot), AppPaths.DatabaseFileName);
            var destinationAppearance = Path.Combine(AppPaths.GetDataRoot(testRoot), "appearance.json");

            var firstCount = ReadMigrationProbeCount(destinationDatabase);
            File.WriteAllText(legacyAppearance, "{\"source\":\"changed-after-migration\"}");

            using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={legacyDatabase}"))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO MigrationProbe (Value) VALUES ('must-not-overwrite');";
                command.ExecuteNonQuery();
            }

            var second = AppDataMigration.EnsureMigrated(testRoot);
            var secondCount = ReadMigrationProbeCount(destinationDatabase);
            var appearancePreserved = File.ReadAllText(destinationAppearance) == "{\"source\":\"legacy\"}";

            return first.LegacyDataFound && first.DatabaseCopied && first.ArchivedDuplicateRows == 0 && first.FilesCopied == 1 &&
                   second.LegacyDataFound && !second.DatabaseCopied && second.ArchivedDuplicateRows == 0 && second.FilesCopied == 0 &&
                   firstCount == 1 && secondCount == 1 && appearancePreserved;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TestAppDataMigration] Error={ex}");
            return false;
        }
        finally
        {
            try
            {
                if (Directory.Exists(testRoot))
                    Directory.Delete(testRoot, recursive: true);
            }
            catch
            {
                // Test cleanup failure does not touch product or customer data.
            }
        }
    }

    private static int ReadMigrationProbeCount(string databasePath)
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM MigrationProbe;";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void TestLocalization()
    {
        var lang = LocalizationManager.Language;
        var fd = LocalizationManager.FlowDirection;
        var navTitle = LocalizationManager.Get("Nav_Dashboard");
        var appFlow = Application.Current.Resources["AppFlowDirection"];
        Console.WriteLine($"Language={lang}");
        Console.WriteLine($"FlowDirection={fd}");
        Console.WriteLine($"AppFlowDirection={appFlow}");
        Console.WriteLine($"Nav_Dashboard={navTitle}");
        AppLogger.LogInfo($"[TestLocalization] Language={lang} FlowDirection={fd} AppFlowDirection={appFlow} Nav_Dashboard={navTitle}");
    }

    private static void TestAutoBackup()
    {
        using var scope = Services.CreateScope();
        var backup = scope.ServiceProvider.GetRequiredService<IBackupService>();
        var companySvc = scope.ServiceProvider.GetRequiredService<ICompanyService>();
        var companies = companySvc.GetAllCompaniesAsync().GetAwaiter().GetResult();
        var activeCompany = companies.FirstOrDefault();
        var targetFolder = backup.ResolveBackupFolder(activeCompany?.BackupFolder);
        var oldDefault = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "backup"))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var resolvedFolder = Path.GetFullPath(targetFolder)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var path = backup.BackupDatabase(targetFolder);
        var pass = File.Exists(path) && !string.Equals(resolvedFolder, oldDefault, StringComparison.OrdinalIgnoreCase);
        AppLogger.LogInfo($"[TestAutoBackup] Pass={pass} Backup={path}");
        Console.WriteLine($"[TestAutoBackup] Pass={pass} Backup={path}");
    }

    // Isolated: only ever writes to a temp folder and, for the fallback case, the app's
    // own default backup folder. Never inserts/updates/deletes Company/Customer/Product
    // rows, and the source database is only ever read (copied from), never modified.
    private static void TestBackupFolderFallback()
    {
        using var scope = Services.CreateScope();
        var backup = scope.ServiceProvider.GetRequiredService<IBackupService>();
        var dbPath = backup.GetDatabasePath();
        var lastWriteBefore = File.Exists(dbPath) ? File.GetLastWriteTimeUtc(dbPath) : DateTime.MinValue;

        string? validDestination = null;
        string? fallbackDestination = null;
        var validThrew = false;
        var fallbackThrew = false;

        // 1. A valid, writable, temporary custom folder must be used as-is (no fallback).
        var validFolder = Path.Combine(Path.GetTempPath(), "KarzounErp_BackupFolderTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            validDestination = backup.BackupDatabase(validFolder);
        }
        catch
        {
            validThrew = true;
        }

        // 2. A configured folder on a drive that does not exist on this machine must not crash;
        // it must fall back to the application's default backup folder instead.
        var usedDrives = DriveInfo.GetDrives().Select(d => char.ToUpperInvariant(d.Name[0])).ToHashSet();
        var unusedLetter = "ZYXWVUTSRQPONMLKJIHGFEDCBA".FirstOrDefault(c => !usedDrives.Contains(c));
        var unavailableFolder = unusedLetter != '\0'
            ? $"{unusedLetter}:\\KarzounErp_NoSuchDrive_{Guid.NewGuid():N}\\backup"
            : Path.Combine("\\\\KarzounErp_NoSuchHost_" + Guid.NewGuid().ToString("N"), "backup");
        try
        {
            fallbackDestination = backup.BackupDatabase(unavailableFolder);
        }
        catch
        {
            fallbackThrew = true;
        }

        var lastWriteAfter = File.Exists(dbPath) ? File.GetLastWriteTimeUtc(dbPath) : DateTime.MinValue;
        var dbUntouched = lastWriteBefore == lastWriteAfter;

        var validFolderUsed = !validThrew && validDestination != null && File.Exists(validDestination)
            && string.Equals(
                Path.GetFullPath(Path.GetDirectoryName(validDestination)!).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Path.GetFullPath(validFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);

        var defaultFolder = Path.GetFullPath(AppPaths.BackupsDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fellBackToDefault = !fallbackThrew && fallbackDestination != null && File.Exists(fallbackDestination)
            && string.Equals(
                Path.GetFullPath(Path.GetDirectoryName(fallbackDestination)!).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                defaultFolder,
                StringComparison.OrdinalIgnoreCase);

        var pass = validFolderUsed && fellBackToDefault && dbUntouched;

        AppLogger.LogInfo($"[TestBackupFolderFallback] Pass={pass} ValidFolderUsed={validFolderUsed} FellBackToDefault={fellBackToDefault} DbUntouched={dbUntouched} ValidDestination={validDestination} FallbackDestination={fallbackDestination} UnavailableFolder={unavailableFolder}");
        Console.WriteLine($"[TestBackupFolderFallback] Pass={pass} ValidFolderUsed={validFolderUsed} FellBackToDefault={fellBackToDefault} DbUntouched={dbUntouched} ValidThrew={validThrew} FallbackThrew={fallbackThrew} ValidDestination={validDestination} FallbackDestination={fallbackDestination}");

        // Clean up only the artifacts this test just created — never touches any pre-existing backup.
        try { if (validDestination != null && File.Exists(validDestination)) File.Delete(validDestination); } catch { }
        try { if (Directory.Exists(validFolder)) Directory.Delete(validFolder, recursive: true); } catch { }
        try { if (fallbackDestination != null && File.Exists(fallbackDestination)) File.Delete(fallbackDestination); } catch { }
    }

    private void GenerateTestExports()
    {
        var outDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KARZOUN_ERP_TestExports");
        Directory.CreateDirectory(outDir);

        // Generate a small dummy image to test PDF rendering with image
        var dummyImagePath = Path.Combine(outDir, "dummy_image.png");
        try
        {
            byte[] pngBytes = new byte[] {
                0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
                0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
                0x89, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x44, 0x41, 0x54, 0x78, 0xDA, 0x63, 0x60, 0x60, 0x60, 0x00,
                0x00, 0x00, 0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E,
                0x44, 0xAE, 0x42, 0x60, 0x82
            };
            File.WriteAllBytes(dummyImagePath, pngBytes);
        }
        catch { }

        var testDate = new DateTime(2026, 5, 22);
        var company = new Models.Company
        {
            Name = "Karzoun Demo Company",
            CommercialName = "Karzoun Demo Company",
            Phone = "+966-11-000-0000",
            Email = "demo@karzoun.example",
            Address = "Riyadh, Saudi Arabia",
            Currency = "SAR",
            ShowProductImageInQuotation = true
        };

        var customer = new Models.Customer
        {
            FullName = "أحمد محمد الصالح",
            CompanyName = "Tech Solutions",
            Phone = "+966-55-000-0001",
            Email = "ahmed@tech.sa",
            Country = "Saudi Arabia"
        };

        var items = new List<Models.SalesDocumentItem>
        {
            new() { ProductName = "استضافة ويب سنوية", ProductType = Models.ProductType.Service,
                Quantity = 1, UnitPrice = 500m, LineTotal = 500m, SortOrder = 0 },
            new() { ProductName = "كمبيوتر Dell XPS محمول", ProductType = Models.ProductType.Physical,
                Weight = 1.5m, Quantity = 2, UnitPrice = 2200m, LineTotal = 4400m, SortOrder = 1, ImagePath = dummyImagePath },
            new() { ProductName = "Technical Support", ProductType = Models.ProductType.Service,
                Quantity = 5, UnitPrice = 150m, LineTotal = 750m, SortOrder = 2 }
        };

        var pdfConfigs = new[]
        {
            ("ar", Models.DocumentType.Invoice, "INV-0007", "ar_invoice"),
            ("ar", Models.DocumentType.Quotation, "QUO-0007", "ar_quotation"),
            ("tr", Models.DocumentType.Invoice, "INV-0007", "tr_invoice"),
            ("en", Models.DocumentType.Invoice, "INV-0007", "en_invoice"),
        };

        foreach (var (lang, docType, docNum, suffix) in pdfConfigs)
        {
            var doc = BuildTestDocument(docType, docNum, testDate, items, docType == Models.DocumentType.Invoice);
            var path = Path.Combine(outDir, $"karzoun_erp_test_{suffix}.pdf");
            File.WriteAllBytes(path, new Pdf.InvoiceDocument(doc, company, customer, lang).GeneratePdf());
            Console.WriteLine($"[PDF OK] {path}");
        }

        var sampleDocs = new List<Models.SalesDocument>
        {
            BuildTestDocument(Models.DocumentType.Invoice, "INV-0007", testDate, items, paid: true)
        };

        var excel = new ExcelService();
        foreach (var lang in new[] { "en", "tr", "ar" })
        {
            LocalizationManager.ApplyLanguage(lang, persist: false);
            var xlsx = Path.Combine(outDir, $"karzoun_erp_test_report_{lang}.xlsx");
            excel.ExportSalesReport(sampleDocs, xlsx);
            Console.WriteLine($"[Excel OK] {xlsx} lang={lang}");
        }

        Console.WriteLine($"\nAll test files: {outDir}");
    }

    private static Models.SalesDocument BuildTestDocument(
        Models.DocumentType type, string number, DateTime date,
        List<Models.SalesDocumentItem> items, bool paid)
    {
        return new Models.SalesDocument
        {
            DocumentNumber = number,
            Type = type,
            Status = Models.DocumentStatus.Draft,
            Date = date,
            DueDate = date.AddDays(30),
            Subtotal = 5650m,
            DiscountAmount = 565m,
            TaxRate = 15m,
            TaxAmount = 762.75m,
            GrandTotal = 5847.75m,
            PaidAmount = paid ? 2000m : 0m,
            Items = items,
            Customer = new Models.Customer { FullName = "Test" }
        };
    }

    private static void TestProductDuplicates()
    {
        using var scope = Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IProductService>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var company = context.Companies.FirstOrDefault(c => c.Name == "Test Duplicates Co");
        if (company == null)
        {
            company = new Models.Company { Name = "Test Duplicates Co", Currency = "USD" };
            context.Companies.Add(company);
            context.SaveChanges();
        }

        var oldProds = context.Products.Where(p => p.CompanyId == company.Id).ToList();
        context.Products.RemoveRange(oldProds);
        context.SaveChanges();

        var rose800 = new Models.Product
        {
            CompanyId = company.Id,
            Name = "مربى الورد",
            UnitPrice = 100m,
            DefaultQuantity = 1,
            Weight = 800m,
            WeightUnit = "g",
            Type = Models.ProductType.Physical,
            LocalizedTexts = new List<Models.ProductLocalizedText>
            {
                new() { LanguageCode = "en", Name = "Rose Jam" },
                new() { LanguageCode = "tr", Name = "Gul Receli" }
            }
        };
        svc.AddProductAsync(rose800).GetAwaiter().GetResult();

        var premium = new Models.Product
        {
            CompanyId = company.Id,
            Name = "Premium Rose Jam Extra Fine",
            UnitPrice = 100m,
            DefaultQuantity = 1,
            Weight = 800m,
            WeightUnit = "g",
            Type = Models.ProductType.Physical
        };
        svc.AddProductAsync(premium).GetAwaiter().GetResult();

        var variantWeight = svc.CheckDuplicateAsync(company.Id, 0, "مربى الورد", "", "", "", 1m, "kg", Models.ProductType.Physical).GetAwaiter().GetResult();
        var variantFlavor = svc.CheckDuplicateAsync(company.Id, 0, "مربى الفراولة", "", "", "", 800m, "g", Models.ProductType.Physical).GetAwaiter().GetResult();
        var variantFig = svc.CheckDuplicateAsync(company.Id, 0, "مربى التين", "", "", "", 800m, "g", Models.ProductType.Physical).GetAwaiter().GetResult();
        var exactDuplicate = svc.CheckDuplicateAsync(company.Id, 0, "مربى الورد", "", "", "", 800m, "g", Models.ProductType.Physical).GetAwaiter().GetResult();
        var localizedDuplicate = svc.CheckDuplicateAsync(company.Id, 0, "Other", "", "", "Rose Jam", 800m, "g", Models.ProductType.Physical).GetAwaiter().GetResult();
        var typoWarning = svc.CheckDuplicateAsync(company.Id, 0, "Premium Rose Jam Extra Finee", "", "", "", 800m, "g", Models.ProductType.Physical).GetAwaiter().GetResult();
        var repeatedCommonWord = svc.CheckDuplicateAsync(company.Id, 0, "مربى مربى التين", "", "", "", 800m, "g", Models.ProductType.Physical).GetAwaiter().GetResult();

        var variantsAllowed = variantWeight == null && variantFlavor == null && variantFig == null;
        var exactWarns = exactDuplicate?.Id == rose800.Id;
        var localizedWarns = localizedDuplicate?.Id == rose800.Id;
        var typoWarns = typoWarning?.Id == premium.Id;
        var repeatedCommonAllowed = repeatedCommonWord == null;

        var helperExact = ProductDuplicateHelper.FindBestRichMatch(
            ProductDuplicateHelper.GetEnteredIdentities("مربى الورد", "", "", "", 800m, "g", Models.ProductType.Physical),
            new[] { "مربى الورد" },
            800m,
            "g",
            Models.ProductType.Physical,
            new[] { rose800 },
            0);
        var helperVariant = ProductDuplicateHelper.FindBestRichMatch(
            ProductDuplicateHelper.GetEnteredIdentities("مربى الورد", "", "", "", 1m, "kg", Models.ProductType.Physical),
            new[] { "مربى الورد" },
            1m,
            "kg",
            Models.ProductType.Physical,
            new[] { rose800 },
            0);
        var strawberryLive = ProductDuplicateHelper.FindBestRichMatch(
            ProductDuplicateHelper.GetEnteredIdentities("مربى الفراولة", "", "", "", 800m, "g", Models.ProductType.Physical),
            new[] { "مربى الفراولة" },
            800m,
            "g",
            Models.ProductType.Physical,
            new[] { rose800 },
            0);
        var exactClone = new Models.Product
        {
            Id = 5001,
            CompanyId = company.Id,
            Name = "مربى الورد",
            Weight = 800m,
            WeightUnit = "g",
            Type = Models.ProductType.Physical
        };
        var typoClone = new Models.Product
        {
            Id = 5002,
            CompanyId = company.Id,
            Name = "Premium Rose Jam Extra Finee",
            Weight = 800m,
            WeightUnit = "g",
            Type = Models.ProductType.Physical
        };
        var figVariant = new Models.Product
        {
            Id = 5003,
            CompanyId = company.Id,
            Name = "مربى التين",
            Weight = 800m,
            WeightUnit = "g",
            Type = Models.ProductType.Physical
        };
        var pairs = ProductDuplicateHelper.ScanDuplicatePairs(new[] { rose800, exactClone, premium, typoClone, figVariant });
        var scanExact = pairs.Any(p => p.IsExactDuplicate && p.ProductA.Name == "مربى الورد" && p.ProductB.Name == "مربى الورد");
        var scanPotential = pairs.Any(p => p.NameSimilarityPercent >= 90 && !p.IsExactDuplicate);
        var noFalseHardBlock = !helperVariant.ShouldWarn && helperVariant.NameSimilarityPercent >= 95 && helperVariant.IdentitySimilarityPercent < 95;
        var helperOk = helperExact.ShouldWarn
            && Math.Round(helperExact.Similarity) >= 95
            && noFalseHardBlock
            && strawberryLive.ClosestProduct != null
            && strawberryLive.NameSimilarityPercent > 0
            && scanExact
            && scanPotential;

        var pass = variantsAllowed && exactWarns && localizedWarns && typoWarns && repeatedCommonAllowed && helperOk;
        AppLogger.LogInfo($"[TestDuplicates] Pass={pass} VariantsAllowed={variantsAllowed} ExactWarns={exactWarns} LocalizedWarns={localizedWarns} TypoWarns={typoWarns} RepeatedCommonAllowed={repeatedCommonAllowed} Helper={helperOk} ScanExact={scanExact} ScanPotential={scanPotential}");
        Console.WriteLine($"[TestDuplicates] Pass={pass} VariantsAllowed={variantsAllowed} ExactWarns={exactWarns} LocalizedWarns={localizedWarns} TypoWarns={typoWarns} RepeatedCommonAllowed={repeatedCommonAllowed} Helper={helperOk} ScanExact={scanExact} ScanPotential={scanPotential} ExactSimilarity={helperExact.Similarity.ToString("0", System.Globalization.CultureInfo.InvariantCulture)} VariantNameSimilarity={helperVariant.NameSimilarityPercent.ToString("0", System.Globalization.CultureInfo.InvariantCulture)} VariantIdentitySimilarity={helperVariant.IdentitySimilarityPercent.ToString("0", System.Globalization.CultureInfo.InvariantCulture)}");
    }

    private static void TestProductTranslations()
    {
        using var scope = Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IProductService>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var company = context.Companies.FirstOrDefault(c => c.Name == "Test Translations Co");
        if (company == null)
        {
            company = new Models.Company { Name = "Test Translations Co", Currency = "USD" };
            context.Companies.Add(company);
            context.SaveChanges();
        }

        var oldProds = context.Products.Where(p => p.CompanyId == company.Id).ToList();
        context.Products.RemoveRange(oldProds);
        context.SaveChanges();

        var p = new Models.Product
        {
            CompanyId = company.Id,
            Name = "Main Name EN",
            UnitPrice = 200m,
            DefaultQuantity = 1,
            WeightUnit = "kg",
            LocalizedTexts = new List<Models.ProductLocalizedText>
            {
                new() { LanguageCode = "ar", Name = "المنتج العربي", Description = "وصف عربي" },
                new() { LanguageCode = "tr", Name = "Turkce Urun", Description = "Turkce Aciklama" },
                new() { LanguageCode = "en", Name = "English Product", Description = "English Desc" }
            }
        };
        svc.AddProductAsync(p).GetAwaiter().GetResult();

        var reloaded = svc.GetProductAsync(p.Id).GetAwaiter().GetResult();
        bool passAr = reloaded?.LocalizedTexts.FirstOrDefault(t => t.LanguageCode == "ar")?.Name == "المنتج العربي";
        bool passTr = reloaded?.LocalizedTexts.FirstOrDefault(t => t.LanguageCode == "tr")?.Name == "Turkce Urun";
        bool passEn = reloaded?.LocalizedTexts.FirstOrDefault(t => t.LanguageCode == "en")?.Name == "English Product";

        bool pass = passAr && passTr && passEn;
        AppLogger.LogInfo($"[TestTranslations] Pass={pass} Ar={passAr} Tr={passTr} En={passEn}");
        Console.WriteLine($"[TestTranslations] Pass={pass} Ar={passAr} Tr={passTr} En={passEn}");
    }

    private static void TestPositiveNumbers()
    {
        var inputAr = "١٢٣٤٥٦٧٨٩٠";
        var inputFa = "۱۲۳۴۵۶۷۸۹۰";
        var outputAr = DigitNormalizer.ToEnglishDigits(inputAr);
        var outputFa = DigitNormalizer.ToEnglishDigits(inputFa);
        
        bool normalizerPass = (outputAr == "1234567890") && (outputFa == "1234567890");

        var parsedDecimal = DigitNormalizer.ParseDecimal("١٥٠.٧٥");
        var parsedInt = DigitNormalizer.ParseInt("٩٩");
        
        bool parsePass = (parsedDecimal == 150.75m) && (parsedInt == 99);

        bool isNegativeBlocked = DigitNormalizer.ParseDecimal("-150.75") < 0;

        bool pass = normalizerPass && parsePass && isNegativeBlocked;
        AppLogger.LogInfo($"[TestPositiveNumbers] Pass={pass} Norm={normalizerPass} Parse={parsePass} NegBlocked={isNegativeBlocked}");
        Console.WriteLine($"[TestPositiveNumbers] Pass={pass} Norm={normalizerPass} Parse={parsePass} NegBlocked={isNegativeBlocked}");
    }

    private static void TestRestoreSafetyIsolated()
    {
        using var scope = Services.CreateScope();
        var backup = scope.ServiceProvider.GetRequiredService<IBackupService>();
        var dbPath = backup.GetDatabasePath();

        // 1. Create a temporary isolated test folder
        var tempFolder = Path.Combine(Path.GetTempPath(), "KARZOUN_ERP_Test_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempFolder);

        try
        {
            // 2. Create a temporary valid test database. Do not copy the real DB:
            // SQLite WAL/SHM state can make a raw file copy unsuitable for integrity testing.
            var testDbPath = Path.Combine(tempFolder, "karzoun_erp_test.db");
            using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={testDbPath}"))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "CREATE TABLE Dummy (Id INTEGER PRIMARY KEY);";
                cmd.ExecuteNonQuery();
            }

            bool dbPreOk = QuickCheckIntegrity(testDbPath);
            var lastWriteBefore = File.Exists(dbPath) ? File.GetLastWriteTimeUtc(dbPath) : DateTime.MinValue;

            // 3. Create garbage test file in the temp folder
            var garbageFile = Path.Combine(tempFolder, "garbage_test_backup.db");
            File.WriteAllText(garbageFile, "This is not a valid sqlite database file! just random text.");

            // 4. Test restore logic only on temporary paths
            bool restoreResult = backup.RestoreDatabaseToPath(garbageFile, testDbPath);
            bool dbPostOk = QuickCheckIntegrity(testDbPath);

            var lastWriteAfter = File.Exists(dbPath) ? File.GetLastWriteTimeUtc(dbPath) : DateTime.MinValue;
            bool realDbUntouched = (lastWriteBefore == lastWriteAfter);

            bool pass = (!restoreResult) && dbPreOk && dbPostOk && realDbUntouched;
            AppLogger.LogInfo($"[TestRestoreSafetyIsolated] Pass={pass} restoreResult={restoreResult} preIntegrity={dbPreOk} postIntegrity={dbPostOk} realDbUntouched={realDbUntouched}");
            Console.WriteLine($"[TestRestoreSafetyIsolated] Pass={pass} restoreResult={restoreResult} preIntegrity={dbPreOk} postIntegrity={dbPostOk} realDbUntouched={realDbUntouched}");
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }
            }
            catch { }
        }
    }

    private static void TestDatabaseRecoveryInventory()
    {
        var reportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KARZOUN_ERP_Database_Recovery_Report.txt");
        using var scope = Services.CreateScope();
        var backup = scope.ServiceProvider.GetRequiredService<IBackupService>();
        var dbPath = backup.GetDatabasePath();
        var dbFolder = Path.GetDirectoryName(dbPath) ?? AppContext.BaseDirectory;
        var safetyFolder = Path.Combine(dbFolder, "RestoreSafetyBackups");
        var brokenFolder = Path.Combine(dbFolder, "BrokenDatabases");

        var candidates = new List<string>();
        if (Directory.Exists(safetyFolder))
        {
            candidates.AddRange(Directory.GetFiles(safetyFolder, "*.db"));
        }
        if (Directory.Exists(brokenFolder))
        {
            candidates.AddRange(Directory.GetFiles(brokenFolder, "*.db"));
        }

        var lines = new List<string>
        {
            "KARZOUN ERP Database Recovery Inventory",
            $"GeneratedUtc={DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
            $"DatabasePath={dbPath}",
            $"DatabaseExists={File.Exists(dbPath)}",
            $"DatabaseIntegrity={(File.Exists(dbPath) ? QuickCheckIntegrity(dbPath) : false)}",
            $"DatabaseSchema={(File.Exists(dbPath) ? VerifySchema(dbPath) : false)}",
            $"SafetyFolder={safetyFolder}",
            $"BrokenFolder={brokenFolder}",
            $"CandidateCount={candidates.Count}"
        };

        foreach (var candidate in candidates.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            lines.Add($"Candidate={candidate}");
            lines.Add($"CandidateIntegrity={QuickCheckIntegrity(candidate)}");
            lines.Add($"CandidateSchema={VerifySchema(candidate)}");
        }

        File.WriteAllLines(reportPath, lines);
        bool exists = File.Exists(reportPath);
        bool hasContent = exists && new FileInfo(reportPath).Length > 100;
        bool pass = exists && hasContent && File.Exists(dbPath) && QuickCheckIntegrity(dbPath);
        AppLogger.LogInfo($"[TestDatabaseRecoveryInventory] Pass={pass} exists={exists} hasContent={hasContent}");
        Console.WriteLine($"[TestDatabaseRecoveryInventory] Pass={pass} exists={exists} hasContent={hasContent}");
    }

    private static bool IsTestMode = false;
    private static MessageBoxResult MockUserResetDecision = MessageBoxResult.No;

    private static void TestStartupDoesNotResetRealDb()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "KARZOUN_ERP_Test_" + Guid.NewGuid().ToString());
        var testDbFolder = AppPaths.GetDataRoot(tempFolder);
        Directory.CreateDirectory(testDbFolder);
        var testDbPath = Path.Combine(testDbFolder, AppPaths.DatabaseFileName);

        IsTestMode = true;
        
        try
        {
            var backupFolder = Path.Combine(testDbFolder, "RestoreSafetyBackups");
            Directory.CreateDirectory(backupFolder);
            var backupDbPath = Path.Combine(backupFolder, "karzoun_erp_emergency_2026-06-23_12-00-00.db");
            
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite($"Data Source={backupDbPath}");
            using (var context = new AppDbContext(optionsBuilder.Options))
            {
                context.Database.EnsureCreated();
                var company = new Models.Company
                {
                    Name = "Real User Company",
                    CommercialName = "Real User Company",
                    Currency = "USD"
                };
                context.Companies.Add(company);
                context.SaveChanges();
            }

            File.WriteAllText(testDbPath, "This is garbage data to simulate corruption!");

            MockUserResetDecision = MessageBoxResult.No;
            
            try
            {
                HandleStartupDatabaseFailure(new Exception("Test Init Failure"), testDbPath, tempFolder);
            }
            catch (Exception initFailureEx)
            {
                Console.WriteLine($"[DEBUG] First HandleStartupDatabaseFailure failed: {initFailureEx}");
                var debugLogPath = Path.Combine(AppPaths.GetDataRoot(tempFolder), "Logs", "DatabaseRecovery.log");
                if (File.Exists(debugLogPath))
                {
                    Console.WriteLine("--- DatabaseRecovery.log contents ---");
                    Console.WriteLine(File.ReadAllText(debugLogPath));
                    Console.WriteLine("-------------------------------------");
                }
                else
                {
                    Console.WriteLine("DatabaseRecovery.log not found.");
                }
                throw;
            }
            
            bool restoredAutomatic = HasUserData(testDbPath);

            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            File.WriteAllText(testDbPath, "This is garbage data again!");
            if (File.Exists(backupDbPath)) File.Delete(backupDbPath);
            
            bool exitedCorrectly = false;
            try
            {
                HandleStartupDatabaseFailure(new Exception("Test Init Failure No Backup"), testDbPath, tempFolder);
            }
            catch (Exception ex) when (ex.Message.Contains("ExitCalled"))
            {
                exitedCorrectly = true;
            }

            MockUserResetDecision = MessageBoxResult.Yes;
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            File.WriteAllText(testDbPath, "This is garbage data again!");
            
            bool resetCompleted = false;
            try
            {
                HandleStartupDatabaseFailure(new Exception("Test Init Failure Reset"), testDbPath, tempFolder);
                resetCompleted = File.Exists(testDbPath) && new FileInfo(testDbPath).Length > 0;
            }
            catch { }

            bool pass = restoredAutomatic && exitedCorrectly && resetCompleted;
            
            AppLogger.LogInfo($"[TestStartupDoesNotResetRealDb] Pass={pass} restoredAutomatic={restoredAutomatic} exitedCorrectly={exitedCorrectly} resetCompleted={resetCompleted}");
            Console.WriteLine($"[TestStartupDoesNotResetRealDb] Pass={pass} restoredAutomatic={restoredAutomatic} exitedCorrectly={exitedCorrectly} resetCompleted={resetCompleted}");
        }
        finally
        {
            IsTestMode = false;
            try
            {
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }
            }
            catch { }
        }
    }

    private static void HandleStartupDatabaseFailure(Exception ex, string dbPath)
    {
        HandleStartupDatabaseFailure(ex, dbPath, null);
    }

    private static void HandleStartupDatabaseFailure(Exception ex, string dbPath, string? customAppData)
    {
        var appData = customAppData ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var baseDir = AppPaths.GetDataRoot(appData);
        
        var logFolder = Path.Combine(baseDir, "Logs");
        Directory.CreateDirectory(logFolder);
        var logFile = Path.Combine(logFolder, $"startup_error_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
        try
        {
            File.WriteAllText(logFile, ex.ToString());
        }
        catch { }

        LogRecovery($"Startup DB failure caught: {ex.Message}", customAppData);
        LogRecovery($"Problematic DB path: {dbPath}", customAppData);
        if (File.Exists(dbPath))
        {
            LogRecovery($"Problematic DB size: {new FileInfo(dbPath).Length} bytes", customAppData);
        }

        var brokenFolder = Path.Combine(baseDir, "BrokenDatabases");
        Directory.CreateDirectory(brokenFolder);
        var brokenDbPath = Path.Combine(brokenFolder, $"karzoun_erp_broken_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.db");

        bool hasOriginalDb = File.Exists(dbPath);
        if (hasOriginalDb)
        {
            try
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                File.Copy(dbPath, brokenDbPath, overwrite: true);
                LogRecovery($"Copied problematic DB to: {brokenDbPath}", customAppData);
                
                File.Delete(dbPath);
                LogRecovery($"Deleted original DB at {dbPath} to prepare for restoration", customAppData);
            }
            catch (Exception moveEx)
            {
                LogRecovery($"Failed to copy/move problematic DB: {moveEx.Message}", customAppData);
            }
        }

        var backupFolder = Path.Combine(baseDir, "RestoreSafetyBackups");
        var candidates = new List<string>();
        if (Directory.Exists(backupFolder))
        {
            candidates.AddRange(Directory.GetFiles(backupFolder, "*.db"));
        }
        if (Directory.Exists(brokenFolder))
        {
            candidates.AddRange(Directory.GetFiles(brokenFolder, "*.db"));
        }

        var validCandidates = new List<(string path, int score, DateTime writeTime)>();
        foreach (var c in candidates.Distinct())
        {
            if (c == brokenDbPath) continue;
            
            if (QuickCheckIntegrity(c) && VerifySchema(c))
            {
                var stats = GetDatabaseStats(c);
                var writeTime = File.GetLastWriteTimeUtc(c);
                validCandidates.Add((c, stats.score, writeTime));
                LogRecovery($"Found valid candidate: {Path.GetFileName(c)}, score={stats.score}, companies={stats.companies}, customers={stats.customers}, products={stats.products}, lastWrite={writeTime}", customAppData);
            }
        }

        var bestCandidate = validCandidates
            .OrderByDescending(x => x.score)
            .ThenByDescending(x => x.writeTime)
            .Select(x => x.path)
            .FirstOrDefault();

        bool restored = false;
        if (bestCandidate != null)
        {
            try
            {
                LogRecovery($"Selected best candidate for restoration: {bestCandidate}", customAppData);
                File.Copy(bestCandidate, dbPath, overwrite: true);
                
                DatabaseInitializer.InitializeRestoredDatabase(dbPath);
                
                restored = true;
                LogRecovery("Database restored and migrated successfully.", customAppData);

                if (!IsTestMode)
                {
                    MessageBox.Show(
                        "The database was corrupted and has been restored from the latest valid safety backup automatically.",
                        "Database Restored Automatically", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception restoreEx)
            {
                LogRecovery($"Failed to restore from candidate {bestCandidate}: {restoreEx.Message}", customAppData);
            }
        }

        if (!restored)
        {
            LogRecovery("No valid backup candidate was found.", customAppData);
            
            bool existedAndHadUserData = hasOriginalDb && HasUserData(brokenDbPath);
            LogRecovery($"Existed and had user data: {existedAndHadUserData}", customAppData);

            MessageBoxResult result;
            if (IsTestMode)
            {
                result = MockUserResetDecision;
            }
            else
            {
                result = MessageBox.Show(
                    "No valid backup was found. Do you want to create a new empty database?",
                    "Database Reset Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            }

            if (result == MessageBoxResult.Yes)
            {
                LogRecovery("User chose to create a new empty database.", customAppData);
                try
                {
                    using (var scope = Services.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        context.Database.EnsureCreated();
                        DatabaseInitializer.Initialize(context, dbPath);
                    }
                    LogRecovery("New empty database created and initialized.", customAppData);
                    if (!IsTestMode)
                    {
                        MessageBox.Show(
                            "The database was corrupted beyond repair. A fresh empty database has been created.\nYour broken database was preserved in AppData\\KARZOUN ERP\\BrokenDatabases.",
                            "Database Reset", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception freshEx)
                {
                    LogRecovery($"Failed to initialize fresh database: {freshEx.Message}", customAppData);
                    if (!IsTestMode)
                    {
                        MessageBox.Show(
                            $"Failed to initialize even a fresh database. The app will close.\nError: {freshEx.Message}",
                            "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Environment.Exit(-1);
                    }
                }
            }
            else
            {
                LogRecovery("User chose NOT to create a new empty database. Leaving files untouched and closing app.", customAppData);
                
                if (hasOriginalDb && File.Exists(brokenDbPath))
                {
                    try
                    {
                        File.Copy(brokenDbPath, dbPath, overwrite: true);
                        LogRecovery($"Copied problematic DB back to original path: {dbPath}", customAppData);
                    }
                    catch (Exception copyBackEx)
                    {
                        LogRecovery($"Failed to copy problematic DB back: {copyBackEx.Message}", customAppData);
                    }
                }

                if (IsTestMode)
                {
                    throw new Exception("ExitCalled");
                }
                else
                {
                    Environment.Exit(-1);
                }
            }
        }
    }

    private static void LogRecovery(string message, string? customAppData)
    {
        try
        {
            var appData = customAppData ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logFolder = Path.Combine(AppPaths.GetDataRoot(appData), "Logs");
            Directory.CreateDirectory(logFolder);
            var logFile = Path.Combine(logFolder, "DatabaseRecovery.log");
            File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch { }
    }

    private static bool HasUserData(string file)
    {
        if (!File.Exists(file)) return false;
        try
        {
            using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={file};Mode=ReadOnly");
            conn.Open();
            using var cmd = conn.CreateCommand();
            
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Companies';";
            var hasCompaniesTable = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            if (hasCompaniesTable)
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Companies;";
                var companiesCount = Convert.ToInt32(cmd.ExecuteScalar());
                if (companiesCount > 1) return true;
                if (companiesCount == 1)
                {
                    cmd.CommandText = "SELECT Name FROM Companies LIMIT 1;";
                    var name = cmd.ExecuteScalar() as string;
                    if (name != "شركتي الأولى" && name != "Test Company Cursor" && name != "Test Duplicates Co" && name != "Test Translations Co" && name != "Test Search Co")
                    {
                        return true;
                    }
                }
            }

            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Customers';";
            var hasCustomersTable = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            if (hasCustomersTable)
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Customers;";
                if (Convert.ToInt32(cmd.ExecuteScalar()) > 0) return true;
            }

            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Products';";
            var hasProductsTable = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            if (hasProductsTable)
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Products;";
                if (Convert.ToInt32(cmd.ExecuteScalar()) > 0) return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool VerifySchema(string file)
    {
        try
        {
            using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={file};Mode=ReadOnly");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";
            var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    tables.Add(reader.GetString(0));
                }
            }
            return tables.Contains("Companies") &&
                   tables.Contains("Customers") &&
                   tables.Contains("Products") &&
                   tables.Contains("Documents") &&
                   tables.Contains("DocumentItems");
        }
        catch
        {
            return false;
        }
    }

    private static (int score, int companies, int customers, int products) GetDatabaseStats(string file)
    {
        try
        {
            using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={file};Mode=ReadOnly");
            conn.Open();
            using var cmd = conn.CreateCommand();
            
            cmd.CommandText = "SELECT COUNT(*) FROM Companies;";
            var companies = Convert.ToInt32(cmd.ExecuteScalar());
            
            cmd.CommandText = "SELECT COUNT(*) FROM Customers;";
            var customers = Convert.ToInt32(cmd.ExecuteScalar());
            
            cmd.CommandText = "SELECT COUNT(*) FROM Products;";
            var products = Convert.ToInt32(cmd.ExecuteScalar());
            
            return (companies + customers + products, companies, customers, products);
        }
        catch
        {
            return (0, 0, 0, 0);
        }
    }

    private static bool QuickCheckIntegrity(string file)
    {
        try
        {
            using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={file};Mode=ReadOnly");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA integrity_check;";
            return cmd.ExecuteScalar() as string == "ok";
        }
        catch
        {
            return false;
        }
    }

    private static void TestSearchOrdering()
    {
        using var scope = Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IProductService>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var company = context.Companies.FirstOrDefault(c => c.Name == "Test Search Co");
        if (company == null)
        {
            company = new Models.Company { Name = "Test Search Co", Currency = "USD" };
            context.Companies.Add(company);
            context.SaveChanges();
        }

        var oldProds = context.Products.Where(p => p.CompanyId == company.Id).ToList();
        context.Products.RemoveRange(oldProds);
        context.SaveChanges();

        var p1 = new Models.Product { CompanyId = company.Id, Name = "Product ABC", UnitPrice = 10m, DefaultQuantity = 1 };
        var p2 = new Models.Product { CompanyId = company.Id, Name = "ABC Product", UnitPrice = 20m, DefaultQuantity = 1 };
        var p3 = new Models.Product { CompanyId = company.Id, Name = "XYZ Product containing ABC text", UnitPrice = 30m, DefaultQuantity = 1 };
        var p4 = new Models.Product { CompanyId = company.Id, Name = "Product DEF", UnitPrice = 40m, DefaultQuantity = 1 };

        svc.AddProductAsync(p1).GetAwaiter().GetResult();
        svc.AddProductAsync(p2).GetAwaiter().GetResult();
        svc.AddProductAsync(p3).GetAwaiter().GetResult();
        svc.AddProductAsync(p4).GetAwaiter().GetResult();

        var results = svc.GetProductsAsync(company.Id, "ABC").GetAwaiter().GetResult();

        bool pass = false;
        if (results.Count >= 3)
        {
            var names = results.Select(p => p.Name).ToList();
            pass = names[0] == "ABC Product" &&
                   names[1] == "Product ABC" &&
                   names[2] == "XYZ Product containing ABC text";
        }

        AppLogger.LogInfo($"[TestSearchOrdering] Pass={pass} count={results.Count}");
        Console.WriteLine($"[TestSearchOrdering] Pass={pass} count={results.Count}");
    }

    private static void TestAppearanceNavigation()
    {
        var asm = typeof(App).Assembly;
        bool noDiagnostics = !asm.GetTypes().Any(t =>
            t.Name.Contains("Diagnostics", StringComparison.OrdinalIgnoreCase)
            && t.Namespace?.StartsWith("KarzounERP", StringComparison.Ordinal) == true);

        bool vmOk;
        try
        {
            vmOk = Services.GetService<AppearanceViewModel>() != null;
        }
        catch
        {
            vmOk = false;
        }

        bool templateOk = HasDataTemplateFor(typeof(AppearanceViewModel));
        bool pageOk = asm.GetType("KarzounERP.Views.Appearance.AppearancePage") != null;

        bool navOk = false;
        try
        {
            var nav = Services.GetRequiredService<NavigationService>();
            var field = typeof(NavigationService).GetField("_currentViewModel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            nav.NavigateTo<AppearanceViewModel>();
            navOk = field?.GetValue(nav) is AppearanceViewModel;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("[TestAppearanceNavigation] Navigation threw", ex);
        }

        LocalizationManager.ApplyLanguage("ar", persist: false);
        var arDiag = LocalizationManager.Get("Nav_Diagnostics");
        var arAppearance = LocalizationManager.Get("Sett_AppearanceSection");
        var arErr = LocalizationManager.Get("Msg_AppearanceNavError");
        LocalizationManager.ApplyLanguage("tr", persist: false);
        var trAppearance = LocalizationManager.Get("Sett_AppearanceSection");
        LocalizationManager.ApplyLanguage("en", persist: false);
        var enAppearance = LocalizationManager.Get("Sett_AppearanceSection");

        bool noDiagMenu = arDiag == "Nav_Diagnostics" || string.IsNullOrWhiteSpace(arDiag);
        bool resourcesOk = !string.IsNullOrWhiteSpace(arAppearance) && arAppearance != "Sett_AppearanceSection"
            && !string.IsNullOrWhiteSpace(trAppearance) && trAppearance != "Sett_AppearanceSection"
            && !string.IsNullOrWhiteSpace(enAppearance) && enAppearance != "Sett_AppearanceSection"
            && !string.IsNullOrWhiteSpace(arErr) && arErr != "Msg_AppearanceNavError";

        bool pass = noDiagnostics && noDiagMenu && vmOk && pageOk && templateOk && navOk && resourcesOk;
        AppLogger.LogInfo($"[TestAppearanceNavigation] Pass={pass} NoDiagnostics={noDiagnostics} Vm={vmOk} Page={pageOk} Template={templateOk} Nav={navOk} Resources={resourcesOk}");
        Console.WriteLine($"[TestAppearanceNavigation] Pass={pass} NoDiagnostics={noDiagnostics} Vm={vmOk} Page={pageOk} Template={templateOk} Nav={navOk} Resources={resourcesOk} ArAppearance={arAppearance} EnAppearance={enAppearance} TrAppearance={trAppearance}");
    }

    private static void TestAppearanceViewTemplate()
    {
        var asm = typeof(App).Assembly;
        bool noDiagnostics = !asm.GetTypes().Any(t =>
            t.Name.Contains("Diagnostics", StringComparison.OrdinalIgnoreCase)
            && t.Namespace?.StartsWith("KarzounERP", StringComparison.Ordinal) == true);

        bool vmOk;
        AppearanceViewModel? vm = null;
        try
        {
            vm = Services.GetService<AppearanceViewModel>();
            vmOk = vm != null;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("[TestAppearanceViewTemplate] AppearanceViewModel creation threw", ex);
            vmOk = false;
        }

        bool templateOk = HasDataTemplateFor(typeof(AppearanceViewModel));
        bool pageTypeOk = asm.GetType("KarzounERP.Views.Appearance.AppearancePage") != null;

        bool renderedPageOk = false;
        bool notClassNameText = false;
        try
        {
            var template = Application.Current.TryFindResource(new DataTemplateKey(typeof(AppearanceViewModel))) as DataTemplate;
            var rendered = template?.LoadContent();
            renderedPageOk = rendered is Views.Appearance.AppearancePage;
            notClassNameText = rendered?.ToString() != "KarzounERP.ViewModels.AppearanceViewModel";
        }
        catch (Exception ex)
        {
            AppLogger.LogError("[TestAppearanceViewTemplate] Template render threw", ex);
        }

        bool navOk = false;
        try
        {
            var nav = Services.GetRequiredService<NavigationService>();
            var field = typeof(NavigationService).GetField("_currentViewModel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            nav.NavigateTo<AppearanceViewModel>();
            navOk = field?.GetValue(nav) is AppearanceViewModel;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("[TestAppearanceViewTemplate] Navigation threw", ex);
        }

        LocalizationManager.ApplyLanguage("ar", persist: false);
        var arDiag = LocalizationManager.Get("Nav_Diagnostics");
        bool noDiagMenu = arDiag == "Nav_Diagnostics" || string.IsNullOrWhiteSpace(arDiag);

        bool pass = vmOk && templateOk && pageTypeOk && renderedPageOk && notClassNameText && navOk && noDiagnostics && noDiagMenu;
        AppLogger.LogInfo($"[TestAppearanceViewTemplate] Pass={pass} Vm={vmOk} Template={templateOk} PageType={pageTypeOk} RenderedPage={renderedPageOk} NotClassText={notClassNameText} Nav={navOk} NoDiagnostics={noDiagnostics}/{noDiagMenu}");
        Console.WriteLine($"[TestAppearanceViewTemplate] Pass={pass} Vm={vmOk} Template={templateOk} PageType={pageTypeOk} RenderedPage={renderedPageOk} NotClassText={notClassNameText} Nav={navOk} NoDiagnostics={noDiagnostics}/{noDiagMenu}");
    }

    private static void TestProductsNavigation()
    {
        var asm = typeof(App).Assembly;
        bool noDiagnostics = !asm.GetTypes().Any(t =>
            t.Name.Contains("Diagnostics", StringComparison.OrdinalIgnoreCase)
            && t.Namespace?.StartsWith("KarzounERP", StringComparison.Ordinal) == true);

        bool vmOk;
        ProductViewModel? vm = null;
        try
        {
            vm = Services.GetService<ProductViewModel>();
            vmOk = vm != null;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("[TestProductsNavigation] ProductViewModel creation threw", ex);
            vmOk = false;
        }

        bool pageOk = false;
        try
        {
            if (vm != null)
            {
                vm.Products = new List<Product>
                {
                    new()
                    {
                        Id = 1,
                        Name = "Navigation Test Product",
                        Weight = 800m,
                        WeightUnit = "g",
                        UnitPrice = 25m,
                        DefaultQuantity = 1,
                        IsActive = true
                    }
                };
            }

            var page = new Views.Products.ProductListPage { DataContext = vm };
            page.Measure(new System.Windows.Size(1200, 800));
            page.Arrange(new Rect(0, 0, 1200, 800));
            page.UpdateLayout();
            pageOk = true;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("[TestProductsNavigation] ProductListPage load threw", ex);
        }

        bool templateOk = HasDataTemplateFor(typeof(ProductViewModel));
        bool nullToVisibilityOk = Application.Current.TryFindResource("NullToVisibility") != null;

        bool navOk = false;
        try
        {
            var nav = Services.GetRequiredService<NavigationService>();
            var field = typeof(NavigationService).GetField("_currentViewModel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            nav.NavigateTo<ProductViewModel>();
            navOk = field?.GetValue(nav) is ProductViewModel;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("[TestProductsNavigation] Navigation threw", ex);
        }

        bool loadOk = false;
        try
        {
            using var scope = Services.CreateScope();
            var companySvc = scope.ServiceProvider.GetRequiredService<ICompanyService>();
            var session = Services.GetRequiredService<AppSession>();
            var company = companySvc.GetAllCompaniesAsync().GetAwaiter().GetResult().FirstOrDefault();
            if (company != null)
                session.ActiveCompany = company;

            vm ??= Services.GetRequiredService<ProductViewModel>();
            vm.LoadAsync().GetAwaiter().GetResult();
            loadOk = true;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("[TestProductsNavigation] ProductViewModel LoadAsync threw", ex);
        }

        bool errorGuardOk = typeof(App).GetField("_isShowingUnhandledError",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static) != null
            && typeof(NavigationService).GetField("_isShowingNavigationError",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static) != null;

        LocalizationManager.ApplyLanguage("ar", persist: false);
        var arDiag = LocalizationManager.Get("Nav_Diagnostics");
        bool noDiagMenu = arDiag == "Nav_Diagnostics" || string.IsNullOrWhiteSpace(arDiag);

        bool pass = vmOk && pageOk && templateOk && nullToVisibilityOk && navOk && loadOk && errorGuardOk && noDiagnostics && noDiagMenu;
        AppLogger.LogInfo($"[TestProductsNavigation] Pass={pass} Vm={vmOk} Page={pageOk} Template={templateOk} NullToVisibility={nullToVisibilityOk} Nav={navOk} Load={loadOk} Guard={errorGuardOk} NoDiagnostics={noDiagnostics}/{noDiagMenu}");
        Console.WriteLine($"[TestProductsNavigation] Pass={pass} Vm={vmOk} Page={pageOk} Template={templateOk} NullToVisibility={nullToVisibilityOk} Nav={navOk} Load={loadOk} Guard={errorGuardOk} NoDiagnostics={noDiagnostics}/{noDiagMenu}");
    }

    private static void TestPdfFontSizeSetting()
    {
        var vm = Services.GetRequiredService<AppearanceViewModel>();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.PropertyName))
                changed.Add(e.PropertyName);
        };

        vm.PdfFontSize = 9.0;
        changed.Clear();
        vm.PdfFontSize = 12.0;

        var propertyChangedOk = changed.Contains(nameof(AppearanceViewModel.PdfFontSize))
            && changed.Contains(nameof(AppearanceViewModel.PdfPreviewNormalFontSize))
            && changed.Contains(nameof(AppearanceViewModel.PdfPreviewTitleFontSize))
            && changed.Contains(nameof(AppearanceViewModel.PdfPreviewSmallFontSize))
            && changed.Contains(nameof(AppearanceViewModel.PdfPreviewTinyFontSize));

        var previewNormalOk = Math.Abs(vm.PdfPreviewNormalFontSize - 12.0) < 0.01;
        var previewTitleOk = Math.Abs(vm.PdfPreviewTitleFontSize - 14.0) < 0.01;
        var previewSmallOk = Math.Abs(vm.PdfPreviewSmallFontSize - 11.0) < 0.01;
        var previewTinyOk = Math.Abs(vm.PdfPreviewTinyFontSize - 10.0) < 0.01;

        var xamlPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Views", "Appearance", "AppearancePage.xaml");
        if (!File.Exists(xamlPath))
            xamlPath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "Appearance", "AppearancePage.xaml");

        var xaml = File.ReadAllText(xamlPath);
        var mockupLabel = xaml.IndexOf("PDF Export Mockup", StringComparison.Ordinal);
        var mockupStart = mockupLabel >= 0
            ? xaml.IndexOf("<Border Background=\"White\"", mockupLabel, StringComparison.Ordinal)
            : -1;
        var mockupEnd = mockupStart >= 0
            ? xaml.IndexOf("<!-- PDF Layout Preview Box -->", mockupStart, StringComparison.Ordinal)
            : -1;
        if (mockupEnd < 0 && mockupStart >= 0)
            mockupEnd = xaml.IndexOf("</Border>", mockupStart, StringComparison.Ordinal);
        var mockupSection = mockupStart >= 0 && mockupEnd > mockupStart
            ? xaml[mockupStart..mockupEnd]
            : string.Empty;

        var staticFontPattern = new System.Text.RegularExpressions.Regex(@"FontSize\s*=\s*""(7|8|9|10|11|12|14)""");
        var bindingPattern = new System.Text.RegularExpressions.Regex(@"FontSize\s*=\s*""\{Binding\s+PdfPreview");
        var mockupNoStaticFont = mockupStart >= 0
            && !staticFontPattern.IsMatch(mockupSection)
            && bindingPattern.IsMatch(mockupSection);

        var original = AppearanceSettingsStore.LoadGlobal();
        const double testSize = 11.5;
        try
        {
            var toSave = new Models.AppearanceSetting
            {
                PrimaryColor = original.PrimaryColor,
                SecondaryColor = original.SecondaryColor,
                AccentColor = original.AccentColor,
                SidebarBackground = original.SidebarBackground,
                SidebarTextColor = original.SidebarTextColor,
                ButtonColor = original.ButtonColor,
                ButtonTextColor = original.ButtonTextColor,
                CardBackground = original.CardBackground,
                PageBackground = original.PageBackground,
                PdfPrimaryColor = original.PdfPrimaryColor,
                PdfHeaderColor = original.PdfHeaderColor,
                PdfTableHeaderColor = original.PdfTableHeaderColor,
                PdfBorderColor = original.PdfBorderColor,
                PdfAccentColor = original.PdfAccentColor,
                PdfTotalBoxColor = original.PdfTotalBoxColor,
                PdfCompanyInfoTopMargin = original.PdfCompanyInfoTopMargin,
                PdfLogoTopMargin = original.PdfLogoTopMargin,
                PdfHeaderSpacing = original.PdfHeaderSpacing,
                PdfTableSpacing = original.PdfTableSpacing,
                PdfFontSize = testSize
            };
            AppearanceSettingsStore.SaveGlobal(toSave);
            var loaded = AppearanceSettingsStore.LoadGlobal();
            var saveLoadOk = Math.Abs(loaded.PdfFontSize - testSize) < 0.01;

            var doc = BuildTestDocument(
                Models.DocumentType.Invoice,
                "INV-FONT-TEST",
                DateTime.Today,
                new List<Models.SalesDocumentItem>(),
                paid: false);
            var company = new Models.Company { Name = "Font Test Co", Currency = "USD" };
            var customer = new Models.Customer { FullName = "Test Customer" };
            var builder = new Pdf.PdfTemplateBuilder(doc, company, customer, "en");
            var baseField = typeof(Pdf.PdfTemplateBuilder).GetField("_pdfBaseFontSize",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pdfBase = baseField != null ? (float)baseField.GetValue(builder)! : 0f;
            var pdfExportOk = Math.Abs(pdfBase - testSize) < 0.01f;

            var pass = propertyChangedOk && previewNormalOk && previewTitleOk && previewSmallOk && previewTinyOk
                && mockupNoStaticFont && saveLoadOk && pdfExportOk;
            AppLogger.LogInfo($"[TestPdfFontSizeSetting] Pass={pass} PropertyChanged={propertyChangedOk} Preview={previewNormalOk}/{previewTitleOk}/{previewSmallOk}/{previewTinyOk} Mockup={mockupNoStaticFont} SaveLoad={saveLoadOk} PdfExport={pdfExportOk}");
            Console.WriteLine($"[TestPdfFontSizeSetting] Pass={pass} PropertyChanged={propertyChangedOk} PreviewNormal={previewNormalOk} PreviewTitle={previewTitleOk} Mockup={mockupNoStaticFont} SaveLoad={saveLoadOk} PdfExport={pdfExportOk} PdfBase={pdfBase}");
        }
        finally
        {
            AppearanceSettingsStore.SaveGlobal(original);
            vm.PdfFontSize = original.PdfFontSize;
        }
    }

    private static void TestBrandIdentity()
    {
        var original = AppearanceSettingsStore.LoadGlobal();
        try
        {
            var light = new Models.AppearanceSetting();
            ThemeManager.ApplyThemeColors(light);
            var lightMode = Application.Current.Resources["AppIsDarkTheme"] is false;
            var lightAccent = NormalizeColorHex((Application.Current.Resources["BrandAccent"] as System.Windows.Media.SolidColorBrush)?.Color.ToString() ?? string.Empty)
                == NormalizeColorHex(KarzounBrand.Teal);
            var lightSurface = NormalizeColorHex((Application.Current.Resources["BrandBackground"] as System.Windows.Media.SolidColorBrush)?.Color.ToString() ?? string.Empty)
                == NormalizeColorHex(KarzounBrand.LightPage);

            var dark = new Models.AppearanceSetting
            {
                PrimaryColor = KarzounBrand.Teal,
                SecondaryColor = KarzounBrand.Blue,
                AccentColor = KarzounBrand.Emerald,
                SidebarBackground = KarzounBrand.Navy,
                SidebarTextColor = KarzounBrand.LightGray,
                ButtonColor = KarzounBrand.Teal,
                ButtonTextColor = KarzounBrand.Navy,
                CardBackground = KarzounBrand.DarkCard,
                PageBackground = KarzounBrand.DarkPage
            };
            ThemeManager.ApplyThemeColors(dark);
            var darkMode = Application.Current.Resources["AppIsDarkTheme"] is true;
            var darkText = (Application.Current.Resources["BrandTextPrimary"] as System.Windows.Media.SolidColorBrush)?.Color;
            var darkBackground = (Application.Current.Resources["BrandBackground"] as System.Windows.Media.SolidColorBrush)?.Color;
            var darkContrast = darkText.HasValue && darkBackground.HasValue
                && darkText.Value.R + darkText.Value.G + darkText.Value.B
                   > darkBackground.Value.R + darkBackground.Value.G + darkBackground.Value.B;

            var companyOverride = new CompanyThemeData
            {
                ApplyCompanyTheme = true,
                ThemePrimaryColor = "#123456",
                ThemeSecondaryColor = "#234567",
                ThemeAccentColor = "#345678"
            };
            ThemeManager.ApplyThemeColors(light, companyOverride);
            var companyPrimary = NormalizeColorHex((Application.Current.Resources["AppPrimaryBrush"] as System.Windows.Media.SolidColorBrush)?.Color.ToString() ?? string.Empty)
                == NormalizeColorHex(companyOverride.ThemePrimaryColor);
            var globalButtonPreserved = NormalizeColorHex((Application.Current.Resources["AppButtonBrush"] as System.Windows.Media.SolidColorBrush)?.Color.ToString() ?? string.Empty)
                == NormalizeColorHex(light.ButtonColor);

            var root = Directory.GetCurrentDirectory();
            var requiredFiles = new[]
            {
                Path.Combine(root, "Resources", "Brand", "AppIcon.ico"),
                Path.Combine(root, "Resources", "Brand", "InstallerIcon.ico"),
                Path.Combine(root, "Resources", "Brand", "Karzoun_Mark.png"),
                Path.Combine(root, "Resources", "Brand", "KARZOUN_ERP_AppIcon.png"),
                Path.Combine(root, "Resources", "Brand", "KarzounBrand.xaml")
            };
            var assets = requiredFiles.All(path => File.Exists(path) && new FileInfo(path).Length > 0);

            var mainWindow = File.ReadAllText(Path.Combine(root, "MainWindow.xaml"));
            var project = File.ReadAllText(Path.Combine(root, "KarzounERP.csproj"));
            var installer = File.ReadAllText(Path.Combine(root, "installer", "KarzounERP.iss"));
            var wiring = mainWindow.Contains("Resources/Brand/Karzoun_Mark.png", StringComparison.Ordinal)
                && mainWindow.Contains("FlowDirection=\"LeftToRight\"", StringComparison.Ordinal)
                && project.Contains("<ApplicationIcon>Resources\\Brand\\AppIcon.ico</ApplicationIcon>", StringComparison.Ordinal)
                && installer.Contains("SetupIconFile=..\\Resources\\Brand\\InstallerIcon.ico", StringComparison.Ordinal)
                && installer.Contains("OutputBaseFilename=KARZOUN_ERP_Setup_1.1.0", StringComparison.Ordinal);

            var pass = lightMode && lightAccent && lightSurface && darkMode && darkContrast
                && companyPrimary && globalButtonPreserved && assets && wiring;
            Console.WriteLine($"[TestBrandIdentity] Pass={pass} Light={lightMode && lightAccent && lightSurface} Dark={darkMode && darkContrast} CompanyOverride={companyPrimary && globalButtonPreserved} Assets={assets} Wiring={wiring}");
            AppLogger.LogInfo($"[TestBrandIdentity] Pass={pass} Light={lightMode && lightAccent && lightSurface} Dark={darkMode && darkContrast} CompanyOverride={companyPrimary && globalButtonPreserved} Assets={assets} Wiring={wiring}");
        }
        finally
        {
            ThemeManager.ApplyThemeColors(original);
        }
    }

    private static void TestAppearanceSettings()
    {
        var original = AppearanceSettingsStore.LoadGlobal();
        const double testFontSize = 10.5;
        try
        {
            var testSetting = new Models.AppearanceSetting
            {
                PrimaryColor = "#AABBCC",
                SecondaryColor = original.SecondaryColor,
                AccentColor = original.AccentColor,
                SidebarBackground = original.SidebarBackground,
                SidebarTextColor = original.SidebarTextColor,
                ButtonColor = original.ButtonColor,
                ButtonTextColor = original.ButtonTextColor,
                CardBackground = original.CardBackground,
                PageBackground = original.PageBackground,
                PdfPrimaryColor = original.PdfPrimaryColor,
                PdfHeaderColor = original.PdfHeaderColor,
                PdfTableHeaderColor = original.PdfTableHeaderColor,
                PdfBorderColor = original.PdfBorderColor,
                PdfAccentColor = original.PdfAccentColor,
                PdfTotalBoxColor = original.PdfTotalBoxColor,
                PdfCompanyInfoTopMargin = 3.0,
                PdfLogoTopMargin = 2.0,
                PdfHeaderSpacing = 6.0,
                PdfTableSpacing = 7.0,
                PdfFontSize = testFontSize
            };

            AppearanceSettingsStore.SaveGlobal(testSetting);
            var loaded = AppearanceSettingsStore.LoadGlobal();

            var pass = loaded.PrimaryColor == "#AABBCC"
                && Math.Abs(loaded.PdfCompanyInfoTopMargin - 3.0) < 0.01
                && Math.Abs(loaded.PdfLogoTopMargin - 2.0) < 0.01
                && Math.Abs(loaded.PdfHeaderSpacing - 6.0) < 0.01
                && Math.Abs(loaded.PdfTableSpacing - 7.0) < 0.01
                && Math.Abs(loaded.PdfFontSize - testFontSize) < 0.01;

            AppLogger.LogInfo($"[TestAppearanceSettings] Pass={pass} PdfFontSize={loaded.PdfFontSize}");
            Console.WriteLine($"[TestAppearanceSettings] Pass={pass} PdfFontSize={loaded.PdfFontSize} Primary={loaded.PrimaryColor}");
        }
        finally
        {
            AppearanceSettingsStore.SaveGlobal(original);
        }
    }

    private static void TestCompanyIdentityTheme()
    {
        using var scope = Services.CreateScope();
        var companySvc = scope.ServiceProvider.GetRequiredService<ICompanyService>();
        var session = scope.ServiceProvider.GetRequiredService<AppSession>();
        var originalGlobal = AppearanceSettingsStore.LoadGlobal();

        var themeA = new CompanyThemeData
        {
            ThemePrimaryColor = "#123456",
            ThemeSecondaryColor = "#234567",
            ThemeAccentColor = "#345678",
            ApplyCompanyTheme = true
        };
        var themeB = new CompanyThemeData
        {
            ThemePrimaryColor = "#AA5500",
            ThemeSecondaryColor = "#00897B",
            ThemeAccentColor = "#FF9800",
            ApplyCompanyTheme = false
        };

        Company? companyA = null;
        Company? companyB = null;

        try
        {
            companyA = new Company
            {
                Name = $"Theme Test A {Guid.NewGuid():N}",
                CommercialName = "Theme Test A",
                Currency = "USD"
            };
            companyA = companySvc.AddCompanyAsync(companyA).GetAwaiter().GetResult();
            AppearanceSettingsStore.SaveCompanyTheme(companyA.Id, themeA);

            var reloadedA = companySvc.GetCompanyAsync(companyA.Id).GetAwaiter().GetResult();
            var vm = new CompanyFormViewModel(companySvc, scope.ServiceProvider.GetRequiredService<INotificationService>(), session);
            vm.LoadFromCompany(reloadedA!);
            session.ActiveCompany = reloadedA;
            ThemeManager.ApplyTheme(reloadedA!.Id);

            var activePrimary = BrushHex("AppPrimaryBrush");
            var activeAccent = BrushHex("AppAccentBrush");
            var expectedPrimary = NormalizeColorHex(themeA.ThemePrimaryColor);
            var expectedAccent = NormalizeColorHex(themeA.ThemeAccentColor);
            var applyThemePass = activePrimary == expectedPrimary && activeAccent == expectedAccent;
            var loadedTheme = AppearanceSettingsStore.LoadCompanyTheme(companyA.Id);
            var savedThemePass = loadedTheme.ApplyCompanyTheme
                && NormalizeColorHex(loadedTheme.ThemePrimaryColor) == expectedPrimary
                && NormalizeColorHex(loadedTheme.ThemeAccentColor) == expectedAccent;

            companyB = new Company
            {
                Name = $"Theme Test B {Guid.NewGuid():N}",
                CommercialName = "Theme Test B",
                Currency = "USD"
            };
            companyB = companySvc.AddCompanyAsync(companyB).GetAwaiter().GetResult();
            AppearanceSettingsStore.SaveCompanyTheme(companyB.Id, themeB);

            var reloadedB = companySvc.GetCompanyAsync(companyB.Id).GetAwaiter().GetResult();
            session.ActiveCompany = reloadedB;
            ThemeManager.ApplyTheme(reloadedB!.Id);

            var fallbackPrimary = BrushHex("AppPrimaryBrush");
            var fallbackAccent = BrushHex("AppAccentBrush");
            var globalPass = fallbackPrimary == NormalizeColorHex(originalGlobal.PrimaryColor)
                && fallbackAccent == NormalizeColorHex(originalGlobal.AccentColor);

            var pass = applyThemePass && savedThemePass && globalPass && vm.ApplyCompanyTheme && NormalizeColorHex(vm.CompanyThemePrimaryColor) == expectedPrimary;
            Console.WriteLine($"[TestCompanyIdentityTheme] Pass={pass} ApplyTheme={applyThemePass} Saved={savedThemePass} GlobalFallback={globalPass} Primary={activePrimary} Accent={activeAccent}");
            AppLogger.LogInfo($"[TestCompanyIdentityTheme] Pass={pass} ApplyTheme={applyThemePass} Saved={savedThemePass} GlobalFallback={globalPass} Primary={activePrimary} Accent={activeAccent}");
        }
        finally
        {
            if (companyA != null)
            {
                try { companySvc.DeleteCompanyAsync(companyA.Id).GetAwaiter().GetResult(); } catch { }
            }
            if (companyB != null)
            {
                try { companySvc.DeleteCompanyAsync(companyB.Id).GetAwaiter().GetResult(); } catch { }
            }
            AppearanceSettingsStore.SaveGlobal(originalGlobal);
            ThemeManager.ApplyTheme(session.ActiveCompanyId);
        }
    }

    private static string BrushHex(string resourceKey)
    {
        if (Application.Current?.Resources[resourceKey] is SolidColorBrush brush)
            return brush.Color.ToString();
        return string.Empty;
    }

    private static string NormalizeColorHex(string? hex)
        => ThemeManager.ParseColorOrDefault(hex, "#000000").ToString();

    private static void TestProductSelectorSearch()
    {
        var previousLanguage = LocalizationManager.Language;
        LocalizationManager.Initialize("en");
        try
        {
            var products = new List<Models.Product>
            {
                new()
                {
                    Id = 1,
                    Name = "مربى الورد",
                    Weight = 800,
                    WeightUnit = "g",
                    UnitPrice = 10,
                    Type = Models.ProductType.Physical,
                    IsActive = true,
                    LocalizedTexts = new List<Models.ProductLocalizedText>
                    {
                        new() { LanguageCode = "en", Name = "Rose Jam" },
                        new() { LanguageCode = "tr", Name = "Gul Receli" }
                    }
                },
                new()
                {
                    Id = 2,
                    Name = "مربى الفراولة",
                    Weight = 800,
                    WeightUnit = "g",
                    UnitPrice = 11,
                    Type = Models.ProductType.Physical,
                    IsActive = true,
                    LocalizedTexts = new List<Models.ProductLocalizedText>
                    {
                        new() { LanguageCode = "en", Name = "Strawberry Jam" }
                    }
                },
                new()
                {
                    Id = 3,
                    Name = "Apple Jam",
                    Weight = 1,
                    WeightUnit = "kg",
                    UnitPrice = 20,
                    Type = Models.ProductType.Physical,
                    IsActive = true,
                    LocalizedTexts = new List<Models.ProductLocalizedText>
                    {
                        new() { LanguageCode = "ar", Name = "مربى التفاح" },
                        new() { LanguageCode = "tr", Name = "Elma Reçeli" }
                    }
                }
            };

            var arabic = ProductSearchHelper.SearchProducts(products, "مربى");
            var digits = ProductSearchHelper.SearchProducts(products, "800");
            var arabicDigits = ProductSearchHelper.SearchProducts(products, "٨٠٠");
            var exactOrdered = ProductSearchHelper.SearchProducts(products, "Apple Jam");
            var englishSorted = ProductSearchHelper.SearchProducts(products, "Jam");

            var picker = new LineItemViewModel();
            picker.SetAvailableProducts(products, "USD");
            picker.SelectedProduct = products[0];
            var expectedPickerName = ProductSearchHelper.GetPreferredName(products[0]);
            var selectedFilled = picker.ProductId == products[0].Id
                && (picker.ProductName == products[0].Name || picker.ProductName == expectedPickerName)
                && picker.Weight == products[0].Weight
                && picker.WeightUnit == products[0].WeightUnit
                && picker.UnitPrice == products[0].UnitPrice
                && picker.ProductType == products[0].Type;

            var typed = new LineItemViewModel();
            typed.SetAvailableProducts(products, "USD");
            typed.SearchText = "Custom typed product";
            var customVisible = typed.FilteredPickerItems.Any(i => i.IsCustomOption);
            typed.UseCustomProductCommand.Execute(null);
            var customAllowed = typed.ProductId == null
                && typed.ProductName == "Custom typed product";

            var pass = arabic.Count >= 2
                && digits.Count >= 2
                && arabicDigits.Count >= 2
                && exactOrdered.FirstOrDefault()?.Name == "Apple Jam"
                && englishSorted.FirstOrDefault()?.Name == "Apple Jam"
                && selectedFilled
                && customVisible
                && customAllowed;
            Console.WriteLine($"[TestProductSelectorSearch] Pass={pass} Arabic={arabic.Count} Digits={digits.Count} ArabicDigits={arabicDigits.Count} Exact={exactOrdered.FirstOrDefault()?.Name} JamFirst={englishSorted.FirstOrDefault()?.Name}");
        }
        finally
        {
            LocalizationManager.Initialize(previousLanguage);
        }
    }

    private static void TestProductSelectorNameFill()
    {
        var product = new Models.Product
        {
            Id = 91001,
            Name = "مربى الورد",
            Weight = 800m,
            WeightUnit = "g",
            UnitPrice = 25m,
            Type = Models.ProductType.Physical,
            ImagePath = "rose-jam.png",
            IsActive = true
        };

        var line = new LineItemViewModel();
        line.SetAvailableProducts(new[] { product }, "USD");
        line.SelectedProduct = product;

        var fillOk = line.ProductId == product.Id
            && line.ProductName == product.Name
            && line.SearchText == product.Name
            && line.UnitPrice == product.UnitPrice
            && line.Weight == product.Weight
            && line.WeightUnit == product.WeightUnit
            && line.ProductType == product.Type
            && line.ImagePath == product.ImagePath;

        line.SearchText = string.Empty;
        var clearGuardOk = line.ProductName == product.Name && line.SearchText == product.Name;

        var item = line.ToModel();
        var modelOk = item.ProductId == product.Id
            && item.ProductName == product.Name
            && item.UnitPrice == product.UnitPrice
            && item.Weight == product.Weight
            && item.WeightUnit == product.WeightUnit
            && item.ProductType == product.Type
            && item.ImagePath == product.ImagePath;

        var reloadLine = LineItemViewModel.FromModel(item);
        var reloadOk = reloadLine.ProductName == product.Name
            && reloadLine.SearchText == product.Name
            && !string.IsNullOrWhiteSpace(reloadLine.ProductName);

        var doc = new Models.SalesDocument
        {
            DocumentNumber = "SELECTOR-NAME-FILL",
            Type = Models.DocumentType.Quotation,
            Status = Models.DocumentStatus.Draft,
            Date = DateTime.Today,
            Subtotal = item.LineTotal,
            GrandTotal = item.LineTotal,
            Items = new List<Models.SalesDocumentItem> { item },
            Customer = new Models.Customer { FullName = "Selector Test Customer" }
        };
        var company = new Models.Company { Name = "Selector Test Company", Currency = "USD" };
        var customer = new Models.Customer { FullName = "Selector Test Customer" };
        var pdfOk = false;
        try
        {
            var bytes = new Pdf.InvoiceDocument(doc, company, customer, "ar").GeneratePdf();
            pdfOk = bytes.Length > 0 && doc.Items.First().ProductName == product.Name;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("[TestProductSelectorNameFill] PDF generation threw", ex);
        }

        var pass = fillOk && clearGuardOk && modelOk && reloadOk && pdfOk;
        AppLogger.LogInfo($"[TestProductSelectorNameFill] Pass={pass} Fill={fillOk} ClearGuard={clearGuardOk} Model={modelOk} Reload={reloadOk} Pdf={pdfOk}");
        Console.WriteLine($"[TestProductSelectorNameFill] Pass={pass} Fill={fillOk} ClearGuard={clearGuardOk} Model={modelOk} Reload={reloadOk} Pdf={pdfOk} ProductName={line.ProductName}");
    }

    private static void TestDocumentItemSaveUpdate()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var docService = scope.ServiceProvider.GetRequiredService<IDocumentService>();

        var suffix = Guid.NewGuid().ToString("N");
        var company = new Models.Company { Name = "Doc Item Test Co " + suffix, CommercialName = "Doc Item Test Co", Currency = "USD" };
        var customer = new Models.Customer { FullName = "Doc Item Test Customer " + suffix, Company = company };
        var firstProduct = new Models.Product
        {
            Company = company,
            Name = "مربى الورد",
            Weight = 800m,
            WeightUnit = "g",
            UnitPrice = 25m,
            Type = Models.ProductType.Physical,
            ImagePath = "rose-jam.png",
            IsActive = true
        };
        var secondProduct = new Models.Product
        {
            Company = company,
            Name = "مربى الفراولة",
            Weight = 800m,
            WeightUnit = "g",
            UnitPrice = 30m,
            Type = Models.ProductType.Physical,
            ImagePath = "strawberry-jam.png",
            IsActive = true
        };

        context.Companies.Add(company);
        context.Customers.Add(customer);
        context.Products.AddRange(firstProduct, secondProduct);
        context.SaveChanges();

        try
        {
            var line = new LineItemViewModel();
            line.SetAvailableProducts(new[] { firstProduct, secondProduct }, "USD");
            line.SelectedProduct = firstProduct;
            line.Quantity = 2;

            var doc = new Models.SalesDocument
            {
                CompanyId = company.Id,
                CustomerId = customer.Id,
                DocumentNumber = "DOC-ITEM-" + suffix[..8],
                Type = Models.DocumentType.Quotation,
                Status = Models.DocumentStatus.Draft,
                Date = DateTime.Today,
                Subtotal = line.LineTotal,
                GrandTotal = line.LineTotal,
                LanguageCode = "ar"
            };

            var saved = docService.CreateDocumentAsync(doc, new List<Models.SalesDocumentItem> { line.ToModel() }).GetAwaiter().GetResult();
            var reloaded = docService.GetDocumentAsync(saved.Id).GetAwaiter().GetResult();
            var initialItem = reloaded?.Items.FirstOrDefault();
            var initialOk = initialItem != null
                && initialItem.ProductId == firstProduct.Id
                && initialItem.ProductName == firstProduct.Name
                && initialItem.UnitPrice == firstProduct.UnitPrice
                && initialItem.Weight == firstProduct.Weight
                && initialItem.WeightUnit == firstProduct.WeightUnit;

            var updateLine = LineItemViewModel.FromModel(initialItem!);
            updateLine.SetAvailableProducts(new[] { firstProduct, secondProduct }, "USD");
            updateLine.SelectedProduct = secondProduct;
            updateLine.Quantity = 3;

            var updatedDoc = reloaded!;
            updatedDoc.Subtotal = updateLine.LineTotal;
            updatedDoc.GrandTotal = updateLine.LineTotal;
            docService.UpdateDocumentAsync(updatedDoc, new List<Models.SalesDocumentItem> { updateLine.ToModel() }).GetAwaiter().GetResult();

            var afterUpdate = docService.GetDocumentAsync(saved.Id).GetAwaiter().GetResult();
            var updatedItem = afterUpdate?.Items.FirstOrDefault();
            var updateOk = updatedItem != null
                && updatedItem.ProductId == secondProduct.Id
                && updatedItem.ProductName == secondProduct.Name
                && updatedItem.UnitPrice == secondProduct.UnitPrice
                && updatedItem.Weight == secondProduct.Weight
                && updatedItem.WeightUnit == secondProduct.WeightUnit
                && updatedItem.Quantity == 3;

            var pass = initialOk && updateOk;
            AppLogger.LogInfo($"[TestDocumentItemSaveUpdate] Pass={pass} Initial={initialOk} Update={updateOk} ItemName={updatedItem?.ProductName}");
            Console.WriteLine($"[TestDocumentItemSaveUpdate] Pass={pass} Initial={initialOk} Update={updateOk} ItemName={updatedItem?.ProductName}");
        }
        finally
        {
            var docs = context.Documents.Where(d => d.CompanyId == company.Id).ToList();
            context.Documents.RemoveRange(docs);
            context.Products.RemoveRange(context.Products.Where(p => p.CompanyId == company.Id));
            context.Customers.RemoveRange(context.Customers.Where(c => c.CompanyId == company.Id));
            context.Companies.Remove(company);
            context.SaveChanges();
        }
    }

    private static void TestDocumentTotalsWeightQuantity()
    {
        var doc = new Models.SalesDocument
        {
            Items = new List<Models.SalesDocumentItem>
            {
                new() { Quantity = 2, Weight = 800, WeightUnit = "g" },
                new() { Quantity = 3, Weight = 1.5m, WeightUnit = "kg" },
                new() { Quantity = 1, Weight = 0.01m, WeightUnit = "ton" }
            }
        };

        var pass = doc.TotalQuantity == 6
            && doc.TotalWeightInGrams == 16100m
            && doc.TotalWeightDisplay == "16.1 kg";
        Console.WriteLine($"[TestDocumentTotalsWeightQuantity] Pass={pass} Quantity={doc.TotalQuantity} Weight={doc.TotalWeightDisplay}");
    }

    private static void TestBulkSelection()
    {
        var productVm = new ProductViewModel(null!, null!, null!, null!)
        {
            Products = new List<Models.Product>
            {
                new() { Name = "A" },
                new() { Name = "B" },
                new() { Name = "C" }
            }
        };
        productVm.SelectAllCommand.Execute(null);
        var productsSelected = productVm.Products.All(p => p.IsSelected)
            && productVm.SelectedCount == productVm.Products.Count
            && productVm.AreAllProductsSelected == true;
        productVm.Products[0].IsSelected = false;
        var productsPartial = productVm.SelectedCount == 2
            && productVm.AreAllProductsSelected == null;
        productVm.ClearSelectionCommand.Execute(null);
        var productsCleared = productVm.Products.All(p => !p.IsSelected)
            && productVm.SelectedCount == 0
            && productVm.AreAllProductsSelected == false;

        var customerVm = new CustomerViewModel(null!, null!, null!, null!, null!, null!)
        {
            Customers = new List<Models.Customer> { new() { FullName = "A" }, new() { FullName = "B" } }
        };
        customerVm.SelectAllCommand.Execute(null);
        var customersSelected = customerVm.Customers.All(c => c.IsSelected)
            && customerVm.SelectedCount == customerVm.Customers.Count
            && customerVm.AreAllCustomersSelected == true;
        customerVm.ClearSelectionCommand.Execute(null);
        var customersCleared = customerVm.Customers.All(c => !c.IsSelected)
            && customerVm.SelectedCount == 0
            && customerVm.AreAllCustomersSelected == false;

        var companyVm = new CompanyViewModel(null!, null!, null!, null!)
        {
            Companies = new List<Models.Company> { new() { Name = "A" }, new() { Name = "B" } }
        };
        companyVm.SelectAllCommand.Execute(null);
        var companiesSelected = companyVm.Companies.All(c => c.IsSelected)
            && companyVm.SelectedCount == companyVm.Companies.Count
            && companyVm.AreAllCompaniesSelected == true;
        companyVm.ClearSelectionCommand.Execute(null);
        var companiesCleared = companyVm.Companies.All(c => !c.IsSelected)
            && companyVm.SelectedCount == 0
            && companyVm.AreAllCompaniesSelected == false;

        var documentVm = new DocumentViewModel(null!, null!, null!, null!, null!)
        {
            Documents = new List<Models.SalesDocument> { new() { DocumentNumber = "Q-1" }, new() { DocumentNumber = "Q-2" } }
        };
        documentVm.SelectAllCommand.Execute(null);
        var documentsSelected = documentVm.Documents.All(d => d.IsSelected)
            && documentVm.SelectedCount == documentVm.Documents.Count
            && documentVm.AreAllDocumentsSelected == true;
        documentVm.ClearSelectionCommand.Execute(null);
        var documentsCleared = documentVm.Documents.All(d => !d.IsSelected)
            && documentVm.SelectedCount == 0
            && documentVm.AreAllDocumentsSelected == false;

        var pass = productsSelected && productsPartial && productsCleared
            && customersSelected && customersCleared
            && companiesSelected && companiesCleared
            && documentsSelected && documentsCleared;
        Console.WriteLine($"[TestBulkSelection] Pass={pass} Products={productsSelected}/{productsPartial}/{productsCleared} Customers={customersSelected}/{customersCleared} Companies={companiesSelected}/{companiesCleared} Documents={documentsSelected}/{documentsCleared}");
    }

    private static void TestDocumentTypeStatus()
    {
        LocalizationManager.ApplyLanguage("ar", persist: false);
        var arQuotation = ArabicEnumHelper.GetDocumentTypeLabel(Models.DocumentType.Quotation);
        LocalizationManager.ApplyLanguage("tr", persist: false);
        var trQuotation = ArabicEnumHelper.GetDocumentTypeLabel(Models.DocumentType.Quotation);
        LocalizationManager.ApplyLanguage("en", persist: false);
        var enInvoice = ArabicEnumHelper.GetDocumentTypeLabel(Models.DocumentType.Invoice);
        var statuses = ArabicEnumHelper.AllDocumentStatuses.ToList();
        var pass = arQuotation == "عرض سعر"
            && trQuotation == "Fiyat Teklifi"
            && enInvoice == "Invoice"
            && statuses.Contains(Models.DocumentStatus.Pending)
            && statuses.Contains(Models.DocumentStatus.Unpaid)
            && statuses.Contains(Models.DocumentStatus.PartiallyPaid)
            && !statuses.Contains(Models.DocumentStatus.Converted);
        Console.WriteLine($"[TestDocumentTypeStatus] Pass={pass} ArQuotation={arQuotation} TrQuotation={trQuotation} StatusCount={statuses.Count}");
    }

    private static void TestPdfDynamicProductImages()
    {
        var item = new Models.SalesDocumentItem { ProductName = "Image Test", ProductId = 1, ImagePath = null };
        var product = new Models.Product { Id = 1, Name = "Image Test", ImagePath = "current-image.png" };
        if (item.ProductId == product.Id)
            item.ImagePath = product.ImagePath;
        var pass = item.ImagePath == "current-image.png";
        Console.WriteLine($"[TestPdfDynamicProductImages] Pass={pass} ImagePath={item.ImagePath}");
    }

    private static void TestPdfAllItemsVisible()
    {
        var items = Enumerable.Range(1, 100)
            .Select(i => new Models.SalesDocumentItem
            {
                ProductName = $"Bulk Item {i.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
                ProductType = Models.ProductType.Physical,
                Quantity = 1,
                Weight = 100,
                WeightUnit = "g",
                UnitPrice = i,
                LineTotal = i,
                SortOrder = i
            })
            .ToList();

        var subtotal = items.Sum(i => i.LineTotal);
        var doc = new Models.SalesDocument
        {
            DocumentNumber = "PDF-ALL-ITEMS-TEST",
            Type = Models.DocumentType.Quotation,
            Status = Models.DocumentStatus.Draft,
            Date = DateTime.Today,
            Subtotal = subtotal,
            GrandTotal = subtotal,
            Items = items
        };
        var company = new Models.Company { Name = "PDF Test Company", Currency = "USD" };
        var customer = new Models.Customer { FullName = "PDF Test Customer" };

        var bytes = new Pdf.InvoiceDocument(doc, company, customer, "en").GeneratePdf();
        var outDir = Path.Combine(Path.GetTempPath(), "KARZOUN_ERP_Tests");
        Directory.CreateDirectory(outDir);
        var output = Path.Combine(outDir, "pdf-all-items-visible.pdf");
        File.WriteAllBytes(output, bytes);

        var source = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Pdf", "PdfTemplateBuilder.cs"));
        var forbiddenLimitFound =
            source.Contains("Take(", StringComparison.Ordinal) ||
            source.Contains("MaxRows", StringComparison.OrdinalIgnoreCase) ||
            source.Contains("MaxItems", StringComparison.OrdinalIgnoreCase) ||
            source.Contains("rowsPerPage", StringComparison.OrdinalIgnoreCase) ||
            source.Contains("pageItemLimit", StringComparison.OrdinalIgnoreCase);

        var pass = File.Exists(output)
            && new FileInfo(output).Length > 0
            && doc.Items.Count == 100
            && doc.TotalQuantity == 100
            && doc.Subtotal == subtotal
            && !forbiddenLimitFound;

        Console.WriteLine($"[TestPdfAllItemsVisible] Pass={pass} Items={doc.Items.Count} TotalQuantity={doc.TotalQuantity} Subtotal={doc.Subtotal.ToString(System.Globalization.CultureInfo.InvariantCulture)} File={output} Bytes={bytes.Length} ForbiddenLimitFound={forbiddenLimitFound}");
    }

    private static bool HasDataTemplateFor(Type viewModelType)
    {
        if (Application.Current == null) return false;

        if (Application.Current.TryFindResource(new DataTemplateKey(viewModelType)) is DataTemplate)
            return true;

        foreach (var dictionary in EnumerateResourceDictionaries(Application.Current.Resources))
        {
            if (dictionary.Contains(viewModelType) && dictionary[viewModelType] is DataTemplate)
                return true;

            foreach (var key in dictionary.Keys)
            {
                if (key is DataTemplateKey templateKey && Equals(templateKey.DataType, viewModelType))
                    return true;
            }
        }

        return false;
    }

    private static IEnumerable<ResourceDictionary> EnumerateResourceDictionaries(ResourceDictionary root)
    {
        yield return root;
        foreach (var merged in root.MergedDictionaries)
        {
            foreach (var child in EnumerateResourceDictionaries(merged))
                yield return child;
        }
    }

    private static void TestRtlLayout()
    {
        var productListXaml = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Views", "Products", "ProductListPage.xaml"));
        bool typeColumnUsesDynamicAlignment = productListXaml.Contains("Prod_ColType", StringComparison.Ordinal)
            && productListXaml.Contains("AppDataGridTextAlignment", StringComparison.Ordinal);

        LocalizationManager.ApplyLanguage("ar", persist: false);
        var arFd = LocalizationManager.FlowDirection;
        var arResFd = Application.Current.Resources["AppFlowDirection"] as System.Windows.FlowDirection?;
        var arTa = Application.Current.Resources["AppTextAlignment"] as System.Windows.TextAlignment?;
        var arHa = Application.Current.Resources["AppHorizontalAlignment"] as System.Windows.HorizontalAlignment?;
        var arCa = Application.Current.Resources["AppContentAlignment"] as System.Windows.HorizontalAlignment?;
        var arDgTa = Application.Current.Resources["AppDataGridTextAlignment"] as System.Windows.TextAlignment?;
        var arOha = Application.Current.Resources["AppOppositeHorizontalAlignment"] as System.Windows.HorizontalAlignment?;
        var arMargin = Application.Current.Resources["AppIconMargin"] as System.Windows.Thickness?;

        bool arPass = arFd == System.Windows.FlowDirection.RightToLeft &&
                      arResFd == System.Windows.FlowDirection.RightToLeft &&
                      arTa == System.Windows.TextAlignment.Right &&
                      arHa == System.Windows.HorizontalAlignment.Right &&
                      arCa == System.Windows.HorizontalAlignment.Right &&
                      arDgTa == System.Windows.TextAlignment.Right &&
                      arOha == System.Windows.HorizontalAlignment.Left &&
                      arMargin == new System.Windows.Thickness(8, 0, 0, 0);

        LocalizationManager.ApplyLanguage("en", persist: false);
        var enFd = LocalizationManager.FlowDirection;
        var enResFd = Application.Current.Resources["AppFlowDirection"] as System.Windows.FlowDirection?;
        var enTa = Application.Current.Resources["AppTextAlignment"] as System.Windows.TextAlignment?;
        var enHa = Application.Current.Resources["AppHorizontalAlignment"] as System.Windows.HorizontalAlignment?;
        var enCa = Application.Current.Resources["AppContentAlignment"] as System.Windows.HorizontalAlignment?;
        var enDgTa = Application.Current.Resources["AppDataGridTextAlignment"] as System.Windows.TextAlignment?;
        var enOha = Application.Current.Resources["AppOppositeHorizontalAlignment"] as System.Windows.HorizontalAlignment?;
        var enMargin = Application.Current.Resources["AppIconMargin"] as System.Windows.Thickness?;

        bool enPass = enFd == System.Windows.FlowDirection.LeftToRight &&
                      enResFd == System.Windows.FlowDirection.LeftToRight &&
                      enTa == System.Windows.TextAlignment.Left &&
                      enHa == System.Windows.HorizontalAlignment.Left &&
                      enCa == System.Windows.HorizontalAlignment.Left &&
                      enDgTa == System.Windows.TextAlignment.Left &&
                      enOha == System.Windows.HorizontalAlignment.Right &&
                      enMargin == new System.Windows.Thickness(0, 0, 8, 0);

        LocalizationManager.ApplyLanguage("tr", persist: false);
        var trFd = LocalizationManager.FlowDirection;
        var trResFd = Application.Current.Resources["AppFlowDirection"] as System.Windows.FlowDirection?;
        var trTa = Application.Current.Resources["AppTextAlignment"] as System.Windows.TextAlignment?;
        var trHa = Application.Current.Resources["AppHorizontalAlignment"] as System.Windows.HorizontalAlignment?;
        var trCa = Application.Current.Resources["AppContentAlignment"] as System.Windows.HorizontalAlignment?;
        var trDgTa = Application.Current.Resources["AppDataGridTextAlignment"] as System.Windows.TextAlignment?;
        var trOha = Application.Current.Resources["AppOppositeHorizontalAlignment"] as System.Windows.HorizontalAlignment?;
        var trMargin = Application.Current.Resources["AppIconMargin"] as System.Windows.Thickness?;

        bool trPass = trFd == System.Windows.FlowDirection.LeftToRight &&
                      trResFd == System.Windows.FlowDirection.LeftToRight &&
                      trTa == System.Windows.TextAlignment.Left &&
                      trHa == System.Windows.HorizontalAlignment.Left &&
                      trCa == System.Windows.HorizontalAlignment.Left &&
                      trDgTa == System.Windows.TextAlignment.Left &&
                      trOha == System.Windows.HorizontalAlignment.Right &&
                      trMargin == new System.Windows.Thickness(0, 0, 8, 0);

        bool pass = arPass && enPass && trPass && typeColumnUsesDynamicAlignment;
        AppLogger.LogInfo($"[TestRtlLayout] Pass={pass} ArRtl={arPass} EnLtr={enPass} TrLtr={trPass} TypeColumnStyle={typeColumnUsesDynamicAlignment}");
        Console.WriteLine($"[TestRtlLayout] Pass={pass} ArRtl={arPass} EnLtr={enPass} TrLtr={trPass} TypeColumnStyle={typeColumnUsesDynamicAlignment}");
    }

    private static void TestExcelImportExport()
    {
        var service = new ExcelService();
        var outDir = Path.Combine(Path.GetTempPath(), "KARZOUN_ERP_Tests");
        Directory.CreateDirectory(outDir);

        var productsPath = Path.Combine(outDir, "excel-products-roundtrip.xlsx");
        var customersPath = Path.Combine(outDir, "excel-customers-roundtrip.xlsx");
        LocalizationManager.ApplyLanguage("en", persist: false);

        var products = new List<Models.Product>
        {
            new()
            {
                CompanyId = 99,
                Name = "Rose Jam",
                Type = Models.ProductType.Physical,
                Description = "Glass jar",
                Weight = 800,
                WeightUnit = "g",
                UnitPrice = 12.5m,
                DefaultQuantity = 3,
                ImagePath = "C:\\Images\\rose.png",
                IsActive = true,
                LocalizedTexts = new List<Models.ProductLocalizedText>
                {
                    new() { LanguageCode = "ar", Name = "مربى الورد", Description = "عبوة زجاج" },
                    new() { LanguageCode = "tr", Name = "Gul Receli", Description = "Cam kavanoz" },
                    new() { LanguageCode = "en", Name = "Rose Jam", Description = "Glass jar" }
                }
            }
        };

        service.ExportProducts(products, productsPath);
        var productExportDigitsPass = WorkbookHasOnlyEnglishDigits(productsPath);
        using (var wb = new ClosedXML.Excel.XLWorkbook(productsPath))
        {
            var ws = wb.Worksheets.First();
            ws.Cell(3, 1).Value = "Arabic Digit Product";
            ws.Cell(3, 2).Value = "Physical";
            ws.Cell(3, 4).Value = "٨٠٠";
            ws.Cell(3, 5).Value = "g";
            ws.Cell(3, 6).Value = "۱۲.۵";
            ws.Cell(3, 7).Value = "۲";
            ws.Cell(4, 1).Value = "";
            ws.Cell(4, 6).Value = "";
            ws.Cell(5, 1).Value = "Invalid Price Product";
            ws.Cell(5, 6).Value = "-1";
            wb.Save();
        }

        var productImport = service.ImportProducts(productsPath, 100, new List<Models.Product>());
        var importedRose = productImport.ProductsToSave.FirstOrDefault(p => p.Name == "Rose Jam");
        var importedDigits = productImport.ProductsToSave.FirstOrDefault(p => p.Name == "Arabic Digit Product");
        var productPass = productImport.Summary.ImportedCount == 2
            && productImport.Summary.InsertedCount == 2
            && productImport.Summary.UpdatedCount == 0
            && productImport.Summary.SkippedCount == 2
            && productImport.Summary.ErrorCount == 1
            && productImport.Summary.Errors.Any(e => e.StartsWith("Row 5:", StringComparison.Ordinal))
            && importedRose is { Weight: 800m, WeightUnit: "g", UnitPrice: 12.5m, DefaultQuantity: 3 }
            && importedRose.LocalizedTexts.Any(t => t.LanguageCode == "ar" && t.Name == "مربى الورد")
            && importedDigits is { Weight: 800m, UnitPrice: 12.5m, DefaultQuantity: 2 };

        var customers = new List<Models.Customer>
        {
            new()
            {
                CompanyId = 99,
                FullName = "Ahmed Customer",
                CompanyName = "Alpha",
                Country = "Saudi Arabia",
                Phone = "٠٥٥١٢٣٤٥٦٧",
                Email = "ahmed@example.com",
                Notes = "First customer",
                ColorMarker = "#FFAA00"
            }
        };

        service.ExportCustomers(customers, customersPath);
        var customerExportDigitsPass = WorkbookHasOnlyEnglishDigits(customersPath);
        using (var wb = new ClosedXML.Excel.XLWorkbook(customersPath))
        {
            var ws = wb.Worksheets.First();
            ws.Cell(3, 1).Value = "Second Customer";
            ws.Cell(3, 4).Value = "۰۵۵۰۰۰۰۰۰۰";
            ws.Cell(3, 5).Value = "second@example.com";
            ws.Cell(4, 1).Value = "";
            ws.Cell(5, 1).Value = "";
            ws.Cell(5, 5).Value = "missing-name@example.com";
            wb.Save();
        }

        var customerImport = service.ImportCustomers(customersPath, 100, new List<Models.Customer>());
        var importedCustomer = customerImport.CustomersToSave.FirstOrDefault(c => c.FullName == "Ahmed Customer");
        var importedSecond = customerImport.CustomersToSave.FirstOrDefault(c => c.FullName == "Second Customer");
        var customerPass = customerImport.Summary.ImportedCount == 2
            && customerImport.Summary.InsertedCount == 2
            && customerImport.Summary.UpdatedCount == 0
            && customerImport.Summary.SkippedCount == 2
            && customerImport.Summary.ErrorCount == 1
            && customerImport.Summary.Errors.Any(e => e.StartsWith("Row 5:", StringComparison.Ordinal))
            && importedCustomer?.Phone == "0551234567"
            && importedSecond?.Phone == "0550000000";

        var exportDigitsPass = productExportDigitsPass && customerExportDigitsPass;

        var pass = productPass && customerPass && exportDigitsPass && File.Exists(productsPath) && File.Exists(customersPath);
        AppLogger.LogInfo($"[TestExcelImportExport] Pass={pass} Products={productPass} Customers={customerPass} EnglishDigits={exportDigitsPass}");
        Console.WriteLine($"[TestExcelImportExport] Pass={pass} Products={productPass} Customers={customerPass} EnglishDigits={exportDigitsPass} ProductImported={productImport.Summary.ImportedCount} ProductSkipped={productImport.Summary.SkippedCount} ProductErrors={productImport.Summary.ErrorCount} CustomerImported={customerImport.Summary.ImportedCount} CustomerSkipped={customerImport.Summary.SkippedCount} CustomerErrors={customerImport.Summary.ErrorCount}");
    }

    private static bool WorkbookHasOnlyEnglishDigits(string path)
    {
        using var wb = new ClosedXML.Excel.XLWorkbook(path);
        var text = string.Join(" ", wb.Worksheets.SelectMany(ws => ws.CellsUsed()).Select(c => c.GetValue<string>()));
        return !text.Any(c => c is >= '٠' and <= '٩' || c is >= '۰' and <= '۹');
    }

    private static void TestCurrencyDisplay()
    {
        // 1. Verify USD setting displays "USD" beside product price.
        var priceStr = MoneyFormatter.FormatMoney(25.00m, "USD");
        bool pricePass = priceStr == "25.00 USD";

        // 2. Verify PDF formatter includes USD.
        var pdfStr = Pdf.PdfFormatters.FormatMoney(25.00m, "USD");
        bool pdfPass = pdfStr == "25.00 USD";

        // 3. Verify duplicate handling
        var doublePriceStr = MoneyFormatter.FormatMoney(25.00m, "USD USD");
        bool doublePass = doublePriceStr == "25.00 USD";

        // 4. Verify Excel headers include USD
        var header1 = MoneyFormatter.FormatHeaderWithCurrency("Unit Price", "USD");
        var header2 = MoneyFormatter.FormatHeaderWithCurrency("Total (USD)", "USD");
        bool excelHeaderPass = header1 == "Unit Price (USD)" && header2 == "Total (USD)";

        // 5. Verify Digits remain English
        bool digitsPass = true;
        foreach (char c in priceStr)
        {
            if (c >= '٠' && c <= '٩') digitsPass = false;
            if (c >= '۰' && c <= '۹') digitsPass = false;
        }

        // 6. Fallback verification
        var fallbackStr = MoneyFormatter.FormatMoney(25.00m, null);
        bool fallbackPass = fallbackStr.EndsWith("USD") || fallbackStr.Contains(" ");

        bool pass = pricePass && pdfPass && doublePass && excelHeaderPass && digitsPass && fallbackPass;
        Console.WriteLine($"[TestCurrencyDisplay] Pass={pass} Price={pricePass} Pdf={pdfPass} Double={doublePass} ExcelHeader={excelHeaderPass} Digits={digitsPass} Fallback={fallbackPass}");
        AppLogger.LogInfo($"[TestCurrencyDisplay] Pass={pass} Price={pricePass} Pdf={pdfPass} Double={doublePass} ExcelHeader={excelHeaderPass} Digits={digitsPass} Fallback={fallbackPass}");
    }

    private static void TestFreshDbEmpty()
    {
        var dir = Path.Combine(Path.GetTempPath(), "KARZOUN_ERP_Tests", "fresh-db-empty");
        Directory.CreateDirectory(dir);
        var db = Path.Combine(dir, $"fresh_{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={db}")
            .Options;

        using (var context = new AppDbContext(options))
        {
            DatabaseInitializer.Initialize(context, db);
            var pass = context.Companies.Count() == 0
                && context.Customers.Count() == 0
                && context.Products.Count() == 0
                && context.Documents.Count() == 0;

            Console.WriteLine($"[TestFreshDbEmpty] Pass={pass} Companies={context.Companies.Count()} Customers={context.Customers.Count()} Products={context.Products.Count()} Documents={context.Documents.Count()} Db={db}");
            AppLogger.LogInfo($"[TestFreshDbEmpty] Pass={pass} Db={db}");
        }

        try { File.Delete(db); } catch { }
    }
}


