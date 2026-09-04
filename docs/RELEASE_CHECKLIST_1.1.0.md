# Release Checklist v1.1.0

## 1. Safety
- [ ] Ensure database is backed up and not reset.
- [ ] Verify database initialization performs migrations automatically.

## 2. Localization & Layout
- [ ] Select Arabic and verify RTL layout.
- [ ] Select English and Turkish and verify LTR layout.
- [ ] Verify English digits format for dates and values.

## 3. Product Selection & Totals
- [ ] Verify autocomplete search in invoice/quotation details.
- [ ] Verify sum calculations of weight/quantity.
- [ ] Verify PDF outputs match selected margins and styles.

## 4. Packaging (Only after explicit User Approval)
- [ ] Compile Release build.
- [ ] Build installer.
- [ ] Create delivery ZIP.

## 5. Candidate Gate
- [ ] Manual QA completed by user.
- [ ] User explicitly said "تم، صدر النسخة" or "All good, release it".
- [ ] Confirm no final installer/ZIP was created before approval.
