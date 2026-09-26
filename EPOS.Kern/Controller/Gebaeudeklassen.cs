using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Baualtersklassen A bis M</b> (Entscheid E47, Konzept Baualtersklassen 3.1) — Liste, Texte,
    /// Quelle und die Regel „das Baujahr führt", ohne Datenbank. Eine Klasse ist ein BAUZEITRAUM, für
    /// Wohn- und Nichtwohngebäude gleich; die Grenzen und Buchstaben A bis L folgen der Deutschen
    /// Wohngebäudetypologie des IWU (2015), das Ende von L (2020) und die Klasse M ab 2021 dem
    /// Typgebäudemodell von Stein/Loga (2025). Die Jahresgrenzen selbst stehen bei
    /// <see cref="Baujahrregel.OBERGRENZEN"/>.
    ///
    /// <para><b>Drei Schichten:</b> Der BUCHSTABE geht in die Datenbank (<c>Baualtersklasse</c>), der
    /// Listenplatz in die Klappliste (0 = A … 12 = M), der TEXT nur auf den Bildschirm (Ressourcen
    /// <c>GEB_BAK_A</c> … <c>GEB_BAK_M</c> mit deutschem Rückfall). Ginge die Abbildung über den Text,
    /// verschöbe eine Übersetzung die gespeicherte Klasse.</para>
    ///
    /// <para>Die Klasse ist öffentlich, weil Oberfläche (Arbeitsstand von Katalogeditor und
    /// Gebäudeverwaltung) und Bericht sie ohne Controller brauchen; <c>GebaeudeStammCtrl</c> leitet
    /// seine Bestandsnamen hierher weiter.</para>
    /// </summary>
    public static class Gebaeudeklassen
    {
        /// <summary>Die deutschen Texte der 13 Klassen — der Rückfall, solange ein Schlüssel fehlt.</summary>
        public static readonly IReadOnlyList<string> TEXTE_DE = new[]
        {
            "bis 1859", "1860 bis 1918", "1919 bis 1948", "1949 bis 1957", "1958 bis 1968",
            "1969 bis 1978", "1979 bis 1983", "1984 bis 1994", "1995 bis 2001", "2002 bis 2009",
            "2010 bis 2015", "2016 bis 2020", "ab 2021",
        };

        /// <summary>Die Zahl der Klassen (13).</summary>
        public static int Anzahl => TEXTE_DE.Count;

        /// <summary>Der Ressourcenschlüssel der Quellenangabe unter der Klappliste der Klasse.</summary>
        public const string SCHLUESSEL_QUELLE = "GEB_BAK_QUELLE";

        /// <summary>Der Ressourcenschlüssel der Zeile „die Klasse folgt aus dem Baujahr {0}".</summary>
        public const string SCHLUESSEL_AUS_BAUJAHR = "GEB_BAK_AUS_BAUJAHR";

        /// <summary>Die Texte der 13 Klassen in der Sprache der Oberfläche, Index 0 = A.</summary>
        public static IReadOnlyList<string> Texte()
        {
            var liste = new List<string>(TEXTE_DE.Count);
            for (int i = 0; i < TEXTE_DE.Count; i++)
                liste.Add(TextAmPlatz(i));
            return liste;
        }

        /// <summary>
        /// Der Buchstabe zu einem Listenplatz: <c>'A' + index</c>. Ein Platz außerhalb der Liste fällt auf
        /// <c>'A'</c> zurück — genauso wie der Rückweg <see cref="Index"/>.
        /// </summary>
        public static char Buchstabe(int index)
            => index < 0 || index >= TEXTE_DE.Count ? 'A' : (char)('A' + index);

        /// <summary>
        /// Der Listenplatz zu einer gespeicherten Klasse — das erste Zeichen minus <c>'A'</c>; leer, negativ
        /// und außerhalb der Liste wird 0 (Bestandsregel der Klappliste).
        /// </summary>
        public static int Index(string baualtersklasse)
        {
            if (string.IsNullOrEmpty(baualtersklasse)) return 0;
            int index = baualtersklasse[0] - 'A';
            return index < 0 || index >= TEXTE_DE.Count ? 0 : index;
        }

        /// <summary>
        /// Der KLARTEXT einer gespeicherten Klasse (Bericht, Wohnflächenangabe, Katalogliste): leer bleibt
        /// leer, ein Buchstabe außerhalb A…M ebenso — anders als <see cref="Index"/> fällt hier nichts auf
        /// die erste Klasse zurück.
        /// </summary>
        public static string Text(string baualtersklasse)
        {
            if (string.IsNullOrEmpty(baualtersklasse)) return "";
            int index = char.ToUpperInvariant(baualtersklasse[0]) - 'A';
            return index >= 0 && index < TEXTE_DE.Count ? TextAmPlatz(index) : "";
        }

        /// <summary>
        /// Der KLARTEXT einer gespeicherten Klasse in der Kultur <paramref name="kultur"/> — für den Bericht, der seine
        /// Sprache übergeben bekommt (Kapitel „Projekt“ und <c>{{gebaeude.baualtersklasse}}</c>); sonst wie
        /// <see cref="Text(string)"/>. <c>null</c> = die Sprache der Oberfläche.
        /// </summary>
        public static string Text(string baualtersklasse, CultureInfo kultur)
        {
            if (string.IsNullOrEmpty(baualtersklasse)) return "";
            int index = char.ToUpperInvariant(baualtersklasse[0]) - 'A';
            return index >= 0 && index < TEXTE_DE.Count ? Ressource("GEB_BAK_" + (char)('A' + index), TEXTE_DE[index], kultur) : "";
        }

        /// <summary>
        /// DAS BAUJAHR FÜHRT (F2): der Listenplatz der Klasse, die aus dem Baujahr folgt
        /// (<see cref="Baujahrregel.KlassenIndex"/>, jedes Jahr 1500…2100); <c>null</c> ohne Baujahr oder
        /// außerhalb des Bereichs — dann ist die Klasse wählbar.
        /// </summary>
        public static int? IndexAusBaujahr(int? baujahr) => Baujahrregel.KlassenIndex(baujahr);

        /// <summary>
        /// Die WIRKSAME Klasse als Listenplatz: die aus dem Baujahr, sonst die gewählte
        /// <paramref name="gewaehlt"/> — so speichern Katalogeditor und Gebäudeverwaltung.
        /// </summary>
        public static int IndexWirksam(int? baujahr, int gewaehlt) => IndexAusBaujahr(baujahr) ?? gewaehlt;

        /// <summary>
        /// Die QUELLE der Einteilung — die Herleitungszeile unter der Klappliste (Konzept Baualtersklassen,
        /// Abschnitt 2: übernommen werden nur die Jahresgrenzen, die Quelle wird genannt).
        /// </summary>
        public static string Quelle()
            => Ressource(SCHLUESSEL_QUELLE,
                         "Einteilung nach der Deutschen Wohngebäudetypologie des IWU (2015), ab 2016 nach Stein/Loga (2025).");

        /// <summary>
        /// Die Zeile, wenn das Baujahr die Klasse führt: „Die Klasse folgt aus dem Baujahr {0}; ohne Baujahr
        /// ist sie wählbar." Das Jahr steht ohne Tausendertrennzeichen.
        /// </summary>
        public static string AusBaujahrText(int baujahr)
            => string.Format(CultureInfo.CurrentCulture,
                             Ressource(SCHLUESSEL_AUS_BAUJAHR, "Die Klasse folgt aus dem Baujahr {0}; ohne Baujahr ist sie wählbar."),
                             baujahr.ToString(CultureInfo.InvariantCulture));

        private static string TextAmPlatz(int index)
            => Ressource("GEB_BAK_" + (char)('A' + index), TEXTE_DE[index]);

        private static string Ressource(string schluessel, string rueckfall, CultureInfo kultur = null)
        {
            string text = null;
            try { text = kultur == null ? MyResource.Resource.ResourceManager.GetString(schluessel)
                                        : MyResource.Resource.ResourceManager.GetString(schluessel, kultur); }
            catch { }
            return string.IsNullOrEmpty(text) ? rueckfall : text;
        }
    }
}
