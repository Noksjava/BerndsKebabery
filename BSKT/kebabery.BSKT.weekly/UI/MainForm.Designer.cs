namespace OverlayMaker.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null!;
        private Button btnErase;
        private Button btnFetchAlvara;
        private Button btnSavePng;
        private Button btnSettings;
        private Button btnCopyPng;
        private PictureBox picPreview;
        private Label lblFile;
        private TextBox txtStatus;
        private Label lblCopyright;
        private MenuStrip menuActions;
        private ToolStripMenuItem manualModeMenuItem;
        private ToolStripMenuItem menuCompileJson1;
        private ToolStripMenuItem menuImportJson;
        private ToolStripMenuItem menuCopyPrompt;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            btnErase = new Button();
            btnFetchAlvara = new Button();
            btnSavePng = new Button();
            btnSettings = new Button();
            btnCopyPng = new Button();
            picPreview = new PictureBox();
            lblFile = new Label();
            txtStatus = new TextBox();
            lblCopyright = new Label();
            menuActions = new MenuStrip();
            manualModeMenuItem = new ToolStripMenuItem();
            menuCompileJson1 = new ToolStripMenuItem();
            menuImportJson = new ToolStripMenuItem();
            menuCopyPrompt = new ToolStripMenuItem();

            ((System.ComponentModel.ISupportInitialize)picPreview).BeginInit();
            SuspendLayout();

            menuActions.Items.AddRange(new ToolStripItem[] { manualModeMenuItem });
            menuActions.Location = new Point(0, 0);
            menuActions.Size = new Size(1006, 24);

            manualModeMenuItem.DropDownItems.AddRange(new ToolStripItem[] { menuCompileJson1, menuImportJson, menuCopyPrompt });
            manualModeMenuItem.Text = "Manual Mode";

            menuCompileJson1.Text = "Compile json1";
            menuCompileJson1.Click += btnCompileJson1_Click;

            menuImportJson.Text = "Import json/json1";
            menuImportJson.Click += btnLoadJson_Click;

            menuCopyPrompt.Text = "Copy AI prompt";
            menuCopyPrompt.Click += btnCopyPrompt_Click;

            btnErase.Location = new Point(12, 30);
            btnErase.Size = new Size(70, 32);
            btnErase.Text = "Erase";
            btnErase.Click += btnErase_Click;

            btnFetchAlvara.Location = new Point(88, 30);
            btnFetchAlvara.Size = new Size(150, 32);
            btnFetchAlvara.Text = "Fetch via Alvara API";
            btnFetchAlvara.Click += btnFetchAlvara_Click;

            btnSavePng.Location = new Point(244, 30);
            btnSavePng.Size = new Size(95, 32);
            btnSavePng.Text = "Save PNG";
            btnSavePng.Click += btnSavePng_Click;

            btnSettings.Location = new Point(345, 30);
            btnSettings.Size = new Size(95, 32);
            btnSettings.Text = "Settings";
            btnSettings.Click += btnSettings_Click;

            lblFile.Location = new Point(12, 72);
            lblFile.Size = new Size(980, 20);
            lblFile.Text = "No JSON loaded.";

            picPreview.Location = new Point(12, 98);
            picPreview.Size = new Size(980, 502);
            picPreview.BorderStyle = BorderStyle.FixedSingle;
            picPreview.SizeMode = PictureBoxSizeMode.Zoom;

            txtStatus.Location = new Point(12, 612);
            txtStatus.Size = new Size(840, 38);
            txtStatus.Multiline = true;
            txtStatus.ReadOnly = true;

            btnCopyPng.Location = new Point(862, 612);
            btnCopyPng.Size = new Size(130, 38);
            btnCopyPng.Text = "Copy PNG";
            btnCopyPng.Click += btnCopyPng_Click;

            lblCopyright.Location = new Point(12, 656);
            lblCopyright.Size = new Size(280, 20);
            lblCopyright.ForeColor = Color.DimGray;
            lblCopyright.Text = "2026 - Bernd's Kebabery";

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1006, 684);
            Controls.Add(menuActions);
            Controls.Add(btnErase);
            Controls.Add(btnFetchAlvara);
            Controls.Add(btnSavePng);
            Controls.Add(btnSettings);
            Controls.Add(lblFile);
            Controls.Add(picPreview);
            Controls.Add(txtStatus);
            Controls.Add(btnCopyPng);
            Controls.Add(lblCopyright);
            MainMenuStrip = menuActions;
            Text = "kebabery.BSKT.weekly";

            ((System.ComponentModel.ISupportInitialize)picPreview).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}
