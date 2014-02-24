namespace ChiitransLite.forms {
    partial class TranslationForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.banWordToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.parseSelectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.translateSelectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addNewNameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.transparentModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clipboardMonitor1 = new ChiitransLite.forms.ClipboardMonitor();
            this.webBrowser1 = new System.Windows.Forms.WebBrowser();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.banWordToolStripMenuItem,
            this.parseSelectionToolStripMenuItem,
            this.translateSelectionToolStripMenuItem,
            this.addNewNameToolStripMenuItem,
            this.toolStripSeparator2,
            this.copyToolStripMenuItem,
            this.pasteToolStripMenuItem,
            this.toolStripSeparator1,
            this.transparentModeToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(207, 192);
            this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
            // 
            // banWordToolStripMenuItem
            // 
            this.banWordToolStripMenuItem.Name = "banWordToolStripMenuItem";
            this.banWordToolStripMenuItem.Size = new System.Drawing.Size(206, 22);
            this.banWordToolStripMenuItem.Text = "Mark as incorrect";
            this.banWordToolStripMenuItem.Click += new System.EventHandler(this.banWordToolStripMenuItem_Click);
            // 
            // parseSelectionToolStripMenuItem
            // 
            this.parseSelectionToolStripMenuItem.Name = "parseSelectionToolStripMenuItem";
            this.parseSelectionToolStripMenuItem.ShortcutKeyDisplayString = "Space";
            this.parseSelectionToolStripMenuItem.Size = new System.Drawing.Size(206, 22);
            this.parseSelectionToolStripMenuItem.Text = "Parse selection";
            this.parseSelectionToolStripMenuItem.Click += new System.EventHandler(this.parseSelectionToolStripMenuItem_Click);
            // 
            // translateSelectionToolStripMenuItem
            // 
            this.translateSelectionToolStripMenuItem.Name = "translateSelectionToolStripMenuItem";
            this.translateSelectionToolStripMenuItem.ShortcutKeyDisplayString = "Enter";
            this.translateSelectionToolStripMenuItem.Size = new System.Drawing.Size(206, 22);
            this.translateSelectionToolStripMenuItem.Text = "Translate selection";
            this.translateSelectionToolStripMenuItem.Click += new System.EventHandler(this.translateSelectionToolStripMenuItem_Click);
            // 
            // addNewNameToolStripMenuItem
            // 
            this.addNewNameToolStripMenuItem.Name = "addNewNameToolStripMenuItem";
            this.addNewNameToolStripMenuItem.ShortcutKeyDisplayString = "Ins";
            this.addNewNameToolStripMenuItem.Size = new System.Drawing.Size(206, 22);
            this.addNewNameToolStripMenuItem.Text = "Add new name...";
            this.addNewNameToolStripMenuItem.Click += new System.EventHandler(this.addNewNameToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(203, 6);
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            this.copyToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+C";
            this.copyToolStripMenuItem.Size = new System.Drawing.Size(206, 22);
            this.copyToolStripMenuItem.Text = "Copy";
            this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
            // 
            // pasteToolStripMenuItem
            // 
            this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
            this.pasteToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+V";
            this.pasteToolStripMenuItem.Size = new System.Drawing.Size(206, 22);
            this.pasteToolStripMenuItem.Text = "Paste";
            this.pasteToolStripMenuItem.Click += new System.EventHandler(this.pasteToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(203, 6);
            // 
            // transparentModeToolStripMenuItem
            // 
            this.transparentModeToolStripMenuItem.Name = "transparentModeToolStripMenuItem";
            this.transparentModeToolStripMenuItem.ShortcutKeyDisplayString = "T";
            this.transparentModeToolStripMenuItem.Size = new System.Drawing.Size(206, 22);
            this.transparentModeToolStripMenuItem.Text = "Transparent Mode";
            this.transparentModeToolStripMenuItem.Click += new System.EventHandler(this.transparentModeToolStripMenuItem_Click);
            // 
            // clipboardMonitor1
            // 
            this.clipboardMonitor1.BackColor = System.Drawing.Color.Red;
            this.clipboardMonitor1.Location = new System.Drawing.Point(0, 0);
            this.clipboardMonitor1.Name = "clipboardMonitor1";
            this.clipboardMonitor1.Size = new System.Drawing.Size(75, 23);
            this.clipboardMonitor1.TabIndex = 1;
            this.clipboardMonitor1.Text = "clipboardMonitor1";
            this.clipboardMonitor1.Visible = false;
            // 
            // webBrowser1
            // 
            this.webBrowser1.AllowNavigation = false;
            this.webBrowser1.AllowWebBrowserDrop = false;
            this.webBrowser1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowser1.IsWebBrowserContextMenuEnabled = false;
            this.webBrowser1.Location = new System.Drawing.Point(0, 0);
            this.webBrowser1.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.ScrollBarsEnabled = false;
            this.webBrowser1.Size = new System.Drawing.Size(652, 272);
            this.webBrowser1.TabIndex = 0;
            this.webBrowser1.WebBrowserShortcutsEnabled = false;
            this.webBrowser1.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.webBrowser1_PreviewKeyDown);
            // 
            // TranslationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(652, 272);
            this.ContextMenuStrip = this.contextMenuStrip1;
            this.Controls.Add(this.clipboardMonitor1);
            this.Controls.Add(this.webBrowser1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = global::ChiitransLite.Properties.Resources.ohayo_small;
            this.KeyPreview = true;
            this.Name = "TranslationForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "[Not connected] - Chiitrans Lite";
            this.TopMost = true;
            this.Activated += new System.EventHandler(this.TranslationForm_Activated);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TranslationForm_FormClosing);
            this.Shown += new System.EventHandler(this.TranslationForm_Shown);
            this.VisibleChanged += new System.EventHandler(this.TranslationForm_VisibleChanged);
            this.Move += new System.EventHandler(this.TranslationForm_Move);
            this.Resize += new System.EventHandler(this.TranslationForm_Resize);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem parseSelectionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addNewNameToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem transparentModeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem banWordToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem translateSelectionToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private ClipboardMonitor clipboardMonitor1;
        private System.Windows.Forms.WebBrowser webBrowser1;
    }
}