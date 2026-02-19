# kebabery.BSKT.weekly (WinForms + SkiaSharp)

Desktop tool for creating weekly basket overlay images from `.json` / `.json1` data and Alvara API fetches.

## Requirements
- Windows
- .NET 8 SDK (or Visual Studio with .NET 8)
- Microsoft Edge WebView2 Runtime (for API fetch mode)

## Run
From the repository root:

- `dotnet restore kebabery.BSKT.weekly.csproj`
- `dotnet run --project kebabery.BSKT.weekly.csproj`

## Current UI functions
### Main buttons
- **Erase**: clears loaded payload, preview, and extracted `.json1` temp folders.
- **Fetch via Alvara API**: fetches from configured API URL and builds payload from API docs.
- **Save PNG**: exports current preview as a transparent PNG.
- **Settings**: opens settings window for API URL, copy URL, and `Save fetched result as JSON1` toggle.
- **Copy PNG**: copies rendered PNG to clipboard.

### Manual Mode dropdown
- **Compile json1**: pick source `.json` + exactly 5 images, then compile `.json1`.
- **Import json/json1**: load an overlay file and render preview.
- **Copy AI prompt**: copies the AI extraction prompt to clipboard.

## API settings behavior
- Default API URL:
  `https://web1-api.alvara.xyz/bts/?tab=all&page=1&limit=10&sortBy=7DaysPriceChange&sortOrder=-1`
- If **Save fetched result as JSON1** is enabled:
  - app prompts for save location and writes a `.json1`
  - preview loads from compiled `.json1`
- If disabled:
  - app skips `.json1` save prompt
  - preview renders directly from fetched payload

## File storage location
All generated/working files are kept under the executable folder:
- `work/json1/` for extracted `.json1` images used during preview
- `work/alvara-icons/` for fetched icon copies during API compilation
- `cache/icons/` for downloaded icon cache
- API debug snapshots (`webview_jsResult.txt`, `webview_decoded.txt`, `last_api_body.json`, `first_doc.json`) next to the executable

## Fonts (optional)
For consistent typography place this file at `fonts/PPSupplySans-Regular.ttf`.
If missing, the app falls back to system fonts.

## Example data
- `example/week.json`
- optional local icons in `example/icons/`
