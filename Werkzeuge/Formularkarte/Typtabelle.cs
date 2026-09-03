namespace Formularkarte;

/// <summary>
/// Ordnet die WinForms-Typen des Bestands ein und benennt die Zielkomponente
/// aus EPOS.UI. Die Tabelle steht in Werkzeuge/Formularkarte/LIESMICH.md
/// noch einmal in Worten - sie ist der Kern der Umstellungsregel.
/// </summary>
public static class Typtabelle
{
    /// <summary>Typen, die ein Eingabefeld sind.</summary>
    private static readonly HashSet<string> Felder = new(StringComparer.Ordinal)
    {
        "TextBox", "RichTextBox", "MaskedTextBox", "ComboBox", "NumericUpDown",
        "CheckBox", "RadioButton", "DateTimePicker", "MonthCalendar",
        "ListBox", "CheckedListBox", "ListView", "DataGridView", "Chart",
        "PictureBox", "ProgressBar", "TrackBar", "DomainUpDown", "WebBrowser"
    };

    /// <summary>Typen, die einen Abschnitt aufmachen.</summary>
    private static readonly HashSet<string> Sektionen = new(StringComparer.Ordinal)
    {
        "GroupBox", "TabPage", "TabControl", "Panel", "FlowLayoutPanel",
        "TableLayoutPanel", "SplitContainer", "SplitterPanel"
    };

    /// <summary>Menue- und Statusleistenteile.</summary>
    private static readonly HashSet<string> Leisten = new(StringComparer.Ordinal)
    {
        "MenuStrip", "StatusStrip", "ToolStrip", "ContextMenuStrip",
        "ToolStripMenuItem", "ToolStripSeparator", "ToolStripStatusLabel",
        "ToolStripButton", "ToolStripLabel", "ToolStripComboBox",
        "ToolStripDropDownButton", "ToolStripTextBox", "ToolStripProgressBar"
    };

    /// <summary>
    /// Typen, die im Designer stehen, aber kein Steuerelement sind - sie
    /// zaehlen in keiner Spalte der Karte mit.
    /// </summary>
    private static readonly HashSet<string> Beiwerk = new(StringComparer.Ordinal)
    {
        "IContainer", "Container", "ComponentResourceManager", "ResourceManager",
        "ChartArea", "Series", "Legend", "Title", "DataPoint", "StripLine",
        "DataGridViewCellStyle", "DataGridViewTextBoxColumn", "DataGridViewCheckBoxColumn",
        "DataGridViewComboBoxColumn", "DataGridViewButtonColumn", "DataGridViewImageColumn",
        "RowStyle", "ColumnStyle", "ColumnHeader", "ListViewItem", "ListViewGroup",
        "TreeNode", "ToolTip", "ErrorProvider", "HelpProvider", "ImageList", "Timer",
        "BindingSource", "OpenFileDialog", "SaveFileDialog", "FolderBrowserDialog",
        "FontDialog", "ColorDialog", "PrintDialog", "PrintDocument", "PrintPreviewDialog",
        "BackgroundWorker", "NotifyIcon", "Padding", "Point", "Size", "SizeF", "Font",
        "Color", "Bitmap", "Icon"
    };

    /// <summary>Beschriftungen.</summary>
    private static readonly HashSet<string> Beschriftungen = new(StringComparer.Ordinal)
    {
        "Label", "LinkLabel"
    };

    /// <summary>Ordnet einen einfachen Typnamen ein.</summary>
    public static Art Einordnen(string typ)
    {
        if (Beschriftungen.Contains(typ)) return Art.Beschriftung;
        if (typ == "Button") return Art.Knopf;
        if (Felder.Contains(typ)) return Art.Feld;
        if (Sektionen.Contains(typ)) return Art.Sektion;
        if (Leisten.Contains(typ)) return Art.Leiste;
        if (Beiwerk.Contains(typ)) return Art.Beiwerk;
        return Art.Sonstig;
    }

