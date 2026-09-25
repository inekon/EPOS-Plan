using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;

namespace ZapfprofilValidierung
{
    /// <summary>
    /// <b>Die Wache über dem Bericht</b> (Konzept Kapitel 9 K5: „bis dahin nur Verhältniszahlen",
    /// Kapitel 4.8: „Objektdaten nie im Repositorium"). Sie hält jeden geschriebenen Berichtstext
    /// gegen zwei Regeln und <b>bricht den Lauf ab</b>, wenn eine verletzt ist — die Datei entsteht
    /// dann nicht:
    ///
    /// <list type="number">
    /// <item><b>Keine Einheit einer Menge oder Leistung.</b> Ein Bericht, der nur Verhältniszahlen
    /// führt, braucht <c>kWh</c>, <c>kW</c>, <c>m³</c>, <c>Btu</c> oder <c>Liter</c> nicht. Steht
    /// eine solche Einheit da, ist mit ihr eine Menge geschrieben — gleich, welche.</item>
    /// <item><b>Keine der Kennzahlen der Messreihe selbst</b> — Jahresmenge, Energie, größter Wert,
    /// Tagesmittel —, in allen Schreibweisen, die ein Bericht benutzen könnte (0 bis 3
    /// Nachkommastellen, Punkt und Komma).</item>
    /// </list>
    ///
    /// <para><b>Warum eine Wache und nicht nur Sorgfalt.</b> Der Bericht ist die eine Stelle, an der
    /// Messdaten das Werkzeug verlassen; er wird ins Repositorium gelegt. Ein versehentlich
    /// mitgeschriebener Absolutwert wäre dort dauerhaft. Die Wache kostet einen Durchlauf über den
    /// Text und ist der Grund, warum der Bericht überhaupt versioniert werden darf.</para>
    ///
    /// <para><b>Was sie nicht kann:</b> Sie erkennt keinen Objektnamen — eine Kennung ist von einem
    /// Namen nicht zu unterscheiden. Die Anonymisierung ist und bleibt Sache des Anwenders; das
    /// <c>LIESMICH.md</c> sagt es.</para>
    /// </summary>
    internal static class Berichtswache
    {
        /// <summary>Die verbotenen Einheiten; Groß- und Kleinschreibung spielt keine Rolle.</summary>
        internal static readonly IReadOnlyList<string> EINHEITEN = Array.AsReadOnly(new[]
        {
            "kWh", "MWh", "kW ", "kW]", "kW)", " Wh", "m³", "m3/", " m3 ", "Btu", "Liter", "l/min", "l/d"
        });

        /// <summary>
        /// <b>Die kleinste Zahl von Ziffern</b>, mit der gesucht wird — die eigentliche Schärfe der
        /// zweiten Regel. Eine kurze Zahl ist <b>kein Fingerabdruck</b>: „0,621" ist ebenso gut eine
        /// Bandgrenze der Dauerlinie wie eine Tagesmenge in m³, und die Wache schlüge Alarm, wo nur
        /// eine Verhältniszahl steht. Ein <b>versehentlich</b> mitgeschriebener Absolutwert trägt
        /// dagegen die Genauigkeit, mit der er gerechnet wurde, also viele Stellen. Sechs Ziffern
        /// sind die Grenze: Der Bericht schreibt keine Verhältniszahl mit sechs Ziffern
        /// (Verhältnisse stehen mit höchstens vier Nachkommastellen, das Residuum als 0).
        ///
        /// <para><b>Die zweite Regel ist die zweite Schicht, nicht die erste.</b> Die erste ist die
        /// Einheitenregel, die dritte ist die Bauform: <see cref="Objektbefund"/> führt keinen
        /// absoluten Messwert, den der Bericht überhaupt schreiben könnte — der
        /// Kalibriervorschlag steht als Verhältnis da, nicht als Betrag.</para>
        /// </summary>
        internal const int MINDESTZIFFERN = 6;

        /// <summary>
        /// Die kleinste Zahl der Nachkommastellen, mit der gesucht wird — und der <b>Grund</b>: Eine
        /// auf eine Stelle gerundete Messgröße ist <b>kein Fingerabdruck</b>. „37,2" ist ebenso gut
        /// eine Abweichung von −37,2 %, und die Wache schlüge Alarm, wo nur eine Verhältniszahl
        /// steht. Ein <b>versehentlich</b> mitgeschriebener Absolutwert trägt dagegen seine eigene
        /// Genauigkeit, also mehrere Stellen; ab zwei Nachkommastellen ist ein Zusammentreffen kein
        /// Thema mehr. Gesucht wird bis <see cref="STELLEN_BIS"/> und zusätzlich in der vollen
        /// Rundreise-Schreibweise.
        /// </summary>
        internal const int STELLEN_VON = 2;

        /// <summary>Die größte Zahl der Nachkommastellen, mit der gesucht wird.</summary>
        internal const int STELLEN_BIS = 6;

        /// <summary>
        /// Prüft <paramref name="text"/>. Ergebnis ist die Liste der Fundstellen; leer = in Ordnung.
        /// <paramref name="reihe"/> darf <c>null</c> sein — dann greift nur die Einheitenregel.
        /// </summary>
        internal static List<string> Pruefen(string text, Messreihe reihe, double spreizungK)
            => Pruefen(text, Verbotene(reihe, spreizungK));

