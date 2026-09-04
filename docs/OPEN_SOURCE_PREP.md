# Open-source preparation

The ignore rules exclude build/publish output, IDE state, runtime databases and
sidecars, backups, logs, customer/runtime data folders, generated PDF/Excel exports,
installer output, archives, secrets, signing keys, and one-off local edit scripts.

Before the first public push, confirm that no already-tracked file appears in:

```powershell
git ls-files | rg -i '(^|/)(bin|obj|\.vs|logs?|backups?|runtime-data|customer-data)(/|$)|\.(db|sqlite3?|log|pdf|xlsx?|csv|tsv|exe|dll|pdb|msi|msix|appx|zip|7z)$'
```

The following local-only artifacts were classified as generated or one-off and
removed during the rebrand: root `do_*.py` transformation scripts, `replace.ps1`,
`new_import.txt`, test logs, the legacy settings screenshot, `TestPdfGeneration.cs`,
and the obsolete icon-generator source/binary/batch files. The complete local
`BrandAssets/` source/preview pack is ignored; only the four approved production
image/icon files and the central resource dictionary under `Resources/Brand/`
belong in source control.