    /// <summary>Kennt der Leser den Typ ueberhaupt?</summary>
    public static bool Bekannt(string typ) => Einordnen(typ) != Art.Sonstig;

    /// <summary>Namensteile, an denen ein Schliessknopf erkannt wird.</summary>
    private static readonly HashSet<string> Schliessknoepfe = new(StringComparer.OrdinalIgnoreCase)
    {
        "ok", "abbrechen", "cancel", "speichern", "save", "uebernehmen",
        "übernehmen", "schliessen", "schließen", "close", "beenden", "exit",
        "uebernahme", "apply"
    };

    /// <summary>Beschriftungen, an denen ein Schliessknopf erkannt wird.</summary>
    private static readonly HashSet<string> Schliesstexte = new(StringComparer.OrdinalIgnoreCase)
    {
        "OK", "Abbrechen", "Cancel", "Speichern", "Save", "Übernehmen",
        "Schließen", "Close", "Beenden", "Exit", "&OK", "&Abbrechen"
    };

    /// <summary>Ist der Knopf ein OK-/Abbrechen-/Speichern-Knopf (Ziel: SpeichernLeiste)?</summary>
    public static bool IstSchliessknopf(Steuerelement knopf)
    {
        if (Schliesstexte.Contains(knopf.Text.Trim())) return true;
        var kern = OhneVorsilbe(knopf.Name).Trim('_');
        return Schliessknoepfe.Contains(kern);
    }

    /// <summary>Ist der Knopf der Hilfeknopf (Ziel: InfoKnopf)?</summary>
    public static bool IstHilfeknopf(Steuerelement knopf) =>
        knopf.Name.Contains("Help", StringComparison.OrdinalIgnoreCase) ||
        knopf.Name.Contains("Hilfe", StringComparison.OrdinalIgnoreCase);

    /// <summary>Uebliche Vorsilben der Bestandsnamen - fuer Kernname und Knopferkennung.</summary>
    private static readonly string[] Vorsilben =
    {
        "numericUpDown", "dateTimePicker", "checkedListBox", "dataGridView",
        "radioButton", "pictureBox", "progressBar", "richTextBox", "comboBox",
        "groupBox", "checkBox", "listView", "listBox", "textBox", "tabPage",
        "tabControl", "button", "label", "panel", "chart", "grid",
        "cmb", "chk", "dgv", "dtp", "lbl", "nud", "num", "opt", "pic",
        "btn", "txt", "cbo", "grp", "pnl", "rad", "tab", "tbx",
        "cb", "lb", "lv", "rb", "tb", "tp", "gb"
    };

    /// <summary>
    /// Kernname eines Steuerelements: Vorsilbe und Unterstrich weg, erster
    /// Buchstabe gross. <c>textBox_Wert</c> wird zu <c>Wert</c>,
    /// <c>cmbBrennstoffArt</c> zu <c>BrennstoffArt</c>. Bleibt nichts
    /// Brauchbares uebrig, gewinnt der ganze Name.
    /// </summary>
    public static string Kernname(string name)
    {
        var rest = OhneVorsilbe(name).TrimStart('_');
        if (rest.Length == 0 || !(char.IsLetter(rest[0]) || rest[0] == '_')) rest = name.TrimStart('_');
        if (rest.Length == 0) rest = name;
        return char.ToUpperInvariant(rest[0]) + rest.Substring(1);
    }

    private static string OhneVorsilbe(string name)
    {
        foreach (var vorsilbe in Vorsilben)
        {
            if (name.Length <= vorsilbe.Length) continue;
            if (!name.StartsWith(vorsilbe, StringComparison.OrdinalIgnoreCase)) continue;

            var rest = name.Substring(vorsilbe.Length);
            // Nur abschneiden, wenn danach ein neues Wort beginnt - sonst
            // wuerde aus "tabelle" das Bruchstueck "elle".
            if (rest[0] == '_' || char.IsUpper(rest[0])) return rest;
        }
        return name;
    }
}
