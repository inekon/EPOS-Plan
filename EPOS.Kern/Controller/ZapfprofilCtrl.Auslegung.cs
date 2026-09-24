using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading;

namespace WindowsFormsApplication1
{
    /// <summary>Woher die Liste der Nenninhalte einer Auslegung kommt (Stufe Z2, Gruppe 2).</summary>
    internal enum Nenninhaltsquelle
    {
        /// <summary>Weder Einstellung noch Parameter — keine Rundung.</summary>
        Keine = 0,

        /// <summary>Die Einstellung <c>Zapfprofil.Nenninhalte</c> (<see cref="Dienste.Einstellungen"/>).</summary>
        Einstellung = 1,

        /// <summary>Die Vorgabe des Parametersatzes (<c>Speicherauslegung.Nenninhalt.Liste.{k}</c>).</summary>
        Parameter = 2
    }

    /// <summary>Die Nenninhalte einer Auslegung samt Quelle; <see cref="Hinweis"/> nennt eine verworfene Angabe.</summary>
    internal sealed record Nenninhaltswahl(Nenninhaltsliste Liste, Nenninhaltsquelle Quelle, Auslegungshinweis Hinweis);

    /// <summary>
    /// Der Anlagenbestand des Projekts, soweit er die Erzeugerart am Speicher nennt
    /// (<c>Tab_WP</c>, <c>Tab_Heizkessel</c>). Einen Vorschlag gibt es nur, wenn genau eine Art
    /// vorkommt — bei beiden bleibt die Wahl beim Anwender, statt still eine zu nehmen.
    /// </summary>
    internal sealed record ZapfErzeugerbestand(int Waermepumpen, int Kessel)
    {
        /// <summary>Die Erzeugerart des Bestands; <c>null</c> ohne Erzeuger oder bei beiden Arten.</summary>
        internal ZapfErzeugerart? Vorschlag
            => Waermepumpen > 0 && Kessel == 0 ? ZapfErzeugerart.Waermepumpe
             : Kessel > 0 && Waermepumpen == 0 ? ZapfErzeugerart.Kessel
             : (ZapfErzeugerart?)null;

        /// <summary>Führt der Bestand Wärmepumpen UND Kessel?</summary>
        internal bool Mehrdeutig => Waermepumpen > 0 && Kessel > 0;
    }

    /// <summary>
    /// Die Laufangaben der Auslegung ohne eigene Spalte (N10 (i)): Erzeugerart und Werkstoff des
    /// Übertragers. <c>null</c> = keine Angabe; die Erzeugerart fällt dann auf den Vorschlag des
    /// Anlagenbestands zurück (<see cref="ZapfErzeugerbestand.Vorschlag"/>), der Werkstoff nie.
    /// Dazu „Stochastisch rechnen" (4.5 b, Stufe Z3): zieht je Topologiegruppe das Ensemble des
    /// Bedarfstags (<see cref="Auslegungseingang.Stochastisch"/>); Seed, Perzentil und
    /// Realisierungen kommen aus den Projektgrößen des Stands. Die Abbruchmarke des nebenläufigen
    /// Laufs (5.1) reicht bis in die Ziehung (<see cref="Auslegungseingang.Abbruch"/>). Die Stufe des
    /// Dialogs (Z4, N11 (c)) setzt im Kern die Marke „Schnellauslegung" (<see cref="Auslegungseingang.Stufe"/>);
    /// <c>null</c> = ein Lauf ohne Dialog.
    /// </summary>
    internal sealed record Auslegungslauf(ZapfErzeugerart? Erzeugerart, ZapfUebertragerwerkstoff? Werkstoff,
                                          bool Stochastisch = false, CancellationToken Abbruch = default,
                                          ZapfStufe? Stufe = null);

