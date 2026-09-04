namespace KarzounERP.Reports;

public class DashboardStats
{
    public int TotalCustomers { get; set; }
    public int TotalQuotations { get; set; }
    public int TotalInvoices { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public decimal PaidTotal { get; set; }
    public decimal UnpaidTotal { get; set; }
    public List<RecentDocument> RecentDocuments { get; set; } = new();
    public List<TopCustomer> TopCustomers { get; set; } = new();
    public List<MonthlySummary> MonthlySummary { get; set; } = new();
}

public class RecentDocument
{
    public int Id { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string TypeLabel { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public DateTime Date { get; set; }
}

public class TopCustomer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int DocumentCount { get; set; }
}

public class MonthlySummary
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthLabel { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal UnpaidAmount { get; set; }
}
