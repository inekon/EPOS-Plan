using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;

namespace ZapfprofilValidierung
{
    /// <summary>
    /// <b>Der eine Bauweg des Katalogs</b>: aus Zeilen von Feldname → Text entstehen
    /// <see cref="Nutzungsart"/>, <see cref="Tagesgangsatz"/>, <see cref="Parametersatz"/> und
    /// <see cref="Zapfkategorie"/> — die Modelle, mit denen <see cref="ZapfprofilRechner"/> rechnet.
    /// Die Feldnamen sind die Spaltennamen der <c>Tab_Tww*_STAMM</c>; ein Paketordner und eine
    /// SQLite-Datei führen dieselben.
    ///
    /// <para><b>Ohne Datenbankzugriff.</b> Hier wird nur umgerechnet; wer die Zeilen geliefert hat,
    /// bleibt offen. Damit ist der Rechenweg des Werkzeugs derselbe wie der der Kern-Tests
    /// (<c>ZapfprofilTestbau</c>) — der Katalog kommt als Modell herein, nicht über einen
    /// Controller.</para>
    /// </summary>
    internal static class Katalogbau
    {
        /// <summary>Spaltenname der Katalogversion; im Paketordner fehlt er.</summary>
        private const string SPALTE_VERSION = "Katalogversion";

        /// <summary>
        /// Baut den Katalog. <paramref name="versionPaket"/> ist die Katalogversion, die Zeilen ohne
        /// Spalte <c>Katalogversion</c> bekommen (Paketordner); <c>null</c> = die Spalte ist da.
        /// </summary>
        internal static Katalog Bauen(List<Dictionary<string, string>> arten,
                                      List<Dictionary<string, string>> saetze,
                                      List<Dictionary<string, string>> gaenge,
                                      List<Dictionary<string, string>> parameter,
                                      List<Dictionary<string, string>> kategorien,
                                      string versionPaket, string herkunft,
                                      List<string> hinweise, out string fehler)
        {
            fehler = null;
            hinweise ??= new List<string>();
            try
            {
                Dictionary<int, Tagesgangsatz> satzJeId = Saetze(saetze, gaenge, versionPaket, hinweise);
                var artenListe = new List<Nutzungsart>();
                int laufend = 0;
                foreach (Dictionary<string, string> z in arten ?? new List<Dictionary<string, string>>())
                {
                    laufend++;
                    int id = Ganz(z, "ID") ?? laufend;
                    int idSatz = Ganz(z, "ID_Tagesgangsatz")
                                 ?? throw new Bauabbruch("Eine Nutzungsart fuehrt kein ID_Tagesgangsatz.");
                    if (!satzJeId.TryGetValue(idSatz, out Tagesgangsatz satz))
                    {
                        hinweise.Add("Die Nutzungsart \"" + Text(z, "Bezeichner") + "\" verweist auf den "
                                     + "Tagesgangsatz " + idSatz.ToString(CultureInfo.InvariantCulture)
                                     + ", den der Katalog nicht fuehrt - sie wird uebergangen.");
                        continue;
                    }
                    artenListe.Add(Art(z, id, satz, versionPaket));
                }

                Parametersatz ps = Parameter(parameter, versionPaket, out fehler);
                if (fehler != null) return null;

                return new Katalog
                {
                    Arten = artenListe,
                    Saetze = satzJeId.Values.OrderBy(s => s.Id).ToArray(),
                    Parameter = ps,
                    Kategorien = Kategorien(kategorien, artenListe, hinweise),
                    Herkunft = herkunft ?? "",
                    Hinweise = hinweise
                };
            }
            catch (Bauabbruch ex) { fehler = ex.Message; return null; }
            catch (FormatException ex) { fehler = "Ein Zahlenfeld des Katalogs ist nicht lesbar: " + ex.Message; return null; }
        }

        // =============================================================================
        //  Tagesgangsätze
        // =============================================================================

