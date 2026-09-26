using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Energiestandard eines Gebäudes</b> (Entscheid E47, Konzept Baualtersklassen 3.2) — ein
    /// eigenes, freiwilliges Feld neben der Baualtersklasse. Die Klasse sagt, WANN gebaut wurde; der
    /// Standard sagt, WIE GUT die Hülle ist, wenn sie besser ist als der gesetzliche Mindeststandard zum
    /// Baujahr (das ist die Klasse selbst und darum kein eigener Eintrag).
    ///
    /// <para><b>Drei Schichten:</b> Gespeichert wird der sprachneutrale CODE (Spalte
    /// <c>Energiestandard</c>, Schemaschritt <see cref="BaualtersklassenSchema.SCHRITT"/>, <c>CHECK</c>
    /// auf <see cref="CODES"/>), angezeigt der Text aus den Ressourcen (<c>GEB_ES_&lt;Code&gt;</c>) mit
    /// deutschem Rückfall. Leer (NULL) heißt „keiner" — das Gebäude entspricht seiner Klasse.</para>
    ///
    /// <para><b>Wohnen und Nichtwohnen:</b> Effizienzhaus 115/100 und 85 gibt es nur für
    /// Wohngebäude (KfW-Merkblätter 261 und 263); die Klappliste zeigt sie nur, wenn die Verwendung
    /// <see cref="GebaeudeStammCtrl.FILTERWERT_WOHN"/> ist (<see cref="Codes(string)"/>). Kein
    /// Rechenweg liest den Standard; er steuert allein die Vorgaben des Imports
    /// (<see cref="GebaeudeVorgaben"/>).</para>
    /// </summary>
    public static class Energiestandard
    {
        /// <summary>teilsaniert — keine amtliche Stufe.</summary>
        public const string TEILSANIERT = "TEILSANIERT";

        /// <summary>saniert nach den Bauteilanforderungen des Gebäudemodernisierungsgesetzes.</summary>
        public const string SANIERT = "SANIERT";

        /// <summary>Niedrigenergiehaus — historischer Begriff.</summary>
        public const string NIEDRIGENERGIE = "NIEDRIGENERGIE";

        /// <summary>Effizienzhaus 115/100 (bis 2022) — nur Wohngebäude, historisch.</summary>
        public const string EH115_100 = "EH115_100";

        /// <summary>Effizienzhaus 85 — nur Wohngebäude.</summary>
        public const string EH85 = "EH85";

        /// <summary>Effizienzhaus bzw. Effizienzgebäude 70.</summary>
        public const string EH70 = "EH70";

        /// <summary>Effizienzhaus bzw. Effizienzgebäude 55.</summary>
        public const string EH55 = "EH55";

        /// <summary>Effizienzhaus bzw. Effizienzgebäude 40.</summary>
        public const string EH40 = "EH40";

        /// <summary>Effizienzhaus bzw. Effizienzgebäude Denkmal.</summary>
        public const string DENKMAL = "DENKMAL";

        /// <summary>Passivhaus bzw. EnerPHit (Passivhaus Institut).</summary>
        public const string PASSIVHAUS = "PASSIVHAUS";

        /// <summary>Nullemissionsgebäude (Gebäudemodernisierungsgesetz, Anforderungswerte offen).</summary>
        public const string NULLEMISSION = "NULLEMISSION";

        /// <summary>Die elf Codes in der Reihenfolge der Klappliste — die Menge des <c>CHECK</c>.</summary>
        public static readonly IReadOnlyList<string> CODES = new[]
        {
            TEILSANIERT, SANIERT, NIEDRIGENERGIE, EH115_100, EH85, EH70, EH55, EH40, DENKMAL, PASSIVHAUS, NULLEMISSION,
        };

        /// <summary>Die Codes, die es nur für Wohngebäude gibt.</summary>
        public static readonly IReadOnlyList<string> NUR_WOHNGEBAEUDE = new[] { EH115_100, EH85 };

        /// <summary>Das Präfix der Ressourcenschlüssel: <c>GEB_ES_</c> + Code.</summary>
        public const string SCHLUESSEL_PRAEFIX = "GEB_ES_";

        /// <summary>Der Ressourcenschlüssel des leeren Eintrags („wie Baualtersklasse").</summary>
        public const string SCHLUESSEL_KEINER = "GEB_ES_KEINER";

        /// <summary>Die deutschen Texte in der Reihenfolge von <see cref="CODES"/> — der Rückfall, solange ein Schlüssel fehlt.</summary>
        private static readonly string[] TEXTE_DE =
        {
            "teilsaniert", "saniert nach den Bauteilanforderungen des GModG", "Niedrigenergiehaus",
            "Effizienzhaus 115/100 (bis 2022)", "Effizienzhaus 85", "Effizienzhaus/-gebäude 70",
            "Effizienzhaus/-gebäude 55", "Effizienzhaus/-gebäude 40", "Effizienzhaus/-gebäude Denkmal",
            "Passivhaus bzw. EnerPHit", "Nullemissionsgebäude",
        };

        /// <summary>Der deutsche Rückfall des leeren Eintrags.</summary>
        private const string KEINER_DE = "wie Baualtersklasse (unsaniert)";

        /// <summary>Ist <paramref name="code"/> einer der elf Codes? Leer und unbekannt: nein.</summary>
        public static bool Gueltig(string code) => Index(code) >= 0;

        /// <summary>Der Listenplatz eines Codes in <see cref="CODES"/>; -1 für leer oder unbekannt.</summary>
        public static int Index(string code)
        {
            if (string.IsNullOrEmpty(code)) return -1;
            for (int i = 0; i < CODES.Count; i++)
                if (string.Equals(CODES[i], code, StringComparison.Ordinal)) return i;
            return -1;
        }

        /// <summary>Der Code zu einem Listenplatz; <c>null</c> ohne Platz oder außerhalb der Liste.</summary>
        public static string Code(int? index)
            => index is int i && i >= 0 && i < CODES.Count ? CODES[i] : null;

        /// <summary>Gibt es den Standard nur für Wohngebäude (Effizienzhaus 115/100 und 85)?</summary>
        public static bool NurWohngebaeude(string code)
        {
            foreach (string c in NUR_WOHNGEBAEUDE)
                if (string.Equals(c, code, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>
        /// Passt der Standard zur Verwendung (Steuerwert der Spalte <c>Wohngebaeude_Nicht_Wohngebaeude</c>)?
        /// Leer passt immer; ein Standard nur für Wohngebäude passt zu einem Nichtwohngebäude nicht. Eine
        /// leere Verwendung gilt als Wohngebäude — so legt der Katalog einen neuen Satz an.
        /// </summary>
        public static bool PasstZu(string code, string verwendung)
        {
            if (string.IsNullOrEmpty(code) || !NurWohngebaeude(code)) return true;
            return string.IsNullOrEmpty(verwendung) ||
                   string.Equals(verwendung, GebaeudeStammCtrl.FILTERWERT_WOHN, StringComparison.Ordinal);
        }

        /// <summary>Die Codes, die zur Verwendung passen, in der Reihenfolge von <see cref="CODES"/>.</summary>
        public static IReadOnlyList<string> Codes(string verwendung)
        {
            var liste = new List<string>(CODES.Count);
            foreach (string c in CODES)
                if (PasstZu(c, verwendung)) liste.Add(c);
            return liste;
        }

        /// <summary>
        /// Der Anzeigetext eines Codes (Ressource <c>GEB_ES_&lt;Code&gt;</c>, sonst der deutsche Rückfall);
        /// leer bleibt leer, ein unbekannter Code erscheint unverändert (ein Datenfehler bleibt sichtbar).
        /// </summary>
        public static string Text(string code)
        {
            int i = Index(code);
            if (i < 0) return code ?? "";
            return Ressource(SCHLUESSEL_PRAEFIX + CODES[i], TEXTE_DE[i]);
        }

        /// <summary>Der Text des leeren Eintrags — „wie Baualtersklasse (unsaniert)".</summary>
        public static string KeinerText() => Ressource(SCHLUESSEL_KEINER, KEINER_DE);

        /// <summary>Der deutsche Rückfalltext eines Codes (für Wachen und Glossar); leer für unbekannt.</summary>
        public static string TextDeutsch(string code)
        {
            int i = Index(code);
            return i < 0 ? "" : TEXTE_DE[i];
        }

        private static string Ressource(string schluessel, string rueckfall)
        {
            string text = null;
            try { text = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(text) ? rueckfall : text;
        }
    }
}
