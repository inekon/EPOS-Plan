using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Die Kennung eines Kalenders einer Zone je Größe (MA-2): wer ihn trägt.</summary>
    public enum Kalenderkennung
    {
        /// <summary>Die Zone trägt einen eigenen Kalender (Hand, Projektdatei).</summary>
        Eigen,
        /// <summary>Der Kalender der Zone ist vom Generator eines Nutzungsprofils erzeugt (<c>Nutzung</c> gesetzt).</summary>
        Profil,
        /// <summary>Die Zone erbt den Kalender des Gebäudes.</summary>
        Geerbt,
        /// <summary>Weder die Zone noch das Gebäude trägt einen Kalender dieser Größe.</summary>
        Fehlt,
        /// <summary>Die Größe gilt für die Zone nicht (Heizen und Kühlen einer unbeheizten Zone).</summary>
        Entfaellt
    }

    /// <summary>Woher das Nutzungsprofil einer Zone kommt (MA-2).</summary>
    public enum Profilherkunft
    {
        /// <summary>Ein Profil des Katalogs (oder ein Nutzungstext) steht an der Zone.</summary>
        Profil,
        /// <summary>Die Zone trägt eigene Kalender ohne Profil (Projektdatei, Hand).</summary>
        Datei,
        /// <summary>Kein Profil: die Zone rechnet mit Vorgaben; der Vorschlag der Profilregel steht daneben.</summary>
        Vorgabe
    }

    /// <summary>Woher Ferien bzw. Heizperiode einer Zone kommen (MA-2, E137 F5 (a)).</summary>
    public enum Ferienkennung
    {
        /// <summary>Die Projektvorgabe (die Zeile am Gebäude) gilt.</summary>
        Projekt,
        /// <summary>Die Zone trägt eine eigene Zeile — eine Abweichung.</summary>
        Abweichung,
        /// <summary>Weder Zone noch Gebäude tragen eine Zeile.</summary>
        Fehlt
    }

    /// <summary>Woher die eigenen Ferien einer Zone kommen (E139): vom Generator eines Nutzungsprofils oder eigen.</summary>
    public enum Ferienherkunft
    {
        /// <summary>Die Zone trägt keine eigenen Ferien.</summary>
        Keine,
        /// <summary>Der Generator eines Nutzungsprofils hat sie geschrieben (ein Kalender der Zone trägt <c>Nutzung</c>).</summary>
        Profilgenerator,
        /// <summary>Eigen — von Hand oder aus einer Projektdatei; die Herkunft ist nicht erkennbar.</summary>
        Eigen
    }

    /// <summary>
    /// <b>Die eigenen Ferien einer Zone</b> (MA-2, E139): die Zahl der Vorgabezeilen <c>FERIEN</c> der Zone (Ferienwerte je
    /// Größe), die Zahl der Ferienzeiträume ihrer Kalender (verschiedene Paare Beginn–Ende der Perioden der Art
    /// <c>FERIEN</c>), ob diese Zeiträume von den Ferienzeiträumen des Projekts abweichen, und die Herkunft.
    /// </summary>
    public sealed record Zonenferien(int Werte, int Zeitraeume, bool ZeitraeumeAbweichend, Ferienherkunft Herkunft)
    {
        /// <summary>Weichen die Ferien der Zone von der Projektvorgabe ab (eigene Ferienwerte oder andere Zeiträume)?</summary>
        public bool Abweichend => Werte > 0 || ZeitraeumeAbweichend;

        /// <summary>Keine eigenen Ferien.</summary>
        public static readonly Zonenferien Ohne = new Zonenferien(0, 0, false, Ferienherkunft.Keine);
    }

    /// <summary>Ein Profil des Katalogs für die Auswahl „Profil zuweisen…“ (Id, Name, Kategorie).</summary>
    public sealed record ZonenNutzungsprofil(long Id, string Name, string Kategorie);

    /// <summary>
    /// <b>Eine Zone im Blatt „Nutzung und Kalender“</b> (MA-2): Fläche, Profil samt Kategorie und Herkunft, der Vorschlag
    /// der Profilregel, je Größe die Kalenderkennung (Schlüssel <see cref="DbWerte.KOND_GROESSE_HEIZSOLL"/> usw.),
    /// Sollwerte Tag und Nacht, Ferien und Heizperiode, die Ampel und die Befunde der Prüfgruppe 3, dazu die eigenen
    /// Ferien der Zone (<see cref="Zonenferien"/>, E139; <c>null</c> = ohne Angabe).
    /// </summary>
    public sealed record ZonenNutzungszeile(int IdZone, string Zone, double? Flaeche, bool Beheizt, string Profil, long? IdProfil,
                                           string Kategorie, Profilherkunft Herkunft, string Vorschlag,
                                           IReadOnlyDictionary<string, Kalenderkennung> Kalender, double? SollTag, double? SollNacht,
                                           Ferienkennung Ferien, Ferienkennung Heizperiode, Pruefampel Ampel,
                                           IReadOnlyList<string> Befunde, Zonenferien FerienAngaben = null);

    /// <summary>
    /// <b>Der Stand des Blatts</b>: die Zonen, ob das Projekt Ferien und Heizperiode vorgibt (am Gebäude oder an einem anderen
    /// Gebäude des Projekts), die Befunde der Gruppe 3 ohne Zone und ob eine Rücknahme möglich ist.
    /// </summary>
    public sealed record ZonenNutzungsstand(int IdGebaeude, IReadOnlyList<ZonenNutzungszeile> Zonen, bool ProjektFerien,
                                           bool ProjektHeizperiode, IReadOnlyList<string> Gebaeudebefunde, bool KannZuruecknehmen);

    /// <summary>Das Ergebnis einer Mehrfachänderung: Erfolg, Meldung, Zahl der geänderten Zonen und der neue Stand.</summary>
    public sealed record ZonenNutzungsergebnis(bool Ok, string Meldung, int Zonen, ZonenNutzungsstand Stand);

    /// <summary>
    /// <b>Nutzung und Kalender aller Zonen eines Gebäudes</b> (Entwurf Geführter Modellaufbau 2 und 4, Welle MA-2, E137 F5/F6):
    /// liest je Zone Profil, Kalenderkennungen, Sollwerte, Ferien und Heizperiode samt den Befunden der Prüfgruppe 3
    /// (<see cref="Modellpruefung"/>) und schreibt eine Handlung für n gewählte Zonen in EINEM Vorgang — Profil zuweisen
    /// (Generator über <see cref="ZonenplanCtrl.NutzungUebernehmen"/>), Projektvorgabe für Ferien und Heizperiode übernehmen
    /// (die Abweichung der Zone fällt; fehlt die Zeile am Gebäude, kommt sie von einem anderen Gebäude des Projekts),
    /// Kalender einer Zone kopieren (<see cref="Konditionierungskopie.Kopieren"/>, ersetzend) und Sollwerte setzen.
    ///
    /// <para><b>Rücknahme:</b> Vor jeder Mehrfachänderung merkt sich die Instanz das Abbild der betroffenen Zonen (Spalten an
    /// <c>Tab_Zone</c>, Kalender, Perioden, Wochen, Vorgabezeilen) und, wo nötig, die Vorgabezeilen des Gebäudes
    /// (Sitzungsgedächtnis wie <see cref="ModellpruefungCtrl"/>, keine Tabelle). <see cref="Zuruecknehmen"/> stellt die letzte
    /// Änderung wieder her. Nur über <see cref="DataRepository"/> mit <c>?</c>-Parametern; kein Schemaschritt, keine
    /// Leseregel des Rechenwegs.</para>
    /// </summary>
    public sealed class ZonenNutzungCtrl
    {
        /// <summary>Die fünf Größen in Schemareihenfolge (Spalten des Blatts).</summary>
        public static readonly IReadOnlyList<string> GROESSEN = new[]
        {
            DbWerte.KOND_GROESSE_HEIZSOLL, DbWerte.KOND_GROESSE_KUEHLSOLL, DbWerte.KOND_GROESSE_LUEFTUNG,
            DbWerte.KOND_GROESSE_GERAETE, DbWerte.KOND_GROESSE_PERSONEN,
        };

        private const string SPALTEN_ZONE = "\"Nutzungsprofil\", \"Raumsolltemperatur_Tag\", \"Raumsolltemperatur_Nachtabsenkung\", \"Herkunft\"";
        private const string TAB_WOCHE = KalenderbedienungSchema.TAB_WOCHE;

        private Abbild _letztes;

        /// <summary>Das Blatt des Projektgebäudes <paramref name="idGebaeude"/> (<c>Tab_Gebaeude.ID</c>).</summary>
        public ZonenNutzungCtrl(int idGebaeude) { IdGebaeude = idGebaeude; }

        /// <summary>Das Gebäude.</summary>
        public int IdGebaeude { get; }

        /// <summary>Kann die letzte Mehrfachänderung zurückgenommen werden?</summary>
        public bool KannZuruecknehmen => _letztes != null;

        /// <summary>Die Profile des Katalogs (Kategorie, dann Nummer); leer ohne Katalog.</summary>
        public static IReadOnlyList<ZonenNutzungsprofil> Profile()
        {
            if (!RaumnutzungCtrl.Lesbar()) return Array.Empty<ZonenNutzungsprofil>();
            var ctrl = new RaumnutzungCtrl();
            Dictionary<long, string> kat = ctrl.Kategorien().ToDictionary(k => k.Id, k => k.Bezeichner ?? "");
            return ctrl.Profile().Select(p => new ZonenNutzungsprofil(p.Id, p.Bezeichner ?? "",
                                                                      kat.TryGetValue(p.IdKatalog, out string k) ? k : ""))
                       .ToList();
        }

        // =====================================================================
        //  Lesen
        // =====================================================================

        /// <summary><b>Liest den Stand</b> des Blatts; <c>null</c>, wenn es das Gebäude nicht gibt.</summary>
        public ZonenNutzungsstand Lesen()
        {
            Modellpruefstand s = new ModellpruefungCtrl(IdGebaeude).Lesen();
            if (s == null) return null;
            Modellpruefergebnis pruefung = Modellpruefung.Pruefen(s);
            List<Modellbefund> befunde = pruefung.Gruppe(Pruefgruppe.NutzungKonditionierung).Befunde.ToList();

            // Kalender je Eigner und Größe, mit Nutzung (Schlüssel 0 = Gebäude).
            var kalender = new Dictionary<int, Dictionary<string, string>>();
            var vorgaben = new Dictionary<int, HashSet<string>>();
            if (KonditionierungSchema.Lesbar())
            {
                foreach (DataRow r in DataRepository.GetDataTable(
                             "SELECT \"ID_Zone\", \"Groesse\", \"Nutzung\" FROM \"" + KonditionierungSchema.TAB_KALENDER +
                             "\" WHERE \"ID_Gebaeude\" = ? ORDER BY \"ID\"", new DbParam("@g", IdGebaeude)).Rows)
                {
                    int eigner = r["ID_Zone"] is DBNull ? 0 : Convert.ToInt32(r["ID_Zone"], CultureInfo.InvariantCulture);
                    if (!kalender.TryGetValue(eigner, out Dictionary<string, string> je)) kalender[eigner] = je = new Dictionary<string, string>(StringComparer.Ordinal);
                    string g = Convert.ToString(r["Groesse"], CultureInfo.InvariantCulture);
                    string n = r["Nutzung"] is DBNull ? "" : Convert.ToString(r["Nutzung"], CultureInfo.InvariantCulture);
                    if (!je.ContainsKey(g) || (je[g].Length == 0 && n.Length > 0)) je[g] = n;
                }
                foreach (DataRow r in DataRepository.GetDataTable(
                             "SELECT DISTINCT \"ID_Zone\", \"Zeile\" FROM \"" + KonditionierungSchema.TAB_VORGABE +
                             "\" WHERE \"ID_Gebaeude\" = ? AND \"Zeile\" IN (?, ?)",
                             new DbParam("@g", IdGebaeude), new DbParam("@f", DbWerte.KOND_ZEILE_FERIEN),
                             new DbParam("@s", DbWerte.KOND_ZEILE_SAISON)).Rows)
                {
                    int eigner = r["ID_Zone"] is DBNull ? 0 : Convert.ToInt32(r["ID_Zone"], CultureInfo.InvariantCulture);
                    if (!vorgaben.TryGetValue(eigner, out HashSet<string> z)) vorgaben[eigner] = z = new HashSet<string>(StringComparer.Ordinal);
                    z.Add(Convert.ToString(r["Zeile"], CultureInfo.InvariantCulture));
                }
            }
            Dictionary<int, Zonenferien> ferien = FerienJeZone(kalender, out bool projektZeitraeume);
            bool gebFerien = s.FerienBestand || Hat(vorgaben, 0, DbWerte.KOND_ZEILE_FERIEN) || projektZeitraeume;
            bool gebSaison = Hat(vorgaben, 0, DbWerte.KOND_ZEILE_SAISON);

            List<ZonenNutzungsprofil> katalog = Profile().ToList();
            var zeilen = new List<ZonenNutzungszeile>();
            foreach (ZoneModel z in s.Zonen)
            {
                kalender.TryGetValue(z.ID, out Dictionary<string, string> eigen);
                kalender.TryGetValue(0, out Dictionary<string, string> geb);
                var kennungen = new Dictionary<string, Kalenderkennung>(StringComparer.Ordinal);
                foreach (string g in GROESSEN)
                {
                    bool thermisch = g == DbWerte.KOND_GROESSE_HEIZSOLL || g == DbWerte.KOND_GROESSE_KUEHLSOLL;
                    if (thermisch && !z.IstBeheizt) { kennungen[g] = Kalenderkennung.Entfaellt; continue; }
                    string n = Groesse(eigen, g);
                    kennungen[g] = n != null ? (n.Length > 0 ? Kalenderkennung.Profil : Kalenderkennung.Eigen)
                                 : Groesse(geb, g) != null ? Kalenderkennung.Geerbt : Kalenderkennung.Fehlt;
                }
                string profil = s.Nutzung(z);
                ZonenNutzungsprofil treffer = profil == null ? null
                    : katalog.FirstOrDefault(p => string.Equals(KonditionierungNutzungSchema.Nutzungstext(p.Name), profil, StringComparison.Ordinal));
                bool eigeneKalender = eigen != null && eigen.Count > 0;
                Profilherkunft herkunft = profil != null ? Profilherkunft.Profil : eigeneKalender ? Profilherkunft.Datei : Profilherkunft.Vorgabe;
                string vorschlag = "";
                if (herkunft == Profilherkunft.Vorgabe && z.IstBeheizt && Modellvorschlagsregeln.Profil(s, z) is Pruefvorschlag pv)
                    vorschlag = pv.Name ?? "";
                List<Modellbefund> jeZone = befunde.Where(b => b.IdZone == z.ID).ToList();
                Pruefampel ampel = jeZone.Count == 0 ? Pruefampel.Gruen : jeZone.Max(b => b.Ampel);
                if (!ferien.TryGetValue(z.ID, out Zonenferien zf)) zf = Zonenferien.Ohne;
                zeilen.Add(new ZonenNutzungszeile(
                    z.ID, z.Bezeichner ?? "", z.Nutzflaeche, z.IstBeheizt, profil ?? "", treffer?.Id, treffer?.Kategorie ?? "",
                    herkunft, vorschlag, kennungen, z.Raumsolltemperatur_Tag, z.Raumsolltemperatur_Nachtabsenkung,
                    zf.Abweichend ? Ferienkennung.Abweichung : gebFerien ? Ferienkennung.Projekt : Ferienkennung.Fehlt,
                    Hat(vorgaben, z.ID, DbWerte.KOND_ZEILE_SAISON) ? Ferienkennung.Abweichung : gebSaison ? Ferienkennung.Projekt : Ferienkennung.Fehlt,
                    ampel, jeZone.Select(b => b.Text ?? "").ToList(), zf));
            }
            return new ZonenNutzungsstand(IdGebaeude, zeilen, gebFerien || s.FerienQuelle.HasValue, gebSaison || s.SaisonQuelle.HasValue,
                                          befunde.Where(b => b.IdZone == null).Select(b => b.Text ?? "").ToList(), KannZuruecknehmen);
        }

        /// <summary>
        /// <b>Die eigenen Ferien je Zone</b> (E139): Ferienwerte (Vorgabezeilen <c>FERIEN</c> der Zone) und Ferienzeiträume
        /// (Perioden der Art <c>FERIEN</c> in den Kalendern der Zone). Die Zeiträume weichen ab, wenn ihre Menge (Beginn–Ende)
        /// nicht die der Kalender des Gebäudes ist; eine Zone ohne Ferienperioden weicht darin nicht ab. Die Herkunft ist
        /// „Profilgenerator“, wenn ein Kalender der Zone die Nutzung eines Profils trägt, sonst „eigen“.
        /// <paramref name="projektZeitraeume"/> sagt, ob die Kalender des Gebäudes Ferienzeiträume tragen.
        /// </summary>
        private Dictionary<int, Zonenferien> FerienJeZone(Dictionary<int, Dictionary<string, string>> kalender, out bool projektZeitraeume)
        {
            var ergebnis = new Dictionary<int, Zonenferien>();
            projektZeitraeume = false;
            if (!KonditionierungSchema.Lesbar()) return ergebnis;
            var werte = new Dictionary<int, int>();
            foreach (DataRow r in DataRepository.GetDataTable(
                         "SELECT \"ID_Zone\", COUNT(*) AS \"Zahl\" FROM \"" + KonditionierungSchema.TAB_VORGABE +
                         "\" WHERE \"ID_Gebaeude\" = ? AND \"ID_Zone\" IS NOT NULL AND \"Zeile\" = ? GROUP BY \"ID_Zone\"",
                         new DbParam("@g", IdGebaeude), new DbParam("@f", DbWerte.KOND_ZEILE_FERIEN)).Rows)
                werte[Convert.ToInt32(r["ID_Zone"], CultureInfo.InvariantCulture)] = Convert.ToInt32(r["Zahl"], CultureInfo.InvariantCulture);
            Dictionary<int, HashSet<(int, int)>> zeitraeume = Ferienzeitraeume();
            HashSet<(int, int)> projekt = zeitraeume.TryGetValue(0, out HashSet<(int, int)> p) ? p : new HashSet<(int, int)>();
            projektZeitraeume = projekt.Count > 0;
            foreach (int zone in werte.Keys.Concat(zeitraeume.Keys).Where(k => k != 0).Distinct())
            {
                werte.TryGetValue(zone, out int n);
                zeitraeume.TryGetValue(zone, out HashSet<(int, int)> eigen);
                int zr = eigen?.Count ?? 0;
                bool abweichend = zr > 0 && !eigen.SetEquals(projekt);
                if (n == 0 && zr == 0) continue;
                bool profil = kalender.TryGetValue(zone, out Dictionary<string, string> je) && je.Values.Any(x => x.Length > 0);
                ergebnis[zone] = new Zonenferien(n, zr, abweichend,
                                                 n > 0 || abweichend ? (profil ? Ferienherkunft.Profilgenerator : Ferienherkunft.Eigen) : Ferienherkunft.Keine);
            }
            return ergebnis;
        }

        /// <summary>Die Ferienzeiträume (Beginn, Ende) der Kalender je Eigner des Gebäudes (Schlüssel 0 = Gebäude).</summary>
        private Dictionary<int, HashSet<(int, int)>> Ferienzeitraeume()
        {
            var je = new Dictionary<int, HashSet<(int, int)>>();
            foreach (DataRow r in DataRepository.GetDataTable(
                         "SELECT k.\"ID_Zone\", p.\"Beginn\", p.\"Ende\" FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" p INNER JOIN \"" +
                         KonditionierungSchema.TAB_KALENDER + "\" k ON k.\"ID\" = p.\"ID_Kalender\" WHERE k.\"ID_Gebaeude\" = ? AND p.\"Art\" = ? " +
                         "AND p.\"Beginn\" IS NOT NULL AND p.\"Ende\" IS NOT NULL",
                         new DbParam("@g", IdGebaeude), new DbParam("@a", DbWerte.KOND_ART_FERIEN)).Rows)
            {
                int eigner = r["ID_Zone"] is DBNull ? 0 : Convert.ToInt32(r["ID_Zone"], CultureInfo.InvariantCulture);
                if (!je.TryGetValue(eigner, out HashSet<(int, int)> menge)) je[eigner] = menge = new HashSet<(int, int)>();
                menge.Add((Convert.ToInt32(r["Beginn"], CultureInfo.InvariantCulture), Convert.ToInt32(r["Ende"], CultureInfo.InvariantCulture)));
            }
            return je;
        }

        private static bool Hat(Dictionary<int, HashSet<string>> v, int eigner, string zeile)
            => v.TryGetValue(eigner, out HashSet<string> z) && z.Contains(zeile);

        /// <summary>Die Nutzung des Kalenders der Größe (leer = ohne Profil), auch über „ALLE“; <c>null</c> = keiner.</summary>
        private static string Groesse(Dictionary<string, string> je, string g)
        {
            if (je == null) return null;
            if (je.TryGetValue(g, out string n)) return n;
            return je.TryGetValue(DbWerte.KOND_GROESSE_ALLE, out string a) ? a : null;
        }

        // =====================================================================
        //  Schreiben — je eine Handlung für n Zonen
        // =====================================================================

        /// <summary>
        /// <b>Gibt den gewählten Zonen ein Profil</b> in einem Vorgang — die Kalender kommen je Zone über den Generator
        /// (Fläche und lichte Höhe der Zone). <paramref name="idProfil"/> <c>null</c> heißt: je Zone der Vorschlag der
        /// Profilregel (Stufen bis zum EPOS-Muster, F6); eine Zone ohne Vorschlag bleibt, wie sie ist.
        /// </summary>
        public ZonenNutzungsergebnis ProfilZuweisen(IReadOnlyCollection<int> zonen, long? idProfil)
        {
            List<int> ids = Gewaehlt(zonen);
            if (ids.Count == 0) return Ohne();
            Modellpruefstand s = new ModellpruefungCtrl(IdGebaeude).Lesen();
            if (s == null) return Fehlt();
            string name = null;
            if (idProfil is long id)
            {
                name = Profile().FirstOrDefault(p => p.Id == id)?.Name;
                if (name == null) return new ZonenNutzungsergebnis(false, Modellpruefung.Ressource("ZN_MSG_PROFIL_FEHLT"), 0, Lesen());
            }
            return Schreiben(ids, false, (v, zonenListe) =>
            {
                int n = 0;
                foreach (int idz in zonenListe)
                {
                    ZoneModel z = s.Zonen.FirstOrDefault(x => x.ID == idz);
                    if (z == null) continue;
                    Planprofil p;
                    if (idProfil.HasValue) p = new Planprofil(idProfil, name);
                    else
                    {
                        Pruefvorschlag pv = z.IstBeheizt ? Modellvorschlagsregeln.Profil(s, z) : null;
                        if (pv == null) continue;
                        p = new Planprofil(pv.IdProfil, pv.Name);
                    }
                    string grund = ZonenplanCtrl.NutzungUebernehmen(IdGebaeude, z.ID, p, z.Nutzflaeche, z.Raumhoehe);
                    if (grund != null) throw new InvalidOperationException(grund.Length > 0 ? grund : Modellpruefung.Ressource("ZN_MSG_PROFIL_ABGELEHNT", z.Bezeichner));
                    HandMarkieren(v, z.ID);
                    n++;
                }
                return n;
            });
        }

        /// <summary>
        /// <b>Die Rückfrage vor „Projektvorgabe übernehmen“</b> (E139): nennt die gewählten Zonen mit eigenen Ferien
        /// (Ferienwerte oder abweichende Zeiträume, auch die vom Profilgenerator geschriebenen) samt Herkunft und Zahl der
        /// Zeiträume; <c>null</c>, wenn keine gewählte Zone eigene Ferien trägt.
        /// </summary>
        public string FerienRueckfrage(IReadOnlyCollection<int> zonen)
        {
            List<int> ids = Gewaehlt(zonen);
            if (ids.Count == 0) return null;
            ZonenNutzungsstand s = Lesen();
            if (s == null) return null;
            List<ZonenNutzungszeile> eigen = s.Zonen.Where(z => ids.Contains(z.IdZone) && z.FerienAngaben?.Abweichend == true).ToList();
            if (eigen.Count == 0) return null;
            string liste = string.Join(Environment.NewLine, eigen.Select(z => Modellpruefung.Ressource(
                "ZN_FRAGE_FERIEN_ZONE", z.Zone, Herkunftstext(z.FerienAngaben.Herkunft), z.FerienAngaben.Zeitraeume, z.FerienAngaben.Werte)));
            return Modellpruefung.Ressource("ZN_FRAGE_FERIEN", eigen.Count, liste);
        }

        /// <summary>Der Text einer Ferienherkunft (Profilgenerator, eigen).</summary>
        public static string Herkunftstext(Ferienherkunft h)
            => h == Ferienherkunft.Profilgenerator ? Modellpruefung.Ressource("ZN_FERIEN_HERKUNFT_PROFIL") : Modellpruefung.Ressource("ZN_FERIEN_HERKUNFT_EIGEN");

        /// <summary>
        /// <b>Übernimmt die Projektvorgabe für Ferien und Heizperiode</b> an den gewählten Zonen (E137 F5 (a), E139): ihre
        /// eigenen Zeilen <c>FERIEN</c> und <c>SAISON</c> fallen. Fehlt die Zeile am Gebäude, kommt sie zuvor von einem anderen
        /// Gebäude des Projekts (wie der Vorschlag der Modellprüfung). Trägt eine Zone eigene Ferien
        /// (<see cref="Zonenferien.Abweichend"/>), wird sie nur mit <paramref name="ferienErsetzen"/> übernommen — dann fallen
        /// auch ihre Ferienperioden: je Kalender der Zone treten die Ferienperioden des Gebäudekalenders derselben Größe an
        /// ihre Stelle; ohne solchen Kalender bleiben nur die Perioden mit den Zeiträumen des Projekts. Ohne Zustimmung bleiben
        /// diese Zonen unverändert, die übrigen übernehmen.
        /// </summary>
        public ZonenNutzungsergebnis ProjektvorgabeUebernehmen(IReadOnlyCollection<int> zonen, bool ferienErsetzen = false)
        {
            List<int> ids = Gewaehlt(zonen);
            if (ids.Count == 0) return Ohne();
            Modellpruefstand s = new ModellpruefungCtrl(IdGebaeude).Lesen();
            if (s == null) return Fehlt();
            ZonenNutzungsstand stand = Lesen();
            var eigen = new HashSet<int>(stand.Zonen.Where(z => ids.Contains(z.IdZone) && z.FerienAngaben?.Abweichend == true).Select(z => z.IdZone));
            List<int> behalten = ferienErsetzen ? new List<int>() : ids.Where(eigen.Contains).ToList();
            ids = ids.Where(z => !behalten.Contains(z)).ToList();
            if (ids.Count == 0)
                return new ZonenNutzungsergebnis(true, Modellpruefung.Ressource("ZN_MSG_FERIEN_BEHALTEN", behalten.Count), 0, stand);
            ZonenNutzungsergebnis e = Schreiben(ids, true, (v, zonenListe) =>
            {
                if (!s.FerienBestand && !s.Gebaeudevorgaben.Contains(DbWerte.KOND_ZEILE_FERIEN) && s.FerienQuelle is int qf)
                    GebaeudezeilenKopieren(v, qf, DbWerte.KOND_ZEILE_FERIEN);
                if (!s.Gebaeudevorgaben.Contains(DbWerte.KOND_ZEILE_SAISON) && s.SaisonQuelle is int qs)
                    GebaeudezeilenKopieren(v, qs, DbWerte.KOND_ZEILE_SAISON);
                HashSet<(int, int)> projekt = Ferienzeitraeume().TryGetValue(0, out HashSet<(int, int)> p) ? p : new HashSet<(int, int)>();
                int n = 0;
                foreach (int idz in zonenListe)
                {
                    bool geaendert = v.Ausfuehren("DELETE FROM \"" + KonditionierungSchema.TAB_VORGABE + "\" WHERE \"ID_Zone\" = ? AND \"Zeile\" IN (?, ?)",
                                                  new DbParam("@z", idz), new DbParam("@f", DbWerte.KOND_ZEILE_FERIEN),
                                                  new DbParam("@s", DbWerte.KOND_ZEILE_SAISON)) > 0;
                    if (eigen.Contains(idz) && FerienperiodenErsetzen(v, idz, projekt)) geaendert = true;
                    if (geaendert)
                    {
                        HandMarkieren(v, idz);
                        n++;
                    }
                }
                return n;
            });
            if (!e.Ok || behalten.Count == 0) return e;
            return e with { Meldung = e.Meldung + " " + Modellpruefung.Ressource("ZN_MSG_FERIEN_BEHALTEN", behalten.Count) };
        }

        /// <summary>
        /// Ersetzt die Ferienperioden der Kalender einer Zone durch die des Gebäudekalenders derselben Größe; ohne solchen
        /// Kalender fallen nur die Perioden, deren Zeitraum keiner des Projekts ist. <c>true</c>, wenn sich etwas geändert hat.
        /// </summary>
        private bool FerienperiodenErsetzen(DbVorgang v, int idZone, HashSet<(int, int)> projekt)
        {
            bool geaendert = false;
            DataTable leer = v.Lese("SELECT * FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" WHERE 0");
            List<string> spalten = leer.Columns.Cast<DataColumn>().Select(c => c.ColumnName)
                                       .Where(c => c != "ID" && c != "ID_Kalender").ToList();
            bool mitWoche = spalten.Contains("ID_Woche");
            string liste = string.Join(", ", spalten.Select(c => "\"" + c + "\""));
            string listeP = string.Join(", ", spalten.Select(c => "p.\"" + c + "\""));
            foreach (DataRow k in v.Lese(
                         "SELECT DISTINCT k.\"ID\", k.\"Groesse\" FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" k INNER JOIN \"" +
                         KonditionierungSchema.TAB_PERIODE + "\" p ON p.\"ID_Kalender\" = k.\"ID\" WHERE k.\"ID_Zone\" = ? AND p.\"Art\" = ?",
                         new DbParam("@z", idZone), new DbParam("@a", DbWerte.KOND_ART_FERIEN)).Rows)
            {
                long idKalender = Convert.ToInt64(k["ID"], CultureInfo.InvariantCulture);
                object gk = v.Skalar("SELECT \"ID\" FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID_Gebaeude\" = ? AND " +
                                     "\"ID_Zone\" IS NULL AND \"Groesse\" = ? ORDER BY \"ID\" LIMIT 1",
                                     new DbParam("@g", IdGebaeude), new DbParam("@gr", k["Groesse"]));
                if (gk != null && gk is not DBNull)
                {
                    v.Ausfuehren("DELETE FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" WHERE \"ID_Kalender\" = ? AND \"Art\" = ?",
                                 new DbParam("@k", idKalender), new DbParam("@a", DbWerte.KOND_ART_FERIEN));
                    v.Ausfuehren("INSERT INTO \"" + KonditionierungSchema.TAB_PERIODE + "\" (\"ID_Kalender\", " + liste + ") SELECT ?, " + listeP +
                                 " FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" p WHERE p.\"ID_Kalender\" = ? AND p.\"Art\" = ?" +
                                 (mitWoche ? " AND p.\"ID_Woche\" IS NULL" : "") +
                                 " AND NOT EXISTS (SELECT 1 FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" x WHERE x.\"ID_Kalender\" = ? AND x.\"Rang\" = p.\"Rang\")",
                                 new DbParam("@k", idKalender), new DbParam("@q", gk), new DbParam("@a", DbWerte.KOND_ART_FERIEN),
                                 new DbParam("@k2", idKalender));
                    geaendert = true;
                    continue;
                }
                foreach (DataRow p in v.Lese("SELECT \"ID\", \"Beginn\", \"Ende\" FROM \"" + KonditionierungSchema.TAB_PERIODE +
                                             "\" WHERE \"ID_Kalender\" = ? AND \"Art\" = ?",
                                             new DbParam("@k", idKalender), new DbParam("@a", DbWerte.KOND_ART_FERIEN)).Rows)
                {
                    bool imProjekt = p["Beginn"] is not DBNull && p["Ende"] is not DBNull &&
                                     projekt.Contains((Convert.ToInt32(p["Beginn"], CultureInfo.InvariantCulture), Convert.ToInt32(p["Ende"], CultureInfo.InvariantCulture)));
                    if (imProjekt) continue;
                    v.Ausfuehren("DELETE FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" WHERE \"ID\" = ?", new DbParam("@p", p["ID"]));
                    geaendert = true;
                }
            }
            return geaendert;
        }

        /// <summary>
        /// <b>Kopiert die Kalender der Zone <paramref name="quelle"/></b> auf die gewählten Zonen — ersetzend (Kalender,
        /// Perioden, Wochen, Vorgabezeilen) samt dem Profilnamen an der Zone.
        /// </summary>
        public ZonenNutzungsergebnis KalenderKopieren(int quelle, IReadOnlyCollection<int> zonen)
        {
            List<int> ids = Gewaehlt(zonen).Where(z => z != quelle).ToList();
            if (ids.Count == 0) return Ohne();
            if (!KonditionierungSchema.Lesbar()) return new ZonenNutzungsergebnis(false, Modellpruefung.Ressource("ZN_MSG_OHNE_KALENDER"), 0, Lesen());
            DataTable q = DataRepository.GetDataTable("SELECT \"Nutzungsprofil\" FROM \"Tab_Zone\" WHERE \"ID\" = ? AND \"ID_Gebaeude\" = ?",
                                                      new DbParam("@z", quelle), new DbParam("@g", IdGebaeude));
            if (q.Rows.Count == 0) return new ZonenNutzungsergebnis(false, Modellpruefung.Ressource("ZN_MSG_QUELLE_FEHLT"), 0, Lesen());
            object profil = q.Rows[0][0];
            return Schreiben(ids, false, (v, zonenListe) =>
            {
                int n = 0;
                foreach (int idz in zonenListe)
                {
                    Konditionierungskopie.Befund b = Konditionierungskopie.Kopieren(
                        v, KonditionierungCtrl.Eigner.Zone(IdGebaeude, quelle), KonditionierungCtrl.Eigner.Zone(IdGebaeude, idz),
                        Konditionierungskopie.Auswahl.Ersetzend);
                    if (!b.Ok) throw new InvalidOperationException(b.Meldung);
                    v.Ausfuehren("UPDATE \"Tab_Zone\" SET \"Nutzungsprofil\" = ? WHERE \"ID\" = ?", new DbParam("@n", profil), new DbParam("@z", idz));
                    HandMarkieren(v, idz);
                    n++;
                }
                return n;
            });
        }

        /// <summary>
        /// <b>Setzt die Sollwerte</b> Tag und Nacht der gewählten Zonen (<c>Tab_Zone.Raumsolltemperatur_Tag</c>,
        /// <c>Raumsolltemperatur_Nachtabsenkung</c>); <c>null</c> lässt den Wert, wie er ist. Bereich 5 bis 30 °C, die Nacht
        /// nicht über dem Tag.
        /// </summary>
        public ZonenNutzungsergebnis SollwerteSetzen(IReadOnlyCollection<int> zonen, double? tag, double? nacht)
        {
            List<int> ids = Gewaehlt(zonen);
            if (ids.Count == 0 || (!tag.HasValue && !nacht.HasValue)) return Ohne();
            if ((tag.HasValue && !(tag.Value >= 5.0 && tag.Value <= 30.0)) || (nacht.HasValue && !(nacht.Value >= 5.0 && nacht.Value <= 30.0))
                || (tag.HasValue && nacht.HasValue && nacht.Value > tag.Value))
                return new ZonenNutzungsergebnis(false, Modellpruefung.Ressource("ZN_MSG_SOLLWERT_BEREICH"), 0, Lesen());
            return Schreiben(ids, false, (v, zonenListe) =>
            {
                int n = 0;
                foreach (int idz in zonenListe)
                {
                    if (tag.HasValue)
                        v.Ausfuehren("UPDATE \"Tab_Zone\" SET \"Raumsolltemperatur_Tag\" = ? WHERE \"ID\" = ?", new DbParam("@t", tag.Value), new DbParam("@z", idz));
                    if (nacht.HasValue)
                        v.Ausfuehren("UPDATE \"Tab_Zone\" SET \"Raumsolltemperatur_Nachtabsenkung\" = ? WHERE \"ID\" = ?", new DbParam("@n", nacht.Value), new DbParam("@z", idz));
                    HandMarkieren(v, idz);
                    n++;
                }
                return n;
            });
        }

        /// <summary><b>Nimmt die letzte Mehrfachänderung zurück</b> (ein Vorgang) und liest neu.</summary>
        public ZonenNutzungsergebnis Zuruecknehmen()
        {
            Abbild a = _letztes;
            if (a == null) return new ZonenNutzungsergebnis(false, Modellpruefung.Ressource("ZN_MSG_KEINE_RUECKNAHME"), 0, Lesen());
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(v))
                {
                    a.Herstellen(v);
                    v.Commit();
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return new ZonenNutzungsergebnis(false, Modellpruefung.Ressource("ZN_MSG_FEHLER", ex.Message), 0, Lesen());
            }
            _letztes = null;
            return new ZonenNutzungsergebnis(true, Modellpruefung.Ressource("ZN_MSG_ZURUECKGENOMMEN", a.Zonen.Count), a.Zonen.Count, Lesen());
        }

        // =====================================================================
        //  Gemeinsamer Weg: Abbild, ein Vorgang, neu lesen
        // =====================================================================

        private ZonenNutzungsergebnis Ohne()
            => new ZonenNutzungsergebnis(false, Modellpruefung.Ressource("ZN_MSG_KEINE_WAHL"), 0, Lesen());

        private ZonenNutzungsergebnis Fehlt()
            => new ZonenNutzungsergebnis(false, Modellpruefung.Ressource("MP_MSG_GEBAEUDE_FEHLT", IdGebaeude), 0, null);

        /// <summary>Die gewählten Zonen, die zu diesem Gebäude gehören (in der Reihenfolge der Wahl, ohne Doppelte).</summary>
        private List<int> Gewaehlt(IReadOnlyCollection<int> zonen)
        {
            if (zonen == null || zonen.Count == 0) return new List<int>();
            var eigene = new HashSet<int>(DataRepository.GetDataTable("SELECT \"ID\" FROM \"Tab_Zone\" WHERE \"ID_Gebaeude\" = ?",
                                                                      new DbParam("@g", IdGebaeude)).Rows.Cast<DataRow>()
                                             .Select(r => Convert.ToInt32(r[0], CultureInfo.InvariantCulture)));
            return zonen.Where(eigene.Contains).Distinct().ToList();
        }

        private ZonenNutzungsergebnis Schreiben(List<int> zonen, bool mitGebaeude, Func<DbVorgang, List<int>, int> handlung)
        {
            Abbild abbild = Abbild.Lesen(IdGebaeude, zonen, mitGebaeude);
            int n;
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(v))
                {
                    n = handlung(v, zonen);
                    v.Commit();
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return new ZonenNutzungsergebnis(false, Modellpruefung.Ressource("ZN_MSG_FEHLER", ex.Message), 0, Lesen());
            }
            if (n > 0) _letztes = abbild;
            return new ZonenNutzungsergebnis(true, Modellpruefung.Ressource("ZN_MSG_GEAENDERT", n), n, Lesen());
        }

        /// <summary>
        /// MA-6: Eine Handlung dieses Blatts macht Nutzung und Kalender der Zone zur Ergänzung von Hand — Herkunft
        /// <c>MANUELL</c> am Satz. Der Wiederimport (<see cref="MehrzonenuebernahmeCtrl.Zusammenfuehren"/>) behält dann Profil,
        /// Kalender und Sollwerte der Zone und aktualisiert nur ihre Geometrie. Die Rücknahme stellt die alte Herkunft her.
        /// </summary>
        private static void HandMarkieren(DbVorgang v, int idZone)
            => v.Ausfuehren("UPDATE \"Tab_Zone\" SET \"Herkunft\" = ? WHERE \"ID\" = ?",
                            new DbParam("@h", DbWerte.HERKUNFT_MANUELL), new DbParam("@z", idZone));

        private void GebaeudezeilenKopieren(DbVorgang v, int quelle, string zeile)
            => v.Ausfuehren(
                "INSERT INTO \"" + KonditionierungSchema.TAB_VORGABE + "\" (\"ID_Gebaeude\", \"Groesse\", \"Zeile\", \"Wert\", \"Aus\", \"Von\", \"Bis\", \"Bedingt_K\") " +
                "SELECT ?, \"Groesse\", \"Zeile\", \"Wert\", \"Aus\", \"Von\", \"Bis\", \"Bedingt_K\" FROM \"" + KonditionierungSchema.TAB_VORGABE + "\" " +
                "WHERE \"ID_Gebaeude\" = ? AND \"ID_Zone\" IS NULL AND \"Zeile\" = ?",
                new DbParam("@g", IdGebaeude), new DbParam("@q", quelle), new DbParam("@z", zeile));

        /// <summary>
        /// <b>Das Abbild der Zonen vor einer Änderung</b>: die drei Spalten an <c>Tab_Zone</c>, Kalender, Perioden, Wochen und
        /// Vorgabezeilen je Zone, auf Wunsch die Vorgabezeilen des Gebäudes. <see cref="Herstellen"/> löscht den heutigen
        /// Stand dieser Eigner und schreibt das Abbild mit seinen alten Ids zurück.
        /// </summary>
        private sealed class Abbild
        {
            private readonly int _gebaeude;
            private readonly bool _mitGebaeude;
            private readonly List<(string Tabelle, DataTable Zeilen)> _zeilen = new List<(string, DataTable)>();
            private DataTable _zonenspalten;

            private Abbild(int gebaeude, List<int> zonen, bool mitGebaeude)
            {
                _gebaeude = gebaeude;
                Zonen = zonen;
                _mitGebaeude = mitGebaeude;
            }

            internal List<int> Zonen { get; }

            private static string Liste(int n) => string.Join(", ", Enumerable.Repeat("?", n));

            private static DbParam[] Parameter(IEnumerable<int> ids) => ids.Select((id, i) => new DbParam("@i" + i, id)).ToArray();

            internal static Abbild Lesen(int gebaeude, List<int> zonen, bool mitGebaeude)
            {
                var a = new Abbild(gebaeude, zonen, mitGebaeude);
                string inZonen = "(" + Liste(zonen.Count) + ")";
                a._zonenspalten = DataRepository.GetDataTable("SELECT \"ID\", " + SPALTEN_ZONE + " FROM \"Tab_Zone\" WHERE \"ID\" IN " + inZonen, Parameter(zonen));
                if (!KonditionierungSchema.Lesbar()) return a;
                if (DataRepository.TabelleVorhanden(TAB_WOCHE))
                    a._zeilen.Add((TAB_WOCHE, DataRepository.GetDataTable("SELECT * FROM \"" + TAB_WOCHE + "\" WHERE \"ID_Zone\" IN " + inZonen, Parameter(zonen))));
                a._zeilen.Add((KonditionierungSchema.TAB_KALENDER, DataRepository.GetDataTable(
                    "SELECT * FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID_Zone\" IN " + inZonen, Parameter(zonen))));
                a._zeilen.Add((KonditionierungSchema.TAB_PERIODE, DataRepository.GetDataTable(
                    "SELECT p.* FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" p INNER JOIN \"" + KonditionierungSchema.TAB_KALENDER +
                    "\" k ON k.\"ID\" = p.\"ID_Kalender\" WHERE k.\"ID_Zone\" IN " + inZonen, Parameter(zonen))));
                a._zeilen.Add((KonditionierungSchema.TAB_VORGABE, DataRepository.GetDataTable(
                    "SELECT * FROM \"" + KonditionierungSchema.TAB_VORGABE + "\" WHERE \"ID_Zone\" IN " + inZonen +
                    (mitGebaeude ? " OR (\"ID_Gebaeude\" = ? AND \"ID_Zone\" IS NULL)" : ""),
                    mitGebaeude ? Parameter(zonen).Concat(new[] { new DbParam("@g", gebaeude) }).ToArray() : Parameter(zonen))));
                return a;
            }

            internal void Herstellen(DbVorgang v)
            {
                string inZonen = "(" + Liste(Zonen.Count) + ")";
                if (KonditionierungSchema.Lesbar())
                {
                    v.Ausfuehren("DELETE FROM \"" + KonditionierungSchema.TAB_PERIODE + "\" WHERE \"ID_Kalender\" IN (SELECT \"ID\" FROM \"" +
                                 KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID_Zone\" IN " + inZonen + ")", Parameter(Zonen));
                    v.Ausfuehren("DELETE FROM \"" + KonditionierungSchema.TAB_KALENDER + "\" WHERE \"ID_Zone\" IN " + inZonen, Parameter(Zonen));
                    if (DataRepository.TabelleVorhanden(TAB_WOCHE))
                        v.Ausfuehren("DELETE FROM \"" + TAB_WOCHE + "\" WHERE \"ID_Zone\" IN " + inZonen, Parameter(Zonen));
                    v.Ausfuehren("DELETE FROM \"" + KonditionierungSchema.TAB_VORGABE + "\" WHERE \"ID_Zone\" IN " + inZonen, Parameter(Zonen));
                    if (_mitGebaeude)
                        v.Ausfuehren("DELETE FROM \"" + KonditionierungSchema.TAB_VORGABE + "\" WHERE \"ID_Gebaeude\" = ? AND \"ID_Zone\" IS NULL",
                                     new DbParam("@g", _gebaeude));
                    foreach ((string tabelle, DataTable t) in _zeilen)
                    {
                        string spalten = string.Join(", ", t.Columns.Cast<DataColumn>().Select(c => "\"" + c.ColumnName + "\""));
                        string sql = "INSERT INTO \"" + tabelle + "\" (" + spalten + ") VALUES (" + Liste(t.Columns.Count) + ")";
                        foreach (DataRow r in t.Rows)
                            v.Ausfuehren(sql, t.Columns.Cast<DataColumn>().Select((c, i) => new DbParam("@p" + i, r[c] ?? DBNull.Value)).ToArray());
                    }
                }
                foreach (DataRow r in _zonenspalten.Rows)
                    v.Ausfuehren("UPDATE \"Tab_Zone\" SET \"Nutzungsprofil\" = ?, \"Raumsolltemperatur_Tag\" = ?, \"Raumsolltemperatur_Nachtabsenkung\" = ?, \"Herkunft\" = ? WHERE \"ID\" = ?",
                                 new DbParam("@n", r["Nutzungsprofil"]), new DbParam("@t", r["Raumsolltemperatur_Tag"]),
                                 new DbParam("@a", r["Raumsolltemperatur_Nachtabsenkung"]), new DbParam("@h", r["Herkunft"]), new DbParam("@z", r["ID"]));
            }
        }
    }
}
