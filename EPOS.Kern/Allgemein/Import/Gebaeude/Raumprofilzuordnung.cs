using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Der Stand der Profilzuordnung eines Raums (Entwurf ZZ 5.3, Anwenderentscheide F2 und F3 vom 17.10.2026).</summary>
    internal enum Raumprofilstand
    {
        /// <summary>Genau ein Kandidat ohne eigene Nutzungsangabe der Datei — wird automatisch gesetzt.</summary>
        Eindeutig,
        /// <summary>Ein Vorschlag, den der Anwender bestätigt: eigene Bezeichnung der Datei (F2), Hallenprofil 23 (F5), Wohnprofil nach Küchenzahl (F4).</summary>
        Vorschlag,
        /// <summary>Mehrere Kandidaten ohne Vorschlag — Rückfrage mit den Kandidaten als Auswahl (F3).</summary>
        Offen,
        /// <summary>Kein Kandidat — Rückfrage mit dem ganzen Katalogbaum.</summary>
        Ohne,
        /// <summary>Von Hand gewählt oder bestätigt (Zuordnungsdialog ZZ-B).</summary>
        Hand,
    }

    /// <summary>Die Stufe der Rangfolge, aus der ein Glied der Begründungskette stammt.</summary>
    internal enum Raumprofilstufe
    {
        /// <summary>Stufe 0: die eigene Bezeichnung der Datei (DIN-Nummer, Profilname, Beschreibung).</summary>
        Datei,
        /// <summary>Stufe 1: die Raumart der Datei (<c>RoomType</c>) über Zuordnungszeile oder Kerntafel.</summary>
        Raumart,
        /// <summary>Stufe 2: der Raumname (Stamm ohne Nummer) über die Stichworttafel.</summary>
        Name,
        /// <summary>Stufe 3: die Gebäudeart (Wohn-/Nichtwohngebäude, Wohneinheiten).</summary>
        Gebaeudeart,
        /// <summary>Die Wahl oder Bestätigung von Hand.</summary>
        Hand,
    }

    /// <summary>Ein Glied der Begründungskette: die Stufe und ihr Wert (Code, Stamm, Prozent) — sprachneutral.</summary>
    internal sealed record Raumprofilgrund(Raumprofilstufe Stufe, string Wert);

    /// <summary>
    /// <b>Die Profilschlüssel der Raumzuordnung</b>: die Nummer eines DIN/TS-18599-10-Profils als Text („1“ … „43“), eines der
    /// zwei Wohnprofile ohne Nummer (<see cref="WOHNEN_EFH"/>, <see cref="WOHNEN_MFH"/>) oder ein Katalogprofil über seinen
    /// Auswahlschlüssel <c>#&lt;Id&gt;</c> (eigene Bezeichnung der Datei, Zuordnungszeile, Wahl von Hand). Aufgelöst wird über
    /// den Katalog (<see cref="Profil"/>): die Nummer über <see cref="Raumnutzungsvorbelegung.DinTreffer"/>, das Wohnprofil
    /// über seinen Namen, ohne Katalog die alte Kennung ohne Id.
    /// </summary>
    internal static class Raumprofilschluessel
    {
        /// <summary>Wohngebäude – Einfamilienhaus (Tabelle 5 der Norm, ohne Nummer).</summary>
        internal const string WOHNEN_EFH = "EFH";

        /// <summary>Wohngebäude – Mehrfamilienhaus (Tabelle 5 der Norm, ohne Nummer).</summary>
        internal const string WOHNEN_MFH = "MFH";

        /// <summary>Der Platzhalter „Wohnprofil“ der Tafeln, den Stufe 3 nach der Gebäudeart auflöst oder streicht.</summary>
        internal const string WOHNEN = "W";

        /// <summary>Die Zeilen 70 (Mehrfamilienhaus) und 71 (Einfamilienhaus) der Projektdatei (<see cref="Din18599Nutzung"/>).</summary>
        internal const int PROJEKTDATEI_MFH = 70, PROJEKTDATEI_EFH = 71;

        /// <summary>Der Schlüssel einer DIN-Nummer; 70 und 71 der Projektdatei werden zu den Wohnprofilen.</summary>
        internal static string Din(int nummer)
            => nummer == PROJEKTDATEI_MFH ? WOHNEN_MFH : nummer == PROJEKTDATEI_EFH ? WOHNEN_EFH : nummer.ToString(CultureInfo.InvariantCulture);

        /// <summary>Die DIN-Nummer eines Schlüssels; <c>null</c> für Wohnprofile, Katalogschlüssel und Platzhalter.</summary>
        internal static int? DinNummer(string schluessel)
            => int.TryParse(schluessel, NumberStyles.None, CultureInfo.InvariantCulture, out int n) ? n : null;

        /// <summary>Ist der Schlüssel eines der zwei Wohnprofile?</summary>
        internal static bool IstWohnen(string s) => s == WOHNEN_EFH || s == WOHNEN_MFH;

        /// <summary>Der Katalogname eines Wohnprofils; <c>null</c> für jeden anderen Schlüssel.</summary>
        internal static string Wohnname(string s)
            => s == WOHNEN_EFH ? RaumnutzungNormkategorienSchema.WOHNEN_EFH : s == WOHNEN_MFH ? RaumnutzungNormkategorienSchema.WOHNEN_MFH : null;

        /// <summary>
        /// <b>Das Profil eines Schlüssels</b>: DIN-Nummer → <see cref="Raumnutzungsvorbelegung.DinTreffer"/>; Wohnprofil → das
        /// Katalogprofil dieses Namens, ohne Katalog die Kennung Wohnen; <c>#Id</c> → dieses Profil. <c>null</c> ohne Treffer.
        /// </summary>
        internal static Planprofil Profil(Raumnutzungsvorbelegung v, string schluessel)
        {
            v ??= Raumnutzungsvorbelegung.Vorgabe;
            if (string.IsNullOrEmpty(schluessel) || schluessel == WOHNEN) return null;
            if (DinNummer(schluessel) is int n) return v.DinTreffer(n);
            if (Wohnname(schluessel) is string name)
            {
                Planprofil p = v.AusText(name);
                if (p != null && !p.NichtImKatalog) return p;
                return v.MitKatalog ? null : v.AusKennung(DbWerte.KOND_NUTZUNG_WOHNEN);
            }
            if (schluessel.StartsWith(Planprofil.SCHLUESSEL_PRAEFIX, StringComparison.Ordinal))
                return v.AusText(schluessel) is { NichtImKatalog: false } k ? k : null;
            return null;
        }

        /// <summary>
        /// <b>Der Schlüssel eines Katalogprofils</b>: die DIN-Nummer, das Wohnprofil oder <c>#Id</c>; <c>null</c> ohne Profil
        /// oder für ein Profil ohne Id (alte Kennung).
        /// </summary>
        internal static string AusProfil(Raumnutzungsvorbelegung v, Planprofil p)
        {
            if (p?.Id is not long) return null;
            v ??= Raumnutzungsvorbelegung.Vorgabe;
            if (v.DinNummer(p.Schluessel) is int n) return Din(n);
            if (string.Equals(p.Name?.Trim(), RaumnutzungNormkategorienSchema.WOHNEN_EFH, StringComparison.OrdinalIgnoreCase)) return WOHNEN_EFH;
            if (string.Equals(p.Name?.Trim(), RaumnutzungNormkategorienSchema.WOHNEN_MFH, StringComparison.OrdinalIgnoreCase)) return WOHNEN_MFH;
            return p.Schluessel;
        }

        /// <summary>Der Anzeigename eines Schlüssels: der Profilname des Katalogs, sonst die Nummer bzw. der Wohnname.</summary>
        internal static string Anzeigename(Raumnutzungsvorbelegung v, string schluessel)
        {
            if (Profil(v, schluessel) is { Name: { Length: > 0 } } p && p.Id.HasValue) return p.Name;
            if (Wohnname(schluessel) is string w) return w;
            if (DinNummer(schluessel) is int n) return Raumprofilzuordnung.Ressource("RNP_IMP_PROFIL_NUMMER", n.ToString(CultureInfo.InvariantCulture));
            return schluessel ?? "";
        }
    }

    /// <summary>
    /// <b>Die Raumart-Tafel des Kerns</b> (Entwurf ZZ 5.3, Stufe 1): der Code der Raumart (<c>HSETU_RaumAllgemein.RoomType</c>
    /// ohne Vorsatz <c>mrt</c>, Groß-/Kleinschreibung egal) → Kandidaten als Profilschlüssel. Ein Code mit einem Kandidaten ist
    /// eindeutig; mehrere Kandidaten bleiben offen (F3: keine Flächenschwellen), es sei denn, eine Stufe schärft oder ein
    /// Vorschlag ist festgelegt (<see cref="Raumprofilzuordnung.VORSCHLAG_JE_KANDIDATENMENGE"/>). Der Platzhalter
    /// <see cref="Raumprofilschluessel.WOHNEN"/> steht für das Wohnprofil im Wohngebäude; im Nichtwohngebäude bleibt der
    /// Nichtwohn-Kandidat daneben. Eine Zuordnungszeile <c>HOTTCAD_RAUMTYP</c> des Katalogs geht der Tafel vor
    /// (<see cref="Raumprofilzuordnung.Zuordnen"/>).
    /// </summary>
    internal static class Raumarttafel
    {
        private const string W = Raumprofilschluessel.WOHNEN;

        /// <summary>Code → Kandidaten in Normreihenfolge.</summary>
        internal static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> KANDIDATEN =
            new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                // Büro und Verwaltung
                ["Office"] = new[] { "1", "2", "3" },
                ["Conference"] = new[] { "4" },
                // Beherbergung
                ["Guests"] = new[] { W, "11" },
                ["Sleeping"] = new[] { W, "11" },
                // Gastronomie und Küche
                ["Eating"] = new[] { W, "12", "13" },
                ["Restaurant"] = new[] { "13" },
                ["Kitchen"] = new[] { W, "14", "15" },
                // Sanitär, Aufenthalt, Neben- und Verkehrsflächen
                ["WC"] = new[] { W, "16" },
                ["Bath"] = new[] { W, "16" },
                ["Shower"] = new[] { W, "16" },
                ["Locker"] = new[] { "16" },
                ["Living"] = new[] { W, "17" },
                ["Child"] = new[] { W, "17" },
                ["AdjoiningRoom"] = new[] { "18" },
                ["Hall"] = new[] { W, "19" },
                ["HallWay"] = new[] { W, "19" },
                ["Ante"] = new[] { W, "19" },
                ["Stairway"] = new[] { "19" },
                // Lager und Technik
                ["Store"] = new[] { "20" },
                ["StorageRoom"] = new[] { "20" },
                ["Basement"] = new[] { "20" },
                ["Roof"] = new[] { "20" },
                ["CentralHeating"] = new[] { "20" },
                ["Connection"] = new[] { "20" },
                // Hallen
                ["Workshop"] = new[] { "22", "23", "24" },
                // Sport und Wellness, Gesundheit
                ["Sauna"] = new[] { "36" },
                ["Fitness"] = new[] { "37" },
                ["Examination"] = new[] { "39" },
            };

        /// <summary>Die Codes, deren Räume im Wohngebäude als Wohnfläche zählen (Stufe 3, Wohnanteil).</summary>
        internal static readonly IReadOnlyCollection<string> WOHNCODES =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Living", "Child", "Sleeping", "Guests" };

        /// <summary>Die Codes einer Küche (Zählung der Wohneinheiten, F4).</summary>
        internal static readonly IReadOnlyCollection<string> KUECHENCODES = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Kitchen" };

        /// <summary>Der Code ohne Vorsatz <c>mrt</c>; leer ohne Raumart.</summary>
        internal static string Code(string raumtyp)
        {
            string t = (raumtyp ?? "").Trim();
            if (t.Length > 3 && t.StartsWith("mrt", StringComparison.Ordinal)) t = t.Substring(3);
            return t;
        }

        /// <summary>Die Kandidaten eines Codes; leer für einen unbekannten oder leeren Code.</summary>
        internal static IReadOnlyList<string> Kandidaten(string code)
            => code.Length > 0 && KANDIDATEN.TryGetValue(code, out IReadOnlyList<string> k) ? k : Array.Empty<string>();
    }

    /// <summary>
    /// <b>Die Stichworttafel der Raumnamen</b> (Entwurf ZZ 5.3, Stufe 2): ein Teilwort des Namensstamms (klein, ohne
    /// angehängte Nummer; deutsch und englisch) → Kandidaten. Sie schärft die Kandidaten der Raumart (Schnittmenge) oder
    /// setzt sie, wenn die Raumart fehlt oder nichts Gemeinsames hat (der Name ist die genauere Bezeichnung). Die erste Zeile
    /// mit Treffer gilt — spezifische Wörter stehen vor allgemeinen.
    /// </summary>
    internal static class Raumnamentafel
    {
        private const string W = Raumprofilschluessel.WOHNEN;

        /// <summary>Die Zeilen in Prüfreihenfolge.</summary>
        internal static readonly IReadOnlyList<(string[] Muster, string[] Kandidaten)> ZEILEN = new (string[], string[])[]
        {
            // Büro und Verwaltung
            (new[] { "einzelbüro", "einzelbuero", "chefbüro", "chefbuero", "private office" }, new[] { "1" }),
            (new[] { "gruppenbüro", "gruppenbuero" }, new[] { "2" }),
            (new[] { "großraum", "grossraum", "open plan", "open-plan" }, new[] { "3" }),
            (new[] { "besprech", "sitzung", "konferenz", "seminar", "meeting", "conference" }, new[] { "4" }),
            (new[] { "empfang", "schalter", "reception", "counter" }, new[] { "5" }),
            // Handel
            (new[] { "verkauf", "laden", "shop", "sales" }, new[] { "6" }),
            // Bildung
            (new[] { "klassen", "unterricht", "classroom" }, new[] { "8" }),
            (new[] { "hörsaal", "hoersaal", "auditorium", "lecture" }, new[] { "9" }),
            // Beherbergung und Pflege
            (new[] { "patient", "bettenzimmer", "krankenzimmer" }, new[] { "10" }),
            (new[] { "hotelzimmer", "gästezimmer", "gaestezimmer", "personalzimmer", "guest room", "fremdenzimmer" }, new[] { "11" }),
            // Gastronomie und Küche
            (new[] { "kantine", "mensa", "speisesaal", "essen", "canteen", "cafeteria" }, new[] { "12" }),
            (new[] { "restaurant", "gastst", "gastraum", "schankraum", "bistro", "café", "cafe", "dining" }, new[] { "13" }),
            (new[] { "kühlraum", "kuehlraum", "kühlzelle", "kuehlzelle", "getränke", "getraenke", "vorratsraum", "spülküche", "spuelkueche", "cold store" }, new[] { "15" }),
            (new[] { "küche", "kueche", "kitchen" }, new[] { W, "14" }),
            // Sanitär und Aufenthalt
            (new[] { "wc", "toilet", "bad", "bath", "dusch", "shower", "umkleide", "locker", "sanit", "washroom", "waschraum" }, new[] { W, "16" }),
            (new[] { "aufenthalt", "pausenraum", "sozialraum", "lounge", "break room" }, new[] { "17" }),
            // Neben-, Verkehrs- und Lagerflächen
            (new[] { "treppe", "stair" }, new[] { "19" }),
            (new[] { "flur", "diele", "windfang", "eingang", "corridor", "hallway", "entrance", "vorraum" }, new[] { W, "19" }),
            (new[] { "foyer", "lobby" }, new[] { "26" }),
            (new[] { "lagerhalle", "hochregal", "warehouse" }, new[] { "43" }),
            (new[] { "server", "rechenzentrum", "edv", "data center", "datacenter" }, new[] { "21" }),
            (new[] { "lager", "abstell", "keller", "archiv", "technik", "heizraum", "heizung", "hausanschluss", "schacht", "aufzug", "elektro",
                     "putzraum", "müll", "muell", "entsorgung", "garage", "carport", "dachboden", "spitzboden", "storage", "store", "basement",
                     "attic", "plant", "mechanical", "electrical", "shaft" }, new[] { "20" }),
            // Hallen und Produktion
            (new[] { "produktion", "werkstatt", "montage", "fertigung", "betrieb", "halle", "workshop", "production", "assembly", "manufactur" },
             new[] { "22", "23", "24" }),
            // Veranstaltung und Kultur
            (new[] { "bühne", "buehne", "stage" }, new[] { "27" }),
            (new[] { "ausstellung", "museum", "exhibition", "gallery" }, new[] { "29" }),
            (new[] { "bibliothek", "lesesaal", "library" }, new[] { "30" }),
            // Sport und Wellness
            (new[] { "turnhalle", "sporthalle", "kegelbahn", "ballraum", "gymnastik", "gymnasium", "sports hall" }, new[] { "33" }),
            (new[] { "sauna" }, new[] { "36" }),
            (new[] { "fitness", "kraftraum", "gym" }, new[] { "37" }),
            // Labor und Gesundheit
            (new[] { "labor", "laboratory" }, new[] { "38" }),
            (new[] { "untersuchung", "behandlung", "massage", "arzt", "praxis", "examination", "treatment" }, new[] { "39" }),
            // Wohnen (nur im Wohngebäude ein Kandidat, sonst Aufenthaltsraum)
            (new[] { "wohn", "schlaf", "kinderzimmer", "living", "bedroom", "sleep", "zimmer" }, new[] { W, "17" }),
        };

        /// <summary>Die Kandidaten eines Namensstamms (<see cref="GebaeudeZonierung.Nutzung"/>); leer ohne Treffer.</summary>
        internal static IReadOnlyList<string> Kandidaten(string stamm)
        {
            string s = (stamm ?? "").Trim().ToLowerInvariant();
            if (s.Length == 0) return Array.Empty<string>();
            foreach ((string[] muster, string[] kandidaten) in ZEILEN)
                if (muster.Any(m => s.Contains(m, StringComparison.Ordinal))) return kandidaten;
            return Array.Empty<string>();
        }

        /// <summary>Zählt der Namensstamm als Wohnnutzung (Stufe 3, Wohnanteil ohne Raumart)?</summary>
        internal static bool Wohnen(string stamm)
        {
            string s = (stamm ?? "").Trim().ToLowerInvariant();
            return s.Length > 0 && new[] { "wohn", "schlaf", "kinderzimmer", "living", "bedroom" }.Any(m => s.Contains(m, StringComparison.Ordinal));
        }

        /// <summary>Ist der Namensstamm eine Küche (Zählung der Wohneinheiten)?</summary>
        internal static bool Kueche(string stamm)
        {
            string s = (stamm ?? "").Trim().ToLowerInvariant();
            return s.Length > 0 && !s.Contains("tee", StringComparison.Ordinal) && !s.Contains("spül", StringComparison.Ordinal)
                   && new[] { "küche", "kueche", "kitchen" }.Any(m => s.Contains(m, StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// <b>Die Gruppen der Norm für den Katalogbaum</b> (Entwurf ZZ 6, Festlegung der Oberfläche — die Norm führt Tabelle 6
    /// ohne Gruppen): Nummer → Gruppenschlüssel; der Anzeigetext steht in <c>RNP_GRUPPE_&lt;SCHLUESSEL&gt;</c>.
    /// </summary>
    internal static class DinProfilgruppen
    {
        /// <summary>Die Gruppen in der Reihenfolge des Baums.</summary>
        internal static readonly IReadOnlyList<string> GRUPPEN = new[]
        {
            "BUERO", "HANDEL", "BILDUNG", "BEHERBERGUNG", "GASTRONOMIE", "NEBENFLAECHEN", "HALLEN", "KULTUR", "SPORT", "PARKEN", "GESUNDHEIT", "WOHNEN",
        };

        /// <summary>Der Gruppenschlüssel einer DIN-Nummer; Wohnprofile ohne Nummer gehören zu <c>WOHNEN</c>; <c>null</c> außerhalb 1 bis 43.</summary>
        internal static string Gruppe(int? nummer)
        {
            if (nummer is not int n) return "WOHNEN";
            if (n >= 1 && n <= 5) return "BUERO";
            if (n == 6 || n == 7) return "HANDEL";
            if (n == 8 || n == 9 || (n >= 30 && n <= 32)) return "BILDUNG";
            if (n == 10 || n == 11 || (n >= 40 && n <= 42)) return "BEHERBERGUNG";
            if (n >= 12 && n <= 15) return "GASTRONOMIE";
            if (n >= 16 && n <= 21) return "NEBENFLAECHEN";
            if ((n >= 22 && n <= 24) || n == 43) return "HALLEN";
            if (n >= 25 && n <= 29) return "KULTUR";
            if (n == 33 || n == 36 || n == 37) return "SPORT";
            if (n == 34 || n == 35) return "PARKEN";
            if (n == 38 || n == 39) return "GESUNDHEIT";
            return null;
        }

        /// <summary>Der Anzeigetext einer Gruppe; ohne Ressource der Schlüssel.</summary>
        internal static string Text(string gruppe) => Raumprofilzuordnung.Ressource("RNP_GRUPPE_" + (gruppe ?? "")) ?? gruppe ?? "";
    }

    /// <summary>Die Gebäudeart für Stufe 3: Wohn- oder Nichtwohngebäude, Wohnanteil, Küchen und das Wohnprofil samt Stand.</summary>
    /// <param name="Wohngebaeude">Wohnanteil der beheizten Fläche ≥ 50 % oder Gebäudeart der Datei.</param>
    /// <param name="WohnanteilProzent">Der Wohnanteil der beheizten Fläche in Prozent (gerundet).</param>
    /// <param name="Kuechen">Zahl der Küchen (Raumart oder Name) — die Näherung der Wohneinheiten.</param>
    /// <param name="AusDatei">Kommt die Art aus der Gebäudeart der Datei (dann eindeutig)?</param>
    /// <param name="Wohnprofil">Der Schlüssel des Wohnprofils (EFH bis zwei Wohneinheiten, sonst MFH); <c>null</c> im Nichtwohngebäude.</param>
    /// <param name="WohnprofilEindeutig">Ist das Wohnprofil eindeutig (Gebäudeart der Datei nennt Ein- oder Mehrfamilienhaus)?</param>
    internal sealed record Gebaeudeartbefund(bool Wohngebaeude, int WohnanteilProzent, int Kuechen, bool AusDatei, string Wohnprofil, bool WohnprofilEindeutig)
    {
        /// <summary>Das Nichtwohngebäude ohne Anhalt.</summary>
        internal static readonly Gebaeudeartbefund Nichtwohnen = new Gebaeudeartbefund(false, 0, 0, false, null, false);
    }

    /// <summary>
    /// <b>Der Befund der Profilzuordnung eines Raums</b>: die Kandidaten in Normreihenfolge, der Vorschlag (bei
    /// <see cref="Raumprofilstand.Eindeutig"/> der einzige Kandidat), der Stand und die Begründungskette.
    /// </summary>
    internal sealed class Raumprofilbefund
    {
        /// <summary>Der Raum.</summary>
        internal AbbildRaum Raum { get; init; }

        /// <summary>Der Code der Raumart (ohne <c>mrt</c>); leer ohne Raumart.</summary>
        internal string Raumart { get; init; } = "";

        /// <summary>Der Namensstamm (klein, ohne Nummer); leer ohne Namen.</summary>
        internal string Namensstamm { get; init; } = "";

        /// <summary>Die Kandidaten als Profilschlüssel in Normreihenfolge; leer = keine.</summary>
        internal IReadOnlyList<string> Kandidaten { get; init; } = Array.Empty<string>();

        /// <summary>Der Vorschlag als Profilschlüssel; <c>null</c> = keiner (offen oder ohne).</summary>
        internal string Vorschlag { get; init; }

        /// <summary>Der Stand.</summary>
        internal Raumprofilstand Stand { get; init; }

        /// <summary>Trägt der Raum eine eigene Nutzungsangabe der Datei (Stufe 0)?</summary>
        internal bool EigeneAngabe { get; init; }

        /// <summary>Die Begründungskette in Stufenfolge.</summary>
        internal IReadOnlyList<Raumprofilgrund> Begruendung { get; init; } = Array.Empty<Raumprofilgrund>();

        /// <summary>Ist das Profil gesetzt (eindeutig, von Hand) oder vorgeschlagen?</summary>
        internal bool MitProfil => Vorschlag != null;

        /// <summary>
        /// Der Gruppenschlüssel der Regel Z7: der Vorschlag, bei offenen Räumen die Kandidatenmenge (<c>?1/2/3</c>), ohne
        /// Kandidaten <c>-</c>. Räume mit gleichem Schlüssel und gleicher Beheizung bilden eine Zone.
        /// </summary>
        internal string Gruppenschluessel
            => Vorschlag ?? (Kandidaten.Count > 0 ? "?" + string.Join("/", Kandidaten) : "-");

        /// <summary>Der Text der Begründungskette (Ressourcen, mit „ · “ gereiht).</summary>
        internal string Begruendungstext => Raumprofilzuordnung.Begruendungstext(Begruendung);

        public override string ToString() => (Raum?.Name ?? Raum?.Kennung ?? "") + " → " + Gruppenschluessel + " (" + Stand + ")";
    }

    /// <summary>
    /// <b>Das Raumprofil einer Vorschlagszone</b> (Z7: alle Räume der Zone teilen den Gruppenschlüssel; Z4/Z6: die
    /// flächengewichtete Mehrheit): Stand, Vorschlag, Kandidaten, Begründung des ersten Raums und — bei der Mehrheit — ihr
    /// Flächenanteil in Prozent (sonst 100).
    /// </summary>
    internal sealed record Raumprofilgruppe(string Gruppenschluessel, Raumprofilstand Stand, string Vorschlag, IReadOnlyList<string> Kandidaten,
                                            IReadOnlyList<Raumprofilgrund> Begruendung, int Prozent = 100)
    {
        /// <summary>Die Gruppe aus einem Befund (Z7).</summary>
        internal static Raumprofilgruppe Aus(Raumprofilbefund b)
            => new Raumprofilgruppe(b.Gruppenschluessel, b.Stand, b.Vorschlag, b.Kandidaten, b.Begruendung);
    }

    /// <summary>Die Zählung der Befunde eines Gebäudes.</summary>
    internal sealed record Raumprofilzaehlung(int Eindeutig, int Vorschlag, int Offen, int Ohne, int Hand)
    {
        /// <summary>Alle Räume.</summary>
        internal int Gesamt => Eindeutig + Vorschlag + Offen + Ohne + Hand;

        /// <summary>Räume, die eine Rückfrage brauchen (offen oder ohne Kandidaten).</summary>
        internal int Rueckfrage => Offen + Ohne;
    }

    /// <summary>
    /// <b>Ein Bündel für den Zuordnungsdialog</b> (ZZ-B): die Räume gleicher Raumart und gleichen Namensstamms mit ihrem
    /// gemeinsamen Befund — Stand, Vorschlag, Kandidaten und Begründung des ersten Raums; weichen die Räume voneinander ab
    /// (etwa nach einer Wahl von Hand an einem Einzelraum), ist das Bündel <see cref="Gemischt"/>.
    /// </summary>
    internal sealed record Raumprofilbuendel(string Raumart, string Namensstamm, string Anzeigename, IReadOnlyList<Raumprofilbefund> Raeume,
                                             double FlaecheM2, double FlaecheMinM2, double FlaecheMaxM2, Raumprofilstand Stand, string Vorschlag,
                                             IReadOnlyList<string> Kandidaten, IReadOnlyList<Raumprofilgrund> Begruendung, bool Gemischt)
    {
        /// <summary>Der Schlüssel des Bündels (Raumart und Stamm).</summary>
        internal string Schluessel => Raumart + "\u001F" + Namensstamm;
    }

    /// <summary>Ein Knoten des Katalogbaums (ZZ-B): Kategorie → Gruppe → Profil; ein Profilknoten trägt den Profilschlüssel.</summary>
    internal sealed record Katalogknoten(string Schluessel, string Name, IReadOnlyList<Katalogknoten> Kinder, string Profilschluessel = null,
                                         Raumnutzungsprofil Profil = null);

    /// <summary>
    /// <b>Die Profilzuordnung je Raum</b> (Entwurf ZZ 5.3 in der Fassung der Anwenderentscheide vom 17.10.2026 — F2: eigene
    /// Bezeichnungen der Datei werden vorgeschlagen, nicht still gesetzt; F3: keine Flächenschwellen, Rückfrage, wo keine
    /// Bezeichnung die Zuordnung erlaubt). Rangfolge: eigene Angabe der Datei → Raumart (Zuordnungszeile <c>HOTTCAD_RAUMTYP</c>
    /// vor <see cref="Raumarttafel"/>) → Raumname schärft (<see cref="Raumnamentafel"/>) → Gebäudeart löst das Wohnprofil
    /// (F4: Wohnanteil ≥ 50 % der beheizten Fläche; EFH bis zwei Wohneinheiten, Näherung Küchenzahl, sonst MFH). Automatisch
    /// gesetzt wird nur ohne eigene Angabe und bei genau einem Kandidaten; der Vorschlag der Hallen 22/23/24 ist 23 (F5).
    /// Ohne Datenbank; der Katalog kommt als <see cref="Raumnutzungsvorbelegung"/> herein.
    /// </summary>
    internal static class Raumprofilzuordnung
    {
        /// <summary>Wohngebäude ab diesem Anteil der beheizten Fläche mit Wohnnutzung (F4).</summary>
        internal const double WOHNANTEIL_GRENZE = 0.5;

        /// <summary>Einfamilienhaus bis zu dieser Zahl von Wohneinheiten (F4).</summary>
        internal const int EFH_WOHNEINHEITEN_MAX = 2;

        /// <summary>Der Vorschlag der Hallenprofile (F5): mittelschwere Arbeit.</summary>
        internal const string VORSCHLAG_HALLE = "23";

        /// <summary>Die festgelegten Vorschläge je Kandidatenmenge (Schlüssel: Kandidaten mit „/“ gereiht); alle anderen Mengen bleiben offen (F3).</summary>
        internal static readonly IReadOnlyDictionary<string, string> VORSCHLAG_JE_KANDIDATENMENGE = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["22/23/24"] = VORSCHLAG_HALLE,
        };

        /// <summary>Die Zuordnungsart der Herleitungszeile: gesetzt aus Raumart und Merkmalen (eindeutig).</summary>
        internal const string QUELLE = "RAUMPROFIL";

        /// <summary>Die Zuordnungsart der Herleitungszeile: Vorschlag, noch nicht bestätigt (F2, F5, F4).</summary>
        internal const string QUELLE_VORSCHLAG = "RAUMPROFIL_VORSCHLAG";

        /// <summary>Die Zuordnungsart der Herleitungszeile: offen, Rückfrage mit Kandidaten (F3).</summary>
        internal const string QUELLE_OFFEN = "RAUMPROFIL_OFFEN";

        /// <summary>Die Zuordnungsart der Herleitungszeile: die flächengewichtete Mehrheit der Raumprofile einer Zone (Z4, Z6; Befund B4).</summary>
        internal const string QUELLE_MEHRHEIT = "RAUMPROFIL_MEHRHEIT";

        /// <summary>Mindestanteil der beheizten Fläche für das Mehrheitsprofil einer Zone (Entwurf ZZ 5.2).</summary>
        internal const double MEHRHEIT_GRENZE = 0.5;

        /// <summary>Kennung einer Meldung: {0} eindeutig, {1} Vorschlag, {2} offen, {3} ohne Kandidaten, {4} Beispiele der Rückfragen.</summary>
        internal const string PROFILE_ZUGEORDNET = "PROFILE_ZUGEORDNET";

        // ------------------------------------------------------------------
        //  Gebäudeart (Stufe 3)
        // ------------------------------------------------------------------

        /// <summary>
        /// <b>Die Gebäudeart für die Zuordnung</b>: die Gebäudeart der Datei, wenn sie wohnt oder nicht wohnt
        /// (<see cref="Gebaeudeverwendung.WohnenAusGebaeudeart"/>); sonst der Wohnanteil der beheizten Fläche (Räume mit
        /// Wohn-Raumart, ohne Raumart mit Wohnnamen) ≥ 50 %. Das Wohnprofil: aus der Gebäudeart der Datei (ein-/mehrfamilien)
        /// eindeutig, sonst nach der Küchenzahl als Vorschlag.
        /// </summary>
        internal static Gebaeudeartbefund Gebaeudeart(AbbildGebaeude g, Func<AbbildRaum, bool> beheizt)
        {
            if (g == null) return Gebaeudeartbefund.Nichtwohnen;
            beheizt ??= r => r.Beheizt;
            double gesamt = 0.0, wohnen = 0.0;
            int kuechen = 0;
            foreach (AbbildRaum r in g.Raeume)
            {
                string code = Raumarttafel.Code(r.Raumtyp);
                string stamm = GebaeudeZonierung.Nutzung(r.Name);
                if (code.Length > 0 ? Raumarttafel.KUECHENCODES.Contains(code) : Raumnamentafel.Kueche(stamm)) kuechen++;
                if (!beheizt(r) || !(r.FlaecheM2 > 0.0)) continue;
                gesamt += r.FlaecheM2.Value;
                bool wohnt = code.Length > 0 ? Raumarttafel.WOHNCODES.Contains(code) : Raumnamentafel.Wohnen(stamm);
                if (wohnt) wohnen += r.FlaecheM2.Value;
            }
            int prozent = gesamt > 0.0 ? (int)Math.Round(100.0 * wohnen / gesamt, MidpointRounding.AwayFromZero) : 0;
            // Die Gebäudeart der Datei zählt nur, wenn sie wohnt: Der CAD-Typname des Gebäudes (etwa „TModelBuilding“) ist eine
            // benannte, aber nichtssagende Art — er darf den Wohnanteil nicht überstimmen (Befund der Quellenprobe ZZ-A).
            bool? datei = Gebaeudeverwendung.WohnenAusGebaeudeart(g.Art);
            bool wohngebaeude = datei == true || (gesamt > 0.0 && wohnen / gesamt >= WOHNANTEIL_GRENZE);
            if (!wohngebaeude) return new Gebaeudeartbefund(false, prozent, kuechen, false, null, false);
            string art = (g.Art ?? "").Trim().ToLowerInvariant();
            bool efh = art.Contains("einfamilien", StringComparison.Ordinal) || art.Contains("singlefamily", StringComparison.Ordinal)
                       || art.Contains("single family", StringComparison.Ordinal) || art.Contains("reihen", StringComparison.Ordinal)
                       || art.Contains("doppelhaus", StringComparison.Ordinal);
            bool mfh = art.Contains("mehrfamilien", StringComparison.Ordinal) || art.Contains("multifamily", StringComparison.Ordinal)
                       || art.Contains("multi family", StringComparison.Ordinal) || art.Contains("apartment", StringComparison.Ordinal);
            if (efh != mfh) return new Gebaeudeartbefund(true, prozent, kuechen, datei.HasValue, efh ? Raumprofilschluessel.WOHNEN_EFH : Raumprofilschluessel.WOHNEN_MFH, true);
            string profil = kuechen >= 1 && kuechen <= EFH_WOHNEINHEITEN_MAX ? Raumprofilschluessel.WOHNEN_EFH : Raumprofilschluessel.WOHNEN_MFH;
            return new Gebaeudeartbefund(true, prozent, kuechen, datei.HasValue, profil, false);
        }

        // ------------------------------------------------------------------
        //  Zuordnung je Raum
        // ------------------------------------------------------------------

        /// <summary>
        /// <b>Ordnet einen Raum zu</b> (Klassenkopf). <paramref name="hand"/> ist die Wahl von Hand (Profilschlüssel; leer =
        /// „keine“), die jede Stufe schlägt.
        /// </summary>
        internal static Raumprofilbefund Zuordnen(AbbildRaum r, Gebaeudeartbefund art, Raumnutzungsvorbelegung v, string hand = null)
        {
            v ??= Raumnutzungsvorbelegung.Vorgabe;
            art ??= Gebaeudeartbefund.Nichtwohnen;
            string code = Raumarttafel.Code(r?.Raumtyp);
            string stamm = GebaeudeZonierung.Nutzung(r?.Name);
            var gruende = new List<Raumprofilgrund>();

            if (hand != null)
            {
                gruende.Add(new Raumprofilgrund(Raumprofilstufe.Hand, hand));
                return new Raumprofilbefund
                {
                    Raum = r, Raumart = code, Namensstamm = stamm, Kandidaten = hand.Length == 0 ? Array.Empty<string>() : new[] { hand },
                    Vorschlag = hand.Length == 0 ? null : hand, Stand = Raumprofilstand.Hand, Begruendung = gruende,
                };
            }

            // Stufe 0 — die eigene Bezeichnung der Datei (F2: Vorschlag mit Herkunft „Bezeichnung der Datei“, nie still gesetzt).
            (string eigene, string bezeichnung) = EigeneAngabe(r, v);
            if (eigene != null)
            {
                gruende.Add(new Raumprofilgrund(Raumprofilstufe.Datei, bezeichnung));
                return new Raumprofilbefund
                {
                    Raum = r, Raumart = code, Namensstamm = stamm, Kandidaten = new[] { eigene }, Vorschlag = eigene,
                    Stand = Raumprofilstand.Vorschlag, EigeneAngabe = true, Begruendung = gruende,
                };
            }

            // Stufe 1 — die Raumart: eine Zuordnungszeile des Katalogs geht der Tafel vor (auch „keine“).
            List<string> kandidaten = new List<string>();
            bool zeile = false;
            if (code.Length > 0)
            {
                if (v.Herleitung(r).Art == RaumnutzungSchema.ZUORDNUNG_HOTTCAD)
                {
                    zeile = true;
                    Planprofil p = v.Aufloesen(RaumnutzungSchema.ZUORDNUNG_HOTTCAD, code);
                    if (Raumprofilschluessel.AusProfil(v, p) is string s) kandidaten.Add(s);
                }
                else kandidaten.AddRange(Raumarttafel.Kandidaten(code));
                gruende.Add(new Raumprofilgrund(Raumprofilstufe.Raumart, code));
            }

            // Stufe 2 — der Raumname schärft (Schnittmenge) oder setzt (ohne Raumart, ohne Zuordnungszeile, ohne Gemeinsames).
            if (!zeile)
            {
                IReadOnlyList<string> name = Raumnamentafel.Kandidaten(stamm);
                if (name.Count > 0)
                {
                    List<string> schnitt = kandidaten.Where(name.Contains).ToList();
                    if (kandidaten.Count == 0 || schnitt.Count == 0 || schnitt.Count < kandidaten.Count)
                    {
                        kandidaten = schnitt.Count > 0 ? schnitt : name.ToList();
                        gruende.Add(new Raumprofilgrund(Raumprofilstufe.Name, stamm));
                    }
                }
            }

            // Stufe 3 — die Gebäudeart löst den Platzhalter Wohnprofil auf oder streicht ihn.
            if (kandidaten.Contains(Raumprofilschluessel.WOHNEN))
            {
                if (art.Wohngebaeude && art.Wohnprofil != null)
                {
                    kandidaten = art.WohnprofilEindeutig
                        ? new List<string> { art.Wohnprofil }
                        : new List<string> { Raumprofilschluessel.WOHNEN_EFH, Raumprofilschluessel.WOHNEN_MFH };
                    gruende.Add(new Raumprofilgrund(Raumprofilstufe.Gebaeudeart, "W" + art.WohnanteilProzent.ToString(CultureInfo.InvariantCulture)));
                    if (!art.WohnprofilEindeutig) gruende.Add(new Raumprofilgrund(Raumprofilstufe.Gebaeudeart, "K" + art.Kuechen.ToString(CultureInfo.InvariantCulture)));
                }
                else
                {
                    kandidaten.Remove(Raumprofilschluessel.WOHNEN);
                    gruende.Add(new Raumprofilgrund(Raumprofilstufe.Gebaeudeart, "N"));
                }
            }
            kandidaten = kandidaten.Distinct(StringComparer.Ordinal).ToList();

            // Ergebnis
            string vorschlag = null;
            Raumprofilstand stand;
            if (kandidaten.Count == 1) { vorschlag = kandidaten[0]; stand = Raumprofilstand.Eindeutig; }
            else if (kandidaten.Count == 0) stand = Raumprofilstand.Ohne;
            else if (kandidaten.Count == 2 && Raumprofilschluessel.IstWohnen(kandidaten[0]) && Raumprofilschluessel.IstWohnen(kandidaten[1]))
            { vorschlag = art.Wohnprofil; stand = Raumprofilstand.Vorschlag; }
            else if (VORSCHLAG_JE_KANDIDATENMENGE.TryGetValue(string.Join("/", kandidaten), out string fest))
            { vorschlag = fest; stand = Raumprofilstand.Vorschlag; }
            else stand = Raumprofilstand.Offen;
            return new Raumprofilbefund
            {
                Raum = r, Raumart = code, Namensstamm = stamm, Kandidaten = kandidaten, Vorschlag = vorschlag, Stand = stand, Begruendung = gruende,
            };
        }

        /// <summary>
        /// Die eigene Nutzungsangabe eines Raums (Stufe 0): <c>EPOS_Zone.DinNummer</c>, <c>EPOS_Zone.Nutzung</c> mit Katalogtreffer,
        /// eine Beschreibung, die eine Nummer oder einen Profilnamen trägt. (Schlüssel, Bezeichnung); <c>(null, null)</c> = keine.
        /// </summary>
        private static (string Schluessel, string Bezeichnung) EigeneAngabe(AbbildRaum r, Raumnutzungsvorbelegung v)
        {
            if (r == null) return (null, null);
            if (r.Konditionierung?.DinNummer is int din && (Raumprofilschluessel.Profil(v, Raumprofilschluessel.Din(din)) != null || v.MitKatalog == false))
                return (Raumprofilschluessel.Din(din), "DIN " + din.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(r.Konditionierung?.Nutzung) && v.AusText(r.Konditionierung.Nutzung) is { NichtImKatalog: false, Id: long } pn
                && Raumprofilschluessel.AusProfil(v, pn) is string sn)
                return (sn, r.Konditionierung.Nutzung.Trim());
            string b = r.Beschreibung?.Trim();
            if (string.IsNullOrEmpty(b)) return (null, null);
            if (int.TryParse(b, NumberStyles.None, CultureInfo.InvariantCulture, out int n) && n >= 1 && n <= 43 && Raumprofilschluessel.Profil(v, Raumprofilschluessel.Din(n)) != null)
                return (Raumprofilschluessel.Din(n), b);
            if (v.AusText(b) is { NichtImKatalog: false, Id: long } pb && Raumprofilschluessel.AusProfil(v, pb) is string sb) return (sb, b);
            return (null, null);
        }

        /// <summary>
        /// <b>Die Befunde aller Räume eines Gebäudes</b> in Dateireihenfolge, mit der Gebäudeart aus denselben Räumen und den
        /// Wahlen von Hand (<paramref name="hand"/>: Raumkennung → Profilschlüssel, leer = „keine“).
        /// </summary>
        internal static IReadOnlyList<Raumprofilbefund> Befunde(AbbildGebaeude g, Func<AbbildRaum, bool> beheizt, Raumnutzungsvorbelegung v,
                                                                IReadOnlyDictionary<string, string> hand = null)
        {
            if (g == null) return Array.Empty<Raumprofilbefund>();
            Gebaeudeartbefund art = Gebaeudeart(g, beheizt);
            return g.Raeume.Select(r => Zuordnen(r, art, v, hand != null && hand.TryGetValue(r.Kennung, out string w) ? w ?? "" : null)).ToList();
        }

        /// <summary>Die Zählung nach Stand.</summary>
        internal static Raumprofilzaehlung Zaehlen(IEnumerable<Raumprofilbefund> befunde)
        {
            int e = 0, vo = 0, of = 0, oh = 0, h = 0;
            foreach (Raumprofilbefund b in befunde ?? Enumerable.Empty<Raumprofilbefund>())
                switch (b.Stand)
                {
                    case Raumprofilstand.Eindeutig: e++; break;
                    case Raumprofilstand.Vorschlag: vo++; break;
                    case Raumprofilstand.Offen: of++; break;
                    case Raumprofilstand.Ohne: oh++; break;
                    default: h++; break;
                }
            return new Raumprofilzaehlung(e, vo, of, oh, h);
        }

        /// <summary>
        /// <b>Die flächengewichtete Mehrheit</b> der Profile einer Raummenge (Z4/Z6/Z5, Entwurf ZZ 5.2): der Profilschlüssel mit
        /// der größten Fläche unter den Räumen mit Profil, wenn er mindestens <see cref="MEHRHEIT_GRENZE"/> der Fläche aller
        /// Räume trägt. <c>Eindeutig</c>, wenn jeder Raum eindeutig oder von Hand dasselbe Profil hat. <c>(null, …)</c> sonst.
        /// </summary>
        internal static (string Schluessel, int Prozent, bool Eindeutig) Mehrheit(IEnumerable<Raumprofilbefund> befunde)
        {
            List<Raumprofilbefund> liste = (befunde ?? Enumerable.Empty<Raumprofilbefund>()).Where(b => b.Raum != null).ToList();
            double gesamt = liste.Sum(b => b.Raum.FlaecheM2 > 0.0 ? b.Raum.FlaecheM2.Value : 0.0);
            if (gesamt <= 0.0) return (null, 0, false);
            var top = liste.Where(b => b.MitProfil).GroupBy(b => b.Vorschlag, StringComparer.Ordinal)
                           .Select(gr => (S: gr.Key, A: gr.Sum(b => b.Raum.FlaecheM2 > 0.0 ? b.Raum.FlaecheM2.Value : 0.0), N: gr.Count()))
                           .OrderByDescending(x => x.A).ThenByDescending(x => x.N).ThenBy(x => x.S, StringComparer.Ordinal).FirstOrDefault();
            if (top.S == null || top.A / gesamt < MEHRHEIT_GRENZE) return (null, 0, false);
            bool eindeutig = liste.All(b => b.Vorschlag == top.S && (b.Stand == Raumprofilstand.Eindeutig || b.Stand == Raumprofilstand.Hand));
            return (top.S, (int)Math.Round(100.0 * top.A / gesamt, MidpointRounding.AwayFromZero), eindeutig);
        }

        // ------------------------------------------------------------------
        //  Schnittstelle für den Zuordnungsdialog (ZZ-B)
        // ------------------------------------------------------------------

        /// <summary>
        /// <b>Die Bündel</b> gleicher Raumart und gleichen Namensstamms, offene zuerst (ohne, offen, Vorschlag, eindeutig, Hand),
        /// dann nach Fläche absteigend. Der Anzeigename ist der häufigste Raumname ohne Nummer, sonst die Raumart.
        /// </summary>
        internal static IReadOnlyList<Raumprofilbuendel> Buendel(IEnumerable<Raumprofilbefund> befunde)
        {
            var liste = new List<Raumprofilbuendel>();
            foreach (var gr in (befunde ?? Enumerable.Empty<Raumprofilbefund>()).Where(b => b.Raum != null)
                                                                                 .GroupBy(b => b.Raumart + "\u001F" + b.Namensstamm, StringComparer.Ordinal))
            {
                List<Raumprofilbefund> raeume = gr.ToList();
                Raumprofilbefund erster = raeume[0];
                bool gemischt = raeume.Any(b => b.Gruppenschluessel != erster.Gruppenschluessel || b.Stand != erster.Stand);
                double[] flaechen = raeume.Where(b => b.Raum.FlaecheM2 > 0.0).Select(b => b.Raum.FlaecheM2.Value).ToArray();
                liste.Add(new Raumprofilbuendel(erster.Raumart, erster.Namensstamm, Anzeigename(raeume), raeume, flaechen.Sum(),
                                                flaechen.Length > 0 ? flaechen.Min() : 0.0, flaechen.Length > 0 ? flaechen.Max() : 0.0,
                                                erster.Stand, erster.Vorschlag, erster.Kandidaten, erster.Begruendung, gemischt));
            }
            return liste.OrderBy(b => Rang(b.Stand)).ThenByDescending(b => b.FlaecheM2).ThenBy(b => b.Anzeigename, StringComparer.Ordinal).ToList();
        }

        /// <summary>Der häufigste Raumname ohne angehängte Nummer, sonst die Raumart, sonst die Kennung des ersten Raums.</summary>
        internal static string Anzeigename(IEnumerable<Raumprofilbefund> raeume)
        {
            List<Raumprofilbefund> liste = raeume.ToList();
            string name = liste.Select(b => Stammname(b.Raum?.Name)).Where(s => s.Length > 0)
                               .GroupBy(s => s, StringComparer.Ordinal).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
                               .Select(g => g.Key).FirstOrDefault();
            if (!string.IsNullOrEmpty(name)) return name;
            string art = liste.Select(b => b.Raumart).FirstOrDefault(a => a.Length > 0);
            return art ?? liste.Select(b => b.Raum?.Kennung).FirstOrDefault(k => !string.IsNullOrEmpty(k)) ?? "";
        }

        /// <summary>Der Raumname ohne angehängte Nummer in der Schreibung der Datei („Büroraum 002“ → „Büroraum“).</summary>
        internal static string Stammname(string name)
        {
            string n = (name ?? "").Trim();
            int ende = n.Length;
            while (ende > 0 && (char.IsDigit(n[ende - 1]) || n[ende - 1] == ' ' || n[ende - 1] == '.' || n[ende - 1] == '-' || n[ende - 1] == '_'))
                ende--;
            return ende > 0 ? n.Substring(0, ende) : n;
        }

        private static int Rang(Raumprofilstand s) => s switch
        {
            Raumprofilstand.Ohne => 0, Raumprofilstand.Offen => 1, Raumprofilstand.Vorschlag => 2, Raumprofilstand.Eindeutig => 3, _ => 4,
        };

        /// <summary>
        /// <b>Der Katalogbaum</b> Kategorie → Gruppe der Norm → Profil (ZZ-B): die Kategorie DIN/TS 18599-10 mit den Gruppen aus
        /// <see cref="DinProfilgruppen"/> (Profile in Nummernfolge, die Wohnprofile unter „Wohnen“), die übrigen Kategorien
        /// flach. Ohne Katalog die drei alten Kennungen als eine Kategorie.
        /// </summary>
        internal static IReadOnlyList<Katalogknoten> Katalogbaum(Raumnutzungsvorbelegung v)
        {
            v ??= Raumnutzungsvorbelegung.Vorgabe;
            var kategorien = new List<Katalogknoten>();
            foreach (var kat in v.Auswahl().GroupBy(a => a.Kategorie ?? "", StringComparer.Ordinal))
            {
                string art = kat.Select(a => a.Art).FirstOrDefault();
                if (art == RaumnutzungSchema.ART_DIN_V_18599_10)
                {
                    var jeGruppe = new Dictionary<string, List<Katalogknoten>>(StringComparer.Ordinal);
                    foreach (var a in kat)
                    {
                        int? nummer = v.DinNummer(a.Schluessel);
                        Planprofil p = v.AusText(a.Schluessel);
                        string gruppe = DinProfilgruppen.Gruppe(nummer) ?? "WOHNEN";
                        if (!jeGruppe.TryGetValue(gruppe, out List<Katalogknoten> kinder)) jeGruppe[gruppe] = kinder = new List<Katalogknoten>();
                        kinder.Add(new Katalogknoten(a.Schluessel, a.Name, Array.Empty<Katalogknoten>(), Raumprofilschluessel.AusProfil(v, p), v.Profil(p?.Id)));
                    }
                    var gruppen = DinProfilgruppen.GRUPPEN.Where(jeGruppe.ContainsKey)
                        .Select(gk => new Katalogknoten(gk, DinProfilgruppen.Text(gk),
                                                        jeGruppe[gk].OrderBy(k => Raumprofilschluessel.DinNummer(k.Profilschluessel) ?? 99).ThenBy(k => k.Name, StringComparer.Ordinal).ToList()))
                        .ToList();
                    kategorien.Add(new Katalogknoten(art, kat.Key, gruppen));
                }
                else
                {
                    kategorien.Add(new Katalogknoten(art ?? kat.Key, kat.Key,
                        kat.Select(a => new Katalogknoten(a.Schluessel, a.Name, Array.Empty<Katalogknoten>(),
                                                          Raumprofilschluessel.AusProfil(v, v.AusText(a.Schluessel)) ?? a.Schluessel, v.Profil(v.AusText(a.Schluessel)?.Id))).ToList()));
                }
            }
            return kategorien;
        }

        // ------------------------------------------------------------------
        //  Texte
        // ------------------------------------------------------------------

        /// <summary>Der Text einer Begründungskette („Raumart Office · Name „büroraum“ · Nichtwohngebäude“).</summary>
        internal static string Begruendungstext(IEnumerable<Raumprofilgrund> gruende)
        {
            var teile = new List<string>();
            foreach (Raumprofilgrund g in gruende ?? Enumerable.Empty<Raumprofilgrund>())
            {
                string t = g.Stufe switch
                {
                    Raumprofilstufe.Datei => Ressource("RNP_IMP_BEGR_DATEI", g.Wert),
                    Raumprofilstufe.Raumart => Ressource("RNP_IMP_BEGR_RAUMART", g.Wert),
                    Raumprofilstufe.Name => Ressource("RNP_IMP_BEGR_NAME", g.Wert),
                    Raumprofilstufe.Gebaeudeart when g.Wert == "N" => Ressource("RNP_IMP_BEGR_NICHTWOHN"),
                    Raumprofilstufe.Gebaeudeart when g.Wert.StartsWith("K", StringComparison.Ordinal) => Ressource("RNP_IMP_BEGR_KUECHEN", g.Wert.Substring(1)),
                    Raumprofilstufe.Gebaeudeart => Ressource("RNP_IMP_BEGR_WOHNGEBAEUDE", g.Wert.TrimStart('W')),
                    Raumprofilstufe.Hand => Ressource("RNP_IMP_BEGR_HAND"),
                    _ => null,
                };
                if (!string.IsNullOrEmpty(t)) teile.Add(t);
            }
            return string.Join(" · ", teile);
        }

        /// <summary>Die Kandidaten als Text (Anzeigenamen mit „ / “ gereiht).</summary>
        internal static string Kandidatentext(Raumnutzungsvorbelegung v, IEnumerable<string> kandidaten)
            => string.Join(" / ", (kandidaten ?? Enumerable.Empty<string>()).Select(k => Raumprofilschluessel.Anzeigename(v, k)));

        /// <summary>Ein Text der Ressourcen, mit Werten gefüllt; <c>null</c> ohne Ressource.</summary>
        internal static string Ressource(string schluessel, params string[] werte)
        {
            try
            {
                string text = MyResource.Resource.ResourceManager.GetString(schluessel, MyResource.Resource.Culture);
                if (string.IsNullOrEmpty(text)) return null;
                return werte == null || werte.Length == 0 ? text : string.Format(CultureInfo.CurrentCulture, text, werte);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
