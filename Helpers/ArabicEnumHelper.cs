using KarzounERP.Models;

namespace KarzounERP.Helpers;

public static class ArabicEnumHelper
{
    public static string GetCommercialMindsetLabel(CommercialMindset value) => value switch
    {
        CommercialMindset.Simple => LocalizationManager.Get("Mindset_Simple"),
        CommercialMindset.New => LocalizationManager.Get("Mindset_New"),
        CommercialMindset.Professional => LocalizationManager.Get("Mindset_Professional"),
        _ => value.ToString()
    };

    public static string GetFollowUpStageLabel(FollowUpStage value) => value switch
    {
        FollowUpStage.New => LocalizationManager.Get("FollowUp_New"),
        FollowUpStage.Contacted => LocalizationManager.Get("FollowUp_Contacted"),
        FollowUpStage.Interested => LocalizationManager.Get("FollowUp_Interested"),
        FollowUpStage.QuotationSent => LocalizationManager.Get("FollowUp_QuotationSent"),
        FollowUpStage.Negotiation => LocalizationManager.Get("FollowUp_Negotiation"),
        FollowUpStage.Won => LocalizationManager.Get("FollowUp_Won"),
        FollowUpStage.Lost => LocalizationManager.Get("FollowUp_Lost"),
        _ => value.ToString()
    };

    public static string GetImportanceLevelLabel(ImportanceLevel value) => value switch
    {
        ImportanceLevel.Normal => LocalizationManager.Get("Importance_Normal"),
        ImportanceLevel.Important => LocalizationManager.Get("Importance_Important"),
        ImportanceLevel.VeryImportant => LocalizationManager.Get("Importance_VeryImportant"),
        _ => value.ToString()
    };

    public static string GetProductTypeLabel(ProductType value) => value switch
    {
        ProductType.Physical => LocalizationManager.Get("ProdType_Physical"),
        ProductType.Service => LocalizationManager.Get("ProdType_Service"),
        _ => value.ToString()
    };

    public static string GetDocumentTypeLabel(DocumentType value) => value switch
    {
        DocumentType.Quotation => LocalizationManager.Get("DocType_Quotation"),
        DocumentType.Invoice => LocalizationManager.Get("DocType_Invoice"),
        _ => value.ToString()
    };

    public static string GetStatusLabel(DocumentStatus value) => value switch
    {
        DocumentStatus.Draft => LocalizationManager.Get("Status_Draft"),
        DocumentStatus.Sent => LocalizationManager.Get("Status_Sent"),
        DocumentStatus.Pending => LocalizationManager.Get("Status_Pending"),
        DocumentStatus.Accepted => LocalizationManager.Get("Status_Accepted"),
        DocumentStatus.Rejected => LocalizationManager.Get("Status_Rejected"),
        DocumentStatus.Paid => LocalizationManager.Get("Status_Paid"),
        DocumentStatus.Unpaid => LocalizationManager.Get("Status_Unpaid"),
        DocumentStatus.Cancelled => LocalizationManager.Get("Status_Cancelled"),
        DocumentStatus.PartiallyPaid => LocalizationManager.Get("Status_PartiallyPaid"),
        DocumentStatus.Converted => LocalizationManager.Get("Status_Converted"),
        DocumentStatus.Quotation => LocalizationManager.Get("Status_Quotation"),
        _ => value.ToString()
    };

    public static string GetLabel(object value) => value switch
    {
        CommercialMindset cm => GetCommercialMindsetLabel(cm),
        FollowUpStage fs => GetFollowUpStageLabel(fs),
        ImportanceLevel il => GetImportanceLevelLabel(il),
        ProductType pt => GetProductTypeLabel(pt),
        DocumentType dt => GetDocumentTypeLabel(dt),
        DocumentStatus ds => GetStatusLabel(ds),
        _ => value.ToString() ?? string.Empty
    };

    public static IEnumerable<CommercialMindset> AllCommercialMindsets =>
        Enum.GetValues<CommercialMindset>();

    public static IEnumerable<FollowUpStage> AllFollowUpStages =>
        Enum.GetValues<FollowUpStage>();

    public static IEnumerable<ImportanceLevel> AllImportanceLevels =>
        Enum.GetValues<ImportanceLevel>();

    public static IEnumerable<ProductType> AllProductTypes =>
        Enum.GetValues<ProductType>();

    public static IEnumerable<DocumentStatus> AllDocumentStatuses =>
        new[]
        {
            DocumentStatus.Draft,
            DocumentStatus.Sent,
            DocumentStatus.Pending,
            DocumentStatus.Accepted,
            DocumentStatus.Rejected,
            DocumentStatus.Paid,
            DocumentStatus.Unpaid,
            DocumentStatus.PartiallyPaid,
            DocumentStatus.Cancelled
        };

    public static IEnumerable<DocumentStatus> QuotationDocumentStatuses =>
        new[] { DocumentStatus.Quotation }.Concat(AllDocumentStatuses);
}
