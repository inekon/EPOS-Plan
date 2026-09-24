using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Ein Satz des Kerns als Kennung und Werte</b> (Umsetzungskonzept Zapfprofilgenerator,
    /// N11 (k); Stufe Z4): Jede Ablehnung, jeder Hinweis, jede Warnung und jeder Satz einer Karte,
    /// den der Zapfprofilgenerator liefert, ist eine <see cref="Kennung"/> mit sprachfreien
    /// <see cref="Werte"/> — kein fertiger Satz. Das Muster steht als Ressource
    /// <c>ZPG_SATZ_</c> + Kennung in BEIDEN Sprachen (<c>Resource.resx</c>, <c>Resource.en-US.resx</c>)
    /// mit denselben Platzhaltern <c>{0}</c>, <c>{1:0.###}</c> …
    ///
    /// <para><b>Zwei Lesarten.</b> <see cref="Klartext"/> ist der deutsche Wortlaut in
    /// invarianter Kultur (Punkt als Dezimalzeichen) — für Laufprotokoll, Ausnahmemeldung und Test,
    /// derselbe Satz auf jedem Rechner. <see cref="Text(Func{string, string}, IFormatProvider)"/>
    /// baut den Satz in einer Oberflächensprache und -kultur; das tut allein die Hülle.</para>
    ///
    /// <para><b>Werte</b> sind Zahlen (<c>double</c>, <c>int</c>, <c>long</c>), Texte (Zonen-,
    /// Nutzungsart- und Parameternamen — Daten, keine Sätze), ein eingebetteter <see cref="ZapfSatz"/>
    /// (ein Begriff wie „die Speichertemperatur" oder ein ganzer Grund) oder eine Liste davon
    /// (mit „, " verbunden). Die Zahlformate stehen im Muster, nicht im Kern.</para>
    ///
    /// <para><b>Rein:</b> keine Datenbank, keine Umgebung; das Muster liest der Kern aus seinen
    /// eigenen Ressourcen. Fehlt ein Muster, steht die Kennung mit ihren Werten da — benannt statt
    /// still (die Wache <c>ZapfSaetzeWacheTests</c> hält jede Kennung gegen beide Sprachen).</para>
    /// </summary>
    internal sealed class ZapfSatz
    {
        /// <summary>Das Präfix der Ressourcenschlüssel der Muster.</summary>
        internal const string PRAEFIX = "ZPG_SATZ_";

        private readonly object[] _werte;

        private ZapfSatz(string kennung, object[] werte)
        {
            if (string.IsNullOrWhiteSpace(kennung)) throw new ArgumentException("Ein Satz braucht eine Kennung.", nameof(kennung));
            Kennung = kennung;
            _werte = werte ?? new object[0];
        }

        /// <summary>Die Kennung des Satzes (ohne Präfix), etwa <c>EINGABE_BEZUGSMENGE_NICHT_POSITIV</c>.</summary>
        internal string Kennung { get; }

        /// <summary>Der Ressourcenschlüssel des Musters: <see cref="PRAEFIX"/> + <see cref="Kennung"/>.</summary>
        internal string Schluessel => PRAEFIX + Kennung;

        /// <summary>Die Werte der Platzhalter {0}, {1}, … — sprachfrei, in dieser Reihenfolge.</summary>
        internal IReadOnlyList<object> Werte => Array.AsReadOnly(_werte);

        /// <summary>Ein Satz aus Kennung und Werten.</summary>
        internal static ZapfSatz Neu(string kennung, params object[] werte)
        {
            // Ein einzelnes Feld eines Referenztyps (etwa string[]) bände C# als die Werte selbst
            // (Feldkovarianz) — es ist aber EIN Wert: eine Liste für einen Platzhalter.
            if (werte != null && werte.GetType() != typeof(object[])) werte = new object[] { werte };
            return new ZapfSatz(kennung, werte == null ? new object[0] : (object[])werte.Clone());
        }

        /// <summary>
        /// Der deutsche Wortlaut in invarianter Kultur — für Protokoll, Ausnahmemeldung und Test.
        /// </summary>
        internal string Klartext => Text(DeutschesMuster, CultureInfo.InvariantCulture);

        /// <summary>
        /// Der Satz in einer Sprache: <paramref name="muster"/> liefert zu einem Ressourcenschlüssel
        /// das Muster (<c>null</c> = unbekannt, dann gilt das deutsche), <paramref name="kultur"/>
        /// formatiert die Zahlen. Eingebettete Sätze folgen derselben Sprache.
        /// </summary>
        internal string Text(Func<string, string> muster, IFormatProvider kultur)
        {
            string m = muster?.Invoke(Schluessel) ?? DeutschesMuster(Schluessel);
            object[] werte = new object[_werte.Length];
            for (int i = 0; i < werte.Length; i++) werte[i] = Wert(_werte[i], muster, kultur);
            if (string.IsNullOrEmpty(m))
                return werte.Length == 0 ? Kennung
                    : Kennung + " (" + string.Join("; ", werte.Select(w => Convert.ToString(w, kultur))) + ")";
            try { return string.Format(kultur, m, werte); }
            catch (FormatException) { return m; }
        }

        /// <inheritdoc />
        public override string ToString() => Klartext;

        /// <summary>
        /// Zwei Sätze sind gleich, wenn Kennung und Werte gleich sind — damit ein Hinweis, der
        /// zweimal entsteht, nur einmal in der Liste steht (<see cref="ZapfHinweis.Einmal"/>).
        /// </summary>
        public override bool Equals(object obj)
            => obj is ZapfSatz s && string.Equals(Kennung, s.Kennung, StringComparison.Ordinal)
               && _werte.Length == s._werte.Length && _werte.Zip(s._werte, WertGleich).All(g => g);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            int h = StringComparer.Ordinal.GetHashCode(Kennung);
            foreach (object w in _werte) h = h * 31 + (w is IEnumerable e && w is not string ? e.Cast<object>().Count() : w?.GetHashCode() ?? 0);
            return h;
        }

        private static bool WertGleich(object a, object b)
        {
            if (a is IEnumerable ea && a is not string && b is IEnumerable eb && b is not string)
                return ea.Cast<object>().SequenceEqual(eb.Cast<object>());
            return Equals(a, b);
        }

        /// <summary>Ein Wert für das Muster: eingebettete Sätze und Listen als Text derselben Sprache.</summary>
        private static object Wert(object w, Func<string, string> muster, IFormatProvider kultur)
        {
            switch (w)
            {
                case null: return "";
                case ZapfSatz s: return s.Text(muster, kultur);
                case string t: return t;
                case IEnumerable liste:
                    var teile = new List<string>();
                    foreach (object o in liste) teile.Add(Convert.ToString(Wert(o, muster, kultur), kultur));
                    return string.Join(", ", teile);
                default: return w;
            }
        }

        /// <summary>Das deutsche Muster aus den neutralen Ressourcen des Kerns; <c>null</c>, wenn es fehlt.</summary>
        internal static string DeutschesMuster(string schluessel)
        {
            try
            {
                string t = MyResource.Resource.ResourceManager.GetString(schluessel, CultureInfo.InvariantCulture);
                return string.IsNullOrEmpty(t) ? null : t;
            }
            catch (Exception) { return null; }
        }
    }
}
