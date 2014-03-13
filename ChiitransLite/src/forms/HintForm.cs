using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using ChiitransLite.translation.edict.parseresult;
using ChiitransLite.misc;
using ChiitransLite.translation.edict;
using ChiitransLite.settings;

namespace ChiitransLite.forms {
    public partial class HintForm : Form {
        private TranslationForm mainForm;
        public bool isVisible = false;
        private bool definitionsSent = false;
        private bool nameDefinitionsSent = false;

        internal class InteropMethods {

            private HintForm form;

            public InteropMethods(HintForm form) {
                this.form = form;
            }

            public void hideForm() {
                form.moveAway();
            }

            public void setHeight(int h) {
                form.Height = h;
            }

            public void setSelectedPage(string stem, int pageNum) {
                form.setSelectedPage(stem, pageNum);
            }

            public void onWheel(int units) {
                form.onWheel(units);
            }

            public void setReading(string stem, string reading) {
                form.setReading(stem, reading);
            }
        }

        public HintForm() {
            InitializeComponent();
            webBrowser1.ObjectForScripting = new BrowserInterop(webBrowser1, new InteropMethods(this));
            webBrowser1.Url = Utils.getUriForBrowser("hint.html");
            moveAway();
            //Utils.setWindowNoActivate(this.Handle);
        }

        internal void setMainForm(TranslationForm form) {
            this.mainForm = form;
        }

        private Point getRealPos(int x, int y, int anchorHeight) {
            Point newpos = new Point(x, y + anchorHeight + 5);
            Rectangle workingArea = Screen.GetWorkingArea(mainForm);
            int screenWidth = workingArea.Width + workingArea.Left;
            int screenHeight = workingArea.Height + workingArea.Top;
            if (newpos.X + Width > screenWidth)
                newpos.X = screenWidth - Width - 15;
            if (newpos.Y + Height > screenHeight)
                newpos.Y = y - (anchorHeight * 2 / 3) - Height; // + ruby height = 0.67 anchorHeight
            if (newpos.X < workingArea.Left)
                newpos.X = workingArea.Left;
            if (newpos.Y < workingArea.Top)
                newpos.Y = workingArea.Top;
            return newpos;
        }

        public void showNoActivate() {
            Winapi.ShowWindow(this.Handle, Winapi.SW_SHOWNOACTIVATE);
            Winapi.SetWindowPos(this.Handle, new IntPtr(-1), 0, 0, 0, 0, 19);
            isVisible = true;
        }

        private void moveAway() {
            this.Location = new Point(99999, 99999);
            isVisible = false;
        }

        private WordParseResult lastData = null;

        internal void display(WordParseResult part, Point where, int anchorHeight) {
            if (!definitionsSent) {
                if (Edict.instance.getDefinitions() != null) {
                    webBrowser1.callScript("setDefinitions", Utils.toJson(Edict.instance.getDefinitions()));
                    definitionsSent = true;
                }
            }
            if (!nameDefinitionsSent) {
                if (Edict.instance.getNameDefinitions() != null) {
                    webBrowser1.callScript("setNameDefinitions", Utils.toJson(Edict.instance.getNameDefinitions()));
                    nameDefinitionsSent = true;
                }
            }
            lastData = part;
            webBrowser1.callScript("show", part.asJsonFull(), Settings.app.getSelectedPage(part.getMatchStem()), Settings.app.transparentMode, Settings.app.fontSize);
            this.Width = (int)(350 * Settings.app.fontSize / 100);
            Location = getRealPos(where.X, where.Y, anchorHeight);
            showNoActivate();
        }

        internal void hideIfNotHovering() {
            Point cursorPos = Cursor.Position;
            if (!DesktopBounds.Contains(cursorPos)) {
                moveAway();
            }
        }

        internal bool onWheel(int units) {
            if (!isVisible) return false;
            return (bool) webBrowser1.callScript(units > 0 ? "pageNext" : "pagePrev");
        }

        internal void setSelectedPage(string stem, int pageNum) {
            Settings.app.setSelectedPage(stem, pageNum);
            if (mainForm != null && lastData != null && lastData.getMatchStem() == stem) {
                OkuriganaType okType = Settings.app.okuriganaType;
                if (okType == OkuriganaType.NORMAL) {
                    mainForm.updateReading(lastData.getMatchStem(), lastData.getReading(okType));
                } else if (okType == OkuriganaType.ENGLISH || okType == OkuriganaType.RUSSIAN) {
                    mainForm.updateReading(lastData.asText(), lastData.getReading(okType));
                }
            }
        }

        internal void applyTheme(string theme) {
            webBrowser1.callScript("applyTheme", theme);
        }

        private void HintForm_Shown(object sender, EventArgs e) {
        }

        internal void setReading(string stem, string reading) {
            if (mainForm != null && lastData != null && lastData.getMatchStem() == stem) {
                Settings.app.setSelectedReading(stem, reading);
                OkuriganaType okType = Settings.app.okuriganaType;
                if (okType == OkuriganaType.NORMAL) {
                    mainForm.updateReading(lastData.getMatchStem(), lastData.getReading(okType));
                } else if (okType == OkuriganaType.ENGLISH || okType == OkuriganaType.RUSSIAN) {
                    mainForm.updateReading(lastData.asText(), lastData.getReading(okType));
                }
            }
        }

        protected override bool ShowWithoutActivation {
            get {
                return true;
            }
        }
    }
}
