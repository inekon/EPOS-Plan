using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Kopf eines <c>.wpx</c>-Pakets — gelesen OHNE die Datenbank</b>
    /// (Auftrag PI-1).
    ///
    /// <para>Er löst den handgeschriebenen JSON-Zerleger der Windows-Hülle ab: Das
    /// Manifest ist eine Sache des Paketformats, und das Paketformat gehört dem Kern.
    /// Eine zweite Lesart wäre eine zweite Wahrheit darüber, was „Schemastand" heißt
    /// — und sie stünde in der Schale, die iOS nicht hat.</para>
    /// </summary>
    /// <param name="Datei">Der Pfad, unter dem das Paket gelesen wurde.</param>
    /// <param name="Quellprojekt">Das Hauptprojekt des Pakets (<c>sourceProject</c>).</param>
    /// <param name="Exportdatum">Ausgabezeitpunkt als Text der Programmsprache; leer, wenn unlesbar.</param>
    /// <param name="Schemastand">Der Migrationsstand (<c>schemaVersion</c>); 0 = Altpaket.</param>
    /// <param name="Formatversion">Die Paketformatfassung (<c>formatVersion</c>).</param>
    /// <param name="Varianten">Die mitgereisten Variantenprojekte, in Paketreihenfolge.</param>
    /// <param name="StammQuelle">
    /// Ist das Hauptprojekt selbst eine Variante, steht hier der Name SEINES
    /// Stammprojekts (aus <c>variantLinks</c>); sonst leer. Daran hängt die
    /// Importreihenfolge: Ein Variantenpaket gehört HINTER das Paket seines Stamms.
    /// </param>
    /// <param name="Fehler">Leer, wenn das Paket lesbar ist; sonst der Grund.</param>
    public sealed record Paketkopf(
        string Datei,
        string Quellprojekt,
        string Exportdatum,
        int Schemastand,
        int Formatversion,
        IReadOnlyList<string> Varianten,
        string StammQuelle,
        string Fehler);

    /// <summary>Was mit einem Paket eines Sammellaufs geschehen ist.</summary>
    public enum Paketbefund
    {
        /// <summary>Eingespielt.</summary>
        Importiert = 0,

        /// <summary>Eingespielt; das Hauptprojekt bekam einen neuen Namen.</summary>
        Umbenannt,

        /// <summary>Eingespielt; sein Stammprojekt stand schon aus einem früheren Paket.</summary>
        StammUebersprungen,

        /// <summary>Gar nicht erst gelesen — kein gültiges Paket.</summary>
        Unlesbar,

        /// <summary>Abgelehnt: fremder Schemastand.</summary>
        Schemastand,

        /// <summary>Der Import ist gescheitert; die Datenbank ist unverändert.</summary>
        Fehler,

        /// <summary>Der Anwender hat den Lauf vor diesem Paket abgebrochen.</summary>
        Abgebrochen
    }

    /// <summary>Eine Zeile der Exportbilanz — eine Stammgruppe, eine Paketdatei.</summary>
    public sealed class ExportZeile
    {
        public ExportZeile(string gruppe, string datei, int projekte, bool erfolg, string grund)
        {
            Gruppe = gruppe ?? "";
            Datei = datei ?? "";
            Projekte = projekte;
            Erfolg = erfolg;
            Grund = grund ?? "";
        }

        /// <summary>Der Name des Stammprojekts — zugleich der Name der Gruppe.</summary>
        public string Gruppe { get; }

        /// <summary>Die geschriebene Datei (voller Pfad); leer, wenn nichts entstand.</summary>
        public string Datei { get; }

        /// <summary>Wie viele Projekte reisen darin? Stamm plus Varianten.</summary>
        public int Projekte { get; }

        public bool Erfolg { get; }

        /// <summary>Leer bei Erfolg; sonst der benannte Grund.</summary>
        public string Grund { get; }
    }

    /// <summary>Die Zahlen eines Sammelexports.</summary>
    public sealed class ExportBilanz
    {
        private readonly List<ExportZeile> _zeilen = new List<ExportZeile>();

        /// <summary>Wie viele Gruppen waren zu schreiben?</summary>
        public int Pakete { get; set; }

        /// <summary>Wie viele Dateien sind entstanden?</summary>
        public int Geschrieben { get; set; }

        /// <summary>Wie viele Gruppen sind gescheitert?</summary>
        public int Fehler { get; set; }

        /// <summary>Wurde der Lauf abgebrochen?</summary>
        public bool Abgebrochen { get; set; }

        /// <summary>Eine Zeile je Gruppe, in Laufreihenfolge.</summary>
        public IReadOnlyList<ExportZeile> Zeilen => _zeilen;

        internal void Anfuegen(ExportZeile zeile) => _zeilen.Add(zeile);
    }

    /// <summary>Eine Zeile der Importbilanz — ein Paket.</summary>
    public sealed class SammelImportZeile
    {
        public SammelImportZeile(string datei, string quellprojekt, string zielname,
                                 Paketbefund befund, string grund, IReadOnlyList<string> bericht)
        {
            Datei = datei ?? "";
            Quellprojekt = quellprojekt ?? "";
            Zielname = zielname ?? "";
            Befund = befund;
            Grund = grund ?? "";
            Bericht = bericht ?? Array.Empty<string>();
        }

        public string Datei { get; }

        /// <summary>Das Hauptprojekt, wie es im Paket heißt.</summary>
        public string Quellprojekt { get; }

        /// <summary>Der Name, unter dem es am Ziel steht; leer, wo nichts entstand.</summary>
        public string Zielname { get; }

        public Paketbefund Befund { get; }

        /// <summary>Leer, wo es nichts zu sagen gibt; sonst der benannte Grund.</summary>
        public string Grund { get; }

        /// <summary>Der Zeilenbericht dieses Pakets (<c>LetzterBericht</c>).</summary>
        public IReadOnlyList<string> Bericht { get; }
    }

    /// <summary>Die Zahlen eines Sammelimports.</summary>
    public sealed class SammelImportBilanz
    {
        private readonly List<SammelImportZeile> _zeilen = new List<SammelImportZeile>();

        /// <summary>Wie viele Dateien wurden übergeben?</summary>
        public int Pakete { get; set; }

        /// <summary>Wie viele sind eingespielt worden?</summary>
        public int Importiert { get; set; }

        /// <summary>Bei wie vielen stand das Stammprojekt schon aus einem früheren Paket?</summary>
        public int Uebersprungen { get; set; }

        /// <summary>Wie viele Hauptprojekte bekamen einen neuen Namen?</summary>
        public int Umbenannt { get; set; }

        /// <summary>Wie viele sind gescheitert oder abgelehnt worden?</summary>
        public int Fehler { get; set; }

        /// <summary>Wurde der Lauf zwischen zwei Paketen abgebrochen?</summary>
        public bool Abgebrochen { get; set; }

        /// <summary>Eine Zeile je Paket, in Laufreihenfolge.</summary>
        public IReadOnlyList<SammelImportZeile> Zeilen => _zeilen;

        /// <summary>Hat der Lauf überhaupt etwas geschrieben?</summary>
        public bool EtwasGeschrieben => Importiert > 0;

        internal void Anfuegen(SammelImportZeile zeile) => _zeilen.Add(zeile);
    }

    /// <summary>
    /// <b>Das Gedächtnis eines Importlaufs über Paketgrenzen</b> (Auftrag PI-1).
    ///
    /// <para>Ohne ihn ist jedes Paket eine Insel: Die Namensabbildung
    /// <c>Quellname → neue Id</c> entsteht und vergeht je Aufruf. Genau daran
    /// scheiterte der Fall, um den es dem Anwender geht — eine Variante in Paket 2,
    /// deren Stamm in Paket 1 steckt.</para>
    /// </summary>
    public sealed class Sammelstand
    {
        /// <summary>
        /// Quellprojektname → Id am Ziel, über alle bisherigen Pakete des Laufs.
        /// <b>Ohne Rücksicht auf Groß- und Kleinschreibung</b> — wie jede andere
        /// Namenssuche dieses Controllers.
        /// </summary>
        public Dictionary<string, int> NameZuId { get; }
            = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Darf das Hauptprojekt des nächsten Pakets übersprungen werden, wenn es in
        /// <see cref="NameZuId"/> schon steht? Der Sammellauf setzt es; ein
        /// Einzelimport lässt es aus.
        /// </summary>
        public bool StammUeberspringen { get; set; }
    }

    /// <summary>
    /// <b>Die Brücke vom Tabellenfortschritt auf den Laufsfortschritt</b> — SYNCHRON.
    ///
    /// <para><b>Warum kein <c>Progress&lt;T&gt;</c>.</b> Der stellt jede Meldung in den
    /// Synchronisationskontext des Erzeugers ein und liefert sie SPÄTER aus. Im Kern
    /// gibt es keinen solchen Kontext, auf den man sich verlassen dürfte, und die
    /// Reihenfolge der Meldungen wäre nicht mehr die des Laufs — ein Fortschritt, der
    /// zurückspringt. Hier wird unmittelbar weitergereicht; wer zeichnen muss,
    /// entscheidet das am anderen Ende (dieselbe Regel wie in
    /// <c>KatalogImportAblauf</c>).</para>
    /// </summary>
    public sealed class Fortschrittsbruecke : IProgress<ProjektDuplizierenCtrl.Fortschritt>
    {
        private readonly IProgress<ImportFortschritt> _ziel;
        private readonly int _paket;
        private readonly int _pakete;
        private readonly string _schluessel;

        /// <param name="ziel">Der Empfänger; <c>null</c> ist erlaubt (dann verpufft alles).</param>
        /// <param name="paket">Nullbasierte Nummer des laufenden Pakets.</param>
        /// <param name="pakete">Zahl der Pakete des Laufs.</param>
        /// <param name="schluessel">Sprachneutraler Schlüssel des Begleittextes.</param>
        public Fortschrittsbruecke(IProgress<ImportFortschritt> ziel, int paket, int pakete,
                                   string schluessel = "TRANSFER_LAUF_PAKET")
        {
            _ziel = ziel;
            _paket = paket < 0 ? 0 : paket;
            _pakete = pakete < 1 ? 1 : pakete;
            _schluessel = schluessel ?? "";
        }

        /// <summary>
        /// Der Anteil ist ZWEISTUFIG: <c>(i + a/b) / n</c> — der Platz dieses Pakets im
        /// Lauf plus sein eigener Fortschritt darin. Damit läuft der Balken über den
        /// ganzen Lauf monoton von 0 nach 1 statt fünfmal von vorn.
        /// </summary>
        public void Report(ProjektDuplizierenCtrl.Fortschritt wert)
        {
            if (_ziel == null) return;

            double innen = wert.Gesamt > 0
                ? Math.Min(1.0, Math.Max(0.0, (double)wert.Aktuell / wert.Gesamt))
                : 0.0;
            double anteil = Math.Min(1.0, (_paket + innen) / _pakete);

            _ziel.Report(new ImportFortschritt(anteil, _schluessel,
                (_paket + 1).ToString(CultureInfo.InvariantCulture),
                _pakete.ToString(CultureInfo.InvariantCulture),
                wert.Tabelle ?? ""));
        }
    }

    /// <summary>
    /// <b>Der SAMMELTRANSFER</b> (Auftrag PI-1, Anwenderentscheid PI-Q1 vom
    /// 19.09.2026) — mehrere Gruppen in einem Export, mehrere Pakete in einem Import.
    ///
    /// <para>Die zweite Hälfte von <see cref="ProjektExportImportCtrl"/>: Der
    /// Einzelweg bleibt unverändert, was dazukommt, steht hier.</para>
    /// </summary>
    public partial class ProjektExportImportCtrl
    {
        /// <summary>Die Endung der Transportdateien.</summary>
        public const string PAKET_ENDUNG = ".wpx";

        /// <summary>Höchstlänge des Dateinamens ohne Endung — Windows-Pfadgrenze mit Reserve.</summary>
        private const int DATEINAME_MAX = 120;

        /// <summary>
        /// Der Zielname, unter dem der letzte Import sein Hauptprojekt angelegt hat —
        /// er kann sich vom Quellnamen unterscheiden (Modus „neuer Name").
        /// </summary>
        internal string LetzterZielname { get; private set; } = "";

        /// <summary>Hat der letzte Import sein Stammprojekt übersprungen?</summary>
        internal bool LetzterStammUebersprungen { get; private set; }

        // ===================================================================================
        //  Paketkopf — lesen, ohne die Datenbank anzufassen
        // ===================================================================================

        /// <summary>
        /// <b>Liest <c>manifest.json</c> aus einem Paket — und wirft NIE.</b>
        ///
        /// <para>Ein Anwender, der fünf Dateien auf einmal wählt, hat mit einiger
        /// Sicherheit eine davon falsch erwischt. Ein Wurf an dieser Stelle machte aus
        /// der Vorschau über fünf Pakete eine Fehlermeldung über null — deshalb trägt
        /// der Kopf seinen Grund im Feld <see cref="Paketkopf.Fehler"/>, und die Liste
        /// bleibt vollständig.</para>
        /// </summary>
        public static Paketkopf PaketKopf(string pfad)
        {
            try
            {
                using (ZipArchive zip = ZipFile.OpenRead(pfad))
                {
                    ZipArchiveEntry e = zip.GetEntry("manifest.json");
                    if (e == null)
                        return Unlesbar(pfad, T("TRANSFER_PAKET_UNLESBAR",
                            "Kein gültiges Projektpaket (manifest.json fehlt)."));

                    string json;
                    using (var r = new StreamReader(e.Open())) json = r.ReadToEnd();

                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        JsonElement wurzel = doc.RootElement;

                        string quelle = Text(wurzel, "sourceProject");
                        string datum = Text(wurzel, "exportedUtc");
                        int schema = Zahl(wurzel, "schemaVersion");
                        int format = Zahl(wurzel, "formatVersion");

                        // Das Datum folgt der PROGRAMMSPRACHE — „g" der aktuellen Kultur
                        // (A-9, Befund W15a-B32a).
                        if (DateTime.TryParse(datum, null, DateTimeStyles.RoundtripKind, out DateTime dt))
                            datum = dt.ToLocalTime().ToString("g");

                        var varianten = new List<string>();
                        if (wurzel.TryGetProperty("variants", out JsonElement vs)
                            && vs.ValueKind == JsonValueKind.Array)
                            foreach (JsonElement v in vs.EnumerateArray())
                                varianten.Add(Text(v, "name"));

                        // Ist das Hauptprojekt selbst eine Variante? Das steht in den
                        // variantLinks — der Eintrag, dessen „projekt" das Hauptprojekt ist.
                        string stammQuelle = "";
                        if (wurzel.TryGetProperty("variantLinks", out JsonElement ls)
                            && ls.ValueKind == JsonValueKind.Array)
                            foreach (JsonElement l in ls.EnumerateArray())
                                if (string.Equals(Text(l, "projekt"), quelle, StringComparison.OrdinalIgnoreCase))
                                { stammQuelle = Text(l, "stamm"); break; }

                        return new Paketkopf(pfad, quelle, datum, schema, format,
                                             varianten, stammQuelle, "");
                    }
                }
            }
            catch (Exception ex)
            {
                return Unlesbar(pfad, string.Format(
                    T("TRANSFER_PAKET_UNLESBAR_GRUND", "Paket konnte nicht gelesen werden: {0}"),
                    ex.Message));
            }
        }

        /// <summary>
        /// <b>Die Reihenfolge, in der die Pakete eines Laufs eingespielt werden:
        /// Stamm vor Variante.</b>
        ///
        /// <para>Der Anwender wählt seine Dateien im Dateiwähler und bekommt sie nach
        /// dem Namen sortiert — also in einer Reihenfolge, die mit der Verwandtschaft
        /// der Projekte nichts zu tun hat. Käme das Variantenpaket zuerst, fände seine
        /// Verknüpfung den Stamm nicht (er ist noch nicht da), die Variante stünde
        /// eigenständig, und der Stamm käme danach, ohne dass sie ihn je fände. Der
        /// Lauf sortiert deshalb selbst.</para>
        ///
        /// <para><b>Was sich nicht auflösen lässt, kommt ans Ende</b> — ein
        /// Variantenpaket, dessen Stamm in KEINEM Paket des Laufs steckt: Es findet
        /// seinen Stamm vielleicht am Zielbestand, und das gelingt zuletzt genauso gut
        /// wie zuerst.</para>
        /// </summary>
        public static IReadOnlyList<Paketkopf> Reihenfolge(IReadOnlyList<Paketkopf> koepfe)
        {
            var folge = new List<Paketkopf>();
            if (koepfe == null || koepfe.Count == 0) return folge;

            var stamm = koepfe.Where(k => string.IsNullOrEmpty(k.StammQuelle)).ToList();
            var varianten = koepfe.Where(k => !string.IsNullOrEmpty(k.StammQuelle)).ToList();
            var gesetzt = new HashSet<Paketkopf>();

            foreach (Paketkopf s in stamm)
            {
                folge.Add(s);
                gesetzt.Add(s);

                // Jedes Variantenpaket, dessen Stamm dieses Paket mitbringt — als
                // Hauptprojekt ODER als mitgereiste Variante.
                foreach (Paketkopf v in varianten)
                {
                    if (gesetzt.Contains(v)) continue;
                    if (!Bringt(s, v.StammQuelle)) continue;
                    folge.Add(v);
                    gesetzt.Add(v);
                }
            }

            foreach (Paketkopf v in varianten)
                if (!gesetzt.Contains(v)) folge.Add(v);

            return folge;
        }

        /// <summary>Bringt dieses Paket das Projekt <paramref name="name"/> mit?</summary>
        private static bool Bringt(Paketkopf kopf, string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (string.Equals(kopf.Quellprojekt, name, StringComparison.OrdinalIgnoreCase)) return true;
            foreach (string v in kopf.Varianten)
                if (string.Equals(v, name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        // ===================================================================================
        //  Sammelexport
        // ===================================================================================

        /// <summary>
        /// <b>Ein Paket je Stammgruppe in einen Zielordner</b> (Anwenderentscheid PI-Q1).
        ///
        /// <para>Der Lauf hält an keinem Fehler an: Was sich schreiben lässt, wird
        /// geschrieben, was nicht, steht mit seinem Grund in der Bilanz. Fünf Gruppen
        /// und eine kaputte ergäben sonst nichts.</para>
        /// </summary>
        /// <param name="gruppen">Die Gruppen aus <see cref="Projektgruppierung.Gruppieren"/>.</param>
        /// <param name="zielOrdner">Der Ordner, in den geschrieben wird; er muss bestehen.</param>
        /// <param name="melder">Fortschritt über den ganzen Lauf; darf <c>null</c> sein.</param>
        /// <param name="abbruch">Wird NUR zwischen zwei Gruppen geprüft.</param>
        public ExportBilanz ExportGruppen(IReadOnlyList<Transfergruppe> gruppen, string zielOrdner,
            IProgress<ImportFortschritt> melder = null, CancellationToken abbruch = default)
        {
            var bilanz = new ExportBilanz { Pakete = gruppen?.Count ?? 0 };
            if (gruppen == null || gruppen.Count == 0) return bilanz;

            for (int i = 0; i < gruppen.Count; i++)
            {
                if (abbruch.IsCancellationRequested) { bilanz.Abgebrochen = true; break; }

                Transfergruppe g = gruppen[i];
                string datei;
                try { datei = Paketdateiname(zielOrdner, g.Stamm); }
                catch (Exception ex)
                {
                    bilanz.Fehler++;
                    bilanz.Anfuegen(new ExportZeile(g.Stamm, "", g.Projektzahl, false, ex.Message));
                    continue;
                }

                var bruecke = new Fortschrittsbruecke(melder, i, gruppen.Count);
                bool ok = ExportEines(g.Stamm, new List<string>(g.Varianten), datei, bruecke,
                                      out string grund);

                if (ok)
                {
                    bilanz.Geschrieben++;
                    bilanz.Anfuegen(new ExportZeile(g.Stamm, datei, g.Projektzahl, true, ""));
                }
                else
                {
                    bilanz.Fehler++;
                    // Eine angefangene Datei bleibt nicht liegen — sonst stünde im
                    // Zielordner ein Paket, das keines ist.
                    try { if (File.Exists(datei)) File.Delete(datei); } catch { }
                    bilanz.Anfuegen(new ExportZeile(g.Stamm, "", g.Projektzahl, false, grund));
                }
            }

            return bilanz;
        }

        /// <summary>
        /// <b>Der Dateiname eines Pakets</b>: <c>&lt;Stammname&gt;.wpx</c> im
        /// Zielordner.
        ///
        /// <para>Verbotene Zeichen werden zum Unterstrich, der Name auf
        /// <see cref="DATEINAME_MAX"/> Zeichen gekürzt, und eine Kollision zählt
        /// hoch — „ (2)", „ (3)" … , dieselbe Schreibweise wie
        /// <c>EindeutigerName</c> beim Import. <b>Es wird nie überschrieben</b>:
        /// Ein zweiter Lauf am selben Tag soll den ersten nicht wegnehmen.</para>
        /// </summary>
        public static string Paketdateiname(string zielOrdner, string gruppenname)
        {
            string basis = Dateinamenteil(gruppenname);
            string ziel = Path.Combine(zielOrdner ?? "", basis + PAKET_ENDUNG);
            if (!File.Exists(ziel)) return ziel;

            for (int n = 2; n < 1000; n++)
            {
                string kand = Path.Combine(zielOrdner ?? "", basis + " (" + n + ")" + PAKET_ENDUNG);
                if (!File.Exists(kand)) return kand;
            }
            return Path.Combine(zielOrdner ?? "",
                basis + " (" + Guid.NewGuid().ToString("N").Substring(0, 6) + ")" + PAKET_ENDUNG);
        }

        /// <summary>
        /// Die Zeichen, die in KEINEM Paketnamen stehen dürfen — eine FESTE Liste,
        /// nicht <c>Path.GetInvalidFileNameChars()</c>.
        ///
        /// <para><b>Warum fest.</b> Die Laufzeit antwortet je Plattform verschieden:
        /// unter Linux und macOS gelten nur der Schrägstrich und das Nullzeichen als
        /// verboten, unter Windows dazu Doppelpunkt, Anführungszeichen, Stern,
        /// Fragezeichen, Senkrechtstrich, Kleiner- und Größerzeichen. Ein Paket, das
        /// auf einem Mac „Haus: Nord.wpx“ hieße, ließe sich unter Windows nicht
        /// einmal ablegen — und der Transfer ZWISCHEN Rechnern ist genau der Zweck
        /// dieser Datei.</para>
        /// </summary>
        private static readonly char[] VERBOTENE_ZEICHEN =
            { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };

        /// <summary>Aus einem Projektnamen einen tragfähigen Dateinamenteil machen.</summary>
        private static string Dateinamenteil(string name)
        {
            string roh = (name ?? "").Trim();
            if (roh.Length == 0) roh = "Projekt";

            var verboten = new HashSet<char>(VERBOTENE_ZEICHEN);
            foreach (char c in Path.GetInvalidFileNameChars()) verboten.Add(c);

            var sb = new System.Text.StringBuilder(roh.Length);
            foreach (char c in roh) sb.Append(verboten.Contains(c) || c < ' ' ? '_' : c);

            string sauber = sb.ToString().TrimEnd('.', ' ');
            if (sauber.Length == 0) sauber = "Projekt";
            if (sauber.Length > DATEINAME_MAX) sauber = sauber.Substring(0, DATEINAME_MAX).TrimEnd('.', ' ');
            return sauber.Length == 0 ? "Projekt" : sauber;
        }

        // ===================================================================================
        //  Sammelimport
        // ===================================================================================

        /// <summary>
        /// <b>Mehrere Pakete in EINEM Lauf</b> (Anwenderentscheid PI-Q1) — mit einer
        /// Namensabbildung über den ganzen Lauf, Stamm vor Variante und ohne Dubletten.
        ///
        /// <para><b>Drei Dinge, die ein Lauf kann und fünf Einzelimporte nicht:</b>
        /// Die Variante findet den Stamm aus einem anderen Paket (Namensabbildung);
        /// ein Stamm, der in zwei Paketen steckt, entsteht einmal (Dublettenschutz);
        /// und die Reihenfolge stimmt, egal wie der Dateiwähler sortiert hat.</para>
        ///
        /// <para><b>Was ein Lauf bewusst NICHT kann</b>: zwei Stände desselben Projekts
        /// zusammenführen. Das ist seit Rev. 2 des Konzepts abgegrenzt (§ 8) und bleibt
        /// es — ein Import ist ein neues Projekt oder ein bewusstes Überschreiben.</para>
        /// </summary>
        /// <param name="pfade">Die gewählten Paketdateien, in der Reihenfolge der Wahl.</param>
        /// <param name="modus">Der Konfliktmodus; er gilt für den ganzen Lauf (TF2).</param>
        /// <param name="melder">Fortschritt über den ganzen Lauf; darf <c>null</c> sein.</param>
        /// <param name="abbruch">Wird NUR zwischen zwei Paketen geprüft — ein Paket wird nie halb eingespielt.</param>
        public SammelImportBilanz ImportierenMehrere(IReadOnlyList<string> pfade,
            BeiVorhandenem modus, IProgress<ImportFortschritt> melder = null,
            CancellationToken abbruch = default)
        {
            var bilanz = new SammelImportBilanz { Pakete = pfade?.Count ?? 0 };
            if (pfade == null || pfade.Count == 0) return bilanz;

            // 1) Köpfe lesen. Was hier schon durchfällt, kostet keine Transaktion.
            var brauchbar = new List<Paketkopf>();
            foreach (string pfad in pfade)
            {
                Paketkopf kopf = PaketKopf(pfad);

                if (!string.IsNullOrEmpty(kopf.Fehler))
                {
                    bilanz.Fehler++;
                    bilanz.Anfuegen(new SammelImportZeile(pfad, kopf.Quellprojekt, "",
                        Paketbefund.Unlesbar, kopf.Fehler, Array.Empty<string>()));
                    continue;
                }

                // B2 (Konzept T2): gleicher Schemastand, sonst gar nicht erst anfassen.
                // schemaVersion 0 = Altpaket und bleibt zugelassen.
                if (kopf.Schemastand != 0 && kopf.Schemastand != SchemaStand.Zielversion)
                {
                    bilanz.Fehler++;
                    bilanz.Anfuegen(new SammelImportZeile(pfad, kopf.Quellprojekt, "",
                        Paketbefund.Schemastand,
                        string.Format(
                            T("TRANSFER_PAKET_SCHEMA",
                              "Das Paket wurde mit Schemastand {0} exportiert, dieser Rechner arbeitet " +
                              "mit Stand {1}."),
                            kopf.Schemastand, SchemaStand.Zielversion),
                        Array.Empty<string>()));
                    continue;
                }

                brauchbar.Add(kopf);
            }

            // 2) Stamm vor Variante.
            IReadOnlyList<Paketkopf> folge = Reihenfolge(brauchbar);

            // 3) Je Paket eine Transaktion — ein Teilerfolg ist ehrlicher als ein
            //    Rückbau, der auch das Gelungene wegnimmt.
            var stand = new Sammelstand { StammUeberspringen = true };

            for (int i = 0; i < folge.Count; i++)
            {
                Paketkopf kopf = folge[i];

                if (abbruch.IsCancellationRequested)
                {
                    bilanz.Abgebrochen = true;
                    for (int r = i; r < folge.Count; r++)
                        bilanz.Anfuegen(new SammelImportZeile(folge[r].Datei, folge[r].Quellprojekt, "",
                            Paketbefund.Abgebrochen, "", Array.Empty<string>()));
                    break;
                }

                var bruecke = new Fortschrittsbruecke(melder, i, folge.Count);
                LetzterZielname = "";
                LetzterStammUebersprungen = false;

                int id = ImportierenIntern(kopf.Datei, null, modus, bruecke, stand, out string fehler);

                if (id <= 0)
                {
                    bilanz.Fehler++;
                    bilanz.Anfuegen(new SammelImportZeile(kopf.Datei, kopf.Quellprojekt, "",
                        Paketbefund.Fehler, fehler ?? "", LetzterBericht ?? (IReadOnlyList<string>)Array.Empty<string>()));
                    continue;
                }

                bilanz.Importiert++;
                Paketbefund befund = Paketbefund.Importiert;
                if (LetzterStammUebersprungen) { bilanz.Uebersprungen++; befund = Paketbefund.StammUebersprungen; }
                else if (!string.Equals(LetzterZielname, kopf.Quellprojekt, StringComparison.Ordinal))
                { bilanz.Umbenannt++; befund = Paketbefund.Umbenannt; }

                bilanz.Anfuegen(new SammelImportZeile(kopf.Datei, kopf.Quellprojekt, LetzterZielname,
                    befund, "", new List<string>(LetzterBericht ?? new List<string>())));
            }

            return bilanz;
        }

        // ===================================================================================
        //  Handwerkszeug
        // ===================================================================================

        /// <summary>
        /// Ist dieses Projekt selbst eine Variante? Die Frage geht an
        /// <c>VariantenCtrl</c> — dieselbe Quelle, aus der auch die Projektliste ihre
        /// Herkunftsspalte hat. Ein Fehler beim Fragen heißt „nein": Eine fehlende
        /// <c>Tab_Variante</c> ist ein Bestand ohne Variantenmodul, kein Verbot.
        /// </summary>
        private static bool IstVariante(int projektId)
        {
            if (projektId <= 0) return false;
            try { return new VariantenCtrl().StammRefDerVariante(projektId) > 0; }
            catch { return false; }
        }

        private static Paketkopf Unlesbar(string pfad, string grund)
            => new Paketkopf(pfad ?? "", "", "", 0, 0, Array.Empty<string>(), "", grund);

        private static string Text(JsonElement e, string name)
            => e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String
               ? (v.GetString() ?? "") : "";

        private static int Zahl(JsonElement e, string name)
            => e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.Number
               && v.TryGetInt32(out int n) ? n : 0;
    }
}