        /// <summary>
        /// Wie <see cref="Pruefen(string, Messreihe, double)"/>, aber mit einer schon gebildeten
        /// Liste verbotener Schreibweisen — der Weg des Sammelberichts, der die Zahlen ALLER Objekte
        /// meiden muss.
        /// </summary>
        internal static List<string> Pruefen(string text, IEnumerable<string> verbotene)
        {
            var funde = new List<string>();
            if (string.IsNullOrEmpty(text)) return funde;

            foreach (string e in EINHEITEN)
                if (text.Contains(e, StringComparison.OrdinalIgnoreCase))
                    funde.Add("Die Einheit \"" + e.Trim() + "\" steht im Bericht - mit ihr steht dort eine Menge "
                              + "oder Leistung. Der Bericht traegt nur Verhaeltniszahlen (K5).");

            foreach (string z in verbotene ?? new string[0])
                if (Steht(text, z))
                    funde.Add("Die Zahl \"" + z + "\" ist eine Kennzahl der MESSUNG und steht im Bericht (K5): "
                              + Umfeld(text, z));
            return funde;
        }

        /// <summary>
        /// <b>Steht die Zahl als Zahl da?</b> Eine reine Teilzeichenprüfung schlägt falschen Alarm:
        /// „37.25" steckt in „137.256" und in „0.37.25". Gefunden wird deshalb nur, was <b>an beiden
        /// Enden</b> nicht in einer längeren Zahl steht.
        ///
        /// <para><b>Ein Punkt allein ist keine Grenze und kein Weiterlauf.</b> „… 23561.63." am
        /// Satzende ist die Zahl, „… 23561.631" ist eine andere. Ein Punkt oder Komma direkt neben der
        /// Fundstelle zählt deshalb nur dann als Teil einer längeren Zahl, wenn <b>dahinter wieder
        /// eine Ziffer</b> steht.</para>
        /// </summary>
        internal static bool Steht(string text, string zahl)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(zahl)) return false;
            int i = 0;
            while ((i = text.IndexOf(zahl, i, StringComparison.Ordinal)) >= 0)
            {
                int nach = i + zahl.Length;
                if (Frei(text, i - 1, -1) && Frei(text, nach, +1)) return true;
                i = nach;
            }
            return false;
        }

        /// <summary>
        /// Ist die Stelle <paramref name="stelle"/> eine Grenze? <paramref name="richtung"/> ist die
        /// Leserichtung nach außen (−1 links, +1 rechts).
        /// </summary>
        private static bool Frei(string text, int stelle, int richtung)
        {
            if (stelle < 0 || stelle >= text.Length) return true;
            char c = text[stelle];
            if (char.IsDigit(c)) return false;
            if (c != '.' && c != ',') return true;
            int weiter = stelle + richtung;
            return weiter < 0 || weiter >= text.Length || !char.IsDigit(text[weiter]);
        }

        /// <summary>Das Umfeld der ersten Fundstelle — damit die Zeile im Bericht zu finden ist.</summary>
        internal static string Umfeld(string text, string zahl)
        {
            int i = text.IndexOf(zahl, StringComparison.Ordinal);
            if (i < 0) return "";
            int a = Math.Max(0, i - 70);
            int b = Math.Min(text.Length, i + zahl.Length + 20);
            char[] umfeld = text.Substring(a, b - a).ToCharArray();
            for (int k = 0; k < umfeld.Length; k++)
                if (char.IsControl(umfeld[k])) umfeld[k] = ' ';
            return "…" + new string(umfeld) + "…";
        }

        /// <summary>
        /// Die Schreibweisen, die nicht im Bericht stehen dürfen: Jahresmenge der Reihe (in ihrer
        /// eigenen Einheit), ihre Energie, ihr größter Wert und ihr Tagesmittel, je mit 0 bis 3
        /// Nachkommastellen und mit Punkt wie mit Komma.
        /// </summary>
        internal static IReadOnlyList<string> Verbotene(Messreihe reihe, double spreizungK)
        {
            if (reihe == null) return new string[0];
            var werte = new List<double> { reihe.Menge, reihe.GroessterWert };
            try { werte.Add(reihe.EnergieKwh(spreizungK)); } catch (Exception) { /* ohne Spreizung keine Energie */ }
            if (reihe.Tage > 0.0) werte.Add(reihe.Menge / reihe.Tage);

            var liste = new List<string>();
            foreach (double w in werte)
            {
                if (double.IsNaN(w) || double.IsInfinity(w) || w == 0.0) continue;
                for (int n = STELLEN_VON; n <= STELLEN_BIS; n++)
                    Aufnehmen(liste, w.ToString("F" + n.ToString(CultureInfo.InvariantCulture),
                                                CultureInfo.InvariantCulture));
                Aufnehmen(liste, w.ToString("R", CultureInfo.InvariantCulture));
            }
            return liste;
        }

        /// <summary>Nimmt eine Schreibweise und ihre Kommafassung auf, wenn sie lang genug ist.</summary>
        private static void Aufnehmen(List<string> liste, string s)
        {
            if (s == null || s.Count(char.IsDigit) < MINDESTZIFFERN) return;
            if (!liste.Contains(s)) liste.Add(s);
            string komma = s.Replace('.', ',');
            if (!liste.Contains(komma)) liste.Add(komma);
        }

        /// <summary>Die Fundstellen als ein Satz für stderr.</summary>
        internal static string Meldung(string datei, IEnumerable<string> funde)
            => "Die Berichtswache haelt \"" + datei + "\" zurueck:" + Environment.NewLine
               + string.Join(Environment.NewLine, funde.Select(f => "  - " + f));
    }
}
