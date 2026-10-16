using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        /// <summary>
        /// Das Bild einer WAHL (Auftrag KD-8 Teil A, Konzept § 4a.1): je Einheit eine Achse,
        /// höchstens zwei; Strahlung (kWh/m²) links, sonst die zuerst gewählte Einheit. Reihen
        /// einer dritten Einheit und Größen ohne Monatsreihe fallen weg. Die Vorgabe
        /// (<see cref="KlimaReihenkatalog.VORGABE_VORSCHAU"/>) ergibt das Bild von
        /// <see cref="Modell(KlimaKennzahlen, Texte)"/>. <c>null</c> ohne Reihe.
        /// </summary>
        public static Zeichenmodell Modell(KlimaKennzahlen k, IReadOnlyList<string> auswahl, Texte texte = null)
        {
            if (auswahl == null || auswahl.Count == 0 || auswahl.SequenceEqual(KlimaReihenkatalog.VORGABE_VORSCHAU))
                return Modell(k, texte);
            if (k == null) return null;
            texte ??= new Texte();

            var gueltig = auswahl.Where(s => KlimaReihenkatalog.Reihe(s) != null && k.Monatsreihe(s) != null)
                                 .Distinct().ToList();
            IReadOnlyList<string> einheiten = KlimaReihenkatalog.Monatseinheiten(gueltig);
            if (einheiten.Count == 0) return null;

            List<ChartRenderer.Reihe> Achse(string einheit) => gueltig
                .Select(KlimaReihenkatalog.Reihe)
                .Where(r => r.Monatseinheit == einheit)
                .Select(r => new ChartRenderer.Reihe(Reihenname(r, texte), (double[])k.Monatsreihe(r.Schluessel).Clone(), r.Rolle))
                .ToList();

            string Titel(List<ChartRenderer.Reihe> reihen, string einheit)
                => reihen.Count == 1 ? reihen[0].Name : einheit;

            var monate = new double[12];
            for (int i = 0; i < 12; i++) monate[i] = i + 1;

            List<ChartRenderer.Reihe> links = Achse(einheiten[0]);
            if (einheiten.Count == 1)
                return ChartRenderer.SummenlinieModell(texte.Titel, links, monate, 1.0, 1.0, texte.Monat,
                                                       Titel(links, einheiten[0]));
            List<ChartRenderer.Reihe> rechts = Achse(einheiten[1]);
            return ChartRenderer.SummenlinieModell(texte.Titel, links, monate, 1.0, 1.0, texte.Monat,
                                                   Titel(links, einheiten[0]), rechts, Titel(rechts, einheiten[1]));
        }

        /// <summary>Der Name einer Reihe im Monatsbild: die zwei Vorgabereihen wie heute, sonst „Name [Einheit]“.</summary>
        private static string Reihenname(KlimaReihe r, Texte texte) => r.Schluessel switch
        {
            KlimaReihenkatalog.R_GLOBAL => texte.Strahlung,
            KlimaReihenkatalog.R_TEMPERATUR => texte.Temperatur,
            _ => string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KLIMA_RW_NAME_EINHEIT, r.Name(), r.Monatseinheit)
        };

        /// <summary>
        /// Der JAHRESVERLAUF des Reiters „Weitere Reihen“ (KD-8 Teil A, Konzept § 4a.2): bis zu drei
        /// Reihen EINER Einheit über <see cref="ChartRenderer.JahresgangModell"/> (unverändert). Reihen
        /// einer anderen Einheit als der ersten fallen weg. <c>null</c> ohne Reihe.
        /// </summary>
        public static Zeichenmodell Jahresverlauf(IReadOnlyList<KlimaReihenwerte> reihen)
        {
            var gueltig = (reihen ?? Array.Empty<KlimaReihenwerte>())
                .Where(r => r?.Reihe != null && r.Werte != null && r.Werte.Length >= 2).ToList();
            if (gueltig.Count == 0) return null;
            string einheit = gueltig[0].Reihe.Einheit;
            var zeichnen = gueltig.Where(r => r.Reihe.Einheit == einheit)
                                  .Take(KlimaReihenkatalog.JAHRESVERLAUF_REIHEN_MAX)
                                  .Select(r => new ChartRenderer.Reihe(r.Reihe.Name(), r.Werte, r.Reihe.Rolle))
                                  .ToList();
            string titel = zeichnen.Count == 1 ? zeichnen[0].Name : MyResource.Resource.KLIMA_RW_DIA_TITEL;
            return ChartRenderer.JahresgangModell(titel, zeichnen, MyResource.Resource.KLIMA_ACHSE_X,
                string.Format(CultureInfo.CurrentCulture, MyResource.Resource.KLIMA_RW_ACHSE, einheit),
                gueltig[0].Reihe.Monatsregel == KlimaMonatsregel.Summe);
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
