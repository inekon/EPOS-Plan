using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>Woher eine Word-Vorlage kommt.</summary>
    public enum Vorlagenquelle
    {
        /// <summary>Mit EPOS-Plan ausgeliefert (<c>Dienste.Pfade.Berichtsvorlagen</c>), schreibgeschützt.</summary>
        Mitgeliefert,

        /// <summary>Eine eigene Vorlage im Vorlagenordner.</summary>
        Eigen,
    }

    /// <summary>
    /// Die mitgelieferten Vorlagen, aus denen „Neue Vorlage…“ eine Kopie anlegt (Konzept Berichtsvorlagen 6.3, 10.2).
    /// </summary>
    public enum Vorlagenmuster
    {
        /// <summary>Die Standardvorlage — fehlt sie, die Stilvorlage.</summary>
        Standard,

        /// <summary>Der Kurzbericht in der Sprache der Oberfläche — eine Lehrvorlage mit Erläuterungen in Kommentaren.</summary>
        Kurzbericht,

        /// <summary>
        /// Die ausführliche Vorlage in der Sprache der Oberfläche (Entscheid BV-E8-4) — der volle Bericht aus Einzelelementen,
        /// frei umbaubar, mit Erläuterungen in Kommentaren. Der Wert steht hinter dem Kurzbericht, damit dessen Kennung bleibt.
        /// </summary>
        Ausfuehrlich,
    }

    /// <summary>Warum eine Vorlage für einen Lauf gewählt wurde (Konzept Berichtsvorlagen 10.3).</summary>
    public enum Vorlagenwahlgrund
    {
        /// <summary>Für dieses Stammprojekt ausdrücklich gewählt (<c>BerichtsKonfiguration.VorlageWord*</c>).</summary>
        Abweichung,

        /// <summary>Die Vorgabe der Installation (Einstellung <c>BerichtVorlageWord</c>).</summary>
        Vorgabe,

        /// <summary>Die Standardvorlage — nichts anderes gewählt oder das Gewählte fehlt.</summary>
        Standard,

        /// <summary>Die Standardvorlage selbst fehlt: Rückfall auf <c>Berichtsvorlage.docx</c> oder die eingebauten Formate.</summary>
        Rueckfall,
    }

    /// <summary>Wie ein Schritt am Vorlagenordner ausging.</summary>
    public enum Vorlagenergebnisart
    {
        /// <summary>Getan.</summary>
        Erledigt,

        /// <summary>Im Vorlagenordner gibt es den Namen schon — „Ersetzen“ oder „Unter neuem Namen“.</summary>
        NameVergeben,

        /// <summary>Die Quelldatei (oder die Standardvorlage) fehlt.</summary>
        QuelleFehlt,

        /// <summary>Kein <c>.docx</c> oder <c>.dotx</c> — „als .docx speichern“.</summary>
        FormatAbgelehnt,

        /// <summary>Die mitgelieferte Vorlage wird nicht verändert.</summary>
        Schreibgeschuetzt,

        /// <summary>Die Vorlage ist in Word geöffnet (Sperrdatei <c>~$…</c>).</summary>
        InWordGeoeffnet,

        /// <summary>Der neue Name taugt nicht als Dateiname.</summary>
        NameUngueltig,

        /// <summary>Der Vorlagenordner ist nicht erreichbar.</summary>
        OrdnerNichtErreichbar,

        /// <summary>Lesen oder Schreiben ist gescheitert.</summary>
        Fehler,
    }

    /// <summary>Der Zustand des Vorlagenordners.</summary>
    public enum Ordnerzustand
    {
        /// <summary>Der Ordner besteht.</summary>
        Vorhanden,

        /// <summary>Der Ordner wurde eben angelegt.</summary>
        Angelegt,

        /// <summary>Der Ordner besteht nicht und ließ sich nicht anlegen (etwa ein getrenntes Netzlaufwerk).</summary>
        NichtErreichbar,

        /// <summary>Kein gültiger, vollständiger Pfad.</summary>
        Ungueltig,
    }

    /// <summary>Eine Word-Vorlage der Liste — mitgeliefert oder eigen — mit stabiler Kennung.</summary>
    public sealed class Vorlageneintrag
    {
        internal Vorlageneintrag(string id, string name, string dateiname, string pfad, Vorlagenquelle quelle,
                                 bool schreibgeschuetzt, bool vorhanden, string pruefsumme, string herkunftspfad,
                                 DateTime? hinzugefuegt, string rueckfallpfad)
        {
            Id = id;
            Name = name;
            Dateiname = dateiname;
            Pfad = pfad;
            Quelle = quelle;
            Schreibgeschuetzt = schreibgeschuetzt;
            Vorhanden = vorhanden;
            Pruefsumme = pruefsumme;
            Herkunftspfad = herkunftspfad;
            Hinzugefuegt = hinzugefuegt;
            Rueckfallpfad = rueckfallpfad;
        }

        /// <summary>Die stabile Kennung: <c>standard</c> oder <c>eigen:</c> + Dateiname.</summary>
        public string Id { get; }

        /// <summary>Der Anzeigename: „Standard (EPOS-Plan)“ bzw. der Dateiname ohne Endung.</summary>
        public string Name { get; }

        /// <summary>Der Dateiname mit Endung.</summary>
        public string Dateiname { get; }

        /// <summary>Der volle Pfad.</summary>
        public string Pfad { get; }

        /// <summary>Mitgeliefert oder eigen.</summary>
        public Vorlagenquelle Quelle { get; }

        /// <summary>Schreibgeschützt: die mitgelieferte immer, eine eigene mit Schreibschutz-Attribut.</summary>
        public bool Schreibgeschuetzt { get; }

        /// <summary>Liegt die Datei vor? Bei der mitgelieferten kann sie fehlen (dann gilt <see cref="Rueckfallpfad"/>).</summary>
        public bool Vorhanden { get; }

        /// <summary>Die Prüfsumme beim Hinzufügen oder Ersetzen (Ablagedatei); <c>null</c> ohne Eintrag.</summary>
        public string Pruefsumme { get; }

        /// <summary>Woher die Vorlage kopiert wurde; <c>null</c> bei „Neue Vorlage…“ oder ohne Eintrag.</summary>
        public string Herkunftspfad { get; }

        /// <summary>Wann sie hinzugefügt oder zuletzt ersetzt wurde; <c>null</c> ohne Eintrag.</summary>
        public DateTime? Hinzugefuegt { get; }

        /// <summary>Nur bei der fehlenden Standardvorlage: <c>Berichtsvorlage.docx</c>, wenn sie vorliegt.</summary>
        public string Rueckfallpfad { get; }

        /// <summary>Ist das die mitgelieferte Standardvorlage?</summary>
        public bool IstStandard { get { return Quelle == Vorlagenquelle.Mitgeliefert; } }

        /// <inheritdoc/>
        public override string ToString() { return Id; }
    }

    /// <summary>Die Vorlage eines Laufs mit dem Grund ihrer Wahl und den Meldungen über Fehlendes.</summary>
    public sealed class Vorlagenwahl
    {
        internal Vorlagenwahl(Vorlageneintrag eintrag, Vorlagenwahlgrund grund, string grundText,
                              IReadOnlyList<string> meldungen, string fehlendeId)
        {
            Eintrag = eintrag;
            Grund = grund;
            GrundText = grundText ?? "";
            Meldungen = meldungen ?? Array.Empty<string>();
            FehlendeId = fehlendeId;
        }

        /// <summary>Die gewählte Vorlage; bei <see cref="Vorlagenwahlgrund.Rueckfall"/> der Standardeintrag ohne Datei.</summary>
        public Vorlageneintrag Eintrag { get; }

        /// <summary>Abweichung, Vorgabe, Standard oder Rückfall.</summary>
        public Vorlagenwahlgrund Grund { get; }

        /// <summary>Der Grund in Worten.</summary>
        public string GrundText { get; }

        /// <summary>Was fehlte und was stattdessen gilt, etwa „„Angebot“ nicht vorhanden – „Standard (EPOS-Plan)“ verwendet“.</summary>
        public IReadOnlyList<string> Meldungen { get; }

        /// <summary>Alle Meldungen in einem Satz; leer ohne.</summary>
        public string Meldung { get { return string.Join(" ", Meldungen); } }

        /// <summary>Die Kennung der gespeicherten, aber fehlenden Vorlage — der gesperrte Eintrag der Auswahl; <c>null</c> ohne.</summary>
        public string FehlendeId { get; }
    }

    /// <summary>Das Ergebnis eines Schritts am Vorlagenordner.</summary>
    public sealed class Vorlagenergebnis
    {
        internal Vorlagenergebnis(Vorlagenergebnisart art, string meldung, Vorlageneintrag eintrag,
                                  Vorlageneintrag vorhandener, string zielpfad)
        {
            Art = art;
            Meldung = meldung ?? "";
            Eintrag = eintrag;
            Vorhandener = vorhandener;
            Zielpfad = zielpfad;
        }

        /// <summary>Wie es ausging.</summary>
        public Vorlagenergebnisart Art { get; }

        /// <summary>Getan?</summary>
        public bool Erfolg { get { return Art == Vorlagenergebnisart.Erledigt; } }

        /// <summary>Die Meldung für den Anwender.</summary>
        public string Meldung { get; }

        /// <summary>Die neue oder betroffene Vorlage.</summary>
        public Vorlageneintrag Eintrag { get; }

        /// <summary>Bei <see cref="Vorlagenergebnisart.NameVergeben"/> die Vorlage, die den Namen schon trägt.</summary>
        public Vorlageneintrag Vorhandener { get; }

        /// <summary>Der geschriebene Pfad; beim Entfernen der Ort im Unterordner „Entfernt“.</summary>
        public string Zielpfad { get; }
    }

    /// <summary>Der Befund über den Vorlagenordner.</summary>
    public sealed class Ordnerbefund
    {
        internal Ordnerbefund(Ordnerzustand zustand, string pfad, bool istVorgabe, string meldung)
        {
            Zustand = zustand;
            Pfad = pfad ?? "";
            IstVorgabe = istVorgabe;
            Meldung = meldung ?? "";
        }

        /// <summary>Vorhanden, angelegt, nicht erreichbar, ungültig.</summary>
        public Ordnerzustand Zustand { get; }

        /// <summary>Der Ordner.</summary>
        public string Pfad { get; }

        /// <summary>Ist es der Vorgabeordner (keine Einstellung gesetzt)?</summary>
        public bool IstVorgabe { get; }

        /// <summary>Die Meldung für den Anwender.</summary>
        public string Meldung { get; }

        /// <summary>Ist der Ordner benutzbar?</summary>
        public bool Erfolg { get { return Zustand == Ordnerzustand.Vorhanden || Zustand == Ordnerzustand.Angelegt; } }
    }

    /// <summary>
    /// <b>Die Word-Vorlagen des Berichts</b> (Konzept Berichtsvorlagen 8.3, 10.2, 10.3; Etappe
    /// BV-E1) — auflisten, hinzufügen, ersetzen, neu anlegen, entfernen, Vorgabe und Abweichung
    /// wählen, auflösen, einmal lesen, prüfen; dazu die Erstellerangaben.
    ///
    /// <para><b>Ablage ohne Datenbank.</b> Die mitgelieferten Vorlagen liegen im Ordner der
    /// Auslieferung (<see cref="IPfade.Berichtsvorlagen"/>), schreibgeschützt. Eigene liegen im
    /// Vorlagenordner — Einstellung <see cref="EINSTELLUNG_ORDNER"/>, Vorgabe
    /// <c>Dokumente/EPOS-Plan/Berichtsvorlagen</c> —, Herkunft und Prüfsumme in der Ablagedatei
    /// <see cref="ABLAGEDATEI"/> daneben. Die Vorgabe der Installation steht in
    /// <see cref="EINSTELLUNG_VORGABE_WORD"/>, die Abweichung je Stammprojekt in der
    /// <see cref="BerichtsKonfiguration"/>, die der Aufrufer lädt und speichert.</para>
    ///
    /// <para><b>Plattformen.</b> Die Umgebung kommt allein über <see cref="Dienste"/> (oder
    /// hereingereicht): Auf iOS ist der Vorlagenordner die Sandbox unter
    /// <see cref="IPfade.Dokumente"/>; die Ordnerwahl selbst — und ihre benannte Ablehnung auf iOS —
    /// macht die Hülle.</para>
    /// </summary>
    public partial class BerichtsvorlagenCtrl
    {
        /// <summary>Einstellung: der Vorlagenordner; leer = Vorgabe.</summary>
        public const string EINSTELLUNG_ORDNER = "BerichtVorlagenordner";

        /// <summary>Einstellung: die Vorgabe der Word-Vorlage als Kennung (<see cref="Vorlageneintrag.Id"/>).</summary>
        public const string EINSTELLUNG_VORGABE_WORD = "BerichtVorlageWord";

        /// <summary>
        /// Einstellung: die Vorgabe der Excel-Vorlage als Kennung (Konzept 10.3, Etappe BV-E7) — <see cref="ID_OHNE"/> oder
        /// <c>eigen:</c> + Dateiname; ohne Einstellung „ohne Vorlage“.
        /// </summary>
        public const string EINSTELLUNG_VORGABE_EXCEL = "BerichtVorlageExcel";

        /// <summary>Einstellung: die Firma für <c>ersteller.firma</c>.</summary>
        public const string EINSTELLUNG_FIRMA = "BerichtFirma";

        /// <summary>
        /// Einstellung: der Pfad des Firmenlogos für <c>bild.ersteller.logo</c> (Anwenderentscheid BV-E2-1);
        /// leer = kein Logo. Die Einstellungsseite der Hülle schreibt denselben Schlüssel.
        /// </summary>
        public const string EINSTELLUNG_LOGO = "BerichtLogo";

        /// <summary>Höchstgröße der Logodatei: 5 MB.</summary>
        public const long GRENZE_LOGO = 5L * 1024 * 1024;

        /// <summary>Die mitgelieferte Standardvorlage.</summary>
        public const string DATEI_STANDARD = "Berichtsvorlage_Standard.docx";

        /// <summary>Die frühere Stilvorlage — Rückfall, wenn die Standardvorlage fehlt.</summary>
        public const string DATEI_RUECKFALL = "Berichtsvorlage.docx";

        /// <summary>
        /// Der mitgelieferte Kurzbericht auf Deutsch (Konzept Berichtsvorlagen 6.3 Nr. 2, Anhang B.1): eine Lehrvorlage,
        /// nicht direkt wählbar, nur als Kopie über „Neue Vorlage…“ (<see cref="NeueVorlage(string, Vorlagenmuster, bool)"/>).
        /// </summary>
        public const string DATEI_KURZBERICHT = "Berichtsvorlage_Kurzbericht.docx";

        /// <summary>Der mitgelieferte Kurzbericht auf Englisch — der Kurzbericht kommt je Sprache (Konzept 4.9).</summary>
        public const string DATEI_KURZBERICHT_EN = "Berichtsvorlage_Kurzbericht_en.docx";

        /// <summary>
        /// Die mitgelieferte ausführliche Vorlage auf Deutsch (Entscheid BV-E8-4): der volle Bericht in der Folge des
        /// Standardberichts, jeder Abschnitt aus Einzelelementen; wie der Kurzbericht nur als Kopie über „Neue Vorlage…“.
        /// </summary>
        public const string DATEI_AUSFUEHRLICH = "Berichtsvorlage_Ausfuehrlich.docx";

        /// <summary>Die mitgelieferte ausführliche Vorlage auf Englisch — sie kommt je Sprache wie der Kurzbericht.</summary>
        public const string DATEI_AUSFUEHRLICH_EN = "Berichtsvorlage_Ausfuehrlich_en.docx";

        /// <summary>Die Ablagedatei mit Herkunft und Prüfsumme im Vorlagenordner.</summary>
        public const string ABLAGEDATEI = ".berichtsvorlagen.json";

        /// <summary>Der Unterordner, in den „Entfernen“ eine Vorlage legt.</summary>
        public const string ORDNER_ENTFERNT = "Entfernt";

        /// <summary>Die Kennung der Standardvorlage.</summary>
        public const string ID_STANDARD = "standard";

        /// <summary>Die Vorsilbe der Kennung einer eigenen Vorlage.</summary>
        public const string ID_PRAEFIX_EIGEN = "eigen:";

        /// <summary>Die Endungen der Word-Vorlagen der Liste.</summary>
        public static readonly IReadOnlyList<string> Endungen = new[] { ".docx", ".dotx" };

        /// <summary>Die Endungen der Excel-Vorlagen der Liste (Konzept 10.3, ab BV-E7).</summary>
        public static readonly IReadOnlyList<string> ExcelEndungen = new[] { ".xlsx", ".xltx" };

        /// <summary>Die Kennung „ohne Excel-Vorlage“ — die Mappe entsteht im Code wie ohne Vorlagenweg (Konzept 7.1).</summary>
        public const string ID_OHNE = "ohne";

        /// <summary>Die Unterordner des Vorgabeordners unter <see cref="IPfade.Dokumente"/>.</summary>
        public static readonly IReadOnlyList<string> Vorgabeunterordner = new[] { "EPOS-Plan", "Berichtsvorlagen" };

        /// <summary>Zeichen, die in keinem Dateinamen stehen dürfen — der Windows-Satz, auf jeder Plattform gleich.</summary>
        private static readonly char[] VerboteneZeichen = { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };

        private static readonly string[] ReservierteNamen =
        {
            "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        };

        private readonly IPfade _pfade;
        private readonly IEinstellungen _einstellungen;
        private readonly Func<string> _lizenzFirma;

        /// <summary>Mit den Diensten der Plattform (<see cref="Dienste.Pfade"/>, <see cref="Dienste.Einstellungen"/>)
        /// und der Firma des aktiven Lizenztokens.</summary>
        public BerichtsvorlagenCtrl() : this(null, null, null)
        {
        }

        /// <summary>
        /// Mit hereingereichten Diensten (Prüfstand); <c>null</c> = der jeweilige Dienst der Plattform.
        /// <paramref name="lizenzFirma"/> liefert die Firma des aktiven Lizenztokens; <c>null</c> =
        /// <see cref="LizenzManager.Token"/>.
        /// </summary>
        public BerichtsvorlagenCtrl(IPfade pfade, IEinstellungen einstellungen, Func<string> lizenzFirma = null)
        {
            _pfade = pfade;
            _einstellungen = einstellungen;
            _lizenzFirma = lizenzFirma ?? FirmaDesTokens;
        }

        private IPfade Pfade { get { return _pfade ?? Dienste.Pfade; } }

        private IEinstellungen Einstellungen { get { return _einstellungen ?? Dienste.Einstellungen; } }

        private static StringComparison PfadVergleich
        {
            get { return OperatingSystem.IsLinux() ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase; }
        }

        // =====================================================================
        //  Vorlagenordner
        // =====================================================================

        /// <summary>Der Vorgabeordner: <see cref="IPfade.Dokumente"/>/EPOS-Plan/Berichtsvorlagen; leer ohne Dokumente-Ordner.</summary>
        public string Vorgabeordner
        {
            get
            {
                string dokumente = Pfade.Dokumente;
                if (string.IsNullOrWhiteSpace(dokumente)) return "";
                return Path.Combine(dokumente, Vorgabeunterordner[0], Vorgabeunterordner[1]);
            }
        }

        /// <summary>Gilt der Vorgabeordner (keine Einstellung)?</summary>
        public bool IstVorgabeordner
        {
            get { return string.IsNullOrWhiteSpace(Einstellungen.Lies(EINSTELLUNG_ORDNER, null)); }
        }

        /// <summary>Der Vorlagenordner: die Einstellung <see cref="EINSTELLUNG_ORDNER"/>, sonst der Vorgabeordner.</summary>
        public string Vorlagenordner
        {
            get
            {
                string gesetzt = Einstellungen.Lies(EINSTELLUNG_ORDNER, null);
                return string.IsNullOrWhiteSpace(gesetzt) ? Vorgabeordner : gesetzt.Trim();
            }
        }

        /// <summary>
        /// Prüft den Vorlagenordner: besteht er? Mit <paramref name="anlegen"/> wird ein fehlender
        /// angelegt — „bei Bedarf“, also vor dem Schreiben.
        /// </summary>
        public Ordnerbefund PruefeOrdner(bool anlegen = false)
        {
            string ordner = Vorlagenordner;
            bool vorgabe = IstVorgabeordner;
            if (!IstGueltigerOrdner(ordner))
                return new Ordnerbefund(Ordnerzustand.Ungueltig, ordner, vorgabe, T(nameof(R.BV_VORLAGEN_ORDNER_UNGUELTIG), ordner ?? ""));
            if (Directory.Exists(ordner))
                return new Ordnerbefund(Ordnerzustand.Vorhanden, ordner, vorgabe, T(nameof(R.BV_VORLAGEN_ORDNER_VORHANDEN), ordner));
            if (anlegen)
            {
                try
                {
                    Directory.CreateDirectory(ordner);
                    return new Ordnerbefund(Ordnerzustand.Angelegt, ordner, vorgabe, T(nameof(R.BV_VORLAGEN_ORDNER_ANGELEGT), ordner));
                }
                catch (Exception)
                {
                    // benannt unten
                }
            }
            return new Ordnerbefund(Ordnerzustand.NichtErreichbar, ordner, vorgabe, T(nameof(R.BV_VORLAGEN_ORDNER_NICHT_ERREICHBAR), ordner));
        }

        /// <summary>
        /// Setzt den Vorlagenordner — geschrieben wird die Einstellung nur für einen bestehenden,
        /// erreichbaren Ordner; sonst benennt der Befund, warum nicht. <c>null</c> oder leer, oder der
        /// Vorgabeordner selbst, setzt auf die Vorgabe zurück. Die Wahl im Dialog macht die Hülle.
        /// </summary>
        public Ordnerbefund SetzeVorlagenordner(string ordner)
        {
            if (string.IsNullOrWhiteSpace(ordner))
            {
                Einstellungen.Loesche(EINSTELLUNG_ORDNER);
                return PruefeOrdner(anlegen: true);
            }
            string pfad = ordner.Trim();
            if (!IstGueltigerOrdner(pfad))
                return new Ordnerbefund(Ordnerzustand.Ungueltig, pfad, false, T(nameof(R.BV_VORLAGEN_ORDNER_UNGUELTIG), pfad));
            if (!Directory.Exists(pfad))
                return new Ordnerbefund(Ordnerzustand.NichtErreichbar, pfad, false, T(nameof(R.BV_VORLAGEN_ORDNER_NICHT_ERREICHBAR), pfad));

            if (GleicherPfad(pfad, Vorgabeordner)) Einstellungen.Loesche(EINSTELLUNG_ORDNER);
            else Einstellungen.Schreib(EINSTELLUNG_ORDNER, pfad);
            return new Ordnerbefund(Ordnerzustand.Vorhanden, pfad, IstVorgabeordner, T(nameof(R.BV_VORLAGEN_ORDNER_VORHANDEN), pfad));
        }

        // =====================================================================
        //  Liste
        // =====================================================================

        /// <summary>
        /// Die Word-Vorlagen: zuerst die Standardvorlage (auch wenn ihre Datei fehlt), dann die eigenen
        /// des Vorlagenordners (<c>*.docx</c>, <c>*.dotx</c>; ohne Sperrdateien <c>~$…</c> und
        /// versteckte), nach Namen sortiert. Der Vorgabeordner wird bei Bedarf angelegt, ein
        /// eingestellter nie.
        /// </summary>
        public IReadOnlyList<Vorlageneintrag> Liste()
        {
            var liste = new List<Vorlageneintrag> { Standardeintrag() };
            liste.AddRange(EigeneEintraege(Endungen));
            return liste;
        }

        /// <summary>
        /// Die Excel-Vorlagen (Konzept 10.2 Zeile „Excel-Vorlage“, Etappe BV-E7): zuerst „ohne Vorlage“, dann die eigenen des
        /// Vorlagenordners (<c>*.xlsx</c>, <c>*.xltx</c>; ohne Sperrdateien und versteckte), nach Namen sortiert. Eine
        /// mitgelieferte Excel-Vorlage gibt es nicht — die Standard-Mappe entsteht im Code (7.1).
        /// </summary>
        public IReadOnlyList<Vorlageneintrag> ListeExcel()
        {
            var liste = new List<Vorlageneintrag> { OhneExcelEintrag() };
            liste.AddRange(EigeneEintraege(ExcelEndungen));
            return liste;
        }

        /// <summary>Der Eintrag „ohne Vorlage“ der Excel-Liste — kein Pfad, schreibgeschützt, immer vorhanden.</summary>
        public Vorlageneintrag OhneExcelEintrag()
        {
            return new Vorlageneintrag(ID_OHNE, T(nameof(R.BV_XL_OHNE_VORLAGE)), "", "", Vorlagenquelle.Mitgeliefert, true, true,
                                       null, null, null, null);
        }

        /// <summary>Der Eintrag einer Excel-Kennung; <c>null</c>, wenn es die eigene Excel-Vorlage nicht (mehr) gibt.</summary>
        public Vorlageneintrag FindeExcel(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            string kennung = id.Trim();
            if (string.Equals(kennung, ID_OHNE, StringComparison.OrdinalIgnoreCase)) return OhneExcelEintrag();
            if (!kennung.StartsWith(ID_PRAEFIX_EIGEN, StringComparison.OrdinalIgnoreCase)) return null;
            string datei = kennung.Substring(ID_PRAEFIX_EIGEN.Length).Trim();
            if (!IstEinfacherDateiname(datei)) return null;
            return EigeneEintraege(ExcelEndungen).FirstOrDefault(e => string.Equals(e.Dateiname, datei, PfadVergleich));
        }

        /// <summary>Ist der Eintrag eine Excel-Vorlage (Endung <c>.xlsx</c>/<c>.xltx</c>) oder „ohne Vorlage“?</summary>
        public static bool IstExcel(Vorlageneintrag eintrag)
        {
            if (eintrag == null) return false;
            if (string.Equals(eintrag.Id, ID_OHNE, StringComparison.OrdinalIgnoreCase)) return true;
            return IstExcelDatei(eintrag.Dateiname);
        }

        /// <summary>Trägt der Dateiname eine Excel-Endung der Liste?</summary>
        public static bool IstExcelDatei(string datei)
        {
            try { return ExcelEndungen.Contains(Path.GetExtension(datei ?? "").ToLowerInvariant()); }
            catch (ArgumentException) { return false; }
        }

        /// <summary>Der Eintrag der mitgelieferten Standardvorlage; fehlt die Datei, mit benanntem Rückfall.</summary>
        public Vorlageneintrag Standardeintrag()
        {
            string ordner = Pfade.Berichtsvorlagen ?? "";
            string pfad = Path.Combine(ordner, DATEI_STANDARD);
            bool vorhanden = File.Exists(pfad);
            string rueckfall = Path.Combine(ordner, DATEI_RUECKFALL);
            return new Vorlageneintrag(ID_STANDARD, T(nameof(R.BV_VORLAGEN_STANDARD)), DATEI_STANDARD, pfad,
                                       Vorlagenquelle.Mitgeliefert, true, vorhanden, null, null, null,
                                       vorhanden || !File.Exists(rueckfall) ? null : rueckfall);
        }

        /// <summary>Der Eintrag zu einer Kennung; <c>null</c>, wenn es die eigene Vorlage nicht (mehr) gibt.</summary>
        public Vorlageneintrag Finde(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            string kennung = id.Trim();
            if (IstStandardId(kennung)) return Standardeintrag();
            if (!kennung.StartsWith(ID_PRAEFIX_EIGEN, StringComparison.OrdinalIgnoreCase)) return null;
            string datei = kennung.Substring(ID_PRAEFIX_EIGEN.Length).Trim();
            if (!IstEinfacherDateiname(datei)) return null;
            return EigeneEintraege(Endungen).FirstOrDefault(e => string.Equals(e.Dateiname, datei, PfadVergleich));
        }

        private List<Vorlageneintrag> EigeneEintraege(IReadOnlyList<string> endungen)
        {
            var eintraege = new List<Vorlageneintrag>();
            string ordner = Vorlagenordner;
            if (!IstGueltigerOrdner(ordner)) return eintraege;
            if (!Directory.Exists(ordner) && IstVorgabeordner)
            {
                try { Directory.CreateDirectory(ordner); } catch (Exception) { /* bleibt leer */ }
            }
            if (!Directory.Exists(ordner)) return eintraege;

            List<string> dateien;
            try { dateien = Directory.EnumerateFiles(ordner).ToList(); }
            catch (Exception) { return eintraege; }

            Ablage ablage = LiesAblage(ordner);
            foreach (string pfad in dateien)
                if (IstVorlagendatei(pfad, endungen)) eintraege.Add(EigenerEintrag(pfad, ablage));

            StringComparer namen = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.IgnoreCase);
            return eintraege.OrderBy(e => e.Name, namen).ThenBy(e => e.Dateiname, StringComparer.Ordinal).ToList();
        }

        private Vorlageneintrag EigenerEintrag(string pfad, Ablage ablage)
        {
            string datei = Path.GetFileName(pfad);
            Ablageeintrag merk = ablage?.Finde(datei);
            bool schreibgeschuetzt = false;
            try { schreibgeschuetzt = (File.GetAttributes(pfad) & FileAttributes.ReadOnly) != 0; }
            catch (Exception) { schreibgeschuetzt = false; }
            return new Vorlageneintrag(ID_PRAEFIX_EIGEN + datei, Path.GetFileNameWithoutExtension(datei), datei, pfad,
                                       Vorlagenquelle.Eigen, schreibgeschuetzt, File.Exists(pfad), merk?.Pruefsumme,
                                       merk?.Herkunftspfad, merk?.Hinzugefuegt, null);
        }

        /// <summary>Gehört die Datei in die Liste: Endung der Liste, keine Sperrdatei, nicht versteckt?</summary>
        private static bool IstVorlagendatei(string pfad, IReadOnlyList<string> endungen)
        {
            string name = Path.GetFileName(pfad);
            if (string.IsNullOrEmpty(name) || name.StartsWith("~$", StringComparison.Ordinal) ||
                name.StartsWith(".", StringComparison.Ordinal)) return false;
            if (!endungen.Contains(Path.GetExtension(name).ToLowerInvariant())) return false;
            try
            {
                return (File.GetAttributes(pfad) & (FileAttributes.Hidden | FileAttributes.System)) == 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // =====================================================================
        //  Hinzufügen, Ersetzen, Neue Vorlage, Entfernen
        // =====================================================================

        /// <summary>
        /// Kopiert eine Vorlage in den Vorlagenordner und merkt Herkunft und Prüfsumme. Gibt es den
        /// Namen schon, geschieht nichts: <see cref="Vorlagenergebnisart.NameVergeben"/> nennt die
        /// vorhandene — dann <see cref="Ersetzen"/> oder <see cref="HinzufuegenAls"/>.
        /// </summary>
        public Vorlagenergebnis Hinzufuegen(string quellpfad)
        {
            Vorlagenergebnis fehler = PruefeQuelle(quellpfad);
            if (fehler != null) return fehler;
            string quelle = quellpfad.Trim();
            return Lege(quelle, Path.GetFileName(quelle), false, quelle, nameof(R.BV_VORLAGEN_HINZUGEFUEGT));
        }

        /// <summary>Wie <see cref="Hinzufuegen"/>, unter einem neuen Namen (die Endung der Quelle bleibt).</summary>
        public Vorlagenergebnis HinzufuegenAls(string quellpfad, string neuerName)
        {
            Vorlagenergebnis fehler = PruefeQuelle(quellpfad);
            if (fehler != null) return fehler;
            string quelle = quellpfad.Trim();
            string datei = Zielname(neuerName, Path.GetExtension(quelle));
            if (datei == null)
                return Ergebnis(Vorlagenergebnisart.NameUngueltig, T(nameof(R.BV_VORLAGEN_NAME_UNGUELTIG), neuerName ?? ""));
            return Lege(quelle, datei, false, quelle, nameof(R.BV_VORLAGEN_HINZUGEFUEGT));
        }

        /// <summary>
        /// Ersetzt eine eigene Vorlage durch die Quelle (gleiche Endung); die mitgelieferte bleibt
        /// unangetastet, eine in Word geöffnete auch.
        /// </summary>
        public Vorlagenergebnis Ersetzen(string quellpfad, Vorlageneintrag eintrag)
        {
            if (eintrag == null || eintrag.Quelle != Vorlagenquelle.Eigen)
                return Ergebnis(Vorlagenergebnisart.Schreibgeschuetzt, T(nameof(R.BV_VORLAGEN_SCHREIBGESCHUETZT)), eintrag);
            Vorlagenergebnis fehler = PruefeQuelle(quellpfad);
            if (fehler != null) return fehler;
            string quelle = quellpfad.Trim();
            string endung = Path.GetExtension(eintrag.Dateiname);
            if (!string.Equals(Path.GetExtension(quelle), endung, StringComparison.OrdinalIgnoreCase))
                return Ergebnis(Vorlagenergebnisart.FormatAbgelehnt, T(nameof(R.BV_VORLAGEN_ENDUNG), endung), eintrag);
            if (IstInWordGeoeffnet(eintrag))
                return Ergebnis(Vorlagenergebnisart.InWordGeoeffnet, T(nameof(R.BV_VORLAGEN_IN_WORD)), eintrag);
            return Lege(quelle, eintrag.Dateiname, true, quelle, nameof(R.BV_VORLAGEN_ERSETZT));
        }

        /// <summary>
        /// „Neue Vorlage…“: kopiert die Standardvorlage (fehlt sie, <c>Berichtsvorlage.docx</c>) unter
        /// <paramref name="name"/> in den Vorlagenordner — der erste Schritt jedes Autors.
        /// </summary>
        public Vorlagenergebnis NeueVorlage(string name)
        {
            return NeueVorlage(name, Vorlagenmuster.Standard, false);
        }

        /// <summary>
        /// „Neue Vorlage…“ aus einem Muster (Konzept 10.2: „Kopie der Standardvorlage oder des Kurzberichts“): mit
        /// <see cref="Vorlagenmuster.Standard"/> wie <see cref="NeueVorlage(string)"/>, mit
        /// <see cref="Vorlagenmuster.Kurzbericht"/> bzw. <see cref="Vorlagenmuster.Ausfuehrlich"/> eine Kopie des
        /// mitgelieferten Kurzberichts bzw. der ausführlichen Vorlage in der Sprache <paramref name="englisch"/>
        /// (<see cref="Musterdatei(Vorlagenmuster, bool)"/>) — ohne Rückfall: Fehlt die Datei, benennt das Ergebnis sie.
        /// Die Kopie trägt die Kommentare des Musters; die Engine entfernt sie beim Erstellen.
        /// </summary>
        public Vorlagenergebnis NeueVorlage(string name, Vorlagenmuster muster, bool englisch)
        {
            string meldung = muster == Vorlagenmuster.Kurzbericht ? nameof(R.BV_VORLAGEN_NEU_KURZBERICHT)
                           : muster == Vorlagenmuster.Ausfuehrlich ? nameof(R.BV_VORLAGEN_NEU_AUSFUEHRLICH)
                           : nameof(R.BV_VORLAGEN_NEU);
            return KopiereMuster(name, muster, englisch, meldung);
        }

        /// <summary>
        /// „In den Vorlagenordner exportieren…“ (Anwenderauftrag zu BV-E7-6): dieselbe Kopie wie
        /// <see cref="NeueVorlage(string, Vorlagenmuster, bool)"/> — eine bearbeitbare Kopie des mitgelieferten Musters unter
        /// <paramref name="name"/> im Vorlagenordner selbst (nicht im Musterordner), gleiche Namensregel, ein vergebener Name
        /// ergibt <see cref="Vorlagenergebnisart.NameVergeben"/>. Den Unterschied macht der Aufrufer: Die Kopie wird nicht
        /// gewählt. Das Ergebnis nennt Name und Pfad (<see cref="Vorlagenergebnis.Zielpfad"/>).
        /// </summary>
        public Vorlagenergebnis Exportieren(string name, Vorlagenmuster muster, bool englisch)
        {
            Vorlagenergebnis r = KopiereMuster(name, muster, englisch, nameof(R.BV_VORLAGEN_EXPORTIERT));
            if (!r.Erfolg) return r;
            return new Vorlagenergebnis(r.Art, T(nameof(R.BV_VORLAGEN_EXPORTIERT), r.Eintrag?.Name ?? "", r.Zielpfad ?? ""),
                                        r.Eintrag, r.Vorhandener, r.Zielpfad);
        }

        /// <summary>Kopiert das Muster als eigene Vorlage in den Vorlagenordner — der gemeinsame Weg von „Neue Vorlage…“ und „Exportieren…“.</summary>
        private Vorlagenergebnis KopiereMuster(string name, Vorlagenmuster muster, bool englisch, string meldung)
        {
            string quelle, fehlt;
            if (muster != Vorlagenmuster.Standard)
            {
                fehlt = Musterdatei(muster, englisch);
                quelle = Musterpfad(muster, englisch);
            }
            else
            {
                Vorlageneintrag standard = Standardeintrag();
                fehlt = standard.Dateiname;
                quelle = standard.Vorhanden ? standard.Pfad : standard.Rueckfallpfad;
            }
            if (quelle == null)
                return Ergebnis(Vorlagenergebnisart.QuelleFehlt, T(nameof(R.BV_VORLAGEN_FEHLT), fehlt));
            string datei = Zielname(name, ".docx");
            if (datei == null)
                return Ergebnis(Vorlagenergebnisart.NameUngueltig, T(nameof(R.BV_VORLAGEN_NAME_UNGUELTIG), name ?? ""));
            return Lege(quelle, datei, false, null, meldung);
        }

        /// <summary>Der Dateiname des Kurzberichts der Sprache: <see cref="DATEI_KURZBERICHT_EN"/> auf Englisch, sonst <see cref="DATEI_KURZBERICHT"/>.</summary>
        public static string DateiKurzbericht(bool englisch)
        {
            return englisch ? DATEI_KURZBERICHT_EN : DATEI_KURZBERICHT;
        }

        /// <summary>Der Dateiname der ausführlichen Vorlage der Sprache: <see cref="DATEI_AUSFUEHRLICH_EN"/> auf Englisch, sonst <see cref="DATEI_AUSFUEHRLICH"/>.</summary>
        public static string DateiAusfuehrlich(bool englisch)
        {
            return englisch ? DATEI_AUSFUEHRLICH_EN : DATEI_AUSFUEHRLICH;
        }

        /// <summary>
        /// Der Dateiname eines mitgelieferten Musters in der Sprache <paramref name="englisch"/>: die Standardvorlage
        /// (sprachneutral), der Kurzbericht oder die ausführliche Vorlage der Sprache.
        /// </summary>
        public static string Musterdatei(Vorlagenmuster muster, bool englisch)
        {
            switch (muster)
            {
                case Vorlagenmuster.Kurzbericht: return DateiKurzbericht(englisch);
                case Vorlagenmuster.Ausfuehrlich: return DateiAusfuehrlich(englisch);
                default: return DATEI_STANDARD;
            }
        }

        /// <summary>
        /// Der Pfad eines mitgelieferten Musters im Ordner <see cref="IPfade.Berichtsvorlagen"/> — die Standardvorlage
        /// (sonst die Stilvorlage) bzw. der Kurzbericht oder die ausführliche Vorlage der Sprache; <c>null</c>, wenn die Datei
        /// fehlt.
        /// </summary>
        public string Musterpfad(Vorlagenmuster muster, bool englisch)
        {
            if (muster == Vorlagenmuster.Standard)
            {
                Vorlageneintrag standard = Standardeintrag();
                return standard.Vorhanden ? standard.Pfad : standard.Rueckfallpfad;
            }
            string pfad = Path.Combine(Pfade.Berichtsvorlagen ?? "", Musterdatei(muster, englisch));
            return File.Exists(pfad) ? pfad : null;
        }

        /// <summary>
        /// Der Baukasten der Word-Vorlagen (Konzept 6.3 Nr. 3, BV-E5): aus dem Katalog der laufenden Fassung
        /// erzeugt, in der Sprache <paramref name="englisch"/> — die Bytes einer <c>.docx</c>
        /// (<see cref="WordBaukasten.Erzeuge(bool, int)"/>). Der Excel-Baukasten kommt mit der Ausgabe Excel (BV-E7).
        /// </summary>
        public static byte[] Baukasten(bool englisch)
        {
            return WordBaukasten.Erzeuge(englisch, Vorlagenfeldkatalog.Katalogfassung);
        }

        /// <summary>
        /// „Baukasten speichern…“: erzeugt den Baukasten und schreibt ihn nach <paramref name="pfad"/> (ohne
        /// Endung mit <c>.docx</c>) — erst ganz in den Speicher, dann die Datei. Ein vorhandenes Ziel ersetzt der
        /// Speichern-Dialog der Plattform nach seiner Rückfrage.
        /// </summary>
        public static Vorlagenergebnis SpeichereBaukasten(string pfad, bool englisch)
        {
            if (string.IsNullOrWhiteSpace(pfad))
                return Ergebnis(Vorlagenergebnisart.NameUngueltig, T(nameof(R.VF_BAUKASTEN_FEHLER), pfad ?? ""));
            string ziel = pfad.Trim();
            try
            {
                if (!string.Equals(Path.GetExtension(ziel), ".docx", StringComparison.OrdinalIgnoreCase)) ziel += ".docx";
                byte[] bytes = Baukasten(englisch);
                string ordner = Path.GetDirectoryName(Path.GetFullPath(ziel));
                if (!string.IsNullOrEmpty(ordner)) Directory.CreateDirectory(ordner);
                File.WriteAllBytes(ziel, bytes);
            }
            catch (Exception ex)
            {
                return Ergebnis(Vorlagenergebnisart.Fehler, T(nameof(R.VF_BAUKASTEN_FEHLER), ex.Message));
            }
            return new Vorlagenergebnis(Vorlagenergebnisart.Erledigt, T(nameof(R.VF_BAUKASTEN_GESPEICHERT), ziel), null, null, ziel);
        }

        /// <summary>
        /// Entfernt eine eigene Vorlage aus der Liste: Die Datei wandert in den Unterordner
        /// <see cref="ORDNER_ENTFERNT"/> des Vorlagenordners — nichts wird still gelöscht. War sie die
        /// Vorgabe, gilt wieder die Standardvorlage.
        /// </summary>
        public Vorlagenergebnis Entfernen(Vorlageneintrag eintrag)
        {
            if (eintrag == null || eintrag.Quelle != Vorlagenquelle.Eigen)
                return Ergebnis(Vorlagenergebnisart.Schreibgeschuetzt, T(nameof(R.BV_VORLAGEN_SCHREIBGESCHUETZT)), eintrag);
            string ordner = Path.GetDirectoryName(eintrag.Pfad) ?? "";
            if (!File.Exists(eintrag.Pfad))
            {
                Vergiss(ordner, eintrag.Dateiname);
                return Ergebnis(Vorlagenergebnisart.QuelleFehlt, T(nameof(R.BV_VORLAGEN_FEHLT), eintrag.Name), eintrag);
            }
            if (IstInWordGeoeffnet(eintrag))
                return Ergebnis(Vorlagenergebnisart.InWordGeoeffnet, T(nameof(R.BV_VORLAGEN_IN_WORD)), eintrag);

            try
            {
                string ablage = Path.Combine(ordner, ORDNER_ENTFERNT);
                Directory.CreateDirectory(ablage);
                string ziel = FreierName(ablage, eintrag.Dateiname);
                File.Move(eintrag.Pfad, ziel);
                Vergiss(ordner, eintrag.Dateiname);
                if (string.Equals(VorgabeWordId, eintrag.Id, PfadVergleich)) Einstellungen.Loesche(EINSTELLUNG_VORGABE_WORD);
                if (string.Equals(VorgabeExcelId, eintrag.Id, PfadVergleich)) Einstellungen.Loesche(EINSTELLUNG_VORGABE_EXCEL);
                return new Vorlagenergebnis(Vorlagenergebnisart.Erledigt, T(nameof(R.BV_VORLAGEN_ENTFERNT), eintrag.Name, ablage),
                                            eintrag, null, ziel);
            }
            catch (Exception ex)
            {
                return Ergebnis(Vorlagenergebnisart.Fehler, T(nameof(R.BV_VORLAGEN_FEHLER), eintrag.Dateiname, ex.Message), eintrag);
            }
        }

        /// <summary>
        /// Hat sich das Original seit dem Hinzufügen geändert („Original geändert – übernehmen?“,
        /// Konzept 10.2)? <c>null</c>, wenn es keine Herkunft gibt oder sie nicht lesbar ist.
        /// </summary>
        public bool? OriginalGeaendert(Vorlageneintrag eintrag)
        {
            if (eintrag == null || string.IsNullOrEmpty(eintrag.Herkunftspfad) || string.IsNullOrEmpty(eintrag.Pruefsumme)) return null;
            if (!File.Exists(eintrag.Herkunftspfad)) return null;
            try
            {
                return !string.Equals(Vorlagenpruefer.Pruefsumme(LiesDatei(eintrag.Herkunftspfad)), eintrag.Pruefsumme,
                                      StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private Vorlagenergebnis PruefeQuelle(string quellpfad)
        {
            if (string.IsNullOrWhiteSpace(quellpfad) || !File.Exists(quellpfad.Trim()))
                return Ergebnis(Vorlagenergebnisart.QuelleFehlt, T(nameof(R.BV_VORLAGEN_QUELLE_FEHLT), quellpfad ?? ""));
            string endung = Path.GetExtension(quellpfad.Trim()).ToLowerInvariant();
            if (!Endungen.Contains(endung) && !ExcelEndungen.Contains(endung))
                return Ergebnis(Vorlagenergebnisart.FormatAbgelehnt, T(nameof(R.BV_VORLAGEN_FORMAT), Path.GetFileName(quellpfad.Trim())));
            return null;
        }

        /// <summary>Kopiert <paramref name="quelle"/> als <paramref name="datei"/> in den Vorlagenordner.</summary>
        private Vorlagenergebnis Lege(string quelle, string datei, bool ersetzen, string herkunft, string meldung)
        {
            Ordnerbefund ordner = PruefeOrdner(anlegen: true);
            if (!ordner.Erfolg) return Ergebnis(Vorlagenergebnisart.OrdnerNichtErreichbar, ordner.Meldung);
            string ziel = Path.Combine(ordner.Pfad, datei);

            if (herkunft != null && GleicherPfad(quelle, ziel))
            {
                Ablage bestand = LiesAblage(ordner.Pfad);
                if (bestand.Finde(datei) == null)
                {
                    try { Merke(ordner.Pfad, datei, null, Vorlagenpruefer.Pruefsumme(LiesDatei(ziel))); }
                    catch (Exception) { /* die Ablage ist Beiwerk */ }
                }
                return new Vorlagenergebnis(Vorlagenergebnisart.Erledigt, T(nameof(R.BV_VORLAGEN_BEREITS_IM_ORDNER), datei),
                                            EigenerEintrag(ziel, LiesAblage(ordner.Pfad)), null, ziel);
            }
            if (!ersetzen && File.Exists(ziel))
                return new Vorlagenergebnis(Vorlagenergebnisart.NameVergeben, T(nameof(R.BV_VORLAGEN_NAME_VERGEBEN), datei), null,
                                            EigenerEintrag(ziel, LiesAblage(ordner.Pfad)), ziel);

            byte[] bytes;
            try { bytes = LiesDatei(quelle); }
            catch (Exception ex)
            {
                return Ergebnis(Vorlagenergebnisart.Fehler, T(nameof(R.BV_VORLAGEN_NICHT_LESBAR), Path.GetFileName(quelle), ex.Message));
            }

            try
            {
                using (var strom = new FileStream(ziel, ersetzen ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    strom.Write(bytes, 0, bytes.Length);
            }
            catch (IOException) when (!ersetzen && File.Exists(ziel))
            {
                return new Vorlagenergebnis(Vorlagenergebnisart.NameVergeben, T(nameof(R.BV_VORLAGEN_NAME_VERGEBEN), datei), null,
                                            EigenerEintrag(ziel, LiesAblage(ordner.Pfad)), ziel);
            }
            catch (Exception ex)
            {
                return Ergebnis(Vorlagenergebnisart.Fehler, T(nameof(R.BV_VORLAGEN_FEHLER), datei, ex.Message));
            }

            Merke(ordner.Pfad, datei, herkunft == null ? null : VollerPfad(herkunft), Vorlagenpruefer.Pruefsumme(bytes));
            return new Vorlagenergebnis(Vorlagenergebnisart.Erledigt, T(meldung, Path.GetFileNameWithoutExtension(datei)),
                                        EigenerEintrag(ziel, LiesAblage(ordner.Pfad)), null, ziel);
        }

        /// <summary>
        /// Taugt <paramref name="name"/> als Name einer neuen Vorlage — dieselbe Regel, nach der
        /// <see cref="NeueVorlage"/> und <see cref="HinzufuegenAls"/> den Dateinamen bilden: ohne Pfadteile,
        /// verbotene Zeichen, reservierte Namen, Sperr- oder Punktvorsilbe; eine Word-Endung darf dabeistehen.
        /// Die Namensprüfung der Oberfläche fragt hier, statt die Regel ein zweites Mal zu führen.
        /// </summary>
        public static bool IstGueltigerName(string name)
        {
            return Zielname(name, ".docx") != null;
        }

        /// <summary>Enthält <paramref name="name"/> ein Zeichen, das in keinem Dateinamen stehen darf (<c>&lt; &gt; : " / \ | ? *</c>)?</summary>
        public static bool HatVerboteneZeichen(string name)
        {
            return !string.IsNullOrEmpty(name) && (name.IndexOfAny(VerboteneZeichen) >= 0 || name.Any(c => c < 32));
        }

        /// <summary>Ein Dateiname aus einer Eingabe: ohne Pfad, ohne verbotene Zeichen, mit Word-Endung; <c>null</c> = ungültig.</summary>
        private static string Zielname(string name, string endung)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            string n = name.Trim();
            string e = Path.GetExtension(n);
            if (Endungen.Contains(e.ToLowerInvariant())) n = n.Substring(0, n.Length - e.Length).TrimEnd();
            if (n.Length == 0 || n.EndsWith(".", StringComparison.Ordinal)) return null;
            string ziel = n + (string.IsNullOrEmpty(endung) ? ".docx" : endung.ToLowerInvariant());
            return IstEinfacherDateiname(ziel) ? ziel : null;
        }

        /// <summary>Ein Dateiname ohne Pfadteile, verbotene Zeichen, reservierte Namen, Sperr- oder Punktvorsilbe.</summary>
        private static bool IstEinfacherDateiname(string datei)
        {
            if (string.IsNullOrWhiteSpace(datei) || datei != datei.Trim()) return false;
            if (datei.IndexOfAny(VerboteneZeichen) >= 0 || datei.Any(c => c < 32)) return false;
            if (datei.StartsWith("~$", StringComparison.Ordinal) || datei.StartsWith(".", StringComparison.Ordinal)) return false;
            string stamm = Path.GetFileNameWithoutExtension(datei);
            return stamm.Length > 0 && !ReservierteNamen.Contains(stamm.ToUpperInvariant());
        }

        private static string FreierName(string ordner, string datei)
        {
            string ziel = Path.Combine(ordner, datei);
            string stamm = Path.GetFileNameWithoutExtension(datei);
            string endung = Path.GetExtension(datei);
            for (int n = 2; File.Exists(ziel) && n < 1000; n++)
                ziel = Path.Combine(ordner, stamm + " (" + n.ToString(CultureInfo.InvariantCulture) + ")" + endung);
            return ziel;
        }

        // =====================================================================
        //  Vorgabe, Abweichung, Auflösung
        // =====================================================================

        /// <summary>Die Vorgabe der Word-Vorlage (Einstellung <see cref="EINSTELLUNG_VORGABE_WORD"/>); ohne Einstellung <see cref="ID_STANDARD"/>.</summary>
        public string VorgabeWordId
        {
            get
            {
                string id = Einstellungen.Lies(EINSTELLUNG_VORGABE_WORD, null);
                return string.IsNullOrWhiteSpace(id) ? ID_STANDARD : id.Trim();
            }
        }

        /// <summary>Setzt die Vorgabe; <c>null</c> entfernt die Einstellung (dann gilt die Standardvorlage).</summary>
        public void SetzeVorgabeWord(Vorlageneintrag eintrag)
        {
            SetzeVorgabeWord(eintrag?.Id);
        }

        /// <summary>Setzt die Vorgabe als Kennung; <c>null</c> oder leer entfernt die Einstellung.</summary>
        public void SetzeVorgabeWord(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) Einstellungen.Loesche(EINSTELLUNG_VORGABE_WORD);
            else Einstellungen.Schreib(EINSTELLUNG_VORGABE_WORD, id.Trim());
        }

        /// <summary>
        /// Setzt die Abweichung des Stammprojekts in seiner Konfiguration (Konzept 10.3); gespeichert wird
        /// sie mit der Konfiguration über <c>BerichtCtrl.Speichere</c>. <c>null</c> entfernt sie.
        /// </summary>
        public static void SetzeAbweichung(BerichtsKonfiguration konfig, Vorlageneintrag eintrag)
        {
            if (konfig == null) return;
            if (eintrag == null)
            {
                EntferneAbweichung(konfig);
                return;
            }
            if (eintrag.Quelle == Vorlagenquelle.Mitgeliefert)
            {
                konfig.VorlageWordQuelle = BerichtsKonfiguration.VORLAGE_QUELLE_STANDARD;
                konfig.VorlageWordDatei = null;
            }
            else
            {
                konfig.VorlageWordQuelle = BerichtsKonfiguration.VORLAGE_QUELLE_EIGEN;
                konfig.VorlageWordDatei = eintrag.Dateiname;
            }
        }

        /// <summary>Entfernt die Abweichung — es gilt wieder die Vorgabe.</summary>
        public static void EntferneAbweichung(BerichtsKonfiguration konfig)
        {
            if (konfig == null) return;
            konfig.VorlageWordQuelle = null;
            konfig.VorlageWordDatei = null;
        }

        /// <summary>Die Kennung der Abweichung; <c>null</c> ohne — auch bei einer Quelle, die EPOS-Plan nicht kennt.</summary>
        public static string AbweichungId(BerichtsKonfiguration konfig)
        {
            string quelle = konfig?.VorlageWordQuelle?.Trim().ToLowerInvariant();
            if (quelle == BerichtsKonfiguration.VORLAGE_QUELLE_STANDARD) return ID_STANDARD;
            if (quelle == BerichtsKonfiguration.VORLAGE_QUELLE_EIGEN && !string.IsNullOrWhiteSpace(konfig.VorlageWordDatei))
                return ID_PRAEFIX_EIGEN + konfig.VorlageWordDatei.Trim();
            return null;
        }

        /// <summary>
        /// Die Vorlage eines Laufs: die Abweichung des Stammprojekts, sonst die Vorgabe, sonst die
        /// Standardvorlage; fehlt die Standardvorlage selbst, der benannte Rückfall. Jede gespeicherte,
        /// aber fehlende Vorlage wird genannt („nicht vorhanden – Standard verwendet“).
        /// </summary>
        public Vorlagenwahl VorlageFuer(BerichtsKonfiguration konfig)
        {
            var fehlend = new List<string>();
            string fehlendeId = null;

            string abweichung = AbweichungId(konfig);
            if (abweichung != null)
            {
                if (IstStandardId(abweichung)) return StandardOderRueckfall(Vorlagenwahlgrund.Abweichung, fehlend, null);
                Vorlageneintrag eintrag = Finde(abweichung);
                if (eintrag != null) return Wahl(eintrag, Vorlagenwahlgrund.Abweichung, fehlend, null);
                fehlend.Add(NameAusId(abweichung));
                fehlendeId = abweichung;
            }

            string vorgabe = VorgabeWordId;
            if (!IstStandardId(vorgabe))
            {
                Vorlageneintrag eintrag = Finde(vorgabe);
                if (eintrag != null) return Wahl(eintrag, Vorlagenwahlgrund.Vorgabe, fehlend, fehlendeId);
                fehlend.Add(NameAusId(vorgabe));
                fehlendeId ??= vorgabe;
            }
            return StandardOderRueckfall(Vorlagenwahlgrund.Standard, fehlend, fehlendeId);
        }

        // =====================================================================
        //  Excel: Vorgabe, Abweichung, Auflösung (Konzept 10.3, Etappe BV-E7)
        // =====================================================================

        /// <summary>Die Vorgabe der Excel-Vorlage (Einstellung <see cref="EINSTELLUNG_VORGABE_EXCEL"/>); ohne Einstellung <see cref="ID_OHNE"/>.</summary>
        public string VorgabeExcelId
        {
            get
            {
                string id = Einstellungen.Lies(EINSTELLUNG_VORGABE_EXCEL, null);
                return string.IsNullOrWhiteSpace(id) ? ID_OHNE : id.Trim();
            }
        }

        /// <summary>Setzt die Vorgabe der Excel-Vorlage als Kennung; <c>null</c>, leer oder „ohne“ entfernt die Einstellung.</summary>
        public void SetzeVorgabeExcel(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || string.Equals(id.Trim(), ID_OHNE, StringComparison.OrdinalIgnoreCase))
                Einstellungen.Loesche(EINSTELLUNG_VORGABE_EXCEL);
            else Einstellungen.Schreib(EINSTELLUNG_VORGABE_EXCEL, id.Trim());
        }

        /// <summary>Setzt die abweichende Excel-Vorlage des Stammprojekts; <c>null</c> entfernt sie (dann gilt die Vorgabe).</summary>
        public static void SetzeAbweichungExcel(BerichtsKonfiguration konfig, Vorlageneintrag eintrag)
        {
            if (konfig == null) return;
            if (eintrag == null)
            {
                EntferneAbweichungExcel(konfig);
                return;
            }
            if (string.Equals(eintrag.Id, ID_OHNE, StringComparison.OrdinalIgnoreCase))
            {
                konfig.VorlageExcelQuelle = BerichtsKonfiguration.VORLAGE_QUELLE_OHNE;
                konfig.VorlageExcelDatei = null;
            }
            else
            {
                konfig.VorlageExcelQuelle = BerichtsKonfiguration.VORLAGE_QUELLE_EIGEN;
                konfig.VorlageExcelDatei = eintrag.Dateiname;
            }
        }

        /// <summary>Entfernt die abweichende Excel-Vorlage — es gilt wieder die Vorgabe.</summary>
        public static void EntferneAbweichungExcel(BerichtsKonfiguration konfig)
        {
            if (konfig == null) return;
            konfig.VorlageExcelQuelle = null;
            konfig.VorlageExcelDatei = null;
        }

        /// <summary>Die Kennung der abweichenden Excel-Vorlage; <c>null</c> ohne.</summary>
        public static string AbweichungExcelId(BerichtsKonfiguration konfig)
        {
            string quelle = konfig?.VorlageExcelQuelle?.Trim().ToLowerInvariant();
            if (quelle == BerichtsKonfiguration.VORLAGE_QUELLE_OHNE) return ID_OHNE;
            if (quelle == BerichtsKonfiguration.VORLAGE_QUELLE_EIGEN && !string.IsNullOrWhiteSpace(konfig.VorlageExcelDatei))
                return ID_PRAEFIX_EIGEN + konfig.VorlageExcelDatei.Trim();
            return null;
        }

        /// <summary>
        /// Die Excel-Vorlage eines Laufs: die Abweichung des Stammprojekts, sonst die Vorgabe, sonst „ohne Vorlage“. Eine
        /// gespeicherte, aber fehlende Vorlage wird genannt („… nicht vorhanden – die Mappe entsteht ohne Vorlage“); ihr
        /// Eintrag ist dann „ohne Vorlage“, <see cref="Vorlagenwahl.FehlendeId"/> nennt die fehlende.
        /// </summary>
        public Vorlagenwahl ExcelVorlageFuer(BerichtsKonfiguration konfig)
        {
            var meldungen = new List<string>();
            string fehlendeId = null;

            string abweichung = AbweichungExcelId(konfig);
            if (abweichung != null)
            {
                Vorlageneintrag eintrag = FindeExcel(abweichung);
                if (eintrag != null)
                    return new Vorlagenwahl(eintrag, Vorlagenwahlgrund.Abweichung, Grundtext(Vorlagenwahlgrund.Abweichung), meldungen, null);
                meldungen.Add(T(nameof(R.BV_XL_NICHT_VORHANDEN), NameAusId(abweichung)));
                fehlendeId = abweichung;
            }

            string vorgabe = VorgabeExcelId;
            if (!string.Equals(vorgabe, ID_OHNE, StringComparison.OrdinalIgnoreCase))
            {
                Vorlageneintrag eintrag = FindeExcel(vorgabe);
                if (eintrag != null)
                    return new Vorlagenwahl(eintrag, Vorlagenwahlgrund.Vorgabe, Grundtext(Vorlagenwahlgrund.Vorgabe), meldungen, fehlendeId);
                meldungen.Add(T(nameof(R.BV_XL_NICHT_VORHANDEN), NameAusId(vorgabe)));
                fehlendeId ??= vorgabe;
            }
            return new Vorlagenwahl(OhneExcelEintrag(), Vorlagenwahlgrund.Standard, Grundtext(Vorlagenwahlgrund.Standard), meldungen, fehlendeId);
        }

        private Vorlagenwahl StandardOderRueckfall(Vorlagenwahlgrund grund, List<string> fehlend, string fehlendeId)
        {
            Vorlageneintrag standard = Standardeintrag();
            if (standard.Vorhanden) return Wahl(standard, grund, fehlend, fehlendeId);

            List<string> meldungen = fehlend.Select(n => T(nameof(R.BV_VORLAGEN_NICHT_VORHANDEN), n, standard.Name)).ToList();
            meldungen.Add(standard.Rueckfallpfad != null
                ? T(nameof(R.BV_VORLAGEN_STANDARD_FEHLT), standard.Dateiname, Path.GetFileName(standard.Rueckfallpfad))
                : T(nameof(R.BV_VORLAGEN_STANDARD_FEHLT_CODE), standard.Dateiname));
            return new Vorlagenwahl(standard, Vorlagenwahlgrund.Rueckfall, Grundtext(Vorlagenwahlgrund.Rueckfall), meldungen, fehlendeId);
        }

        private Vorlagenwahl Wahl(Vorlageneintrag eintrag, Vorlagenwahlgrund grund, List<string> fehlend, string fehlendeId)
        {
            List<string> meldungen = fehlend.Select(n => T(nameof(R.BV_VORLAGEN_NICHT_VORHANDEN), n, eintrag.Name)).ToList();
            return new Vorlagenwahl(eintrag, grund, Grundtext(grund), meldungen, fehlendeId);
        }

        private string Grundtext(Vorlagenwahlgrund grund)
        {
            switch (grund)
            {
                case Vorlagenwahlgrund.Abweichung: return T(nameof(R.BV_VORLAGEN_GRUND_ABWEICHUNG));
                case Vorlagenwahlgrund.Vorgabe: return T(nameof(R.BV_VORLAGEN_GRUND_VORGABE));
                case Vorlagenwahlgrund.Rueckfall: return T(nameof(R.BV_VORLAGEN_GRUND_RUECKFALL));
                default: return T(nameof(R.BV_VORLAGEN_GRUND_STANDARD));
            }
        }

        private static bool IstStandardId(string id)
        {
            return string.Equals((id ?? "").Trim(), ID_STANDARD, StringComparison.OrdinalIgnoreCase);
        }

        private static string NameAusId(string id)
        {
            string t = (id ?? "").Trim();
            if (!t.StartsWith(ID_PRAEFIX_EIGEN, StringComparison.OrdinalIgnoreCase)) return t;
            string datei = t.Substring(ID_PRAEFIX_EIGEN.Length).Trim();
            try { return Path.GetFileNameWithoutExtension(datei); }
            catch (ArgumentException) { return datei; }
        }

        // =====================================================================
        //  Lesen und prüfen
        // =====================================================================

        /// <summary>
        /// Liest die Vorlage EINMAL in den Speicher, mit <see cref="FileShare.ReadWrite"/> — genau diese
        /// Bytes werden geprüft und gefüllt, auch wenn der Anwender während der Simulation speichert
        /// (Konzept 6.8). Bei der fehlenden Standardvorlage der Rückfall; <c>null</c>, wenn keine Datei
        /// da ist. Wirft <see cref="IOException"/>, wenn die Datei da, aber nicht lesbar oder größer als
        /// <see cref="Vorlagenpruefer.GRENZE_DATEI"/> ist.
        /// </summary>
        public byte[] LiesBytes(Vorlageneintrag eintrag)
        {
            string pfad = Lesepfad(eintrag);
            return pfad == null ? null : LiesDatei(pfad);
        }

        /// <summary>Liegt neben der Vorlage eine Sperrdatei <c>~$…</c> — ist sie in Word geöffnet?</summary>
        public bool IstInWordGeoeffnet(Vorlageneintrag eintrag)
        {
            return IstInWordGeoeffnet(eintrag?.Pfad);
        }

        /// <summary>
        /// Liegt neben der Datei eine Sperrdatei von Word? Word kürzt den Namen dafür um ein oder zwei
        /// Zeichen; geprüft werden alle drei Formen.
        /// </summary>
        public static bool IstInWordGeoeffnet(string pfad)
        {
            if (string.IsNullOrWhiteSpace(pfad)) return false;
            string ordner, name;
            try
            {
                ordner = Path.GetDirectoryName(pfad);
                name = Path.GetFileName(pfad);
            }
            catch (ArgumentException)
            {
                return false;
            }
            if (string.IsNullOrEmpty(ordner) || string.IsNullOrEmpty(name)) return false;
            return Sperrdateinamen(name).Any(k => File.Exists(Path.Combine(ordner, k)));
        }

        /// <summary>Die möglichen Namen der Sperrdatei: <c>~$name</c>, um ein und um zwei Zeichen gekürzt.</summary>
        internal static IEnumerable<string> Sperrdateinamen(string name)
        {
            yield return "~$" + name;
            if (name.Length > 1) yield return "~$" + name.Substring(1);
            if (name.Length > 2) yield return "~$" + name.Substring(2);
        }

        /// <summary>
        /// Prüft eine Vorlage der Liste (liest sie dafür einmal). Ist sie in Word geöffnet, kommt die
        /// Warnung „in Word geöffnet – ungespeicherte Änderungen fehlen“ hinzu.
        /// </summary>
        public Pruefbefund Pruefe(Vorlageneintrag eintrag, Pruefstufe stufe, Pruefkontext kontext)
        {
            kontext ??= new Pruefkontext();
            byte[] bytes;
            try { bytes = LiesBytes(eintrag); }
            catch (Exception ex)
            {
                return Unlesbar(stufe, kontext, Tk(kontext, nameof(R.BV_VORLAGEN_NICHT_LESBAR), eintrag?.Name ?? "", ex.Message),
                                nameof(R.BV_VORLAGEN_NICHT_LESBAR));
            }
            if (bytes == null)
                return Unlesbar(stufe, kontext, Tk(kontext, nameof(R.BV_VORLAGEN_FEHLT), eintrag?.Name ?? ""), nameof(R.BV_VORLAGEN_FEHLT));
            return Pruefe(bytes, eintrag, stufe, kontext);
        }

        /// <summary>Prüft schon gelesene Bytes einer Vorlage — der Weg „einmal lesen, prüfen, füllen“.</summary>
        public Pruefbefund Pruefe(byte[] bytes, Vorlageneintrag eintrag, Pruefstufe stufe, Pruefkontext kontext)
        {
            kontext ??= new Pruefkontext();
            if (kontext.Dateiname == null && eintrag != null)
                kontext = kontext.MitDateiname(Path.GetFileName(Lesepfad(eintrag) ?? eintrag.Pfad));
            // BV-E7: eine Excel-Vorlage prüft der Excel-Prüfer (dieselben Regeln wie der Excel-Füller).
            if (IstExcel(eintrag) || IstExcelDatei(kontext.Dateiname))
                return ExcelVorlagenpruefer.Pruefe(bytes, stufe, kontext);
            Pruefbefund befund = Vorlagenpruefer.Pruefe(bytes, stufe, kontext);
            if (eintrag != null && IstInWordGeoeffnet(eintrag))
                befund = befund.MitMeldung(new Pruefmeldung(Befundstufe.Warnung, nameof(R.BV_VORLAGEN_IN_WORD),
                                                            Tk(kontext, nameof(R.BV_VORLAGEN_IN_WORD)), eintrag.Name,
                                                            Tk(kontext, nameof(R.BV_VORLAGEN_IN_WORD_TUN)), null, null));
            return befund;
        }

        private static Pruefbefund Unlesbar(Pruefstufe stufe, Pruefkontext kontext, string text, string kennung)
        {
            var meldung = new Pruefmeldung(Befundstufe.Fehler, kennung, text, Tk(kontext, nameof(R.VF_PRUEF_ORT_DATEI)),
                                           Tk(kontext, nameof(R.VF_PRUEF_UNLESBAR_TUN)), null, null);
            return Pruefbefund.Unlesbar(stufe, meldung, "");
        }

        private static string Lesepfad(Vorlageneintrag eintrag)
        {
            if (eintrag == null) return null;
            if (!string.IsNullOrEmpty(eintrag.Pfad) && File.Exists(eintrag.Pfad)) return eintrag.Pfad;
            if (!string.IsNullOrEmpty(eintrag.Rueckfallpfad) && File.Exists(eintrag.Rueckfallpfad)) return eintrag.Rueckfallpfad;
            return null;
        }

        /// <summary>Liest eine Datei ganz, geteilt lesend und schreibend (Word darf sie offen halten).</summary>
        private static byte[] LiesDatei(string pfad)
        {
            using (var strom = new FileStream(pfad, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                if (strom.Length > Vorlagenpruefer.GRENZE_DATEI)
                    throw new IOException(T(nameof(R.VF_PRUEF_GROESSE),
                                            (strom.Length / (1024.0 * 1024.0)).ToString("0", CultureInfo.CurrentCulture),
                                            (Vorlagenpruefer.GRENZE_DATEI / (1024 * 1024)).ToString(CultureInfo.CurrentCulture)));
                using (var puffer = new MemoryStream(strom.CanSeek ? (int)Math.Max(0, strom.Length) : 0))
                {
                    strom.CopyTo(puffer);
                    return puffer.ToArray();
                }
            }
        }

        // =====================================================================
        //  Ersteller
        // =====================================================================

        /// <summary>
        /// Die Erstellerangaben für <c>ersteller.*</c> (BV-Q8): die Firma aus der Einstellung
        /// <see cref="EINSTELLUNG_FIRMA"/>, ohne Einstellung die des aktiven Lizenztokens (bei einer
        /// Demo- oder Personenlizenz womöglich leer). Programm und Fassung bleiben <c>null</c> — die
        /// setzt der Kern (<see cref="Berichtswerte.Aus"/>). Eine ausdrücklich leer gesetzte Firma
        /// bleibt leer. Dazu das Logo für <c>bild.ersteller.logo</c> aus <see cref="EINSTELLUNG_LOGO"/>,
        /// EINMAL geladen: PNG oder JPEG, höchstens <see cref="GRENZE_LOGO"/>; fehlt die Datei, ist sie
        /// unlesbar, zu groß oder kein Bild, bleibt das Logo <c>null</c>, und
        /// <see cref="Erstellerangaben.LogoWarnung"/> nennt den Grund („Logo nicht gefunden: &lt;pfad&gt;“).
        /// </summary>
        public Erstellerangaben Ersteller()
        {
            string firma = Einstellungen.Lies(EINSTELLUNG_FIRMA, null);
            if (firma == null) firma = FirmaAusLizenz();
            var angaben = new Erstellerangaben
            {
                Firma = string.IsNullOrWhiteSpace(firma) ? null : firma.Trim(),
                Programm = null,
                Version = null,
            };
            LadeLogo(angaben);
            return angaben;
        }

        /// <summary>Der eingestellte Pfad des Logos (<see cref="EINSTELLUNG_LOGO"/>); <c>null</c> = keins.</summary>
        public string LogoPfad
        {
            get
            {
                string pfad = Einstellungen.Lies(EINSTELLUNG_LOGO, null);
                return string.IsNullOrWhiteSpace(pfad) ? null : pfad.Trim();
            }
        }

        /// <summary>Ist ein Logo eingestellt, und lässt es sich laden (PNG oder JPEG, höchstens 5 MB)?</summary>
        public bool LogoVorhanden()
        {
            var angaben = new Erstellerangaben();
            LadeLogo(angaben);
            return angaben.Logo != null;
        }

        /// <summary>
        /// Schreibt den Pfad des Logos (<see cref="EINSTELLUNG_LOGO"/>); <c>null</c> oder leer entfernt die
        /// Einstellung — dann entfällt das Platzhalterbild im Bericht.
        /// </summary>
        public void SchreibeLogo(string pfad)
        {
            if (string.IsNullOrWhiteSpace(pfad)) Einstellungen.Loesche(EINSTELLUNG_LOGO);
            else Einstellungen.Schreib(EINSTELLUNG_LOGO, pfad.Trim());
        }

        /// <summary>Lädt das eingestellte Logo einmal in die Erstellerangaben — oder nennt, warum nicht.</summary>
        private void LadeLogo(Erstellerangaben angaben)
        {
            string pfad = LogoPfad;
            if (pfad == null) return;
            byte[] daten;
            try
            {
                var info = new FileInfo(pfad);
                if (!info.Exists)
                {
                    angaben.LogoWarnung = T(nameof(R.BV_VORLAGEN_LOGO_FEHLT), pfad);
                    return;
                }
                if (info.Length > GRENZE_LOGO)
                {
                    angaben.LogoWarnung = T(nameof(R.BV_VORLAGEN_LOGO_GROSS), pfad, Megabyte(info.Length), GRENZE_LOGO / (1024 * 1024));
                    return;
                }
                using (var strom = new FileStream(pfad, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var puffer = new MemoryStream())
                {
                    strom.CopyTo(puffer);
                    daten = puffer.ToArray();
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException ||
                                       ex is NotSupportedException || ex is System.Security.SecurityException)
            {
                angaben.LogoWarnung = T(nameof(R.BV_VORLAGEN_LOGO_FEHLT), pfad);
                return;
            }
            if (daten.LongLength > GRENZE_LOGO)
            {
                angaben.LogoWarnung = T(nameof(R.BV_VORLAGEN_LOGO_GROSS), pfad, Megabyte(daten.LongLength), GRENZE_LOGO / (1024 * 1024));
                return;
            }
            if (Bildinhalt.Aus(daten, pfad) == null)
            {
                angaben.LogoWarnung = T(nameof(R.BV_VORLAGEN_LOGO_FORMAT), pfad);
                return;
            }
            angaben.Logo = daten;
            angaben.LogoDateiname = Path.GetFileName(pfad);
        }

        private static string Megabyte(long bytes)
        {
            return (bytes / (1024.0 * 1024.0)).ToString("0.0", CultureInfo.CurrentCulture);
        }

        /// <summary>Die Firma des aktiven Lizenztokens — die Vorbelegung des Felds „Firma“; <c>null</c> ohne.</summary>
        public string FirmaAusLizenz()
        {
            try
            {
                string firma = _lizenzFirma?.Invoke();
                return string.IsNullOrWhiteSpace(firma) ? null : firma.Trim();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Schreibt die Firma; <c>null</c> entfernt die Einstellung (dann gilt wieder die Firma der
        /// Lizenz), ein leerer Text setzt sie ausdrücklich leer.
        /// </summary>
        public void SchreibeFirma(string text)
        {
            if (text == null) Einstellungen.Loesche(EINSTELLUNG_FIRMA);
            else Einstellungen.Schreib(EINSTELLUNG_FIRMA, text.Trim());
        }

        private static string FirmaDesTokens()
        {
            return LizenzManager.Token?.Firma;
        }

        // =====================================================================
        //  Ablagedatei (Herkunft und Prüfsumme, keine Datenbank)
        // =====================================================================

        /// <summary>Der Inhalt der Ablagedatei.</summary>
        internal sealed class Ablage
        {
            public int Fassung { get; set; } = 1;

            public List<Ablageeintrag> Vorlagen { get; set; } = new List<Ablageeintrag>();

            public Ablageeintrag Finde(string datei)
            {
                return Vorlagen?.FirstOrDefault(v => v != null && string.Equals(v.Datei, datei, PfadVergleich));
            }
        }

        /// <summary>Herkunft und Prüfsumme einer eigenen Vorlage.</summary>
        internal sealed class Ablageeintrag
        {
            public string Datei { get; set; }

            public string Herkunftspfad { get; set; }

            public string Pruefsumme { get; set; }

            public DateTime? Hinzugefuegt { get; set; }
        }

        /// <summary>Eingerückt und mit Umlauten im Klartext — die Ablagedatei soll ein Mensch lesen können.</summary>
        private static readonly JsonSerializerOptions AblageJson = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        /// <summary>Liest die Ablagedatei duldsam: fehlt sie oder ist sie unlesbar, eine leere.</summary>
        internal static Ablage LiesAblage(string ordner)
        {
            try
            {
                string pfad = Path.Combine(ordner, ABLAGEDATEI);
                if (!File.Exists(pfad)) return new Ablage();
                string json;
                using (var strom = new FileStream(pfad, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var leser = new StreamReader(strom, Encoding.UTF8))
                    json = leser.ReadToEnd();
                Ablage ablage = JsonSerializer.Deserialize<Ablage>(json, AblageJson);
                if (ablage == null) return new Ablage();
                ablage.Vorlagen ??= new List<Ablageeintrag>();
                return ablage;
            }
            catch (Exception)
            {
                return new Ablage();
            }
        }

        private static void Merke(string ordner, string datei, string herkunft, string pruefsumme)
        {
            Ablage ablage = LiesAblage(ordner);
            ablage.Vorlagen.RemoveAll(v => v == null || string.Equals(v.Datei, datei, PfadVergleich));
            ablage.Vorlagen.Add(new Ablageeintrag
            {
                Datei = datei,
                Herkunftspfad = herkunft,
                Pruefsumme = pruefsumme,
                Hinzugefuegt = DateTime.Now,
            });
            SchreibeAblage(ordner, ablage);
        }

        private static void Vergiss(string ordner, string datei)
        {
            if (string.IsNullOrEmpty(ordner)) return;
            Ablage ablage = LiesAblage(ordner);
            if (ablage.Vorlagen.RemoveAll(v => v == null || string.Equals(v.Datei, datei, PfadVergleich)) > 0)
                SchreibeAblage(ordner, ablage);
        }

        /// <summary>Schreibt die Ablagedatei über eine Zwischendatei; Einträge ohne Datei fallen weg. Scheitern ist still — die Vorlage selbst liegt schon.</summary>
        private static void SchreibeAblage(string ordner, Ablage ablage)
        {
            string ziel = Path.Combine(ordner, ABLAGEDATEI);
            string zwischen = ziel + ".tmp";
            try
            {
                ablage.Vorlagen.RemoveAll(v => v == null || string.IsNullOrEmpty(v.Datei) || !File.Exists(Path.Combine(ordner, v.Datei)));
                ablage.Vorlagen.Sort((a, b) => string.Compare(a.Datei, b.Datei, StringComparison.OrdinalIgnoreCase));
                File.WriteAllText(zwischen, JsonSerializer.Serialize(ablage, AblageJson), new UTF8Encoding(false));
                File.Move(zwischen, ziel, true);
            }
            catch (Exception)
            {
                try { if (File.Exists(zwischen)) File.Delete(zwischen); } catch (Exception) { /* nichts */ }
            }
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>Ein vollständiger Pfad — auch unter Windows kein laufwerksrelativer wie <c>C:ordner</c>.</summary>
        private static bool IstGueltigerOrdner(string ordner)
        {
            if (string.IsNullOrWhiteSpace(ordner)) return false;
            try
            {
                return Path.IsPathFullyQualified(ordner) && Path.GetFullPath(ordner).Length > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string VollerPfad(string pfad)
        {
            try { return Path.GetFullPath(pfad); }
            catch (Exception) { return pfad; }
        }

        private static bool GleicherPfad(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
            string x = VollerPfad(a.Trim()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string y = VollerPfad(b.Trim()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(x, y, PfadVergleich);
        }

        private static Vorlagenergebnis Ergebnis(Vorlagenergebnisart art, string meldung, Vorlageneintrag eintrag = null)
        {
            return new Vorlagenergebnis(art, meldung, eintrag, null, null);
        }

        /// <summary>Ein Text der Oberfläche (Sprache der Oberfläche), formatiert.</summary>
        private static string T(string schluessel, params object[] argumente)
        {
            return Formatiere(schluessel, CultureInfo.CurrentUICulture, CultureInfo.CurrentCulture, argumente);
        }

        /// <summary>Ein Text in der Sprache des Berichts (für Meldungen im Prüfbefund).</summary>
        private static string Tk(Pruefkontext kontext, string schluessel, params object[] argumente)
        {
            CultureInfo kultur = BerichtTexte.KulturFuer(kontext?.Englisch ?? false);
            return Formatiere(schluessel, kultur, kultur, argumente);
        }

        private static string Formatiere(string schluessel, CultureInfo sprache, CultureInfo format, object[] argumente)
        {
            string muster = null;
            try { muster = R.ResourceManager.GetString(schluessel, sprache); }
            catch (Exception) { muster = null; }
            muster ??= schluessel;
            if (argumente == null || argumente.Length == 0) return muster;
            try { return string.Format(format, muster, argumente); }
            catch (FormatException) { return muster; }
        }
    }
}
