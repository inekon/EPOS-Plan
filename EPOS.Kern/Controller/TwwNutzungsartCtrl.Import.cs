using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using NReco.Csv;

namespace WindowsFormsApplication1
{
    /// <summary>Eine Datei eines Katalogpakets: ihr Name (ohne Pfad, etwa <c>Tab_TwwNutzungsart_STAMM.csv</c>) und ihr Text.</summary>
    internal sealed record TwwPaketdatei(string Name, string Inhalt);

    /// <summary>Wie eine Nutzungsart des Pakets im Katalogimport ausgegangen ist.</summary>
    internal enum TwwImportausgang
    {
        /// <summary>Neu im Katalog (Status <c>IMPORT</c>).</summary>
        Angelegt = 0,

        /// <summary>Nicht angelegt, weil der Katalog sie schon führt (gleicher Inhalt oder Auslieferung).</summary>
        Uebersprungen = 1,

        /// <summary>Nicht angelegt, weil sie eine Regel des Katalogs verletzt; <see cref="TwwImportzeile.Grund"/> nennt sie.</summary>
        Abgelehnt = 2,

        /// <summary>
        /// Die vorhandene Zeile trägt jetzt die Werte des Pakets (Bedarfstag, Parameter — ZU30,
        /// ZU31): dieselbe <c>ID</c>, Stand <c>IMPORT</c>, <c>ReadOnly</c> 0. Nutzungsarten werden
        /// nie ersetzt.
        /// </summary>
        Ersetzt = 3
    }

    /// <summary>
    /// Zu welcher Tabelle eine Zeile des Importberichts gehört (ZU32): Der Bericht führt je Tabelle
    /// eigene Zeilen, in der Reihenfolge Bedarfstage, Parameter, Nutzungsarten.
    /// </summary>
    internal enum TwwImportbereich
    {
        /// <summary><c>Tab_TwwNutzungsart_STAMM</c> samt Tagesgangsatz, Tagesgängen und Kategorien.</summary>
        Nutzungsart = 0,

        /// <summary><c>Tab_TwwBedarfstag_STAMM</c> samt <c>Tab_TwwBedarfstagEreignis_STAMM</c>.</summary>
        Bedarfstag = 1,

        /// <summary><c>Tab_TwwParameter_STAMM</c>.</summary>
        Parameter = 2
    }

    /// <summary>
    /// Eine Zeile des Importberichts — je Eintrag des Pakets: Ausgang, Name und Katalogversion
    /// aus dem Paket, der Name im Katalog (bei einer abweichenden namensgleichen Zeile „… (Import n)"),
    /// die neue Id (0, wenn nichts angelegt wurde), die Zeile der Paketdatei und der Grund als
    /// <see cref="ZapfSatz"/> (bei „angelegt" nur, wenn der Name abweicht).
    /// </summary>
    internal sealed record TwwImportzeile(TwwImportausgang Ausgang, string Nutzungsart, string Katalogversion,
                                          string Katalogname, int IdNeu, int Zeile, ZapfSatz Grund)
    {
        /// <summary>Die Tabelle, zu der die Zeile gehört (Vorgabe: die Nutzungsarten).</summary>
        internal TwwImportbereich Bereich { get; init; }

        /// <summary>
        /// Die Id der Zeile, die der Import ersetzt hat (<see cref="TwwImportausgang.Ersetzt"/>) —
        /// dieselbe wie vorher, damit ein Projekt, das darauf zeigt, weiter darauf zeigt; 0 sonst.
        /// </summary>
        internal int IdErsetzt { get; init; }
    }

    /// <summary>
    /// <b>Der Bericht eines Katalogimports</b>: je Nutzungsart eine <see cref="TwwImportzeile"/>, dazu
    /// benannte Hinweise (übergangene Dateien und Zeilen) und — wenn das Paket seiner Form nach nicht
    /// taugt — der <see cref="Abbruch"/>: Dann ist NICHTS geändert.
    /// </summary>
    internal sealed class TwwKatalogimportBericht
    {
        /// <summary>
        /// Je Eintrag des Pakets eine Zeile, in der Reihenfolge Bedarfstage, Parameter,
        /// Nutzungsarten und darin in der Reihenfolge der Paketdatei (ZU32).
        /// </summary>
        internal List<TwwImportzeile> Zeilen { get; } = new List<TwwImportzeile>();

        /// <summary>Übergangene Dateien, Tagesgänge und Kategorien — benannt, nie still.</summary>
        internal List<ZapfSatz> Hinweise { get; } = new List<ZapfSatz>();

        /// <summary>Der Grund, aus dem das Paket als Ganzes abgelehnt ist; <c>null</c> = gelesen.</summary>
        internal ZapfSatz Abbruch { get; set; }

        /// <summary>
        /// War es ein Prüflauf? Dann ist NICHTS geschrieben, und die Zeilen sagen, was ein Import
        /// täte („würde ersetzen").
        /// </summary>
        internal bool Pruefmodus { get; set; }

        internal int Angelegt => Zeilen.Count(z => z.Ausgang == TwwImportausgang.Angelegt);
        internal int Uebersprungen => Zeilen.Count(z => z.Ausgang == TwwImportausgang.Uebersprungen);
        internal int Abgelehnt => Zeilen.Count(z => z.Ausgang == TwwImportausgang.Abgelehnt);
        internal int Ersetzt => Zeilen.Count(z => z.Ausgang == TwwImportausgang.Ersetzt);

        /// <summary>Die Ids der angelegten Nutzungsarten, in der Reihenfolge des Pakets.</summary>
        internal IReadOnlyList<int> NeueIds => Zeilen
            .Where(z => z.Ausgang == TwwImportausgang.Angelegt && z.Bereich == TwwImportbereich.Nutzungsart)
            .Select(z => z.IdNeu).ToList();

        /// <summary>Die Zeilen einer Tabelle, in ihrer Reihenfolge.</summary>
        internal IReadOnlyList<TwwImportzeile> ZeilenVon(TwwImportbereich bereich)
            => Zeilen.Where(z => z.Bereich == bereich).ToList();
    }