    /// <summary>
    /// Das Ergebnis eines Auslegungslaufs samt den Angaben, aus denen er rechnete: Nenninhalte,
    /// Anlagenbestand, die angesetzte Erzeugerart und der Parametersatz der Katalogversion.
    /// </summary>
    internal sealed record Auslegungsrechnung(Auslegungsergebnis Ergebnis, Nenninhaltswahl Nenninhalte,
                                              ZapfErzeugerbestand Bestand, ZapfErzeugerart? Erzeugerart,
                                              Parametersatz Parameter);

    /// <summary>
    /// <b>Die Datenseite der Auslegung</b> (Umsetzungskonzept Zapfprofilgenerator 3.3, 4.5, 4.7;
    /// Stufe Z2, Gruppe 2): die Leser für Bedarfstage (<c>Tab_TwwBedarfstag_STAMM</c> samt
    /// Ereignissen) und die DIN-4708-Ausstattungen (<c>Tab_TwwDin4708Wert_STAMM</c>), die
    /// Nenninhalte aus der Einstellung <c>Zapfprofil.Nenninhalte</c> mit der Vorgabe des
    /// Parametersatzes, der Anlagenbestand als Vorschlag der Erzeugerart, der Konstruktor und der
    /// Schreibweg des konstruierten Tags.
    ///
    /// <para><b>Der konstruierte Bedarfstag</b> ist bis zum Speichern ein Entwurf
    /// (<see cref="ZapfprofilStand.BedarfstagEntwurf"/>, Id <see cref="ENTWURF_ID"/>) und gilt bei
    /// θ_KW,A des Parametersatzes (N10 (j), Folge): Die Auslegung rechnet ihn wie jeden Katalogtag
    /// auf das θ_KW,A des Projekts um. <see cref="Speichern(int, ZapfprofilStand, DbVorgang)"/>
    /// legt ihn im Vorgang des Aufrufers als Katalogzeile an — Status <c>EIGEN</c>, Herkunftsart
    /// <c>EIGENKONSTRUKTION</c>, neutrale Quelle, Quelle_Art Konstruktor — und hängt die
    /// Projektzeile an ihn.</para>
    ///
    /// <para><b>Erzeugerart und Werkstoff</b> tragen keine Spalte in <c>Tab_TwwProjekt</c>; sie
    /// sind Laufangaben (<see cref="Auslegungslauf"/>) und werden nicht gespeichert — kein
    /// Schemaschritt in dieser Stufe (N10 (i), Folge für Z4).</para>
    ///
    /// <para>Alle Zugriffe über <see cref="DataRepository"/> bzw. den <see cref="DbVorgang"/> mit
    /// <c>?</c>-Parametern.</para>
    /// </summary>
    internal static partial class ZapfprofilCtrl
    {
        /// <summary>Der Schlüssel der Einstellung mit der Liste der Nenninhalte (Konzept 3.1).</summary>
        internal const string EINSTELLUNG_NENNINHALTE = "Zapfprofil.Nenninhalte";

        /// <summary>Die Id eines konstruierten, noch nicht gespeicherten Bedarfstags.</summary>
        internal const int ENTWURF_ID = -1;

        /// <summary>Kennung des Hinweises: Die Einstellung der Nenninhalte ist keine gültige Liste.</summary>
        internal const string HINWEIS_NENNINHALTE_EINSTELLUNG = "NENNINHALTE_EINSTELLUNG_UNGUELTIG";

        /// <summary>Kennung des Hinweises: Die Nenninhalte des Parametersatzes sind keine gültige Liste.</summary>
        internal const string HINWEIS_NENNINHALTE_PARAMETER = "NENNINHALTE_PARAMETER_UNGUELTIG";

        // =================================================================================
        // Leser
        // =================================================================================

        /// <summary>
        /// Alle Bedarfstage des Katalogs samt Ereignissen, geordnet nach Bezeichner und
        /// Katalogversion — zwei Abfragen. Ohne Tabellen eine leere Liste.
        /// </summary>
        internal static IReadOnlyList<BedarfstagKatalogzeile> Bedarfstage()
        {
            if (!DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_BEDARFSTAG_STAMM)
                || !DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM))
                return new BedarfstagKatalogzeile[0];

