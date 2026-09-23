using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Stand eines gerechneten Werts (Umsetzungskonzept Zapfprofilgenerator 2.1;
    /// Methodikkonzept 2.1: Vorgabe, vom Anwender überschrieben, aus Messdaten kalibriert —
    /// dazu „umgerechnet", wenn ein Katalogwert auf die Projekttemperaturen gebracht wurde, 4.1).
    /// </summary>
    internal enum Wertstatus
    {
        /// <summary>Der Wert kommt aus dem Katalog oder dem Parametersatz, unverändert.</summary>
        Vorgabe = 1,

        /// <summary>Der Anwender hat die Vorgabe überschrieben (eine nullbare Spalte der Zone oder des Projekts ist gesetzt).</summary>
        Ueberschrieben = 2,

        /// <summary>Der Wert ist gegen einen Messwert kalibriert (4.1, Messwertgrenzen).</summary>
        Kalibriert = 3,

        /// <summary>Der Wert ist über den Temperaturfaktor auf die Projekttemperaturen umgerechnet (4.0, 4.1).</summary>
        Umgerechnet = 4
    }

    /// <summary>
    /// Ein Eintrag des Herkunftsprotokolls: welcher Wert (<see cref="Feld"/>) einer Zone —
    /// leer für das Projekt — welchen Stand hat, woher er kommt und was dabei geschah.
    /// <see cref="Quelle"/> ist die Provenienz der Katalog- oder Parametergruppe, aus der der
    /// Wert stammt; <c>null</c>, wo der Anwender ihn gesetzt hat. <see cref="Vermerk"/> nennt
    /// Faktor, Messwertquelle oder Rechenweg im Klartext.
    /// </summary>
    internal sealed record Herkunftseintrag(
        string Zone,
        string Feld,
        double? Wert,
        string Einheit,
        Wertstatus Status,
        Provenienz Quelle,
        string Vermerk);

    /// <summary>
    /// <b>Das Herkunftsprotokoll einer Rechnung</b> (Konzept 2.1, Provenienz): je Feld und
    /// Zone ein Eintrag, in der Reihenfolge, in der der Rechenweg die Werte festlegt. Die
    /// Rechenklassen schreiben hinein; das Ergebnis gibt eine unveränderliche Abschrift
    /// weiter (<see cref="Abschrift"/>).
    /// </summary>
    internal sealed class Herkunftsprotokoll
    {
        private readonly List<Herkunftseintrag> _eintraege = new List<Herkunftseintrag>();

        /// <summary>Alle Einträge in der Reihenfolge ihres Entstehens — nur lesbar.</summary>
        internal IReadOnlyList<Herkunftseintrag> Eintraege => _eintraege.AsReadOnly();

        /// <summary>Hält einen Wert fest.</summary>
        internal void Vermerken(string zone, string feld, double? wert, string einheit, Wertstatus status,
                                Provenienz quelle, string vermerk = null)
        {
            if (string.IsNullOrEmpty(feld)) throw new ArgumentException("Ein Protokolleintrag ohne Feld.", nameof(feld));
            _eintraege.Add(new Herkunftseintrag(zone ?? "", feld, wert, einheit ?? "", status, quelle, vermerk ?? ""));
        }

        /// <summary>Der zuletzt vermerkte Eintrag zu Zone und Feld; <c>null</c>, wenn es keinen gibt.</summary>
        internal Herkunftseintrag Letzter(string zone, string feld)
        {
            for (int i = _eintraege.Count - 1; i >= 0; i--)
            {
                Herkunftseintrag e = _eintraege[i];
                if (string.Equals(e.Zone, zone ?? "", StringComparison.Ordinal)
                    && string.Equals(e.Feld, feld, StringComparison.Ordinal)) return e;
            }
            return null;
        }

        /// <summary>Eine unveränderliche Abschrift aller Einträge.</summary>
        internal IReadOnlyList<Herkunftseintrag> Abschrift() => Array.AsReadOnly(_eintraege.ToArray());
    }

    /// <summary>
    /// Die Feldnamen des Herkunftsprotokolls — an EINER Stelle, damit Rechenweg, Tests und
    /// später die Oberfläche dieselben Wörter benutzen.
    /// </summary>
    internal static class ZapfFeld
    {
        internal const string BEZUGSMENGE = "Bezugsmenge";
        internal const string BEDARF_SPEZ = "BedarfSpez";
        internal const string FLAECHENKENNWERT = "Flaechenkennwert";
        internal const string TAGESBEDARF = "Tagesbedarf";
        internal const string ZAPFTEMPERATUR = "Zapftemperatur";
        internal const string KALTWASSER_MITTEL = "KaltwasserMittel";
        internal const string KALTWASSER_AMPLITUDE = "KaltwasserAmplitude";
        internal const string KALTWASSER_MONAT_MAXIMUM = "KaltwasserMonatMaximum";
        internal const string TEMPERATURFAKTOR = "Temperaturfaktor";
        internal const string JAHRESENERGIE = "Jahresenergie";
        internal const string MONATSFAKTOREN = "Monatsfaktoren";
        internal const string WOCHENFAKTOREN = "Wochenfaktoren";
        internal const string TAGESGANGSATZ = "Tagesgangsatz";
        internal const string FERIENFAKTOR = "Ferienfaktor";
        internal const string KALIBRIERFAKTOR = "Kalibrierfaktor";
        internal const string ZIRKULATION_METHODE = "Zirkulation.Methode";
        internal const string ZIRKULATION_LAUFZEIT = "Zirkulation.Laufzeit";
        internal const string ZIRKULATION_ANTEIL = "Zirkulation.Anteil";
        internal const string ZIRKULATION_KENNWERT = "Zirkulation.Kennwert";
        internal const string ZIRKULATION_LAGE = "Zirkulation.Lage";
        internal const string ZIRKULATION_FLAECHE = "Zirkulation.Flaeche";
        internal const string ZIRKULATION_LAENGE = "Zirkulation.Laenge";
        internal const string ZIRKULATION_VERLUST_JE_METER = "Zirkulation.VerlustJeMeter";
        internal const string ZIRKULATION_LEISTUNG = "Zirkulation.Leistung";
        internal const string ZIRKULATION_GEWICHT = "Zirkulation.Gewicht";
        internal const string ZIRKULATION_ZONENANTEIL = "Zirkulation.Zonenanteil";
        internal const string ZIRKULATION_JAHRESVERLUST = "Zirkulation.Jahresverlust";
    }
}
