# Release Checklist: KARZOUN ERP v1.0.2

This checklist outlines the manual and automated steps required to verify the correctness of localized settings and PDF generation prior to publishing.

## 1. Localized Settings Live Verification
- [ ] Run application.
- [ ] Navigate to settings page.
- [ ] Switch app language to Arabic, click "Save Language".
- [ ] Verify default document text fields show Arabic settings.
- [ ] Edit "Default Quotation Notes" to a custom Arabic string and click "Save Settings".
- [ ] Switch language to Turkish, click "Save Language".
- [ ] Verify default document text fields show Turkish settings, and that the custom Arabic string is NOT visible.
- [ ] Edit "Default Quotation Notes" to a custom Turkish string and click "Save Settings".
- [ ] Switch language to English, click "Save Language".
- [ ] Verify default document text fields show English settings, and that custom Arabic/Turkish strings are NOT visible.
- [ ] Edit "Default Quotation Notes" to a custom English string and click "Save Settings".
- [ ] Cycle back through Arabic and Turkish to verify that each language correctly retains its specific saved configuration.

## 2. Unsaved Changes Prompt Check
- [ ] Select settings text box, type some changes.
- [ ] Click another language radio button.
- [ ] Click "Save Language".
- [ ] Confirm a prompt warns you about unsaved settings changes.
- [ ] Click "Cancel" and verify the settings language radio button is reverted to the active language without losing input.
- [ ] Repeat, click "Yes" and verify the changes are written to the current language profile before applying the new language.

## 3. PDF Export Verification
- [ ] Create a new Invoice/Quotation in the application.
- [ ] Verify notes/footers are auto-populated matching the active document language.
- [ ] Export PDF.
- [ ] Check exported PDF layouts in all three languages to verify that payment details, notes, footers, and QR templates match their respective localized settings.
- [ ] If a document has a default Arabic note but is exported in English/Turkish, verify the Arabic notes are overridden with their correct target language defaults.
