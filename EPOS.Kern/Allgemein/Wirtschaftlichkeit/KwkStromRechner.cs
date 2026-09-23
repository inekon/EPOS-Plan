using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE E7c — die <b>Stromkennzahl σ</b> EINER Anlage samt Herkunft (§ 2 Nr. 16
    /// KWKG, Befund K‑1, Entscheid E7‑Q2 (2)).
    ///
    /// <para><b>Drei Herkünfte, keine vierte.</b> <see cref="KwkStromRechner.HERKUNFT_GEPFLEGT"/>
    /// — die Geräteeigenschaft steht an der Anlage
    /// (<c>Tab_Energieanlagen.KWKG_Stromkennzahl</c>); <see cref="KwkStromRechner.HERKUNFT_BERECHNET"/>
    /// — sie ist leer, und σ = P_el ÷ P_th der Gerätezeile (<c>Tab_BHKW</c>);
    /// <see cref="KwkStromRechner.HERKUNFT_FEHLT"/> — beides geht nicht. Dann gibt es
    /// <b>keinen Ersatzwert und keine Vorgabe</b> (Auflage des Anwenders zu E7‑Q2 (2)):
    /// <see cref="Wert"/> bleibt <c>null</c>, und die Anlage bekommt nach Fall 2 keinen
    /// KWK-Strom.</para>
    /// </summary>
    public sealed class KwkStromkennzahl
    {
        /// <summary>Die geltende Kennzahl; <c>null</c> = nicht bestimmbar.</summary>
        public double? Wert;

        /// <summary>Steuerwert <c>KwkStromRechner.HERKUNFT_*</c> (ASCII, sprachneutral).</summary>
        public string Herkunft = KwkStromRechner.HERKUNFT_FEHLT;

        /// <summary>Der Vorschlag P_el ÷ P_th der Gerätezeile — auch dann, wenn eine
        /// gepflegte Kennzahl gilt (der Dialog zeigt beide); <c>null</c> = nicht
        /// bestimmbar.</summary>
        public double? VorschlagWert;

        /// <summary>Die Herkunft im Klartext, z. B. „berechnet aus P_el ÷ P_th der
        /// Gerätezeile = 50,0 kW ÷ 81,0 kW".</summary>
        public string Herleitung = "";

        /// <summary>true, wenn eine Kennzahl gilt.</summary>
        public bool Bestimmbar { get { return Wert.HasValue; } }
    }

    /// <summary>
    /// ETAPPE E7c — die Mengen des KWK-Stroms EINER Anlage nach dem zweiten Fall des
    /// § 2 Nr. 16 KWKG. Reine Ausgabe von <see cref="KwkStromRechner.Fall2"/>; alle
    /// Mengen in MWh/a.
    /// </summary>
    public sealed class KwkStromFall2
    {
        /// <summary>Nettostromerzeugung (Fall 1) — Klemme minus Hilfsstrom.</summary>
        public double NettoMWh;

        /// <summary>Wärmeproduktion des Moduls bzw. der Anlage.</summary>
        public double WaermeMWh;

        /// <summary>Anteil am Wärmeüberschuss des Projekts — nur nach P_el verteilt
        /// (Entscheid E7‑Q2 (1)).</summary>
        public double UeberschussAnteilMWh;

        /// <summary>Nutzwärme = Wärmeproduktion − Anteil am Wärmeüberschuss, bei 0
        /// geklemmt.</summary>
        public double NutzwaermeMWh;

        /// <summary>Die Stromkennzahl samt Herkunft.</summary>
        public KwkStromkennzahl Stromkennzahl = new KwkStromkennzahl();

        /// <summary>KWK-Strom = min(Netto, Nutzwärme × σ); 0, wenn σ nicht bestimmbar
        /// ist.</summary>
        public double KwkStromMWh;

        /// <summary>Kürzung = Netto − KWK-Strom, bei 0 geklemmt. Sie geht zuerst von der
        /// Einspeisemenge ab (<see cref="KwkStromRechner.Kuerzen"/>).</summary>
        public double KuerzungMWh;
    }

    /// <summary>
    /// ETAPPE E7c — <b>der zweite Fall des § 2 Nr. 16 KWKG</b> (Befund K‑1, Entscheide
    /// EZ‑5 und E7‑Q2 vom 23.09.2026): Verfügt eine Anlage über eine Vorrichtung zur
    /// Abwärmeabfuhr, ist KWK-Strom nicht die Nettostromerzeugung, sondern
    /// <c>min(Nettostromerzeugung, Nutzwärme × Stromkennzahl)</c>.
    ///
    /// <para><b>Reine Funktionen ohne Datenbankzugriff</b> (Leitentscheidung L9) — derselbe
    /// Rechner für die Mengenbildung je Anlage (<c>WirtschaftlichkeitCtrl.ReiheJeAnlage</c>),
    /// den Ersatzweg und den Vorschlag im BHKW-Dialog.</para>
    ///
    /// <para><b>Die fünf Teilentscheide E7‑Q2:</b> (1) die Wärme bleibt je Modul, nur der
    /// Wärmeüberschuss des Projekts wird nach P_el auf die Module verteilt
    /// (<see cref="UeberschussAnteil"/>); (2) σ ist die gepflegte Geräteeigenschaft oder,
    /// wenn leer, P_el ÷ P_th der Gerätezeile — sonst keine (<see cref="Stromkennzahl"/>);
    /// (3) die Kürzung geht zuerst von der Einspeisung ab (<see cref="Kuerzen"/>); (4) der
    /// Ersatzweg verteilt die Nutzwärme des Projekts nach P_el; (5) die Felder stehen in
    /// der Überlagerung „Sätze und Herkunft" des BHKW-Dialogs.</para>
    /// </summary>
    public static class KwkStromRechner
    {
        /// <summary>σ steht gepflegt an der Anlage.</summary>
        public const string HERKUNFT_GEPFLEGT = "GEPFLEGT";

        /// <summary>σ ist aus P_el ÷ P_th der Gerätezeile berechnet.</summary>
        public const string HERKUNFT_BERECHNET = "BERECHNET";

        /// <summary>σ ist nicht bestimmbar — kein Ersatzwert, keine Vorgabe.</summary>
        public const string HERKUNFT_FEHLT = "FEHLT";

        /// <summary>Zahlenformat der Kennzahl in Herleitung und Dialog: drei
        /// Nachkommastellen (Mockup: „0,845").</summary>
        public const string FORMAT_KENNZAHL = "N3";

        /// <summary>
        /// Der Vorschlag <c>σ = P_el ÷ P_th</c> der Gerätezeile; <c>null</c>, sobald eine
        /// der beiden Leistungen fehlt oder nicht positiv ist — eine Kennzahl 0 oder eine
        /// Division durch 0 ist keine Stromkennzahl.
        /// </summary>
        public static double? Vorschlag(double? pelKW, double? pthKW)
        {
            if (!pelKW.HasValue || !pthKW.HasValue) return null;
            if (pelKW.Value <= 0 || pthKW.Value <= 0) return null;
            return pelKW.Value / pthKW.Value;
        }

        /// <summary>
        /// Die geltende Stromkennzahl (Entscheid E7‑Q2 (2) mit Auflage): gepflegt, sonst
        /// berechnet, sonst keine. Eine gepflegte Zahl ≤ 0 gilt als nicht gepflegt — 0
        /// ist im Dialog „kein eigener Wert".
        /// </summary>
        public static KwkStromkennzahl Stromkennzahl(double? gepflegt, double? pelKW, double? pthKW,
                                                    CultureInfo kultur)
        {
            if (kultur == null) kultur = CultureInfo.CurrentCulture;
            var k = new KwkStromkennzahl { VorschlagWert = Vorschlag(pelKW, pthKW) };

            if (gepflegt.HasValue && gepflegt.Value > 0)
            {
                k.Wert = gepflegt.Value;
                k.Herkunft = HERKUNFT_GEPFLEGT;
                k.Herleitung = T("WIRT_KWKG_SIGMA_GEPFLEGT", "gepflegt");
                return k;
            }

            if (k.VorschlagWert.HasValue)
            {
                k.Wert = k.VorschlagWert.Value;
                k.Herkunft = HERKUNFT_BERECHNET;
                k.Herleitung = string.Format(kultur,
                    T("WIRT_KWKG_SIGMA_BERECHNET",
                      "berechnet aus P_el ÷ P_th der Gerätezeile = {0} kW ÷ {1} kW"),
                    pelKW.Value.ToString("N1", kultur), pthKW.Value.ToString("N1", kultur));
                return k;
            }

            k.Herkunft = HERKUNFT_FEHLT;
            k.Herleitung = T("WIRT_KWKG_SIGMA_FEHLT",
                             "weder gepflegt noch aus P_el ÷ P_th der Gerätezeile bestimmbar");
            return k;
        }

        /// <summary>
        /// Der Anteil einer Anlage am Wärmeüberschuss des Projekts — <b>nur nach
        /// P_el</b> (Entscheid E7‑Q2 (1)). Ohne Überschuss oder ohne Leistungssumme 0.
        /// </summary>
        public static double UeberschussAnteil(double ueberschussMWh, double pelKW, double pelSummeKW)
        {
            if (ueberschussMWh <= 0 || pelSummeKW <= 0 || pelKW <= 0) return 0;
            return ueberschussMWh * pelKW / pelSummeKW;
        }

        /// <summary>Nutzwärme = Wärmeproduktion − Anteil am Wärmeüberschuss, bei 0
        /// geklemmt — eine negative Wärme gibt es nicht.</summary>
        public static double Nutzwaerme(double waermeMWh, double ueberschussAnteilMWh)
        {
            return Math.Max(0, waermeMWh - Math.Max(0, ueberschussAnteilMWh));
        }

        /// <summary>
        /// Der zweite Fall für EINE Anlage: KWK-Strom = min(Netto, Nutzwärme × σ), die
        /// Kürzung = Netto − KWK-Strom. Ohne bestimmbare Kennzahl ist der KWK-Strom 0 und
        /// die Kürzung die ganze Nettostromerzeugung — kein Wert nach Fall 2, kein
        /// Ersatz.
        /// </summary>
        public static KwkStromFall2 Fall2(double nettoMWh, double waermeMWh, double ueberschussAnteilMWh,
                                          KwkStromkennzahl sigma)
        {
            var f = new KwkStromFall2
            {
                NettoMWh = Math.Max(0, nettoMWh),
                WaermeMWh = Math.Max(0, waermeMWh),
                UeberschussAnteilMWh = Math.Max(0, ueberschussAnteilMWh),
                Stromkennzahl = sigma ?? new KwkStromkennzahl()
            };
            f.NutzwaermeMWh = Nutzwaerme(f.WaermeMWh, f.UeberschussAnteilMWh);
            f.KwkStromMWh = f.Stromkennzahl.Bestimmbar
                          ? Math.Max(0, Math.Min(f.NettoMWh, f.NutzwaermeMWh * f.Stromkennzahl.Wert.Value))
                          : 0;
            f.KuerzungMWh = Math.Max(0, f.NettoMWh - f.KwkStromMWh);
            return f;
        }

        /// <summary>
        /// Die Kürzung Netto − KWK-Strom geht <b>zuerst von der Einspeisemenge</b> ab,
        /// erst der Rest vom Eigenverbrauch (Entscheid E7‑Q2 (3)). Beide Mengen bleiben
        /// nicht negativ; gibt der Rest den Eigenverbrauch nicht her, bleibt er 0.
        /// </summary>
        public static void Kuerzen(double kuerzungMWh, ref double eigenMWh, ref double einspMWh)
        {
            if (kuerzungMWh <= 0) return;
            double vonEinsp = Math.Min(Math.Max(0, einspMWh), kuerzungMWh);
            einspMWh = Math.Max(0, einspMWh - vonEinsp);
            double rest = kuerzungMWh - vonEinsp;
            if (rest > 0) eigenMWh = Math.Max(0, eigenMWh - rest);
        }

        /// <summary>Ein Ressourcentext mit deutschem Rückfall (Muster
        /// <c>WirtschaftlichkeitCtrl.T</c>).</summary>
        private static string T(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch { return rueckfall; }
        }
    }
}
