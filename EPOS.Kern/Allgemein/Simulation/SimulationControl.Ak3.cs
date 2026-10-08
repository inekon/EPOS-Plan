using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Vorbereitung des AK3-Wegs an der Kaskade</b> (AK3-W2; Entwurf AK3, Festlegung 13 und 2.1
    /// Schritt 4). Beide Schalter sind im Kern gesetzt und noch nicht wählbar: Ohne sie rechnet
    /// <see cref="Kaskade_Zweikanalig"/> Anweisung für Anweisung wie zuvor.
    /// </summary>
    public partial class SimulationControl
    {
        /// <summary>
        /// <b>Vektorstufen als Schleifenmitglieder</b> (Festlegung 13): Heizerzeuger ohne Speicher
        /// (Heizkessel, Solarthermie, BHKW), die in der Kaskade VOR der Speicherstufe stehen, rechnen
        /// im AK3-Weg als Mitglieder der Stundenschleife (Muster <see cref="ZwischenstufenAufnehmen"/>);
        /// Vektorstufen danach rechnen wie heute über den Rest. Ein Projekt ohne Speicherstufe rechnet
        /// alle Heizerzeuger als Schleifenmitglieder ohne Speicher. Die Abweichung zum Vektorweg ist
        /// benannt; Vorgabe <c>false</c> = der heutige Weg.
        /// </summary>
        internal bool Ak3VektorstufenInSchleife { get; set; }

        /// <summary>
        /// Bedarfsnaht der Kaskadenstunde (<see cref="Kaskadenschleife.Stundenbedarf"/>), an die
        /// Speicherstufe durchgereicht. <c>null</c> = Vorgabe <see cref="VektorStundenbedarf"/>.
        /// </summary>
        internal IStundenbedarf Stundenbedarf { get; set; }

        /// <summary>
        /// Die Kaskadenpositionen, deren Vektorstufe im AK3-Weg Schleifenmitglied wird
        /// (Festlegung 13) — rein, ohne Datenbank: jede Position mit Heizkessel, Solarthermie oder
        /// BHKW, die noch kein Mitglied ist und VOR dem ersten Mitglied steht; ohne Mitglied jede
        /// solche Position. Die Wärmepumpe ist immer Mitglied und taucht hier nie auf.
        /// </summary>
        /// <param name="tool">Kaskadenplätze (<c>Tool_1</c> bis <c>Tool_4</c>, Erzeugercodes nach <see cref="DbWerte"/>).</param>
        /// <param name="mitglied">je Platz: schon Mitglied der Speicherstufe.</param>
        internal static List<int> Ak3Aufnahmepositionen(IReadOnlyList<string> tool, IReadOnlyList<bool> mitglied)
        {
            var positionen = new List<int>();
            if (tool == null || mitglied == null) return positionen;

            int n = tool.Count < 4 ? tool.Count : 4;
            if (mitglied.Count < n) n = mitglied.Count;

            int erste = -1;
            for (int i = 0; i < n; i++)
                if (mitglied[i]) { erste = i; break; }

            int ende = erste < 0 ? n : erste;
            for (int i = 0; i < ende; i++)
            {
                string t = tool[i];
                if (t == DbWerte.ERZEUGER_HEIZKESSEL || t == DbWerte.ERZEUGER_SOLARTHERMIE || t == DbWerte.ERZEUGER_BHKW)
                    positionen.Add(i);
            }
            return positionen;
        }

        /// <summary>
        /// Nimmt im AK3-Weg die Vektorstufen nach <see cref="Ak3Aufnahmepositionen"/> in die
        /// Stundenschleife auf. Gerufen in <see cref="Kaskade_Zweikanalig"/> NACH
        /// <see cref="ZwischenstufenAufnehmen"/> und nur bei <see cref="Ak3VektorstufenInSchleife"/>.
        /// </summary>
        private void Ak3VektorstufenAufnehmen()
        {
            if (tool == null) return;

            var mitglied = new bool[tool.Length];
            for (int i = 0; i < tool.Length; i++) mitglied[i] = IstSchleifenstufe(i);

            foreach (int i in Ak3Aufnahmepositionen(tool, mitglied))
            {
                if (tool[i] == DbWerte.ERZEUGER_HEIZKESSEL) _kesselInSchleife = true;
                else if (tool[i] == DbWerte.ERZEUGER_SOLARTHERMIE) _solarInSchleife = true;
                else if (tool[i] == DbWerte.ERZEUGER_BHKW) _bhkwInSchleife = true;
            }
        }

        // =========================================================================================
        //  AK3-W3b — der AK3-Weg an der Kaskade (Entwurf AK3 2.1 Schritte 3 bis 5)
        // =========================================================================================

        /// <summary>Die Anlagen-Ids der Wärmeerzeuger des Kreises, indexgleich zur Erzeugerliste (AK3-K, Vorrangschätzung).</summary>
        private List<int> _ak3WaermeAnlagen;

        /// <summary>Hat <see cref="Ak3Einrichten"/> Naht und Schalter für den letzten Lauf gesetzt?</summary>
        private bool _ak3Eingerichtet;

        /// <summary>
        /// Richtet vor der Kaskade den AK3-Weg ein, wenn die Bedarfsseite ihn erfasst hat
        /// (<see cref="SimulationWaermebedarf.Ak3"/>): die Bedarfsnaht <see cref="Ak3Stundenbedarf"/> und die
        /// Vektorstufen als Schleifenmitglieder (Festlegung 13). Ohne AK3-Weg bleibt alles, wie es ist; Naht und
        /// Schalter eines früheren AK3-Laufs derselben Instanz werden zurückgenommen.
        /// </summary>
        private void Ak3Einrichten()
        {
            if (_ak3Eingerichtet)
            {
                Stundenbedarf = null;
                Ak3VektorstufenInSchleife = false;
                _ak3Eingerichtet = false;
            }
            Ak3Weg weg = simulation_Waermebedarf?.Ak3;
            if (weg == null || weg.Gebaeude.Count == 0 || weg.Gebaeude.Any(g => g.Pass1W == null)) return;

            Ak3VektorstufenInSchleife = true;
            Stundenbedarf = new Ak3Stundenbedarf(weg, () => Ak3KreisBauen(weg), simulation_Waermebedarf.Heizkreis,
                (h, d) =>
                {
                    if (_wpInSchleife && simulation_wp?.Waermebedarf_stuendlich != null) simulation_wp.Waermebedarf_stuendlich[h] += d;
                });
            _ak3Eingerichtet = true;
        }

        /// <summary>
        /// Baut den Kreis bei der ersten Kaskadenstunde — dann sind die Module aufgebaut: Stepper einschwingen
        /// (720 h ohne Kaskade, Festlegung 16), Wärmepumpen aus ihren Modulen (Kennlinien, Quelle, Kappung), Kessel
        /// und BHKW mit <c>Ptherm</c> der Projektkopie (Festlegung 5), Speicherleser über die Puffer der Registry.
        /// </summary>
        private Anlagenkopplung Ak3KreisBauen(Ak3Weg weg)
        {
            var gebaeude = new List<Kopplungsgebaeude>();
            foreach (Ak3Weg.Eintrag e in weg.Gebaeude)
            {
                e.Stepper.Beginnen();
                gebaeude.Add(new Kopplungsgebaeude(e.Index, e.Zeile.ID_Gebaeude, e.Zeile.Gebaeudename, e.Stepper, e.Faktor));
            }
            var erzeuger = new List<IErzeugerkapazitaet>();
            _ak3WaermeAnlagen = new List<int>();
            foreach (Anlagenfahrplan.Erzeugerzeile z in weg.Erzeuger)
            {
                _ak3WaermeAnlagen.Add(z.Modell.ID);
                WaermepumpeKapazitaet wp = z.Typ == WizardItemClass.WP_TYP && _wpInSchleife
                    ? simulation_wp?.Ak3Kapazitaet(z.Modell.ID, z.Fahrplan, true) : null;
                erzeuger.Add(wp ?? (IErzeugerkapazitaet)new FesteKapazitaet(z.Fahrplan));
            }
            var kreis = new Anlagenkopplung(gebaeude, erzeuger, new Speicherleser(RegistrySpeicher()))
            {
                // H2 (Festlegung 23, Q-AK3-2): nur Gebäude mit Heizkurve und k_R > 0; ohne solches kein Raumeinfluss.
                Raumeinfluss = Raumeinfluss.AusGebaeuden(weg.Gebaeude.Select(e => e.Zeile).ToList()),
            };
            // AK3-K (4.2): die Kälteschranke der Kältestunde im Kreis.
            kreis.Kaelteschranke = Ak3KaelteschrankeBauen(erzeuger);
            // KK3 (Entwurf KK 2.2, 2.3): Raumeinfluss der Kühlkurve und gleitender Erzeugervorlauf — nur mit Kälteschranke und
            // einem Gebäude mit wirksamer Kühlkurve (Kernschalter, Stufe AK3); sonst null und der Kreis bleibt am festen Vorlauf.
            if (kreis.Kaelteschranke != null)
                kreis.KuehlRaumeinfluss = KuehlRaumeinfluss.AusEingaengen(gebaeude.Select(g => g.Stepper.EingangEinzone).ToList());
            // Fallwechsel (2.4): die Stützstelle der ersten Wärmepumpe am Vorlauf jedes Durchlaufs.
            WaermepumpeKapazitaet erste = erzeuger.OfType<WaermepumpeKapazitaet>().FirstOrDefault();
            if (erste != null)
            {
                kreis.Stuetzstelle = erste.Stuetzstelle;
                kreis.StuetzstelleHalten = !erste.Interpolieren;
            }
            return kreis;
        }

        /// <summary>
        /// Die Stundenschleife im AK3-Weg: ein Fehler des Kreises oder des Gebäudemodells bricht den Lauf benannt
        /// ab (F-A15, keine stille Näherung); danach führt die Bedarfsseite ihre Reihen nach.
        /// </summary>
        private bool Ak3Rechnen(Kaskadenschleife schleife, Kanalsatz kanaele)
        {
            bool ok;
            try
            {
                ok = schleife.Rechnen(kanaele);
            }
            catch (AnlagenkopplungException ex)
            {
                SimulationProtokoll.Aktuell.Fehlermeldung(ex.Message);
                return false;
            }
            catch (GebaeudeModellException ex)
            {
                SimulationProtokoll.Aktuell.Fehlermeldung("Anlagenkopplung AK3, Gebäudemodell VDI 6007 [" + ex.Grund + "]: " + ex.Message);
                return false;
            }
            if (!ok) return false;
            simulation_Waermebedarf.Ak3Nachfuehren();
            if (!Ak3KaelteUebernehmen(kanaele)) return false;
            Ak3KreiszaehlerMelden();
            return true;
        }

        /// <summary>
        /// <b>Die Kreiszähler der Kälteseite als Laufhinweis</b> (AK3-K K3, Festlegung 20; die Ergebnisspalten folgen mit
        /// S1 in K4): Stunden an der Kälteschranke, Umschaltstunden, Kälte-Restbedarf, Fallwechsel und der Vergleich der
        /// Vorrangschätzung mit der echten Kältestunde. Nur mit Kälteseite im Kreis; je Feldlauf gilt der letzte.
        /// </summary>
        private void Ak3KreiszaehlerMelden()
        {
            Ak3Weg weg = simulation_Waermebedarf.Ak3;
            Anlagenkopplung k = weg?.Kreis;
            if (weg == null || k == null || k.Kaelteschranke == null) return;
            SimulationProtokoll.Aktuell.Hinweis(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_AK3K_KREISZAEHLER,
                k.StundenAnDerKaelteschranke, k.StundenUmschaltung, k.KaelteRestKwh, k.KaelteRestStunden,
                k.StuetzstellenWechsel + k.FallWechsel, k.KaelteschrankeZuKnappStunden, k.KaelteschrankeZuKnappKwh,
                k.KaelteschrankeZuWeitStunden));
        }

        /// <summary>
        /// <b>AK3-K (Fehler 1.1 (a))</b>: Der Kühlkanal des Kanalsatzes der
        /// Kaskade nimmt dieselbe Abweichung je Stunde auf wie der Kühlkanal der Bedarfsseite — die Deckungsprobe Kälte
        /// vergleicht beide. Eine verletzte Bedarfsprobe Kälte bricht den Lauf benannt ab.
        /// </summary>
        private bool Ak3KaelteUebernehmen(Kanalsatz kanaele)
        {
            Ak3Weg weg = simulation_Waermebedarf.Ak3;
            if (weg == null) return true;
            SimulationKaeltebedarf kaelte = simulation_Waermebedarf.Kaelteseite;
            if (!string.IsNullOrEmpty(kaelte.Fehlertext))
            {
                FehlertextAufnehmen(kaelte.Fehlertext);
                return false;
            }
            if (kanaele != null && kaelte.Gerechnet)
            {
                double[] kanal = kanaele.Kuehlung;
                for (int h = 0; h < Kanalsatz.STUNDEN_JAHR; h++)
                    if (weg.KaelteDeltaKwh[h] != 0.0) kanal[h] += weg.KaelteDeltaKwh[h];
            }
            return true;
        }

        /// <summary>Ist der Heizkessel Mitglied der Stundenschleife des letzten Laufs? (Proben)</summary>
        internal bool KesselInSchleife => _kesselInSchleife;

        /// <summary>Ist die Solarthermie Mitglied der Stundenschleife des letzten Laufs? (Proben)</summary>
        internal bool SolarInSchleife => _solarInSchleife;

        /// <summary>Ist das BHKW Mitglied der Stundenschleife des letzten Laufs? (Proben)</summary>
        internal bool BhkwInSchleife => _bhkwInSchleife;
    }
}