    /// <summary>
    /// <b>Der Katalogimport der Brauchwasser-Nutzungsarten</b> (Umsetzungskonzept
    /// Zapfprofilgenerator 5.4, Kapitel 6 (b), N2, N12 (m)/(p); Stufe Z4): eine Nutzungsart samt
    /// Tagesgangsatz, Tagesgängen und Zapfkategorien aus einem Paket im Format N2 — je Tabelle eine
    /// Datei <c>&lt;Tabelle&gt;.csv</c> (UTF-8, Zeilenende CRLF, LF oder CR, Kopfzeile mit den
    /// Spaltennamen, Trenner <c>;</c> oder <c>,</c>, Felder nach RFC 4180, Zahlen mit Punkt, leeres
    /// Feld = NULL). Das Paket vergibt die <c>ID</c> seiner Köpfe selbst; Tagesgänge verweisen über
    /// <c>ID_Tagesgangsatz</c>, Nutzungsarten über <c>ID_Tagesgangsatz</c>, Kategorien über
    /// <c>ID_Nutzungsart</c> auf diese IDs — die Datenbank vergibt die echten.
    ///
    /// <para><b>Was entsteht.</b> Jede angelegte Zeile ist eine Anwenderzeile: <c>Status = 'IMPORT'</c>,
    /// <c>ReadOnly = 0</c>, ohne <c>ID_Vorlage</c>, ohne internen <c>Beleg</c> und ohne Freigabe (die
    /// Auslieferungsvorlage entfernt sie wieder, Konzept 6 (b)). Jede Wertgruppe trägt die
    /// Herkunftsart <c>IMPORT</c> („vom Anwender eingespielt") mit Quelle, Ausgabe und Version des
    /// Pakets — ausgenommen <c>FREI</c> (frei verfügbare Daten, etwa der Paketteil N12 (p)) und
    /// <c>FIKTIV</c> (erfundene Werte), die ihre Herkunftsart behalten. Status und <c>ReadOnly</c> des
    /// Pakets werden gelesen und übergangen.</para>
    ///
    /// <para><b>Dublettenscan (Inhaltsvergleich wie im Projektimport, ZU17).</b> Führt der Katalog eine
    /// Nutzungsart mit demselben natürlichen Schlüssel (Bezeichner, Katalogversion), wird verglichen:
    /// alle Wertgruppen ohne ihre Provenienz, der Tagesgangsatz über seine vier Tagesgänge und die
    /// Zapfkategorien in ihrer Reihenfolge. Gleich — übersprungen; eine Zeile der Auslieferung gilt als
    /// gleich (unveränderliche Version) — übersprungen; abweichend — angelegt als „Bezeichner (Import n)"
    /// mit der kleinsten freien Zahl, oder übersprungen, wenn eine frühere „(Import n)" denselben Inhalt
    /// trägt. Für den Tagesgangsatz gilt dieselbe Regel. <b>Die Katalogsperre bleibt unberührt:</b> Der
    /// Import ändert und löscht keine vorhandene Zeile.</para>
    ///
    /// <para><b>Kategorien als Datenblock (N12 (m)/(p)).</b> Eine Kategorie mit <c>ID_Nutzungsart</c>
    /// gehört zu dieser Nutzungsart des Pakets; Kategorien OHNE sie sind ein Vorgabesatz und binden an
    /// jede Nutzungsart des Pakets, die keine eigenen führt. Ohne die Tabelle (Stand vor Schritt 115)
    /// sind sie benannt übergangen.</para>
    ///
    /// <para><b>Zwei Stufen der Ablehnung.</b> Taugt das Paket seiner Form nach nicht (unbekannte oder
    /// fehlende Spalte, falsche Feldzahl, keine Zahl, doppelte ID, ein Ereignis ohne seinen
    /// Bedarfstag), ist es als Ganzes abgelehnt und
    /// NICHTS geändert — der Bericht nennt Datei, Zeile und Spalte. Verletzt eine Nutzungsart eine
    /// Regel (Pflichtangabe, Wertemenge, Raster, Tagesgangsatz, Kategorien), ist nur sie abgelehnt;
    /// die übrigen werden angelegt. Geschrieben wird in EINEM Vorgang.</para>
    ///
    /// <para><b>Bedarfstage und Parameter (ZU30 bis ZU33).</b> Das Paket darf dazu
    /// <c>Tab_TwwBedarfstag_STAMM.csv</c>, <c>Tab_TwwBedarfstagEreignis_STAMM.csv</c> und
    /// <c>Tab_TwwParameter_STAMM.csv</c> führen; sie sind wahlfrei, und ohne die Tabelle im Schema
    /// ist die Datei benannt übergangen. Hier gilt eine ANDERE Dublettenregel als bei den
    /// Nutzungsarten: <b>Der Import ersetzt die vorhandene Zeile</b> — am Platz, mit derselben
    /// <c>ID</c>, damit ein Projekt, das den Bedarfstag gewählt hat, weiter darauf zeigt, und
    /// <b>auch eine Zeile der Auslieferung</b>. Die ersetzte Zeile ist danach eine Anwenderzeile
    /// (<c>Status = 'IMPORT'</c>, <c>ReadOnly = 0</c>, ohne <c>Beleg</c>) mit der Provenienz des
    /// Pakets; die Ereignisse eines Bedarfstags werden vollständig ersetzt. Der Bericht nennt jede
    /// Ersetzung, und ein Hinweis nennt die betroffenen Auslieferungszeilen. Eine Versionsbildung
    /// „(Import n)" gibt es hier nicht — ein Parameter ist ein Wert, keine Version.</para>
    ///
    /// <para><b>Prüfmodus.</b> <see cref="Importieren(IReadOnlyList{TwwPaketdatei}, bool)"/> mit
    /// <c>pruefen = true</c> rechnet denselben Bericht und schreibt NICHTS (der Vorgang wird
    /// zurückgerollt); die Oberfläche sagt dann „würde ersetzen" statt „ersetzt".</para>
    /// </summary>
    internal static partial class TwwNutzungsartCtrl
    {
        /// <summary>
        /// Die Dateien des Katalogpakets in Einspielreihenfolge (Verwiesene zuerst): der Bedarfstag
        /// vor seinen Ereignissen, die Parameter, dann der Tagesgangsatz vor seinen Tagesgängen und
        /// die Nutzungsart vor ihren Kategorien.
        /// </summary>
        internal static readonly IReadOnlyList<string> IMPORT_TABELLEN = new[]
        {
            TwwSchema.TAB_TWW_BEDARFSTAG_STAMM,
            TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM,
            TwwSchema.TAB_TWW_PARAMETER_STAMM,
            TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM,
            TwwSchema.TAB_TWW_TAGESGANG_STAMM,
            TwwSchema.TAB_TWW_NUTZUNGSART_STAMM,
            TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM
        };

        /// <summary>Der Zusatz einer abweichenden namensgleichen Version: „ (Import n)" — derselbe wie im Projektimport.</summary>
        internal static string ImportZusatz(int n) => " (Import " + n.ToString(CultureInfo.InvariantCulture) + ")";

        /// <summary>
        /// <b>Höchstzahl der Einträge eines Katalogpakets</b> (numerische Setzung): Das Format N2
        /// kennt vier Dateien des Nutzungsartkatalogs und die Dateien des Paketteils; 200 Einträge
        /// lassen Ordner, Beilagen und Schreibweisen zu und fangen ein Archiv ab, das nicht dieses
        /// Paket ist. Geprüft wird das Zentralverzeichnis, bevor ein Byte entpackt wird — dieselbe
        /// Regel wie im Normformvektorleser (N13 (s), Größenschutz).
        /// </summary>
        internal const int HOECHSTENS_EINTRAEGE = 200;

        /// <summary>
        /// <b>Höchste entpackte Gesamtgröße eines Katalogpakets [Byte]</b> (numerische Setzung,
        /// 64 MB): Die CSV-Dateien tragen Text; ein Katalog mit einigen hundert Nutzungsarten und
        /// ihren Tagesgängen bleibt weit darunter. Die Grenze fängt das aufgeblähte Archiv ab,
        /// ohne es zu entpacken.
        /// </summary>
        internal const long HOECHSTENS_BYTE_ENTPACKT = 64L * 1024 * 1024;

        /// <summary>
        /// <b>Höchste Größe EINER Paketdatei [Byte]</b> (numerische Setzung, 16 MB): Der Leser hält
        /// jede Datei ganz im Speicher (<see cref="File.ReadAllText(string, Encoding)"/> bzw.
        /// <see cref="StreamReader.ReadToEnd"/>); die Grenze gilt für das Archiv wie für den Ordner.
        /// </summary>
        internal const long HOECHSTENS_BYTE_JE_DATEI = 16L * 1024 * 1024;

        /// <summary>Spalten, die das Paket führen darf, die der Import aber nicht übernimmt.</summary>
        private static readonly HashSet<string> IMPORT_UEBERGANGEN = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Status", "ReadOnly", "Beleg", "Freigabe", "ID_Vorlage"
        };

        // =================================================================================
        // Das Paket lesen
        // =================================================================================

