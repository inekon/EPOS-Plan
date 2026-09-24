using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE H2 — der Bezugsgrößen-Auflöser der Endenergie-Bemessungen
    /// (<c>Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md</c> § 4.5, Festlegung
    /// 29.08.2026; Anschluss an Etappe H1, offener Punkt H1-1).
    ///
    /// <para><b>Was er liefert.</b> Für eine Betriebskostenposition mit
    /// <c>BEMESSUNG_PROZENT_ENDENERGIEKOSTEN</c> (Weg A) oder
    /// <c>BEMESSUNG_PROZENT_ENDENERGIEBEDARF</c> (Weg B) die Endenergie der
    /// betrachteten Anlage aus dem <b>jüngsten Simulationslauf</b>: den
    /// Jahresbedarf [kWh/a] und — soweit Preise gepflegt sind — die
    /// Arbeitskosten [€/a]. Die Menge ist ein ERGEBNISWERT, kein Eingabewert:
    /// Sie wird bei jedem Lesen frisch geholt, die Spalte
    /// <c>Tab_ProjektWerte.Menge</c> bleibt Ausweisgröße („Stand des Laufs").</para>
    ///
    /// <para><b>Endenergie je Komponente</b> (Tabelle § 4.5):
    /// BHKW und Heizkessel = Brennstoff (<c>Verbrauch</c> je Modul × Arbeitspreis
    /// seines <c>CarrierId</c>), Wärmepumpe = Strom (<c>Stromverbrauch</c> +
    /// <c>Heizstab</c> × Strombezugspreis). Solarthermie, Pufferspeicher,
    /// Stromspeicher und Photovoltaik haben KEINE Endenergie — dort ist nur der
    /// feste Jahresbetrag zulässig, der Auflöser liefert null.</para>
    ///
    /// <para><b>DER ELEKTROKESSEL</b> (Gerät mit <c>Tab_Heizkessel.Brennstoff</c> = 13,
    /// <see cref="SimulationSPK.IstStromkesselBrennstoff"/>) ist ein Heizkessel mit der
    /// Endenergie der Wärmepumpe: STROM. Seine Modulzeile führt bewusst
    /// <c>Verbrauch</c> = 0, weil die Simulation seinen Einsatz auf den Stromzähler
    /// bucht und er über den Reststrombedarf schon im Netzbezug steht, den die
    /// Kostenrechnung eigens bepreist. Der Auflöser weist die Menge trotzdem aus —
    /// <see cref="SimulationSPK.StromeinsatzElektrokesselMwh"/>, dieselbe Regel, mit der
    /// die Simulation sie bucht — und bewertet sie mit dem Arbeitspreis des
    /// Stromträgers. Das ist eine ANZEIGE, keine zweite Buchung: In Energiekosten,
    /// CO₂-Bilanz und BEHG-Abgabe steht dieser Strom weiterhin genau einmal, nämlich
    /// im Netzbezug. Brennstoff- und Elektrokessel werden deshalb nie zu EINER Summe
    /// vermengt: zwei Energieformen mit zwei Preisen.</para>
    ///
    /// <para><b>Anlagenscharf mit Komponentensumme als Rückfall.</b> Trägt die
    /// Position eine <c>ID_Anlage</c> (Schritt 45), zählen nur die Modulzeilen
    /// dieser Anlage — die Zuordnung läuft über den Bezeichner, denn die
    /// Ergebnis-Modulzeilen führen keinen Anlagenschlüssel
    /// (<c>ErgebnisBHKWModulModel.Modul</c> = <c>Tab_Energieanlagen.Bezeichner</c>,
    /// dasselbe Verfahren wie <c>ErdreichAuswertung</c>). Ist die Anlage im Lauf
    /// nicht vertreten, gibt es bewusst KEINE Bezugsgröße statt der Summe einer
    /// anderen Anlage. Ohne <c>ID_Anlage</c> gilt die Summe aller Module der
    /// Komponente.</para>
    ///
    /// <para><b>Preise aus der EINEN Wahrheit.</b> Arbeitspreis je Träger und
    /// Stromträger kommen aus <see cref="KostenEmissionRechner"/>
    /// (<c>ArbeitspreisJeKwh</c>/<c>StromTraegerId</c>) — keine zweite
    /// Preisverrechnung. Fehlt der Preis eines beteiligten Trägers, bleiben die
    /// KOSTEN null (Weg A ohne Basis), der BEDARF in kWh bleibt bestimmbar.</para>
    /// </summary>
    internal sealed class EndenergieAufloeser
    {
        /// <summary>
        /// <c>Tab_KostenKomponente.ID</c> der Wärmepumpe — dieselbe feste Nummer wie
        /// in <c>Tab_KostenKomponente</c> („Wärmepumpe" = 1; Begründung für
        /// die festen Nummern 1…7 dort). BHKW und Heizkessel stehen bereits benannt
        /// in <see cref="BetriebskostenCtrl"/>.
        /// </summary>
        internal const int KOMPONENTE_WAERMEPUMPE = 1;

        /// <summary>ETAPPE H4a: Photovoltaik (3) und Solarthermie (4) — Quelle wie oben.</summary>
        internal const int KOMPONENTE_PHOTOVOLTAIK = 3;
        internal const int KOMPONENTE_SOLARTHERMIE = 4;

        /// <summary>ANWENDERBEFUND 10.09.2026 (H4c): Stromspeicher (5) — Quelle wie oben.</summary>
        internal const int KOMPONENTE_STROMSPEICHER = 5;

        /// <summary>Endenergie einer Position — das Ergebnis des Auflösers.</summary>
        internal sealed class Groesse
        {
            /// <summary>Jahresbedarf [kWh/a] — Basis von Weg B; immer &gt; 0.</summary>
            public double BedarfKwh;

            /// <summary>Arbeitskosten [€/a] — Basis von Weg A; null, wenn ein
            /// beteiligter Träger keinen Arbeitspreis führt.</summary>
            public double? KostenEuro;

            /// <summary>Klartext der Basis für Herleitungen, z. B.
            /// „BHKW ‚Modul 1'" oder „alle Heizkessel-Module" — seit Etappe B6
            /// zweisprachig aus <c>MyResource.Resource.AUFLOESER_*</c>.</summary>
            public string Basis = "";

            /// <summary>
            /// Der Arbeitspreis [€/kWh], mit dem WEG B
            /// (<c>BEMESSUNG_PROZENT_ENDENERGIEBEDARF</c>) den
            /// <see cref="BedarfKwh"/> bewertet; <c>null</c> = nicht gepflegt, dann
            /// hat Weg B keine Bezugsgröße.
            ///
            /// <para><b>Anlagenscharf</b> (Anwenderentscheid 19.09.2026): Bezieht die
            /// Anlage selbst Strom und trägt sie einen eigenen Stromträger
            /// (Wärmepumpe, Heizstab, Elektrokessel), gilt DESSEN Arbeitspreis —
            /// derselbe, mit dem <see cref="KostenEuro"/> ihre Endenergie bewertet.
            /// Eine Anlage mit Brennstoffträger (BHKW, Heizkessel auf Gas oder Öl)
            /// bewertet ihren Hilfsstrom mit dem Stromträger des PROJEKTS
            /// (<see cref="StrompreisJeKwh"/>) — ihr eigener Träger ist Brennstoff und
            /// wäre für eine Strommenge der falsche Preis.</para>
            /// </summary>
            public double? BewertungspreisJeKwh;

            /// <summary><c>true</c> = <see cref="BewertungspreisJeKwh"/> ist der Preis
            /// des EIGENEN Stromträgers der Anlage, <c>false</c> = der des
            /// Projekt-Stromträgers. Er entscheidet, welche ABHILFE genannt wird, wenn
            /// der Preis fehlt (<c>KostenHerleitung.GrundText</c>).</summary>
            public bool EigenerStromtraeger;
        }

        private readonly int _idProjekt;
        private readonly ErgebnisModel _ergebnis;
        private readonly Dictionary<int, string> _anlagenName = new Dictionary<int, string>();

        /// <summary>
        /// Die Bezeichner der Anlagen, deren Kesselgerät auf STROM läuft. Der Schlüssel
        /// ist der Bezeichner, weil die Ergebnis-Modulzeile ihre Anlage als Namen führt
        /// (<c>ErgebnisHeizkesselModulModel.Modul</c>) — dasselbe Verfahren wie bei
        /// <see cref="_anlagenName"/>.
        ///
        /// <para><b>Warum das Gerät und nicht das Brennstoffwort der Zeile.</b> Das Wort
        /// „Strom" schreibt der Lauf erst seit Befund B-1; gespeicherte Läufe von davor
        /// führen die Spalte leer (Projekt 1024 der Testdatenbank). Der Brennstoff des
        /// Geräts gilt dagegen für jede Zeile, alte wie neue — und es ist genau die
        /// Angabe, aus der die Simulation ihr <c>IstStromkessel</c> bildet.</para>
        /// </summary>
        private readonly HashSet<string> _elektrokessel =
            new HashSet<string>(StringComparer.Ordinal);

        private readonly Dictionary<int, double?> _preisJeTraeger = new Dictionary<int, double?>();
        private double? _strompreis;
        private bool _strompreisErmittelt;

        /// <summary>
        /// ANWENDERENTSCHEID 19.09.2026: <c>Tab_Energieanlagen.ID</c> → der EIGENE
        /// Stromträger dieser Anlage. Gefüllt aus
        /// <see cref="ProjektEnergietraegerCtrl.EigeneStromTraeger"/> — derselben
        /// Erkennung, aus der auch die Rangfolge des Projekt-Stromträgers entsteht;
        /// eine zweite Wahrheit über „welcher Träger ist Strom" gibt es nicht.
        /// Anlagen ohne eigenen Stromträger stehen nicht in der Karte.
        /// </summary>
        private Dictionary<int, int> _eigenerStromtraeger;

        /// <summary>ETAPPE E9a: das Szenario, dessen Trägerpreise dieser Auflöser liest
        /// (<c>null</c>/ERWARTET = die Erwartet-Preise).</summary>
        private readonly string _szenario;

        private EndenergieAufloeser(int idProjekt, ErgebnisModel ergebnis, string szenario)
        {
            _idProjekt = idProjekt;
            _ergebnis = ergebnis;
            _szenario = szenario;
        }

        /// <summary>
        /// Baut den Auflöser für ein Projekt. ETAPPE H4a: Auch OHNE Simulationslauf
        /// entsteht eine Instanz (<c>_ergebnis</c> = null) — die Investitions-Bezugsgröße
        /// braucht keinen Lauf. Die Lauf-Größen (Endenergie, kWh-Mengen) liefern dann
        /// null, wie es die Festlegung verlangt („ohne Lauf keine Menge, kein Betrag").
        /// </summary>
        internal static EndenergieAufloeser FuerProjekt(int idProjekt)
        {
            return FuerProjekt(idProjekt, null, 1.0);
        }

        /// <summary>
        /// ETAPPE E9a (vollständige Szenarioabdeckung) — derselbe Auflöser für den
        /// SZENARIOlauf: Die Mengen des Laufs tragen den Mengenfaktor des Szenarios
        /// (<see cref="SzenarioMengen.Ergebnis"/>, E9a‑Q2) und die Preise die Trägerpreise
        /// des Szenarios (<see cref="KostenEmissionRechner.ArbeitspreisJeKwh(int, int, string)"/>,
        /// E9a‑Q3). So bemessen sich die Betriebskosten „% der Endenergie-, Brennstoff- oder
        /// Stromkosten", „je Stunde" und „je kWh" an denselben Mengen und Preisen wie die
        /// Energiekosten des Szenarios. <paramref name="mengenFaktor"/> = 1,0 und ERWARTET
        /// sind der Weg von vor E9a.
        /// </summary>
        internal static EndenergieAufloeser FuerProjekt(int idProjekt, string szenario, double mengenFaktor)
        {
            try
            {
                ErgebnisModel erg = null;
                try { erg = new ErgebnisCtrl().Load(idProjekt); } catch { }
                erg = SzenarioMengen.Ergebnis(erg, mengenFaktor);

                var a = new EndenergieAufloeser(idProjekt, erg, szenario);
                a.AnlagenNamenLaden();
                a.ElektrokesselLaden();
                try { a._eigenerStromtraeger = ProjektEnergietraegerCtrl.EigeneStromTraeger(idProjekt); }
                catch { }
                if (a._eigenerStromtraeger == null) a._eigenerStromtraeger = new Dictionary<int, int>();
                return a;
            }
            catch { return null; }
        }

        /// <summary>Strombezugspreis [€/kWh] des Projekts (Arbeitspreis des
        /// Stromträgers); null = nicht gepflegt. Bewertet Weg B und die
        /// Wärmepumpen-Endenergie.</summary>
        internal double? StrompreisJeKwh
        {
            get
            {
                if (!_strompreisErmittelt)
                {
                    _strompreisErmittelt = true;
                    int carrier = KostenEmissionRechner.StromTraegerId(_idProjekt);
                    _strompreis = carrier > 0
                        ? KostenEmissionRechner.ArbeitspreisJeKwh(_idProjekt, carrier, _szenario)
                        : null;
                }
                return _strompreis;
            }
        }

        /// <summary>
        /// Endenergie der Position — anlagenscharf, sofern <paramref name="idAnlage"/>
        /// gesetzt ist, sonst Komponentensumme. null = keine Bezugsgröße (Komponente
        /// ohne Endenergie, Anlage nicht im Lauf, oder Bedarf 0).
        /// </summary>
        internal Groesse FuerPosition(int komponentenID, int idAnlage)
        {
            string anlagenName = null;
            if (idAnlage > 0 && !_anlagenName.TryGetValue(idAnlage, out anlagenName))
                return null;   // Anlage unbekannt — keine fremde Summe unterstellen

            switch (komponentenID)
            {
                case BetriebskostenCtrl.KOMPONENTE_BHKW:
                    // ETAPPE B6: Die Basis-Texte sind Herleitungsprosa - seit hier
                    // zweisprachig wie jeder andere Anzeigetext. Das Komponentenwort
                    // steht getrennt vom Satzbau, weil beide Sprachen es anders beugen.
                    return Brennstoffsumme(BhkwModule(), anlagenName,
                                           MyResource.Resource.AUFLOESER_KOMP_BHKW);
                case BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL:
                    return Kesselsumme(anlagenName, idAnlage);
                case KOMPONENTE_WAERMEPUMPE:
                    return Waermepumpensumme(anlagenName, idAnlage);
                default:
                    return null;   // § 4.5: keine Endenergie — nur fester Jahresbetrag
            }
        }

        /// <summary>
        /// ANWENDERENTSCHEID 19.09.2026 — DER PREIS EINER STROMANLAGE.
        ///
        /// <para>Eine Anlage, die selbst Strom bezieht (Wärmepumpe, Heizstab,
        /// Elektrokessel), wird mit dem Arbeitspreis IHRES Stromträgers bewertet,
        /// sobald sie einen eigenen trägt; sonst gilt der Stromträger des Projekts
        /// (<see cref="StrompreisJeKwh"/>). Damit liegt hinter Weg A
        /// (<c>KostenEuro</c>) und Weg B (<c>BewertungspreisJeKwh</c>) EIN Preis —
        /// zwei verschiedene wären an derselben Anlage nicht erklärbar.</para>
        ///
        /// <para>Ohne Anlagenbezug (<paramref name="idAnlage"/> 0 = Komponentensumme)
        /// gibt es keine EINE Anlage und damit keinen eigenen Träger: Dann bleibt es
        /// beim Projekt-Stromträger.</para>
        /// </summary>
        /// <param name="eigener"><c>true</c> = der gelieferte Preis ist der des
        /// eigenen Trägers der Anlage.</param>
        private double? Strompreis(int idAnlage, out bool eigener)
        {
            eigener = false;
            int carrier;
            if (idAnlage > 0 && _eigenerStromtraeger != null &&
                _eigenerStromtraeger.TryGetValue(idAnlage, out carrier) && carrier > 0)
            {
                eigener = true;
                return Preis(carrier);
            }
            return StrompreisJeKwh;
        }

        // ------------------------------------------------------------------ intern

        private void AnlagenNamenLaden()
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID, Bezeichner FROM Tab_Energieanlagen WHERE ID_Projekt = ?",
                    new DbParam("@p", _idProjekt));
                if (dt == null) return;
                foreach (DataRow r in dt.Rows)
                {
                    if (r["ID"] == DBNull.Value) continue;
                    _anlagenName[Convert.ToInt32(r["ID"])] =
                        r["Bezeichner"] == DBNull.Value ? "" : Convert.ToString(r["Bezeichner"]);
                }
            }
            catch { }
        }

        /// <summary>
        /// Die Anlagenbezeichner der ELEKTROKESSEL des Projekts — der Verbund
        /// <c>Tab_Energieanlagen</c> → <c>Tab_Heizkessel</c> über den Kesselverweis der
        /// Anlagenzeile, gefiltert auf <see cref="SimulationSPK.BRENNSTOFF_STROM"/>.
        /// Leere Menge = kein Elektrokessel, Abfrage gescheitert oder altes Schema; dann
        /// bleibt alles wie zuvor.
        /// </summary>
        private void ElektrokesselLaden()
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT a.Bezeichner FROM Tab_Energieanlagen AS a " +
                    "INNER JOIN Tab_Heizkessel AS k ON a.ID_Kessel = k.ID " +
                    "WHERE a.ID_Projekt = ? AND k.Brennstoff = ?",
                    new DbParam("@p", _idProjekt),
                    new DbParam("@b", SimulationSPK.BRENNSTOFF_STROM));
                if (dt == null) return;
                foreach (DataRow r in dt.Rows)
                {
                    if (r[0] == DBNull.Value) continue;
                    string name = Convert.ToString(r[0]);
                    if (!string.IsNullOrEmpty(name)) _elektrokessel.Add(name);
                }
            }
            catch { }
        }

        /// <summary>Läuft die Kessel-Modulzeile dieses Namens auf Strom?</summary>
        private bool IstElektrokessel(string modul)
        {
            return _elektrokessel.Count > 0 && _elektrokessel.Contains(modul ?? "");
        }

        /// <summary>
        /// Die NUTZWÄRME einer Kessel-Modulzeile [MWh/a] — die Bezugsgröße von
        /// „je kWh thermisch“ am Heizkessel.
        ///
        /// <para><b>Gelesen wird die Spalte, abgeleitet nur der Rest</b> — dieselbe
        /// Ordnung wie bei <see cref="HilfsstromRechner.KesselBrennstoffMWh"/>.
        /// <c>Waermeproduktion</c> schreibt der Lauf erst seit Befund B-1; ein
        /// GESPEICHERTER Lauf von davor trägt dort 0, führt die Wärme aber auf ihren
        /// beiden Kanälen. Die Ableitung ist keine Näherung, sondern die Definition
        /// selbst (<c>SimulationRunner</c>: <c>Waermeproduktion = Waerme_Gas +
        /// Waerme_Oel</c>); Bericht und Variantenvergleich lesen die Zeile seit jeher
        /// nach genau dieser Regel.</para>
        /// </summary>
        private static double KesselNutzwaermeMWh(ErgebnisHeizkesselModulModel m)
        {
            return m.Waermeproduktion > 0 ? m.Waermeproduktion : m.Waerme_Gas + m.Waerme_Oel;
        }

        private double? Preis(int carrierId)
        {
            double? p;
            if (_preisJeTraeger.TryGetValue(carrierId, out p)) return p;
            p = KostenEmissionRechner.ArbeitspreisJeKwh(_idProjekt, carrierId, _szenario);
            _preisJeTraeger[carrierId] = p;
            return p;
        }

        private sealed class Brennstoffzeile
        {
            public string Modul;
            public double VerbrauchMWh;
            public int CarrierId;
        }

        private List<Brennstoffzeile> BhkwModule()
        {
            var liste = new List<Brennstoffzeile>();
            if (_ergebnis != null && _ergebnis.BHKW != null && _ergebnis.BHKW.Module != null)
                foreach (ErgebnisBHKWModulModel m in _ergebnis.BHKW.Module)
                    liste.Add(new Brennstoffzeile { Modul = m.Modul, VerbrauchMWh = m.Verbrauch, CarrierId = m.CarrierId });
            return liste;
        }

        /// <summary>
        /// Die Kessel-Modulzeilen, nach Welt getrennt. <paramref name="elektro"/>
        /// <c>true</c> = die ELEKTROkessel mit ihrem Stromeinsatz
        /// (<see cref="SimulationSPK.StromeinsatzElektrokesselMwh"/>), <c>false</c> =
        /// die Brennstoffkessel mit ihrem <c>Verbrauch</c>. Die beiden Listen sind
        /// disjunkt; zusammengelegt würden zwei Energieformen mit zwei Preisen zu einer
        /// Zahl verrührt.
        /// </summary>
        private List<Brennstoffzeile> KesselModule(bool elektro)
        {
            var liste = new List<Brennstoffzeile>();
            if (_ergebnis == null || _ergebnis.Heizkessel == null || _ergebnis.Heizkessel.Module == null)
                return liste;

            foreach (ErgebnisHeizkesselModulModel m in _ergebnis.Heizkessel.Module)
            {
                if (IstElektrokessel(m.Modul) != elektro) continue;
                liste.Add(new Brennstoffzeile
                {
                    Modul = m.Modul,
                    VerbrauchMWh = elektro
                        ? SimulationSPK.StromeinsatzElektrokesselMwh(m.Waerme_Gas, m.Waerme_Oel)
                        // ANWENDERBEFUND 19.09.2026 (#363): NICHT die rohe Spalte.
                        // Tab_ErgebnisHeizkesselModul.Verbrauch führt der Lauf erst seit
                        // Befund B-1; ein GESPEICHERTER Lauf von davor trägt dort 0 —
                        // und dann stand „% der Endenergiekosten" ohne Bezugsgröße da,
                        // obwohl derselbe Lauf Wärme und Nutzungsgrad führt. Der
                        // Rückfall ist die EINE, schon vorhandene Ableitung der
                        // Steuerseite (Etappe B3 Paket a), keine zweite Wahrheit; bei
                        // gefüllter Spalte gibt sie unverändert deren Wert zurück.
                        : HilfsstromRechner.KesselBrennstoffMWh(m),
                    CarrierId = m.CarrierId
                });
            }
            return liste;
        }

        /// <summary>Brennstoff-Endenergie (BHKW/Heizkessel): Σ Verbrauch, Kosten je
        /// Modul zum Arbeitspreis seines Trägers.</summary>
        private Groesse Brennstoffsumme(List<Brennstoffzeile> module, string anlagenName, string komponentenWort)
        {
            double bedarfKwh = 0;
            double kosten = 0;
            bool kostenVollstaendig = true;
            int getroffen = 0;

            foreach (Brennstoffzeile m in module)
            {
                if (anlagenName != null &&
                    !string.Equals(m.Modul ?? "", anlagenName, StringComparison.Ordinal))
                    continue;

                getroffen++;
                if (m.VerbrauchMWh <= 0) continue;
                bedarfKwh += m.VerbrauchMWh * 1000.0;

                double? preis = Preis(m.CarrierId);
                if (preis.HasValue) kosten += m.VerbrauchMWh * 1000.0 * preis.Value;
                else kostenVollstaendig = false;
            }

            if (anlagenName != null && getroffen == 0) return null;   // Anlage nicht im Lauf
            if (bedarfKwh <= 0) return null;

            return new Groesse
            {
                BedarfKwh = bedarfKwh,
                KostenEuro = kostenVollstaendig ? kosten : (double?)null,
                // Weg B bewertet die BRENNSTOFFmenge dieser Anlage mit Strom: Der
                // Hilfsstrom eines BHKW oder eines Gas-/Ölkessels kommt aus dem
                // Netzbezug des Projekts, nicht aus seinem Brennstoffträger.
                BewertungspreisJeKwh = StrompreisJeKwh,
                EigenerStromtraeger = false,
                Basis = anlagenName != null
                    ? string.Format(CultureInfo.CurrentCulture,
                                    MyResource.Resource.AUFLOESER_BASIS_ANLAGE,
                                    komponentenWort, anlagenName)
                    : string.Format(CultureInfo.CurrentCulture,
                                    MyResource.Resource.AUFLOESER_BASIS_ALLE, komponentenWort)
            };
        }

        /// <summary>
        /// Endenergie der Komponente HEIZKESSEL — getrennt nach den zwei Welten
        /// (E1, Anwenderentscheid 18.09.2026).
        ///
        /// <para><b>Anlagenscharf</b> (die Position trägt eine <c>ID_Anlage</c>): Der
        /// Kessel ist entweder ein Elektro- oder ein Brennstoffkessel — die Frage ist
        /// eindeutig, und die Antwort kommt aus der Welt DIESES Kessels.</para>
        ///
        /// <para><b>Als Komponentensumme</b> (ohne <c>ID_Anlage</c>): Es gilt die
        /// BRENNSTOFFsumme, solange es eine gibt; führt das Projekt ausschließlich
        /// Elektrokessel, gilt ihre Stromsumme. Gemischt wird nie: Der Brennstoff der
        /// einen Kessel und der Netzbezugsstrom der anderen sind zwei Energieformen mit
        /// zwei Preisen, und eine gemeinsame Zahl wäre für beide Bemessungen
        /// („% des Endenergiebedarfs", „% der Endenergiekosten") keine Basis, sondern
        /// eine Vermengung. Die Anzeige der Kostenseite stellt sie deshalb ohnehin als
        /// zwei Zeilen dar — sie fragt je Anlage.</para>
        /// </summary>
        private Groesse Kesselsumme(string anlagenName, int idAnlage)
        {
            bool elektro = anlagenName != null && IstElektrokessel(anlagenName);

            if (elektro) return Elektrokesselsumme(anlagenName, idAnlage);

            Groesse brennstoff = Brennstoffsumme(KesselModule(false), anlagenName,
                                                 MyResource.Resource.AUFLOESER_KOMP_HEIZKESSEL);
            if (brennstoff != null || anlagenName != null) return brennstoff;

            // Reines Elektrokesselprojekt: Die Brennstoffwelt ist leer, die Stromwelt
            // nicht. Ohne diesen Zweig bliebe die Komponente ohne jede Bezugsgröße.
            return Elektrokesselsumme(null, 0);
        }

        /// <summary>
        /// Strom-Endenergie der ELEKTROKESSEL: Stromeinsatz × Strombezugspreis, nach
        /// demselben Muster wie <see cref="Waermepumpensumme"/>. Die Herkunft nennt den
        /// Netzbezug — dort und nur dort wird diese Energie bepreist.
        /// </summary>
        private Groesse Elektrokesselsumme(string anlagenName, int idAnlage)
        {
            double bedarfKwh = 0;
            int getroffen = 0;
            foreach (Brennstoffzeile m in KesselModule(true))
            {
                if (anlagenName != null &&
                    !string.Equals(m.Modul ?? "", anlagenName, StringComparison.Ordinal))
                    continue;
                getroffen++;
                if (m.VerbrauchMWh > 0) bedarfKwh += m.VerbrauchMWh * 1000.0;
            }

            if (anlagenName != null && getroffen == 0) return null;   // Anlage nicht im Lauf
            if (bedarfKwh <= 0) return null;

            bool eigener;
            double? preis = Strompreis(idAnlage, out eigener);
            string kern = anlagenName != null
                ? string.Format(CultureInfo.CurrentCulture,
                                MyResource.Resource.AUFLOESER_BASIS_ANLAGE,
                                MyResource.Resource.AUFLOESER_KOMP_ELEKTROKESSEL, anlagenName)
                : string.Format(CultureInfo.CurrentCulture,
                                MyResource.Resource.AUFLOESER_BASIS_ALLE,
                                MyResource.Resource.AUFLOESER_KOMP_ELEKTROKESSEL);

            return new Groesse
            {
                BedarfKwh = bedarfKwh,
                KostenEuro = preis.HasValue ? bedarfKwh * preis.Value : (double?)null,
                BewertungspreisJeKwh = preis,
                EigenerStromtraeger = eigener,
                Basis = string.Format(CultureInfo.CurrentCulture,
                                      MyResource.Resource.AUFLOESER_BASIS_NETZBEZUG, kern)
            };
        }

        /// <summary>Strom-Endenergie der Wärmepumpe: (Stromverbrauch + Heizstab) ×
        /// Strombezugspreis.</summary>
        private Groesse Waermepumpensumme(string anlagenName, int idAnlage)
        {
            if (_ergebnis == null || _ergebnis.Waermepumpe == null || _ergebnis.Waermepumpe.Module == null) return null;

            double bedarfKwh = 0;
            int getroffen = 0;
            foreach (ErgebnisWaermepumpeModulModel m in _ergebnis.Waermepumpe.Module)
            {
                if (anlagenName != null &&
                    !string.Equals(m.Modul ?? "", anlagenName, StringComparison.Ordinal))
                    continue;
                getroffen++;
                bedarfKwh += (m.Stromverbrauch + m.Heizstab) * 1000.0;
            }

            if (anlagenName != null && getroffen == 0) return null;
            if (bedarfKwh <= 0) return null;

            bool eigener;
            double? preis = Strompreis(idAnlage, out eigener);
            return new Groesse
            {
                BedarfKwh = bedarfKwh,
                KostenEuro = preis.HasValue ? bedarfKwh * preis.Value : (double?)null,
                BewertungspreisJeKwh = preis,
                EigenerStromtraeger = eigener,
                Basis = anlagenName != null
                    ? string.Format(CultureInfo.CurrentCulture,
                                    MyResource.Resource.AUFLOESER_BASIS_ANLAGE,
                                    MyResource.Resource.AUFLOESER_KOMP_WAERMEPUMPE, anlagenName)
                    // Eigener Schlüssel statt „alle {0}-Module": Die Mehrzahl der
                    // Wärmepumpe heißt nicht wie ihre Einzahl, und ein Satzbaukasten,
                    // der das übergeht, erzeugt in jeder Sprache einen Fehler.
                    : MyResource.Resource.AUFLOESER_BASIS_ALLE_WP
            };
        }

        // ================================================================ ETAPPE H4a
        // Lauf-Bezugsgrößen der KD1-Bemessungsarten (Konzept Kostendialoge § 5.3):
        // „je kWh thermisch" = erzeugte Wärme, „je kWh elektrisch" = erzeugter bzw.
        // bezogener Strom aus dem Simulationslauf — anlagenscharf über den
        // Bezeichner, Komponentensumme als Rückfall (dasselbe Verfahren wie die
        // Endenergie oben). null = keine Basis (kein Lauf, Komponente ohne die
        // Größe, Anlage nicht im Lauf, Summe 0) — nie eine Fantasiezahl.

        /// <summary>Erzeugte Wärme [kWh/a] der Komponente bzw. Anlage; null = keine Basis.</summary>
        internal double? WaermeerzeugungKwh(int komponentenID, int idAnlage)
        {
            string anlagenName = null;
            if (idAnlage > 0 && !_anlagenName.TryGetValue(idAnlage, out anlagenName)) return null;

            // Die Brennstoffzeile dient hier nur als (Modul, MWh)-Paar — befüllt wird
            // sie mit der WÄRMEproduktion, nicht mit dem Brennstoff.
            var zeilen = new List<Brennstoffzeile>();
            switch (komponentenID)
            {
                case BetriebskostenCtrl.KOMPONENTE_BHKW:
                    if (_ergebnis != null && _ergebnis.BHKW != null && _ergebnis.BHKW.Module != null)
                        foreach (ErgebnisBHKWModulModel m in _ergebnis.BHKW.Module)
                            zeilen.Add(new Brennstoffzeile { Modul = m.Modul, VerbrauchMWh = m.Waermeproduktion });
                    break;
                case BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL:
                    if (_ergebnis != null && _ergebnis.Heizkessel != null && _ergebnis.Heizkessel.Module != null)
                        foreach (ErgebnisHeizkesselModulModel m in _ergebnis.Heizkessel.Module)
                            zeilen.Add(new Brennstoffzeile { Modul = m.Modul, VerbrauchMWh = KesselNutzwaermeMWh(m) });
                    break;
                case KOMPONENTE_WAERMEPUMPE:
                    if (_ergebnis != null && _ergebnis.Waermepumpe != null && _ergebnis.Waermepumpe.Module != null)
                        foreach (ErgebnisWaermepumpeModulModel m in _ergebnis.Waermepumpe.Module)
                            zeilen.Add(new Brennstoffzeile { Modul = m.Modul, VerbrauchMWh = m.Waermeproduktion });
                    break;
                case KOMPONENTE_SOLARTHERMIE:
                    if (_ergebnis != null && _ergebnis.Solarthermie != null && _ergebnis.Solarthermie.Module != null)
                        foreach (ErgebnisSolarthermieModulModel m in _ergebnis.Solarthermie.Module)
                            zeilen.Add(new Brennstoffzeile { Modul = m.Modul, VerbrauchMWh = m.Waermeproduktion });
                    break;
                default:
                    return null;
            }

            return SummeKwh(zeilen, anlagenName);
        }

        /// <summary>Strommenge [kWh/a] der Komponente bzw. Anlage — erzeugt bei
        /// BHKW/Photovoltaik, bezogen bei der Wärmepumpe (Stromverbrauch + Heizstab)
        /// und beim ELEKTROKESSEL (sein Stromeinsatz, E1), ENTLADEN beim Stromspeicher
        /// (H4c); null = keine Basis.</summary>
        internal double? StromgroesseKwh(int komponentenID, int idAnlage)
        {
            string anlagenName = null;
            if (idAnlage > 0 && !_anlagenName.TryGetValue(idAnlage, out anlagenName)) return null;

            // ANWENDERBEFUND 10.09.2026 (H4c): Der Stromspeicher hat eine Strommenge —
            // aber keine Modulliste und keine MWh. Er geht deshalb einen eigenen Weg,
            // NICHT durch SummeKwh (Begründung an SpeicherEntladungKwh).
            if (komponentenID == KOMPONENTE_STROMSPEICHER) return SpeicherEntladungKwh(idAnlage);

            var zeilen = new List<Brennstoffzeile>();
            switch (komponentenID)
            {
                case BetriebskostenCtrl.KOMPONENTE_BHKW:
                    if (_ergebnis != null && _ergebnis.BHKW != null && _ergebnis.BHKW.Module != null)
                        foreach (ErgebnisBHKWModulModel m in _ergebnis.BHKW.Module)
                            zeilen.Add(new Brennstoffzeile { Modul = m.Modul, VerbrauchMWh = m.Stromproduktion });
                    break;
                case KOMPONENTE_PHOTOVOLTAIK:
                    if (_ergebnis != null && _ergebnis.Photovoltaik != null && _ergebnis.Photovoltaik.Module != null)
                        foreach (ErgebnisPhotovoltaikModulModel m in _ergebnis.Photovoltaik.Module)
                            zeilen.Add(new Brennstoffzeile { Modul = m.Modul, VerbrauchMWh = m.Stromproduktion });
                    break;
                case KOMPONENTE_WAERMEPUMPE:
                    if (_ergebnis != null && _ergebnis.Waermepumpe != null && _ergebnis.Waermepumpe.Module != null)
                        foreach (ErgebnisWaermepumpeModulModel m in _ergebnis.Waermepumpe.Module)
                            zeilen.Add(new Brennstoffzeile { Modul = m.Modul, VerbrauchMWh = m.Stromverbrauch + m.Heizstab });
                    break;
                case BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL:
                    // E1: Nur der ELEKTROKESSEL hat am Heizkessel eine elektrische
                    // Größe — sein Stromeinsatz. Ein Brennstoffkessel hat keine, und
                    // eine Position „je kWh elektrisch" an ihm bekommt weiterhin keine
                    // Basis statt einer erfundenen Zahl.
                    zeilen.AddRange(KesselModule(true));
                    break;
                default:
                    return null;
            }

            return SummeKwh(zeilen, anlagenName);
        }

        /// <summary>
        /// ANWENDERBEFUND 10.09.2026 (H4c): die ENTLADENE Jahresenergie [kWh/a] des
        /// Stromspeichers aus dem jüngsten Lauf — die Bezugsgröße von „je kWh
        /// elektrisch" an diesem Gewerk.
        ///
        /// <para><b>Warum die Entladung und nicht die Ladung.</b> Ein Wartungssatz
        /// „€ je kWh" bemisst sich an der NUTZBAREN Arbeit des Speichers; die Ladung
        /// enthält zusätzlich die Verluste (<c>Ladung_Gesamt</c> =
        /// <c>Entladung_Gesamt</c> + <c>Verluste_Gesamt</c>, Fachkonzept Stromspeicher
        /// 7.1). Der Anwender wollte „was der Speicher im Jahr geleistet hat".</para>
        ///
        /// <para><b>Warum ein eigener Weg statt <see cref="SummeKwh"/>.</b> Zwei
        /// Unterschiede zu den Erzeugerzeilen, beide erhoben statt vermutet: Die
        /// Speicherzeile trägt ihre Anlage als SCHLÜSSEL
        /// (<c>Tab_ErgebnisStromspeicher.ID_Energieanlage</c>) statt als Bezeichner —
        /// die Zuordnung ist also genauer als der Namensvergleich der anderen Gewerke —,
        /// und ihre Energien stehen in <b>kWh/a</b>, nicht in MWh/a. Durch
        /// <see cref="SummeKwh"/> geschickt wäre die Zahl tausendfach zu groß.</para>
        ///
        /// <para>null = kein Lauf, keine Speicherzeile im Lauf, diese Anlage nicht im
        /// Lauf oder Summe 0 — dann greift der Anwenderentscheid I-2 wie überall.</para>
        /// </summary>
        private double? SpeicherEntladungKwh(int idAnlage)
        {
            if (_ergebnis == null || _ergebnis.Stromspeicher == null) return null;

            double kwh = 0;
            int getroffen = 0;
            foreach (ErgebnisStromspeicherModel s in _ergebnis.Stromspeicher)
            {
                if (idAnlage > 0 && s.ID_Energieanlage != idAnlage) continue;
                getroffen++;
                if (s.Entladung_Gesamt > 0) kwh += s.Entladung_Gesamt;
            }

            if (idAnlage > 0 && getroffen == 0) return null;   // Anlage nicht im Lauf
            return kwh > 0 ? kwh : (double?)null;
        }

        /// <summary>Anlagen-/Komponentensumme in kWh nach den H2-Filterregeln.</summary>
        private double? SummeKwh(List<Brennstoffzeile> zeilen, string anlagenName)
        {
            double kwh = 0;
            int getroffen = 0;
            foreach (Brennstoffzeile m in zeilen)
            {
                if (anlagenName != null &&
                    !string.Equals(m.Modul ?? "", anlagenName, StringComparison.Ordinal))
                    continue;
                getroffen++;
                if (m.VerbrauchMWh > 0) kwh += m.VerbrauchMWh * 1000.0;
            }
            if (anlagenName != null && getroffen == 0) return null;
            return kwh > 0 ? kwh : (double?)null;
        }

        // ================================================================ PAKET FX2
        // ANWENDERENTSCHEID B-4 (02.09.2026): „,je Stunde' (EUR_PRO_H) ist ein fester
        // Wert, wie bei Wartungskosten in € pro erzeugter Strommenge — nur die Summe
        // über den Betrachtungszeitraum wird jeweils aus dem Lauf ermittelt."
        // Also dasselbe Muster wie EUR_PRO_KWH_ELEKTRISCH (H4a): der SATZ [€/h] bleibt
        // Eingabe, die MENGE [h/a] kommt frisch aus dem jüngsten Lauf; die gespeicherte
        // Menge bleibt Konserve, wenn frisch nichts zu holen ist (H2-1-Ordnung).

        /// <summary>
        /// PAKET FX2 (Anwenderentscheid B-4): Stundenzahl [h/a] der Komponente bzw.
        /// Anlage aus dem jüngsten Lauf — die Bezugsmenge der Bemessung „je Stunde".
        /// null = keine Basis (kein Lauf, Komponente ohne Stundengröße, Anlage nicht
        /// im Lauf, Summe 0); dann gilt weiter die gespeicherte Menge.
        /// </summary>
        /// <remarks>
        /// <para><b>Was der Rechenkern hergibt — erhoben, nicht geraten.</b></para>
        /// <list type="bullet">
        /// <item><description><b>Wärmepumpe (1): ECHTE Betriebsstunden.</b>
        /// <c>ErgebnisWaermepumpeModulModel.Betriebsstunden</c> zählt die Stunden, in
        /// denen das Modul läuft (<c>SimulationWaermepumpe.Modul_WP_Laufzeit</c>,
        /// Teilstunden anteilig, Guard <c>result[PTHERM] &gt; 0</c> aus B0-13). Das ist
        /// die Größe, die die Bemessung meint.</description></item>
        /// <item><description><b>BHKW (7): benannte NÄHERUNG.</b> Die Modulzeile führt
        /// nur <c>VbhThermisch</c> = Wärme ÷ Wärmeleistung
        /// (<c>SimulationBHKW.Laufzeiten</c>) und <c>VbhElektrisch</c> — beides
        /// VOLLBENUTZUNGSstunden. Taktung und Teillast bildet der Rechenkern nicht ab;
        /// ein Modul, das ein Jahr lang halb moduliert läuft, hat 8.760 Betriebsstunden
        /// und 4.380 thermische Vbh. Genommen wird <c>VbhThermisch</c> — dieselbe
        /// Größe, die eine Position mit der Bemessung „je Stunde" am BHKW als
        /// Bezugsgröße trägt — die Vollbenutzungsstunden des BHKW, die
        /// <see cref="DbWerte.BEMESSUNG_EUR_PRO_H"/> als Näherung ausweist. Eine zweite
        /// Wahrheit wäre schlimmer als die benannte Näherung.</description></item>
        /// <item><description><b>Heizkessel (2) und alle übrigen: null.</b> Die
        /// Kessel-Modulzeile führt WEDER Betriebsstunden NOCH Vollbenutzungsstunden
        /// (Spalten: Waerme_Gas, Waerme_Oel, Waermeproduktion, Brennstoff, Verbrauch,
        /// Jahresnutzungsgrad, carrier_id, Hilfsenergie). Es gibt nichts zu ermitteln —
        /// dann lieber keine Zahl als eine erfundene.</description></item>
        /// </list>
        /// </remarks>
        internal double? BetriebsstundenH(int komponentenID, int idAnlage)
        {
            string anlagenName = null;
            if (idAnlage > 0 && !_anlagenName.TryGetValue(idAnlage, out anlagenName)) return null;

            // Die Brennstoffzeile dient auch hier nur als (Modul, Zahl)-Paar; das Feld
            // trägt STUNDEN, nicht MWh — deshalb summiert SummeStunden ohne Faktor.
            var zeilen = new List<Brennstoffzeile>();
            switch (komponentenID)
            {
                case KOMPONENTE_WAERMEPUMPE:
                    if (_ergebnis != null && _ergebnis.Waermepumpe != null && _ergebnis.Waermepumpe.Module != null)
                        foreach (ErgebnisWaermepumpeModulModel m in _ergebnis.Waermepumpe.Module)
                            zeilen.Add(new Brennstoffzeile { Modul = m.Modul, VerbrauchMWh = m.Betriebsstunden });
                    break;
                case BetriebskostenCtrl.KOMPONENTE_BHKW:
                    if (_ergebnis != null && _ergebnis.BHKW != null && _ergebnis.BHKW.Module != null)
                        foreach (ErgebnisBHKWModulModel m in _ergebnis.BHKW.Module)
                            zeilen.Add(new Brennstoffzeile { Modul = m.Modul, VerbrauchMWh = m.VbhThermisch });
                    break;
                default:
                    return null;   // Heizkessel, Solar, PV, Speicher: keine Stundengröße
            }

            return SummeStunden(zeilen, anlagenName);
        }

        /// <summary>Anlagen-/Komponentensumme in Stunden — dieselben H2-Filterregeln
        /// wie <see cref="SummeKwh"/>, nur ohne die MWh→kWh-Umrechnung.</summary>
        private double? SummeStunden(List<Brennstoffzeile> zeilen, string anlagenName)
        {
            double h = 0;
            int getroffen = 0;
            foreach (Brennstoffzeile m in zeilen)
            {
                if (anlagenName != null &&
                    !string.Equals(m.Modul ?? "", anlagenName, StringComparison.Ordinal))
                    continue;
                getroffen++;
                if (m.VerbrauchMWh > 0) h += m.VerbrauchMWh;
            }
            if (anlagenName != null && getroffen == 0) return null;
            return h > 0 ? h : (double?)null;
        }

        // =====================================================================
        // ANWENDERBEFUND 19.09.2026 — WARUM DIE LAUFGRÖSSE FEHLT
        //
        //   „Die Hilfsenergiekosten werden nicht berechnet auch nach Simulation.
        //    Korrigiere und prüfe andere Felder mit Abhängigkeiten."
        //
        // Die Landkarte in WirtschaftlichkeitCtrl.BasisGrund beantwortet nur EINE
        // Frage: Kennt das Gewerk diese Größe überhaupt? Wo es sie kennt, sagte sie
        // ausnahmslos BASISGRUND_LAUF — „kein Simulationslauf". Der Anwender las das
        // an einer Zeile, deren Lauf samt Menge dastand und der allein der
        // ARBEITSPREIS des Energieträgers fehlte, und suchte den Fehler an der
        // falschen Stelle. Vier Lagen sehen im Raster gleich aus und verlangen vier
        // verschiedene Handgriffe:
        //
        //   kein Lauf              → Simulation starten          (BASISGRUND_LAUF)
        //   Anlage nicht im Lauf   → Kaskadenplatz vergeben      (BASISGRUND_ANLAGE)
        //   Menge des Laufs ist 0  → nichts zu tun, benennen     (BASISGRUND_MENGE)
        //   Arbeitspreis fehlt     → Energieträgerverwaltung     (BASISGRUND_PREIS)
        //
        // ANWENDERENTSCHEID 19.09.2026: Der letzte Fall zerfällt auf Weg B in zwei —
        // der eigene Stromträger der Anlage (BASISGRUND_PREIS) und der Stromträger des
        // Projekts (BASISGRUND_STROMPREIS). Es sind zwei Einträge in der
        // Energieträgerverwaltung; wer am falschen pflegt, ändert nichts.
        //
        // Der Auflöser hält alles in der Hand, was die Unterscheidung braucht — den
        // Lauf, die Anlagen des Laufs und die Preise. Er rechnet dafür nichts Neues,
        // sondern fragt dieselben Wege noch einmal; gefragt wird nur, wenn die Zeile
        // KEINE Bezugsgröße hat (der Ausnahmefall, nicht der Regelfall).
        // =====================================================================

        /// <summary>Gibt es überhaupt einen gespeicherten Lauf? <c>false</c> = der
        /// Grund heißt <see cref="WirtschaftlichkeitCtrl.BASISGRUND_LAUF"/>.</summary>
        internal bool LaufVorhanden
        {
            get { return _ergebnis != null; }
        }

        // =====================================================================
        // ETAPPE E7c — B‑4 Rest: die zwei projektweiten Alt-Arten frisch aus dem Lauf
        // =====================================================================

        /// <summary>
        /// ETAPPE E7c (B‑4 Rest) — die Bezugsgröße von
        /// <c>PROZENT_BRENNSTOFFKOSTEN</c>: die <b>projektweiten Brennstoffkosten</b>
        /// [€/a] des jüngsten Laufs — Σ Verbrauch × Arbeitspreis über alle
        /// Brennstoffmodule (BHKW und Brennstoffkessel; Elektrokessel bleiben in der
        /// Stromwelt). Es ist Weg A („% der Endenergiekosten"), nur projektweit statt
        /// anlagenscharf — und damit derselbe Rechenweg wie dort (<c>Brennstoffsumme</c>).
        /// <c>null</c> = kein Lauf, kein Brennstoff oder ein Träger ohne Arbeitspreis;
        /// dann bleibt die gepflegte Menge (Konserve).
        /// </summary>
        internal double? BrennstoffkostenProjektEuro()
        {
            if (_ergebnis == null) return null;
            var alle = new List<Brennstoffzeile>(BhkwModule());
            alle.AddRange(KesselModule(false));
            Groesse g = Brennstoffsumme(alle, null, MyResource.Resource.AUFLOESER_KOMP_BHKW);
            return g != null ? g.KostenEuro : null;
        }

        /// <summary>
        /// ETAPPE E7c (B‑4 Rest) — die Bezugsgröße von <c>PROZENT_STROMKOSTEN</c>: die
        /// <b>projektweiten Stromkosten</b> [€/a] des jüngsten Laufs — der Netzbezug
        /// (<c>Stromrestbedarf</c>, darin Wärmepumpe, Heizstab und Elektrokessel) × dem
        /// Arbeitspreis des Projekt-Stromträgers; dieselbe Bewertung wie Weg A an einer
        /// Stromanlage ohne eigenen Träger. <c>null</c> = kein Lauf, kein Netzbezug oder
        /// kein Strompreis.
        /// </summary>
        internal double? StromkostenProjektEuro()
        {
            double mwh = NetzbezugMWh();
            if (mwh <= 0) return null;
            double? preis = StrompreisJeKwh;
            if (!preis.HasValue) return null;
            return mwh * 1000.0 * preis.Value;
        }

        /// <summary>Der Netzbezug des Laufs [MWh/a] (<c>Energiebedarf.Stromrestbedarf</c>);
        /// 0 ohne Lauf oder ohne Energiebedarfszeile.</summary>
        private double NetzbezugMWh()
        {
            return _ergebnis != null && _ergebnis.Energiebedarf != null
                ? _ergebnis.Energiebedarf.Stromrestbedarf : 0.0;
        }

        /// <summary>
        /// ETAPPE E7c (B‑4 Rest): WARUM eine der zwei projektweiten Arten keine
        /// Bezugsgröße hat — kein Lauf, keine Menge (kein Brennstoff bzw. kein
        /// Netzbezug) oder kein Arbeitspreis. Leer = sie hat eine. Die Steuerwerte sind
        /// dieselben wie bei <see cref="GrundOhneBasis"/>.
        /// </summary>
        internal string GrundOhneProjektkosten(string bem)
        {
            if (_ergebnis == null) return WirtschaftlichkeitCtrl.BASISGRUND_LAUF;
            if (string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_STROMKOSTEN, StringComparison.Ordinal))
            {
                if (NetzbezugMWh() <= 0) return WirtschaftlichkeitCtrl.BASISGRUND_MENGE;
                return StrompreisJeKwh.HasValue ? "" : WirtschaftlichkeitCtrl.BASISGRUND_STROMPREIS;
            }
            if (BrennstoffkostenProjektEuro().HasValue) return "";
            double menge = 0;
            foreach (Brennstoffzeile m in BhkwModule()) if (m.VerbrauchMWh > 0) menge += m.VerbrauchMWh;
            foreach (Brennstoffzeile m in KesselModule(false)) if (m.VerbrauchMWh > 0) menge += m.VerbrauchMWh;
            return menge > 0 ? WirtschaftlichkeitCtrl.BASISGRUND_PREIS : WirtschaftlichkeitCtrl.BASISGRUND_MENGE;
        }

        /// <summary>
        /// Warum trägt diese Position keine Laufgröße? Rückgabe ist einer der
        /// <c>WirtschaftlichkeitCtrl.BASISGRUND_*</c>-Steuerwerte.
        ///
        /// <para><b>Nur für Arten, deren Größe aus dem Lauf kommt.</b> Der Aufrufer
        /// fragt erst, wenn die Landkarte
        /// (<see cref="WirtschaftlichkeitCtrl.BasisGrund(string, int)"/>) den Lauf
        /// benannt hat — hier wird die Antwort nur GENAUER, nie eine andere.</para>
        /// </summary>
        internal string GrundOhneBasis(int komponentenID, int idAnlage, string bem)
        {
            if (_ergebnis == null) return WirtschaftlichkeitCtrl.BASISGRUND_LAUF;
            if (!AnlageImLauf(komponentenID, idAnlage))
                return WirtschaftlichkeitCtrl.BASISGRUND_ANLAGE;

            // Die zwei Endenergie-Arten brauchen zur Menge auch einen PREIS: Weg A den
            // Arbeitspreis jedes beteiligten Trägers, Weg B den Strombezugspreis
            // (§ 4.5). Steht die Menge und fehlt die Zahl trotzdem, fehlt der Preis —
            // und das ist die Lage des Befundes („Arbeitspreis 0,00 bei: Erdgas E").
            if (string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN, StringComparison.Ordinal) ||
                string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF, StringComparison.Ordinal))
            {
                Groesse g = FuerPosition(komponentenID, idAnlage);
                if (g == null) return WirtschaftlichkeitCtrl.BASISGRUND_MENGE;

                // ANWENDERENTSCHEID 19.09.2026: WELCHER Preis fehlt. Weg B bewertet die
                // Menge mit Strom — einer Stromanlage mit eigenem Träger zu DESSEN
                // Arbeitspreis, einer Brennstoffanlage zu dem des Projekt-Stromträgers.
                // Beides heißt „kein Arbeitspreis", verlangt aber einen Handgriff an
                // einem ANDEREN Träger; ein Steuerwert für beide Lagen schickte den
                // Anwender in der Hälfte der Fälle an den falschen Eintrag.
                return string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF,
                                     StringComparison.Ordinal) && !g.EigenerStromtraeger
                    ? WirtschaftlichkeitCtrl.BASISGRUND_STROMPREIS
                    : WirtschaftlichkeitCtrl.BASISGRUND_PREIS;
            }

            // Die reinen Mengenarten (kWh thermisch/elektrisch, Stunden) kennen keinen
            // Preis: Lauf da, Anlage da, also ist die Menge selbst 0.
            return WirtschaftlichkeitCtrl.BASISGRUND_MENGE;
        }

        /// <summary>
        /// Steht diese Anlage im Lauf? <paramref name="idAnlage"/> 0 = Komponenten-
        /// summe; dann zählt, ob die Komponente überhaupt Zeilen im Lauf führt.
        ///
        /// <para>Der Vergleich läuft über den BEZEICHNER, wie in jedem Mengenweg
        /// dieser Klasse — der Stromspeicher ist auch hier die Ausnahme, er führt
        /// seine Anlage als Schlüssel (<see cref="SpeicherEntladungKwh"/>).</para>
        /// </summary>
        private bool AnlageImLauf(int komponentenID, int idAnlage)
        {
            if (_ergebnis == null) return false;

            if (komponentenID == KOMPONENTE_STROMSPEICHER)
            {
                if (_ergebnis.Stromspeicher == null) return false;
                foreach (ErgebnisStromspeicherModel s in _ergebnis.Stromspeicher)
                    if (idAnlage <= 0 || s.ID_Energieanlage == idAnlage) return true;
                return false;
            }

            List<string> namen = Modulnamen(komponentenID);
            if (namen.Count == 0) return false;
            if (idAnlage <= 0) return true;

            string anlagenName;
            if (!_anlagenName.TryGetValue(idAnlage, out anlagenName)) return false;
            foreach (string n in namen)
                if (string.Equals(n ?? "", anlagenName, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>Die Modulbezeichner, die der Lauf für dieses Gewerk führt; leer =
        /// keine Modulliste (Gewerk ohne Laufgröße oder Lauf ohne diese Stufe).</summary>
        private List<string> Modulnamen(int komponentenID)
        {
            var namen = new List<string>();
            if (_ergebnis == null) return namen;

            switch (komponentenID)
            {
                case KOMPONENTE_WAERMEPUMPE:
                    if (_ergebnis.Waermepumpe != null && _ergebnis.Waermepumpe.Module != null)
                        foreach (ErgebnisWaermepumpeModulModel m in _ergebnis.Waermepumpe.Module)
                            namen.Add(m.Modul);
                    break;
                case BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL:
                    if (_ergebnis.Heizkessel != null && _ergebnis.Heizkessel.Module != null)
                        foreach (ErgebnisHeizkesselModulModel m in _ergebnis.Heizkessel.Module)
                            namen.Add(m.Modul);
                    break;
                case KOMPONENTE_PHOTOVOLTAIK:
                    if (_ergebnis.Photovoltaik != null && _ergebnis.Photovoltaik.Module != null)
                        foreach (ErgebnisPhotovoltaikModulModel m in _ergebnis.Photovoltaik.Module)
                            namen.Add(m.Modul);
                    break;
                case KOMPONENTE_SOLARTHERMIE:
                    if (_ergebnis.Solarthermie != null && _ergebnis.Solarthermie.Module != null)
                        foreach (ErgebnisSolarthermieModulModel m in _ergebnis.Solarthermie.Module)
                            namen.Add(m.Modul);
                    break;
                case BetriebskostenCtrl.KOMPONENTE_BHKW:
                    if (_ergebnis.BHKW != null && _ergebnis.BHKW.Module != null)
                        foreach (ErgebnisBHKWModulModel m in _ergebnis.BHKW.Module)
                            namen.Add(m.Modul);
                    break;
            }
            return namen;
        }
    }
}
