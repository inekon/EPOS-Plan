using System;
using System.Collections.Generic;
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
    /// <para><b>Geschrieben wird hier nichts</b> — in dieser Welle weder Katalogsatz noch
    /// Herkunftsdaten. Der Schreibweg ist ein Delegat des WIRTS (<see cref="Gaben"/>,
    /// <c>uebernehmen</c>); ohne ihn ist OK im Dialog weich gesperrt. Für den Weg „als
    /// Katalogsatz ablegen" bildet <see cref="NachKatalogdaten"/> das Ergebnis auf die Felder
    /// des Gebäudeeditors ab, und <see cref="Quelle"/>, <see cref="Quellzuordnungen"/> und
    /// <see cref="Satz"/> stehen für die spätere Persistenz bereit (Schritt S-F).</para>
    /// </summary>
    internal sealed class GebaeudeImportHuelle
    {
        private readonly GebaeudeImportAblauf _ablauf = new GebaeudeImportAblauf();
        private GebaeudeImportSatz _satz;

        /// <summary>Die Hülle des gbXML-Imports — die einzige Ausprägung in G4c.</summary>
        internal GebaeudeImportHuelle() : this(new GbxmlImportProfil())
        {
        }

        /// <summary>Die Hülle eines Profils; die Größengrenze wird für die laufende Plattform belegt.</summary>
        internal GebaeudeImportHuelle(GebaeudeImportProfil profil)
        {
            Profil = profil ?? throw new ArgumentNullException(nameof(profil));
            Profil.MaxBytes = Profil.GrenzeFuerPlattform(OperatingSystem.IsIOS());
        }

        /// <summary>Das Profil des Laufs samt Plattformgrenze.</summary>
        internal GebaeudeImportProfil Profil { get; }

        /// <summary>Die Quelle des letzten gelesenen Laufs (Dateiname, SHA-256, Größe, Schema …); <c>null</c> ohne.</summary>
        internal GebaeudeQuelle Quelle => _ablauf.Quelle;

        /// <summary>Der Satz der letzten Zuordnung bzw. Prüfung — mit den Handänderungen des Dialogs; <c>null</c> ohne.</summary>
        internal GebaeudeImportSatz Satz => _satz;

        /// <summary>Die Paarungen Quellentität ↔ Gebäude des gewählten Gebäudes für <c>Tab_Importzuordnung</c>.</summary>
        internal IReadOnlyList<GebaeudeQuellzuordnung> Quellzuordnungen
            => _satz?.Quellzuordnungen ?? (IReadOnlyList<GebaeudeQuellzuordnung>)Array.Empty<GebaeudeQuellzuordnung>();

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

        /// <summary>Was das Format ausmacht, als Daten für die Komponente.</summary>
        internal GebaeudeImportProfilDaten ProfilDaten()
            => new GebaeudeImportProfilDaten(
                GebaeudeZuordnungsModell.FormatText(Profil),
                Profil.Dateifilter,
                Profil.MaxBytes > 0 ? GebaeudeZuordnungsModell.GroesseText(Profil.MaxBytes) : "",
                Profil.Zonierungsregeln.Select(GebaeudeZuordnungsModell.ZonenregelText).ToList(),
                Profil.HilfeSchluessel);

        // =================================================================================
        // Dateiwahl und Lesen
        // =================================================================================

        /// <summary>
        /// Der Dateiwähler der Plattform, dann die Größe gegen die Grenze des Profils — VOR dem
        /// Öffnen. <c>null</c> = abgebrochen; eine zu große oder nicht lesbare Datei kommt als
        /// benannte Ablehnung zurück.
        /// </summary>
        internal async Task<GebaeudeDateiwahl> DateiWaehlenAsync(string filter)
        {
            string pfad = await Dienste.Datei.DateiOeffnenAsync(
                MyResource.Resource.GIMP_DLG_DATEI_TITEL,
                string.IsNullOrEmpty(filter) ? Profil.Dateifilter : filter,
                null);
            if (string.IsNullOrEmpty(pfad)) return null;

            string name = GebaeudeQuelle.NurName(pfad);
            long groesse;
            try
            {
                groesse = new FileInfo(pfad).Length;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException
                                       || ex is ArgumentException || ex is NotSupportedException)
            {
                return new GebaeudeDateiwahl(pfad, name, 0,
                    Text(new PruefMeldung(PruefStufe.Fehler, Profil.Meldung("LESEFEHLER"), ex.Message)));
            }

            if (!GebaeudeImportAblauf.GroesseZulaessig(groesse, Profil))
                return new GebaeudeDateiwahl(pfad, name, groesse,
                    Text(new PruefMeldung(PruefStufe.Fehler, Profil.Meldung("ZU_GROSS"),
                        groesse.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        Profil.MaxBytes.ToString(System.Globalization.CultureInfo.InvariantCulture))));

            return new GebaeudeDateiwahl(pfad, name, groesse);
        }

        /// <summary>
        /// Liest die Datei im Arbeitsfaden (Kulturweitergabe) — der Ablauf prüft die Größe ein
        /// zweites Mal, bevor ein Byte an den Leser geht. Ein Abbruch wirft
        /// <see cref="OperationCanceledException"/> bis zur Komponente, die still aussteigt.
        /// </summary>
        internal async Task<GebaeudeLesestand> LesenAsync(string pfad, IProgress<GebaeudeImportFortschritt> melder,
                                                          CancellationToken abbruch)
        {
            _satz = null;
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
                    return _ablauf.Lesen(strom, pfad, Profil, bruecke, abbruch);
            }, abbruch);

            if (oeffnungsfehler != null)
                return new GebaeudeLesestand(false, null, Array.Empty<string>(), new[]
                {
                    MeldungDaten(new PruefMeldung(PruefStufe.Fehler, Profil.Meldung("LESEFEHLER"), oeffnungsfehler)),
                });
            return Lesestand(zahl);
        }

        private GebaeudeLesestand Lesestand(int zahl)
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
                GebaeudeZuordnungsModell.FormatText(Profil),
                GebaeudeZuordnungsModell.SchemaText(Profil, q.Schemastand),
                GebaeudeZuordnungsModell.GroesseText(q.Groesse),
                GebaeudeZuordnungsModell.ZonenregelText(q.Zonenregel));
            return new GebaeudeLesestand(true, kopf, _ablauf.Gebaeude.ToList(), meldungen);
        }

        // =================================================================================
        // Zuordnen und Prüfen
        // =================================================================================

        /// <summary>Ordnet ein Gebäude zu (Klasse, Haken der Raumliste) und baut den Stand der Zeilen.</summary>
        internal GebaeudeImportStand Zuordnen(GebaeudeZuordnungsanfrage anfrage)
        {
            if (anfrage == null || _ablauf.Abbild == null) return new GebaeudeImportStand();

            IReadOnlyDictionary<string, bool> haken = anfrage.BeheiztUebersteuert ?? new Dictionary<string, bool>();
            GebaeudeImportSatz satz = _ablauf.Zuordnen(anfrage.Gebaeudeindex, Klasse(anfrage.Baualtersklasse), haken);
            _satz = satz;

            return new GebaeudeImportStand
            {
                Kopftext = GebaeudeZuordnungsModell.KopfText(satz),
                Vorschlagsname = string.IsNullOrWhiteSpace(satz.Gebaeudename) ? satz.Gebaeudekennung : satz.Gebaeudename,
                Raeume = _ablauf.Raeume(anfrage.Gebaeudeindex, haken).Select(RaumDaten).ToList(),
                Zeilen = satz.Zeilen.Select(ZeileDaten).ToList(),
                Meldungen = satz.Meldungen.Select(MeldungDaten).ToList(),
                ManuellHerkunftText = GebaeudeZuordnungsModell.HerkunftText(Importherkunft.Manuell),
            };
        }

        /// <summary>
        /// Die Prüfung am OK — DIESELBE des Kerns (<see cref="GebaeudeImportAblauf.Pruefen"/>) auf
        /// dem Satz samt Handänderungen, Haken und dem Namen des neuen Gebäudes.
        /// </summary>
        internal IReadOnlyList<GebaeudeImportMeldung> Pruefen(GebaeudeImportErgebnis ergebnis)
        {
            GebaeudeImportSatz satz = SatzAusErgebnis(ergebnis);
            if (satz == null) return Array.Empty<GebaeudeImportMeldung>();
            return GebaeudeZuordnungsModell.Pruefe(satz, ergebnis.Gebaeudename ?? "").Select(MeldungDaten).ToList();
        }

        /// <summary>
        /// Baut den Satz eines Ergebnisses neu — dieselbe Zuordnung, darauf die Handänderungen
        /// (Herkunft „manuell") und die Haken des Dialogs. Der Satz bleibt als <see cref="Satz"/>
        /// stehen, für die Persistenz der Herkunft (Schritt S-F).
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
            _satz = satz;
            return satz;
        }

        /// <summary>Der Klassenbuchstabe zum Index der Klappliste (0 = A … 20 = U); außerhalb <c>null</c>.</summary>
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

        /// <summary>Übersetzt die Fortschrittsschritte des Ablaufs in Anzeigetexte der Komponente.</summary>
        private sealed class Fortschrittsbruecke : IProgress<ImportFortschritt>
        {
            private readonly IProgress<GebaeudeImportFortschritt> _ziel;

            internal Fortschrittsbruecke(IProgress<GebaeudeImportFortschritt> ziel) => _ziel = ziel;

            public void Report(ImportFortschritt wert)
                => _ziel.Report(new GebaeudeImportFortschritt(wert.Anteil, GebaeudeZuordnungsModell.FortschrittText(wert)));
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
                        if (ergebnis.Baualtersklasse is int k && k >= 0 && k < GebaeudeVorgaben.Alle.Count) d.Baualtersklasse = k;
                        else if (!string.IsNullOrEmpty(z.Textwert)) d.Baualtersklasse = GebaeudeStammCtrl.KlassenIndex(z.Textwert);
                        break;
                    case GebaeudeZielfelder.BAUART:
                        int bauart = GebaeudeZielfelder.BauartIndex(z.Textwert);
                        if (bauart >= 0) { d.Bauart = bauart; bauartGesetzt = true; }
                        break;
                    case GebaeudeZielfelder.BAUWEISE:
                        if (w.HasValue) eigeneBauweise = w;
                        break;

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
                    case GebaeudeZielfelder.LUFTWECHSEL_INFILTRATION: d.LuftwechselInfiltration = w; break;
                    case GebaeudeZielfelder.LUFTWECHSEL_NUTZER: d.LuftwechselNutzer = w; break;
                    case GebaeudeZielfelder.SOLL_TAG: d.SollTag = w; break;

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
    }
}
