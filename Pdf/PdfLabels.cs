namespace FornixxCRM.Pdf;

/// <summary>PDF/Excel export labels by language (ar, tr, en).</summary>
public static class PdfLabels
{
    private static readonly Dictionary<string, Dictionary<string, string>> L = new()
    {
        ["DocTitleInvoice"] = new() { ["ar"] = "فاتورة نهائية", ["tr"] = "Nihai Fatura", ["en"] = "Final Invoice" },
        ["DocTitleQuotation"] = new() { ["ar"] = "عرض سعر", ["tr"] = "Fiyat Teklifi", ["en"] = "Quotation" },
        ["DocNo"] = new() { ["ar"] = "رقم المستند", ["tr"] = "Belge No", ["en"] = "Document No" },
        ["Date"] = new() { ["ar"] = "التاريخ", ["tr"] = "Tarih", ["en"] = "Date" },
        ["DueDate"] = new() { ["ar"] = "تاريخ الاستحقاق", ["tr"] = "Son Ödeme", ["en"] = "Due Date" },
        ["ValidUntil"] = new() { ["ar"] = "صالح حتى", ["tr"] = "Geçerlilik", ["en"] = "Valid Until" },
        ["Status"] = new() { ["ar"] = "الحالة", ["tr"] = "Durum", ["en"] = "Status" },
        ["CustInfo"] = new() { ["ar"] = "بيانات العميل", ["tr"] = "Müşteri Bilgileri", ["en"] = "Customer Information" },
        ["Name"] = new() { ["ar"] = "الاسم", ["tr"] = "Ad", ["en"] = "Name" },
        ["Company"] = new() { ["ar"] = "الشركة", ["tr"] = "Şirket", ["en"] = "Company" },
        ["Phone"] = new() { ["ar"] = "الهاتف", ["tr"] = "Telefon", ["en"] = "Phone" },
        ["Email"] = new() { ["ar"] = "البريد", ["tr"] = "E-posta", ["en"] = "Email" },
        ["Country"] = new() { ["ar"] = "الدولة", ["tr"] = "Ülke", ["en"] = "Country" },
        ["ColProduct"] = new() { ["ar"] = "اسم المنتج / الوصف", ["tr"] = "Ürün / Açıklama", ["en"] = "Product / Description" },
        ["ColType"] = new() { ["ar"] = "النوع", ["tr"] = "Tür", ["en"] = "Type" },
        ["DocType"] = new() { ["ar"] = "نوع المستند", ["tr"] = "Belge Türü", ["en"] = "Document Type" },
        ["ColWeight"] = new() { ["ar"] = "الوزن", ["tr"] = "Ağırlık", ["en"] = "Weight" },
        ["ColQty"] = new() { ["ar"] = "العدد", ["tr"] = "Miktar", ["en"] = "Qty" },
        ["ColUnitPrice"] = new() { ["ar"] = "سعر القطعة", ["tr"] = "Birim Fiyat", ["en"] = "Unit Price" },
        ["ColTotal"] = new() { ["ar"] = "الإجمالي", ["tr"] = "Toplam", ["en"] = "Total" },
        ["Subtotal"] = new() { ["ar"] = "المجموع الفرعي", ["tr"] = "Ara Toplam", ["en"] = "Subtotal" },
        ["Discount"] = new() { ["ar"] = "الخصم", ["tr"] = "İndirim", ["en"] = "Discount" },
        ["Tax"] = new() { ["ar"] = "الضريبة", ["tr"] = "Vergi", ["en"] = "Tax" },
        ["GrandTotal"] = new() { ["ar"] = "الإجمالي النهائي", ["tr"] = "Genel Toplam", ["en"] = "Grand Total" },
        ["Paid"] = new() { ["ar"] = "المدفوع", ["tr"] = "Ödenen", ["en"] = "Paid" },
        ["Remaining"] = new() { ["ar"] = "المتبقي", ["tr"] = "Kalan", ["en"] = "Remaining" },
        ["Notes"] = new() { ["ar"] = "ملاحظات", ["tr"] = "Notlar", ["en"] = "Notes" },
        ["PaymentInfo"] = new() { ["ar"] = "معلومات الدفع", ["tr"] = "Ödeme Bilgileri", ["en"] = "Payment Info" },
        ["ShippingNote"] = new() { ["ar"] = "ملاحظة الشحن", ["tr"] = "Kargo Notu", ["en"] = "Shipping Note" },
        ["Page"] = new() { ["ar"] = "صفحة", ["tr"] = "Sayfa", ["en"] = "Page" },
        ["FooterDefault"] = new() {
            ["ar"] = "المبلغ لا يشمل الشحن إلا إذا تم ذكر ذلك صراحة.",
            ["tr"] = "Aksi belirtilmedikçe kargo dahil değildir.",
            ["en"] = "Shipping is not included unless stated." },
        ["TyPhysical"] = new() { ["ar"] = "منتج", ["tr"] = "Ürün", ["en"] = "Product" },
        ["TyService"] = new() { ["ar"] = "خدمة", ["tr"] = "Hizmet", ["en"] = "Service" },
        ["StDraft"] = new() { ["ar"] = "مسودة", ["tr"] = "Taslak", ["en"] = "Draft" },
        ["StSent"] = new() { ["ar"] = "تم الإرسال", ["tr"] = "Gönderildi", ["en"] = "Sent" },
        ["StAccepted"] = new() { ["ar"] = "مقبول", ["tr"] = "Kabul Edildi", ["en"] = "Accepted" },
        ["StRejected"] = new() { ["ar"] = "مرفوض", ["tr"] = "Reddedildi", ["en"] = "Rejected" },
        ["StPaid"] = new() { ["ar"] = "مدفوع", ["tr"] = "Ödendi", ["en"] = "Paid" },
        ["StCancelled"] = new() { ["ar"] = "ملغي", ["tr"] = "İptal", ["en"] = "Cancelled" },
        ["StPartial"] = new() { ["ar"] = "مدفوع جزئياً", ["tr"] = "Kısmen Ödendi", ["en"] = "Partially Paid" },
        ["StConverted"] = new() { ["ar"] = "تم التحويل", ["tr"] = "Dönüştürüldü", ["en"] = "Converted" },
        // Excel sheet / customer columns
        ["ExcelCustomers"] = new() { ["ar"] = "العملاء", ["tr"] = "Müşteriler", ["en"] = "Customers" },
        ["ExcelDocuments"] = new() { ["ar"] = "المستندات", ["tr"] = "Belgeler", ["en"] = "Documents" },
        ["ExcelSalesReport"] = new() { ["ar"] = "تقرير المبيعات", ["tr"] = "Satış Raporu", ["en"] = "Sales Report" },
        ["CustImportance"] = new() { ["ar"] = "الأهمية", ["tr"] = "Önem", ["en"] = "Importance" },
        ["CustStage"] = new() { ["ar"] = "مرحلة المتابعة", ["tr"] = "Takip Aşaması", ["en"] = "Follow-up Stage" },
        ["CustMindset"] = new() { ["ar"] = "النمط التجاري", ["tr"] = "Ticari Yaklaşım", ["en"] = "Commercial Mindset" },
        ["CustNotes"] = new() { ["ar"] = "ملاحظات", ["tr"] = "Notlar", ["en"] = "Notes" },
        ["CustCreatedAt"] = new() { ["ar"] = "تاريخ الإنشاء", ["tr"] = "Oluşturulma", ["en"] = "Created At" },
        ["RepMonth"] = new() { ["ar"] = "الشهر", ["tr"] = "Ay", ["en"] = "Month" },
        ["RepInvoiceCount"] = new() { ["ar"] = "عدد الفواتير", ["tr"] = "Fatura Sayısı", ["en"] = "Invoice Count" },
        ["RepTotal"] = new() { ["ar"] = "الإجمالي", ["tr"] = "Toplam", ["en"] = "Total" },
        ["RepPaid"] = new() { ["ar"] = "المدفوع", ["tr"] = "Ödenen", ["en"] = "Paid" },
        ["RepUnpaid"] = new() { ["ar"] = "غير المدفوع", ["tr"] = "Ödenmemiş", ["en"] = "Unpaid" },
    };

    public static string Get(string key, string lang)
    {
        var code = lang is "ar" or "tr" or "en" ? lang : "en";
        if (!L.TryGetValue(key, out var map)) return key;
        if (map.TryGetValue(code, out var t)) return t;
        return map.GetValueOrDefault("en", key);
    }

    public static bool IsArabic(string lang) => lang == "ar";
}
