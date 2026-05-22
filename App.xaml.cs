using FornixxCRM.Data;
using FornixxCRM.Helpers;
using FornixxCRM.Services;
using FornixxCRM.Services.Interfaces;
using FornixxCRM.ViewModels;
using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.IO;
using System.Windows;

namespace FornixxCRM;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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

        FornixxCRM.Helpers.AppLogger.LogInfo($"[App] ===== App startup {DateTime.Now:yyyy-MM-dd HH:mm:ss} =====");

        // Initialize localization BEFORE MaterialDesign so string resources are available
        var language = FornixxCRM.Helpers.LocalizationManager.LoadSavedLanguage();
        FornixxCRM.Helpers.LocalizationManager.Initialize(language);

        // Localization smoke test: after Initialize, log state, exit (no UI).
        // Usage: dotnet run --project FornixxCRM.csproj -- --test-localization
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
            PrimaryColor = PrimaryColor.Orange,
            SecondaryColor = SecondaryColor.Teal
        };
        Resources.MergedDictionaries.Add(bundledTheme);

        var defaultsDict = new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml")
        };
        Resources.MergedDictionaries.Add(defaultsDict);

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        var appDataDb = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FornixxCRM", "fornixx.db");

        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            DatabaseInitializer.Initialize(context, appDataDb);
        }

        if (e.Args.Contains("--test-company-add"))
        {
            TestCompanyAddAsync().GetAwaiter().GetResult();
            Shutdown(0);
            return;
        }

        if (e.Args.Contains("--test-navigation"))
        {
            TestNavigation();
            Shutdown(0);
            return;
        }

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
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dbFolder = Path.Combine(appData, "FornixxCRM");
        Directory.CreateDirectory(dbFolder);
        var dbPath = Path.Combine(dbFolder, "fornixx.db");

        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlite($"Data Source={dbPath}"), ServiceLifetime.Scoped);

        // Services
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

        // Window
        services.AddSingleton<MainWindow>();
    }

    public static T GetRequiredService<T>() where T : notnull
        => Services.GetRequiredService<T>();

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

    private static void GenerateTestExports()
    {
        var outDir = @"C:\Users\USER\Desktop\FornixxCRM_TestPdfs";
        Directory.CreateDirectory(outDir);

        var testDate = new DateTime(2026, 5, 22);
        var company = new Models.Company
        {
            Name = "Fornixx Corp",
            CommercialName = "Fornixx Corp",
            Phone = "+966-11-000-0000",
            Email = "info@fornixx.com",
            Address = "Riyadh, Saudi Arabia",
            Currency = "SAR"
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
                Weight = 1.5m, Quantity = 2, UnitPrice = 2200m, LineTotal = 4400m, SortOrder = 1 },
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
            var path = Path.Combine(outDir, $"fornixx_test_{suffix}.pdf");
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
            var xlsx = Path.Combine(outDir, $"fornixx_test_report_{lang}.xlsx");
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
}

