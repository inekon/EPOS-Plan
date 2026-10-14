using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Tagesansicht einer TWW-Messreihe</b> im Optionendialog „Format und Vorschau“ (IM-4):
    /// statt des Jahresbilds EIN Tag der gelesenen <see cref="Messreihe"/>.
    ///
    /// <para><b>Welcher Tag.</b> Der erste VOLLSTÄNDIGE Tag der Datei — der erste Schritt, der um
    /// 00:00 beginnt und dem ein ganzer Tag folgt (<c>1440 / Auflösung</c> Schritte). Hat die Datei
    /// keinen solchen Tag, zeigt das Bild den ersten Tag ab dem Beginn der Reihe, so weit sie
    /// reicht.</para>
    ///
    /// <para><b>Die Zeitachse zählt Stunden</b> (0 … 24): Je Stunde steht die Menge ihrer Schritte
    /// (<see cref="Messreihe.MengeJeSchritt"/>, kWh oder m³ — eine Leistung als kWh). So ist eine
    /// Minuten- wie eine Stundenreihe im selben Bild lesbar. Eine Tagesreihe (Auflösung ein Tag)
    /// hat keinen Verlauf im Tag; sie zeigt die Tagesmengen der ersten
    /// <see cref="TAGE_TAGESREIHE"/> Tage.</para>
    ///
    /// <para><b>Die Kennzahlenzeile</b> nennt die Zahl der gelesenen Tage und den gezeigten Tag.
    /// Das Bild ist ein vorhandenes Modell (<see cref="ChartRenderer.StundenprofilModell"/>); der
    /// Renderer bleibt unberührt.</para>
    /// </summary>
    internal static class GanglinienTagesbild
    {
        /// <summary>Stunden eines Tages.</summary>
        internal const int STUNDEN_JE_TAG = 24;

        /// <summary>Gezeigte Tage einer Tagesreihe (Auflösung ein Tag).</summary>
        internal const int TAGE_TAGESREIHE = 31;

        /// <summary>Die Einheit einer gemessenen Größe je Schritt; <c>null</c> = erst die Datei sagt sie.</summary>
        internal static string Einheit(ZapfMessgroesse? groesse)
        {
            if (groesse == null) return "";
            return groesse == ZapfMessgroesse.Volumen ? "m\u00b3" : "kWh";
        }

        /// <summary>Der erste Schritt des ersten vollständigen Tages; -1 = keiner.</summary>
        internal static int ErsterVollerTag(Messreihe reihe)
        {
            if (reihe == null || reihe.AufloesungMin <= 0 || reihe.AufloesungMin >= Messreihe.MINUTEN_JE_TAG) return -1;
            int jeTag = Messreihe.MINUTEN_JE_TAG / reihe.AufloesungMin;
            for (int i = 0; i + jeTag <= reihe.Schritte && i < jeTag; i++)
                if (reihe.Zeitpunkt(i).TimeOfDay == TimeSpan.Zero) return i;
            return -1;
        }

        /// <summary>
        /// Die Mengen je Stunde des gezeigten Tages (24 Werte; eine Tagesreihe: die Tagesmengen der
        /// ersten Tage) und der Beginn des gezeigten Ausschnitts.
        /// </summary>
        internal static double[] Werte(Messreihe reihe, out DateTime beginn)
        {
            beginn = reihe.Beginn;
            IReadOnlyList<double> menge = reihe.MengeJeSchritt;
            if (reihe.AufloesungMin >= Messreihe.MINUTEN_JE_TAG)
                return menge.Take(Math.Min(TAGE_TAGESREIHE, menge.Count)).ToArray();

            int start = ErsterVollerTag(reihe);
            if (start < 0) start = 0;
            beginn = reihe.Zeitpunkt(start);
            int jeTag = Messreihe.MINUTEN_JE_TAG / reihe.AufloesungMin;
            var stunden = new double[STUNDEN_JE_TAG];
            for (int j = 0; j < jeTag && start + j < menge.Count; j++)
            {
                int stunde = j * reihe.AufloesungMin / 60;
                if (stunde < STUNDEN_JE_TAG) stunden[stunde] += menge[start + j];
            }
            return stunden;
        }

        /// <summary>
        /// Das Tagesbild einer gelesenen Reihe samt Kennzahlenzeile; die Werte sind die Mengen je
        /// Schritt der ganzen Reihe — das, was das Einspielen übernimmt.
        /// </summary>
        internal static GanglinienDateibild AusMessreihe(Messreihe reihe)
        {
            if (reihe == null) return GanglinienDateibild.Abgelehnt(MyResource.Resource.IMPORT_PROT_DATEI_LEER);
            CultureInfo k = CultureInfo.CurrentCulture;
            bool tagesreihe = reihe.AufloesungMin >= Messreihe.MINUTEN_JE_TAG;
            double[] werte = Werte(reihe, out DateTime beginn);
            string einheit = reihe.IstVolumen ? "m\u00b3" : "kWh";
            string yTitel = reihe.IstVolumen ? MyResource.Resource.IMPORT_ACHSE_TWW_VOLUMEN
                                             : MyResource.Resource.IMPORT_ACHSE_TWW_ENERGIE;
            string xTitel = tagesreihe ? MyResource.Resource.IMPORT_ACHSE_TAG_DER_REIHE
                                       : MyResource.Resource.IMPORT_ACHSE_TAGESSTUNDE;
            Zeichnung.Zeichenmodell modell = ChartRenderer.StundenprofilModell("", werte, tagesreihe ? 0 : 3,
                                                                                xTitel, yTitel, einheit);
            string tage = reihe.Tage.ToString("N1", k);
            string kennzahl = tagesreihe
                ? string.Format(k, MyResource.Resource.IMPORT_PROBE_TAGE_TAGESREIHE, tage,
                                werte.Length.ToString("N0", k))
                : string.Format(k, MyResource.Resource.IMPORT_PROBE_TAGE, tage, beginn.ToString("d", k));
            return new GanglinienDateibild(modell, reihe.MengeJeSchritt.ToArray(), kennzahl, "", einheit);
        }
    }
}
