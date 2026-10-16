using System.Collections.Generic;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die kleine Grafik der Vorschaukarte im Klimadatenimport (Auftrag KD-3, Konzept
    /// Klimadatenimport § 4): Monatssumme der Globalstrahlung (linke Achse) und
    /// Monatsmittel der Lufttemperatur (rechte Achse) über den zwölf Monaten — als
    /// <see cref="Zeichenmodell"/> für den Baustein <c>DiagrammSvg</c>. Die Werte
    /// rechnet <see cref="KlimaKennzahlen"/>; hier wird nur gezeichnet.
    ///
    /// <para>Gezeichnet über <see cref="ChartRenderer.SummenlinieModell"/>: freie x-Achse
    /// (Monat 1 bis 12), zwei Achsen, keine eigene Zeichenroutine.</para>
    /// </summary>
    public static class KlimaVorschaubild
    {
        /// <summary>Die Beschriftungen; Vorgabe sind die Ressourcen der aktuellen Sprache.</summary>
        public sealed class Texte
        {
            public string Titel { get; set; } = MyResource.Resource.KLIMA_VK_BILD_TITEL;
            public string Monat { get; set; } = MyResource.Resource.KLIMA_VK_BILD_MONAT;
            public string Strahlung { get; set; } = MyResource.Resource.KLIMA_VK_BILD_STRAHLUNG;
            public string Temperatur { get; set; } = MyResource.Resource.KLIMA_VK_BILD_TEMPERATUR;
        }

        /// <summary>Das Bild; <c>null</c> ohne Kennzahlen oder ohne zwölf Monatswerte.</summary>
        public static Zeichenmodell Modell(KlimaKennzahlen k, Texte texte = null)
        {
            if (k == null || k.MonatsmittelTemperatur == null || k.MonatssummeGlobalstrahlungKwhM2 == null ||
                k.MonatsmittelTemperatur.Length != 12 || k.MonatssummeGlobalstrahlungKwhM2.Length != 12)
                return null;
            texte ??= new Texte();

            var monate = new double[12];
            for (int i = 0; i < 12; i++) monate[i] = i + 1;

            var links = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe(texte.Strahlung, (double[])k.MonatssummeGlobalstrahlungKwhM2.Clone(),
                                        Farbrolle.WAERME_SOLAR)
            };
            var rechts = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe(texte.Temperatur, (double[])k.MonatsmittelTemperatur.Clone(),
                                        Farbrolle.AUSSENTEMPERATUR)
            };
            return ChartRenderer.SummenlinieModell(texte.Titel, links, monate, 1.0, 1.0, texte.Monat,
                                                   texte.Strahlung, rechts, texte.Temperatur);
        }
    }
}
