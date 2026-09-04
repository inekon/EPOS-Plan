using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die ANZEIGEEINHEIT einer Energiemenge — MWh (Vorgabe) oder kWh
    /// (Anwenderentscheid W8‑O‑5 / W9‑O‑3 vom 04.09.2026).
    ///
    /// <para><b>Warum es die Klasse gibt.</b> Der Bestand trug die Einheit als
    /// Zeichenkette neben der Zahl: <c>EINHEIT_MWH</c> in der Ergebnishülle,
    /// <c>"MWh"</c> im Bedarfsprofildialog, „… in kWh …" in einer der drei
    /// Prüfmeldungen — und dazwischen ein nacktes <c>/ 1000</c>, das nur in EINEM
    /// der beiden Wege stand (Befund W8‑B4). Wer eine Zahl las, musste wissen, aus
    /// welcher Maske sie kam. Hier steht die Einheit stattdessen AM WERT: Die
    /// Hülle sagt, in welcher Einheit ihre Zahl vorliegt, die Anzeige sagt, in
    /// welcher sie sie zeigen will, und die Umrechnung ist eine einzige, geprüfte
    /// Rechnung.</para>
    ///
    /// <para><b>Die Umrechnung ist in der Identität EXAKT.</b> <see cref="AusMWh"/>
    /// auf <see cref="MWh"/> und <see cref="AusKWh"/> auf <see cref="KWh"/> geben
    /// den Wert unverändert zurück, statt ihn über <c>× 1000 × 0,001</c> zu
    /// schicken. Ohne diese Fallunterscheidung wäre eine Anzeige bei der Vorgabe
    /// MWh nicht mehr zeichengleich zum Bestand — der Faktor 0,001 ist als
    /// Gleitkommazahl nicht exakt.</para>
    ///
    /// <para><b>Der Rechenkern bleibt unberührt.</b> Diese Klasse rechnet ANZEIGEN
    /// um, nicht Simulationen: <c>SimulationWaermebedarf</c> und
    /// <c>SimulationStrombedarf</c> teilen weiter selbst durch 1000 bzw. 4000, und
    /// der Referenzlauf bleibt byte-gleich.</para>
    ///
    /// <para><b>Die Nachkommastellen gehören zur Einheit.</b> MWh zeigt zwei
    /// (<c>F2</c>, der Bestand), kWh keine (<c>F0</c>) — 594,30 kWh sind nicht
    /// genauer als 594, und drei Stellen mehr vor dem Komma brauchen keine zwei
    /// dahinter.</para>
    /// </summary>
    public sealed record Energieeinheit
    {
        private Energieeinheit(string text, string format)
        {
            Text = text;
            Format = format;
        }

        /// <summary>Megawattstunden — die VORGABE beider Bedarfsansichten.</summary>
        public static readonly Energieeinheit MWh = new Energieeinheit("MWh", "F2");

        /// <summary>
        /// Kilowattstunden. Heißt in C# <c>KWh</c> und trägt „kWh" als
        /// <see cref="Text"/>; ein Bezeichner fängt hier groß an.
        /// </summary>
        public static readonly Energieeinheit KWh = new Energieeinheit("kWh", "F0");

        private static readonly Energieeinheit[] ALLE = { MWh, KWh };

        /// <summary>Beide Einheiten in Anzeigereihenfolge — die Auswahlliste.</summary>
        public static IReadOnlyList<Energieeinheit> Alle
        {
            get { return ALLE; }
        }

        /// <summary>Die Vorgabe, wenn nichts gewählt wurde: <see cref="MWh"/>.</summary>
        public static Energieeinheit Vorgabe
        {
            get { return MWh; }
        }

        /// <summary>Das Kürzel — zugleich der abgelegte Schlüssel der Wahl.</summary>
        public string Text { get; }

        /// <summary>Die Formatangabe der Anzeige: <c>F2</c> bei MWh, <c>F0</c> bei kWh.</summary>
        public string Format { get; }

        /// <summary>Ein Wert in KILOWATTSTUNDEN, ausgedrückt in dieser Einheit.</summary>
        public double AusKWh(double kWh)
        {
            return ReferenceEquals(this, KWh) ? kWh : kWh / 1000.0;
        }

        /// <summary>Ein Wert in MEGAWATTSTUNDEN, ausgedrückt in dieser Einheit.</summary>
        public double AusMWh(double mWh)
        {
            return ReferenceEquals(this, MWh) ? mWh : mWh * 1000.0;
        }

        /// <summary>
        /// Ein Wert aus <paramref name="quelle"/>, ausgedrückt in dieser Einheit —
        /// der Weg, den beide Bedarfsansichten gehen: Die Hülle nennt die Einheit
        /// ihrer Zahl, die Anzeige rechnet um.
        /// </summary>
        public double Aus(Energieeinheit quelle, double wert)
        {
            if (quelle == null || ReferenceEquals(quelle, this)) return wert;
            return ReferenceEquals(quelle, KWh) ? AusKWh(wert) : AusMWh(wert);
        }

        /// <summary>Der Rückweg einer EINGABE in dieser Einheit nach Megawattstunden.</summary>
        public double NachMWh(double wert)
        {
            return ReferenceEquals(this, MWh) ? wert : wert / 1000.0;
        }

        /// <summary>Der Rückweg einer EINGABE in dieser Einheit nach Kilowattstunden.</summary>
        public double NachKWh(double wert)
        {
            return ReferenceEquals(this, KWh) ? wert : wert * 1000.0;
        }

        /// <summary>Der Anzeigetext eines Wertes, der bereits in dieser Einheit vorliegt.</summary>
        public string Formatiere(double wert)
        {
            return wert.ToString(Format, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Die Einheit zu einem abgelegten Kürzel; alles Unbekannte — auch
        /// <c>null</c> — ergibt die <see cref="Vorgabe"/>.
        /// </summary>
        public static Energieeinheit AusText(string text)
        {
            if (string.IsNullOrEmpty(text)) return Vorgabe;
            for (int i = 0; i < ALLE.Length; i++)
                if (string.Equals(ALLE[i].Text, text, StringComparison.OrdinalIgnoreCase))
                    return ALLE[i];
            return Vorgabe;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Text;
        }
    }

    /// <summary>
    /// Die GEMERKTE Einheitenwahl der beiden Bedarfsansichten (Anwenderentscheid
    /// vom 04.09.2026: „Konsistent in den Ansichten").
    ///
    /// <para>Der Bedarfsprofildialog (W9) und der Ergebnisdialog (W8) lesen
    /// dieselbe Wahl und schreiben sie beim Umschalten zurück — sonst stünde im
    /// einen Fenster MWh und im daraus geöffneten anderen kWh. Abgelegt wird sie
    /// über <see cref="Dienste.Einstellungen"/> unter <see cref="SCHLUESSEL"/>;
    /// ohne Eintrag gilt <see cref="Energieeinheit.Vorgabe"/>.</para>
    /// </summary>
    public static class BedarfEinheitWahl
    {
        /// <summary>Der Schlüssel in <see cref="Dienste.Einstellungen"/>.</summary>
        public const string SCHLUESSEL = "BedarfEinheit";

        /// <summary>Die gemerkte Wahl; ohne Eintrag MWh.</summary>
        public static Energieeinheit Lies()
        {
            string text = null;
            try { text = Dienste.Einstellungen.Lies(SCHLUESSEL, null); }
            catch { }
            return Energieeinheit.AusText(text);
        }

        /// <summary>Merkt die Wahl. <c>null</c> legt die Vorgabe ab.</summary>
        public static void Schreib(Energieeinheit einheit)
        {
            Energieeinheit e = einheit ?? Energieeinheit.Vorgabe;
            try { Dienste.Einstellungen.Schreib(SCHLUESSEL, e.Text); }
            catch { }
        }
    }
}
