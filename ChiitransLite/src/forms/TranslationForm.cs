using ChiitransLite.misc;
using ChiitransLite.settings;
using ChiitransLite.texthook;
using ChiitransLite.translation;
using ChiitransLite.translation.atlas;
using ChiitransLite.translation.edict;
using ChiitransLite.translation.edict.parseresult;
using ChiitransLite.translation.po;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChiitransLite.forms {
    public partial class TranslationForm : Form {

        private HintForm hintForm;
        private BackgroundForm backgroundForm;
        private ParseResult lastParseResult;
        private ParseOptions lastParseOptions;
        private ParseResult lastSelectedParseResult;
        private int waitingForId = -1;
        private string lastSelection = null;
        private bool lastIsRealSelection = true;
        private bool isFullscreen = false;
        
        class InteropMethods {
            private TranslationForm form;

            public InteropMethods(TranslationForm form) {
                this.form = form;
            }

            public void dragForm() {
                form.sendSysCommand(Winapi.MOUSE_MOVE);
            }

            public void dblClickCaption() {
                if (form.WindowState == FormWindowState.Maximized) {
                    form.WindowState = FormWindowState.Normal;
                } else {
                    form.WindowState = FormWindowState.Maximized;
                }
            }

            public void resizeForm(int dx, int dy) {
                if (form.WindowState != FormWindowState.Normal) {
                    return;
                }
                uint msg = 0;
                switch (dx) {
                    case -1:
                        switch (dy) {
                            case -1:
                                msg = 0xF004;
                                break;
                            case 0:
                                msg = 0xF001;
                                break;
                            case 1:
                                msg = 0xF007;
                                break;
                        }
                        break;
                    case 0:
                        switch (dy) {
                            case -1:
                                msg = 0xF003;
                                break;
                            case 1:
                                msg = 0xF006;
                                break;
                        }
                        break;
                    case 1:
                        switch (dy) {
                            case -1:
                                msg = 0xF005;
                                break;
                            case 0:
                                msg = 0xF002;
                                break;
                            case 1:
                                msg = 0xF008;
                                break;
                        }
                        break;
                }
                if (msg != 0) {
                    form.sendSysCommand(msg);
                }
            }

            public void formMinimize() {
                form.WindowState = FormWindowState.Minimized;
            }

            public void formClose() {
                form.Close();
            }

            public void showHint(double parseId, double num, double x, double y, double h, double browserW, double browserH) {
                form.showHint((int)parseId, (int)num, x, y, h, browserW, browserH);
            }

            public void hideHint() {
                form.hideHint();
            }

            public bool onWheel(int units) {
                return form.hintForm.onWheel(units);
            }

            public void setTransparentMode(bool isEnabled) {
                form.setTransparentMode(isEnabled, false);
            }

            public void setTransparencyLevel(double level) {
                form.setTransparencyLevel(level);
            }

            public void setTransparencyLevel(decimal level) {
                form.setTransparencyLevel((double)level);
            }

            public void setFontSize(double fontSize) {
                Settings.app.fontSize = fontSize;
            }

            public void setFontSize(decimal fontSize) {
                Settings.app.fontSize = (double)fontSize;
            }

            public object getOptions() {
                return new {
                    transparentMode = Settings.app.transparentMode,
                    transparencyLevel = Settings.app.transparencyLevel,
                    fontSize = Settings.app.fontSize
                };
            }

            public bool getAero() {
                return Utils.isWindowsVistaOrLater() && Winapi.DwmIsCompositionEnabled();
            }

            public void showContextMenu(string selection, bool isRealSelection, int selectedParseResultId) {
                form.showContextMenu(selection, isRealSelection, selectedParseResultId);
            }

            public void registerTranslators(object[] trans) {
                Settings.app.registerTranslators(trans.Cast<string>().ToList());
            }

            public string translateAtlas(string src) {
                return Atlas.instance.translate(src);
            }

            public string translateAtlas2(string src) {
                return Atlas.instance.translateWithReplacements(src);
            }

            public string translateCustom(string src) {
                return PoManager.instance.getTranslation(src);
            }

            public object httpRequest(string url, bool useShiftJis, string method, string query) {
                try {
                    return new { res = Utils.httpRequest(url, useShiftJis, method, query) };
                } catch (Exception ex) {
                    return new { error = ex.Message };
                }
            }

        }

        public TranslationForm() {
            InitializeComponent();

                hintForm = new HintForm();
                hintForm.setMainForm(this);
                backgroundForm = new BackgroundForm();
                backgroundForm.setMainForm(this);
                FormUtil.restoreLocation(this);
                TopMost = Settings.app.stayOnTop;
                webBrowser1.ObjectForScripting = new BrowserInterop(webBrowser1, new InteropMethods(this));
                webBrowser1.Url = Utils.getUriForBrowser("translation.html");
                TranslationService.instance.onTranslationRequest += (id, raw, src) =>
                {
                    var translators = Settings.app.getSelectedTranslators(!Atlas.instance.isNotFound);
                    if (translators.Count == 1 && Settings.session.po != null)
                    {
                        // trying .po translation
                        var poTrans = PoManager.instance.getTranslation(raw);
                        if (!string.IsNullOrEmpty(poTrans))
                        {
                            webBrowser1.callScript("newTranslationResult", id, Utils.toJson(new TranslationResult(poTrans, false)));
                            return;
                        }
                    }
                    webBrowser1.callScript("translate", id, raw, src, Utils.toJson(translators));
                };
                TranslationService.instance.onEdictDone += (id, parse) =>
                {
                    lastParseResult = parse;
                    if (id == waitingForId)
                    {
                        waitingForId = -1;
                        return;
                    }
                    lastParseOptions = null;
                    submitParseResult(parse);
                };
                if (OptionsForm.instance.Visible)
                {
                    this.SuspendTopMostBegin();
                }
                OptionsForm.instance.VisibleChanged += (sender, e) =>
                {
                    if ((sender as Form).Visible)
                    {
                        this.SuspendTopMostBegin();
                    }
                    else
                    {
                        this.SuspendTopMostEnd();
                    }
                };
                SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
                //Utils.setWindowNoActivate(this.Handle);
                Winapi.RegisterHotKey(Handle, 0, (int)Winapi.KeyModifier.None, (int)Keys.Oemtilde);
        }

        void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e) {
            if (this.Visible) {
                isFullscreen = !isFullscreen && Utils.isFullscreen();
                if (isFullscreen) {
                    setTransparentMode(false);
                    FormUtil.fixFormPosition(this);
                } else {
                    setWindowNoActivate(false);
                }
                hideHint();
                BringToFront();
                moveBackgroundForm();
                if (isFullscreen) {
                    Task.Factory.StartNew(() => {
                        Thread.Sleep(2000);
                        Invoke(new Action(() => {
                            hideHint();
                            BringToFront();
                        }));
                    });
                }
            }
        }

        private void setWindowNoActivate(bool p) {
            Utils.setWindowNoActivate(Handle, p);
            Utils.setWindowNoActivate(hintForm.Handle, p);
        }

        private static TranslationForm _instance = null;
        
        public static TranslationForm instance {
            get {
                if (_instance == null) {
                    _instance = new TranslationForm();
                }
                return _instance;
            }
        }

        public void setCaption(string s) {
            Text = s;
        }

        internal void sendSysCommand(uint command) {
            Winapi.ReleaseCapture(webBrowser1.Handle);
            Winapi.DefWindowProc(this.Handle, Winapi.WM_SYSCOMMAND, (UIntPtr)command, IntPtr.Zero);
        }

        private void TranslationForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
                this.Hide();
            } else {
                this.clipboardMonitor1.Close();
            }
        }

        public void showHint(int parseId, int num, double x, double y, double h, double browserW, double browserH) {
            WordParseResult part = TranslationService.instance.getParseResult(parseId, num);
            if (part != null) {
                Invoke(new Action(() => {
                    double qx = browserW == 0 ? 1 : webBrowser1.Width / browserW;
                    double qy = browserH == 0 ? 1 : webBrowser1.Height / browserH;
                    hintForm.display(part, webBrowser1.PointToScreen(new Point((int)Math.Round(x * qx), (int)Math.Round(y * qy))), (int)Math.Round(h * qy));
                }));
            }
        }

        public void hideHint() {
            Invoke(new Action(() => {
                hintForm.hideIfNotHovering();
            }));
        }

        private Keys lastKeyCode;
        private Keys lastModifiers;
        private long lastDate;

        private void webBrowser1_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e) {
            long now = DateTime.Now.Ticks;
            if (e.KeyCode == lastKeyCode && e.Modifiers == lastModifiers && now - lastDate < 50 * TimeSpan.TicksPerMillisecond) {
                return;
            }
            lastKeyCode = e.KeyCode;
            lastModifiers = e.Modifiers;
            lastDate = now;
            if (e.Modifiers == Keys.Control && (e.KeyCode == Keys.C || e.KeyCode == Keys.Insert)) {
                updateLastSelection();
                copyToolStripMenuItem_Click(null, null);
            } else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.V ||
                  e.Modifiers == Keys.Shift && e.KeyCode == Keys.Insert) {
                pasteToolStripMenuItem_Click(null, null);
            } else if (e.Modifiers == Keys.None && e.KeyCode == Keys.F5) {
                webBrowser1.Refresh();
            } else if (e.Modifiers == Keys.None && e.KeyCode == Keys.Space) {
                updateLastSelection();
                parseSelectionToolStripMenuItem_Click(null, null);
            } else if (e.Modifiers == Keys.None && e.KeyCode == Keys.Enter) {
                updateLastSelection();
                translateSelectionToolStripMenuItem_Click(null, null);
            } else if (e.Modifiers == Keys.None && e.KeyCode == Keys.Insert) {
                updateLastSelection();
                addNewNameToolStripMenuItem_Click(null, null);
            } else if (e.Modifiers == Keys.None && e.KeyCode == Keys.T) {
                transparentModeToolStripMenuItem_Click(null, null);
            } else if (e.Modifiers == Keys.None && e.KeyCode == Keys.Back) {
                editTextToolStripMenuItem_Click(null, null);
            } else if (e.Modifiers == Keys.None && e.KeyCode == Keys.Delete) {
                if (unrealSelection()) {
                    banWordToolStripMenuItem_Click(null, null);
                }
            } else if (e.Modifiers == Keys.None && e.KeyCode == Keys.S) {
                if (unrealSelection()) {
                    saveWordToolStripMenuItem_Click(null, null);
                }
            }
        }

        private bool unrealSelection() {
            string s = (string)webBrowser1.callScript("getCurrentWord");
            //Logger.log("word: " + s);
            if (!string.IsNullOrEmpty(s)) {
                lastIsRealSelection = false;
                lastSelection = s;
                lastSelectedParseResult = getSelectedParseResult();
                return true;
            } else {
                return false;
            }
        }

        private void TranslationForm_Move(object sender, EventArgs e) {
            if (backgroundForm == null)
            {
                return;
            }
            FormUtil.saveLocation(this);
            moveBackgroundForm();
        }

        internal void moveBackgroundForm() {
            if (Settings.app.transparentMode) {
                backgroundForm.updatePos();
            }
        }

        private void TranslationForm_Resize(object sender, EventArgs e) {
            if (backgroundForm == null)
            {
                return;
            }
            FormUtil.saveLocation(this);
            moveBackgroundForm();
        }

        internal void updateReading(string text, string reading) {
            webBrowser1.callScript("updateReading", text, reading);
        }

        private string getSelection() {
            return cleanTextSelection((string)webBrowser1.callScript("getTextSelection"));
        }

        private string cleanTextSelection(string sel) {
            if (string.IsNullOrEmpty(sel)) {
                return null;
            }
            string res = Regex.Replace(sel, @"\u200B.*?\u200B", "");
            if (res == "") {
                return sel.Replace("\u200B", "");
            } else {
                return res;
            }
        }
        
        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e) {
            bool hasText = Clipboard.ContainsText();
            pasteToolStripMenuItem.Enabled = hasText;
            bool hasSelection = !string.IsNullOrWhiteSpace(lastSelection);
            parseSelectionToolStripMenuItem.Enabled = hasSelection && lastIsRealSelection;
            addNewNameToolStripMenuItem.Enabled = hasSelection;
            translateSelectionToolStripMenuItem.Enabled = hasSelection;
            transparentModeToolStripMenuItem.Checked = Settings.app.transparentMode;
            if (hasSelection && !lastIsRealSelection) {
                banWordToolStripMenuItem.Enabled = true;
                saveWordToolStripMenuItem.Enabled = true;
                banWordToolStripMenuItem.Text = "Mark as incorrect: " + lastSelection;
            } else {
                banWordToolStripMenuItem.Enabled = false;
                saveWordToolStripMenuItem.Enabled = false;
                banWordToolStripMenuItem.Text = "Mark as incorrect";
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e) {
            try {
                string sel = lastIsRealSelection ? lastSelection : null;
                if (sel != null) {
                    Clipboard.SetText(sel);
                } else if (lastParseResult != null) {
                    Clipboard.SetText(lastParseResult.asText());
                }
            } catch {
                // some weird errors are possible
            }
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e) {
            if (Clipboard.ContainsText()) {
                string fromClipboard = Clipboard.GetText();
                TranslationService.instance.update(fromClipboard, false);
            }
        }

        private void parseSelectionToolStripMenuItem_Click(object sender, EventArgs e) {
            string sel = lastIsRealSelection ? lastSelection : null;
            if (!string.IsNullOrWhiteSpace(sel)) {
                ParseResult pr = lastSelectedParseResult;
                if (pr != null) {
                    Settings.app.removeBannedWord(sel);
                    if (lastParseOptions == null) {
                        lastParseOptions = new ParseOptions();
                    }
                    lastParseOptions.addUserWord(sel);
                    waitingForId = pr.id;
                    TranslationService.instance.updateId(pr.id, pr.asText(), lastParseOptions).ContinueWith((res) => {
                        ParseResult newRes = res.Result;
                        if (newRes != null && !newRes.getParts().Any((p) => p.asText() == sel)) {
                            webBrowser1.callScript("flash", "No match found");
                        } else {
                            submitParseResult(newRes);
                        }
                    });
                }
            }
        }

        private void submitParseResult(ParseResult parse) {
            webBrowser1.callScript("newParseResult", parse.id, ParseResult.serializeResult(parse));
        }

        private ParseResult getSelectedParseResult() {
            object idObj = webBrowser1.callScript("getSelectedEntryId");
            if (idObj == null) {
                return null;
            } else {
                int id;
                if (idObj is int) {
                    id = (int)idObj;
                } else {
                    try {
                        id = (int)((double)idObj);
                    } catch {
                        Utils.info(idObj.GetType().ToString());
                        id = 0;
                    }
                }
                if (id == 0) {
                    return lastParseResult;
                } else {
                    return TranslationService.instance.getParseResult(id);
                }
            }
        }

        private void addNewNameToolStripMenuItem_Click(object sender, EventArgs e) {
            string sel = lastSelection;
            if (!string.IsNullOrWhiteSpace(sel)) {
                ParseResult pr = lastSelectedParseResult;
                EdictMatch oldName = Edict.instance.findName(sel);
                string oldSense = "";
                string oldNameType = null;
                if (oldName != null) {
                    EdictEntry oldNameEntry = oldName.findAnyName();
                    if (oldNameEntry != null) {
                        if (oldNameEntry.sense.Count > 0 && oldNameEntry.sense[0].glossary.Count > 0) {
                            oldSense = oldNameEntry.sense[0].glossary[0];
                        }
                        oldNameType = oldNameEntry.nameType;
                    }
                }
                if (oldSense == "" && sel.All((c) => TextUtils.isKana(c))) {
                    if (Settings.app.okuriganaType == OkuriganaType.RUSSIAN) {
                        oldSense = TextUtils.kanaToCyrillic(sel);
                    } else {
                        oldSense = TextUtils.kanaToRomaji(sel);
                    }
                    oldSense = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(oldSense);
                }
                UserNameForm userNameForm = new UserNameForm();
                this.hideHint();
                this.SuspendTopMost(() => {
                    moveBackgroundForm();
                    if (userNameForm.Open(sel, oldSense, oldNameType)) {
                        string key = userNameForm.getKey();
                        string sense = userNameForm.getSense();
                        string nameType = userNameForm.getNameType();
                        if (sense != "") {
                            Settings.session.addUserName(key, sense, nameType);
                        } else {
                            Settings.session.removeUserName(key);
                        }
                        if (pr != null) {
                            TranslationService.instance.updateId(pr.id, pr.asText(), null);
                        }
                    }
                });
                moveBackgroundForm();
            }
        }

        internal void setTransparentMode(bool isEnabled, bool propagateToClient = true) {
            if (Settings.app.transparentMode != isEnabled) {
                Settings.app.transparentMode = isEnabled;
            }
            if (isEnabled) {
                this.TransparencyKey = Color.FromArgb(0, 0, 1);
                this.BackColor = Color.FromArgb(0, 0, 1);
                setWindowNoActivate(isFullscreen);
                this.backgroundForm.Show();
                moveBackgroundForm();
                backgroundForm.Refresh();
            } else {
                this.TransparencyKey = Color.Empty;
                this.BackColor = SystemColors.Window;
                this.backgroundForm.Hide();
                setWindowNoActivate(isFullscreen);
            }
            if (propagateToClient) {
                webBrowser1.callScript("setTransparentMode", isEnabled);
            }
        }

        internal void setTransparencyLevel(double level) {
            Settings.app.transparencyLevel = level;
            backgroundForm.Opacity = level * 0.01;
        }

        private void TranslationForm_Shown(object sender, EventArgs e) {
            setClipboardTranslation(Settings.app.clipboardTranslation);
            applyCurrentSettings();
            SystemEvents_DisplaySettingsChanged(null, null);
        }

        private void transparentModeToolStripMenuItem_Click(object sender, EventArgs e) {
            setTransparentMode(!Settings.app.transparentMode);
        }

        private void TranslationForm_VisibleChanged(object sender, EventArgs e) {
            if (Visible) {
                setTransparentMode(Settings.app.transparentMode);
            } else {
                backgroundForm.Hide();
            }
        }

        internal void applyCurrentSettings() {
            this.SuspendTopMostBegin();
            this.SuspendTopMostEnd();
            webBrowser1.callScript("applyTheme", Settings.app.cssTheme);
            webBrowser1.callScript("setSeparateWords", Settings.app.separateWords);
            webBrowser1.callScript("setSeparateSpeaker", Settings.app.separateSpeaker);
            hintForm.applyTheme(Settings.app.cssTheme);
        }

        internal void showContextMenu(string selection, bool isRealSelection, int selectedParseResultId) {
            lastSelection = cleanTextSelection(selection);
            lastIsRealSelection = isRealSelection;
            if (selectedParseResultId != 0) {
                lastSelectedParseResult = TranslationService.instance.getParseResult(selectedParseResultId);
            } else {
                lastSelectedParseResult = lastParseResult;
            }
            contextMenuStrip1.Show(Cursor.Position);
        }

        private void updateLastSelection() {
            lastSelection = getSelection();
            lastIsRealSelection = true;
            lastSelectedParseResult = getSelectedParseResult();
        }

        private void banWordToolStripMenuItem_Click(object sender, EventArgs e) {
            if (!lastIsRealSelection && !string.IsNullOrEmpty(lastSelection)) {
                ParseResult pr = lastSelectedParseResult;
                if (pr != null) {
                    EdictMatchType? matchType = lastSelectedParseResult.getMatchTypeOf(lastSelection);
                    if (matchType.HasValue) {
                        Settings.app.addBannedWord(lastSelection, matchType.Value);
                        TranslationService.instance.updateId(pr.id, pr.asText(), null);
                    }
                }
            }
        }

        private void translateSelectionToolStripMenuItem_Click(object sender, EventArgs e) {
            string sel = lastSelection;
            if (!string.IsNullOrWhiteSpace(sel)) {
                TranslationService.instance.update(sel, false);
            }
        }

        private void TranslationForm_Activated(object sender, EventArgs e) {
            moveBackgroundForm();
            //oppan window style
            if (TextHook.instance.isConnected) {
                try {
                    IntPtr gameWindow = TextHook.instance.currentProcess.MainWindowHandle;
                    if (gameWindow != Winapi.INVALID_HANDLE_VALUE) {
                        int wstyle = Winapi.GetWindowLong(gameWindow, Winapi.GWL_EXSTYLE);
                        if ((wstyle & Winapi.WS_EX_TOPMOST) != 0) {
                            Winapi.SetWindowPos(gameWindow, Winapi.HWND_NOTOPMOST, 0, 0, 0, 0, Winapi.SWP_NOACTIVATE | Winapi.SWP_NOMOVE | Winapi.SWP_NOSIZE);
                            /*wstyle = wstyle & ~Winapi.WS_EX_TOPMOST;
                            Logger.log("Unsetting TOPMOST style");
                            Winapi.SetWindowLong(gameWindow, Winapi.GWL_EXSTYLE, wstyle);*/
                        }
                    }
                } catch (Exception ex) {
                    Logger.logException(ex);
                }
            }
        }

        public void setClipboardTranslation(bool isEnabled) {
            if (Settings.app.clipboardTranslation != isEnabled) {
                Settings.app.clipboardTranslation = isEnabled;
            }
            _setClipboardTranslation(isEnabled);
        }

        private void _setClipboardTranslation(bool isEnabled) {
            if (isEnabled) {
                clipboardMonitor1.ClipboardChanged += clipboardMonitor1_ClipboardChanged;
                clipboardMonitor1.initialize();
            } else {
                clipboardMonitor1.ClipboardChanged -= clipboardMonitor1_ClipboardChanged;
            }
        }

        void clipboardMonitor1_ClipboardChanged(object sender, EventArgs e) {
            if (Clipboard.ContainsText()) {
                string text = Clipboard.GetText();
                if (!Settings.app.clipboardJapanese || TextUtils.containsJapanese(text)) {
                    TranslationService.instance.update(TextHookContext.cleanText(Clipboard.GetText()));
                }
            }
        }

        protected override bool ShowWithoutActivation {
            get {
                return true;
            }
        }

        const int WM_DWMCOMPOSITIONCHANGED = 0x031E;
        const int WM_HOTKEY = 0x0312;
        
        protected override void WndProc(ref Message m) {
            if (m.Msg == WM_DWMCOMPOSITIONCHANGED) {
                setTransparentMode(Settings.app.transparentMode);
            } else if (m.Msg == WM_HOTKEY) {
                hotKeyPressed();
            }
            base.WndProc(ref m);
        }

        private void hotKeyPressed() {
            IntPtr handle = Winapi.WindowFromPoint(Cursor.Position);
            if (handle != Winapi.INVALID_HANDLE_VALUE) {
                string txt = Winapi.GetWindowTextRaw(handle);
                if (!string.IsNullOrWhiteSpace(txt)) {
                    if (txt.Length > 1000) {
                        txt = txt.Substring(0, 1000);
                    }
                    TranslationService.instance.update(txt);
                    /*if (TextUtils.containsJapanese(txt)) {
                        
                    }*/
                }
            }
        }

        private void editTextToolStripMenuItem_Click(object sender, EventArgs e) {
            string text;
            if (lastParseResult != null) {
                text = lastParseResult.asText();
            } else {
                text = "";
            }
            string newText = null;
            this.SuspendTopMost(() => {
                newText = Interaction.InputBox("Enter new text:", "Edit Text", text);
            });
            if (!string.IsNullOrWhiteSpace(newText) && text != newText) {
                TranslationService.instance.update(newText);
            }
        }

        private void saveWordToolStripMenuItem_Click(object sender, EventArgs e) {
            if (!lastIsRealSelection && !string.IsNullOrEmpty(lastSelection) && lastSelectedParseResult != null) {
                foreach (ParseResult p in lastSelectedParseResult.getParts()) {
                    if (p.asText() == lastSelection && (p is WordParseResult)) {
                        saveWord(p as WordParseResult);
                        webBrowser1.callScript("flash", "Word saved.");
                    }
                }
            }
        }

        private void saveWord(WordParseResult wordParseResult) {
            EdictEntry entry = wordParseResult.getSelectedEntry();
            string kanji = string.Join(",", (from k in entry.kanji select k.text));
            string kana = string.Join(",", (from k in entry.kana select k.text));
            string meaning = string.Join("/", (from m in entry.sense select m.glossary.First()).Take(3));
            string fn = Settings.app.SaveWordPath;
            if (File.Exists(fn)) {
                foreach (string s in File.ReadAllLines(fn)) {
                    if (kanji != "") {
                        if (s.StartsWith(kanji)) {
                            return;
                        }
                    } else {
                        if (s.StartsWith("\t" + kana)) {
                            return;
                        }
                    }
                }
            }
            File.AppendAllText(fn, kanji + "\t" + kana + "\t" + meaning + "\n");
        }
    }
}
