using System.Linq;
using Windows.ApplicationModel.DataTransfer;

namespace P4ConvertToMarkup
{
    /// <summary>
    /// Main entry program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main entry point.
        /// </summary>
        [STAThread]
        static async Task Main(string[] args)
        {
            try
            {
                Program.Debug($"{typeof(Program).Name}.Main Usage: ./p4markup [streamname]");
                Program.Debug($"{typeof(Program).Name}.Main stream: {args[0]}");

                P4ConvertToMarkup convert = new();
                string markup = await convert.ConvertClipboardhHistoryToP4Markup(args[0]);
                TextCopy.ClipboardService.SetText(markup);

            }
            catch (Exception ex)
            {
                Program.Debug($"{typeof(Program).Name}.Main Error: {ex}");
                
                throw;
            }
        }

        public static void Debug(string msg)
        {
            Console.WriteLine($"{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")} - {msg}");
        }
    }


    /// <summary>
    /// Converts clipboard history, to p4 markup.
    /// </summary>
    public class P4ConvertToMarkup
    {

        private readonly static string P4Markup = "[$stream] [[$shortCode]($url)] - $summary";

        /// <summary>
        /// Converts last 2 entries from clipboard history, to P4 markup, and puts it in the current clipboard.
        /// </summary>
        public async Task<string> ConvertClipboardhHistoryToP4Markup(string stream)
        {
            Program.Debug($"{GetType().Name}.ConvertClipboardhHistoryToP4Markup:-- START, stream: {stream}, P4Markup template: {P4Markup}");

            ClipBoardHistory history = new();
            List<string> clips = await history.GetClipboardHistory();
            Program.Debug($"{GetType().Name}.ConvertClipboardhHistoryToP4Markup got clips: {clips.Count}");

            if (clips.Count < 2)
            {
                Program.Debug($"{GetType().Name}.ConvertClipboardhHistoryToP4Markup not enough clips, clip[0|1] must be the URL or summary");
                return string.Empty;
            }

            string clip1 = clips[0];
            string clip2 = clips[1];

            if (!GetDataFromClips(clip1, clip2, out string url, out string summary, out string shortCode))
            {
                return string.Empty;
            }

            string result = P4Markup.Replace("$stream", stream)
                           .Replace("$url", url)
                           .Replace("$shortCode", shortCode)
                           .Replace("$summary", summary);
            Program.Debug($"{GetType().Name}.ConvertClipboardhHistoryToP4Markup result: {result}");

            return result;
        }

        private bool GetDataFromClips(string clip1, string clip2, out string url, out string summary, out string shortCode)
        {
            url = null;
            shortCode = null;
            summary = null;

            Program.Debug($"{GetType().Name}.GetDataFromClips clip1: {clip1}");
            Program.Debug($"{GetType().Name}.GetDataFromClips clip2: {clip2}");

            if (clip1.Trim().ToLower().StartsWith("https://"))
            {
                url = clip1;
                summary = clip2;
            }
            if (clip2.Trim().ToLower().StartsWith("https://"))
            {
                url = clip2;
                summary = clip1;
            }

            if (string.IsNullOrEmpty(url))
            {
                Program.Debug($"{GetType().Name}.GetDataFromClips neither clip1, nor clip2 starts with https:// unable to convert.");
                return false;
            }
            if (string.IsNullOrEmpty(summary))
            {
                Program.Debug($"{GetType().Name}.GetDataFromClips neither clip1, nor clip2 contains a summary.");
                return false;
            }

            Program.Debug($"{GetType().Name}.GetDataFromClips url: {url}");
            Program.Debug($"{GetType().Name}.GetDataFromClips summary: {summary}");

            shortCode = ExtractShortCodeFromURL(url);
            Program.Debug($"{GetType().Name}.GetDataFromClips shortCode: {shortCode}");

            if (string.IsNullOrEmpty(shortCode))
            {
                Program.Debug($"{GetType().Name}.GetDataFromClips the url: '{url}' did not contain the shortcode.");
                return false;
            }

            return true;
        }


        private string ExtractShortCodeFromURL(string url)
        {
            return url.Trim().Split("/").Last();
        }
    }


    /// <summary>
    /// Provides clipboard history access
    /// </summary>
    public class ClipBoardHistory
    {

        public async Task<List<string>> GetClipboardHistory()
        {
            Program.Debug($"{GetType().Name}.ClipBoardHistory:-- START");

            List<string> results = new();
            ClipboardHistoryItemsResult items = await Clipboard.GetHistoryItemsAsync();
            
            for (int i=0; i<items.Items.Count; i++)
            {
                ClipboardHistoryItem? item = items.Items[i];
                try
                {
                    string data = await item?.Content?.GetTextAsync() ?? string.Empty;
                    if (!string.IsNullOrEmpty(data)) results.Add(data);
                }
                catch (Exception ex)
                {
                    Program.Debug($"{GetType().Name}.ClipBoardHistory Warning: Could not get item #{i} of {items.Items.Count} from ClipboardHistory as text: {ex}");
                }
            }

            Program.Debug($"{GetType().Name}.ClipBoardHistory:-- DONE, results: {results.Count}");
            return results;
        }
    }

}
