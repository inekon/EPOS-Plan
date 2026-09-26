using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Lädt die Daten von Stamm + Varianten lesend in BerichtsDaten-DTOs
    /// (Konzept Kap. 8.2) — das aktive Projekt der App wird dabei NICHT umgeschaltet.
    /// Optional wird je Projekt vorab headless simuliert (SimulationRunner, frische
    /// Instanz je Projekt — Muster aus der Variantenseite UcBkUebersicht).
    /// Fehler eines einzelnen Projekts brechen den Lauf nicht ab (VariantenDaten.Fehler).
    ///
    /// Für einen BERICHTSLAUF (Word und/oder Excel) ist ausschließlich
    /// <see cref="SammleFuerBericht"/> der Einstieg: dort ist die Kette
    /// „frische Simulation → Wirtschaftlichkeitsrechnung → Bausteine" verbindlich
    /// (Nutzeranforderung 15.08.2026). <see cref="Sammle"/> bleibt der Einstieg für
    /// den Wirtschaftlichkeits-Reiter und den Verlaufsdialog, die anschließend selbst
    /// rechnen.
    ///
    /// <para><b>Er liegt im KERN</b> (Etappe E3, Schritt 4 — Befund P1). Bis dahin
    /// stand er in der Windows-Schale, obwohl er keine einzige WinForms-Anweisung
    /// führt: Damit war der EINE Rechenaufruf der Wirtschaftlichkeit
    /// (<c>WirtschaftlichkeitCtrl.Berechne</c> über <see cref="Sammle"/>,
    /// <c>KostenEmissionRechner.Berechne</c> über <see cref="SammleFuerBericht"/>)
    /// auf iOS nicht zu haben. Seine Quellen sind ausschließlich Kern-Controller;
    /// die einzige Naht, die er brauchte — die Brennstoffmengen aus
    /// <see cref="EnergieMengen"/> —, war selbst plattformfrei und steht jetzt
    /// daneben. Beide Schalen rufen dieselbe Stelle; einen zweiten Aufbau gibt es
    /// nicht.</para>
    /// </summary>
    public class BerichtsDatenSammler
    {
        /// <summary>Fortschrittsmeldung für Dialog/Statuszeile.</summary>
        public class Fortschritt
        {
            public string Text = "";
            public int Aktuell;
            public int Gesamt;
        }

        /// <summary>Datenlage eines Projekts für die Dialog-Anzeige (Zeitstempel, ⚠).</summary>
        public class VariantenStatus
        {
            public int IdProjekt;
            public string Projektname = "";
            public string Variantenname = "";
            public bool IstStamm;
            public DateTime? SimStand;      // null = kein Ergebnis
            public bool Veraltet;           // SimStand < Aenderungsdatum des Projekts

            public string SimStandText
            {
                get
                {
                    if (!SimStand.HasValue) return "— (fehlt) ⚠";
                    string t = SimStand.Value.ToString("dd.MM.yy HH:mm");
                    return Veraltet ? t + " ⚠" : t;
                }
            }
        }

        // ------------------------------------------------------------- Status (leichtgewichtig)

        /// <summary>
        /// Ermittelt die Datenlage der Vergleichsgruppe ohne die Ergebnisbäume zu laden
        /// (nur Zeitstempel-Abfragen) — Grundlage der Dialogliste (Konzept Kap. 3.1).
        /// </summary>
        public static List<VariantenStatus> ErmittleStatus(int idStamm, string stammName)
        {
            var liste = new List<VariantenStatus>();
            var gruppe = new VariantenCtrl().LadeGruppe(idStamm, stammName);
            foreach (VariantenCtrl.VarianteInfo vi in gruppe)
            {
                var st = new VariantenStatus
                {
                    IdProjekt = vi.IdProjekt,
                    Projektname = vi.Projektname,
                    Variantenname = vi.Variantenname,
                    IstStamm = vi.IstStamm
                };
                st.SimStand = LiesSimZeitstempel(vi.IdProjekt);
                if (st.SimStand.HasValue)
                {
                    DateTime? aend = LiesAenderungsdatum(vi.IdProjekt);
                    st.Veraltet = aend.HasValue && st.SimStand.Value < aend.Value;
                }
                liste.Add(st);
            }
            return liste;
        }

        private static DateTime? LiesSimZeitstempel(int idProjekt)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT Zeitstempel FROM " + ErgebnisCtrl.TAB_KOPF +
                    " WHERE ID_Projekt = ? ORDER BY ID DESC LIMIT 1",
                    new DbParam("@p", idProjekt));
                if (o != null && o != DBNull.Value) return Convert.ToDateTime(o);
            }
            catch { }
            return null;
        }

        private static DateTime? LiesAenderungsdatum(int idProjekt)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT Aenderungsdatum FROM Tab_Projekt WHERE ID = ?",
                    new DbParam("@p", idProjekt));
                if (o != null && o != DBNull.Value) return Convert.ToDateTime(o);
            }
            catch { }
            return null;
        }

        // ------------------------------------------------------------- Berichtslauf

        /// <summary>
        /// EINZIGER Einstieg der Berichtserzeugung — Word UND Excel arbeiten danach auf
        /// demselben <see cref="BerichtsDaten"/>-Baum (Nutzeranforderung 15.08.2026).
        ///
        /// Verbindliche Kette je Berichtslauf:
        ///  (a) jede gewählte Variante (und der Stamm) wird FRISCH simuliert und
        ///      gespeichert — <see cref="Sammle"/> mit neuRechnen = true, also über
        ///      SimulationRunner.SimuliereUndSpeichere samt Paket-8-Fehlerkanal;
        ///  (b) direkt anschließend läuft die Wirtschaftlichkeitsrechnung derselben
        ///      Vergleichsgruppe auf genau diesen frischen Ergebnissen
        ///      (<see cref="WirtschaftlichkeitCtrl.Berechne"/> — derselbe Rechenweg,
        ///      den der Reiter „Wirtschaftlichkeit" nimmt, inkl. Persistieren);
        ///  (c) erst danach sammeln die Bausteine.
        ///
        /// Der frühere Schnellpfad „Vor Ausgabe neu rechnen = aus" entfällt bewusst:
        /// ein Bericht darf nie auf veralteten Ergebnissen oder einer übersprungenen
        /// Wirtschaftlichkeitsrechnung stehen. Aufwand: n Projekte × (Simulation +
        /// Zahlungsreihen), die Wirtschaftlichkeit einmal für die ganze Gruppe.
        ///
        /// Die Wirtschaftlichkeit wird gruppenweise gerechnet, nicht je Variante:
        /// die Kennzahlen einer Variante sind Differenzen gegen den Stamm, der
        /// Rechner braucht deshalb alle Projekte in EINEM Aufruf (bestehende
        /// Rechenkette, hier nur aufgerufen).
        /// </summary>
        public BerichtsDaten SammleFuerBericht(int idStamm, string stammName, List<int> variantenIds,
                                               bool mitZeitreihen,
                                               IProgress<Fortschritt> fortschritt, CancellationToken abbruch)
        {
            return SammleFuerBericht(idStamm, stammName, variantenIds, mitZeitreihen,
                                     fortschritt, abbruch, null);
        }

        /// <summary>
        /// ETAPPE E5 (Empfehlung Q6, 22.09.2026) — derselbe Berichtslauf mit der
        /// <b>Vergleichssicht</b> (§ 2.15), die VOR der Wirtschaftlichkeitsrechnung am
        /// Berichtsbaum steht.
        ///
        /// <para><b>Der Befund.</b> Bis hierher setzte die Berichtshülle die Sicht erst NACH
        /// dem Sammeln: Gerechnet war gegen die Referenz der Gruppe, die Tafeln von Wort-
        /// und Tabellenbericht nannten in Sicht 2 aber A als Referenz — Differenzen und
        /// Tafeln sprachen von verschiedenen Ständen.</para>
        ///
        /// <para><b>Jetzt</b> gilt die Sicht schon im Rechenschritt: Der GESPEICHERTE Lauf
        /// rechnet weiter gegen die Referenz der Gruppe (aus ihm soll der Bericht
        /// reproduzierbar sein), und in Sicht 2 rechnet derselbe Rechenweg zusätzlich gegen
        /// A, ohne zu speichern — Muster der Ergebnisseite. Aus diesem Lauf entstehen
        /// Kennzahlen, Bandbreite, Sensitivität und Empfehlung des Berichts.</para>
        /// </summary>
        /// <param name="sicht">Die Vergleichssicht der Sitzung; <c>null</c> = Sicht 1.</param>
        public BerichtsDaten SammleFuerBericht(int idStamm, string stammName, List<int> variantenIds,
                                               bool mitZeitreihen,
                                               IProgress<Fortschritt> fortschritt, CancellationToken abbruch,
                                               Vergleichssicht sicht)
        {
            // BV-E3: Der Schalter ist der Zeitreihenteil des Bedarfs; Verlauf und Emissionsbilanz
            // erhebt der Lauf wie bisher, sobald ein Schreiber sie zeigt.
            return SammleFuerBericht(idStamm, stammName, variantenIds,
                                     new Berichtsbedarf((mitZeitreihen ? Vorlagenbedarf.Zeitreihen : Vorlagenbedarf.Keiner) |
                                                        Vorlagenbedarf.Verlauf | Vorlagenbedarf.Emissionsbilanz),
                                     fortschritt, abbruch, sicht);
        }

        /// <summary>
        /// ETAPPE BV-E3 (Konzept Berichtsvorlagen 5.1, 8.5) — derselbe Berichtslauf mit dem
        /// <b>Bedarf</b> des Berichts: Simulation und Wirtschaftlichkeitsrechnung laufen immer, die
        /// Stundenreihen, der Kapitalwertverlauf und die Emissionsbilanz nur, wenn der Bericht sie
        /// zeigt (<see cref="Berichtsbedarf"/>; die Hülle bildet ihn mit
        /// <see cref="Berichtsbedarf.FuerLauf"/> aus Vorlage und Häkchen).
        ///
        /// <para><b>Danach der Wertesatz.</b> Was Wirtschaftlichkeitsbaustein, Anhang E, Tabellenbericht
        /// und Formelmappe beim Schreiben aus der Datenbank lasen oder daraus rechneten, ermittelt der
        /// Lauf EINMAL nach der Wirtschaftlichkeitsrechnung über dieselben Rechenwege
        /// (<see cref="WirtschaftsBerichtswerte.Ermittle"/>) und legt es als
        /// <see cref="BerichtsDaten.Wirtschaft"/> an den Baum — Word und Excel lesen dieselben Zahlen,
        /// und der Verlauf wird einmal gerechnet statt je Ausgabe.</para>
        /// </summary>
        /// <param name="bedarf">Was der Bericht zeigt; <c>null</c> = <see cref="Berichtsbedarf.Alles"/>.</param>
        /// <param name="sicht">Die Vergleichssicht der Sitzung; <c>null</c> = Sicht 1.</param>
        public BerichtsDaten SammleFuerBericht(int idStamm, string stammName, List<int> variantenIds,
                                               Berichtsbedarf bedarf,
                                               IProgress<Fortschritt> fortschritt, CancellationToken abbruch,
                                               Vergleichssicht sicht)
        {
            Berichtsbedarf b = bedarf ?? Berichtsbedarf.Alles;

            // Der Wirtschaftlichkeitsschritt ist ein zusätzlicher Fortschrittsschritt
            // hinter den Projekten — sonst stünde der Balken schon auf 100 %, während
            // noch gerechnet wird.
            IProgress<Fortschritt> melder = fortschritt == null
                ? null : new FortschrittMitZusatz(fortschritt, 1);

            // Die Stundenreihen: was der Bericht zeigt (Bedarf) — und was die RECHNUNG braucht.
            // SP-W1: Ist am Stromträger ein Leistungspreis gepflegt, braucht die Kostenseite die
            // Reihen — ohne Bezugsspitze fällt sein Leistungsanteil aus den Energiekosten.
            // LS-E-2 (VF-1): Der Leistungspreis zählt für die GANZE Gruppe; führt ihn nur eine
            // Variante, fehlte ihr ohne Reihen der Leistungsanteil. Die Regel stand bis BV-E3 in
            // der Hülle (BerichtSeiteGaben) und gilt jetzt für jeden Aufrufer.
            bool mitZeitreihen = b.Zeitreihen ||
                                 KostenEmissionRechner.StromLeistungspreisGepflegt(idStamm, variantenIds);

            BerichtsDaten daten = Sammle(idStamm, stammName, variantenIds,
                                         true /* immer frisch simulieren */, mitZeitreihen,
                                         melder, abbruch);

            // Q6: die Sicht als Momentaufnahme VOR dem Rechnen — sie ändert sich während
            // des Berichtslaufs nicht mehr.
            if (daten != null) daten.Sicht = sicht != null ? sicht.Kopie() : null;

            RechneWirtschaftlichkeit(daten, fortschritt, abbruch);

            // BV-E3: der Wertesatz der Wirtschaftlichkeit — nach der Rechnung, mit der Sicht, der
            // Referenz und der Bewertung, die sie am Baum hinterlassen hat.
            if (daten != null)
            {
                abbruch.ThrowIfCancellationRequested();
                daten.Wirtschaft = WirtschaftsBerichtswerte.Ermittle(daten, b);
            }
            return daten;
        }

        /// <summary>
        /// BV-E3 — wie viele Zeitreihensätze dieser Sammler eingesammelt hat (Nachweis des Bedarfs:
        /// ohne Bedarf und ohne gepflegten Leistungspreis keiner).
        /// </summary>
        internal int ZeitreihenErhoben { get; private set; }

        /// <summary>
        /// Schritt (b) der Berichtskette: Wirtschaftlichkeit der gesammelten
        /// Vergleichsgruppe über den bestehenden Rechenweg
        /// (<see cref="WirtschaftlichkeitCtrl.Berechne"/>: alle Szenarien, Sensitivität,
        /// Strommatrix, Persistenz in Tab_ErgebnisWirtschaftlichkeit). Hier wird nichts
        /// nachgerechnet — nur aufgerufen und das Ergebnis am Berichtsbaum hinterlegt.
        ///
        /// Fehlerverhalten wie bei einem Simulationsfehler im Sammler: der Berichtslauf
        /// bricht NICHT ab, die betroffene Variante wird mit Namen in
        /// <see cref="BerichtsDaten.Warnungen"/> gemeldet (Abschlussmeldung des Dialogs
        /// und Anhang-Kapitel „Hinweise dieses Berichtslaufs").
        /// </summary>
        internal void RechneWirtschaftlichkeit(BerichtsDaten daten,
                                               IProgress<Fortschritt> fortschritt,
                                               CancellationToken abbruch)
        {
            if (daten == null || daten.Varianten.Count == 0) return;
            abbruch.ThrowIfCancellationRequested();

            int schritte = daten.Varianten.Count + 1;
            Melde(fortschritt, schritte, schritte, "Wirtschaftlichkeit: " + daten.Stammprojektname);

            try
            {
                var ctrl = new WirtschaftlichkeitCtrl();
                WirtschaftlichkeitParameter p = ctrl.LadeParameter(daten.IdStamm);
                daten.IdGruppenreferenz = p.IdReferenzprojekt;

                // ETAPPE E5 (Q6): Der GESPEICHERTE Lauf rechnet gegen die Referenz der
                // GRUPPE — ausdrücklich, damit eine Sicht 2 am Baum ihn nicht auf A
                // umlenkt (Berechne nimmt sonst A aus der Sicht). 0 heißt Stamm, und den
                // nennt der Aufruf beim Namen.
                int idGruppe = p.IdReferenzprojekt > 0 ? p.IdReferenzprojekt : daten.IdStamm;
                List<SensitivitaetZeile> sens;
                daten.Wirtschaftlichkeit = ctrl.Berechne(daten, p, idGruppe, true, out sens)
                                           ?? new List<WirtschaftlichkeitErgebnis>();

                // In Sicht 2 rechnet DERSELBE Rechenweg gegen A, ohne zu speichern — aus
                // diesem Lauf entstehen Tafeln, Bandbreite und Empfehlung des Berichts, damit
                // Differenzen und Tafeln dieselbe Referenz nennen. Die Gruppenrechnung oben
                // bleibt der gebuchte Stand.
                if (daten.Sicht != null && daten.Sicht.IstPaar && daten.Wirtschaftlichkeit.Count > 0)
                {
                    List<SensitivitaetZeile> sensPaar;
                    List<WirtschaftlichkeitErgebnis> paar = ctrl.Berechne(
                        daten, p, daten.Sicht.Referenz, false, out sensPaar);
                    if (paar != null && paar.Count > 0)
                    {
                        daten.Wirtschaftlichkeit = paar;
                        sens = sensPaar;
                    }
                }

                // ETAPPE E5: die BEWERTUNG dieses Laufs — Bandbreite mit Einstufungen,
                // Vorschlagssatz, Hinweistext, Deklarationen, Nutzungsdauer-Hinweise, die
                // Stände ohne Nachweis und die Sensitivität — aus denselben Kernmethoden,
                // die die Hülle der Seite ruft, und gegen DIESELBE Referenz wie die Zahlen
                // (Sicht 2: A, sonst die Gruppe).
                daten.Bewertung = Bewertung(daten, daten.Wirtschaftlichkeit, p, sens);

                if (daten.Wirtschaftlichkeit.Count == 0)
                    daten.Melde(null, Berichtshinweisstufe.Warnung,
                                "Wirtschaftlichkeit: die Rechnung lieferte kein Ergebnis — " +
                                "Kostenpositionen und Parameter der Vergleichsgruppe prüfen.");

                // Unvollständige Rechnungen je Projekt sichtbar machen (Szenario
                // „Erwartet" genügt — Fehlgrund/Hinweis sind szenarioübergreifend gleich).
                foreach (VariantenDaten v in daten.Varianten)
                {
                    WirtschaftlichkeitErgebnis e = null;
                    foreach (WirtschaftlichkeitErgebnis kandidat in daten.Wirtschaftlichkeit)
                        if (kandidat.IdProjekt == v.IdProjekt &&
                            kandidat.Szenario == WirtschaftlichkeitSzenario.ERWARTET)
                        { e = kandidat; break; }

                    if (e == null)
                    {
                        if (daten.Wirtschaftlichkeit.Count > 0)
                            daten.Melde(v, Berichtshinweisstufe.Warnung, "Wirtschaftlichkeit konnte nicht gerechnet werden.");
                        continue;
                    }
                    // Die Wirtschaftlichkeit fügt ihre Hinweise mit „ | " an — gegliedert wird
                    // daraus je Hinweis ein Punkt, damit gleichlautende Stände zusammenfallen.
                    if (e.Fehlgrund != null)
                        daten.Melde(v, Berichtshinweisstufe.Warnung, "Wirtschaftlichkeit unvollständig — " + e.Fehlgrund);
                    else if (e.Hinweis != null)
                        daten.Melde(v, Berichtshinweisstufe.Hinweis, "Wirtschaftlichkeit — " + e.Hinweis,
                                    e.Hinweis.Split(new[] { " | " }, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(t => "Wirtschaftlichkeit — " + t.Trim()));
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                // Gleiches Muster wie ein gescheitertes Projekt im Sammler: melden,
                // weiterlaufen. Die Bausteine fallen dann auf den persistierten Stand
                // zurück und weisen ihn als solchen aus.
                daten.WirtschaftlichkeitFehler = ex.Message;
                daten.Melde(null, Berichtshinweisstufe.Warnung,
                            "Wirtschaftlichkeit konnte für diesen Berichtslauf nicht " +
                            "berechnet werden: " + ex.Message);

                // ETAPPE E5 (Nr. 31): Die Bausteine fallen hier auf den GESPEICHERTEN
                // Stand zurück — die Bewertung folgt ihnen dorthin, und genau dort
                // tragen Zeilen ohne Umschlag ihr Kennzeichen.
                //
                // ETAPPE E5 (Q6): Der gespeicherte Stand rechnet gegen die Referenz der
                // GRUPPE. Eine Sicht 2 darüber nennte A als Referenz neben Zahlen, die
                // gegen die Gruppe gerechnet sind — genau der Befund, den Q6 behebt. Der
                // Rückfall zeigt deshalb Sicht 1; die Warnung darüber sagt, dass es der
                // gespeicherte Stand ist.
                daten.Sicht = null;
                try
                {
                    var ctrl = new WirtschaftlichkeitCtrl();
                    WirtschaftlichkeitParameter p = ctrl.LadeParameter(daten.IdStamm);
                    var ids = new List<int>();
                    foreach (VariantenDaten v in daten.Varianten) ids.Add(v.IdProjekt);
                    daten.IdGruppenreferenz = p.IdReferenzprojekt;
                    daten.Bewertung = Bewertung(daten, ctrl.LadeErgebnisse(ids), p, null);
                }
                catch { daten.Bewertung = null; }
            }
        }

        /// <summary>
        /// ETAPPE E5 — die Bewertung eines Berichtslaufs in der Berichtskultur. Ein Fehler
        /// hier kostet die Bewertung, nie den Bericht.
        /// </summary>
        /// <param name="sensitivitaet">Die Sensitivitätszeilen des Laufs; <c>null</c> = die
        /// gespeicherten (Rückfall auf den gebuchten Stand).</param>
        private static WirtschaftlichkeitBewertung Bewertung(BerichtsDaten daten,
                                                             List<WirtschaftlichkeitErgebnis> alle,
                                                             WirtschaftlichkeitParameter p,
                                                             List<SensitivitaetZeile> sensitivitaet)
        {
            try
            {
                return WirtschaftlichkeitBewertung.FuerBericht(daten, alle, p, BerichtTexte.Kultur,
                                                              sensitivitaet);
            }
            catch { return null; }
        }

        /// <summary>
        /// Reicht Fortschrittsmeldungen weiter und erhöht die Gesamtzahl um die
        /// Schritte, die nach dem Sammeln noch folgen (Wirtschaftlichkeit).
        /// Bewusst KEIN <see cref="Progress{T}"/>: der läuft im Berichtslauf ohne
        /// SynchronizationContext und stellt dann über den ThreadPool zu — die
        /// Statuszeile könnte Meldungen verdreht anzeigen.
        /// </summary>
        private sealed class FortschrittMitZusatz : IProgress<Fortschritt>
        {
            private readonly IProgress<Fortschritt> _ziel;
            private readonly int _zusatz;

            public FortschrittMitZusatz(IProgress<Fortschritt> ziel, int zusatz)
            { _ziel = ziel; _zusatz = zusatz; }

            public void Report(Fortschritt f)
            {
                if (_ziel == null || f == null) return;
                _ziel.Report(new Fortschritt
                {
                    Text = f.Text,
                    Aktuell = f.Aktuell,
                    Gesamt = f.Gesamt + _zusatz
                });
            }
        }

        // ------------------------------------------------------------- Sammeln

        /// <summary>
        /// Sammelt alle Berichtsdaten. variantenIds = gewählte Varianten (ohne Stamm).
        /// Für einen Berichtslauf NICHT direkt aufrufen — dort ist
        /// <see cref="SammleFuerBericht"/> der Einstieg (Simulation + Wirtschaftlichkeit
        /// verbindlich). Direkte Aufrufer sind der Wirtschaftlichkeits-Reiter und der
        /// Verlaufsdialog, die anschließend selbst rechnen.
        ///
        /// neuRechnen: alle Projekte vorab simulieren; fehlende Ergebnisse werden
        /// unabhängig davon immer gerechnet (Konzept Kap. 3.1/8.2).
        /// mitZeitreihen: für die Ganglinien wird je Projekt IMMER frisch in-memory
        /// simuliert und die Stundenreihen werden eingesammelt (Konzept Kap. 6.2) —
        /// Kennzahlen und Ganglinien stammen dann garantiert aus demselben Lauf.
        /// </summary>
        public BerichtsDaten Sammle(int idStamm, string stammName, List<int> variantenIds,
                                    bool neuRechnen, bool mitZeitreihen,
                                    IProgress<Fortschritt> fortschritt, CancellationToken abbruch)
        {
            _mitZeitreihen = mitZeitreihen;
            var daten = new BerichtsDaten { IdStamm = idStamm, Stammprojektname = stammName ?? "" };
            _daten = daten;

            // Reihenfolge: Stamm zuerst, dann die gewählten Varianten in Gruppenreihenfolge.
            var gruppe = new VariantenCtrl().LadeGruppe(idStamm, stammName);
            var auswahl = new List<VariantenCtrl.VarianteInfo>();
            foreach (VariantenCtrl.VarianteInfo vi in gruppe)
                if (vi.IstStamm || (variantenIds != null && variantenIds.Contains(vi.IdProjekt)))
                    auswahl.Add(vi);

            int gesamt = auswahl.Count, aktuell = 0;
            foreach (VariantenCtrl.VarianteInfo vi in auswahl)
            {
                abbruch.ThrowIfCancellationRequested();
                aktuell++;
                Melde(fortschritt, aktuell, gesamt, (vi.IstStamm ? "Stamm" : "Variante") + ": " + vi.Projektname);

                var v = new VariantenDaten
                {
                    IdProjekt = vi.IdProjekt,
                    Projektname = vi.Projektname,
                    Variantenname = vi.Variantenname,
                    IstStamm = vi.IstStamm
                };
                daten.Varianten.Add(v);

                try
                {
                    SammleProjekt(v, neuRechnen, fortschritt, aktuell, gesamt, abbruch);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    v.Fehler = ex.Message;
                    daten.Melde(v, Berichtshinweisstufe.Warnung, "konnte nicht geladen werden: " + ex.Message);
                }
            }

            // Abweichungserkennung: jede Variante gegen den Stamm (Konzept Kap. 4.3).
            VariantenDaten stammDaten = daten.Varianten.Count > 0 && daten.Varianten[0].IstStamm
                ? daten.Varianten[0] : null;
            if (stammDaten != null && stammDaten.Details != null)
            {
                foreach (VariantenDaten v in daten.Varianten)
                {
                    if (v.IstStamm || v.Details == null) continue;
                    try { v.Abweichungen = AbweichungsErmittler.Vergleiche(stammDaten.Details, v.Details); }
                    catch { /* Abweichungstabelle bleibt dann leer */ }
                }
            }

            return daten;
        }

        private bool _mitZeitreihen;

        /// <summary>
        /// Warnungssammlung des laufenden <see cref="Sammle"/>-Aufrufs (Nacharbeit
        /// Paket 8, Befund N5). Sie liegt als Feld vor, damit <see cref="SammleProjekt"/>
        /// die Meldungen des headless-Laufs dorthin schreiben kann, ohne dass die
        /// Signatur wächst — dasselbe Muster wie <see cref="_mitZeitreihen"/>.
        /// </summary>
        private BerichtsDaten _daten;

        private void SammleProjekt(VariantenDaten v, bool neuRechnen,
                                   IProgress<Fortschritt> fortschritt, int aktuell, int gesamt,
                                   CancellationToken abbruch)
        {
            var ergCtrl = new ErgebnisCtrl();

            // 1. Datenlage feststellen.
            DateTime? stand = LiesSimZeitstempel(v.IdProjekt);
            DateTime? aend = LiesAenderungsdatum(v.IdProjekt);
            v.ErgebnisFehlte = !stand.HasValue;
            v.ErgebnisVeraltet = stand.HasValue && aend.HasValue && stand.Value < aend.Value;

            // 2. Simulieren, wenn gefordert, kein Ergebnis vorliegt oder Zeitreihen
            //    für Ganglinien gebraucht werden (die gibt es nur aus dem frischen Lauf).
            if (neuRechnen || v.ErgebnisFehlte || _mitZeitreihen)
            {
                abbruch.ThrowIfCancellationRequested();
                Melde(fortschritt, aktuell, gesamt, "Simuliere: " + v.Projektname);

                // Frische Instanz je Projekt (Muster btnSimulieren_Click) — die Instanz
                // bleibt hier zugreifbar, damit der ZeitreihenExtraktor die Stundenreihen
                // einsammeln kann, bevor sie verworfen wird.
                //
                // NACHARBEIT PAKET 8, BEFUND N2: über SimuliereUndSpeichere statt über
                // Simuliere + eigenem Save. Dieser Pfad läuft in Task.Run auf einem
                // ThreadPool-Thread (UcBericht, UcWirtschaftlichkeit,
                // Form_WirtschaftlichkeitVerlauf); ein hier selbst gerufenes
                // ErgebnisCtrl.Save stand AUSSERHALB des dialogfreien Engine-Modus und
                // hätte bei einem Datenbankfehler eine MessageBox auf dem Worker-Thread
                // geöffnet — der Fortschrittsbalken wäre eingefroren. SimuliereUndSpeichere
                // klammert Ergebnisaufbau und Speichern korrekt (Befund N4) und liefert
                // die Meldungen des Laufs gleich mit.
                var runner = new SimulationRunner();
                string fehler;
                int erg = runner.SimuliereUndSpeichere(v.IdProjekt, out fehler);

                if (runner.LaufOk)
                {
                    // erg <= 0 heißt hier: gerechnet, aber nicht gespeichert. Die
                    // Stundenreihen sind trotzdem gültig — Verhalten wie bisher.
                    if (erg > 0) v.FrischSimuliert = true;
                    if (_mitZeitreihen)
                    {
                        ZeitreihenErhoben++;
                        try { v.Zeitreihen = ZeitreihenExtraktor.AusLauf(runner); }
                        catch { v.Zeitreihen = null; }
                    }
                }
                else if (v.ErgebnisFehlte)
                    throw new InvalidOperationException("Simulation fehlgeschlagen: " + (fehler ?? "unbekannter Fehler"));
                // War ein (älteres) Ergebnis vorhanden, läuft der Bericht damit weiter —
                // der Zeitstempel weist den Stand aus; Ganglinien entfallen dann mit Hinweis.

                // NACHARBEIT PAKET 8, BEFUND N5: Die Warnungen und Hinweise des Laufs
                // gehen sonst verloren. „out fehler" ist nur im Misserfolgsfall belegt,
                // und ein ERFOLGREICHER Lauf kann sehr wohl gemeldet haben, dass er mit
                // einer Ersatzannahme gerechnet hat (fehlender Tagesverteilungstyp,
                // abgeschnittene Prozesswärme, extrapolierte WP-Kennlinie). Vor Paket 8
                // sah der Anwender an dieser Stelle eine MessageBox.
                LaufmeldungenUebernehmen(v, runner, erg, fehler);
            }

            // 3. Ergebnisbaum + Projektstammdaten laden.
            v.Ergebnis = ergCtrl.Load(v.IdProjekt);
            if (v.Ergebnis == null)
                throw new InvalidOperationException("Kein Simulationsergebnis vorhanden.");
            v.SimulationsStand = v.Ergebnis.Zeitstempel;

            ProjektCtrl pc = new ProjektCtrl();
            pc.ReadSingle(v.IdProjekt);
            if (pc.rows > 0)
            {
                v.Projekt = new ProjektModel
                {
                    m_ID = pc.m_ID,
                    m_szProjektname = pc.m_szProjektname,
                    m_szBearbeiter = pc.m_szBearbeiter,
                    m_szBeschreibung = pc.m_szBeschreibung,
                    m_szKunde = pc.m_szKunde,
                    m_Aenderungsdatum = pc.m_Aenderungsdatum,
                    m_ID_Klimaregion = pc.m_ID_Klimaregion,
                    m_Erstelldatum = pc.m_Erstelldatum
                };
            }

            // 4. Brennstoffmengen (best effort — fehlendes Kostenmodul stoppt nichts).
            try { v.Brennstoffmengen = EnergieMengen.BaueBrennstoffmengen(v.IdProjekt); }
            catch { v.Brennstoffmengen = null; }

            // 5. Kosten-/Emissionsverrechnung (Phase 5), danach Kennzahlen aus dem
            //    Katalog (dessen Emissions-/Kosten-Zeilen lesen die Rechnerwerte).
            KostenEmissionRechner.Berechne(v);
            KennzahlenKatalog.Berechne(v);

            // Ersatzannahme des Emissionspfades sichtbar machen (Befund 30.08.2026):
            // Ohne zugeordneten Stromträger rechnet der Netzbezug mit dem
            // Strommix-Vorgabewert weiter, während die Kosten in derselben Lage „—"
            // melden. Dieselbe Behandlung wie die Ersatzannahmen eines
            // Simulationslaufs (LaufmeldungenUebernehmen).
            if (v.CO2StrommixRueckfall && _daten != null)
                _daten.Melde(v, Berichtshinweisstufe.Warnung,
                               "Der Netzstrom rechnet mit dem Strommix-Vorgabewert (" +
                               KostenEmissionRechner.STROMMIX_CO2_G_JE_KWH.ToString(
                                   "0.#", System.Globalization.CultureInfo.InvariantCulture) +
                               " g/kWh) — dem Projekt ist kein Stromträger mit gepflegtem " +
                               "Emissionsfaktor zugeordnet. Die CO₂-Kennzahlen stammen " +
                               "insoweit nicht aus den Projektdaten.");

            // SP-W1: Dasselbe Muster für den Leistungspreis des Stromträgers. Er ist
            // gepflegt, aber der Lauf hat keine Zeitreihen geführt — ohne Bezugsspitze
            // gibt es keine Basis, und der Anteil entfällt. Das sieht wie ein zu
            // günstiges Ergebnis aus, wenn es niemand sagt.
            if (!string.IsNullOrEmpty(v.LeistungspreisOhneSpitze) && _daten != null)
                _daten.Melde(v, Berichtshinweisstufe.Warnung,
                               "Für den Stromträger „" + v.LeistungspreisOhneSpitze +
                               "“ ist ein Leistungspreis gepflegt, der Lauf führt aber keine " +
                               "Bezugsspitze — der Leistungsanteil fehlt in den Energiekosten.");

            // BEFUNDE B-1/N1 (Anwenderentscheid 30.08.2026): Dasselbe Muster für die
            // zweite stille Lücke der Kostenkette — ein Heizkessel hat Wärme erzeugt,
            // aber sein Brennstoffverbrauch steht nicht im Ergebnis. Die Kennzahlen
            // bleiben unverändert (nichts wird abgeleitet), der Fehlbetrag wird nur
            // benannt. Wortlaut wie die Hinweiszeile der Wirtschaftlichkeit
            // (WIRT_KESSELBRENNSTOFF_FEHLT), damit beide Kanäle dasselbe sagen.
            if (v.KesselVerbrauchFehlt && _daten != null)
                _daten.Melde(v, Berichtshinweisstufe.Warnung,
                               "Energiekosten/CO₂-Bilanz unvollständig: Der Brennstoffverbrauch " +
                               "des Heizkessels " + Kesselnamen(v) + " liegt im Simulationsergebnis " +
                               "nicht vor — Kesselbrennstoff fehlt in Energiekosten, CO₂-Bilanz " +
                               "und BEHG-Abgabe.");

            // 6. Detail-Daten (Gebäude, Anlage, Komponenten, Klimaregion) für
            //    Projektbeschreibung, Kenndaten-Tabellen und Abweichungserkennung.
            try { v.Details = ProjektDetails.Lade(v.IdProjekt); }
            catch { v.Details = null; }

            // 7. Zeitreihen für Ganglinien: Phase 3 (In-Memory-Lauf liefert die Reihen).
        }

        /// <summary>Die betroffenen Kessel als Aufzählung für die Meldung (B-1/N1).</summary>
        private static string Kesselnamen(VariantenDaten v)
        {
            return (v.KesselOhneVerbrauch == null || v.KesselOhneVerbrauch.Count == 0)
                ? "?" : string.Join(", ", v.KesselOhneVerbrauch);
        }

        /// <summary>
        /// Übernimmt Warnungen und Hinweise eines headless-Laufs in die Warnungsliste des
        /// Berichts (Nacharbeit Paket 8, Befund N5).
        ///
        /// Jede Meldung wird dem Projekt zugeordnet — bei einem Variantenbericht laufen
        /// bis zu einem Dutzend Simulationen hintereinander, und eine Warnung ohne
        /// Projektbezug wäre nicht zuzuordnen. Fehler des Kanals stehen bereits in
        /// <paramref name="fehler"/> und werden vom Aufrufer behandelt; hier kommt der
        /// nicht abbrechende Teil dazu.
        /// </summary>
        private void LaufmeldungenUebernehmen(VariantenDaten v, SimulationRunner runner,
                                              int erg, string fehler)
        {
            if (_daten == null || runner == null || runner.Protokoll == null) return;

            // Die Stufe des Protokolls geht mit: Warnungen bleiben auf der Berichtsseite
            // sichtbar, Hinweise klappt sie ein.
            foreach (string w in runner.Protokoll.Warnungen)
                _daten.Melde(v, Berichtshinweisstufe.Warnung, w);
            foreach (string h in runner.Protokoll.Hinweise)
                _daten.Melde(v, Berichtshinweisstufe.Hinweis, h);

            // Gerechnet, aber nicht gespeichert: Der Bericht läuft mit dem älteren
            // Ergebnisstand weiter - das gehört sichtbar gemacht.
            if (runner.LaufOk && erg <= 0)
                _daten.Melde(v, Berichtshinweisstufe.Warnung, "Das frisch gerechnete Ergebnis konnte nicht gespeichert " +
                               "werden" + (string.IsNullOrEmpty(fehler) ? "." : " (" + fehler + ")."));

        }

        private static void Melde(IProgress<Fortschritt> p, int aktuell, int gesamt, string text)
        {
            if (p != null) p.Report(new Fortschritt { Aktuell = aktuell, Gesamt = gesamt, Text = text });
        }
    }
}
