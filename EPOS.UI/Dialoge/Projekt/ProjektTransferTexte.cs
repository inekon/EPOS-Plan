using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Projekt;

/// <summary>
/// Die Anzeigetexte des MEHRFACHTRANSFERS — ein Bündel statt zweiundzwanzig
/// einzelner Parameter (Auftrag PI-1, Hausregel „Texte ab zehn als Bündel").
///
/// <para><b>Warum ein zweites Bündel und nicht die vierzig dazu.</b> Die vierzig
/// Bestandsparameter des Dialogs stehen einzeln, und die Hülle wie beide
/// Testklassen reichen sie so herein. Sie umzubauen wäre eine Änderung an
/// achtzig Stellen, die mit dem Auftrag nichts zu tun hat und jeden Bestandsfall
/// anfasst. Was NEU dazukommt, bekommt deshalb die Bauform, die das Haus heute
/// nimmt — <c>KiChatTexte</c>, <c>LizenzTexte</c>, <c>Katalogfiltertexte</c>.</para>
///
/// <para><b>Es füllt sich selbst</b> aus <c>MyResource</c> in der
/// Oberflächensprache; wer etwas anderes braucht, überschreibt die Eigenschaft.
/// Jede trägt ihren Ressourcenschlüssel im Kommentar.</para>
/// </summary>
public sealed class ProjektTransferTexte
{
    // =====================================================================
    //  Exportblatt — die Projektliste
    // =====================================================================

    /// <summary>Überschrift über der Mehrfachliste (<c>PTR_LBL_PROJEKTE</c>).</summary>
    public string LabelProjekte { get; set; } = Resource.PTR_LBL_PROJEKTE;

    /// <summary>
    /// „Zu {0} gewählten Varianten kommen {1} Stammprojekte hinzu."
    /// (<c>PTR_HINWEIS_STAMMZUG</c>) — der Satz, der die stille Mitnahme sichtbar
    /// macht.
    /// </summary>
    public string HinweisStammzug { get; set; } = Resource.PTR_HINWEIS_STAMMZUG;

    /// <summary>Beschriftung eines Häkchenblocks: „Varianten von „{0}":" (<c>PTR_GRUPPE_VARIANTEN</c>).</summary>
    public string GruppeVarianten { get; set; } = Resource.PTR_GRUPPE_VARIANTEN;

    /// <summary>
    /// Der Grund, warum das Häkchen einer ausdrücklich gewählten Variante
    /// gesperrt ist (<c>PTR_GESPERRT_GEWAEHLT</c>) — er steht als <c>title</c> am
    /// Eintrag. <b>Eine Sperre ohne Grund ist eine Sackgasse.</b>
    /// </summary>
    public string GesperrtGewaehlt { get; set; } = Resource.PTR_GESPERRT_GEWAEHLT;

    /// <summary>Beschriftung der Zielordnerzeile (<c>PTR_LBL_ZIELORDNER</c>).</summary>
    public string LabelZielordner { get; set; } = Resource.PTR_LBL_ZIELORDNER;

    /// <summary>Der Knopf „Ordner wählen…" (<c>PTR_BTN_ORDNER</c>).</summary>
    public string BtnOrdner { get; set; } = Resource.PTR_BTN_ORDNER;

    /// <summary>„Bitte mindestens ein Projekt wählen." (<c>PTR_MSG_KEINE_WAHL</c>).</summary>
    public string MeldungKeineWahl { get; set; } = Resource.PTR_MSG_KEINE_WAHL;

    /// <summary>„Bitte einen Zielordner wählen." (<c>PTR_MSG_KEIN_ORDNER</c>).</summary>
    public string MeldungKeinOrdner { get; set; } = Resource.PTR_MSG_KEIN_ORDNER;

    /// <summary>Die Exportbilanz „{0} von {1} Paketen geschrieben, {2} fehlgeschlagen." (<c>PTR_BILANZ_EXPORT</c>).</summary>
    public string BilanzExport { get; set; } = Resource.PTR_BILANZ_EXPORT;

    // =====================================================================
    //  Importblatt — die Paketvorschau
    // =====================================================================

    /// <summary>Der Knopf „Dateien wählen…" (<c>PTR_BTN_DATEIEN</c>).</summary>
    public string BtnDateien { get; set; } = Resource.PTR_BTN_DATEIEN;

    /// <summary>Spaltenkopf „Datei" (<c>PTR_SP_PAKET_DATEI</c>).</summary>
    public string SpPaketDatei { get; set; } = Resource.PTR_SP_PAKET_DATEI;

    /// <summary>Spaltenkopf „Hauptprojekt" (<c>PTR_SP_PAKET_PROJEKT</c>).</summary>
    public string SpPaketProjekt { get; set; } = Resource.PTR_SP_PAKET_PROJEKT;

    /// <summary>Spaltenkopf „Varianten" (<c>PTR_SP_PAKET_VARIANTEN</c>).</summary>
    public string SpPaketVarianten { get; set; } = Resource.PTR_SP_PAKET_VARIANTEN;

    /// <summary>Spaltenkopf „Schemastand" (<c>PTR_SP_PAKET_SCHEMA</c>).</summary>
    public string SpPaketSchema { get; set; } = Resource.PTR_SP_PAKET_SCHEMA;

    /// <summary>Spaltenkopf „Hinweis" (<c>PTR_SP_PAKET_HINWEIS</c>).</summary>
    public string SpPaketHinweis { get; set; } = Resource.PTR_SP_PAKET_HINWEIS;

    /// <summary>Der Schemastand eines passenden Pakets (<c>PTR_PAKET_SCHEMA_OK</c>).</summary>
    public string PaketSchemaOk { get; set; } = Resource.PTR_PAKET_SCHEMA_OK;

    /// <summary>Der Schemastand eines abweichenden Pakets (<c>PTR_PAKET_SCHEMA_ALT</c>).</summary>
    public string PaketSchemaAlt { get; set; } = Resource.PTR_PAKET_SCHEMA_ALT;

    /// <summary>„Variante von „{0}"" in der Hinweisspalte (<c>PTR_PAKET_STAMM_AUS</c>).</summary>
    public string PaketStammAus { get; set; } = Resource.PTR_PAKET_STAMM_AUS;

    /// <summary>„derselbe Stamm wie Paket {0} — wird übersprungen" (<c>PTR_PAKET_DUBLETTE</c>).</summary>
    public string PaketDublette { get; set; } = Resource.PTR_PAKET_DUBLETTE;

    /// <summary>
    /// Der Begleittext des Laufs „Paket {0} von {1} — {2}"
    /// (<c>PTR_LAUF_PAKET</c>) — er füllt den sprachneutralen Schlüssel
    /// <c>TRANSFER_LAUF_PAKET</c>, den der Kern meldet.
    /// </summary>
    public string LaufPaket { get; set; } = Resource.PTR_LAUF_PAKET;

    /// <summary>Die Importbilanz (<c>PTR_BILANZ_IMPORT</c>).</summary>
    public string BilanzImport { get; set; } = Resource.PTR_BILANZ_IMPORT;

    /// <summary>
    /// Der leise Hinweis, dass der Zielname bei mehreren Paketen nicht gilt
    /// (<c>PTR_MSG_ZIELNAME_MEHRERE</c>).
    /// </summary>
    public string MeldungZielnameMehrere { get; set; } = Resource.PTR_MSG_ZIELNAME_MEHRERE;
}
