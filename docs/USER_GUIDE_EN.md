# User Guide: Document Text Localization (v1.0.2)

This guide explains how to set and manage language-specific company document templates (invoices and quotations) in **KARZOUN ERP**.

## Configuring Company Settings by Language
Prior to v1.0.2, document notes, footers, payment details, and QR templates were shared globally across all application languages. Starting in v1.0.2, you can customize these default fields separately for Arabic, Turkish, and English.

### Steps to Set Localized Document Defaults:
1. Navigate to the **Settings** page.
2. Select the target language from the top section (e.g. English).
3. Click the **"Save Language"** button to apply the selected language to the app.
4. Locate the **"Default Texts"** section and fill out the fields in that language:
   - Default Invoice Notes
   - Default Quotation Notes
   - Legal Footer Text
   - Default Payment Info
   - QR Code Text / URL Template
5. Click **"Save Settings"**.
6. Switch the language to Turkish or Arabic, save the language preference, modify their text fields, and save settings again to complete the configurations.

## Automatic Field Population in Documents
- When creating a new invoice or quotation, the application automatically populates note, footer, and payment fields using the default values matching the active document language.
- Changing the document language before editing the text boxes will automatically reload the template defaults for the newly selected language.
- Existing saved documents will preserve their original text strings and are not modified automatically.

## PDF Generation Fallbacks
- Exporting a document to PDF applies the localized settings associated with the target document language.
- If the document contains default Arabic strings but is exported in Turkish or English, the application automatically overrides the Arabic placeholders with the correct Turkish/English default text block to ensure clear presentation.