        /// <summary>
        /// <b>Die Dateien eines Pakets</b> zu einem gewählten Pfad: ein ZIP-Archiv liefert seine
        /// <c>*.csv</c>-Einträge (ohne Ordner), eine CSV-Datei die <c>Tab_Tww*.csv</c> ihres Ordners
        /// (das Paket ist der Ordner, gewählt wird eine Datei darin) und dazu sich selbst, ein Ordner
        /// seine <c>*.csv</c>. Eine Datei, die sich nicht lesen lässt, ergibt den benannten
        /// <paramref name="fehler"/> und eine leere Liste.
        ///
        /// <para><b>Der Größenschutz des Archivs</b> (N13 (s)): Eintragszahl und entpackte
        /// Gesamtgröße stehen im Zentralverzeichnis und werden geprüft, <b>bevor ein Byte entpackt
        /// wird</b> (<c>KATALOGIMPORT_ZU_GROSS</c>); dazu die Größe jeder einzelnen Datei
        /// (<c>KATALOGIMPORT_DATEI_ZU_GROSS</c>, auch für Ordner und Einzeldatei — dort samt
        /// Eintragszahl und Gesamtgröße, denn ein Ordner kann ebenso überladen sein). Ein Eintragsname,
        /// der aus dem Archiv herauszeigt (<c>..</c>, Wurzel, Laufwerk), wird benannt abgelehnt
        /// (<c>KATALOGIMPORT_PFAD_UNZULAESSIG</c>); ein Unterordner ist erlaubt, denn der Leser nimmt
        /// den Dateinamen. Verzeichniseinträge fallen still, sie tragen keinen Inhalt.</para>
        ///
        /// <para><b>Das Zentralverzeichnis ist eine Behauptung</b> der Datei, also gilt <b>dieselbe
        /// Grenze ein zweites Mal beim Lesen</b>: Je Eintrag wird bis zur Grenze und ein Byte darüber
        /// gelesen, und die Summe der gelesenen Einträge läuft mit; wer darüber kommt, fällt mit
        /// derselben Kennung (Muster <c>Allgemein/Import/Ifc/IfcLeser.Entpacken</c>). Diese zweite
        /// Wand ist <b>Vorsorge</b>: <see cref="ZipArchiveEntry.Open"/> begrenzt den Entpackstrom
        /// heute selbst auf die ausgewiesene Größe (gemessen in
        /// <c>EPOS.Kern.Tests/TwwKatalogimportTests</c>), ein zu klein ausgewiesener Eintrag kommt
        /// also <b>gekürzt</b> herein statt zu groß — und fällt dann der Formprüfung des Einspielens
        /// zu, nicht dem Größenschutz. Der Leser verlässt sich nicht darauf. Die beiden Grenzen sind
        /// <b>Parameter mit den Konstanten als Vorgabe</b>, damit ein Test das Greifen der Prüfung an
        /// einem kleinen Archiv messen kann statt an 64 MB.</para>
        /// </summary>
        internal static IReadOnlyList<TwwPaketdatei> PaketLesen(string pfad, out ZapfSatz fehler,
                                                                long grenzeJeDatei = HOECHSTENS_BYTE_JE_DATEI,
                                                                long grenzeGesamt = HOECHSTENS_BYTE_ENTPACKT)
        {
            fehler = null;
            var dateien = new List<TwwPaketdatei>();
            string name = Path.GetFileName(pfad ?? "");
            try
            {
                if (Directory.Exists(pfad))
                {
                    string[] gefunden = Directory.GetFiles(pfad, "*.csv")
                                                 .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
                    if (MengeZuGross(gefunden, grenzeGesamt, out fehler)) return new TwwPaketdatei[0];
                    foreach (string d in gefunden)
                    {
                        if (ZuGross(d, grenzeJeDatei, out fehler)) return new TwwPaketdatei[0];
                        dateien.Add(new TwwPaketdatei(Path.GetFileName(d), Lesen(d)));
                    }
                }
                else if (string.Equals(Path.GetExtension(pfad), ".zip", StringComparison.OrdinalIgnoreCase))
                {
                    using (ZipArchive zip = ZipFile.OpenRead(pfad))
                    {
                        // Die Mengengrenze VOR dem Entpacken, allein aus dem Zentralverzeichnis:
                        // Eintragszahl und entpackte Gesamtgroesse (wie im Normformvektorleser). Die
                        // Summe bricht AN der Grenze ab, damit sie an erfundenen Laengen nicht
                        // ueberlaeuft (200 Eintraege mit 2^62 Byte waeren sonst eine kleine Zahl).
                        long entpackt = 0;
                        foreach (ZipArchiveEntry e in zip.Entries)
                        {
                            long l = Math.Max(0L, e.Length);
                            if (l > grenzeGesamt - entpackt) { entpackt = grenzeGesamt + 1; break; }
                            entpackt += l;
                        }
                        if (zip.Entries.Count > HOECHSTENS_EINTRAEGE || entpackt > grenzeGesamt)
                        {
                            fehler = ZapfSatz.Neu("KATALOGIMPORT_ZU_GROSS", zip.Entries.Count, HOECHSTENS_EINTRAEGE,
                                                  entpackt, grenzeGesamt);
                            return new TwwPaketdatei[0];
                        }
                        long gesamt = 0;
                        foreach (ZipArchiveEntry e in zip.Entries.OrderBy(x => x.FullName, StringComparer.OrdinalIgnoreCase))
                        {
                            // Ein Verzeichniseintrag traegt keinen Inhalt (Name leer) und faellt still.
                            if (e.Name.Length == 0) continue;
                            if (!Pfadsicher(e.FullName))
                            {
                                fehler = ZapfSatz.Neu("KATALOGIMPORT_PFAD_UNZULAESSIG", e.FullName);
                                return new TwwPaketdatei[0];
                            }
                            if (!string.Equals(Path.GetExtension(e.Name), ".csv", StringComparison.OrdinalIgnoreCase)) continue;
                            if (e.Length > grenzeJeDatei)
                            {
                                fehler = ZapfSatz.Neu("KATALOGIMPORT_DATEI_ZU_GROSS", e.Name, e.Length, grenzeJeDatei);
                                return new TwwPaketdatei[0];
                            }
                            // Und nun dieselbe Grenze BEIM Lesen: Das Verzeichnis kann gelogen haben.
                            string inhalt = EintragLesen(e, Math.Min(grenzeJeDatei, grenzeGesamt - gesamt), out long gelesen);
                            if (inhalt == null)
                            {
                                fehler = gelesen > grenzeJeDatei
                                    ? ZapfSatz.Neu("KATALOGIMPORT_DATEI_ZU_GROSS", e.Name, gelesen, grenzeJeDatei)
                                    : ZapfSatz.Neu("KATALOGIMPORT_ZU_GROSS", zip.Entries.Count, HOECHSTENS_EINTRAEGE,
                                                   gesamt + gelesen, grenzeGesamt);
                                return new TwwPaketdatei[0];
                            }
                            gesamt += gelesen;
                            dateien.Add(new TwwPaketdatei(e.Name, inhalt));
                        }
                    }
                }
                else
                {
                    string ordner = Path.GetDirectoryName(Path.GetFullPath(pfad)) ?? "";
                    List<string> satz = Directory.GetFiles(ordner, "Tab_Tww*.csv")
                                                 .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
                    // Die gewaehlte Datei gehoert dazu, auch wenn ihr Name nicht auf das Muster passt.
                    if (!satz.Any(d => string.Equals(Path.GetFileName(d), name, StringComparison.OrdinalIgnoreCase)))
                        satz.Add(pfad);
                    if (MengeZuGross(satz, grenzeGesamt, out fehler)) return new TwwPaketdatei[0];
                    var gelesen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (string d in satz)
                    {
                        if (ZuGross(d, grenzeJeDatei, out fehler)) return new TwwPaketdatei[0];
                        string dn = Path.GetFileName(d);
                        if (!gelesen.Add(dn)) continue;
                        dateien.Add(new TwwPaketdatei(dn, Lesen(d)));
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException
                                       || ex is NotSupportedException || ex is ArgumentException)
            {
                fehler = ZapfSatz.Neu("KATALOGIMPORT_DATEI_UNLESBAR", name, ex.Message);
                return new TwwPaketdatei[0];
            }
            return dateien;
        }

        private static string Lesen(string datei) => File.ReadAllText(datei, Encoding.UTF8);

        /// <summary>
        /// Ist die Datei größer als <paramref name="grenze"/> (<see cref="HOECHSTENS_BYTE_JE_DATEI"/>)?
        /// Dann steht der benannte Grund in <paramref name="fehler"/> und nichts wird gelesen.
        /// </summary>
        private static bool ZuGross(string datei, long grenze, out ZapfSatz fehler)
        {
            fehler = null;
            long laenge = new FileInfo(datei).Length;
            if (laenge <= grenze) return false;
            fehler = ZapfSatz.Neu("KATALOGIMPORT_DATEI_ZU_GROSS", Path.GetFileName(datei), laenge, grenze);
            return true;
        }

        /// <summary>
        /// <b>Eintragszahl und Gesamtgröße eines Ordner- oder Einzeldateiwegs</b> — dieselbe Grenze
        /// wie im Archiv (<see cref="HOECHSTENS_EINTRAEGE"/>, <paramref name="grenzeGesamt"/>), nur
        /// aus dem Dateisystem statt aus dem Zentralverzeichnis. Die Summe bricht AN der Grenze ab,
        /// damit sie nicht überläuft.
        /// </summary>
        private static bool MengeZuGross(IReadOnlyList<string> dateien, long grenzeGesamt, out ZapfSatz fehler)
        {
            fehler = null;
            long gesamt = 0;
            foreach (string d in dateien)
            {
                long l = Math.Max(0L, new FileInfo(d).Length);
                if (l > grenzeGesamt - gesamt) { gesamt = grenzeGesamt + 1; break; }
                gesamt += l;
            }
            if (dateien.Count <= HOECHSTENS_EINTRAEGE && gesamt <= grenzeGesamt) return false;
            fehler = ZapfSatz.Neu("KATALOGIMPORT_ZU_GROSS", dateien.Count, HOECHSTENS_EINTRAEGE, gesamt, grenzeGesamt);
            return true;
        }

        /// <summary>
        /// <b>Liest einen Archiveintrag, höchstens <paramref name="grenze"/> + 1 Byte</b> (Muster
        /// <c>IfcLeser.Entpacken</c>): Ergebnis ist der Text in UTF-8 (BOM erlaubt) oder <c>null</c>,
        /// wenn der Eintrag entpackt über die Grenze geht — dann hat das Zentralverzeichnis gelogen.
        /// <paramref name="gelesen"/> nennt die gelesenen Byte, bei einer Ablehnung also
        /// <paramref name="grenze"/> + 1 oder etwas darüber (der letzte Block wird ganz geschrieben).
        /// </summary>
        private static string EintragLesen(ZipArchiveEntry eintrag, long grenze, out long gelesen)
        {
            using (Stream quelle = eintrag.Open())
            using (var ziel = new MemoryStream())
            {
                var block = new byte[81920];
                int n;
                while ((n = quelle.Read(block, 0, block.Length)) > 0)
                {
                    ziel.Write(block, 0, n);
                    if (ziel.Length > grenze) { gelesen = ziel.Length; return null; }
                }
                gelesen = ziel.Length;
                ziel.Position = 0;
                using (var leser = new StreamReader(ziel, Encoding.UTF8, true))
                    return leser.ReadToEnd();
            }
        }

        /// <summary>
        /// Ist der Eintragsname des Archivs unbedenklich? Ein Unterordner ist erlaubt (ein ZIP aus
        /// einem Ordner trägt ihn; der Leser nimmt ohnehin nur den Dateinamen), ein Schritt nach
        /// oben (<c>..</c>) und ein absoluter Pfad (Wurzel oder Laufwerk) nicht: Ein solcher Name
        /// zeigt aus dem Archiv heraus und hat in einem Katalogpaket nichts zu suchen.
        ///
        /// <para>Ein <c>:</c> allein ist kein Grund — ein unter Unix gepacktes Paket darf es im
        /// Dateinamen tragen. Abgelehnt wird allein das <b>Laufwerksmuster</b> <c>^[A-Za-z]:</c> am
        /// Anfang eines Pfadteils.</para>
        /// </summary>
        private static bool Pfadsicher(string eintrag)
        {
            if (string.IsNullOrWhiteSpace(eintrag)) return false;
            if (eintrag[0] == '/' || eintrag[0] == '\\') return false;
            foreach (string teil in eintrag.Split('/', '\\'))
                if (teil == ".." || Laufwerksanfang(teil)) return false;
            return true;
        }

        /// <summary>Beginnt der Name mit einem Laufwerksbuchstaben (<c>^[A-Za-z]:</c>)?</summary>
        private static bool Laufwerksanfang(string teil)
            => teil.Length >= 2 && teil[1] == ':' && char.IsAsciiLetter(teil[0]);

        // =================================================================================
        // Importieren
        // =================================================================================

        /// <summary>
        /// <b>Spielt das Paket ein</b> (Klassenkommentar): Form prüfen, je Eintrag die Regeln,
        /// den Dublettenscan und das Anlegen, Ersetzen oder Überspringen in EINEM Vorgang. Der
        /// Bericht nennt jeden Eintrag mit Tabelle, Ausgang und Grund; ein Abbruch lässt den Katalog
        /// unverändert.
        ///
        /// <para><paramref name="pruefen"/> = <c>true</c> ist der <b>Prüflauf</b>: derselbe Bericht,
        /// aber der Vorgang wird zurückgerollt — es bleibt nichts geschrieben.</para>
        /// </summary>
        internal static TwwKatalogimportBericht Importieren(IReadOnlyList<TwwPaketdatei> dateien, bool pruefen = false)
        {
            var bericht = new TwwKatalogimportBericht { Pruefmodus = pruefen };
            if (!TabellenVorhanden() || !DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_TAGESGANG_STAMM))
            {
                bericht.Abbruch = ZapfSatz.Neu("KATALOGIMPORT_TABELLEN_FEHLEN");
                return bericht;
            }

            Paket paket;
            try
            {
                paket = PaketAus(dateien ?? new TwwPaketdatei[0], bericht);
            }
            catch (PaketFehler f)
            {
                bericht.Abbruch = f.Satz;
                return bericht;
            }
            if (bericht.Abbruch != null) return bericht;

            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    var satzZiel = new Dictionary<long, int>();
                    var zeilen = new List<TwwImportzeile>();
                    var ersetzteAuslieferung = new List<string>();
                    // Die Reihenfolge des Berichts (ZU32): Bedarfstage, Parameter, Nutzungsarten.
                    foreach (PaketBedarfstag b in paket.Bedarfstage)
                        zeilen.Add(BedarfstagEinspielen(v, b, paket, ersetzteAuslieferung));
                    int ersetzteParameter = 0;
                    foreach (PaketParameterzeile p in paket.Parameter)
                    {
                        TwwImportzeile z = ParameterEinspielen(v, p, ersetzteAuslieferung);
                        if (z.Ausgang == TwwImportausgang.Ersetzt) ersetzteParameter++;
                        zeilen.Add(z);
                    }
                    foreach (PaketNutzungsart p in paket.Nutzungsarten)
                        zeilen.Add(Einspielen(v, p, paket, satzZiel));

                    // Ein Prüflauf schreibt nichts: ohne Commit rollt der Vorgang zurück.
                    if (!pruefen) v.Commit();
                    bericht.Zeilen.AddRange(zeilen);
                    if (ersetzteParameter > 0)
                        bericht.Hinweise.Add(ZapfSatz.Neu("KATALOGIMPORT_PARAMETER_WIRKUNG", ersetzteParameter));
                    if (ersetzteAuslieferung.Count > 0)
                        bericht.Hinweise.Add(ZapfSatz.Neu("KATALOGIMPORT_AUSLIEFERUNG_ERSETZT",
                                                          ersetzteAuslieferung.Count, ersetzteAuslieferung.ToArray()));
                }
            }
            catch (Exception ex) when (ex is not LesemodusException)
            {
                bericht.Zeilen.Clear();
                bericht.Abbruch = ZapfSatz.Neu("KATALOGIMPORT_FEHLGESCHLAGEN", ex.Message);
            }
            return bericht;
        }

        /// <summary>Eine Nutzungsart des Pakets: Ablehnung, Dublettenscan und Anlegen im laufenden Vorgang.</summary>
        private static TwwImportzeile Einspielen(DbVorgang v, PaketNutzungsart p, Paket paket, Dictionary<long, int> satzZiel)
        {
            if (p.Fehler != null)
                return new TwwImportzeile(TwwImportausgang.Abgelehnt, p.Bezeichner, p.Katalogversion, "", 0, p.Zeile, p.Fehler);

            string bezeichner = p.Entwurf.Bezeichner.Trim();
            string version = p.Entwurf.Katalogversion.Trim();
            string name = bezeichner;
            ZapfSatz grund = null;

            int? ziel = NutzungsartId(v, bezeichner, version);
            if (ziel.HasValue)
            {
                if (StatusVon(v, TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, ziel.Value) == ZapfKatalogstatus.Auslieferung)
                    return new TwwImportzeile(TwwImportausgang.Uebersprungen, bezeichner, version, bezeichner, 0, p.Zeile,
                                              ZapfSatz.Neu("KATALOGIMPORT_AUSLIEFERUNG"));
                if (NutzungsartGleich(v, ziel.Value, p, paket.MitKategorien))
                    return new TwwImportzeile(TwwImportausgang.Uebersprungen, bezeichner, version, bezeichner, 0, p.Zeile,
                                              ZapfSatz.Neu("KATALOGIMPORT_GLEICH_VORHANDEN"));
                for (int n = 1; ; n++)
                {
                    string kandidat = bezeichner + ImportZusatz(n);
                    int? frueher = NutzungsartId(v, kandidat, version);
                    if (!frueher.HasValue) { name = kandidat; break; }
                    if (NutzungsartGleich(v, frueher.Value, p, paket.MitKategorien))
                        return new TwwImportzeile(TwwImportausgang.Uebersprungen, bezeichner, version, kandidat, 0, p.Zeile,
                                                  ZapfSatz.Neu("KATALOGIMPORT_GLEICH_ALS", kandidat));
                }
                grund = ZapfSatz.Neu("KATALOGIMPORT_NEUE_VERSION", name);
            }

            int satz = SatzEinspielen(v, p.Satz, satzZiel);
            TwwNutzungsartEntwurf e = p.Entwurf with { Bezeichner = name, Katalogversion = version, IdTagesgangsatz = satz };
            var werte = Fachwerte(e);
            werte.Add(new DbParam("@vorlage", null));
            werte.Add(new DbParam("@status", TwwWertemengen.Text(ZapfKatalogstatus.Import)));
            werte.Add(new DbParam("@beleg", null));
            int neu = v.EinfuegenUndId(SQL_INSERT, werte.ToArray());

            if (paket.MitKategorien)
                for (int i = 0; i < p.Kategorien.Count; i++)
                {
                    Zapfkategorie k = p.Kategorien[i];
                    Provenienz h = ImportHerkunft(k.Herkunft);
                    v.Ausfuehren(SQL_KATEGORIE_INSERT,
                        new DbParam("@n", neu), new DbParam("@k", k.Name.Trim()), new DbParam("@r", i + 1),
                        new DbParam("@v", k.VolumenstromLJeMin), new DbParam("@d", k.DauerMin), new DbParam("@a", k.Anteil),
                        new DbParam("@s", k.StreuungLJeMin), new DbParam("@kap", k.KappungLJeMin.HasValue ? (object)k.KappungLJeMin.Value : null),
                        new DbParam("@q", h.Quelle), new DbParam("@aus", h.Ausgabe), new DbParam("@ver", h.Version),
                        new DbParam("@art", TwwWertemengen.Text(h.Art)),
                        new DbParam("@st", TwwWertemengen.Text(ZapfKatalogstatus.Import)), new DbParam("@beleg", null));
                }
            return new TwwImportzeile(TwwImportausgang.Angelegt, bezeichner, version, name, neu, p.Zeile, grund);
        }

        /// <summary>
        /// Der Tagesgangsatz einer angelegten Nutzungsart: schon in diesem Import angelegt oder
        /// wiedergefunden — dessen Id; ein namensgleicher Satz gleichen Inhalts im Katalog — seine Id
        /// (auch eine Auslieferung, sie wird nur verwiesen, nie geändert); sonst ein neuer Satz (Status
        /// <c>IMPORT</c>) mit seinen vier Tagesgängen, bei abweichendem namensgleichem Satz als
        /// „(Import n)".
        /// </summary>
        private static int SatzEinspielen(DbVorgang v, PaketSatz s, Dictionary<long, int> satzZiel)
        {
            if (satzZiel.TryGetValue(s.Id, out int schon)) return schon;

            string name = s.Bezeichner.Trim();
            string version = s.Katalogversion.Trim();
            int? ziel = SatzId(v, name, version);
            if (ziel.HasValue)
            {
                if (SatzGleich(v, ziel.Value, s)) return satzZiel[s.Id] = ziel.Value;
                for (int n = 1; ; n++)
                {
                    string kandidat = name + ImportZusatz(n);
                    int? frueher = SatzId(v, kandidat, version);
                    if (!frueher.HasValue) { name = kandidat; break; }
                    if (SatzGleich(v, frueher.Value, s)) return satzZiel[s.Id] = frueher.Value;
                }
            }

            int neu = v.EinfuegenUndId(
                "INSERT INTO " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM +
                " (Bezeichner, Katalogversion, Status, Beleg, ReadOnly) VALUES (?, ?, ?, NULL, 0)",
                new[] { new DbParam("@b", name), new DbParam("@k", version),
                        new DbParam("@s", TwwWertemengen.Text(ZapfKatalogstatus.Import)) });
            for (int t = 0; t < Tagesgangsatz.TAGTYPEN; t++)
                TagesgangEinfuegen(v, neu, t + 1, s.Anteile[t], ImportHerkunft(s.Herkunft[t]));
            return satzZiel[s.Id] = neu;
        }

        /// <summary>
        /// Die Provenienz einer eingespielten Wertgruppe: Herkunftsart <c>IMPORT</c> mit Quelle, Ausgabe
        /// und Version des Pakets; <c>FREI</c> und <c>FIKTIV</c> bleiben, was sie sind.
        /// </summary>
        internal static Provenienz ImportHerkunft(Provenienz p)
            => p.Art == Herkunftsart.Frei || p.Art == Herkunftsart.Fiktiv ? p : p with { Art = Herkunftsart.Import };

        // =================================================================================
        // Dublettenscan
        // =================================================================================

        private static int? NutzungsartId(DbVorgang v, string bezeichner, string version)
            => IdOderNull(v.Skalar("SELECT ID FROM " + TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + " WHERE Bezeichner = ? AND Katalogversion = ?",
                                   new DbParam("@b", bezeichner), new DbParam("@k", version)));

        private static int? SatzId(DbVorgang v, string bezeichner, string version)
            => IdOderNull(v.Skalar("SELECT ID FROM " + TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM + " WHERE Bezeichner = ? AND Katalogversion = ?",
                                   new DbParam("@b", bezeichner), new DbParam("@k", version)));

        private static int? IdOderNull(object o) => o == null ? (int?)null : Convert.ToInt32(o, CultureInfo.InvariantCulture);

        private static ZapfKatalogstatus StatusVon(DbVorgang v, string tabelle, int id)
        {
            object s = v.Skalar("SELECT Status FROM " + tabelle + " WHERE ID = ?", new DbParam("@id", id));
            return TwwWertemengen.Status(Convert.ToString(s, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Trägt die Katalogzeile <paramref name="ziel"/> den Inhalt der Paketzeile? Die Wertgruppen ohne
        /// Provenienz (dieselbe Gruppenregel wie beim Ändern), der Tagesgangsatz über seine vier
        /// Tagesgänge und — wo die Tabelle da ist — die Kategorien in ihrer Reihenfolge.
        /// </summary>
        private static bool NutzungsartGleich(DbVorgang v, int ziel, PaketNutzungsart p, bool mitKategorien)
        {
            TwwNutzungsartEntwurf z = Bezugszeile(v, ziel);
            if (z == null) return false;
            ProvenienzNachfuehren(p.Entwurf with { Katalogversion = z.Katalogversion }, z, out bool geaendert);
            if (geaendert || !SatzGleich(v, z.IdTagesgangsatz, p.Satz)) return false;
            if (!mitKategorien) return true;

            List<(Zapfkategorie Kategorie, string Beleg)> alt = KategorienImVorgang(v, ziel);
            if (alt.Count != p.Kategorien.Count) return false;
            for (int i = 0; i < alt.Count; i++)
                if (!string.Equals(alt[i].Kategorie.Name, p.Kategorien[i].Name.Trim(), StringComparison.Ordinal)
                    || !WerteGleich(alt[i].Kategorie, p.Kategorien[i])) return false;
            return true;
        }

        /// <summary>Trägt der Katalogsatz <paramref name="ziel"/> dieselben vier Tagesgänge wie der Satz des Pakets?</summary>
        private static bool SatzGleich(DbVorgang v, int ziel, PaketSatz s)
        {
            DataTable dt = v.Lese("SELECT * FROM " + TwwSchema.TAB_TWW_TAGESGANG_STAMM + " WHERE ID_Tagesgangsatz = ?",
                                  new DbParam("@id", ziel));
            if (dt == null || dt.Rows.Count != Tagesgangsatz.TAGTYPEN) return false;
            foreach (DataRow r in dt.Rows)
            {
                int t = ZapfprofilCtrl.Ganz(r, "Tagtyp") - 1;
                if (t < 0 || t >= Tagesgangsatz.TAGTYPEN) return false;
                for (int h = 0; h < Tagesgangsatz.STUNDEN; h++)
                    if (!ZapfprofilCtrl.Zahl(r, ZapfprofilCtrl.AnteilSpalte(h + 1)).Equals(s.Anteile[t][h])) return false;
            }
            return true;
        }

        // =================================================================================
        // Die Form des Pakets
        // =================================================================================

        /// <summary>
        /// Das gelesene Paket: die Nutzungsarten, Bedarfstage und Parameter in Dateireihenfolge, ob
        /// Kategorien geschrieben werden und ob der Bedarfstag eine Spalte <c>Bezugsart</c> hat.
        /// </summary>
        private sealed class Paket
        {
            internal List<PaketNutzungsart> Nutzungsarten { get; } = new List<PaketNutzungsart>();
            internal List<PaketBedarfstag> Bedarfstage { get; } = new List<PaketBedarfstag>();
            internal List<PaketParameterzeile> Parameter { get; } = new List<PaketParameterzeile>();
            internal bool MitKategorien { get; set; }

            /// <summary>Führt <c>Tab_TwwBedarfstag_STAMM</c> die Spalte <c>Bezugsart</c> (Schritt 124)?</summary>
            internal bool MitBezugsart { get; set; }
        }

        /// <summary>Ein Tagesgangsatz des Pakets: Paket-Id, Name, die vier Tagesgänge samt Provenienz und — wenn er nicht taugt — der Grund.</summary>
        private sealed class PaketSatz
        {
            internal long Id { get; set; }
            internal string Bezeichner { get; set; } = "";
            internal string Katalogversion { get; set; } = "";
            internal double[][] Anteile { get; } = new double[Tagesgangsatz.TAGTYPEN][];
            internal Provenienz[] Herkunft { get; } = new Provenienz[Tagesgangsatz.TAGTYPEN];
            internal ZapfSatz Fehler { get; set; }
        }

        /// <summary>Eine Nutzungsart des Pakets als Entwurf samt Satz, Kategorien und — wenn sie nicht taugt — dem Grund.</summary>
        private sealed class PaketNutzungsart
        {
            internal long? Id { get; set; }
            internal int Zeile { get; set; }
            internal string Bezeichner { get; set; } = "";
            internal string Katalogversion { get; set; } = "";
            internal TwwNutzungsartEntwurf Entwurf { get; set; }
            internal PaketSatz Satz { get; set; }
            internal List<Zapfkategorie> Kategorien { get; set; } = new List<Zapfkategorie>();
            internal ZapfSatz Fehler { get; set; }
        }

        /// <summary>Eine Datei als Tabelle: Name, Kopf und je Datenzeile ihre Zeilennummer und Felder.</summary>
        private sealed class PaketTabelle
        {
            internal string Datei { get; set; } = "";
            internal string Tabelle { get; set; } = "";
            internal Dictionary<string, int> Spalten { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            internal List<(int Zeile, string[] Felder)> Zeilen { get; } = new List<(int, string[])>();

            internal bool Hat(string spalte) => Spalten.ContainsKey(spalte);

            /// <summary>Der Text des Feldes, beschnitten; leer, wenn die Spalte fehlt.</summary>
            internal string Text((int Zeile, string[] Felder) z, string spalte)
                => Spalten.TryGetValue(spalte, out int i) ? (z.Felder[i] ?? "").Trim() : "";

            internal double? Zahl((int Zeile, string[] Felder) z, string spalte)
            {
                string roh = Text(z, spalte);
                if (roh.Length == 0) return null;
                if (double.TryParse(roh, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) && !double.IsNaN(d)
                    && !double.IsInfinity(d)) return d;
                throw new PaketFehler(ZapfSatz.Neu("KATALOGIMPORT_ZAHL", Datei, z.Zeile, spalte, roh));
            }

            internal long? Ganz((int Zeile, string[] Felder) z, string spalte)
            {
                string roh = Text(z, spalte);
                if (roh.Length == 0) return null;
                if (long.TryParse(roh, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l)) return l;
                throw new PaketFehler(ZapfSatz.Neu("KATALOGIMPORT_GANZZAHL", Datei, z.Zeile, spalte, roh));
            }
        }

        /// <summary>Die benannte Ablehnung des Pakets als Ganzes.</summary>
        private sealed class PaketFehler : Exception
        {
            internal PaketFehler(ZapfSatz satz) : base(satz.Klartext) { Satz = satz; }
            internal ZapfSatz Satz { get; }
        }

        /// <summary>
        /// Liest die Dateien in das Paket: Zuordnung der Dateien zu Tabellen (übergangene benannt),
        /// Form jeder Tabelle, die Sätze mit ihren Tagesgängen, die Nutzungsarten mit Satz und
        /// Kategorien. Ein Formfehler wirft <see cref="PaketFehler"/>.
        /// </summary>
        private static Paket PaketAus(IReadOnlyList<TwwPaketdatei> dateien, TwwKatalogimportBericht bericht)
        {
            var tabellen = new Dictionary<string, PaketTabelle>(StringComparer.OrdinalIgnoreCase);
            bool kategorienDa = DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM);
            bool bedarfstageDa = DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_BEDARFSTAG_STAMM)
                                 && DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM);
            bool parameterDa = DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_PARAMETER_STAMM);
            bool bedarfstageGemeldet = false;
            foreach (TwwPaketdatei d in dateien)
            {
                string tabelle = IMPORT_TABELLEN.FirstOrDefault(t => string.Equals(t + ".csv", d.Name, StringComparison.OrdinalIgnoreCase));
                if (tabelle == null)
                {
                    bericht.Hinweise.Add(ZapfSatz.Neu("KATALOGIMPORT_DATEI_UEBERGANGEN", d.Name));
                    continue;
                }
                if (tabelle == TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM && !kategorienDa)
                {
                    bericht.Hinweise.Add(ZapfSatz.Neu("KATALOGIMPORT_KATEGORIEN_OHNE_TABELLE"));
                    continue;
                }
                if ((tabelle == TwwSchema.TAB_TWW_BEDARFSTAG_STAMM || tabelle == TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM)
                    && !bedarfstageDa)
                {
                    // EIN Hinweis, auch wenn beide Dateien liegen - es fehlt dieselbe Stufe des Schemas.
                    if (!bedarfstageGemeldet) bericht.Hinweise.Add(ZapfSatz.Neu("KATALOGIMPORT_BEDARFSTAGE_OHNE_TABELLE"));
                    bedarfstageGemeldet = true;
                    continue;
                }
                if (tabelle == TwwSchema.TAB_TWW_PARAMETER_STAMM && !parameterDa)
                {
                    bericht.Hinweise.Add(ZapfSatz.Neu("KATALOGIMPORT_PARAMETER_OHNE_TABELLE"));
                    continue;
                }
                if (tabellen.ContainsKey(tabelle)) throw new PaketFehler(ZapfSatz.Neu("KATALOGIMPORT_DATEI_DOPPELT", d.Name));
                tabellen[tabelle] = Tabelle(d, tabelle);
            }
            if (!tabellen.TryGetValue(TwwSchema.TAB_TWW_NUTZUNGSART_STAMM, out PaketTabelle tn))
                throw new PaketFehler(ZapfSatz.Neu("KATALOGIMPORT_KEINE_DATEI", TwwSchema.TAB_TWW_NUTZUNGSART_STAMM + ".csv"));

            var paket = new Paket
            {
                MitKategorien = kategorienDa,
                MitBezugsart = bedarfstageDa
                               && DataRepository.SpalteVorhanden(TwwSchema.TAB_TWW_BEDARFSTAG_STAMM, TwwSchema.SPALTE_BEZUGSART)
            };
            paket.Bedarfstage.AddRange(Bedarfstage(tabellen, paket, bericht));
            paket.Parameter.AddRange(Parameterzeilen(tabellen, bericht));
            Dictionary<long, PaketSatz> saetze = Saetze(tabellen, bericht);
            List<PaketKategorie> kategorien =
                tabellen.TryGetValue(TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM, out PaketTabelle tk) ? Kategorien(tk) : new List<PaketKategorie>();

            Pflicht(tn, "Bezeichner", "Katalogversion", "Bezugsart", "Bedarf_Niedrig", "Bedarf_Mittel", "Bedarf_Hoch",
                    "Bedarf_Quelle", "Bedarf_Version", "Bedarf_Herkunftsart", "Bezug_Zapftemperatur", "Bezug_Kaltwasser",
                    "Bilanzgrenze", "Kalenderart", "Jahresgang_Quelle", "Jahresgang_Version", "Jahresgang_Herkunftsart",
                    "Wochengang_Quelle", "Wochengang_Version", "Wochengang_Herkunftsart", "ID_Tagesgangsatz");
            Pflicht(tn, Enumerable.Range(1, NutzungsartRaster.MONATE).Select(m => "Monat_" + m.ToString(CultureInfo.InvariantCulture)).ToArray());
            Pflicht(tn, Enumerable.Range(1, NutzungsartRaster.WOCHENTAGE).Select(w => "Woche_" + w.ToString(CultureInfo.InvariantCulture)).ToArray());

            var ids = new HashSet<long>();
            var schluessel = new HashSet<string>(StringComparer.Ordinal);
            foreach (var z in tn.Zeilen)
            {
                long? id = tn.Ganz(z, "ID");
                if (id.HasValue && !ids.Add(id.Value))
                    throw new PaketFehler(ZapfSatz.Neu("KATALOGIMPORT_ID_DOPPELT", tn.Datei, z.Zeile, id.Value));
                PaketNutzungsart p = PaketNutzungsartAus(tn, z, saetze);
                p.Id = id;
                if (p.Fehler == null && !schluessel.Add(p.Bezeichner + "\u0001" + p.Katalogversion))
                    p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_DOPPELT_IM_PAKET");
                paket.Nutzungsarten.Add(p);
            }

            // Die Kategorien: eigene je Nutzungsart, sonst der Vorgabesatz OHNE ID_Nutzungsart —
            // und zwar der seiner GRUPPE. Der freie Paketteil fuehrt seit Z5 zwei Vorgabesaetze
            // (Wohnen, Nichtwohnen) und trennt sie allein durch die Steuerspalte „Gruppe"
            // (<see cref="TwwSchema.STEUERSPALTE_GRUPPE"/>). Beide zusammen an EINE Nutzungsart zu
            // binden ergaebe zwei Kategorien desselben Namens und damit KATEGORIE_NAME_DOPPELT fuer
            // jede Zeile des Pakets. Welche Gruppe eine Nutzungsart traegt, sagt
            // TwwSchema.Kategoriengruppe aus ihrer Kalenderart — DIESELBE Regel wie im Katalog und
            // in der Auslieferungsvorlage.
            List<PaketKategorie> eigene = kategorien.Where(k => k.Id.HasValue).ToList();
            List<PaketKategorie> vorgabe = kategorien.Where(k => !k.Id.HasValue).OrderBy(k => k.Reihenfolge).ToList();
            // Ein Paket ohne Steuerspalte fuehrt EINEN Vorgabesatz unter dem leeren Schluessel; er
            // gilt dann fuer jede Gruppe — so bleibt ein Paket aus der Zeit vor Z5 lesbar.
            Dictionary<string, List<PaketKategorie>> vorgabeJeGruppe = vorgabe
                .GroupBy(k => k.Gruppe, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
            int ohneZiel = eigene.Count(k => !paket.Nutzungsarten.Any(p => p.Id == k.Id));
            if (ohneZiel > 0) bericht.Hinweise.Add(ZapfSatz.Neu("KATALOGIMPORT_KATEGORIEN_UEBERGANGEN", ohneZiel));
            int ohneVorgabe = 0;
            foreach (PaketNutzungsart p in paket.Nutzungsarten)
            {
                List<PaketKategorie> satz = eigene.Where(x => p.Id.HasValue && x.Id == p.Id).OrderBy(x => x.Reihenfolge).ToList();
                // Die Gruppe wird NICHT geraten: Welche eine Zeile traegt, sagt allein ihre gelesene
                // Kalenderart. Eine Zeile, die schon einen Grund traegt, ist ohnehin abgelehnt - fuer
                // sie wird nichts angenommen.
                if (satz.Count == 0 && p.Fehler == null)
                {
                    if (p.Entwurf == null) p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_PFLICHT_FEHLT", "Kalenderart");
                    else if (vorgabeJeGruppe.Count > 0)
                    {
                        string gruppe = TwwSchema.Kategoriengruppe((long)p.Entwurf.Kalender);
                        if (vorgabeJeGruppe.TryGetValue(gruppe, out List<PaketKategorie> jeGruppe)) satz = jeGruppe;
                        else if (vorgabeJeGruppe.TryGetValue("", out List<PaketKategorie> ohneGruppe)) satz = ohneGruppe;
                        else p.Fehler = ZapfSatz.Neu("KATALOGIMPORT_VORGABESATZ_GRUPPE", gruppe);
                    }
                    // Ein Paket OHNE jeden Vorgabesatz laesst die Nutzungsart kategorienlos - sie
                    // rechnet dann ohne Streuung. Das wird benannt, nie still; fehlt die ganze
                    // Tabelle, nennt es KATALOGIMPORT_KATEGORIEN_OHNE_TABELLE schon.
                    else if (kategorienDa) ohneVorgabe++;
                }
                p.Kategorien = satz.Select(x => x.Kategorie).ToList();
                if (p.Fehler != null || satz.Count == 0) continue;
                p.Fehler = satz.Select(x => x.Fehler).FirstOrDefault(f => f != null) ?? Zapfkategoriensatz.Pruefen(p.Kategorien);
            }
            if (ohneVorgabe > 0) bericht.Hinweise.Add(ZapfSatz.Neu("KATALOGIMPORT_OHNE_VORGABESATZ", ohneVorgabe));
            return paket;
        }

        /// <summary>Eine Datei als Tabelle: Trenner aus der Kopfzeile, jede Spalte bekannt, jede Zeile mit der Feldzahl des Kopfes.</summary>
        private static PaketTabelle Tabelle(TwwPaketdatei d, string tabelle)
        {
            var t = new PaketTabelle { Datei = d.Name, Tabelle = tabelle };
            string text = d.Inhalt ?? "";
            if (text.Length > 0 && text[0] == '﻿') text = text.Substring(1);
            // Ein Paket aus Windows trägt CRLF, eines aus Linux, macOS oder iOS LF, ein älteres Excel für
            // Mac CR. Der CSV-Leser trennt Sätze bei allen dreien; die Kopfzeile für die Trennerwahl und
            // ein Zeilenumbruch IN einem Feld in Anführungszeichen hingen aber an der Herkunft.
            text = text.Replace("\r\n", "\n").Replace('\r', '\n');
            int ende = text.IndexOf('\n');
            string kopfzeile = ende < 0 ? text : text.Substring(0, ende);
            string trenner = kopfzeile.IndexOf(';') >= 0 ? ";" : ",";

            HashSet<string> bekannt = new HashSet<string>(DataRepository.SpaltenVonTabelle(tabelle), StringComparer.OrdinalIgnoreCase);
            // Die Steuerspalte „Gruppe" des freien Paketteils (Vorgabesatz je Nutzungsartengruppe,
            // Stufe Z5) ist keine Spalte der Tabelle: Ein Katalogimport derselben Datei liest sie mit,
            // übernimmt sie aber nicht — die Kategorien eines Imports hängen an ihrer Nutzungsart.
            if (tabelle == TwwSchema.TAB_TWW_ZAPFKATEGORIE_STAMM) bekannt.Add(TwwSchema.STEUERSPALTE_GRUPPE);
            // Die Spalte „Bezugsart" des Bedarfstags kommt erst mit Schemaschritt 124 (T3). Ein Paket,
            // das sie führt, soll an einer älteren Datenbank nicht als Ganzes fallen: Die Spalte gilt
            // als bekannt, der Wert bleibt liegen — und das sagt ein Hinweis des Berichts.
            if (tabelle == TwwSchema.TAB_TWW_BEDARFSTAG_STAMM) bekannt.Add(TwwSchema.SPALTE_BEZUGSART);
            // NReco setzt KEINE Vorgabe fuer BufferSize (ohne sie teilt der Leser durch null); die
            // Groesse begrenzt die Laenge EINES Satzes - 64 kB wie der Ganglinienleser.
            var csv = new CsvReader(new StringReader(text), trenner) { BufferSize = 65536, TrimFields = true };
            bool kopf = true;
            while (csv.Read())
            {
                var felder = new string[csv.FieldsCount];
                for (int i = 0; i < felder.Length; i++) felder[i] = csv[i];
                int zeile = csv.ReadLinesCount;
                if (kopf)
                {
                    kopf = false;
                    for (int i = 0; i < felder.Length; i++)
                    {
                        string s = (felder[i] ?? "").Trim();
                        if (!bekannt.Contains(s)) throw new PaketFehler(ZapfSatz.Neu("KATALOGIMPORT_SPALTE_UNBEKANNT", d.Name, s, tabelle));
                        if (t.Spalten.ContainsKey(s)) throw new PaketFehler(ZapfSatz.Neu("KATALOGIMPORT_SPALTE_DOPPELT", d.Name, s));
                        t.Spalten[s] = i;
                    }
                    continue;
                }
                if (felder.Length != t.Spalten.Count)
                    throw new PaketFehler(ZapfSatz.Neu("KATALOGIMPORT_FELDZAHL", d.Name, zeile, felder.Length, t.Spalten.Count));
                t.Zeilen.Add((zeile, felder));
            }
            return t;
        }

        /// <summary>Jede genannte Spalte steht im Kopf der Tabelle — sonst ist das Paket abgelehnt.</summary>
        private static void Pflicht(PaketTabelle t, params string[] spalten)
        {
            foreach (string s in spalten)
                if (!t.Hat(s)) throw new PaketFehler(ZapfSatz.Neu("KATALOGIMPORT_SPALTE_FEHLT", t.Datei, s));
        }

        /// <summary>Die Tagesgangsätze des Pakets samt ihren Tagesgängen; ein Tagesgang ohne Satz ist benannt übergangen.</summary>
        private static Dictionary<long, PaketSatz> Saetze(Dictionary<string, PaketTabelle> tabellen, TwwKatalogimportBericht bericht)
        {
            var saetze = new Dictionary<long, PaketSatz>();
            if (!tabellen.TryGetValue(TwwSchema.TAB_TWW_TAGESGANGSATZ_STAMM, out PaketTabelle ts)) return saetze;
            Pflicht(ts, "ID", "Bezeichner", "Katalogversion");
            foreach (var z in ts.Zeilen)
            {
                long id = ts.Ganz(z, "ID") ?? throw new PaketFehler(ZapfSatz.Neu("KATALOGIMPORT_ID_FEHLT", ts.Datei, z.Zeile, "ID"));
                if (saetze.ContainsKey(id)) throw new PaketFehler(ZapfSatz.Neu("KATALOGIMPORT_ID_DOPPELT", ts.Datei, z.Zeile, id));
                var s = new PaketSatz { Id = id, Bezeichner = ts.Text(z, "Bezeichner"), Katalogversion = ts.Text(z, "Katalogversion") };
                if (s.Bezeichner.Length == 0) s.Fehler = ZapfSatz.Neu("KATALOGIMPORT_SATZ_PFLICHT", "#" + id.ToString(CultureInfo.InvariantCulture), "Bezeichner");
                else if (s.Katalogversion.Length == 0) s.Fehler = ZapfSatz.Neu("KATALOGIMPORT_SATZ_PFLICHT", s.Bezeichner, "Katalogversion");
                saetze[id] = s;
            }

            if (!tabellen.TryGetValue(TwwSchema.TAB_TWW_TAGESGANG_STAMM, out PaketTabelle tg)) tg = null;
            if (tg != null)
            {
                Pflicht(tg, "ID_Tagesgangsatz", "Tagtyp", "Quelle", "Version", "Herkunftsart");
                Pflicht(tg, Enumerable.Range(1, Tagesgangsatz.STUNDEN).Select(ZapfprofilCtrl.AnteilSpalte).ToArray());
                int ohneSatz = 0;
                foreach (var z in tg.Zeilen)
                {
                    long satzId = tg.Ganz(z, "ID_Tagesgangsatz")
                                  ?? throw new PaketFehler(ZapfSatz.Neu("KATALOGIMPORT_ID_FEHLT", tg.Datei, z.Zeile, "ID_Tagesgangsatz"));
                    if (!saetze.TryGetValue(satzId, out PaketSatz s)) { ohneSatz++; continue; }
                    long tagtyp = tg.Ganz(z, "Tagtyp") ?? 0;
                    if (tagtyp < 1 || tagtyp > Tagesgangsatz.TAGTYPEN)
                    {
                        s.Fehler ??= ZapfSatz.Neu("KATALOGIMPORT_WERT_UNGUELTIG", "Tagtyp", tg.Text(z, "Tagtyp"));
                        continue;
                    }
                    int t = (int)tagtyp - 1;
                    if (s.Anteile[t] != null)
                        throw new PaketFehler(ZapfSatz.Neu("KATALOGIMPORT_TAGTYP_DOPPELT", tg.Datei, z.Zeile, (int)tagtyp, satzId));
                    var anteile = new double[Tagesgangsatz.STUNDEN];
                    bool leer = false;
                    for (int h = 0; h < Tagesgangsatz.STUNDEN; h++)
                    {
                        double? a = tg.Zahl(z, ZapfprofilCtrl.AnteilSpalte(h + 1));
                        if (!a.HasValue) leer = true;
                        anteile[h] = a ?? 0.0;
                    }
                    s.Anteile[t] = anteile;
                    Provenienz herkunft = Herkunft(tg, z, "", out ZapfSatz fehler);
                    s.Herkunft[t] = herkunft;
                    if (s.Fehler == null && fehler != null) s.Fehler = ZapfSatz.Neu("KATALOGIMPORT_SATZ_ANGABE", s.Bezeichner, fehler);
                    if (s.Fehler == null && (leer || !SummeEins(anteile, Tagesgangsatz.STUNDEN)))
                        s.Fehler = ZapfSatz.Neu("KATALOGIMPORT_TAGESGANG", s.Bezeichner, (int)tagtyp);
                }
                if (ohneSatz > 0) bericht.Hinweise.Add(ZapfSatz.Neu("KATALOGIMPORT_TAGESGAENGE_UEBERGANGEN", ohneSatz));
            }
            foreach (PaketSatz s in saetze.Values)
                if (s.Fehler == null && s.Anteile.Any(a => a == null))
                    s.Fehler = ZapfSatz.Neu("KATALOGIMPORT_SATZ_UNVOLLSTAENDIG", s.Bezeichner);
            return saetze;
        }

        /// <summary>
        /// Die Kategorien des Pakets: Paket-Id der Nutzungsart (leer = Vorgabesatz), Kategorie,
        /// Reihenfolge und — fehlt ihr eine Angabe der Provenienz — der Grund. Werte prüft
        /// <see cref="Zapfkategoriensatz.Pruefen"/> je Satz.
        /// </summary>
        private static List<PaketKategorie> Kategorien(PaketTabelle tk)
        {
            Pflicht(tk, "Kategorie", "Reihenfolge", "Volumenstrom_l_min", "Dauer_min", "Anteil", "Sigma", "Quelle", "Version", "Herkunftsart");
            var liste = new List<PaketKategorie>();
            foreach (var z in tk.Zeilen)
            {
                string name = tk.Text(z, "Kategorie");
                Provenienz h = Herkunft(tk, z, "", out ZapfSatz fehler);
                long dauer = tk.Ganz(z, "Dauer_min") ?? 0;
                var k = new Zapfkategorie(0, name, tk.Zahl(z, "Volumenstrom_l_min") ?? double.NaN,
                                          tk.Zahl(z, "Sigma") ?? double.NaN, dauer > int.MaxValue || dauer < 1 ? 0 : (int)dauer,
                                          tk.Zahl(z, "Anteil") ?? double.NaN, h)
                {
                    KappungLJeMin = tk.Zahl(z, "Kappung_l_min")
                };
                long reihenfolge = tk.Ganz(z, "Reihenfolge") ?? 0;
                liste.Add(new PaketKategorie
                {
                    Id = tk.Hat("ID_Nutzungsart") ? tk.Ganz(z, "ID_Nutzungsart") : null,
                    Gruppe = tk.Text(z, TwwSchema.STEUERSPALTE_GRUPPE),
                    Kategorie = k,
                    Reihenfolge = (int)Math.Min(int.MaxValue, Math.Max(int.MinValue, reihenfolge)),
                    Fehler = fehler == null ? null : ZapfSatz.Neu("KATALOGIMPORT_KATEGORIE_ANGABE", name, fehler)
                });
            }
            return liste;
        }

        /// <summary>Eine Kategorie des Pakets samt Bindung, Reihenfolge und Grund.</summary>
        private sealed class PaketKategorie
        {
            internal long? Id { get; set; }

            /// <summary>
            /// Die Nutzungsartengruppe des Vorgabesatzes aus der Steuerspalte
            /// <see cref="TwwSchema.STEUERSPALTE_GRUPPE"/> (keine Tabellenspalte); leer, wenn das
            /// Paket sie nicht fuehrt oder die Zeile an eine Nutzungsart gebunden ist.
            /// </summary>
            internal string Gruppe { get; set; } = "";

            internal Zapfkategorie Kategorie { get; set; }
            internal int Reihenfolge { get; set; }
            internal ZapfSatz Fehler { get; set; }
        }

        /// <summary>
        /// Eine Nutzungsart aus ihrer Zeile: der Entwurf mit der Provenienz des Pakets (die Herkunftsart
        /// setzt erst <see cref="ImportHerkunft"/>) und der erste Regelverstoß als Grund.
        /// </summary>
        private static PaketNutzungsart PaketNutzungsartAus(PaketTabelle t, (int Zeile, string[] Felder) z, Dictionary<long, PaketSatz> saetze)
        {
            var p = new PaketNutzungsart { Zeile = z.Zeile, Bezeichner = t.Text(z, "Bezeichner"), Katalogversion = t.Text(z, "Katalogversion") };
            ZapfSatz fehler = null;
            void Fehlt(string spalte) { fehler ??= ZapfSatz.Neu("KATALOGIMPORT_PFLICHT_FEHLT", spalte); }
            double Wert(string spalte) { double? w = t.Zahl(z, spalte); if (!w.HasValue) Fehlt(spalte); return w ?? 0.0; }
            int Stufe(string spalte, Type art)
            {
                long? w = t.Ganz(z, spalte);
                if (!w.HasValue) { Fehlt(spalte); return 0; }
                if (w.Value < int.MinValue || w.Value > int.MaxValue || !Enum.IsDefined(art, (int)w.Value))
                    fehler ??= ZapfSatz.Neu("KATALOGIMPORT_WERT_UNGUELTIG", spalte, t.Text(z, spalte));
                return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, w.Value));
            }

            if (p.Bezeichner.Length == 0) Fehlt("Bezeichner");
            if (p.Katalogversion.Length == 0) Fehlt("Katalogversion");
            int bezug = Stufe("Bezugsart", typeof(ZapfBezugsart));
            var bedarf = new double[NutzungsartRaster.NIVEAUS];
            var min = new double?[NutzungsartRaster.NIVEAUS];
            var max = new double?[NutzungsartRaster.NIVEAUS];
            for (int n = 0; n < NutzungsartRaster.NIVEAUS; n++)
            {
                string spalte = NutzungsartRaster.Niveauspalten[n];
                bedarf[n] = Wert(spalte);
                min[n] = t.Zahl(z, spalte + "_Min");
                max[n] = t.Zahl(z, spalte + "_Max");
            }
            Provenienz hb = Herkunft(t, z, "Bedarf_", out ZapfSatz fb);
            double zapf = Wert("Bezug_Zapftemperatur");
            double kalt = Wert("Bezug_Kaltwasser");
            int grenze = Stufe("Bilanzgrenze", typeof(ZapfBilanzgrenze));
            int kalender = Stufe("Kalenderart", typeof(ZapfKalenderart));
            double? ferien = t.Zahl(z, "Ferienfaktor");
            var monate = new double[NutzungsartRaster.MONATE];
            for (int m = 0; m < monate.Length; m++) monate[m] = Wert("Monat_" + (m + 1).ToString(CultureInfo.InvariantCulture));
            Provenienz hj = Herkunft(t, z, "Jahresgang_", out ZapfSatz fj);
            var woche = new double[NutzungsartRaster.WOCHENTAGE];
            for (int w = 0; w < woche.Length; w++) woche[w] = Wert("Woche_" + (w + 1).ToString(CultureInfo.InvariantCulture));
            Provenienz hw = Herkunft(t, z, "Wochengang_", out ZapfSatz fw);
            fehler ??= fb ?? fj ?? fw;

            long? satzId = t.Ganz(z, "ID_Tagesgangsatz");
            if (!satzId.HasValue) Fehlt("ID_Tagesgangsatz");
            else if (!saetze.TryGetValue(satzId.Value, out PaketSatz satz)) fehler ??= ZapfSatz.Neu("KATALOGIMPORT_SATZ_FEHLT", satzId.Value);
            else
            {
                p.Satz = satz;
                fehler ??= satz.Fehler;
            }

            p.Entwurf = new TwwNutzungsartEntwurf
            {
                Bezeichner = p.Bezeichner,
                Katalogversion = p.Katalogversion,
                Bezug = (ZapfBezugsart)bezug,
                Bedarf = bedarf,
                BedarfMin = min,
                BedarfMax = max,
                BedarfHerkunft = hb == null ? null : ImportHerkunft(hb),
                Bezugstemperaturen = new Temperaturbezug(zapf, kalt),
                Grenze = (ZapfBilanzgrenze)grenze,
                Kalender = (ZapfKalenderart)kalender,
                Ferienfaktor = ferien,
                Monatsfaktoren = monate,
                JahresgangHerkunft = hj == null ? null : ImportHerkunft(hj),
                Wochenfaktoren = woche,
                WochengangHerkunft = hw == null ? null : ImportHerkunft(hw)
            };
            if (fehler == null && !RasterGueltig(p.Entwurf)) fehler = ZapfSatz.Neu("KATALOGIMPORT_RASTER");
            p.Fehler = fehler;
            return p;
        }

        /// <summary>
        /// Die Provenienz einer Wertgruppe (<paramref name="praefix"/> + Quelle, Ausgabe, Version,
        /// Herkunftsart); fehlt eine Pflichtangabe oder ist die Herkunftsart fremd, der Grund in
        /// <paramref name="fehler"/> und <c>null</c>.
        /// </summary>
        private static Provenienz Herkunft(PaketTabelle t, (int Zeile, string[] Felder) z, string praefix, out ZapfSatz fehler)
        {
            fehler = null;
            string quelle = t.Text(z, praefix + "Quelle");
            string ausgabe = t.Text(z, praefix + "Ausgabe");
            string version = t.Text(z, praefix + "Version");
            string art = t.Text(z, praefix + "Herkunftsart");
            if (quelle.Length == 0) { fehler = ZapfSatz.Neu("KATALOGIMPORT_PFLICHT_FEHLT", praefix + "Quelle"); return null; }
            if (version.Length == 0) { fehler = ZapfSatz.Neu("KATALOGIMPORT_PFLICHT_FEHLT", praefix + "Version"); return null; }
            Herkunftsart h;
            try { h = TwwWertemengen.Herkunft(art); }
            catch (ArgumentException)
            {
                fehler = ZapfSatz.Neu("KATALOGIMPORT_WERT_UNGUELTIG", praefix + "Herkunftsart", art);
                return null;
            }
            return new Provenienz(quelle, ausgabe.Length == 0 ? null : ausgabe, version, h);
        }
    }
}
