using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die DATENSEITE des Gebäudeimports</b> (Stufe G4c, Welle 2; Softwarearchitektur 3.4,
    /// Umsetzungskonzept 3.3) — plattformfrei in <c>EPOS.UI.Daten</c>, damit Windows und iOS
    /// denselben Weg nehmen. Sie baut den Parametersatz des <c>GebaeudeImportDialog</c> aus dem
    /// Kern und übersetzt dessen Werte in die DTO der Komponente; alle Anzeigetexte je Zeile
    /// (Herkunft, Beleg, Wert, Meldungen) kommen aus <see cref="GebaeudeZuordnungsModell"/>.
    ///
    /// <para><b>Eine Instanz je Dialog, kein <c>static</c>-Zustand.</b> Der Zustand lebt im
    /// <see cref="GebaeudeImportAblauf"/>, den die Hülle hält (Softwarearchitektur 1.5: „der
    /// Zustand lebt im Ablauf, nicht in der Komponente"); die Delegaten des Parametersatzes
    /// arbeiten auf ihm.</para>
    ///
    /// <para><b>Die Größengrenze fällt VOR dem Öffnen</b> (1.5, Regel 2): Der Dateiwähler läuft
    /// über <c>Dienste.Datei.DateiOeffnenAsync</c> (erwartet, nie synchron — Wächter
    /// <c>HuellenwegTests</c>), danach prüft <see cref="GebaeudeImportAblauf.GroesseZulaessig"/>
    /// die Größe gegen <c>MaxBytes</c>, das die Hülle je Plattform belegt
    /// (<see cref="GebaeudeImportProfil.GrenzeFuerPlattform"/>). Eine zu große Datei ist eine
    /// benannte Ablehnung an die Komponente, gelesen wird nicht. <b>Das Lesen</b> läuft im
    /// Arbeitsfaden über <see cref="Kulturweitergabe.Starten{T}"/> (Wächter
    /// <c>ParallelitaetWacheTests</c>) mit Fortschritt und Abbruch.</para>
    ///
    /// <para><b>Das Profil folgt der Dateiwahl</b> (Stufe G4, Welle 4): Der Einstieg im
    /// Gebäudedialog bietet EINE Dateiwahl mit dem gemeinsamen Filter beider Formate
    /// (<see cref="GebaeudeImportProfil.DATEIFILTER_ALLE"/>); welches Profil gilt, entscheidet der
    /// Kern an der Endung (<see cref="GebaeudeImportProfil.FuerDatei"/>) — eine andere Endung ist
    /// die benannte Ablehnung „Dateiart nicht unterstützt". Die Größengrenze prüft die Hülle danach
    /// je Profil und Plattform wie gehabt. Mit einem festen Profil
    /// (<see cref="GebaeudeImportHuelle(GebaeudeImportProfil, int, bool?)"/>) bleibt es bei diesem.</para>
    ///
    /// <para><b>Geschrieben wird hier nichts.</b> Der Schreibweg ist ein Delegat des WIRTS
    /// (<see cref="Gaben"/>, <c>uebernehmen</c>); ohne ihn ist OK im Dialog weich gesperrt. Für den
    /// Weg „als Katalogsatz ablegen" bildet <see cref="NachKatalogdaten"/> das Ergebnis auf die
    /// Felder des Gebäudeeditors ab (<see cref="Vorbelegung"/>), und <see cref="Herkunft"/> reicht
    /// Quelle und Paarungen als ausstehende Herkunft an die neue Projektzeile — geschrieben wird
    /// sie erst mit der Gebäudeliste (<c>WizardCtrl.GebaeudeZuordnungAnlegen</c>).</para>
    ///
    /// <para><b>„Dieselbe Datei schon importiert":</b> Mit einem Projekt fragt die Hülle nach dem
    /// Lesen über den SHA-256, ob ein Gebäude DIESES Projekts schon aus der Datei stammt
    /// (<see cref="GebaeudeImportCtrl.ImporteImProjekt"/>), und reicht einen leisen Hinweis mit
    /// Gebäudename und Zeitpunkt an den Dialog — gesperrt wird nichts.</para>
    ///
    /// <para><b>Der Namensabgleich der Baustoffe</b> (Abschnitt „Baustoffe"): Jeder Vorschlag entsteht
    /// mit dem Abgleich (<see cref="Baustoffabgleich"/>) — Katalog, Synonyme und gemerkte Zuordnungen
    /// liest die Hülle EINMAL je Dialog (<see cref="BaustoffabgleichDaten.Abzug"/>): mit einem Projekt
    /// über <see cref="BaustoffabgleichCtrl"/>, ohne Projekt aus der Auslieferungssaat im Speicher
    /// (<see cref="BaustoffabgleichDaten.AusSaat"/>), damit die Hülle ohne Projekt weiter keine
    /// Datenbank fragt. Darüber legt sie die noch nicht gespeicherten Zuordnungen anderer Importe
    /// derselben Liste (<see cref="Vorgemerkt"/>) und die des Dialogs
    /// (<see cref="GebaeudeZuordnungsanfrage.Baustoffzuordnungen"/>) und bildet den Vorschlag bei jeder
    /// Änderung neu. Gemerkt wird erst mit der Gebäudeliste: Die Zuordnungen reisen in der
    /// <see cref="Herkunft"/>.</para>
    /// </summary>
    internal sealed class GebaeudeImportHuelle
    {
        private readonly GebaeudeImportAblauf _ablauf = new GebaeudeImportAblauf();
        private readonly GebaeudeImportProfil _festesProfil;
        private readonly int _idProjekt;
        private readonly bool _ios;
        private GebaeudeImportProfil _profil;
        private GebaeudeImportSatz _satz;
        private GebaeudeBauteilvorschlag _vorschlag;
        private GebaeudeZonierung _zonierung;
        private bool _alsZone;

        private IBaustoffabgleichQuelle _abgleichsquelle;
        private BaustoffabgleichDaten _abzug;
        private IReadOnlyList<GebaeudeBaustoffgruppe> _katalog;
        private IReadOnlyDictionary<string, int?> _baustoffzuordnungen = new Dictionary<string, int?>(StringComparer.Ordinal);

        /// <summary>
        /// Die Hülle des Einstiegs im Gebäudedialog: EINE Dateiwahl für gbXML und IFC, das Profil
        /// folgt der Endung der gewählten Datei.
        /// </summary>
        /// <param name="idProjekt">Das Projekt für den Hinweis „schon importiert"; 0 = keines — dann fragt die Hülle keine Datenbank.</param>
        /// <param name="ios">
        /// Die Plattform der Größengrenze: <c>true</c> = iOS, <c>false</c> = Windows, <c>null</c> = die
        /// laufende. Die Schalen lassen sie weg; ein Prüfstand (Wirt der Rasterprobe) stellt sie ein.
        /// </param>
        internal GebaeudeImportHuelle(int idProjekt = 0, bool? ios = null)
        {
            _idProjekt = idProjekt;
            _ios = ios ?? OperatingSystem.IsIOS();
        }

        /// <summary>Die Hülle EINES Profils; die Größengrenze wird für die Plattform belegt (<paramref name="ios"/> wie oben).</summary>
        internal GebaeudeImportHuelle(GebaeudeImportProfil profil, int idProjekt = 0, bool? ios = null)
        {
            _ios = ios ?? OperatingSystem.IsIOS();
            _festesProfil = MitPlattformgrenze(profil ?? throw new ArgumentNullException(nameof(profil)));
            _profil = _festesProfil;
            _idProjekt = idProjekt;
        }

        /// <summary>
        /// Das Profil des Laufs samt Plattformgrenze — das feste, sonst das der zuletzt gelesenen
        /// Datei; <c>null</c>, solange nach der Dateiwahl entschieden wird und nichts gelesen ist.
        /// </summary>
        internal GebaeudeImportProfil Profil => _profil;

        /// <summary>Folgt das Profil der Dateiwahl (kein festes Profil)?</summary>
        internal bool ProfilNachDatei => _festesProfil == null;

        /// <summary>Der Filter des Dateiwählers: der des festen Profils, sonst der gemeinsame beider Formate.</summary>
        internal string Dateifilter => _festesProfil?.Dateifilter ?? GebaeudeImportProfil.DATEIFILTER_ALLE;

        /// <summary>Die Quelle des letzten gelesenen Laufs (Dateiname, SHA-256, Größe, Schema …); <c>null</c> ohne.</summary>
        internal GebaeudeQuelle Quelle => _ablauf.Quelle;

        /// <summary>Der Satz der letzten Zuordnung bzw. Prüfung — mit den Handänderungen des Dialogs; <c>null</c> ohne.</summary>
        internal GebaeudeImportSatz Satz => _satz;

        /// <summary>Die Paarungen Quellentität ↔ Gebäude des gewählten Gebäudes für <c>Tab_Importzuordnung</c>.</summary>
        internal IReadOnlyList<GebaeudeQuellzuordnung> Quellzuordnungen
            => _satz?.Quellzuordnungen ?? (IReadOnlyList<GebaeudeQuellzuordnung>)Array.Empty<GebaeudeQuellzuordnung>();

        /// <summary>
        /// Der Bauteilvorschlag der letzten Zuordnung bzw. Prüfung (Stufe G4b) — mit derselben Klasse
        /// und denselben Raumhaken wie der Satz; <c>null</c> ohne.
        /// </summary>
        internal GebaeudeBauteilvorschlag Vorschlag => _vorschlag;

        /// <summary>
        /// Kommt das Gebäude als Zone mit Bauteilen? Der Schalter des letzten geprüften Ergebnisses —
        /// nur, wenn sich der Vorschlag bilden ließ.
        /// </summary>
        internal bool AlsZone => _alsZone;

        /// <summary>
        /// Die Quelle des Namensabgleichs; ungesetzt gilt: mit Projekt die Datenbank
        /// (<see cref="BaustoffabgleichCtrl"/> samt den gemerkten Zuordnungen des Projekts), ohne
        /// Projekt die Auslieferungssaat im Speicher. Ein Prüfstand setzt eine eigene.
        /// </summary>
        internal IBaustoffabgleichQuelle Abgleichsquelle
        {
            get => _abgleichsquelle ??= _idProjekt > 0 ? new BaustoffabgleichCtrl(_idProjekt) : BaustoffabgleichDaten.AusSaat();
            init => _abgleichsquelle = value;
        }

        /// <summary>
        /// Zuordnungen anderer Importe derselben Gebäudeliste, die erst mit ihr gespeichert werden
        /// (Schlüssel → Katalogbaustoff, <c>null</c> = entfernt) — sie gelten hier wie gemerkte;
        /// <c>null</c> = keine.
        /// </summary>
        internal IReadOnlyDictionary<string, int?> Vorgemerkt { get; init; }

        /// <summary>Die Zuordnungen des Dialogs aus dem zuletzt geprüften Ergebnis, bereinigt (<see cref="Wirksame"/>).</summary>
        internal IReadOnlyDictionary<string, int?> Baustoffzuordnungen => _baustoffzuordnungen;

        // =================================================================================
        // Der Parametersatz
        // =================================================================================

        /// <summary>
        /// Der PARAMETERSATZ des Dialogs — ohne <c>Geschlossen</c>; der Wirt setzt es selbst.
        /// </summary>
        /// <param name="uebernehmen">
        /// Der Schreibweg des Wirts (<c>null</c> als Antwort = übernommen, sonst der Grund);
        /// <c>null</c> = keiner — dann steht der Schlüssel nicht im Satz, und OK ist weich gesperrt.
        /// </param>
        internal IReadOnlyDictionary<string, object> Gaben(Func<GebaeudeImportErgebnis, Task<string>> uebernehmen = null)
        {
            var gaben = new Dictionary<string, object>
            {
                ["Profil"] = ProfilDaten(),
                ["Baualtersklassen"] = GebaeudeStammCtrl.Baualtersklassen(),
                ["DateiWaehlen"] = new Func<string, Task<GebaeudeDateiwahl>>(DateiWaehlenAsync),
                ["Lesen"] = new Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>(LesenAsync),
                ["Zuordnen"] = new Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>(Zuordnen),
                ["Pruefen"] = new Func<GebaeudeImportErgebnis, IReadOnlyList<GebaeudeImportMeldung>>(Pruefen),
            };
            if (uebernehmen != null) gaben["Uebernehmen"] = uebernehmen;
            return gaben;
        }

        /// <summary>
        /// Was das Format ausmacht, als Daten für die Komponente — mit festem Profil dessen Angaben,
        /// sonst die beider Formate: Formatnamen, der gemeinsame Filter, die Grenze je Format und
        /// Plattform („gbXML 25 MB · IFC 50 MB"), die Zonierungsregeln beider und der gemeinsame
        /// Hilfeschlüssel.
        /// </summary>
        internal GebaeudeImportProfilDaten ProfilDaten()
        {
            if (_festesProfil != null)
                return new GebaeudeImportProfilDaten(
                    GebaeudeZuordnungsModell.FormatText(_festesProfil),
                    _festesProfil.Dateifilter,
                    _festesProfil.MaxBytes > 0 ? GebaeudeZuordnungsModell.GroesseText(_festesProfil.MaxBytes) : "",
                    _festesProfil.Zonierungsregeln.Select(GebaeudeZuordnungsModell.ZonenregelText).ToList(),
                    _festesProfil.HilfeSchluessel);

            IReadOnlyList<GebaeudeImportProfil> alle = BeideProfile();
            return new GebaeudeImportProfilDaten(
                string.Join(", ", alle.Select(GebaeudeZuordnungsModell.FormatText)),
                GebaeudeImportProfil.DATEIFILTER_ALLE,
                string.Join(" · ", alle.Where(p => p.MaxBytes > 0)
                                       .Select(p => GebaeudeZuordnungsModell.FormatText(p) + " " +
                                                    GebaeudeZuordnungsModell.GroesseText(p.MaxBytes))),
                alle.SelectMany(p => p.Zonierungsregeln).Select(GebaeudeZuordnungsModell.ZonenregelText).Distinct().ToList(),
                GebaeudeImportProfil.HILFE_ZUORDNUNG);
        }

        /// <summary>Beide Formate mit der Grenze der Plattform der Hülle — gbXML zuerst.</summary>
        private IReadOnlyList<GebaeudeImportProfil> BeideProfile()
            => new[] { MitPlattformgrenze(new GbxmlImportProfil()), MitPlattformgrenze(new IfcImportProfil()) };

        /// <summary>Belegt die Größengrenze eines Profils für die Plattform der Hülle (Softwarearchitektur 1.5, Regel 2).</summary>
        private GebaeudeImportProfil MitPlattformgrenze(GebaeudeImportProfil profil)
        {
            if (profil != null) profil.MaxBytes = profil.GrenzeFuerPlattform(_ios);
            return profil;
        }

        /// <summary>
        /// Das Profil einer Datei: das feste, sonst das der Endung samt Plattformgrenze
        /// (<see cref="GebaeudeImportProfil.FuerDatei"/>); <c>null</c> = Dateiart nicht unterstützt.
        /// </summary>
        private GebaeudeImportProfil ProfilFuer(string pfad)
            => _festesProfil ?? MitPlattformgrenze(GebaeudeImportProfil.FuerDatei(pfad));

        /// <summary>Die benannte Ablehnung einer Datei, deren Endung kein Format trägt.</summary>
        private static string DateiartText(string dateiname)
            => Formatieren(MyResource.Resource.GIMP_DLG_DATEIART, GebaeudeQuelle.NurName(dateiname));

        private static string Formatieren(string vorlage, params object[] werte)
        {
            try { return string.Format(CultureInfo.CurrentCulture, vorlage ?? "", werte); }
            catch (FormatException) { return vorlage ?? ""; }
        }

        // =================================================================================
        // Dateiwahl und Lesen
        // =================================================================================

        /// <summary>
        /// Der Dateiwähler der Plattform, dann das Profil nach der Endung und die Größe gegen seine
        /// Grenze — VOR dem Öffnen. <c>null</c> = abgebrochen; eine Datei anderer Art, eine zu große
        /// oder eine nicht lesbare Datei kommt als benannte Ablehnung zurück.
        /// </summary>
        internal async Task<GebaeudeDateiwahl> DateiWaehlenAsync(string filter)
        {
            string pfad = await Dienste.Datei.DateiOeffnenAsync(
                MyResource.Resource.GIMP_DLG_DATEI_TITEL,
                string.IsNullOrEmpty(filter) ? Dateifilter : filter,
                null);
            if (string.IsNullOrEmpty(pfad)) return null;

            string name = GebaeudeQuelle.NurName(pfad);
            GebaeudeImportProfil profil = ProfilFuer(pfad);
            if (profil == null) return new GebaeudeDateiwahl(pfad, name, 0, DateiartText(name));

            long groesse;
            try
            {
                groesse = new FileInfo(pfad).Length;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException
                                       || ex is ArgumentException || ex is NotSupportedException)
            {
                return new GebaeudeDateiwahl(pfad, name, 0,
                    Text(new PruefMeldung(PruefStufe.Fehler, profil.Meldung("LESEFEHLER"), ex.Message)));
            }

            if (!GebaeudeImportAblauf.GroesseZulaessig(groesse, profil))
                return new GebaeudeDateiwahl(pfad, name, groesse,
                    Text(new PruefMeldung(PruefStufe.Fehler, profil.Meldung("ZU_GROSS"),
                        groesse.ToString(CultureInfo.InvariantCulture),
                        profil.MaxBytes.ToString(CultureInfo.InvariantCulture))));

            return new GebaeudeDateiwahl(pfad, name, groesse);
        }

        /// <summary>
        /// Liest die Datei im Arbeitsfaden (Kulturweitergabe) mit dem Profil ihrer Endung — der
        /// Ablauf prüft die Größe ein zweites Mal, bevor ein Byte an den Leser geht. Ein Abbruch
        /// wirft <see cref="OperationCanceledException"/> bis zur Komponente, die still aussteigt.
        /// </summary>
        internal async Task<GebaeudeLesestand> LesenAsync(string pfad, IProgress<GebaeudeImportFortschritt> melder,
                                                          CancellationToken abbruch)
        {
            _satz = null;
            GebaeudeImportProfil profil = ProfilFuer(pfad);
            if (profil == null)
                return new GebaeudeLesestand(false, null, Array.Empty<string>(), new[]
                {
                    new GebaeudeImportMeldung(WarnStufe.Fehler, GebaeudeZuordnungsModell.StufeText(PruefStufe.Fehler),
                                              DateiartText(pfad), "GIMP_DLG_DATEIART"),
                });
            _profil = profil;

            IProgress<ImportFortschritt> bruecke = melder == null ? null : new Fortschrittsbruecke(melder);
            string oeffnungsfehler = null;

            int zahl = await Kulturweitergabe.Starten(() =>
            {
                FileStream strom;
                try
                {
                    strom = File.OpenRead(pfad);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException
                                           || ex is ArgumentException || ex is NotSupportedException)
                {
                    oeffnungsfehler = ex.Message;
                    return 0;
                }
                using (strom)
                    return _ablauf.Lesen(strom, pfad, profil, bruecke, abbruch);
            }, abbruch);

            if (oeffnungsfehler != null)
                return new GebaeudeLesestand(false, null, Array.Empty<string>(), new[]
                {
                    MeldungDaten(new PruefMeldung(PruefStufe.Fehler, profil.Meldung("LESEFEHLER"), oeffnungsfehler)),
                });
            return Lesestand(zahl, profil);
        }

        private GebaeudeLesestand Lesestand(int zahl, GebaeudeImportProfil profil)
        {
            List<GebaeudeImportMeldung> meldungen = _ablauf.Meldungen.Select(MeldungDaten).ToList();
            GebaeudeQuelle q = _ablauf.Quelle;
            if (zahl <= 0 || q == null)
            {
                // Nicht gelesen: der Grund zuerst — die Fehler, ohne sie alles Gemeldete.
                List<GebaeudeImportMeldung> fehler = meldungen.Where(m => m.Stufe == WarnStufe.Fehler).ToList();
                return new GebaeudeLesestand(false, null, Array.Empty<string>(), fehler.Count > 0 ? fehler : meldungen);
            }

            var kopf = new GebaeudeImportKopf(
                q.Dateiname,
                GebaeudeZuordnungsModell.FormatText(profil),
                GebaeudeZuordnungsModell.SchemaText(profil, q.Schemastand),
                GebaeudeZuordnungsModell.GroesseText(q.Groesse),
                GebaeudeZuordnungsModell.ZonenregelText(q.Zonenregel));
            return new GebaeudeLesestand(true, kopf, _ablauf.Gebaeude.ToList(), meldungen, SchonImportiertText(q));
        }

        /// <summary>
        /// Der leise Hinweis „dieselbe Datei schon importiert": Stammt ein Gebäude DIESES Projekts
        /// schon aus einer Datei mit demselben SHA-256, nennt er das jüngste mit Name und Zeitpunkt
        /// (in der Anzeigekultur); ohne Projekt oder ohne Treffer leer. Gesperrt wird nichts.
        /// </summary>
        private string SchonImportiertText(GebaeudeQuelle quelle)
        {
            if (_idProjekt <= 0 || quelle == null) return "";
            List<GebaeudeImportCtrl.ImportTreffer> treffer = new GebaeudeImportCtrl().ImporteImProjekt(_idProjekt, quelle.Hash);
            if (treffer.Count == 0) return "";
            GebaeudeImportCtrl.ImportTreffer jungster = treffer[0];
            return Formatieren(MyResource.Resource.GIMP_DLG_SCHON_IMPORTIERT, jungster.Gebaeudename,
                               Zeitpunkttext(jungster.Zeitpunkt));
        }

        /// <summary>Ein Zeitpunkt nach ISO 8601 in der Anzeigekultur („25.09.2026 10:00"); unlesbar, wie er steht.</summary>
        internal static string Zeitpunkttext(string iso)
            => DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset z)
                ? z.ToString("g", CultureInfo.CurrentCulture)
                : iso ?? "";

        // =================================================================================
        // Zuordnen und Prüfen
        // =================================================================================

        /// <summary>
        /// Ordnet ein Gebäude zu (Klasse, Haken der Raumliste), legt die Handwerte des Dialogs auf
        /// (Herkunft „manuell") und zieht die Vorgaben nach, die von ihnen abhängen
        /// (<see cref="GebaeudeImportSatz.FolgevorgabenNachziehen"/>); daraus der Stand der Zeilen.
        /// </summary>
        internal GebaeudeImportStand Zuordnen(GebaeudeZuordnungsanfrage anfrage)
        {
            if (anfrage == null || _ablauf.Abbild == null) return new GebaeudeImportStand();

            IReadOnlyDictionary<string, bool> haken = anfrage.BeheiztUebersteuert ?? new Dictionary<string, bool>();
            GebaeudeImportSatz satz = _ablauf.Zuordnen(anfrage.Gebaeudeindex, Klasse(anfrage.Baualtersklasse), haken);
            foreach (KeyValuePair<string, double?> hand in anfrage.Handwerte ?? new Dictionary<string, double?>())
                satz.ManuellSetzen(hand.Key, hand.Value);
            satz.FolgevorgabenNachziehen();
            _satz = satz;

            // Zonierung und Bauteilvorschlag mit derselben Klasse und denselben Raumhaken — jede
            // Anfrage (Regel, Klasse, Gebäude, Raumhaken, Handwert, Baustoffzuordnung) bildet sie neu,
            // mit dem Namensabgleich samt den Zuordnungen des Dialogs.
            VorschlagBilden(anfrage.Gebaeudeindex, Klasse(anfrage.Baualtersklasse), anfrage.Zonenregel, haken,
                            anfrage.Baustoffzuordnungen);

            return new GebaeudeImportStand
            {
                Kopftext = GebaeudeZuordnungsModell.KopfText(satz),
                Vorschlagsname = string.IsNullOrWhiteSpace(satz.Gebaeudename) ? satz.Gebaeudekennung : satz.Gebaeudename,
                Raeume = _ablauf.Raeume(anfrage.Gebaeudeindex, haken).Select(RaumDaten).ToList(),
                Zeilen = satz.Zeilen.Select(ZeileDaten).ToList(),
                Meldungen = satz.Meldungen.Select(MeldungDaten).ToList(),
                ManuellHerkunftText = GebaeudeZuordnungsModell.HerkunftText(Importherkunft.Manuell),
                Bauteile = BauteileDaten(_vorschlag),
                Baustoffe = BaustoffeDaten(_vorschlag, anfrage.Baustoffzuordnungen),
                KlasseDerDatei = GebaeudeZuordnungsModell.KlasseDerDatei(satz),
                KlassenHinweis = GebaeudeZuordnungsModell.KlassenHinweis(satz),
                Zonierung = GebaeudeImportZonen.ZonierungDaten(_zonierung, _vorschlag, haken),
            };
        }

        /// <summary>
        /// <b>Zonierung und Bauteilvorschlag einer Anfrage</b> (Stufe G6c): die Zonierung nach der
        /// gewählten Regel — eine Regel, die das Gebäude nicht trägt, und <c>null</c> heißen die Vorgabe
        /// der Datei (M7: je Geschoss, ohne Raumgrenzen eine Zone) —, dann der Vorschlag darauf. Ergibt die
        /// Regel nur eine Zone, ist es der Einzonenweg aus G4b, unverändert.
        /// </summary>
        private void VorschlagBilden(int index, char? klasse, string regel, IReadOnlyDictionary<string, bool> haken,
                                     IReadOnlyDictionary<string, int?> zuordnungen)
        {
            _zonierung = Zonieren(_ablauf.Abbild, index, regel, haken);
            _vorschlag = GebaeudeBauteilvorschlag.Bilden(_ablauf, index, klasse, haken, Abgleich(zuordnungen),
                                                         Mehrzonig(_zonierung) ? _zonierung : null);
        }

        /// <summary>Die Zonierung eines Gebäudes nach <paramref name="regel"/>, sonst nach der Vorgabe; <c>null</c> ohne Gebäude.</summary>
        internal static GebaeudeZonierung Zonieren(GebaeudeAbbild abbild, int index, string regel, IReadOnlyDictionary<string, bool> haken)
        {
            if (abbild == null || index < 0 || index >= abbild.Gebaeude.Count) return null;
            (IReadOnlyList<string> regeln, string vorgabe, bool _) = GebaeudeZonierung.Waehlbar(abbild, index);
            string wirksam = regel != null && regeln.Contains(regel) ? regel : vorgabe;
            return GebaeudeZonierung.Bilden(abbild, index, wirksam, haken);
        }

        /// <summary>Trägt die Zonierung mehr als eine Zone? Sonst gilt der Einzonenweg (Z5/X4 oder eine einzige Gruppe).</summary>
        internal static bool Mehrzonig(GebaeudeZonierung z)
            => z != null && !z.Abgelehnt && !z.Einzonig && z.Zonen.Count > 1;

        /// <summary>Die Zonierung der letzten Zuordnung bzw. Prüfung; <c>null</c> ohne.</summary>
        internal GebaeudeZonierung Zonierung => _zonierung;

        /// <summary>
        /// Die Prüfung am OK — DIESELBE des Kerns (<see cref="GebaeudeImportAblauf.Pruefen"/>) auf
        /// dem Satz samt Handänderungen, Haken und dem Namen des neuen Gebäudes.
        ///
        /// <para><b>Dazu die Prüfung des vorbelegten Editors</b> (<see cref="EditorBefund"/>): Die
        /// Übernahme führt in den Katalogeditor, dessen Abbrechen den Import verwirft. Was dessen OK
        /// anhielte — etwa ein fehlender g-Wert, weil weder die Datei noch eine Baualtersklasse ihn
        /// liefert —, ist deshalb schon hier ein benannter Fehler: Der Anwender wählt eine Klasse oder
        /// trägt den Wert ein, statt im Editor festzusitzen. Gefragt wird nur, wenn der Kern keinen
        /// Fehler meldet, damit eine Lücke nicht zweimal erscheint.</para>
        /// </summary>
        internal IReadOnlyList<GebaeudeImportMeldung> Pruefen(GebaeudeImportErgebnis ergebnis)
        {
            GebaeudeImportSatz satz = SatzAusErgebnis(ergebnis);
            if (satz == null) return Array.Empty<GebaeudeImportMeldung>();
            List<GebaeudeImportMeldung> meldungen =
                GebaeudeZuordnungsModell.Pruefe(satz, ergebnis.Gebaeudename ?? "").Select(MeldungDaten).ToList();

            // Als Zone mit Bauteilen gewählt, aber der Vorschlag lässt sich nicht bilden: benannt
            // abgelehnt, nicht still als Summenweg übernommen.
            if (ergebnis.AlsZone && (_vorschlag == null || _vorschlag.Abgelehnt))
                meldungen.Add(new GebaeudeImportMeldung(WarnStufe.Fehler, GebaeudeZuordnungsModell.StufeText(PruefStufe.Fehler),
                    Formatieren(MyResource.Resource.GIMP_DLG_ALS_ZONE_NICHT, Ablehnungstext(_vorschlag)), ALS_ZONE_NICHT));
            if (meldungen.Any(m => m.Stufe == WarnStufe.Fehler)) return meldungen;

            GebaeudePruefbefund befund = EditorBefund(ergebnis);
            if (befund != null)
                meldungen.Add(new GebaeudeImportMeldung(WarnStufe.Fehler, GebaeudeZuordnungsModell.StufeText(PruefStufe.Fehler),
                    Formatieren(satz.Baualtersklasse.HasValue ? MyResource.Resource.GIMP_DLG_EDITOR_BEFUND
                                                              : MyResource.Resource.GIMP_DLG_EDITOR_BEFUND_KLASSE, befund.Meldung),
                    EDITOR_BEFUND));
            return meldungen;
        }

        /// <summary>Die Kennung der Meldung „der Gebäudeeditor nähme das Gebäude so nicht an".</summary>
        internal const string EDITOR_BEFUND = "GIMP_DLG_EDITOR_BEFUND";

        /// <summary>Die Kennung der Meldung „als Zone mit Bauteilen gewählt, aber nicht möglich".</summary>
        internal const string ALS_ZONE_NICHT = "GIMP_DLG_ALS_ZONE_NICHT";

        /// <summary>
        /// <b>Die Prüfregeln des vorbelegten Editors beim OK</b> — DIESELBE Funktion, die der
        /// Katalogeditor im Modus Neu ruft (<see cref="GebaeudeArbeitsstand.Pruefen"/> nach
        /// <c>Laden(…, neu: true)</c>, mit den Texten seines Parametersatzes), auf der
        /// <see cref="Vorbelegung"/> des Ergebnisses; <c>null</c> = der Editor nähme sie an.
        /// </summary>
        internal GebaeudePruefbefund EditorBefund(GebaeudeImportErgebnis ergebnis)
        {
            var arbeit = new GebaeudeArbeitsstand();
            arbeit.Laden(Vorbelegung(ergebnis).Daten, neu: true);
            return arbeit.Pruefen(true, GebaeudeKatalogHuelle.Prueftexte(), GebaeudeKatalogHuelle.Texte());
        }

        /// <summary>
        /// Baut den Satz eines Ergebnisses neu — dieselbe Zuordnung, darauf die Handänderungen
        /// (Herkunft „manuell") und die Haken des Dialogs, dann die Folgevorgaben
        /// (<see cref="GebaeudeImportSatz.FolgevorgabenNachziehen"/>), damit Vorbelegung und Prüfung
        /// am OK stimmen. Der Satz bleibt als <see cref="Satz"/> stehen, für die Persistenz der
        /// Herkunft (Schritt S-F).
        /// </summary>
        internal GebaeudeImportSatz SatzAusErgebnis(GebaeudeImportErgebnis ergebnis)
        {
            if (ergebnis == null || _ablauf.Abbild == null) return null;
            GebaeudeImportSatz satz = _ablauf.Zuordnen(ergebnis.Gebaeudeindex, Klasse(ergebnis.Baualtersklasse),
                                                       ergebnis.BeheiztUebersteuert);
            foreach (GebaeudeFeldzeileDaten z in ergebnis.Zeilen ?? Array.Empty<GebaeudeFeldzeileDaten>())
            {
                if (z == null) continue;
                if (z.HerkunftSchluessel == GebaeudeHerkunftSchluessel.Manuell) satz.ManuellSetzen(z.Zielfeld, z.Wert);
                satz.HakenSetzen(z.Zielfeld, z.Haken);
            }
            satz.FolgevorgabenNachziehen();
            _satz = satz;

            // Zonierung und Bauteilvorschlag zum Ergebnis — mit Regel und Baustoffzuordnungen des
            // Dialogs — und ob das Gebäude als Zone(n) mit Bauteilen kommt.
            VorschlagBilden(ergebnis.Gebaeudeindex, Klasse(ergebnis.Baualtersklasse), ergebnis.Zonenregel,
                            ergebnis.BeheiztUebersteuert, ergebnis.Baustoffzuordnungen);
            _alsZone = ergebnis.AlsZone && !_vorschlag.Abgelehnt;
            _baustoffzuordnungen = Wirksame(ergebnis.Baustoffzuordnungen, _vorschlag);
            return satz;
        }

        // =================================================================================
        // Der Namensabgleich der Baustoffe
        // =================================================================================

        /// <summary>
        /// Katalog, Synonyme und gemerkte Zuordnungen — EINMAL je Dialog gelesen, darüber die
        /// vorgemerkten Zuordnungen anderer Importe derselben Liste (<see cref="Vorgemerkt"/>).
        /// </summary>
        private BaustoffabgleichDaten Abzug()
            => _abzug ??= BaustoffabgleichDaten.Abzug(Abgleichsquelle).MitZuordnungen(Vorgemerkt);

        /// <summary>Der Abgleich mit den Zuordnungen des Dialogs über dem Abzug — neu je Anfrage, denn er behält seine Treffer.</summary>
        internal Baustoffabgleich Abgleich(IReadOnlyDictionary<string, int?> zuordnungen)
            => new Baustoffabgleich(Abzug().MitZuordnungen(zuordnungen));

        /// <summary>
        /// Die Zuordnungen eines Ergebnisses, wie sie gespeichert werden: Schlüssel normalisiert, nur
        /// für Materialnamen, die der Vorschlag zeigt, und nur, was sich gegen den Abzug ändert —
        /// eine Zuordnung, die schon so gemerkt ist, und das Entfernen einer, die es nicht gibt,
        /// entfallen.
        /// </summary>
        internal IReadOnlyDictionary<string, int?> Wirksame(IReadOnlyDictionary<string, int?> zuordnungen, GebaeudeBauteilvorschlag vorschlag)
        {
            var wirksam = new Dictionary<string, int?>(StringComparer.Ordinal);
            if (zuordnungen == null || zuordnungen.Count == 0 || vorschlag == null) return wirksam;
            var sichtbar = new HashSet<string>(vorschlag.Materialien.Select(m => m.Normiert), StringComparer.Ordinal);
            var gemerkt = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (BaustoffNamenzuordnung z in Abzug().Anwenderzuordnungen())
            {
                string k = Baustoffabgleich.Schluessel(z.Materialname);
                if (k.Length > 0) gemerkt[k] = z.IdBaustoff;
            }
            foreach (KeyValuePair<string, int?> paar in zuordnungen)
            {
                string k = Baustoffabgleich.Schluessel(paar.Key);
                if (k.Length == 0 || !sichtbar.Contains(k)) continue;
                bool bekannt = gemerkt.TryGetValue(k, out int id);
                if (paar.Value is int neu ? bekannt && id == neu : !bekannt) continue;
                wirksam[k] = paar.Value;
            }
            return wirksam;
        }

        /// <summary>
        /// Der Abschnitt „Baustoffe" als Daten des Dialogs: je Materialname des Vorschlags eine Zeile
        /// (Stufe, Baustoff, Stoffwerte, woher die Werte kommen, gelb ohne Treffer), die
        /// Zusammenfassung und die Klappliste des Katalogs; <c>null</c>, wenn die Datei keine
        /// Materialnamen trägt.
        /// </summary>
        internal GebaeudeBaustoffeDaten BaustoffeDaten(GebaeudeBauteilvorschlag v, IReadOnlyDictionary<string, int?> zuordnungen)
        {
            if (v == null || v.Materialien.Count == 0) return null;
            IReadOnlyCollection<string> gemerkt = Abzug().GemerkteSchluessel();
            var dialog = new HashSet<string>((zuordnungen ?? new Dictionary<string, int?>()).Keys.Select(Baustoffabgleich.Schluessel),
                                             StringComparer.Ordinal);
            return new GebaeudeBaustoffeDaten
            {
                Zusammenfassung = GebaeudeZuordnungsModell.BaustoffZusammenfassung(v.Materialien),
                Zeilen = v.Materialien.Select(m => new GebaeudeMaterialzeileDaten
                {
                    Name = m.Name,
                    Schluessel = m.Normiert,
                    Schichten = m.Schichten,
                    Abgleich = GebaeudeZuordnungsModell.AbgleichText(m.Treffer),
                    AbgleichSchluessel = GebaeudeZuordnungsModell.AbgleichSchluessel(m.Treffer),
                    Beleg = GebaeudeZuordnungsModell.BelegText(m.Treffer?.Beleg),
                    IdBaustoff = m.Treffer?.Baustoff?.ID,
                    Baustoff = GebaeudeZuordnungsModell.BaustoffText(m.Treffer?.Baustoff),
                    Stoffwerte = m.Treffer?.Baustoff == null ? "" : GebaeudeZuordnungsModell.StoffwerteText(m.Treffer.Baustoff),
                    Werte = GebaeudeZuordnungsModell.MaterialwerteText(m),
                    OhneTreffer = GebaeudeZuordnungsModell.IstOhneTreffer(m),
                    Gemerkt = gemerkt.Contains(m.Normiert),
                    Vorgemerkt = dialog.Contains(m.Normiert),
                }).ToList(),
                Katalog = Katalog(),
            };
        }

        /// <summary>
        /// Die Klappliste der Katalogbaustoffe, gruppiert nach <c>Gruppe</c> in der Reihenfolge ihres
        /// ersten Auftretens (Katalog nach Id); je Gruppe die herstellerneutralen Zeilen zuerst, dann
        /// die Herstellerzeilen mit dem Hersteller im Text; ohne Gruppe zuletzt. Einmal je Dialog.
        /// </summary>
        internal IReadOnlyList<GebaeudeBaustoffgruppe> Katalog()
        {
            if (_katalog != null) return _katalog;
            List<BaustoffModel> stoffe = Abzug().Baustoffe().Where(b => b != null).OrderBy(b => b.ID).ToList();
            var gruppen = new List<GebaeudeBaustoffgruppe>();
            foreach (IGrouping<string, BaustoffModel> g in stoffe
                         .GroupBy(b => string.IsNullOrWhiteSpace(b.Gruppe) ? null : b.Gruppe.Trim())
                         .OrderBy(g => g.Key == null ? 1 : 0)
                         .ThenBy(g => g.Min(b => string.IsNullOrWhiteSpace(b.Hersteller) ? b.ID : int.MaxValue))
                         .ThenBy(g => g.Min(b => b.ID)))
            {
                List<GebaeudeBaustoffwahl> eintraege = g
                    .OrderBy(b => string.IsNullOrWhiteSpace(b.Hersteller) ? 0 : 1)
                    .ThenBy(b => b.ID)
                    .Select(b => new GebaeudeBaustoffwahl(b.ID, GebaeudeZuordnungsModell.BaustoffText(b)))
                    .ToList();
                gruppen.Add(new GebaeudeBaustoffgruppe(g.Key ?? MyResource.Resource.GIMP_BS_OHNE_GRUPPE, eintraege));
            }
            return _katalog = gruppen;
        }

        /// <summary>
        /// Das Ergebnis mit nachgezogenen Folgevorgaben: Jede Zeile, die im Dialog noch die Herkunft
        /// „Vorgabe" trägt, nimmt Wert und Beleg aus dem neu gebauten Satz (<see cref="SatzAusErgebnis"/>)
        /// — so stimmt eine Vorgabe, die von einer Handänderung abhängt (innere Gewinne, Nachtsollwert),
        /// auch dann, wenn der Dialog sie noch nicht neu zugeordnet hat. Der Haken bleibt der des Dialogs.
        /// </summary>
        internal GebaeudeImportErgebnis Nachgezogen(GebaeudeImportErgebnis ergebnis)
        {
            GebaeudeImportSatz satz = SatzAusErgebnis(ergebnis);
            if (satz == null) return ergebnis;
            string vorgabe = GebaeudeZuordnungsModell.HerkunftSchluessel(Importherkunft.Vorgabe);
            List<GebaeudeFeldzeileDaten> zeilen = (ergebnis.Zeilen ?? Array.Empty<GebaeudeFeldzeileDaten>())
                .Select(z => z != null && z.HerkunftSchluessel == vorgabe
                             && satz.Zeile(z.Zielfeld) is GebaeudeFeldzeile s && ImportherkunftWerte.IstVorgabe(s.Herkunft)
                             && !Nullable.Equals(s.Wert, z.Wert)
                    ? ZeileDaten(s) with { Haken = z.Haken }
                    : z)
                .ToList();
            return ergebnis with { Zeilen = zeilen };
        }

        /// <summary>Der Klassenbuchstabe zum Index der Klappliste (0 = A … 12 = M, E47); außerhalb <c>null</c>.</summary>
        private static char? Klasse(int? index)
            => index is int i && i >= 0 && i < GebaeudeVorgaben.Alle.Count ? GebaeudeStammCtrl.KlassenBuchstabe(i) : (char?)null;

        // =================================================================================
        // Übersetzung Kern → DTO — die Texte kommen aus dem Zuordnungsmodell
        // =================================================================================

        private static GebaeudeFeldzeileDaten ZeileDaten(GebaeudeFeldzeile z) => new GebaeudeFeldzeileDaten
        {
            Zielfeld = z.Zielfeld,
            Gruppe = GebaeudeZuordnungsModell.GruppenText(z.Gruppe),
            Feld = GebaeudeZuordnungsModell.FeldText(z.Zielfeld),
            Wert = z.Wert,
            Textwert = z.Textwert,
            WertText = GebaeudeZuordnungsModell.WertText(z),
            Einheit = z.Einheit,
            Beleg = GebaeudeZuordnungsModell.BelegText(z.Beleg),
            Vorgabe = z.VorgabeWert.HasValue ? GebaeudeZuordnungsModell.ZahlText(z.VorgabeWert) : "",
            VorgabeBeleg = GebaeudeZuordnungsModell.BelegText(z.VorgabeBeleg),
            HerkunftText = GebaeudeZuordnungsModell.HerkunftText(z.Herkunft),
            HerkunftSchluessel = GebaeudeZuordnungsModell.HerkunftSchluessel(z.Herkunft),
            Haken = z.Uebernehmen,
            Markierung = z.Markierung == PruefStufe.Fehler ? GebaeudeZeilenmarkierung.Rot
                       : z.Markierung == PruefStufe.Warnung ? GebaeudeZeilenmarkierung.Gelb
                       : GebaeudeZeilenmarkierung.Keine,
            Eingebbar = z.Eingebbar,
            HakenSetzbar = z.HakenSetzbar,
        };

        private static GebaeudeRaumzeileDaten RaumDaten(GebaeudeRaumzeile r)
            => new GebaeudeRaumzeileDaten(
                r.Kennung,
                r.Anzeigename,
                r.FlaecheM2.HasValue ? GebaeudeZuordnungsModell.ZahlText(r.FlaecheM2) + " m²" : GebaeudeZuordnungsModell.ZahlText(null),
                r.Beheizt,
                r.BeheiztLautDatei,
                GebaeudeZuordnungsModell.RaumGrundText(r),
                r.Uebersteuert);

        private static GebaeudeImportMeldung MeldungDaten(PruefMeldung m)
            => new GebaeudeImportMeldung(
                m.Stufe == PruefStufe.Fehler ? WarnStufe.Fehler : m.Stufe == PruefStufe.Warnung ? WarnStufe.Warnung : WarnStufe.Hinweis,
                GebaeudeZuordnungsModell.StufeText(m.Stufe),
                GebaeudeZuordnungsModell.MeldungText(m),
                m.Schluessel);

        private static string Text(PruefMeldung m) => GebaeudeZuordnungsModell.MeldungText(m);

        // =================================================================================
        // Der Bauteilvorschlag als Daten des Abschnitts „Bauteile (echte Hülle)" (Stufe G4b)
        // =================================================================================

        /// <summary>
        /// Der Bauteilvorschlag als Daten des Dialogs: übernehmbar oder mit Grund abgelehnt, die
        /// Kopfzeile der Zone, die Zeilen, die Zeile zur inneren Masse und die Meldungen; <c>null</c>
        /// ohne Vorschlag.
        /// </summary>
        internal static GebaeudeBauteileDaten BauteileDaten(GebaeudeBauteilvorschlag v)
        {
            if (v == null) return null;
            ZoneModel zone = v.Zone;
            return new GebaeudeBauteileDaten
            {
                Moeglich = !v.Abgelehnt,
                Ablehnung = v.Abgelehnt ? Ablehnungstext(v) : "",
                Kopftext = zone == null ? ""
                    : v.Mehrzonig
                    ? Formatieren(MyResource.Resource.GIMP_BT_KOPF_ZONEN, v.Zonen.Count,
                                  GebaeudeZuordnungsModell.ZahlText(v.Zonen.Sum(z => z.Nutzflaeche ?? 0.0)), v.Zeilen.Count, v.Aufbauten.Count)
                    : Formatieren(MyResource.Resource.GIMP_BT_KOPF, zone.Bezeichner, GebaeudeZuordnungsModell.ZahlText(zone.Nutzflaeche),
                                  v.Zeilen.Count, v.Aufbauten.Count),
                Zeilen = v.Zeilen.Select(BauteilzeileDaten).ToList(),
                Innenweg = zone == null ? "" : InnenwegText(v),
                Meldungen = v.Meldungen.Select(MeldungDaten).ToList(),
            };
        }

        /// <summary>Der Grund, warum sich ein Vorschlag nicht übernehmen lässt: seine Fehler, sonst ein allgemeiner Satz.</summary>
        private static string Ablehnungstext(GebaeudeBauteilvorschlag v)
        {
            string fehler = v == null ? "" : string.Join(" ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler).Select(Text));
            return fehler.Length > 0 ? fehler : MyResource.Resource.GIMP_BT_NICHT_MOEGLICH;
        }

        /// <summary>Eine Bauteilzeile als Anzeigetexte; die Herkunft der Zeile ist die ihrer Werte (Datei oder Vorgabe).</summary>
        private static GebaeudeBauteilzeileDaten BauteilzeileDaten(GebaeudeBauteilzeile z)
        {
            BauteilModel b = z.Bauteil;
            string leer = MyResource.Resource.GIMP_WERT_LEER;
            Importherkunft herkunft = GebaeudeZuordnungsModell.HerkunftAusSchluessel(b.Herkunft);
            return new GebaeudeBauteilzeileDaten(
                b.Bezeichner ?? "",
                BauteilaufbauCtrl.BauteilartText(b.Bauteilart),
                GebaeudeZuordnungsModell.ZahlText(b.Flaeche) + " m²",
                b.U_Wert.HasValue ? GebaeudeZuordnungsModell.ZahlText(b.U_Wert) + " W/(m²K)"
                    : b.ID_Aufbau.HasValue ? MyResource.Resource.GIMP_BT_AUS_SCHICHTEN : leer,
                b.Azimut.HasValue ? AzimutText(b.Azimut.Value) : leer,
                b.Neigung.HasValue ? GebaeudeZuordnungsModell.ZahlText(b.Neigung) + "°" : leer,
                RandText(b.Randbedingung),
                GebaeudeZuordnungsModell.HerkunftText(herkunft),
                GebaeudeZuordnungsModell.HerkunftSchluessel(herkunft),
                z.Kennung);
        }

        /// <summary>
        /// Der Azimut als Anzeigetext, auf eine Nachkommastelle gerundet (ein gedrehter Lageplan gibt
        /// Werte wie 63,435°). Nur die Anzeige: Das Bauteil behält den Wert der Datei. Was auf 360°
        /// rundet, zeigt 0°, und eine gerundete Null trägt kein Vorzeichen.
        /// </summary>
        internal static string AzimutText(double azimut)
        {
            double gerundet = Math.Round(azimut, 1, MidpointRounding.AwayFromZero);
            if (gerundet >= 360) gerundet -= 360;
            return (gerundet + 0.0).ToString("0.#", CultureInfo.CurrentCulture) + "°";
        }

        /// <summary>Die Randbedingung einer Zeile als Anzeigetext — leer heißt an Innenwand und Decke „innerhalb der Zone".</summary>
        internal static string RandText(string rand)
        {
            switch (rand)
            {
                case null: return MyResource.Resource.GIMP_BT_RAND_INNEN;
                case DbWerte.RANDBEDINGUNG_AUSSENLUFT: return MyResource.Resource.BTDLG_RAND_AUSSENLUFT;
                case DbWerte.RANDBEDINGUNG_ERDREICH: return MyResource.Resource.BTDLG_RAND_ERDREICH;
                case DbWerte.RANDBEDINGUNG_UNBEHEIZT: return MyResource.Resource.BTDLG_RAND_UNBEHEIZT;
                case DbWerte.RANDBEDINGUNG_ZONE: return MyResource.Resource.BTDLG_RAND_ZONE;
                default: return rand;
            }
        }

        /// <summary>Die Zeile zur inneren Masse: Innenbauteile, Innenflächenfaktor aus der Datei oder Vorgabe.</summary>
        private static string InnenwegText(GebaeudeBauteilvorschlag v)
        {
            switch (v.Innenweg)
            {
                case Innenweg.Bauteile:
                    return Formatieren(MyResource.Resource.GIMP_BT_INNENWEG_BAUTEILE, v.Zeilen.Count(z => z.Summenfeld == null),
                                       GebaeudeZuordnungsModell.ZahlText(Math.Round(v.FlaecheInnen, 2)));
                case Innenweg.Innenflaechenfaktor:
                    return Formatieren(MyResource.Resource.GIMP_BT_INNENWEG_FAKTOR,
                                       GebaeudeZuordnungsModell.ZahlText(Math.Round(v.Innenflaechenfaktor ?? 0.0, 3)));
                default:
                    return Formatieren(MyResource.Resource.GIMP_BT_INNENWEG_VORGABE,
                                       GebaeudeZuordnungsModell.ZahlText(GebaeudeFestwerte.VORGABE_INNENFLAECHENFAKTOR));
            }
        }

        /// <summary>Übersetzt die Fortschrittsschritte des Ablaufs in Anzeigetexte der Komponente.</summary>
        private sealed class Fortschrittsbruecke : IProgress<ImportFortschritt>
        {
            private readonly IProgress<GebaeudeImportFortschritt> _ziel;

            internal Fortschrittsbruecke(IProgress<GebaeudeImportFortschritt> ziel) => _ziel = ziel;

            public void Report(ImportFortschritt wert)
                => _ziel.Report(new GebaeudeImportFortschritt(wert.Anteil, GebaeudeZuordnungsModell.FortschrittText(wert)));
        }

        // =================================================================================
        // Die Anbindung im Gebäudedialog — ausstehende Herkunft und vorbelegter Editor
        // =================================================================================

        /// <summary>
        /// <b>Die ausstehende Herkunft</b> des zuletzt geprüften bzw. übernommenen Ergebnisses:
        /// die Quelle des Laufs und die Paarungen des Einzonenwegs
        /// (<see cref="GebaeudeImportCtrl.Einzonenpaarungen"/>), dazu der Bauteilvorschlag, wenn das
        /// Gebäude als Zone mit Bauteilen kommt (<see cref="AlsZone"/>); <c>null</c> ohne Satz. Sie
        /// reist an der neuen Projektzeile, bis die Gebäudeliste gespeichert wird — dann schreibt
        /// <c>WizardCtrl.GebaeudeZuordnungAnlegen</c> Zone, Bauteile, Aufbauten und Herkunft in einem
        /// Vorgang. Plattformfrei: derselbe Weg unter Windows und iOS.
        ///
        /// <para>Die Zuordnungen der Baustoffe (<see cref="Baustoffzuordnungen"/>) reisen immer mit, auch
        /// ohne Bauteilvorschlag: Sie gelten für das Projekt, nicht für das eine Gebäude, und ein
        /// späterer Import mit Bauteilen trifft die Namen dann über die gemerkte Zuordnung.</para>
        /// </summary>
        internal GebaeudeImportHerkunft Herkunft
            => _satz == null || Quelle == null ? null
             : new GebaeudeImportHerkunft(QuelleDesLaufs, GebaeudeImportCtrl.Einzonenpaarungen(_satz), _alsZone ? _vorschlag : null,
                                          _baustoffzuordnungen.Count > 0 ? _baustoffzuordnungen : null);

        /// <summary>
        /// Die Quelle, wie sie gemerkt wird: Kommt das Gebäude mit mehreren Zonen (Stufe G6c), trägt sie die
        /// Zonenregel, nach der sie gebildet sind (<c>Tab_Importquelle.Zonenregel</c>); sonst die des Profils.
        /// </summary>
        private GebaeudeQuelle QuelleDesLaufs
        {
            get
            {
                GebaeudeQuelle q = Quelle;
                if (q == null || !_alsZone || _vorschlag?.Mehrzonig != true
                    || string.Equals(q.Zonenregel, _vorschlag.Zonierung.Regel, StringComparison.Ordinal))
                    return q;
                return new GebaeudeQuelle(q.Format, q.Dateiname, q.Hash, q.Groesse, q.Schemastand, q.Zeitpunkt,
                                          q.Programmfassung, _vorschlag.Zonierung.Regel, q.FehlendeEntitaeten);
            }
        }

        /// <summary>Die Herleitungszeile des vorbelegten Editors: „Vorbelegt aus dem Import: Datei …, Format …"; ohne Lauf leer.</summary>
        internal string Vorbelegungstext
            => Quelle == null ? ""
             : Formatieren(MyResource.Resource.GIMP_VORBELEGT, Quelle.Dateiname, GebaeudeZuordnungsModell.FormatText(_ablauf.Profil));

        /// <summary>
        /// <b>Der vorbelegte Satz des Gebäudeeditors im Modus Neu</b>: <see cref="NachKatalogdaten"/>
        /// auf den Vorgabedaten eines neuen Gebäudes — denselben, mit denen der Editor im Modus Neu
        /// öffnet (<c>GebaeudeKatalogHuelle.AusModell(new GebaeudeModel())</c>) —, dazu die
        /// Herleitungszeile samt den übernommenen Vorgaben (<see cref="Vorgabentext"/>): Der Editor
        /// zeigt keine Herkunft je Feld, an dieser Zeile bleibt eine Vorgabe als Vorgabe erkennbar.
        /// </summary>
        internal GebaeudeVorbelegung Vorbelegung(GebaeudeImportErgebnis ergebnis)
        {
            // Die Folgevorgaben (innere Gewinne, Nachtsollwert) nach den Handänderungen des Dialogs.
            ergebnis = Nachgezogen(ergebnis);
            string vorgaben = Vorgabentext(ergebnis);
            return new GebaeudeVorbelegung(NachKatalogdaten(GebaeudeKatalogHuelle.AusModell(new GebaeudeModel()), ergebnis),
                                           vorgaben.Length == 0 ? Vorbelegungstext : Vorbelegungstext + " " + vorgaben);
        }

        /// <summary>
        /// Die übernommenen Vorgaben eines Ergebnisses als ein Satz: „Vorgaben, nicht aus der Datei:
        /// Luftwechselrate 0,7 1/h; …" — jede Zeile mit Haken und Herkunft Vorgabe, mit Feld, Wert und
        /// Einheit in der Anzeigekultur; ohne solche Zeile leer.
        /// </summary>
        internal static string Vorgabentext(GebaeudeImportErgebnis ergebnis)
        {
            string vorgabe = GebaeudeZuordnungsModell.HerkunftSchluessel(Importherkunft.Vorgabe);
            List<string> teile = (ergebnis?.Zeilen ?? Array.Empty<GebaeudeFeldzeileDaten>())
                .Where(z => z != null && z.Haken && z.HerkunftSchluessel == vorgabe)
                .Select(z => z.Textwert != null
                    ? z.Feld + " (" + z.WertText + ")"
                    : (z.Feld + " " + GebaeudeZuordnungsModell.ZahlText(z.Wert) + " " + z.Einheit).Trim())
                .ToList();
            return teile.Count == 0 ? "" : Formatieren(MyResource.Resource.GIMP_VORBELEGT_VORGABEN, string.Join("; ", teile));
        }

        // =================================================================================
        // Der Weg „als Katalogsatz ablegen" — Abbildung auf den Gebäudeeditor
        // =================================================================================

        /// <summary>
        /// <b>Bildet ein Ergebnis auf die Felder des Gebäudeeditors ab</b> — der Weg „als
        /// Katalogsatz ablegen": Der Editor öffnet danach vorbelegt im Modus Neu und schreibt auf
        /// seinem eigenen OK-Weg. <paramref name="grundlage"/> bleibt unberührt (tiefe Kopie).
        ///
        /// <para><b>Jede Zeile MIT Haken setzt ihr Feld</b>, auch leer (eine leere Anschlusslänge
        /// wird <c>null</c>); eine Zeile OHNE Haken lässt die Grundlage stehen. Kein Feld wird
        /// geraten: Prüfgrößen (Volumen) und die gesamte Fensterfläche haben kein Feld, die
        /// Summe Ost + West entsteht nach der Regel des Editors
        /// (<see cref="Gebaeudehuellbilanz.FensterOstWest"/>). Die Bauweise folgt der Bauart
        /// (<see cref="Gebaeudebauweise.BauweiseAusBauart"/>, dieselbe Rechnung, die der Editor im
        /// Modus Neu vor dem Speichern fährt), außer eine übernommene Bauweise-Zeile trägt einen
        /// eigenen Wert. Der Name des neuen Gebäudes kommt aus dem Ergebnis, sobald er nicht leer ist.</para>
        /// </summary>
        internal static GebaeudeKatalogDaten NachKatalogdaten(GebaeudeKatalogDaten grundlage, GebaeudeImportErgebnis ergebnis)
        {
            GebaeudeKatalogDaten d = (grundlage ?? new GebaeudeKatalogDaten()).Kopie();
            if (ergebnis == null) return d;
            if (!string.IsNullOrWhiteSpace(ergebnis.Gebaeudename)) d.Name = ergebnis.Gebaeudename.Trim();

            bool bauartGesetzt = false, ostWestBeruehrt = false;
            double? eigeneBauweise = null;

            foreach (GebaeudeFeldzeileDaten z in ergebnis.Zeilen ?? Array.Empty<GebaeudeFeldzeileDaten>())
            {
                if (z == null || !z.Haken) continue;
                double? w = z.Wert;
                switch (z.Zielfeld)
                {
                    // ---- Kenngrößen
                    case GebaeudeZielfelder.NUTZFLAECHE: d.WohnflaecheGesamt = w; break;
                    case GebaeudeZielfelder.RAUMHOEHE: d.Raumhoehe = w; break;
                    case GebaeudeZielfelder.FLAECHE_JE_NUTZER: d.FlaecheNutzer = w; break;
                    case GebaeudeZielfelder.INNERE_GEWINNE: d.Waermegewinne = w; break;
                    case GebaeudeZielfelder.BAUALTERSKLASSE:
                        // Die Klasse des Satzes (E47: das Baujahr der Datei führt), sonst die gewählte.
                        if (!string.IsNullOrEmpty(z.Textwert)) d.Baualtersklasse = GebaeudeStammCtrl.KlassenIndex(z.Textwert);
                        else if (ergebnis.Baualtersklasse is int k && k >= 0 && k < GebaeudeVorgaben.Alle.Count) d.Baualtersklasse = k;
                        break;
                    case GebaeudeZielfelder.BAUJAHR: d.Baujahr = Jahr(w); break;
                    case GebaeudeZielfelder.BAUART:
                        int bauart = GebaeudeZielfelder.BauartIndex(z.Textwert);
                        if (bauart >= 0) { d.Bauart = bauart; bauartGesetzt = true; }
                        break;
                    case GebaeudeZielfelder.BAUWEISE:
                        if (w.HasValue) eigeneBauweise = w;
                        break;
                    // Der Innenflächenfaktor hat ein Feld im Editor (Gruppe Gebäudemodell); leer = Vorgabe 2,5.
                    case GebaeudeZielfelder.INNENFLAECHENFAKTOR: d.Innenflaechenfaktor = w; break;

                    // ---- Außenwand, Fenster, Dach, Grund, Sonstige
                    case GebaeudeZielfelder.FLAECHE_AUSSENWAND: d.FlaecheAussenwand = w; break;
                    case GebaeudeZielfelder.U_AUSSENWAND: d.UWertAussenwand = w; break;
                    case GebaeudeZielfelder.FENSTER_NORD: d.FensterflaecheNord = w; break;
                    case GebaeudeZielfelder.FENSTER_OST: d.FensterflaecheOst = w; ostWestBeruehrt = true; break;
                    case GebaeudeZielfelder.FENSTER_SUED: d.FensterflaecheSued = w; break;
                    case GebaeudeZielfelder.FENSTER_WEST: d.FensterflaecheWest = w; ostWestBeruehrt = true; break;
                    case GebaeudeZielfelder.U_FENSTER: d.UWertFenster = w; break;
                    case GebaeudeZielfelder.G_WERT: d.Fensterdurchlassgrad = w; break;
                    case GebaeudeZielfelder.FLAECHE_DACH: d.Dachflaeche = w; break;
                    case GebaeudeZielfelder.U_DACH: d.UWertDachflaeche = w; break;
                    case GebaeudeZielfelder.FLAECHE_GRUND: d.Grundflaeche = w; break;
                    case GebaeudeZielfelder.U_GRUND: d.UWertGrundflaeche = w; break;
                    case GebaeudeZielfelder.GRUND_RANDBEDINGUNG:
                        if (!string.IsNullOrEmpty(z.Textwert)) d.GrundflaecheRandbedingung = z.Textwert;
                        break;
                    case GebaeudeZielfelder.FLAECHE_SONSTIGE: d.SonstigeFlaechen = w; break;
                    case GebaeudeZielfelder.U_SONSTIGE: d.UWertSonstiges = w; break;

                    // ---- Wärmebrücken: ψ und Anschlusslängen (leer bleibt leer)
                    case GebaeudeZielfelder.PSI_FENSTER_WAND: d.WbvkFensterWand = w; break;
                    case GebaeudeZielfelder.PSI_WAND_DACH: d.WbvkWandDach = w; break;
                    case GebaeudeZielfelder.PSI_AUSSENWAND_KELLER: d.WbvkAussenwandKeller = w; break;
                    case GebaeudeZielfelder.LAENGE_FENSTER_WAND: d.AnschlussFensterWand = w; break;
                    case GebaeudeZielfelder.LAENGE_WAND_DACH: d.AnschlussWandDach = w; break;
                    case GebaeudeZielfelder.LAENGE_AUSSENWAND_KELLER: d.AnschlussAussenwandKeller = w; break;

                    // ---- Lüftung (D12) und Sollwert
                    case GebaeudeZielfelder.LUFTWECHSELRATE: d.Luftwechselrate = w; break;
                    case GebaeudeZielfelder.LUFTWECHSEL_INFILTRATION: d.LuftwechselInfiltration = w; break;
                    case GebaeudeZielfelder.LUFTWECHSEL_NUTZER: d.LuftwechselNutzer = w; break;
                    case GebaeudeZielfelder.SOLL_TAG: d.SollTag = w; break;
                    case GebaeudeZielfelder.SOLL_NACHT: d.NachtAbsenkung = w; break;
                    case GebaeudeZielfelder.NACHT_BEGINN: d.NachtBeginn = Ganz(w); break;
                    case GebaeudeZielfelder.NACHT_ENDE: d.NachtEnde = Ganz(w); break;

                    // VOLUMEN (Prüfgröße) und FENSTER_GESAMT (abgeleitet) haben kein Feld.
                }
            }

            if (ostWestBeruehrt)
            {
                double? ostWest = Gebaeudehuellbilanz.FensterOstWest(d.FensterflaecheOst, d.FensterflaecheWest, d.FensterflaecheOstWest);
                if (ostWest.HasValue) d.FensterflaecheOstWest = ostWest;
            }

            if (eigeneBauweise.HasValue) d.Bauweise = eigeneBauweise.Value;
            else if (bauartGesetzt) d.Bauweise = Gebaeudebauweise.BauweiseAusBauart(d.Bauart, d.WohnflaecheGesamt ?? 0);
            return d;
        }

        /// <summary>
        /// Die Jahreszahl einer Baujahr-Zeile als Ganzzahl; <c>null</c> bleibt <c>null</c>. Die Prüfung am
        /// OK des Zuordnungsdialogs lässt nur ganze Jahre im Bereich der Spalte durch
        /// (<c>IMP_GEB_PROT_BAUJAHR_UNGUELTIG</c>); was ohne sie hierher kommt, wird gerundet, und der
        /// Editor hält einen Wert außerhalb des Bereichs mit seiner eigenen Regel an.
        /// </summary>
        private static int? Jahr(double? wert) => Ganz(wert);

        /// <summary>
        /// Eine Ganzzahlzeile (Baujahr, Beginn und Ende der Nachtzeit) als Ganzzahl; <c>null</c> bleibt
        /// <c>null</c>. Die Prüfung am OK lässt nur ganze Werte im Bereich der Spalte durch; was ohne sie
        /// hierher kommt, wird gerundet, und der Editor hält einen Wert außerhalb mit seiner Regel an.
        /// </summary>
        private static int? Ganz(double? wert)
            => wert is double j && j >= int.MinValue && j <= int.MaxValue ? (int)Math.Round(j) : (int?)null;
    }

    /// <summary>
    /// <b>Vorbelegte Daten des Gebäudeeditors</b> (Stufe G4, Welle 4): der Feldsatz, mit dem der
    /// Editor im Modus Neu öffnet, und die leise Herleitungszeile, die sagt, woher er stammt.
    /// Der optionale Parameter von <c>GebaeudeKatalogHuelle.Gaben</c>; <c>null</c> dort heißt „wie
    /// bisher".
    /// </summary>
    /// <param name="Daten">Der vorbelegte Feldsatz.</param>
    /// <param name="Herleitung">Die Herleitungszeile im Editor; leer = keine.</param>
    internal sealed record GebaeudeVorbelegung(GebaeudeKatalogDaten Daten, string Herleitung);
}