        private static Dictionary<int, Tagesgangsatz> Saetze(List<Dictionary<string, string>> saetze,
                                                            List<Dictionary<string, string>> gaenge,
                                                            string versionPaket, List<string> hinweise)
        {
            var anteile = new Dictionary<int, double[,]>();
            var herkunft = new Dictionary<int, Provenienz[]>();
            foreach (Dictionary<string, string> z in gaenge ?? new List<Dictionary<string, string>>())
            {
                int id = Ganz(z, "ID_Tagesgangsatz")
                         ?? throw new Bauabbruch("Ein Tagesgang fuehrt kein ID_Tagesgangsatz.");
                int tagtyp = Ganz(z, "Tagtyp") ?? throw new Bauabbruch("Ein Tagesgang fuehrt keinen Tagtyp.");
                if (tagtyp < 1 || tagtyp > Tagesgangsatz.TAGTYPEN)
                    throw new Bauabbruch("Ein Tagesgang fuehrt den Tagtyp "
                                         + tagtyp.ToString(CultureInfo.InvariantCulture) + " ausserhalb 1 bis 4.");
                if (!anteile.TryGetValue(id, out double[,] a))
                {
                    a = new double[Tagesgangsatz.TAGTYPEN, Tagesgangsatz.STUNDEN];
                    anteile[id] = a;
                    herkunft[id] = new Provenienz[Tagesgangsatz.TAGTYPEN];
                }
                for (int h = 1; h <= Tagesgangsatz.STUNDEN; h++)
                    a[tagtyp - 1, h - 1] = Zahl(z, "Anteil_" + h.ToString("00", CultureInfo.InvariantCulture)) ?? 0.0;
                herkunft[id][tagtyp - 1] = Herkunft(z, "");
            }

            var ergebnis = new Dictionary<int, Tagesgangsatz>();
            foreach (Dictionary<string, string> z in saetze ?? new List<Dictionary<string, string>>())
            {
                int id = Ganz(z, "ID") ?? throw new Bauabbruch("Ein Tagesgangsatz fuehrt keine ID.");
                if (!anteile.TryGetValue(id, out double[,] a))
                {
                    hinweise.Add("Der Tagesgangsatz \"" + Text(z, "Bezeichner") + "\" fuehrt keinen Tagesgang "
                                 + "- er wird uebergangen.");
                    continue;
                }
                ergebnis[id] = new Tagesgangsatz(id, a, herkunft[id])
                {
                    Bezeichner = Text(z, "Bezeichner"),
                    Katalogversion = Text(z, SPALTE_VERSION) ?? versionPaket ?? "",
                    Status = Status(z),
                    ReadOnly = Wahr(z, "ReadOnly")
                };
            }
            return ergebnis;
        }

        // =============================================================================
        //  Nutzungsarten
        // =============================================================================

        private static Nutzungsart Art(Dictionary<string, string> z, int id, Tagesgangsatz satz, string versionPaket)
        {
            var monate = new double[NutzungsartRaster.MONATE];
            for (int m = 1; m <= NutzungsartRaster.MONATE; m++)
                monate[m - 1] = Zahl(z, "Monat_" + m.ToString(CultureInfo.InvariantCulture))
                                ?? throw new Bauabbruch("Der Nutzungsart \"" + Text(z, "Bezeichner")
                                       + "\" fehlt Monat_" + m.ToString(CultureInfo.InvariantCulture) + ".");
            var woche = new double[NutzungsartRaster.WOCHENTAGE];
            for (int w = 1; w <= NutzungsartRaster.WOCHENTAGE; w++)
                woche[w - 1] = Zahl(z, "Woche_" + w.ToString(CultureInfo.InvariantCulture))
                               ?? throw new Bauabbruch("Der Nutzungsart \"" + Text(z, "Bezeichner")
                                      + "\" fehlt Woche_" + w.ToString(CultureInfo.InvariantCulture) + ".");
            var bedarf = new double[NutzungsartRaster.NIVEAUS];
            for (int n = 0; n < NutzungsartRaster.NIVEAUS; n++)
                bedarf[n] = Zahl(z, NutzungsartRaster.Niveauspalten[n])
                            ?? throw new Bauabbruch("Der Nutzungsart \"" + Text(z, "Bezeichner") + "\" fehlt "
                                   + NutzungsartRaster.Niveauspalten[n] + ".");

            var bandbreite = new Bedarfsbandbreite(
                new[] { Zahl(z, "Bedarf_Niedrig_Min"), Zahl(z, "Bedarf_Mittel_Min"), Zahl(z, "Bedarf_Hoch_Min") },
                new[] { Zahl(z, "Bedarf_Niedrig_Max"), Zahl(z, "Bedarf_Mittel_Max"), Zahl(z, "Bedarf_Hoch_Max") });

            return new Nutzungsart(id, Text(z, "Bezeichner"),
                (ZapfBezugsart)Pflichtganz(z, "Bezugsart"), bedarf,
                new Temperaturbezug(Pflichtzahl(z, "Bezug_Zapftemperatur"), Pflichtzahl(z, "Bezug_Kaltwasser")),
                (ZapfBilanzgrenze)Pflichtganz(z, "Bilanzgrenze"),
                (ZapfKalenderart)Pflichtganz(z, "Kalenderart"),
                Zahl(z, "Ferienfaktor"), monate, woche, satz,
                new Katalogherkunft(Herkunft(z, "Bedarf_"), bandbreite, Herkunft(z, "Jahresgang_"),
                                    Herkunft(z, "Wochengang_"), satz.JeTagtyp))
            {
                Katalogversion = Text(z, SPALTE_VERSION) ?? versionPaket ?? "",
                Status = Status(z),
                ReadOnly = Wahr(z, "ReadOnly"),
                IdVorlage = Ganz(z, "ID_Vorlage"),
                Freigabe = Text(z, "Freigabe")
            };
        }

        // =============================================================================
        //  Parametersatz
        // =============================================================================

