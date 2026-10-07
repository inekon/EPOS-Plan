using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Ein Erzeuger, wie ihn der Fahrplan sieht</b> (AK2-2a) — Nennleistung und die Masken je Stunde,
    /// sonst nichts: kein Wirkungsgrad, kein Preis, keine Kennlinie. Die Datenbankseite füllt ihn in
    /// <see cref="Anlagenfahrplan.AusProjekt"/>; Proben bauen ihn synthetisch.
    /// </summary>
    internal sealed class Fahrplanerzeuger
    {
        /// <summary>Bezeichner der Anlage — für Meldungen.</summary>
        internal string Bezeichner { get; init; } = "";

        /// <summary>
        /// Die Leistung, die der Erzeuger voll verfügbar anbietet [kW] — Wärmepumpe <c>Tab_WP.Nennleistung</c>
        /// (dieselbe Zahl, die der Bestand als <c>Grenzleistung</c> des Moduls führt), Heizkessel
        /// <c>Tab_Heizkessel.Ptherm</c>, BHKW <c>Tab_BHKW.Ptherm</c>.
        /// </summary>
        internal double NennleistungKw { get; init; }

        /// <summary>Gesperrte Stunden (8760); <c>null</c> = nie gesperrt. Die Sperrzeit geht dem Zeitprogramm vor (F7).</summary>
        internal bool[] Gesperrt { get; init; }

        /// <summary>Das Zeitprogramm der Anlage; <c>null</c> = immer verfügbar.</summary>
        internal Anlagenzeitprogramm Zeitprogramm { get; init; }

        /// <summary>Stunden unter dem Abschaltpunkt eines bivalenten Betriebs (8760); <c>null</c> = nie.</summary>
        internal bool[] Abgeschaltet { get; init; }

        /// <summary>Vorlaufangebot [°C]: <c>Vorlauf_Max</c>, Rückfall <c>Vorlauf</c>; NaN = keine Angabe (keine Grenze).</summary>
        internal double VorlaufAngebotC { get; init; } = double.NaN;
    }

    /// <summary>
    /// <b>Der Anlagenfahrplan</b> (Anlagenkopplung 5.3, 5.4, 6.2; Festlegungen F1, F3, F7) — die 8 760
    /// Verfügbarkeiten des PROJEKTS auf dem <b>Profilweg</b>: einmal je Projekt, vorab, ohne Ladezustand und
    /// ohne Kaskadenergebnis. Je Stunde:
    /// <list type="number">
    /// <item><b>Leistung</b> = Σ Nennleistung × Verfügbarkeit des Erzeugers; die Verfügbarkeit ist 0 in einer
    /// Sperrstunde (geht vor), sonst der Faktor des Zeitprogramms, und 0 unter dem Abschaltpunkt.</item>
    /// <item><b>Vorlauf</b> = das höchste Angebot unter den Erzeugern, die in der Stunde etwas liefern; trägt
    /// einer davon kein Angebot, gibt es keine Grenze (NaN).</item>
    /// <item><b>Grund</b> = der Ausfall mit der größten Leistung (Gleichstand: Reihenfolge der Aufzählung);
    /// ohne Ausfall <see cref="Verfuegbarkeitsgrund.KeineBegrenzung"/>, ohne Erzeuger
    /// <see cref="Verfuegbarkeitsgrund.KeinErzeuger"/>.</item>
    /// <item><b>Speichervorrat über die Sperrdauer</b> (5.4): In jedem zusammenhängenden Block von Sperrstunden
    /// darf der Vorrat (nutzbare Kapazität <c>Q_max</c> der Heizungsspeicher) die Sperrlücke füllen, gleich
    /// verteilt: höchstens Vorrat ÷ Blocklänge je Stunde — nie als Ladezustand. Reicht er nicht, heißt der
    /// Grund der Sperrlücke <see cref="Verfuegbarkeitsgrund.SpeicherLeer"/>.</item>
    /// </list>
    /// <para><b>Was er nicht ist:</b> keine Deckungsrechnung, kein Preis, kein Wirkungsgrad (5.3). Er liest
    /// Anlagendaten und darf deshalb weder das Modul <c>Gebaeude/</c> noch den Altweg nennen
    /// (<c>ModultrennungswacheTests</c>, Satz 3).</para>
    /// </summary>
    internal sealed class Anlagenfahrplan
    {
        /// <summary>Zahl der Stunden des Rechenjahres.</summary>
        internal const int STUNDEN = 8760;

        private readonly Anlagenverfuegbarkeit[] _stunden;

        private Anlagenfahrplan(Anlagenverfuegbarkeit[] stunden, int erzeuger, double vorratKwh)
        {
            _stunden = stunden;
            Erzeugerzahl = erzeuger;
            SpeichervorratKwh = vorratKwh;
        }

        /// <summary>Die Verfügbarkeit der Stunde <paramref name="h"/> (0 … 8759).</summary>
        internal Anlagenverfuegbarkeit Stunde(int h) => _stunden[h];

        /// <summary>Die 8 760 Verfügbarkeiten.</summary>
        internal IReadOnlyList<Anlagenverfuegbarkeit> Stunden => _stunden;

        /// <summary>Zahl der Erzeuger, aus denen der Fahrplan gebildet ist.</summary>
        internal int Erzeugerzahl { get; }

        /// <summary>Der Speichervorrat, der über Sperrdauern verteilt wurde [kWh].</summary>
        internal double SpeichervorratKwh { get; }

        // =====================================================================
        //  Der Rechenkern — ohne Datenbank
        // =====================================================================

        /// <summary>
        /// Rechnet den Fahrplan aus den <paramref name="erzeuger"/>n und dem Speichervorrat
        /// <paramref name="speichervorratKwh"/> (≥ 0; 0 = kein Speicher).
        /// </summary>
        internal static Anlagenfahrplan Rechnen(IReadOnlyList<Fahrplanerzeuger> erzeuger, double speichervorratKwh)
        {
            if (erzeuger == null) throw new ArgumentNullException(nameof(erzeuger));
            double vorrat = speichervorratKwh > 0.0 && !double.IsInfinity(speichervorratKwh) ? speichervorratKwh : 0.0;
            var stunden = new Anlagenverfuegbarkeit[STUNDEN];
            if (erzeuger.Count == 0)
            {
                for (int h = 0; h < STUNDEN; h++)
                    stunden[h] = new Anlagenverfuegbarkeit(0.0, double.NaN, Verfuegbarkeitsgrund.KeinErzeuger);
                return new Anlagenfahrplan(stunden, 0, vorrat);
            }

            int gruende = Enum.GetValues(typeof(Verfuegbarkeitsgrund)).Length;
            var leistung = new double[STUNDEN];
            var sperrLuecke = new double[STUNDEN];
            var vorlauf = new double[STUNDEN];
            var ausfallJeGrund = new double[STUNDEN, gruende];

            for (int h = 0; h < STUNDEN; h++)
            {
                double summe = 0.0, hoechsterVorlauf = double.NegativeInfinity;
                bool ohneAngebot = false;
                foreach (Fahrplanerzeuger e in erzeuger)
                {
                    double nenn = e.NennleistungKw > 0.0 && !double.IsInfinity(e.NennleistungKw) ? e.NennleistungKw : 0.0;
                    double faktor;
                    if (e.Gesperrt != null && e.Gesperrt[h])
                    {
                        faktor = 0.0;
                        ausfallJeGrund[h, (int)Verfuegbarkeitsgrund.Sperrzeit] += nenn;
                        sperrLuecke[h] += nenn;
                    }
                    else
                    {
                        faktor = e.Zeitprogramm != null ? e.Zeitprogramm.Faktor(h) : 1.0;
                        if (faktor < 1.0) ausfallJeGrund[h, (int)Verfuegbarkeitsgrund.Zeitprogramm] += (1.0 - faktor) * nenn;
                        if (faktor > 0.0 && e.Abgeschaltet != null && e.Abgeschaltet[h])
                        {
                            ausfallJeGrund[h, (int)Verfuegbarkeitsgrund.Abschaltpunkt] += faktor * nenn;
                            faktor = 0.0;
                        }
                    }
                    double beitrag = faktor * nenn;
                    summe += beitrag;
                    if (beitrag > 0.0)
                    {
                        if (double.IsNaN(e.VorlaufAngebotC)) ohneAngebot = true;
                        else hoechsterVorlauf = Math.Max(hoechsterVorlauf, e.VorlaufAngebotC);
                    }
                }
                leistung[h] = summe;
                vorlauf[h] = ohneAngebot || double.IsNegativeInfinity(hoechsterVorlauf) ? double.NaN : hoechsterVorlauf;
            }

            // Der Speichervorrat über die Sperrdauer (5.4): je zusammenhängendem Block gleich verteilt.
            if (vorrat > 0.0)
            {
                int h = 0;
                while (h < STUNDEN)
                {
                    if (!(sperrLuecke[h] > 0.0)) { h++; continue; }
                    int beginn = h;
                    while (h < STUNDEN && sperrLuecke[h] > 0.0) h++;
                    double jeStunde = vorrat / (h - beginn);
                    for (int s = beginn; s < h; s++)
                    {
                        double gedeckt = Math.Min(sperrLuecke[s], jeStunde);
                        leistung[s] += gedeckt;
                        double rest = sperrLuecke[s] - gedeckt;
                        ausfallJeGrund[s, (int)Verfuegbarkeitsgrund.Sperrzeit] = 0.0;
                        ausfallJeGrund[s, (int)Verfuegbarkeitsgrund.SpeicherLeer] = rest > 0.0 ? rest : 0.0;
                    }
                }
            }

            for (int h = 0; h < STUNDEN; h++)
            {
                int grund = 0;
                double groesster = 0.0;
                for (int g = 1; g < gruende; g++)
                    if (ausfallJeGrund[h, g] > groesster)
                    {
                        groesster = ausfallJeGrund[h, g];
                        grund = g;
                    }
                stunden[h] = new Anlagenverfuegbarkeit(leistung[h], vorlauf[h], (Verfuegbarkeitsgrund)grund);
            }
            return new Anlagenfahrplan(stunden, erzeuger.Count, vorrat);
        }

        // =====================================================================
        //  Die Datenbankseite
        // =====================================================================

        /// <summary>
        /// <b>Der Fahrplan eines Projekts</b> aus seinen Anlagen: die belegten Plätze der Wärmekaskade
        /// (<see cref="Kaskade.Belegt"/>; Wärmepumpe, Heizkessel, BHKW — Solarthermie ist kein planbarer
        /// Erzeuger), je Typ die Anlagenzeilen, deren Senke Heizwärme bedient (<c>WS_Typ</c> leer, „Beides"
        /// oder „Heizung", dieselbe Regel wie <see cref="WErzeugerCtrl.VorlaufDesHeizkanals"/>). Sperrzeit und
        /// Abschaltpunkt wirken wie im Bestand nur an der Wärmepumpe; der Speichervorrat ist die Summe
        /// <c>Q_max</c> der Speicher, die den Heizkanal bedienen.
        /// </summary>
        /// <param name="idProjekt">Das Projekt.</param>
        /// <param name="aussentemperatur">Außentemperatur je Stunde [°C] (8760) — für den Abschaltpunkt.</param>
        /// <param name="wochentagDesErstenTags">Wochentag der Stunde 0 (0 = Montag) für das Zeitprogramm.</param>
        /// <param name="idKlimaregion">Klimaregion — für den Wochentag der Sperrfenster wie im Bestand.</param>
        /// <exception cref="AnlagenzeitprogrammFehler">ein ungültiges Zeitprogramm — benannt, nie still.</exception>
        internal static Anlagenfahrplan AusProjekt(int idProjekt, double[] aussentemperatur, int wochentagDesErstenTags,
                                                   int idKlimaregion)
        {
            var liste = ErzeugerAusProjekt(idProjekt, aussentemperatur, wochentagDesErstenTags, idKlimaregion)
                .Select(z => z.Fahrplan).ToList();

            double vorrat = 0.0;
            foreach (WaermesenkeClass.PufferInfo p in WaermesenkeClass.ProjektPufferListe(
                         idProjekt, WaermesenkeClass.VERWENDUNG_HEIZUNG, true))
                vorrat += p.Q_max;
            return Rechnen(liste, vorrat);
        }

        /// <summary>Ein Heizerzeuger des Projekts mit Anlagentyp, Anlagenzeile und Fahrplanteil (AK3-W3b).</summary>
        internal sealed class Erzeugerzeile
        {
            internal Erzeugerzeile(int typ, WErzeugerModel modell, Fahrplanerzeuger fahrplan)
            {
                Typ = typ;
                Modell = modell;
                Fahrplan = fahrplan;
            }

            /// <summary>Anlagentyp nach <see cref="WizardItemClass"/> (WP, Kessel, BHKW).</summary>
            internal int Typ { get; }

            /// <summary>Die Anlagenzeile (<c>Tab_Energieanlagen</c>).</summary>
            internal WErzeugerModel Modell { get; }

            /// <summary>Masken, Zeitprogramm, Vorlaufangebot und Nennleistung des Erzeugers.</summary>
            internal Fahrplanerzeuger Fahrplan { get; }
        }

        /// <summary>
        /// <b>Die Heizerzeuger des Projekts</b> in der Ordnung des Fahrplans (belegte Kaskadenplätze, je Typ nach
        /// Anlagen-Id) — der eine Lader für <see cref="AusProjekt"/> (AK2) und die Angebotsfunktion (AK3-W3b). Je
        /// Anlagenzeile ein Modul: <c>Tab_Energieanlagen</c> führt keine Modulzahl (die Kapazität trägt Anzahl 1).
        /// </summary>
        /// <exception cref="AnlagenzeitprogrammFehler">ein ungültiges Zeitprogramm — benannt, nie still.</exception>
        internal static List<Erzeugerzeile> ErzeugerAusProjekt(int idProjekt, double[] aussentemperatur,
                                                               int wochentagDesErstenTags, int idKlimaregion)
        {
            var liste = new List<Erzeugerzeile>();
            KonfigurationModel konfig = KonfigurationCtrl.LiesProjekt(idProjekt);
            int wt = wochentagDesErstenTags >= 0 && wochentagDesErstenTags <= 6 ? wochentagDesErstenTags : 0;
            foreach (string platz in Kaskade.Belegt(konfig))
            {
                int typ = Kaskade.TypZuAnlagentyp(platz);
                if (typ != WizardItemClass.WP_TYP && typ != WizardItemClass.KESSEL_TYP && typ != WizardItemClass.BHKW_TYP)
                    continue;
                foreach (WErzeugerModel m in WErzeugerCtrl.ModelleJeTyp(idProjekt, typ).OrderBy(x => x.ID))
                {
                    if (!BedientHeizung(m.WS_Typ)) continue;
                    liste.Add(new Erzeugerzeile(typ, m, new Fahrplanerzeuger
                    {
                        Bezeichner = m.Bezeichner ?? "",
                        NennleistungKw = Nennleistung(typ, m),
                        Gesperrt = typ == WizardItemClass.WP_TYP ? Sperrmaske(m, idKlimaregion) : null,
                        Zeitprogramm = Anlagenzeitprogramm.Lesen(m.Zeitprogramm, m.Bezeichner, wt),
                        Abgeschaltet = typ == WizardItemClass.WP_TYP ? Abschaltmaske(m, aussentemperatur) : null,
                        VorlaufAngebotC = m.Vorlauf_Max.HasValue ? m.Vorlauf_Max.Value
                                          : m.Vorlauf > 0 ? m.Vorlauf : double.NaN,
                    }));
                }
            }
            return liste;
        }

        private static bool BedientHeizung(string wsTyp)
            => string.IsNullOrEmpty(wsTyp)
               || string.Equals(wsTyp, DbWerte.WS_TYP_BEIDES, StringComparison.Ordinal)
               || string.Equals(wsTyp, DbWerte.WS_TYP_HEIZUNG, StringComparison.Ordinal);

        /// <summary>Die Nennleistung je Erzeugertyp [kW] aus der Projektkopie des Geräts; 0, wenn keine gepflegt ist.</summary>
        private static double Nennleistung(int typ, WErzeugerModel m)
        {
            string sql;
            int id;
            if (typ == WizardItemClass.WP_TYP) { sql = "SELECT Nennleistung FROM Tab_WP WHERE ID = ?"; id = m.ID_WP; }
            else if (typ == WizardItemClass.KESSEL_TYP) { sql = "SELECT Ptherm FROM Tab_Heizkessel WHERE ID = ?"; id = m.ID_Kessel; }
            else { sql = "SELECT Ptherm FROM Tab_BHKW WHERE ID = ?"; id = m.ID_BHKW; }
            if (id <= 0) return 0.0;
            return StilleDb.Kommazahl(StilleDb.Scalar(sql, StilleDb.Par("@id", DbParamTyp.Integer, id)));
        }

        /// <summary>Die Sperrmaske der Wärmepumpe — dasselbe Sperrprofil wie <c>SimulationWaermepumpe</c> (Altfenster und Sperrfenster).</summary>
        private static bool[] Sperrmaske(WErzeugerModel m, int idKlimaregion)
        {
            List<Sperrfenster> fenster = Sperrprofil.Lesen(m.ID);
            if (!m.Sperrung && fenster.Count == 0) return null;
            int wt = fenster.Count > 0 ? ProfilBedarf.WochentagJan1AusKlimaregion(idKlimaregion) : 0;
            Sperrprofil p = Sperrprofil.Bilden(m.Sperrung, m.Sperrzeit_von, m.Sperrzeit_bis, fenster, wt);
            return (bool[])p.Verdichter.Clone();
        }

        /// <summary>
        /// Die Stunden, in denen eine bivalent betriebene Wärmepumpe wie im Bestand aus ist: teilparallel bei
        /// θ_a ≤ Abschaltpunkt, alternativ bei θ_a &lt; Abschaltpunkt; parallel nie.
        /// </summary>
        internal static bool[] Abschaltmaske(WErzeugerModel m, double[] aussentemperatur)
        {
            if (m == null || !m.Bivalenter_Betrieb || aussentemperatur == null || aussentemperatur.Length < STUNDEN) return null;
            bool teilparallel = m.Betriebsart == DbWerte.WP_BETRIEBSART_TEILPARALLEL;
            bool alternativ = m.Betriebsart == DbWerte.WP_BETRIEBSART_ALTERNATIV;
            if (!teilparallel && !alternativ) return null;
            var aus = new bool[STUNDEN];
            for (int h = 0; h < STUNDEN; h++)
                aus[h] = teilparallel ? aussentemperatur[h] <= m.Abschaltpunkt : aussentemperatur[h] < m.Abschaltpunkt;
            return aus;
        }
    }
}
