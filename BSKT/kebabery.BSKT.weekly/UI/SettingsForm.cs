using System;
using System.Drawing;
using System.Windows.Forms;
using OverlayMaker.Services;

namespace OverlayMaker.UI
{
    public sealed class SettingsForm : Form
    {
        private readonly TextBox _txtApiUrl;
        private readonly CheckBox _chkSaveFetchedAsJson1;
        private readonly Button _btnCopyUrl;
        private readonly Button _btnClose;

        public string ApiUrl => _txtApiUrl.Text.Trim();
        public bool SaveFetchedAsJson1 => _chkSaveFetchedAsJson1.Checked;

        public SettingsForm(string apiUrl, bool saveFetchedAsJson1)
        {
            Text = "Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(760, 150);

            var lblApiUrl = new Label
            {
                Location = new Point(12, 15),
                Size = new Size(520, 20),
                Text = "API URL"
            };

            _txtApiUrl = new TextBox
            {
                Location = new Point(12, 38),
                Size = new Size(640, 23),
                Text = apiUrl
            };

            _btnCopyUrl = new Button
            {
                Location = new Point(658, 36),
                Size = new Size(90, 26),
                Text = "Copy URL"
            };
            _btnCopyUrl.Click += (_, __) => ClipboardService.CopyText(_txtApiUrl.Text.Trim());

            _chkSaveFetchedAsJson1 = new CheckBox
            {
                Location = new Point(12, 74),
                Size = new Size(240, 22),
                Text = "Save fetched result as JSON1",
                Checked = saveFetchedAsJson1
            };

            _btnClose = new Button
            {
                Location = new Point(658, 112),
                Size = new Size(90, 28),
                Text = "Close",
                DialogResult = DialogResult.OK
            };

            Controls.Add(lblApiUrl);
            Controls.Add(_txtApiUrl);
            Controls.Add(_btnCopyUrl);
            Controls.Add(_chkSaveFetchedAsJson1);
            Controls.Add(_btnClose);

            AcceptButton = _btnClose;
        }
    }
}
