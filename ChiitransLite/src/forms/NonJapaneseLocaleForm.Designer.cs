namespace ChiitransLite.forms {
    partial class NonJapaneseLocaleForm {
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NonJapaneseLocaleForm));
            this.label1 = new System.Windows.Forms.Label();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.radioButtonLE = new System.Windows.Forms.RadioButton();
            this.radioButtonAppLocale = new System.Windows.Forms.RadioButton();
            this.radioButtonRun = new System.Windows.Forms.RadioButton();
            this.checkBoxDontAsk = new System.Windows.Forms.CheckBox();
            this.buttonOk = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label1.ForeColor = System.Drawing.Color.Red;
            this.label1.Location = new System.Drawing.Point(13, 11);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(329, 114);
            this.label1.TabIndex = 0;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(13, 134);
            this.linkLabel1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(282, 16);
            this.linkLabel1.TabIndex = 1;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "How to change the system locale to Japanese";
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // radioButtonLE
            // 
            this.radioButtonLE.AutoSize = true;
            this.radioButtonLE.Location = new System.Drawing.Point(16, 199);
            this.radioButtonLE.Name = "radioButtonLE";
            this.radioButtonLE.Size = new System.Drawing.Size(310, 20);
            this.radioButtonLE.TabIndex = 2;
            this.radioButtonLE.TabStop = true;
            this.radioButtonLE.Text = "Use Locale Emulator (Windows 7 and later only)";
            this.radioButtonLE.UseVisualStyleBackColor = true;
            // 
            // radioButtonAppLocale
            // 
            this.radioButtonAppLocale.AutoSize = true;
            this.radioButtonAppLocale.Location = new System.Drawing.Point(16, 225);
            this.radioButtonAppLocale.Name = "radioButtonAppLocale";
            this.radioButtonAppLocale.Size = new System.Drawing.Size(120, 20);
            this.radioButtonAppLocale.TabIndex = 3;
            this.radioButtonAppLocale.TabStop = true;
            this.radioButtonAppLocale.Text = "Use AppLocale";
            this.radioButtonAppLocale.UseVisualStyleBackColor = true;
            // 
            // radioButtonRun
            // 
            this.radioButtonRun.AutoSize = true;
            this.radioButtonRun.Location = new System.Drawing.Point(16, 251);
            this.radioButtonRun.Name = "radioButtonRun";
            this.radioButtonRun.Size = new System.Drawing.Size(263, 20);
            this.radioButtonRun.TabIndex = 4;
            this.radioButtonRun.TabStop = true;
            this.radioButtonRun.Text = "Run the application without any changes";
            this.radioButtonRun.UseVisualStyleBackColor = true;
            // 
            // checkBoxDontAsk
            // 
            this.checkBoxDontAsk.AutoSize = true;
            this.checkBoxDontAsk.Location = new System.Drawing.Point(16, 288);
            this.checkBoxDontAsk.Name = "checkBoxDontAsk";
            this.checkBoxDontAsk.Size = new System.Drawing.Size(150, 20);
            this.checkBoxDontAsk.TabIndex = 5;
            this.checkBoxDontAsk.Text = "Do not ask me again";
            this.checkBoxDontAsk.UseVisualStyleBackColor = true;
            // 
            // buttonOk
            // 
            this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOk.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.buttonOk.Location = new System.Drawing.Point(178, 323);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(75, 23);
            this.buttonOk.TabIndex = 6;
            this.buttonOk.Text = "OK";
            this.buttonOk.UseVisualStyleBackColor = true;
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.buttonCancel.Location = new System.Drawing.Point(261, 323);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 7;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 171);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(107, 16);
            this.label2.TabIndex = 8;
            this.label2.Text = "Select an option:";
            // 
            // NonJapaneseLocaleForm
            // 
            this.Icon = ChiitransLite.Properties.Resources.ohayo_small;
            this.AcceptButton = this.buttonOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(348, 358);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOk);
            this.Controls.Add(this.checkBoxDontAsk);
            this.Controls.Add(this.radioButtonRun);
            this.Controls.Add(this.radioButtonAppLocale);
            this.Controls.Add(this.radioButtonLE);
            this.Controls.Add(this.linkLabel1);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NonJapaneseLocaleForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Non-Japanese locale";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.RadioButton radioButtonLE;
        private System.Windows.Forms.RadioButton radioButtonAppLocale;
        private System.Windows.Forms.RadioButton radioButtonRun;
        private System.Windows.Forms.CheckBox checkBoxDontAsk;
        private System.Windows.Forms.Button buttonOk;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Label label2;
    }
}