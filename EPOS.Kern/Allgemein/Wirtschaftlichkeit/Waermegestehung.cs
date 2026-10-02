using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Wärmegestehungskosten „nur Wärmeerzeuger"</b> (Anwenderentscheid 30.09.2026:
    /// „Die Gestehungskosten sollten nur den Bedarf und die Kosten der den Anlagen zugeordneten
    /// enthalten.") — die Regel an EINER Stelle.
    ///
    /// <para><b>Die Kennzahl.</b> Kosten der Wärmeerzeugung, annuisiert, je kWh Jahreswärmebedarf:
    /// <c>(−Kapitalwert der Wärmeerzeugung × a(i, T)) ÷ (Wärmebedarf × 1.000)</c>. Der Kapitalwert
    /// der Wärmeerzeugung entsteht mit demselben Rechenkern wie der des Projekts
    /// (<see cref="KapitalwertRechner"/>) — nur aus einem Zahlungsgerüst, das allein die
    /// Wärmeerzeugung trägt:</para>
    /// <list type="bullet">
    ///   <item><description><b>Anlagen</b>: Investition nach Zuschuss, Betriebskosten,
    ///     Ersatzbeschaffungen und Restwert der Positionen, die <see cref="PositionZaehlt"/>
    ///     der Wärme zuordnet — Wärmepumpe, Heizkessel, BHKW, Solarthermie, Pufferspeicher und die
    ///     allgemeinen Positionen, die keiner Stromanlage zugeordnet sind.</description></item>
    ///   <item><description><b>Energie</b>: die Energiekosten der Wärmeerzeuger
    ///     (<see cref="Energiekosten"/>) — Brennstoff der Kessel und BHKW samt CO₂-Abgabe, der Strom
    ///     von Wärmepumpe, Heizstab und Elektrokessel zum Arbeitspreis des Stromträgers der Anlage
    ///     (<see cref="WaermestromArbeitEur"/>, Register EZ‑6) ohne Anrechnung von
    ///     PV-Eigenverbrauch, Grund- und Leistungspreis eines Trägers nach
    ///     <see cref="Anteil"/> —, abzüglich der Stromgutschrift für den im Projekt verbrauchten
    ///     BHKW-Strom (<see cref="StromgutschriftEur"/>); bei produzierendem Gewerbe mindert die
    ///     entgangene Entlastung nach § 9b StromStG diese Gutschrift
    ///     (<see cref="Entgangene9bReihe"/>).</description></item>
    ///   <item><description><b>Erlöse</b>: die Erlöse der Wärmeerzeuger — eingespeister BHKW-Strom
    ///     und die Reihen aus <see cref="ErloesReiheZaehlt"/>. Die Stromsteuer-Befreiung des
    ///     BHKW-Eigenstroms zählt nicht: Die Stromgutschrift enthält sie schon.</description></item>
    /// </list>
    /// <para><b>Draußen</b> bleiben Haushaltsstrom und Stromverbraucher, Kältestrom und Kühlung,
    /// Photovoltaik (Investition, Betrieb, Erlöse, Eigenverbrauch) und Stromspeicher. Der
    /// <b>Kapitalwert</b> und alle übrigen Kennzahlen bleiben projektweit — diese Klasse ändert
    /// nur die eine Kennzahl.</para>
    ///
    /// <para><b>Ohne Datenbank und ohne Oberfläche</b>: Die Mengen und Preise liefern
    /// <see cref="KostenEmissionRechner"/> (je Träger, <see cref="EnergieTraegerNachweis"/>) und
    /// die Wirtschaftlichkeit (Positionen, Erlöse); hier steht allein, was davon zählt.</para>
    /// </summary>
    internal static class Waermegestehung
    {
        // ------------------------------------------------------------------
        // Kostenkomponenten (Tab_KostenKomponente.ID, feste Nummern der Auslieferung)
        // ------------------------------------------------------------------
        internal const int KOMPONENTE_WAERMEPUMPE = 1;
        internal const int KOMPONENTE_HEIZKESSEL = 2;
        internal const int KOMPONENTE_PHOTOVOLTAIK = 3;
        internal const int KOMPONENTE_SOLARTHERMIE = 4;
        internal const int KOMPONENTE_STROMSPEICHER = 5;
        internal const int KOMPONENTE_PUFFERSPEICHER = 6;
        internal const int KOMPONENTE_BHKW = 7;
        internal const int KOMPONENTE_WAERMEZENTRALE = 8;
        internal const int KOMPONENTE_BAULICHE_ANLAGEN = 9;
        internal const int KOMPONENTE_STROMEINSPEISUNG = 10;

        // ------------------------------------------------------------------
        // Anlagentypen (Tab_Typ_Energieanlagen.ID)
        // ------------------------------------------------------------------
        internal const int TYP_WAERMEPUMPE = 1;
        internal const int TYP_SOLARTHERMIE = 2;
        internal const int TYP_PHOTOVOLTAIK = 3;
        internal const int TYP_BATTERIESPEICHER = 4;
        internal const int TYP_HEIZKESSEL = 10;
        internal const int TYP_BHKW = 11;
        internal const int TYP_PUFFERSPEICHER = 12;

        /// <summary>
        /// <b>Die Zuordnung einer Kostenposition</b> (Investition, Zuschuss, Betriebskosten):
        /// Zählt sie zur Wärmeerzeugung?
        /// <list type="number">
        ///   <item><description>Photovoltaik und Stromspeicher: nie.</description></item>
        ///   <item><description>Wärmepumpe, Heizkessel, Solarthermie, Pufferspeicher, BHKW:
        ///     immer.</description></item>
        ///   <item><description>Allgemeine Positionen (Wärmezentrale, bauliche Anlagen,
        ///     Stromeinspeisung, Positionen ohne oder mit unbekannter Komponente): Ist die Position
        ///     einer Anlage zugeordnet, entscheidet deren Typ (<see cref="AnlagentypZaehlt"/>);
        ///     sonst zählt sie — außer der Stromeinspeisung, die nur zählt, wenn das Projekt ein
        ///     BHKW führt (dessen Einspeisung gutgeschrieben wird).</description></item>
        /// </list>
        /// </summary>
        /// <param name="komponente"><c>Tab_ProjektWerte.KomponentenID</c>; 0 = keine.</param>
        /// <param name="anlagentyp">Typ der zugeordneten Anlage (<c>Tab_Energieanlagen.ID_Type</c>);
        /// 0 = keine Anlage zugeordnet oder Typ unbekannt.</param>
        /// <param name="bhkwImProjekt">Führt das Projekt ein BHKW?</param>
        internal static bool PositionZaehlt(int komponente, int anlagentyp, bool bhkwImProjekt)
        {
            switch (komponente)
            {
                case KOMPONENTE_PHOTOVOLTAIK:
                case KOMPONENTE_STROMSPEICHER:
                    return false;
                case KOMPONENTE_WAERMEPUMPE:
                case KOMPONENTE_HEIZKESSEL:
                case KOMPONENTE_SOLARTHERMIE:
                case KOMPONENTE_PUFFERSPEICHER:
                case KOMPONENTE_BHKW:
                    return true;
            }
            if (anlagentyp > 0) return AnlagentypZaehlt(anlagentyp);
            if (komponente == KOMPONENTE_STROMEINSPEISUNG) return bhkwImProjekt;
            return true;
        }

        /// <summary>Ist der Anlagentyp ein Wärmeerzeuger oder Wärmespeicher? Photovoltaik und
        /// Batteriespeicher nicht; ein unbekannter Typ zählt wie eine allgemeine Position.</summary>
        internal static bool AnlagentypZaehlt(int anlagentyp)
        {
            return anlagentyp != TYP_PHOTOVOLTAIK && anlagentyp != TYP_BATTERIESPEICHER;
        }

        /// <summary>
        /// <b>Die Erlösreihen der Wärmeerzeuger</b>: KWK-Zuschlag und seine Pauschale und die
        /// Energiesteuer-Entlastung (§ 53/§ 53a/§ 54 EnergieStG — Brennstoff von BHKW und Kessel).
        /// Nicht dabei: die PV-Vergütung; die Stromsteuer-Entlastung nach § 9b StromStG, die am
        /// Netzbezug des ganzen Anschlusses hängt — die Wärmeerzeugung trägt von ihr allein den Teil,
        /// der auf den BHKW-Eigenstrom entgeht (<see cref="Entgangene9bReihe"/>); und die
        /// Stromsteuer-Befreiung des BHKW-Stroms (§ 9 Abs. 1 Nr. 3 StromStG, Reihe
        /// <see cref="KapitalwertRechner.ErloesReihe.STROMSTEUER_BEFREIUNG"/>, nur im Modus ERLOES).
        ///
        /// <para><b>Warum die Befreiung nicht zählt</b> (Anwenderentscheid 02.10.2026, „Befunde wie
        /// Empfehlung umsetzen", Register EZ‑21): Die Stromgutschrift
        /// (<see cref="StromgutschriftEur"/>) schreibt den BHKW-Eigenstrom zum Arbeitspreis des
        /// Netzträgers gut — und dieser Arbeitspreis enthält die Stromsteuer. Der Vorteil, dass auf
        /// den Eigenstrom keine Stromsteuer anfällt, steht damit schon in der Gutschrift; die
        /// Befreiungsreihe zählte ihn ein zweites Mal. Die Stromsteuer zählt deshalb einmal — in der
        /// Gutschrift. Kapitalwert und übrige Kennzahlen buchen die Reihe im Modus ERLOES
        /// unverändert.</para>
        /// </summary>
        internal static bool ErloesReiheZaehlt(string name)
        {
            return string.Equals(name, KapitalwertRechner.ErloesReihe.KWKG, StringComparison.Ordinal) ||
                   string.Equals(name, KapitalwertRechner.ErloesReihe.KWKG_PAUSCHALE, StringComparison.Ordinal) ||
                   string.Equals(name, KapitalwertRechner.ErloesReihe.ENERGIESTEUER, StringComparison.Ordinal);
        }

        /// <summary>
        /// <b>Der Anteil der Wärmeerzeuger an Grund- und Leistungspreis eines Trägers</b> —
        /// nach Jahresmenge: Gehört der Träger nur Wärmeerzeugern, ganz (1); teilen sich
        /// Wärmeerzeuger und andere Verbraucher den Träger (typisch Strom), ihr Mengenanteil.
        /// Ohne Wärmemenge 0; ohne bekannte Gesamtmenge ganz.
        /// </summary>
        internal static double Anteil(double waermeMWh, double gesamtMWh)
        {
            if (!(waermeMWh > 0)) return 0.0;
            if (!(gesamtMWh > waermeMWh)) return 1.0;
            return waermeMWh / gesamtMWh;
        }

        /// <summary>
        /// <b>Die Energiekosten der Wärmeerzeuger [€/a]</b> aus der Aufstellung je Träger:
        /// je Träger die Arbeitskosten der Wärmemenge plus Grund- und Leistungspreis nach
        /// <see cref="Anteil"/>. Die CO₂-Abgabe steht nicht hier (sie ist eine eigene Reihe und
        /// hängt ganz am Brennstoff der Wärmeerzeuger). <c>null</c> ohne Aufstellung.
        /// </summary>
        internal static double? Energiekosten(IEnumerable<EnergieTraegerNachweis> traeger)
        {
            if (traeger == null) return null;
            double summe = 0.0;
            bool irgendeiner = false;
            foreach (EnergieTraegerNachweis t in traeger)
            {
                if (t == null) continue;
                irgendeiner = true;
                double fix = t.GrundpreisEur + t.LeistungEur;
                summe += t.WaermeArbeitEur;
                if (fix != 0.0) summe += fix * Anteil(t.WaermeMengeMWh, t.VerbrauchGesamtMWh);
            }
            return irgendeiner ? (double?)summe : null;
        }

        /// <summary>
        /// Der Strom der Wärmeerzeuger [MWh/a]: Wärmepumpe, Heizstab und Elektrokessel — die
        /// Mengen, die der Lauf je Erzeuger getrennt vom Grundstrombedarf führt
        /// (<see cref="ErgebnisWaermepumpeModel.Stromverbrauch_WP"/>,
        /// <see cref="ErgebnisWaermepumpeModel.Stromverbrauch_Heizstab"/>,
        /// <see cref="ErgebnisHeizkesselModel.Stromverbrauch"/>). Der Kältestrom der Wärmepumpe
        /// gehört nicht dazu.
        /// </summary>
        internal static double WaermestromMWh(ErgebnisModel m)
        {
            if (m == null) return 0.0;
            double s = 0.0;
            if (m.Waermepumpe != null)
                s += Math.Max(0.0, m.Waermepumpe.Stromverbrauch_WP) +
                     Math.Max(0.0, m.Waermepumpe.Stromverbrauch_Heizstab);
            if (m.Heizkessel != null) s += Math.Max(0.0, m.Heizkessel.Stromverbrauch);
            return s;
        }

        /// <summary>
        /// Der Strom einer Erzeugerzeile, deren Anlage einen <b>eigenen Stromträger</b> führt —
        /// Menge und Arbeitspreis dieses Trägers (<see cref="WaermestromArbeitEur"/>).
        /// </summary>
        internal sealed class EigenerStrom
        {
            /// <summary>Der Modulname der Erzeugerzeile (= Bezeichner der Anlage).</summary>
            public string Modul = "";
            /// <summary>Der eigene Stromträger der Anlage (<c>energy_carrier.id</c>).</summary>
            public int CarrierId;
            /// <summary>Der Wärmestrom dieser Zeile [MWh/a].</summary>
            public double MengeMWh;
            /// <summary>Der Arbeitspreis des eigenen Trägers [€/kWh]; <c>null</c> = nicht gepflegt.</summary>
            public double? PreisJeKwh;
        }

        /// <summary>
        /// Der Strom der Wärmeerzeuger <b>je Erzeugerzeile</b> [MWh/a], in der Reihenfolge des
        /// Laufs: je Wärmepumpen-Modulzeile Strom und Heizstab
        /// (<see cref="ErgebnisWaermepumpeModulModel.Stromverbrauch"/> +
        /// <see cref="ErgebnisWaermepumpeModulModel.Heizstab"/>, ohne Kältestrom), je Kessel-Modulzeile
        /// eines Elektrokessels der Stromeinsatz
        /// (<see cref="SimulationSPK.StromeinsatzElektrokesselMwh"/>). Der Schlüssel ist der Modulname
        /// (getrimmt) — derselbe wie der Bezeichner der Anlage. Die Summe ist der Sache nach
        /// <see cref="WaermestromMWh"/>; gerechnet wird mit ihr nur der Teil, der einen eigenen Träger
        /// trägt.
        /// </summary>
        /// <param name="elektrokessel">Die Modulnamen der Elektrokessel des Projekts (getrimmt).</param>
        internal static List<KeyValuePair<string, double>> WaermestromJeModul(ErgebnisModel m,
                                                                             ICollection<string> elektrokessel)
        {
            var liste = new List<KeyValuePair<string, double>>();
            if (m == null) return liste;
            if (m.Waermepumpe != null && m.Waermepumpe.Module != null)
                foreach (ErgebnisWaermepumpeModulModel mo in m.Waermepumpe.Module)
                {
                    if (mo == null) continue;
                    double mwh = Math.Max(0.0, mo.Stromverbrauch) + Math.Max(0.0, mo.Heizstab);
                    if (mwh > 0) liste.Add(new KeyValuePair<string, double>((mo.Modul ?? "").Trim(), mwh));
                }
            if (m.Heizkessel != null && m.Heizkessel.Module != null && elektrokessel != null &&
                elektrokessel.Count > 0)
                foreach (ErgebnisHeizkesselModulModel mo in m.Heizkessel.Module)
                {
                    if (mo == null) continue;
                    string name = (mo.Modul ?? "").Trim();
                    if (!elektrokessel.Contains(name)) continue;
                    double mwh = Math.Max(0.0, SimulationSPK.StromeinsatzElektrokesselMwh(mo.Waerme_Gas, mo.Waerme_Oel));
                    if (mwh > 0) liste.Add(new KeyValuePair<string, double>(name, mwh));
                }
            return liste;
        }

        /// <summary>
        /// <b>Die Arbeitskosten des Wärmestroms [€/a]</b> — EZ‑6 gilt auch hier (Register EZ‑6 und
        /// EZ‑21, P646): „Der Strompreis einer Anlage ist der ihres eigenen Trägers." Der Strom
        /// einer Wärmepumpe, eines Heizstabs oder eines Elektrokessels, dessen Anlage einen eigenen
        /// Stromträger führt (<see cref="ProjektEnergietraegerCtrl.EigeneStromTraeger"/>), zählt zum
        /// Arbeitspreis dieses Trägers; der übrige Wärmestrom zum Arbeitspreis des Trägers, der den
        /// Netzbezug bepreist (zugeordnet oder Rückfallträger). Dieselbe Wahl wie der
        /// Endenergie-Auflöser der Betriebskosten (<c>EndenergieAufloeser.Strompreis</c>).
        ///
        /// <para><c>Σ min(Menge_eigen, Rest) × Preis_eigen + Rest × Preis_Netz</c>; der Rest beginnt
        /// bei <paramref name="waermestromMWh"/> — die Summe der Mengen übersteigt nie den
        /// Wärmestrom des Laufs. Ein eigener Träger ohne Arbeitspreis rechnet seine Menge zum
        /// Netzpreis (benannte Grenze: Der Netzbezug wird einmal mit dem Netzträger bezahlt, ein
        /// Preis aus dem Nichts entsteht nicht). Ohne eigene Träger ist das Ergebnis Zeichen für
        /// Zeichen <c>Wärmestrom × 1.000 × Preis_Netz</c>.</para>
        ///
        /// <para>Die Stromgutschrift des BHKW-Eigenstroms bleibt beim Netzträger
        /// (<see cref="StromgutschriftEur"/>, Entscheid „Arbeitspreis bleibt").</para>
        /// </summary>
        internal static double WaermestromArbeitEur(double waermestromMWh, double netzpreisJeKwh,
                                                    IEnumerable<EigenerStrom> eigene)
        {
            double rest = Math.Max(0.0, waermestromMWh);
            double eur = 0.0;
            bool abweichend = false;
            if (eigene != null)
                foreach (EigenerStrom e in eigene)
                {
                    if (e == null || !(e.MengeMWh > 0) || !e.PreisJeKwh.HasValue || !(rest > 0)) continue;
                    double menge = Math.Min(e.MengeMWh, rest);
                    eur += menge * 1000.0 * e.PreisJeKwh.Value;
                    rest -= menge;
                    abweichend = true;
                }
            if (!abweichend) return waermestromMWh * 1000.0 * netzpreisJeKwh;
            return eur + rest * 1000.0 * netzpreisJeKwh;
        }

        /// <summary>
        /// Der Stromverbrauch ALLER Verbraucher des Anschlusses [MWh/a] — die Bezugsgröße des
        /// Anteils am Grund- und Leistungspreis des Stromträgers: Grundstrombedarf (Stromverbraucher,
        /// Stromganglinien), Strom der Wärmeerzeuger und Kältestrom.
        /// </summary>
        internal static double StromverbrauchGesamtMWh(ErgebnisModel m)
        {
            if (m == null) return 0.0;
            double s = WaermestromMWh(m);
            if (m.Energiebedarf != null) s += Math.Max(0.0, m.Energiebedarf.Strombedarf_Gesamt);
            if (m.Waermepumpe != null && m.Waermepumpe.Stromverbrauch_Kuehlung.HasValue)
                s += Math.Max(0.0, m.Waermepumpe.Stromverbrauch_Kuehlung.Value);
            return s;
        }

        /// <summary>
        /// Der im Projekt verbrauchte BHKW-Strom [MWh/a]: aus der Strommatrix des Laufs
        /// (<see cref="StromMatrix.KwkEigenGesamtMWh"/>, stundenweise min-Regel), ohne Stundenreihen
        /// der Teil des Strombedarfs an der BHKW-Stufe, den das BHKW gedeckt hat (Strombedarf −
        /// Reststrombedarf, höchstens die Erzeugung). 0 ohne BHKW.
        /// </summary>
        internal static double BhkwEigenstromMWh(StromMatrix matrix, ErgebnisBHKWModel bhkw)
        {
            if (bhkw == null || !(bhkw.Stromproduktion > 0)) return 0.0;
            if (matrix != null && !matrix.StrombedarfFehlt)
                return Math.Max(0.0, matrix.KwkEigenGesamtMWh);
            double gedeckt = Math.Max(0.0, bhkw.Strombedarf - bhkw.Reststrombedarf);
            return Math.Min(bhkw.Stromproduktion, gedeckt);
        }

        /// <summary>
        /// <b>Die Stromgutschrift [€/a]</b>: der im Projekt verbrauchte BHKW-Strom als vermiedener
        /// Bezug zum Arbeitspreis des Stromträgers — damit der Wärmepreis eines BHKW nicht um seinen
        /// Eigenstrom verzerrt wird. 0 ohne Menge oder ohne Arbeitspreis.
        /// </summary>
        internal static double StromgutschriftEur(double eigenstromMWh, double? arbeitspreisJeKwh)
        {
            if (!(eigenstromMWh > 0) || !arbeitspreisJeKwh.HasValue) return 0.0;
            return eigenstromMWh * 1000.0 * arbeitspreisJeKwh.Value;
        }

        /// <summary>
        /// <b>Die entgangene Entlastung nach § 9b StromStG [€/a]</b> (Anwenderentscheid 02.10.2026:
        /// „Die Stromsteuer-Entlastung nach § 9b StromStG mindert die Stromsteuer-Entlastung durch
        /// das BHKW. Nur die zusätzliche Entlastung durch das BHKW wird angerechnet."). Der
        /// Arbeitspreis der Stromgutschrift enthält die Stromsteuer; auf den Netzbezug, den der
        /// BHKW-Eigenstrom ersetzt, bekäme ein Unternehmen des produzierenden Gewerbes (oder der
        /// Land- und Forstwirtschaft) die Entlastung nach § 9b ohnehin. Der Vorteil des Eigenstroms
        /// ist deshalb nur Arbeitspreis − Entlastungssatz je MWh.
        ///
        /// <para><b>Dieselben Regeln wie die Entlastung des Projekts</b> (Anwenderentscheid
        /// 02.10.2026, Register EZ‑22): abgezogen wird die Differenz der Entlastung ohne und mit
        /// BHKW-Eigenstrom, <c>max(0, (N + E) × s − S) − max(0, N × s − S)</c> — der Sockelbetrag S
        /// mindert sie nur, soweit der Netzbezug N ihn nicht schon trägt. Gerechnet von
        /// <see cref="SteuerGutschriftRechner.Entgangene9bEur"/>, derselben Funktion wie die
        /// § 9b-Korrektur des Ausweises der vermiedenen Stromkosten. Ohne Sockel (oder wenn N ihn
        /// trägt) ist das Ergebnis Zeichen für Zeichen Eigenstrom × Satz. 0 ohne Menge oder ohne
        /// Satz &gt; 0.</para>
        ///
        /// <para><b>Gedeckelt</b> (§ 6.3 Nr. 41): Der Satz zählt höchstens mit dem
        /// Stromsteueranteil des Arbeitspreises, mit dem die Stromgutschrift rechnet
        /// (<see cref="SteuerGutschriftRechner.Satz9bWirksam"/>) — der Abzug übersteigt nie die
        /// Stromsteuer, die die Gutschrift enthält.</para>
        /// </summary>
        /// <param name="eigenstromMWh">Der im Projekt verbrauchte BHKW-Strom [MWh/a]
        /// (<see cref="BhkwEigenstromMWh"/>).</param>
        /// <param name="satzEurJeMWh">Der Entlastungssatz [€/MWh]; <c>null</c> = nicht gepflegt.</param>
        /// <param name="netzbezugMWh">Der Netzbezug des Projekts nach dem Lauf [MWh/a] — dieselbe
        /// Menge, mit der die Entlastung des Projekts rechnet
        /// (<see cref="SteuerEingabe.NetzbezugMWh"/>).</param>
        /// <param name="sockelEur">Der Sockelbetrag des Jahres [€/a]; <c>null</c> = nicht gepflegt.</param>
        /// <param name="deckelEurJeMWh">Die Obergrenze des Satzes [€/MWh] — der Stromsteueranteil des
        /// Arbeitspreises der Gutschrift, ohne gepflegten Anteil der Regelsatz; <c>null</c> = keine.</param>
        internal static double Entgangene9bEntlastungEur(double eigenstromMWh, double? satzEurJeMWh,
                                                        double netzbezugMWh = 0.0, double? sockelEur = null,
                                                        double? deckelEurJeMWh = null)
        {
            return SteuerGutschriftRechner.Entgangene9bEur(netzbezugMWh, eigenstromMWh, satzEurJeMWh, sockelEur,
                                                          deckelEurJeMWh);
        }

        /// <summary>
        /// <b>Die entgangene § 9b-Entlastung jahresscharf</b>: die NEGATIVE Erlösreihe
        /// <see cref="KapitalwertRechner.ErloesReihe.STROMSTEUER_ENTLASTUNG_ENTGANGEN"/> des
        /// Zahlungsgerüsts der Wärmeerzeugung — im Jahr t = 1…T −<see cref="Entgangene9bEntlastungEur"/>
        /// mit Satz, Sockelbetrag und Deckel des Kalenderjahres <c>Förderbeginn + t − 1</c>, wie die
        /// Steuerreihen des Projekts. Als eigene Reihe bleibt der Abzug jahresscharf und nominal; im
        /// Energiebetrag würde er mit der Energiepreissteigerung fortgeschrieben, die für einen
        /// gesetzlichen Satz nicht gilt.
        ///
        /// <para><c>null</c> — dann rechnet die Kennzahl bitgleich ohne Abzug —, wenn das Projekt
        /// weder produzierendes Gewerbe noch Land- und Forstwirtschaft ist
        /// (<see cref="SteuerGutschriftRechner.ProduzierendesGewerbe"/>, dieselbe Prüfung wie die
        /// § 9b-Korrektur des Ausweises), wenn keine Stromgutschrift gerechnet ist (ohne Gutschrift
        /// gibt es nichts zu mindern) oder wenn kein Jahr einen Abzug &gt; 0 führt.</para>
        /// </summary>
        /// <param name="steuer">Die Steuereingabe des Laufs (Unternehmensart, Netzbezug); <c>null</c>
        /// = kein Steuerpfad.</param>
        /// <param name="eigenstromMWh">Der im Projekt verbrauchte BHKW-Strom [MWh/a] — dieselbe
        /// Menge, mit der die Stromgutschrift rechnet.</param>
        /// <param name="stromgutschriftEur">Die Stromgutschrift des ersten Jahres [€/a]
        /// (<see cref="StromgutschriftEur"/>).</param>
        /// <param name="jahre">Der Betrachtungszeitraum T [a].</param>
        /// <param name="foerderbeginn">Das Kalenderjahr des ersten Betrachtungsjahres.</param>
        /// <param name="satzImJahr">Der Entlastungssatz eines Kalenderjahres [€/MWh] aus dem
        /// Gesetzeskatalog; <c>null</c> = nicht gepflegt. Gefragt wird nur, wenn die Reihe greifen
        /// kann.</param>
        /// <param name="sockelImJahr">Der Sockelbetrag eines Kalenderjahres [€/a] aus dem
        /// Gesetzeskatalog; <c>null</c> (die Funktion oder ihr Wert) = keiner. Der Netzbezug, gegen
        /// den er wirkt, ist <see cref="SteuerEingabe.NetzbezugMWh"/> der Steuereingabe.</param>
        /// <param name="deckelImJahr">Die Obergrenze des Satzes eines Kalenderjahres [€/MWh] — der
        /// Stromsteueranteil des Netzträgers, ohne gepflegten Anteil der Regelsatz des Jahres;
        /// <c>null</c> (die Funktion oder ihr Wert) = keine.</param>
        internal static KapitalwertRechner.ErloesReihe Entgangene9bReihe(
            SteuerEingabe steuer, double eigenstromMWh, double stromgutschriftEur,
            int jahre, int foerderbeginn, Func<int, double?> satzImJahr,
            Func<int, double?> sockelImJahr = null, Func<int, double?> deckelImJahr = null)
        {
            if (steuer == null || !SteuerGutschriftRechner.ProduzierendesGewerbe(steuer)) return null;
            if (!(eigenstromMWh > 0) || !(stromgutschriftEur > 0) || satzImJahr == null) return null;

            int T = Math.Max(1, jahre);
            var jeJahr = new double[T + 1];
            bool etwas = false;
            for (int t = 1; t <= T; t++)
            {
                int jahr = foerderbeginn + t - 1;
                double abzug = Entgangene9bEntlastungEur(eigenstromMWh, satzImJahr(jahr), steuer.NetzbezugMWh,
                                                         sockelImJahr != null ? sockelImJahr(jahr) : null,
                                                         deckelImJahr != null ? deckelImJahr(jahr) : null);
                if (abzug == 0.0) continue;
                jeJahr[t] = -abzug;
                etwas = true;
            }
            return etwas
                ? new KapitalwertRechner.ErloesReihe(
                      KapitalwertRechner.ErloesReihe.STROMSTEUER_ENTLASTUNG_ENTGANGEN, jeJahr)
                : null;
        }

        /// <summary>
        /// <b>Der Nachweis des § 9b-Abzugs</b> (Register EZ‑22): eine Hinweiszeile je Regel, die im
        /// ersten Jahr wirkt — der Deckel (gepflegter Stromsteueranteil unter dem Satz,
        /// <c>WIRT_GESTEHUNG_9B_DECKEL</c>), die Obergrenze ohne gepflegten Anteil (Regelsatz der
        /// Stromsteuer, <c>WIRT_GESTEHUNG_9B_REGELSATZ</c> — benannt, auch wenn sie nicht greift)
        /// und der Sockelbetrag, den der Netzbezug nicht ganz trägt (<c>WIRT_GESTEHUNG_9B_SOCKEL</c>:
        /// Abzug statt Eigenstrom × wirksamem Satz). Die Zahlen rechnet dieselbe Funktion wie die
        /// Reihe (<see cref="SteuerGutschriftRechner.Entgangene9bEur"/>). Mehrere Zeilen mit
        /// „ | " verkettet wie die übrigen Laufhinweise; <c>null</c>, wenn keine Regel zu nennen ist
        /// oder kein Abzug greifen kann (ohne Eigenstrom, ohne Satz &gt; 0).
        /// </summary>
        /// <param name="eigenstromMWh">Der BHKW-Eigenstrom der Gutschrift [MWh/a].</param>
        /// <param name="netzbezugMWh">Der Netzbezug des Projekts [MWh/a] (§ 3.8).</param>
        /// <param name="satzEurJeMWh">Der § 9b-Satz des ersten Jahres [€/MWh].</param>
        /// <param name="sockelEur">Der Sockelbetrag des ersten Jahres [€/a]; <c>null</c> = keiner.</param>
        /// <param name="anteilEurJeMWh">Der gepflegte Stromsteueranteil des Netzträgers [€/MWh];
        /// <c>null</c> = nicht gepflegt.</param>
        /// <param name="regelsatzEurJeMWh">Der Regelsatz der Stromsteuer des ersten Jahres [€/MWh] —
        /// die Obergrenze ohne gepflegten Anteil; <c>null</c> = nicht gepflegt.</param>
        internal static string Nachweis9b(double eigenstromMWh, double netzbezugMWh, double? satzEurJeMWh,
                                          double? sockelEur, double? anteilEurJeMWh, double? regelsatzEurJeMWh,
                                          System.Globalization.CultureInfo kultur)
        {
            if (!(eigenstromMWh > 0) || !satzEurJeMWh.HasValue || !(satzEurJeMWh.Value > 0)) return null;
            var teile = new List<string>();
            double satz = satzEurJeMWh.Value;
            double? deckel = anteilEurJeMWh ?? regelsatzEurJeMWh;
            double wirksam = SteuerGutschriftRechner.Satz9bWirksam(satz, deckel).Value;

            if (anteilEurJeMWh.HasValue)
            {
                if (wirksam < satz)
                    teile.Add(string.Format(kultur, MyResource.Resource.WIRT_GESTEHUNG_9B_DECKEL,
                        wirksam.ToString("N2", kultur), satz.ToString("N2", kultur)));
            }
            else if (regelsatzEurJeMWh.HasValue)
                teile.Add(string.Format(kultur, MyResource.Resource.WIRT_GESTEHUNG_9B_REGELSATZ,
                    regelsatzEurJeMWh.Value.ToString("N2", kultur)));

            double abzug = SteuerGutschriftRechner.Entgangene9bEur(netzbezugMWh, eigenstromMWh, satz, sockelEur, deckel);
            double ohneSockel = wirksam > 0 ? eigenstromMWh * wirksam : 0.0;
            if (abzug < ohneSockel)
                teile.Add(string.Format(kultur, MyResource.Resource.WIRT_GESTEHUNG_9B_SOCKEL,
                    abzug.ToString("N2", kultur), ohneSockel.ToString("N2", kultur),
                    Math.Max(0.0, netzbezugMWh).ToString("N1", kultur), (sockelEur ?? 0.0).ToString("N2", kultur)));

            return teile.Count == 0 ? null : string.Join(" | ", teile);
        }

        /// <summary>
        /// <b>Die Kennzahl [€/kWh]</b>: <c>(−Kapitalwert × a(i, T)) ÷ (Wärmebedarf × 1.000)</c>;
        /// <c>null</c> ohne Wärmebedarf.
        /// </summary>
        internal static double? Kennzahl(double kapitalwert, double zinsProzent, int jahre, double waermeMWh)
        {
            if (!(waermeMWh > 0)) return null;
            double a = KapitalwertRechner.Annuitaet(zinsProzent / 100.0, jahre);
            return (-kapitalwert * a) / (waermeMWh * 1000.0);
        }

        /// <summary>
        /// Die <b>Zerlegung des Zählers</b> [€/a, annuisiert]: Anlagen (Investition, Betrieb,
        /// Ersatz, Restwert), Energie (samt CO₂-Abgabe, abzüglich der Stromgutschrift, zuzüglich
        /// der entgangenen § 9b-Entlastung, die diese Gutschrift mindert) und Erlöse — Kosten
        /// positiv, Erlöse positiv als Abzug. Reine Auskunft aus dem Zahlungsbild der
        /// Wärmeerzeugung; die Kennzahl rechnet mit dem Kapitalwert.
        /// </summary>
        internal sealed class Zerlegung
        {
            /// <summary>Annuität der Anlagen [€/a] (Investition nach Zuschuss, Betrieb, Ersatz, Restwert).</summary>
            public double AnlagenEurJahr;
            /// <summary>Annuität der Energiekosten der Wärmeerzeuger [€/a], CO₂-Abgabe eingeschlossen,
            /// Stromgutschrift abgezogen, entgangene § 9b-Entlastung zugeschlagen.</summary>
            public double EnergieEurJahr;
            /// <summary>Darin: die Stromgutschrift des ersten Jahres [€/a] (nicht annuisiert).</summary>
            public double StromgutschriftJahr1;
            /// <summary>Darin: die entgangene § 9b-Entlastung des ersten Jahres [€/a] (nicht
            /// annuisiert), positiv — um sie ist die Stromgutschrift gemindert; 0 = kein Abzug.</summary>
            public double Entgangene9bJahr1;
            /// <summary>Annuität der Erlöse der Wärmeerzeuger [€/a].</summary>
            public double ErloeseEurJahr;
            /// <summary>Der Jahreswärmebedarf [kWh/a].</summary>
            public double WaermeKwh;

            /// <summary>Zähler [€/a] = Anlagen + Energie − Erlöse.</summary>
            public double ZaehlerEurJahr { get { return AnlagenEurJahr + EnergieEurJahr - ErloeseEurJahr; } }

            /// <summary>
            /// Aus der Gliederung des Zahlungsbilds der Wärmeerzeugung; <c>null</c> ohne sie. Die
            /// entgangene § 9b-Entlastung reist im Kapitalwert als negative Erlösreihe (jahresscharf)
            /// und steht deshalb in der Gliederung unter den Erlösen; der Sache nach mindert sie die
            /// Stromgutschrift — die Zerlegung führt ihren Barwert unter Energie. Der Zähler bleibt
            /// dabei derselbe.
            /// </summary>
            /// <param name="entgangen9b">Die Reihe aus <see cref="Entgangene9bReihe"/>; <c>null</c> = keine.</param>
            internal static Zerlegung Aus(Zahlungsgliederung g, double waermeMWh, double stromgutschriftJahr1,
                                          KapitalwertRechner.ErloesReihe entgangen9b = null)
            {
                if (g == null || !(waermeMWh > 0)) return null;
                double a = KapitalwertRechner.Annuitaet(g.ZinsProzent / 100.0, g.Jahre);
                double anlagen = 0, energie = 0, erloese = 0;
                foreach (Zahlungsbestandteil b in g.Bestandteile)
                {
                    if (b == null) continue;
                    if (string.Equals(b.Schluessel, Zahlungsgliederung.ENERGIE, StringComparison.Ordinal))
                        energie -= b.Barwert;
                    else if (string.Equals(b.Schluessel, Zahlungsgliederung.ERLOESE, StringComparison.Ordinal))
                        erloese += b.Barwert;
                    else anlagen -= b.Barwert;
                }
                if (entgangen9b != null)
                {
                    // Barwert der (negativen) Reihe — aus den Erlösen heraus, als Kosten zur Energie.
                    double barwert9b = 0;
                    for (int t = 1; t <= g.Jahre; t++)
                    {
                        double w = entgangen9b.Wert(t);
                        if (w != 0) barwert9b += w * Math.Pow(1.0 + g.ZinsProzent / 100.0, -t);
                    }
                    erloese -= barwert9b;
                    energie -= barwert9b;
                }
                return new Zerlegung
                {
                    AnlagenEurJahr = anlagen * a,
                    EnergieEurJahr = energie * a,
                    ErloeseEurJahr = erloese * a,
                    StromgutschriftJahr1 = stromgutschriftJahr1,
                    Entgangene9bJahr1 = entgangen9b != null ? -entgangen9b.Jahr1 : 0.0,
                    WaermeKwh = waermeMWh * 1000.0
                };
            }
        }
    }
}
