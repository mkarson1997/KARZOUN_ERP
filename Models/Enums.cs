namespace KarzounERP.Models;

public enum CommercialMindset
{
    Simple = 0,
    New = 1,
    Professional = 2
}

public enum FollowUpStage
{
    New = 0,
    Contacted = 1,
    Interested = 2,
    QuotationSent = 3,
    Negotiation = 4,
    Won = 5,
    Lost = 6
}

public enum ImportanceLevel
{
    Normal = 0,
    Important = 1,
    VeryImportant = 2
}

public enum ProductType
{
    Physical = 0,
    Service = 1
}

public enum DocumentType
{
    Quotation = 0,
    Invoice = 1
}

public enum DocumentStatus
{
    Draft = 0,
    Sent = 1,
    Accepted = 2,
    Rejected = 3,
    Paid = 4,
    Cancelled = 5,
    PartiallyPaid = 6,
    Converted = 7,
    Pending = 8,
    Unpaid = 9,
    Quotation = 10
}
