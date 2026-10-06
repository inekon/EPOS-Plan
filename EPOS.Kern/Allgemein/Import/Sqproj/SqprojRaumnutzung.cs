using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>Ein Nutzungsprofil der Projektdatei als Raumnutzungsprofil samt der Zahl der Zonen, die es tragen (NP4b).</summary>
    public sealed class Projektdateiprofil
    {
        /// <summary>Das abgebildete Profil (ohne Id und Kategorie; die Übernahme setzt beides).</summary>
        public Raumnutzungsprofil Profil { get; init; }

        /// <summary>Wie viele Zonen der Projektdatei tragen diese DIN-Nummer?</summary>
        public int Zonen { get; init; }
    }

    /// <summary>
    /// <b>Die Nutzungsprofile einer Projektdatei für eine eigene Kategorie</b> (Konzept Nutzungsprofile Q46, Stufe NP4b): je
    /// benutzter DIN-Profilnummer ein Raumnutzungsprofil, dazu Name und Quellenhinweis der Kategorie „Projektdatei
    /// &lt;Dateiname&gt;" und die Meldungen der Abbildung. Rein, ohne Datenbank; geschrieben wird erst mit
    /// <see cref="RaumnutzungCtrl.ProjektdateiUebernehmen"/>. Die Werte stammen aus der lizenzierten Software des Anwenders —
    /// sie landen nur in seiner Datenbank, nie im Repositorium, in einer Saat oder in der Auslieferung (NP-F21).
    /// </summary>
    public sealed class Projektdateiprofile
    {
        /// <summary>Der Dateiname ohne Pfad.</summary>
        public string Dateiname { get; init; } = "";

        /// <summary>Der Name der Zielkategorie („Projektdatei &lt;Dateiname&gt;", höchstens 80 Zeichen).</summary>
        public string Kategorie { get; init; } = "";

        /// <summary>Der Quellenhinweis der Zielkategorie.</summary>
        public string Quellenhinweis { get; init; }

        /// <summary>Die Profile, nach DIN-Nummer aufsteigend.</summary>
        public IReadOnlyList<Projektdateiprofil> Profile { get; init; } = Array.Empty<Projektdateiprofil>();

        /// <summary>Die Meldungen der Abbildung (Umrechnung, nicht Übernommenes) als Text der Oberflächensprache.</summary>
        public IReadOnlyList<string> Meldungen { get; init; } = Array.Empty<string>();

        /// <summary>Die benannte Ablehnung (keine Projektdatei, zu groß, nicht lesbar); <c>null</c> = gelesen.</summary>
        public string Ablehnung { get; init; }

        /// <summary>Ist die Datei abgelehnt?</summary>
        public bool Abgelehnt => Ablehnung != null;
    }

    /// <summary>
    /// <b>Bildet die DIN-V-18599-10-Nutzungsprofile einer Projektdatei auf Raumnutzungsprofile ab</b> (Q46, NP4b; Befund B10):
    /// je benutzter Profilnummer (<c>PdProfileUsage.ProfileUsageType</c> an mindestens einer Zone) Nummer, Name und die Werte
    /// des Spaltensatzes in den Einheiten von Konzept 4.1.
    /// <list type="bullet">
    /// <item><b>Zeiten:</b> Nutzungszeit aus <c>PeriodOfOperationFrom/To</c>, Betrieb aus <c>HeatedFrom/To</c>, wo er von der
    /// Nutzungszeit abweicht; eine fehlende Grenze ist Mitternacht (die Nullzeit der Datei, 0 bzw. 24 Uhr).</item>
    /// <item><b>Heizen:</b> <c>Heiz_Soll</c> aus <c>NominalRoomTemperature</c>, <c>Heiz_Soll_Ausserhalb</c> = Sollwert −
    /// <c>DropOfTemperatureSetback</c> (wie die CSV-Spalte <c>Heiz_Absenkung_K</c>, Konzept 6.4).</item>
    /// <item><b>Außenluft:</b> <c>SupplyAirChange</c> in 1/h, sonst <c>MinimumExternalAirFlowBasedOnArea</c> in m³/(h·m²);
    /// die Außenluft je Person bleibt ohne Belegungsdichte draußen (benannt).</item>
    /// <item><b>Personen:</b> die flächenbezogene Wärmeabgabe als m² je Person bei <see cref="Matrixeingang.PERSON_W"/> W je
    /// Person (derselbe Nennwert je Fläche, benannt); Anteil aus den Vollnutzungsstunden je Tag an der Nutzungszeit.</item>
    /// <item><b>Geräte:</b> Leistung in W/m², Anteil wie bei Personen.</item>
    /// <item><b>Beleuchtung:</b> Die Datei führt die Beleuchtungsstärke in lx, das Profil eine Leistung in W/m² — der Wert
    /// steht in der Beschreibung, die Leistung bleibt leer (benannt).</item>
    /// </list>
    /// Werte außerhalb der Grenzen der Größe werden nicht übernommen und benannt; zwei Profile der Datei mit derselben
    /// Nummer und verschiedenen Werten: das erste in fester Reihenfolge gilt, benannt.
    /// </summary>
    internal static class SqprojRaumnutzung
    {
        /// <summary>Die Nachkommastellen der umgerechneten Fläche je Person.</summary>
        private const int STELLEN_FLAECHE = 3;

        /// <summary>Die Nachkommastellen der Anteile.</summary>
        private const int STELLEN_ANTEIL = 4;

        /// <summary>Der Name der Zielkategorie zu einer Datei, auf 80 Zeichen gekürzt.</summary>
        internal static string Kategoriename(string dateiname)
        {
            string name = GebaeudeQuelle.NurName(dateiname ?? "").Trim();
            string k = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RNP_PD_KATEGORIE, name);
            if (k.Length <= RaumnutzungSchema.BEZEICHNER_MAX_ZEICHEN) return k;
            int ueber = k.Length - RaumnutzungSchema.BEZEICHNER_MAX_ZEICHEN;
            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RNP_PD_KATEGORIE,
                                 name.Substring(0, Math.Max(1, name.Length - ueber)));
        }

        /// <summary>
        /// <b>Liest eine Projektdatei aus einem Strom</b> über eine Arbeitskopie im Temp-Ordner (gelesen wird allein die Kopie,
        /// sie wird danach gelöscht) und bildet ihre Profile ab. Wirft nur beim Abbruch; jede Ablehnung kommt benannt zurück.
        /// </summary>
        internal static Projektdateiprofile Lesen(Stream quelle, string dateiname, long maxBytes, CancellationToken abbruch = default)
        {
            string name = GebaeudeQuelle.NurName(dateiname ?? "");
            Projektdateiprofile Ab(string schluessel, params string[] werte)
                => new Projektdateiprofile
                {
                    Dateiname = name, Kategorie = Kategoriename(name),
                    Ablehnung = GebaeudeZuordnungsModell.MeldungText(new PruefMeldung(PruefStufe.Fehler, schluessel, werte)),
                };
            if (quelle == null) return Ab(SqprojProtokoll.KEINE_DATEI, name);
            long grenze = maxBytes > 0 ? maxBytes : long.MaxValue;
            if (quelle.CanSeek && quelle.Length - quelle.Position > grenze)
                return Ab(SqprojProtokoll.ZU_GROSS, SqprojProtokoll.Mb(quelle.Length - quelle.Position), SqprojProtokoll.Mb(grenze));

            string ordner = Path.Combine(Path.GetTempPath(), "epos-sqproj");
            string kopie = Path.Combine(ordner, Path.GetRandomFileName() + ".sqproj");
            try
            {
                Directory.CreateDirectory(ordner);
                long bytes = 0;
                using (var ziel = new FileStream(kopie, FileMode.CreateNew, FileAccess.Write))
                {
                    var block = new byte[81920];
                    int n;
                    while ((n = quelle.Read(block, 0, block.Length)) > 0)
                    {
                        abbruch.ThrowIfCancellationRequested();
                        bytes += n;
                        if (bytes > grenze) return Ab(SqprojProtokoll.ZU_GROSS, SqprojProtokoll.Mb(bytes), SqprojProtokoll.Mb(grenze));
                        ziel.Write(block, 0, n);
                    }
                }
                SqprojAbbild a = SqprojLeser.Lesen(kopie);
                if (a.Abgelehnt)
                    return new Projektdateiprofile
                    {
                        Dateiname = name, Kategorie = Kategoriename(name), Ablehnung = GebaeudeZuordnungsModell.MeldungText(a.Ablehnung),
                    };
                return Bilden(a, name);
            }
            catch (IOException ex)
            {
                return Ab(SqprojProtokoll.LESEFEHLER, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Ab(SqprojProtokoll.LESEFEHLER, ex.Message);
            }
            finally
            {
                try
                {
                    if (File.Exists(kopie)) File.Delete(kopie);
                }
                catch (IOException) { /* bleibt im Temp-Ordner, der nächste Lauf stört sich nicht daran */ }
                catch (UnauthorizedAccessException) { }
            }
        }

        /// <summary>
        /// <b>Bildet die Profile eines gelesenen Abbilds ab</b> — je benutzter DIN-Nummer eines, nach Nummer aufsteigend; ein
        /// abgelehntes Abbild ergibt die Ablehnung.
        /// </summary>
        internal static Projektdateiprofile Bilden(SqprojAbbild abbild, string dateiname)
        {
            string name = GebaeudeQuelle.NurName(dateiname ?? "");
            if (abbild == null || abbild.Abgelehnt)
                return new Projektdateiprofile
                {
                    Dateiname = name, Kategorie = Kategoriename(name),
                    Ablehnung = GebaeudeZuordnungsModell.MeldungText(abbild?.Ablehnung
                        ?? new PruefMeldung(PruefStufe.Fehler, SqprojProtokoll.NICHT_GELESEN)),
                };

            var meldungen = new List<string>();
            int ohneNummer = abbild.Zonen.Count(z => z.Nutzungsprofil != null && !z.Nutzungsprofil.Profilnummer.HasValue);
            var profile = new List<Projektdateiprofil>();
            var namen = new List<string>();
            foreach (IGrouping<int, SqprojZone> gruppe in abbild.Zonen
                         .Where(z => z.Nutzungsprofil?.Profilnummer != null)
                         .GroupBy(z => z.Nutzungsprofil.Profilnummer.Value)
                         .OrderBy(g => g.Key))
            {
                List<SqprojNutzungsprofil> quellen = gruppe.Select(z => z.Nutzungsprofil)
                                                           .GroupBy(p => p.Uuid, StringComparer.OrdinalIgnoreCase)
                                                           .Select(g => g.First()).ToList();
                var eigene = new List<string>();
                Raumnutzungsprofil p = Abbilden(quellen[0], name, eigene);
                int abweichend = quellen.Skip(1).Count(q => !Abbilden(q, name, new List<string>()).Kennwerte().SequenceEqual(p.Kennwerte()));
                if (abweichend > 0)
                    eigene.Add(Text(MyResource.Resource.RNP_PD_MSG_ABWEICHEND, p.Nummer, Z(abweichend + 1), quellen[0].Name ?? ""));
                p.Bezeichner = KonditionierungsvorlageCtrl.EindeutigerName(namen, p.Bezeichner);
                namen.Add(p.Bezeichner);
                meldungen.AddRange(eigene);
                profile.Add(new Projektdateiprofil { Profil = p, Zonen = gruppe.Select(z => z.Uuid).Distinct(StringComparer.OrdinalIgnoreCase).Count() });
            }
            if (ohneNummer > 0) meldungen.Add(Text(MyResource.Resource.RNP_PD_MSG_OHNE_NUMMER, Z(ohneNummer)));
            if (profile.Count == 0) meldungen.Insert(0, MyResource.Resource.RNP_PD_KEINE);
            string quelle = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.RNP_PD_QUELLE, name);
            return new Projektdateiprofile
            {
                Dateiname = name,
                Kategorie = Kategoriename(name),
                Quellenhinweis = quelle.Length <= RaumnutzungSchema.BESCHREIBUNG_MAX_ZEICHEN ? quelle : quelle.Substring(0, RaumnutzungSchema.BESCHREIBUNG_MAX_ZEICHEN),
                Profile = profile,
                Meldungen = meldungen,
            };
        }

        /// <summary>Bildet ein Nutzungsprofil der Datei ab; was nicht übernommen oder umgerechnet wird, steht in <paramref name="meldungen"/>.</summary>
        internal static Raumnutzungsprofil Abbilden(SqprojNutzungsprofil q, string dateiname, List<string> meldungen)
        {
            if (q == null) throw new ArgumentNullException(nameof(q));
            string nr = q.Profilnummer?.ToString(CultureInfo.InvariantCulture);
            string bezeichner = (q.Name ?? "").Trim();
            if (bezeichner.Length == 0) bezeichner = Text(MyResource.Resource.RNP_PD_NAME_OHNE, nr ?? "—");
            if (bezeichner.Length > RaumnutzungSchema.BEZEICHNER_MAX_ZEICHEN) bezeichner = bezeichner.Substring(0, RaumnutzungSchema.BEZEICHNER_MAX_ZEICHEN).TrimEnd();
            string beschreibung = Text(MyResource.Resource.RNP_PD_BESCHREIBUNG, GebaeudeQuelle.NurName(dateiname ?? ""), nr ?? "—");
            if (q.Beleuchtungsstaerke is double lx)
            {
                beschreibung += Text(MyResource.Resource.RNP_PD_BESCHREIBUNG_LUX, Z(lx));
                meldungen.Add(Text(MyResource.Resource.RNP_PD_MSG_BELEUCHTUNG, nr, Z(lx)));
            }
            if (beschreibung.Length > RaumnutzungSchema.BESCHREIBUNG_MAX_ZEICHEN) beschreibung = beschreibung.Substring(0, RaumnutzungSchema.BESCHREIBUNG_MAX_ZEICHEN);
            var p = new Raumnutzungsprofil { Nummer = nr, Bezeichner = bezeichner, Beschreibung = beschreibung };

            // Zeiten: Nutzung aus der Betriebszeit der Norm-Tabelle, Betrieb aus der Heizzeit, wo sie abweicht.
            (p.Nutzung_Von, p.Nutzung_Bis) = Fenster(q.BetriebVon, q.BetriebBis);
            (int? hv, int? hb) = Fenster(q.HeizVon, q.HeizBis);
            if (hv.HasValue && (hv != p.Nutzung_Von || hb != p.Nutzung_Bis)) (p.Betrieb_Von, p.Betrieb_Bis) = (hv, hb);

            // Heizen: Sollwert und Absenkung (Heiz_Soll_Ausserhalb = Sollwert − Absenkung, wie Heiz_Absenkung_K der CSV).
            if (q.Raumtemperatur is double t)
            {
                p.Heiz_Soll = Grenze(Konditionierungsgroesse.Heizsoll, t, "NominalRoomTemperature", nr, meldungen);
                if (q.Absenkung is double ab && p.Heiz_Soll is double soll)
                    p.Heiz_Soll_Ausserhalb = Grenze(Konditionierungsgroesse.Heizsoll, Math.Round(soll - ab, 2), "DropOfTemperatureSetback", nr, meldungen);
            }
            else if (q.Absenkung.HasValue)
                meldungen.Add(Text(MyResource.Resource.RNP_PD_MSG_ABSENKUNG, nr));

            // Außenluft: Luftwechsel vor flächenbezogener Außenluft; je Person nur mit Belegungsdichte (keine).
            if (q.Zuluftwechsel is double n)
            {
                p.Aussenluft = n;
                p.Aussenluft_Einheit = RaumnutzungSchema.EINHEIT_JE_STUNDE;
            }
            else
            {
                if (q.AussenluftJeFlaeche is double f)
                {
                    p.Aussenluft = f;
                    p.Aussenluft_Einheit = RaumnutzungSchema.EINHEIT_JE_FLAECHE;
                }
                if (q.AussenluftJePerson.HasValue) meldungen.Add(Text(MyResource.Resource.RNP_PD_MSG_LUFT_PERSON, nr));
            }

            // Personen: W/m² als m² je Person bei der Wärmeabgabe des Hauses; Anteil aus Vollnutzungsstunden.
            if (q.PersonenWm2 is double pw && pw > 0.0)
            {
                p.Personen_Waerme = Matrixeingang.PERSON_W;
                p.Personen_Flaeche = Math.Round(Matrixeingang.PERSON_W / pw, STELLEN_FLAECHE, MidpointRounding.AwayFromZero);
                p.Personen_Anteil = Anteil(q.VollnutzungPersonenH, q.Betriebsstunden, "DailyEffectiveLoadHoursOfPersons", nr, meldungen);
                meldungen.Add(Text(MyResource.Resource.RNP_PD_MSG_PERSONEN, nr, Z(pw), Z(p.Personen_Flaeche.Value), Z(Matrixeingang.PERSON_W)));
            }
            else if (q.Personenzahl.HasValue)
                meldungen.Add(Text(MyResource.Resource.RNP_PD_MSG_PERSONENZAHL, nr));

            // Geräte: Leistung je Fläche, Anteil aus Vollnutzungsstunden.
            if (q.GeraeteWm2 is double gw)
            {
                p.Geraete_Leistung = gw;
                p.Geraete_Anteil = Anteil(q.VollnutzungGeraeteH, q.Betriebsstunden, "DailyEffectiveLoadHoursOfDevices", nr, meldungen);
            }
            return p;
        }

        /// <summary>Das Zeitfenster: fehlt eine Grenze, ist es Mitternacht (0 bzw. 24 Uhr); beide leer oder gleich = kein Fenster.</summary>
        internal static (int? Von, int? Bis) Fenster(int? von, int? bis)
        {
            if (!von.HasValue && !bis.HasValue) return (null, null);
            int v = von ?? 0, b = bis ?? 24;
            return v == b % 24 ? (null, null) : (v, b);
        }

        private static double? Anteil(double? vollH, int? betriebsstunden, string spalte, string nr, List<string> meldungen)
        {
            if (!(vollH is double v) || !(betriebsstunden is int h) || h <= 0) return null;
            double a = Math.Round(v / h, STELLEN_ANTEIL, MidpointRounding.AwayFromZero);
            if (a <= 1.0) return a;
            meldungen.Add(Text(MyResource.Resource.RNP_PD_MSG_BEGRENZT, nr, spalte));
            return 1.0;
        }

        private static double? Grenze(Konditionierungsgroesse g, double w, string spalte, string nr, List<string> meldungen)
        {
            if (Konditionierungsgroessen.ImBereich(g, w)) return w;
            meldungen.Add(Text(MyResource.Resource.RNP_PD_MSG_BEGRENZT, nr, spalte));
            return null;
        }

        private static string Text(string muster, params string[] werte) => string.Format(CultureInfo.CurrentCulture, muster, werte);

        private static string Z(double w) => w.ToString("0.###", CultureInfo.CurrentCulture);

        private static string Z(int w) => w.ToString(CultureInfo.CurrentCulture);
    }
}
