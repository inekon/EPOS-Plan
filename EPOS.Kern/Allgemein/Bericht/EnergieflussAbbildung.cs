using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Abbildung der Energieflussbilanz auf die Bilddaten des Renderers</b> (Konzept Energieflussbild, Abschnitte
    /// 2.2, 3.1 und 3.4, Schritt SK-12): <see cref="EnergieflussBilanz"/> → <see cref="EnergieflussBildDaten"/>. Sie
    /// vergibt die Farbrolle und die Bandart jedes Bandes, übernimmt Spalte, Namen, Kurznamen, Jahressummen und
    /// Zeigertexte der Knoten und setzt die Spaltentitel in der Sprache der Bilanz. Sie rechnet nichts (jede Menge
    /// und jeder Text kommen aus der Bilanz) und zeichnet nichts (Lage, Maßstab und Beschriftung sind Sache von
    /// <c>ChartRenderer.EnergieflussModell</c>); die Hinweiszeilen bleiben an der Bilanz.
    ///
    /// <para><b>Die Farbregel (3.4):</b> Ein Band in einen Bus trägt die Rolle seiner Quelle (Netz, Photovoltaik,
    /// BHKW-Strom, Wärmepumpe, Kessel, Solar …), ein Band aus einem Bus die Rolle seiner Verwendung (Wärmepumpe,
    /// Heizstab, Kälteerzeuger, Strombedarf, Speicherladung, Einspeisung; aus dem Wärmebus die Kanalrollen Heizung,
    /// Warmwasser, Prozesswärme). Brennstoffbänder tragen <see cref="Farbrolle.BRENNSTOFF"/>, Umweltanteile und
    /// Kondensationswärme <see cref="Farbrolle.UMWELTWAERME"/>, Kälte <see cref="Farbrolle.KAELTE"/> (Split-Geräte
    /// <see cref="Farbrolle.KAELTE_DEZENTRAL"/>), Verluste <see cref="Farbrolle.VERLUST"/>, „ungedeckt“ und „nicht
    /// zugeordnet“ <see cref="Farbrolle.REST"/> mit gestricheltem Rand; die Füllstandsänderung eines Speichers ist
    /// gespeicherte Ladung (<see cref="Farbrolle.SPEICHERLADUNG"/>). Der Elektrokessel ist ein Kessel
    /// (<see cref="Farbrolle.WAERME_KESSEL"/>); der Name unterscheidet ihn.</para>
    /// </summary>
    public static class EnergieflussAbbildung
    {
        /// <summary>Spaltenzahl des Bildes (Tabelle 2.2).</summary>
        public const int SPALTEN = Energiefluss.SPALTE_SENKEN + 1;

        /// <summary>
        /// Die Bilddaten zur Bilanz: je Knoten ein Bildknoten, je Band ein Bildband, sieben Spaltentitel. Eine leere oder
        /// fehlende Bilanz ergibt leere Bilddaten (der Renderer zeichnet dann ein leeres Bild).
        /// </summary>
        public static EnergieflussBildDaten Bild(EnergieflussBilanz bilanz)
        {
            var daten = new EnergieflussBildDaten();
            if (bilanz == null) return daten;
            var knoten = new Dictionary<string, EnergieflussKnoten>(StringComparer.Ordinal);
            foreach (EnergieflussKnoten k in bilanz.Knoten)
                if (k != null && !string.IsNullOrEmpty(k.Id) && !knoten.ContainsKey(k.Id)) knoten[k.Id] = k;

            daten.Knoten = knoten.Values.Select(Bildknoten).ToList();
            daten.Baender = bilanz.Fluesse.Where(f => f != null)
                                  .Select(f => Bildband(f, knoten.TryGetValue(f.Von, out EnergieflussKnoten v) ? v : null,
                                                           knoten.TryGetValue(f.Nach, out EnergieflussKnoten n) ? n : null))
                                  .ToList();
            daten.Spaltentitel = Spaltentitel(bilanz);
            return daten;
        }

        /// <summary>
        /// Die sieben Spaltentitel in der Sprache der Bilanz: „Quellen“, „Wandler“, der Name des Strombusses, „Wandler“,
        /// die Namen der Busse Wärme und Kälte, die im Bild stehen („Wärme · Kälte“), „Speicher“, „Bedarf“.
        /// </summary>
        public static IReadOnlyList<string> Spaltentitel(EnergieflussBilanz bilanz)
        {
            bool en = bilanz?.Englisch ?? false;
            string Bus(string id, string schluessel)
            {
                EnergieflussKnoten k = bilanz?.KnotenMit(id);
                return k != null && !string.IsNullOrEmpty(k.Name) ? k.Name : Energiefluss.T(schluessel, en);
            }
            var busse = new List<string>();
            if (bilanz?.KnotenMit(Energiefluss.WAERME) != null) busse.Add(Bus(Energiefluss.WAERME, nameof(MyResource.Resource.EF_KN_WAERME)));
            if (bilanz?.KnotenMit(Energiefluss.KAELTE) != null) busse.Add(Bus(Energiefluss.KAELTE, nameof(MyResource.Resource.EF_KN_KAELTE)));
            if (busse.Count == 0)
                busse.Add(Bus(Energiefluss.WAERME, nameof(MyResource.Resource.EF_KN_WAERME)) + " · " + Bus(Energiefluss.KAELTE, nameof(MyResource.Resource.EF_KN_KAELTE)));
            var titel = new string[SPALTEN];
            titel[Energiefluss.SPALTE_QUELLEN] = Energiefluss.T(nameof(MyResource.Resource.EF_SP_QUELLEN), en);
            titel[Energiefluss.SPALTE_WANDLER_I] = Energiefluss.T(nameof(MyResource.Resource.EF_SP_WANDLER), en);
            titel[Energiefluss.SPALTE_STROM] = Bus(Energiefluss.STROM, nameof(MyResource.Resource.EF_KN_STROM));
            titel[Energiefluss.SPALTE_WANDLER_II] = Energiefluss.T(nameof(MyResource.Resource.EF_SP_WANDLER), en);
            titel[Energiefluss.SPALTE_BUSSE] = string.Join(" · ", busse);
            titel[Energiefluss.SPALTE_SPEICHER] = Energiefluss.T(nameof(MyResource.Resource.EF_SP_SPEICHER), en);
            titel[Energiefluss.SPALTE_SENKEN] = Energiefluss.T(nameof(MyResource.Resource.EF_SP_BEDARF), en);
            return titel;
        }

        // ---------------------------------------------------------------- Knoten

        private static EnergieflussBildKnoten Bildknoten(EnergieflussKnoten k)
        {
            bool verlust = k.Art == EnergieflussKnotenart.Verlust;
            return new EnergieflussBildKnoten
            {
                Kennung = k.Id,
                Spalte = Math.Max(Energiefluss.SPALTE_QUELLEN, Math.Min(Energiefluss.SPALTE_SENKEN, k.Spalte)),
                Name = k.Name ?? "",
                Kurzname = k.Kurzname ?? "",
                Mwh = Jahressumme(k),
                Rolle = verlust ? Farbrolle.VERLUST : Farbrolle.KNOTEN,
                Art = k.Art,
                Zeigertext = string.IsNullOrEmpty(k.Zeigertext) ? null : k.Zeigertext,
            };
        }

        /// <summary>Die Jahressumme, die der Bericht am Knoten nennt: Quellen ihren Abfluss, alle anderen ihren Zufluss.</summary>
        public static double Jahressumme(EnergieflussKnoten k)
            => k.Art == EnergieflussKnotenart.Quelle ? k.AbflussMwh : k.ZuflussMwh;

        // ---------------------------------------------------------------- Bänder

        private static EnergieflussBildBand Bildband(EnergieflussFluss f, EnergieflussKnoten von, EnergieflussKnoten nach)
        {
            EnergieflussBandart art = Bandart(f);
            return new EnergieflussBildBand
            {
                Von = f.Von ?? "",
                Nach = f.Nach ?? "",
                Mwh = f.Mwh,
                Rolle = Rolle(f, von, nach),
                Art = art,
                Zeigertext = string.IsNullOrEmpty(f.Zeigertext) ? null : f.Zeigertext,
            };
        }

        /// <summary>Verlustbänder sind Verlust, „ungedeckt“ und „nicht zugeordnet“ gestrichelt; alles andere ein gewöhnliches Band.</summary>
        public static EnergieflussBandart Bandart(EnergieflussFluss f)
            => f.Form == EnergieflussForm.Verlust ? EnergieflussBandart.Verlust
             : f.Form == EnergieflussForm.Rest ? EnergieflussBandart.NichtZugeordnet
             : EnergieflussBandart.Normal;

        /// <summary>Die Farbrolle eines Bandes nach der Regel 3.4 (Klassenkommentar).</summary>
        public static Farbrolle Rolle(EnergieflussFluss f, EnergieflussKnoten von, EnergieflussKnoten nach)
        {
            switch (f.Form)
            {
                case EnergieflussForm.Verlust: return Farbrolle.VERLUST;
                case EnergieflussForm.Rest: return Farbrolle.REST;
                case EnergieflussForm.Brennstoff: return Farbrolle.BRENNSTOFF;
                case EnergieflussForm.Umwelt: return Farbrolle.UMWELTWAERME;
            }
            string vonId = f.Von ?? "", nachId = f.Nach ?? "";
            if (nachId.StartsWith(Energiefluss.FUELLSTAND, StringComparison.Ordinal)) return Farbrolle.SPEICHERLADUNG;
            if (von != null && von.Art == EnergieflussKnotenart.Bus) return Verwendung(nachId, f.Form);
            if (nach != null && nach.Art == EnergieflussKnotenart.Bus) return Quelle(vonId, nachId, f.Form);
            if (von != null && von.Art == EnergieflussKnotenart.Speicher)
                return IstStromspeicher(vonId) ? Farbrolle.STROM_SPEICHER : Verwendung(nachId, f.Form);
            if (von != null && von.Art == EnergieflussKnotenart.Quelle) return Quelle(vonId, nachId, f.Form);
            return NachForm(f.Form);
        }

        /// <summary>Rolle eines Bandes in einen Bus bzw. aus einer Quelle: die seiner Quelle.</summary>
        private static Farbrolle Quelle(string von, string nach, EnergieflussForm form)
        {
            if (von == Energiefluss.NETZBEZUG || von == Energiefluss.KUEHLSTROM_EIGEN) return Farbrolle.STROM_NETZ;
            if (von == Energiefluss.PHOTOVOLTAIK) return Farbrolle.STROM_PV;
            if (von == Energiefluss.BHKW) return nach == Energiefluss.STROM ? Farbrolle.STROM_BHKW : Farbrolle.WAERME_BHKW;
            if (von == Energiefluss.HEIZKESSEL || von == Energiefluss.ELEKTROKESSEL) return Farbrolle.WAERME_KESSEL;
            if (von == Energiefluss.WP_HEIZEN) return Farbrolle.WAERME_WP;
            if (von == Energiefluss.HEIZSTAB) return Farbrolle.HEIZSTAB;
            if (von == Energiefluss.SOLARTHERMIE) return Farbrolle.WAERME_SOLAR;
            if (IstSplit(von)) return Farbrolle.KAELTE_DEZENTRAL;
            if (von == Energiefluss.WP_KUEHLEN || von == Energiefluss.FREIE_KUEHLUNG
                || von.StartsWith(Energiefluss.KAELTEMASCHINE, StringComparison.Ordinal)) return Farbrolle.KAELTE;
            return NachForm(form);
        }

        /// <summary>Rolle eines Bandes aus einem Bus oder Speicher: die seiner Verwendung.</summary>
        private static Farbrolle Verwendung(string nach, EnergieflussForm form)
        {
            if (nach == Energiefluss.WP_HEIZEN) return Farbrolle.WAERME_WP;
            if (nach == Energiefluss.HEIZSTAB) return Farbrolle.HEIZSTAB;
            if (nach == Energiefluss.ELEKTROKESSEL) return Farbrolle.WAERME_KESSEL;
            if (IstSplit(nach)) return Farbrolle.KAELTE_DEZENTRAL;
            if (nach == Energiefluss.WP_KUEHLEN || nach == Energiefluss.FREIE_KUEHLUNG || nach == Energiefluss.KUEHLUNG
                || nach.StartsWith(Energiefluss.KAELTEMASCHINE, StringComparison.Ordinal)) return Farbrolle.KAELTE;
            if (nach == Energiefluss.STROMBEDARF) return Farbrolle.VERBRAUCH_GESAMT;
            if (nach == Energiefluss.EINSPEISUNG_PV || nach == Energiefluss.EINSPEISUNG_BHKW) return Farbrolle.UEBERSCHUSS;
            if (IstSpeicher(nach)) return Farbrolle.SPEICHERLADUNG;
            if (nach == Energiefluss.HEIZUNG) return Farbrolle.HEIZWAERME;
            if (nach == Energiefluss.WARMWASSER) return Farbrolle.WARMWASSER;
            if (nach == Energiefluss.PROZESSWAERME) return Farbrolle.PROZESSWAERME;
            return NachForm(form);
        }

        /// <summary>Rückfall für einen unbekannten Knoten: die Energieform des Bandes.</summary>
        private static Farbrolle NachForm(EnergieflussForm form)
            => form == EnergieflussForm.Kaelte ? Farbrolle.KAELTE
             : form == EnergieflussForm.Strom ? Farbrolle.STROM_NETZ
             : Farbrolle.HEIZWAERME;

        private static bool IstSplit(string id)
            => id.StartsWith(Energiefluss.SPLIT, StringComparison.Ordinal) || id == Energiefluss.WEITERE + Energiefluss.GRUPPE_SPLIT;

        private static bool IstStromspeicher(string id)
            => id.StartsWith(Energiefluss.STROMSPEICHER, StringComparison.Ordinal);

        private static bool IstSpeicher(string id)
            => id.StartsWith(Energiefluss.PUFFER, StringComparison.Ordinal)
            || id.StartsWith(Energiefluss.KAELTESPEICHER, StringComparison.Ordinal)
            || IstStromspeicher(id)
            || id == Energiefluss.WEITERE + Energiefluss.GRUPPE_PUFFER;
    }
}
