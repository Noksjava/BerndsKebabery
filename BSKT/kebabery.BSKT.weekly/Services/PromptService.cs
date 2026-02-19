namespace OverlayMaker.Services
{
    public sealed class PromptService
    {
        public string BuildPrompt()
        {
            return
@"You will receive an image of a leaderboard overlay and must output ONLY valid JSON.

Goal:
Extract the top 5 rows. For each row return:
rank (int), name (string), ticker (string), change24h (number), change7d (number), and icon (string).
If you cannot infer an icon path, return an empty string for icon.

Also return:
width and height for the overlay canvas in pixels, matching the intended export size.

Output schema:
{
  ""width"": 1200,
  ""height"": 650,
  ""rows"": [
    { ""rank"": 1, ""name"": ""..."", ""ticker"": ""$..."", ""change24h"": -3.72, ""change7d"": 4.79, ""icon"": ""icons/xxx.png"" }
  ]
}

Rules:
- Output ONLY JSON, no extra text.
- Numbers must be numbers, not strings.
- Keep ticker exactly as shown, including $ if present.
- Use dot decimals.
- This Json file should be prepared and available for user to download.";
        }
    }
}