            var ereignisse = new Dictionary<int, List<Zapfereignis>>();
            DataTable de = DataRepository.GetDataTable(
                "SELECT ID_Bedarfstag, Minute_Beginn, Dauer_min, Energie_Kwh FROM " + TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM +
                " ORDER BY ID_Bedarfstag, Reihenfolge, ID");
            if (de != null)
                foreach (DataRow r in de.Rows)
                {
                    int tag = Ganz(r, "ID_Bedarfstag");
                    if (!ereignisse.TryGetValue(tag, out List<Zapfereignis> l)) ereignisse[tag] = l = new List<Zapfereignis>();
                    l.Add(new Zapfereignis(Ganz(r, "Minute_Beginn"), Ganz(r, "Dauer_min"), Zahl(r, "Energie_Kwh")));
                }

            var liste = new List<BedarfstagKatalogzeile>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner, Katalogversion, Quelle_Art, Bezugsmenge, Quelle, Ausgabe, Version, Herkunftsart, " +
                "Status, ReadOnly FROM " + TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + " ORDER BY Bezeichner, Katalogversion, ID");
            if (dt != null)
                foreach (DataRow r in dt.Rows)
                {
                    int id = Ganz(r, "ID");
                    liste.Add(new BedarfstagKatalogzeile(id, Text(r, "Bezeichner"), Text(r, "Katalogversion"),
                        (ZapfBedarfstagquelle)Ganz(r, "Quelle_Art"), ZahlOderNull(r, "Bezugsmenge"), Herkunft(r, ""),
                        ereignisse.TryGetValue(id, out List<Zapfereignis> e) ? e.AsReadOnly()
                                                                               : (IReadOnlyList<Zapfereignis>)new Zapfereignis[0])
                    {
                        Status = TwwWertemengen.Status(Text(r, "Status")),
                        ReadOnly = Wahr(r, "ReadOnly")
                    });
                }
            return liste.AsReadOnly();
        }

        /// <summary>
        /// Die Ausstattungsklassen der DIN-4708-Kennzahl (Art <c>AUSSTATTUNG</c>) einer
        /// Katalogversion: Id, neutraler Schlüssel, Σ v·w_v [Wh] — nie abgedruckt.
        /// </summary>
        internal static IReadOnlyList<Din4708Ausstattung> Ausstattungen(string katalogversion)
        {
            if (string.IsNullOrEmpty(katalogversion)
                || !DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_DIN4708_WERT_STAMM))
                return new Din4708Ausstattung[0];

            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, Schluessel, Wert FROM " + TwwSchema.TAB_TWW_DIN4708_WERT_STAMM +
                " WHERE Art = ? AND Katalogversion = ? ORDER BY Schluessel, ID",
                new DbParam("@art", TwwSchema.DIN4708_ART_AUSSTATTUNG), new DbParam("@version", katalogversion));
            var liste = new List<Din4708Ausstattung>();
            if (dt != null)
                foreach (DataRow r in dt.Rows)
                    liste.Add(new Din4708Ausstattung(Ganz(r, "ID"), Text(r, "Schluessel"), Zahl(r, "Wert")));
            return liste.AsReadOnly();
        }

        /// <summary>Die Katalogwerte der DIN-4708-Kennzahl einer Katalogversion: Belegung je Raumzahl und Ausstattungen.</summary>
        internal static Din4708Katalog Din4708Katalog(string katalogversion)
            => new Din4708Katalog(Belegung(katalogversion), Ausstattungen(katalogversion));

        /// <summary>
        /// Die Nenninhalte der Auslegung: die Einstellung <c>Zapfprofil.Nenninhalte</c>
        /// (Zahlen in Litern, getrennt durch Semikolon oder Leerraum), sonst die Vorgabe des
        /// Parametersatzes (<see cref="Nenninhaltsliste.AusParametern"/>). Eine ungültige
        /// Einstellung wird benannt verworfen (Hinweis) — es gilt dann die Vorgabe; ohne beides
        /// keine Liste (die Auslegung rundet dann nicht und sagt es).
        /// </summary>
        internal static Nenninhaltswahl Nenninhalte(Parametersatz ps)
        {
            Auslegungshinweis hinweis = null;
            string text = null;
            try { text = Dienste.Einstellungen.Lies(EINSTELLUNG_NENNINHALTE, null); }
            catch (Exception) { text = null; }
            if (!string.IsNullOrWhiteSpace(text))
            {
                Nenninhaltsliste ausEinstellung = NenninhalteLesen(text, out ZapfSatz grund);
                if (ausEinstellung != null) return new Nenninhaltswahl(ausEinstellung, Nenninhaltsquelle.Einstellung, null);
                hinweis = new Auslegungshinweis(HINWEIS_NENNINHALTE_EINSTELLUNG,
                    ZapfSatz.Neu("AUSHINWEIS_NENNINHALTE_EINSTELLUNG", EINSTELLUNG_NENNINHALTE, grund), true);
            }
            try
            {
                Nenninhaltsliste ausParametern = Nenninhaltsliste.AusParametern(ps);
                if (ausParametern != null) return new Nenninhaltswahl(ausParametern, Nenninhaltsquelle.Parameter, hinweis);
            }
            catch (ZapfAuslegungException ex)
            {
                hinweis ??= new Auslegungshinweis(HINWEIS_NENNINHALTE_PARAMETER, ex.Satz, true);
            }
            return new Nenninhaltswahl(null, Nenninhaltsquelle.Keine, hinweis);
        }

        /// <summary>
        /// Die Liste aus dem Text der Einstellung; <c>null</c> mit <paramref name="grund"/>, wenn
        /// ein Glied keine Zahl ist oder die Folge nicht streng aufsteigend und positiv ist.
        /// Dezimalpunkt und -komma werden angenommen (invariant gelesen).
        /// </summary>
        internal static Nenninhaltsliste NenninhalteLesen(string text, out ZapfSatz grund)
        {
            grund = null;
            var werte = new List<double>();
            foreach (string teil in (text ?? "").Split(new[] { ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!double.TryParse(teil.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double w)
                    || double.IsNaN(w) || double.IsInfinity(w))
                {
                    grund = ZapfSatz.Neu("AUSTEXT_KEINE_ZAHL", teil);
                    return null;
                }
                werte.Add(w);
            }
            try { return Nenninhaltsliste.Aus(werte); }
            catch (ZapfAuslegungException ex)
            {
                grund = ex.Satz;
                return null;
            }
        }

        /// <summary>
        /// Der Anlagenbestand des Projekts: Zahl der Wärmepumpen (<c>Tab_WP</c>) und Kessel
        /// (<c>Tab_Heizkessel</c>). Fehlt eine Tabelle, zählt sie 0.
        /// </summary>
        internal static ZapfErzeugerbestand Erzeugerbestand(int idProjekt)
        {
            int wp = DataRepository.TabelleVorhanden("Tab_WP")
                ? Convert.ToInt32(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_WP WHERE ID_Projekt = ?",
                                                               new DbParam("@projekt", idProjekt)) ?? 0, CultureInfo.InvariantCulture)
                : 0;
            int kessel = DataRepository.TabelleVorhanden("Tab_Heizkessel")
                ? Convert.ToInt32(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_Heizkessel WHERE ID_Projekt = ?",
                                                               new DbParam("@projekt", idProjekt)) ?? 0, CultureInfo.InvariantCulture)
                : 0;
            return new ZapfErzeugerbestand(wp, kessel);
        }

        /// <summary>
        /// Der Kalender des Projekts für die Auslegung (derselbe wie für die Vorschau,
        /// <c>SimulationWaermebedarf.ZapfprofilKalenderLesen</c>); <c>false</c> ohne Klimaregion.
        /// </summary>
        internal static bool KalenderLesen(int idProjekt, out int wochentagJan1, out bool[] we)
        {
            wochentagJan1 = 0;
            we = null;
            var projekt = new ProjektCtrl();
            if (idProjekt > 0) projekt.ReadSingle(idProjekt);
            if (projekt.m_ID_Klimaregion <= 0) return false;
            var sim = new SimulationWaermebedarf { m_ID_Projekt = idProjekt };
            sim.ZapfprofilKalenderLesen(projekt.m_ID_Klimaregion);
            wochentagJan1 = sim.WochentagJan1;
            we = sim.WochenendkennzeichenKopie();
            return true;
        }

        // =================================================================================
        // Rechnen
        // =================================================================================

        /// <summary>
        /// <b>Die Auslegung eines Arbeitsstands</b> — derselbe Eingang wie der Lauf
        /// (<see cref="Eingang(int, ZapfprofilStand, int, bool[], IReadOnlyList{Nutzungsart})"/>),
        /// dazu Bedarfstage, DIN-4708-Katalog, Nenninhalte und die Laufangaben; ein konstruierter
        /// Entwurf des Stands zählt als gewählter Katalogtag (Id <see cref="ENTWURF_ID"/>).
        /// Fehlen Tabellen oder Katalogversion, wirft der Eingang benannt
        /// (<see cref="ZapfprofilEingabeException"/>, <see cref="ParametersatzException"/>).
        /// </summary>
        internal static Auslegungsrechnung Auslegung(int idProjekt, ZapfprofilStand stand, int wochentagJan1, bool[] we,
                                                    Auslegungslauf lauf)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            IReadOnlyList<Nutzungsart> katalog = Katalog();

            var tage = new List<BedarfstagKatalogzeile>(Bedarfstage());
            ZapfprofilStand rechenstand = stand;
            if (stand.BedarfstagEntwurf != null)
            {
                tage.Add(stand.BedarfstagEntwurf with { Id = ENTWURF_ID });
                ProjektStand p = (stand.Projekt ?? ProjektVorgabe())
                                 with { BedarfstagQuelle = ZapfBedarfstagquelle.Konstruktor, IdBedarfstag = ENTWURF_ID };
                rechenstand = stand with { Projekt = p };
            }

            Zapfprofileingang e = Eingang(idProjekt, rechenstand, wochentagJan1, we, katalog);
            Parametersatz ps = e.Parameter;
            Nenninhaltswahl nenn = Nenninhalte(ps);
            ZapfErzeugerbestand bestand = Erzeugerbestand(idProjekt);
            ZapfErzeugerart? art = lauf?.Erzeugerart ?? bestand.Vorschlag;

            var a = new Auslegungseingang
            {
                Din4708 = Din4708Katalog(ps.Katalogversion),
                Bedarfstage = tage.AsReadOnly(),
                Nenninhalte = nenn.Liste,
                Erzeugerart = art,
                Uebertragerwerkstoff = lauf?.Werkstoff,
                Stochastisch = lauf?.Stochastisch == true,
                Abbruch = lauf?.Abbruch ?? CancellationToken.None,
                Stufe = lauf?.Stufe
            };
            Auslegungsergebnis r = ZapfprofilAuslegung.Rechnen(e, katalog, a);
            if (nenn.Hinweis != null)
            {
                var hinweise = new List<Auslegungshinweis>(r.Hinweise) { nenn.Hinweis };
                r = r with { Hinweise = hinweise.AsReadOnly() };
            }
            return new Auslegungsrechnung(r, nenn, bestand, art, ps);
        }

        // =================================================================================
        // Konstruktor (A100, NA.5.2.3)
        // =================================================================================

        /// <summary>Die Zapfregeln des Konstruktors aus dem Parametersatz (<c>Konstruktor.Regel.*</c>).</summary>
        internal static IReadOnlyList<Zapfregel> Konstruktorregeln(Parametersatz ps) => Zapfregel.AusParametern(ps);

        /// <summary>
        /// <b>Der Konstruktor:</b> baut aus den Zeilen einen Bedarfstag bei θ_KW,A des
        /// Parametersatzes (<c>A100.Kaltwasser.Auslegung</c>; N10 (j), Folge) und liefert ihn als
        /// Entwurf einer Katalogzeile — Quelle Konstruktor, Status <c>EIGEN</c>, Herkunftsart
        /// <c>EIGENKONSTRUKTION</c>, ohne Bezugsmenge (der Tag gilt für das Projekt, er wird nicht
        /// skaliert). Ohne Namen, ohne Zeile oder mit einer ungültigen Zeile die benannte
        /// Ablehnung (<see cref="ZapfAuslegungException"/>).
        /// </summary>
        internal static BedarfstagKatalogzeile BedarfstagKonstruieren(IReadOnlyList<Konstruktorzeile> zeilen, string bezeichner,
                                                                      Parametersatz ps)
        {
            if (ps == null) throw new ArgumentNullException(nameof(ps));
            string name = (bezeichner ?? "").Trim();
            if (name.Length == 0)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.BedarfstagUngueltig,
                    ZapfSatz.Neu("AUSLEGUNG_KONSTRUKTOR_OHNE_NAME"));
            double kwKatalog = ps.Wert(ZapfAuslegungParameter.KALTWASSER_AUSLEGUNG);
            Bedarfstag tag = Bedarfstag.Konstruieren(zeilen, kwKatalog, name);
            return new BedarfstagKatalogzeile(ENTWURF_ID, name, ps.Katalogversion, ZapfBedarfstagquelle.Konstruktor, null,
                new Provenienz(TwwNutzungsartCtrl.QUELLE_EIGENKONSTRUKTION, null, ps.Katalogversion, Herkunftsart.Eigenkonstruktion),
                tag.Ereignisse)
            {
                Status = ZapfKatalogstatus.Eigen,
                ReadOnly = false
            };
        }

        /// <summary>
        /// Ein freier Name für einen konstruierten Bedarfstag in der Katalogversion:
        /// <paramref name="stamm"/>, sonst „stamm (2)", „stamm (3)" … — nur ein Vorschlag, der
        /// Schreibweg prüft den Namen erneut.
        /// </summary>
        internal static string FreierBedarfstagname(string stamm, string katalogversion)
        {
            string basis = string.IsNullOrWhiteSpace(stamm) ? "Bedarfstag" : stamm.Trim();
            var belegt = new HashSet<string>(StringComparer.Ordinal);
            if (DataRepository.TabelleVorhanden(TwwSchema.TAB_TWW_BEDARFSTAG_STAMM))
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT Bezeichner FROM " + TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + " WHERE Katalogversion = ?",
                    new DbParam("@version", katalogversion ?? ""));
                if (dt != null) foreach (DataRow r in dt.Rows) belegt.Add(Text(r, "Bezeichner"));
            }
            if (!belegt.Contains(basis)) return basis;
            for (int n = 2; ; n++)
            {
                string kandidat = basis + " (" + n.ToString(CultureInfo.InvariantCulture) + ")";
                if (!belegt.Contains(kandidat)) return kandidat;
            }
        }

        // =================================================================================
        // Schreibweg des Entwurfs (im Vorgang von Speichern)
        // =================================================================================

        /// <summary>
        /// Prüft den Entwurf vor dem ersten Schreiben: Name, Quelle Konstruktor, Katalogversion,
        /// Ereignisse im Tag (<see cref="Bedarfstag.AusKatalog"/>) und der Name frei in seiner
        /// Katalogversion (<c>UNIQUE (Bezeichner, Katalogversion)</c>) — sonst die benannte
        /// Ablehnung des Schreibwegs.
        /// </summary>
        private static void EntwurfPruefen(DbVorgang v, BedarfstagKatalogzeile entwurf)
        {
            string name = (entwurf.Bezeichner ?? "").Trim();
            if (name.Length == 0 || entwurf.QuelleArt != ZapfBedarfstagquelle.Konstruktor
                || string.IsNullOrWhiteSpace(entwurf.Katalogversion))
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.BedarfstagUngueltig, "",
                    ZapfSatz.Neu("SPEICHER_ENTWURF_UNVOLLSTAENDIG"));
            try { Bedarfstag.AusKatalog(entwurf, 1.0); }
            catch (ZapfAuslegungException ex)
            {
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.BedarfstagUngueltig, "", ex.Satz);
            }
            if (Anzahl(v, "SELECT COUNT(*) FROM " + TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + " WHERE Bezeichner = ? AND Katalogversion = ?",
                       name, entwurf.Katalogversion) > 0)
                throw new ZapfprofilSpeicherException(ZapfSpeicherfehler.BedarfstagNameBelegt, "",
                    ZapfSatz.Neu("SPEICHER_ENTWURF_NAME_BELEGT", name, entwurf.Katalogversion));
        }

        /// <summary>
        /// Legt den geprüften Entwurf als Katalogzeile an — Quelle_Art Konstruktor, ohne
        /// Bezugsmenge, neutrale Quelle, Herkunftsart <c>EIGENKONSTRUKTION</c>, Status
        /// <c>EIGEN</c>, <c>ReadOnly</c> 0 — samt Ereignissen in ihrer Reihenfolge; liefert die Id.
        /// </summary>
        private static int BedarfstagAnlegen(DbVorgang v, BedarfstagKatalogzeile entwurf)
        {
            string version = string.IsNullOrWhiteSpace(entwurf.Herkunft?.Version) ? entwurf.Katalogversion : entwurf.Herkunft.Version;
            int id = v.EinfuegenUndId(
                "INSERT INTO " + TwwSchema.TAB_TWW_BEDARFSTAG_STAMM + " (Bezeichner, Katalogversion, Quelle_Art, Bezugsmenge, " +
                "Quelle, Ausgabe, Version, Herkunftsart, Status, Beleg, ReadOnly) VALUES (?, ?, ?, NULL, ?, NULL, ?, ?, ?, NULL, 0)",
                new[]
                {
                    new DbParam("@bezeichner", entwurf.Bezeichner.Trim()),
                    new DbParam("@version", entwurf.Katalogversion),
                    new DbParam("@art", (int)ZapfBedarfstagquelle.Konstruktor),
                    new DbParam("@quelle", TwwNutzungsartCtrl.QUELLE_EIGENKONSTRUKTION),
                    new DbParam("@herkunftsversion", version),
                    new DbParam("@herkunft", TwwSchema.HERKUNFT_EIGENKONSTRUKTION),
                    new DbParam("@status", TwwSchema.STATUS_EIGEN)
                });
            int reihenfolge = 0;
            foreach (Zapfereignis e in entwurf.Ereignisse)
                v.Ausfuehren(
                    "INSERT INTO " + TwwSchema.TAB_TWW_BEDARFSTAG_EREIGNIS_STAMM +
                    " (ID_Bedarfstag, Minute_Beginn, Dauer_min, Energie_Kwh, Reihenfolge) VALUES (?, ?, ?, ?, ?)",
                    new DbParam("@tag", id), new DbParam("@beginn", e.MinuteBeginn), new DbParam("@dauer", e.DauerMin),
                    new DbParam("@energie", e.EnergieKwh), new DbParam("@reihenfolge", ++reihenfolge));
            return id;
        }
    }
}
