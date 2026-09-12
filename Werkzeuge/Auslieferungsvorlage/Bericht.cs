using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Auslieferungsvorlage
{
    /// <summary>
    /// Der Prüfbericht — gleichzeitig Konsolenausgabe und Datei <c>&lt;ziel&gt;.bericht.txt</c>.
    ///
    /// <para><b>Warum eine Datei neben der Zieldatei.</b> Die Vorlage ist ein
    /// Auslieferungsartefakt: Wer sie in ein Setup packt, muss VORHER sehen koennen, was
    /// aus welchem Bestand entstanden ist, wie viele Zeilen je Katalog stehen und dass
    /// kein Projekt uebrig blieb. Setup-Konzept 6.1 verlangt genau diese Gegenpruefung
    /// („Projektliste leer, Katalogzahlen plausibel, Dateigroesse deutlich unter …");
    /// bis hierher war sie Handarbeit auf Zuruf.</para>
    /// </summary>
    internal sealed class Bericht
    {
        private readonly StringBuilder _text = new StringBuilder();
        private readonly bool _aufKonsole;

        internal Bericht(bool aufKonsole) { _aufKonsole = aufKonsole; }

        /// <summary>Sammelt jede Zeile, die mit „WARNUNG" oder „FEHLER" beginnt.</summary>
        internal List<string> Auffaelligkeiten { get; } = new List<string>();

        internal void Zeile(string text = "")
        {
            _text.AppendLine(text);
            if (_aufKonsole) Console.WriteLine(text);
            string k = (text ?? string.Empty).TrimStart();
            if (k.StartsWith("WARNUNG", StringComparison.Ordinal) || k.StartsWith("FEHLER", StringComparison.Ordinal))
                Auffaelligkeiten.Add(k);
        }

        internal void Leer() => Zeile();

        internal void Abschnitt(string ueberschrift)
        {
            Zeile();
            Zeile(new string('=', 78));
            Zeile(ueberschrift);
            Zeile(new string('=', 78));
        }

        internal void Tabellenkopf(string a, string b, string c) =>
            Zeile(a.PadRight(46) + b.PadLeft(14) + c.PadLeft(14));

        internal void Tabellenzeile(string a, long b, long c) =>
            Zeile(a.PadRight(46) +
                  b.ToString("#,##0", CultureInfo.InvariantCulture).PadLeft(14) +
                  c.ToString("#,##0", CultureInfo.InvariantCulture).PadLeft(14));

        internal void Speichern(string pfad)
        {
            // UTF-8 ohne Vorspann: Der Bericht wird auch von Skripten gelesen
            // (Setup/build-setup.ps1), und ein BOM stoert dort mehr, als er nutzt.
            File.WriteAllText(pfad, _text.ToString(), new UTF8Encoding(false));
        }

        public override string ToString() => _text.ToString();
    }
}