        private static Parametersatz Parameter(List<Dictionary<string, string>> zeilen, string versionPaket,
                                               out string fehler)
        {
            fehler = null;
            var werte = new List<ZapfParameterwert>();
            string version = versionPaket;
            foreach (Dictionary<string, string> z in zeilen ?? new List<Dictionary<string, string>>())
            {
                string s = Text(z, "Schluessel");
                if (string.IsNullOrEmpty(s)) continue;
                double? w = Zahl(z, "Wert");
                if (w == null) continue;
                version ??= Text(z, SPALTE_VERSION);
                werte.Add(new ZapfParameterwert(s, w.Value, Text(z, "Einheit") ?? "", Herkunft(z, "")));
            }
            if (werte.Count == 0) { fehler = "Der Katalog fuehrt keinen einzigen Parameter."; return null; }
            try { return Parametersatz.Aus(version ?? "", werte); }
            catch (ParametersatzException ex) { fehler = "Parametersatz: " + ex.Satz?.Klartext ?? ex.Message; return null; }
        }

        // =============================================================================
        //  Zapfkategorien
        // =============================================================================

        private static IReadOnlyList<Zapfkategorie> Kategorien(List<Dictionary<string, string>> zeilen,
                                                              List<Nutzungsart> arten, List<string> hinweise)
        {
            var liste = new List<Zapfkategorie>();
            bool gruppenform = false;
            foreach (Dictionary<string, string> z in zeilen ?? new List<Dictionary<string, string>>())
            {
                int? id = Ganz(z, "ID_Nutzungsart");
                if (id == null)
                {
                    gruppenform = gruppenform || z.ContainsKey("Gruppe");
                    continue;
                }
                liste.Add(new Zapfkategorie(id.Value, Text(z, "Kategorie"),
                                            Pflichtzahl(z, "Volumenstrom_l_min"), Pflichtzahl(z, "Sigma"),
                                            Pflichtganz(z, "Dauer_min"), Pflichtzahl(z, "Anteil"), Herkunft(z, ""))
                {
                    KappungLJeMin = Zahl(z, "Kappung_l_min")
                });
            }
            if (gruppenform)
                hinweise.Add("Die Zapfkategorien des Paketordners fuehren die Spalte \"Gruppe\" statt "
                             + "\"ID_Nutzungsart\". Diese Form ordnet erst der Katalogimport des Programms zu; "
                             + "das Werkzeug liest sie nicht. Fuer die Stochastik eine SQLite-Katalogquelle oder "
                             + "einen Paketordner mit ID_Nutzungsart angeben.");
            foreach (Zapfkategorie k in liste)
                if (arten.All(a => a.Id != k.IdNutzungsart))
                    hinweise.Add("Die Zapfkategorie \"" + k.Name + "\" verweist auf die Nutzungsart "
                                 + k.IdNutzungsart.ToString(CultureInfo.InvariantCulture)
                                 + ", die der Katalog nicht fuehrt.");
            return liste;
        }

        // =============================================================================
        //  Felder
        // =============================================================================

        private sealed class Bauabbruch : Exception
        {
            internal Bauabbruch(string satz) : base(satz) { }
        }

        private static string Text(Dictionary<string, string> z, string feld)
            => z != null && z.TryGetValue(feld, out string w) ? w : null;

        private static double? Zahl(Dictionary<string, string> z, string feld)
        {
            string w = Text(z, feld);
            if (string.IsNullOrWhiteSpace(w)) return null;
            return double.Parse(w.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private static int? Ganz(Dictionary<string, string> z, string feld)
        {
            double? w = Zahl(z, feld);
            return w == null ? (int?)null : (int)Math.Round(w.Value, MidpointRounding.AwayFromZero);
        }

        private static double Pflichtzahl(Dictionary<string, string> z, string feld)
            => Zahl(z, feld) ?? throw new Bauabbruch("Dem Katalog fehlt das Feld " + feld + ".");

        private static int Pflichtganz(Dictionary<string, string> z, string feld)
            => Ganz(z, feld) ?? throw new Bauabbruch("Dem Katalog fehlt das Feld " + feld + ".");

        private static bool Wahr(Dictionary<string, string> z, string feld)
        {
            string w = Text(z, feld);
            return w == "1" || string.Equals(w, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static ZapfKatalogstatus Status(Dictionary<string, string> z)
        {
            string w = Text(z, "Status");
            try { return w == null ? ZapfKatalogstatus.Auslieferung : TwwWertemengen.Status(w); }
            catch (Exception) { return ZapfKatalogstatus.Auslieferung; }
        }

        /// <summary>Die Provenienz einer Wertgruppe; <paramref name="praefix"/> ist etwa <c>Bedarf_</c>.</summary>
        private static Provenienz Herkunft(Dictionary<string, string> z, string praefix)
        {
            string art = Text(z, praefix + "Herkunftsart");
            Herkunftsart a;
            try { a = art == null ? Herkunftsart.Frei : TwwWertemengen.Herkunft(art); }
            catch (Exception) { a = Herkunftsart.Frei; }
            return new Provenienz(Text(z, praefix + "Quelle") ?? "", Text(z, praefix + "Ausgabe"),
                                  Text(z, praefix + "Version") ?? "", a);
        }
    }
}
