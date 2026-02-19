using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace OverlayMaker.Services
{
    public static class ClipboardService
    {
        public static void CopyText(string text)
        {
            Clipboard.SetText(text ?? "");
        }

        public static void CopyPngImage(byte[] pngBytes)
        {
            using var src = new MemoryStream(pngBytes);
            using var bitmap = new Bitmap(src);

            var data = new DataObject();
            data.SetData(DataFormats.Bitmap, true, new Bitmap(bitmap));
            data.SetData("PNG", false, new MemoryStream(pngBytes));

            Clipboard.SetDataObject(data, true);
        }
    }
}
