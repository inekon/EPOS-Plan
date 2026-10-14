using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Text des Rechenkerns als <b>Ressourcenschlüssel mit Argumenten</b> (Herkunft und Rechenweg der
    /// Pufferspeicher-Auslegung). Der Kern formatiert nicht in einer Sprache: Oberfläche und Bericht lösen
    /// den Baustein mit <see cref="Aufloesen(CultureInfo)"/> in ihrer Sprache auf — Muster aus
    /// <c>MyResource.Resource</c>, Zahlen mit der Kultur formatiert (höchstens drei Nachkommastellen),
    /// Bausteine unter den Argumenten rekursiv. Fehlt der Schlüssel, gilt das deutsche Rückfallmuster
    /// (<see cref="Rueckfall"/>), damit nichts leer wird. <see cref="Klartext"/> ist der deutsche Text,
    /// unabhängig von der Kultur des Fadens (Protokolle, Tests, Rückfall).
    /// </summary>
    public sealed class Textbaustein : IEquatable<Textbaustein>
    {
        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        /// <summary>Der leere Baustein (kein Schlüssel, kein Text).</summary>
        public static readonly Textbaustein Leer = new Textbaustein(null, "");

        /// <summary>Der Ressourcenschlüssel; <c>null</c> = reiner Klartext (z. B. Tabellenname, Fehlermeldung).</summary>
        public string Schluessel { get; }

        /// <summary>Das deutsche Muster (<c>{0}</c> … wie in der Ressource) — Rückfall, wenn der Schlüssel fehlt.</summary>
        public string Rueckfall { get; }

        /// <summary>Die Argumente: Zahlen, Texte oder Bausteine.</summary>
        public IReadOnlyList<object> Argumente { get; }

        public Textbaustein(string schluessel, string rueckfall, params object[] argumente)
        {
            Schluessel = string.IsNullOrEmpty(schluessel) ? null : schluessel;
            Rueckfall = rueckfall ?? "";
            Argumente = argumente == null ? Array.Empty<object>() : (object[])argumente.Clone();
        }

        /// <summary>Ein Baustein mit Schlüssel, deutschem Muster und Argumenten.</summary>
        public static Textbaustein T(string schluessel, string rueckfall, params object[] argumente)
            => new Textbaustein(schluessel, rueckfall, argumente);

        /// <summary>Ein Klartext ohne Schlüssel (sprachneutral oder fremd, etwa eine Fehlermeldung); wird nie formatiert.</summary>
        public static Textbaustein Klar(string text) => new Textbaustein(null, text ?? "");

        /// <summary>Trägt der Baustein weder Schlüssel noch Text?</summary>
        public bool IstLeer => Schluessel == null && Rueckfall.Length == 0;

        /// <summary>Der deutsche Text (Rückfallmuster, Zahlen de-DE) — unabhängig von der Kultur des Fadens.</summary>
        public string Klartext => Bilden(null, DE);

        /// <summary>Der Text in der Anzeigesprache des Fadens (<see cref="CultureInfo.CurrentUICulture"/>), Zahlen in <see cref="CultureInfo.CurrentCulture"/>.</summary>
        public string Aufloesen() => Bilden(CultureInfo.CurrentUICulture, CultureInfo.CurrentCulture);

        /// <summary>Der Text in der Sprache und mit den Zahlformaten von <paramref name="kultur"/> (etwa die Berichtssprache).</summary>
        public string Aufloesen(CultureInfo kultur)
        {
            CultureInfo k = kultur ?? CultureInfo.CurrentUICulture;
            return Bilden(k, k);
        }

        /// <summary>Löst einen Baustein auf; <c>null</c> ergibt den leeren Text.</summary>
        public static string Aufloesen(Textbaustein t, CultureInfo kultur = null)
            => t == null ? "" : kultur == null ? t.Aufloesen() : t.Aufloesen(kultur);

        /// <summary>Ist der Schlüssel in der Sprache auflösbar (Ressource vorhanden und nicht leer)?</summary>
        public static bool Aufloesbar(string schluessel, CultureInfo sprache)
        {
            if (string.IsNullOrEmpty(schluessel)) return false;
            try { return !string.IsNullOrEmpty(MyResource.Resource.ResourceManager.GetString(schluessel, sprache)); }
            catch (Exception) { return false; }
        }

        /// <summary>Alle Schlüssel des Bausteins samt seiner Argumente (für Prüfungen).</summary>
        public IEnumerable<string> AlleSchluessel()
        {
            if (Schluessel != null) yield return Schluessel;
            foreach (object a in Argumente)
                if (a is Textbaustein t)
                    foreach (string s in t.AlleSchluessel()) yield return s;
        }

        /// <summary>sprache <c>null</c> = nur das deutsche Rückfallmuster (Klartext).</summary>
        private string Bilden(CultureInfo sprache, CultureInfo zahlen)
        {
            string muster = null;
            if (sprache != null && Schluessel != null)
            {
                try { muster = MyResource.Resource.ResourceManager.GetString(Schluessel, sprache); }
                catch (Exception) { muster = null; }
            }
            if (string.IsNullOrEmpty(muster)) muster = Rueckfall;
            if (Argumente.Count == 0) return muster;
            object[] werte = Argumente.Select(a => (object)Wert(a, sprache, zahlen)).ToArray();
            try { return string.Format(zahlen, muster, werte); }
            catch (FormatException) { return muster; }
        }

        private static string Wert(object a, CultureInfo sprache, CultureInfo zahlen)
        {
            switch (a)
            {
                case null: return "";
                case Textbaustein t: return t.Bilden(sprache, zahlen);
                case double d: return d.ToString("#,##0.###", zahlen);
                case float f: return ((double)f).ToString("#,##0.###", zahlen);
                case decimal m: return m.ToString("#,##0.###", zahlen);
                case IFormattable fo: return fo.ToString(null, zahlen);
                default: return a.ToString();
            }
        }

        public bool Equals(Textbaustein other)
            => other != null && string.Equals(Schluessel, other.Schluessel, StringComparison.Ordinal)
                             && string.Equals(Klartext, other.Klartext, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as Textbaustein);

        public override int GetHashCode() => HashCode.Combine(Schluessel, Klartext);

        /// <summary>Der deutsche Klartext.</summary>
        public override string ToString() => Klartext;
    }
}
