using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE E7c (E7c1‑Q7) — die fünf Spalten des zweiten Falls nach § 2 Nr. 16 KWKG
    /// in der Modultafel des Berichts: Fall, Stromkennzahl σ, Nutzwärme, KWK-Strom,
    /// Kürzung. Word und Excel lesen Kopf, Werte und Stellenzahl HIER — die zwei
    /// Tafeln zeigen dieselben Spalten, keine schreibt ihre eigene Auswahl.
    ///
    /// <para><b>Nur wenn ein Modul Fall 2 rechnet.</b> Ohne Kennzeichen steht in jeder
    /// Zeile „Fall 1" und in den übrigen vier Spalten nichts, was die Tafel nicht schon
    /// zeigt — sie bleibt dann, wie sie war (<see cref="Noetig"/>).</para>
    ///
    /// <para><b>Fall 1 in einer Tafel mit Fall 2:</b> Der KWK-Strom ist dort die
    /// Nettostromerzeugung (§ 2 Nr. 16, erster Fall); σ, Nutzwärme und Kürzung bleiben
    /// leer, denn Fall 1 liest sie nicht. Ein gebuchter Stand ohne Nettomenge zeigt
    /// auch beim KWK-Strom „—" statt einer 0.</para>
    /// </summary>
    internal static class KwkgFall2Spalten
    {
        /// <summary>Stellen der Mengen im Word-Bericht [MWh/a] — die Herleitung der
        /// Erlöszeile nennt sie mit drei.</summary>
        internal const int NACHKOMMASTELLEN_MWH = 1;

        /// <summary>Zellformat der Mengen im Excel-Bericht; der Zellwert bleibt ungerundet.</summary>
        internal const string EXCELFORMAT_MWH = "#,##0.0";

        /// <summary>Zellformat der Stromkennzahl im Excel-Bericht — drei Stellen wie
        /// <see cref="KwkStromRechner.FORMAT_KENNZAHL"/>.</summary>
        internal const string EXCELFORMAT_SIGMA = "0.000";

        /// <summary>Leerzeichen einer Zelle ohne Wert (Word).</summary>
        internal const string LEER = "—";

        /// <summary>true, sobald ein Modul der Tafel Fall 2 rechnet.</summary>
        internal static bool Noetig(IEnumerable<KwkgModulNachweis> module)
        {
            return module != null && module.Any(IstFall2);
        }

        internal static bool IstFall2(KwkgModulNachweis m)
        {
            return m != null && m.Abwaermeabfuhr == true;
        }

        /// <summary>Die fünf Spaltenköpfe in der Sprache des Laufs.</summary>
        internal static string[] Kopf()
        {
            return new[]
            {
                MyResource.Resource.WIRT_KWKG_SP_FALL,
                MyResource.Resource.WIRT_KWKG_SP_SIGMA,
                MyResource.Resource.WIRT_KWKG_SP_NUTZWAERME,
                MyResource.Resource.WIRT_KWKG_SP_KWK_STROM,
                MyResource.Resource.WIRT_KWKG_SP_KUERZUNG
            };
        }

        /// <summary>1 = Nettostromerzeugung, 2 = Vorrichtung zur Abwärmeabfuhr.</summary>
        internal static int Fall(KwkgModulNachweis m)
        {
            return IstFall2(m) ? 2 : 1;
        }

        internal static double? Stromkennzahl(KwkgModulNachweis m)
        {
            return IstFall2(m) ? m.Stromkennzahl : null;
        }

        internal static double? NutzwaermeMWh(KwkgModulNachweis m)
        {
            return IstFall2(m) ? m.NutzwaermeMWh : null;
        }

        /// <summary>Fall 2: min(Netto ; Nutzwärme × σ); Fall 1: die Nettostromerzeugung,
        /// sofern der Stand sie führt.</summary>
        internal static double? KwkStromMWh(KwkgModulNachweis m)
        {
            if (m == null) return null;
            if (IstFall2(m)) return m.KwkStromMWh;
            return m.StromNettoMWh > 0 ? m.StromNettoMWh : (double?)null;
        }

        internal static double? KuerzungMWh(KwkgModulNachweis m)
        {
            return IstFall2(m) ? m.KuerzungMWh : null;
        }

        /// <summary>Die fünf Zellen einer Word-Zeile; eine fehlende Größe wird „—".</summary>
        internal static string[] Werte(KwkgModulNachweis m, CultureInfo kultur)
        {
            string mwh = "N" + NACHKOMMASTELLEN_MWH.ToString(CultureInfo.InvariantCulture);
            return new[]
            {
                Fall(m).ToString(CultureInfo.InvariantCulture),
                Text(Stromkennzahl(m), KwkStromRechner.FORMAT_KENNZAHL, kultur),
                Text(NutzwaermeMWh(m), mwh, kultur),
                Text(KwkStromMWh(m), mwh, kultur),
                Text(KuerzungMWh(m), mwh, kultur)
            };
        }

        private static string Text(double? wert, string format, CultureInfo kultur)
        {
            return wert.HasValue ? wert.Value.ToString(format, kultur) : LEER;
        }
    }
}
