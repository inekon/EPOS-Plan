using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using WindowsFormsApplication1;

namespace Gebaeudevergleich
{
    /// <summary>
    /// <b><c>vergleich</c></b> — rechnet jedes Gebäude der <c>--db</c> einmal auf dem
    /// Tagesbilanz-Weg („alt") und einmal auf dem VDI-6007-Weg („neu") und bewertet die
    /// Abweichung.
    ///
    /// <para><b>Beide Wege werden erzwungen.</b> Je Gebäude zwei Aufrufe
    /// <see cref="GebaeudeBedarfCtrl.Rechnen"/> mit <c>DbWerte.GEBAEUDE_MODELL_TAGESBILANZ</c> und
    /// <c>DbWerte.GEBAEUDE_MODELL_VDI6007</c> — nie <c>null</c>; der Spaltenwert ist nur ein
    /// Merkmal. Das ist das Aufrufmuster des Gebäudedialogs (<c>GebaeudeBedarfHuelle.Gaben</c>).</para>
    ///
    /// <para><b>Fehler je Gebäude.</b> Vor jedem Aufruf <c>SimulationProtokoll.NeuStarten()</c>,
    /// jeder Aufruf in <c>try/catch</c>. Als Fehler gilt <c>Erfolgreich == false</c>, eine
    /// Ausnahme, ein nicht endlicher Wert oder eine Jahreswärme ≤ 0; die Zeile wird rot, der
    /// Stapel läuft weiter. Der Beginn jedes Aufrufs steht vorher im Protokoll (geflusht); nach
    /// 60 s kommt eine Warnung. Abbrechen lässt sich ein Aufruf im Prozess nicht — die
    /// Windows-Suite braucht dafür Kindprozesse.</para>
    ///
    /// <para><b>Übersprungen</b> (ohne Rechnung, mit Grund) wird ein Projekt ohne oder mit
    /// unbekannter Klimaregion und ein Projekt ohne Gebäude.</para>
    /// </summary>
    internal static class Vergleichslauf
    {
        /// <summary>Nach so vielen Sekunden meldet ein Aufruf eine Warnung.</summary>
        internal const int WARNUNG_NACH_SEKUNDEN = 60;

        private static readonly Regex FEHLERCODE = new Regex(@"\[([A-Za-z][A-Za-z0-9]*)\]", RegexOptions.CultureInvariant);

        internal static int Ausfuehren(Argumente arg, Ausgabe aus)
        {
            aus.Konsole("Gebaeudevergleich vergleich");
            string bindung = Einstieg.DatenbankBinden(arg.Db);
            if (bindung != null) return Abbruch(aus, bindung);
            aus.Protokoll("Datenbank " + arg.Db);
            string hashVorher = Sqlitehilfe.Sha256(arg.Db);
            aus.ProtokollPruefsumme("SHA-256 der Datenbank vor dem Lauf: ", hashVorher);

            int stand = Schemapruefung.Stand();
            aus.Protokoll("Schemastand " + Zahl(stand) + ", Zielstand " + Zahl(SchemaStand.Zielversion));
            string schema = Schemapruefung.Pruefen(stand);
            if (schema != null) return Abbruch(aus, schema);

            Namensbereinigung bereinigung = arg.MitNamen ? Namensbereinigung.Leer : Namensbereinigung.AusDatenbank();
            aus.Scharfschalten(bereinigung);
            aus.Protokoll("Namensbereinigung: " + (arg.MitNamen ? "aus (--mit-namen)" : Zahl(bereinigung.Anzahl) + " Werte"));

            List<Projektzeile> projekte = ProjekteLesen(arg, aus);
            int mitGebaeude = projekte.Count(p => !p.Uebersprungen);
            int gebaeude = projekte.Sum(p => p.Gebaeude.Count);
            aus.Konsole(Zahl(projekte.Count) + " Projekte, davon " + Zahl(mitGebaeude) + " zu rechnen, " +
                        Zahl(projekte.Count - mitGebaeude) + " übersprungen; " + Zahl(gebaeude) + " Gebäude");

            Stopwatch gesamt = Stopwatch.StartNew();
            foreach (Projektzeile p in projekte.Where(p => !p.Uebersprungen))
            {
                foreach (Gebaeudezeile z in p.Gebaeude) Rechnen(p, z, arg, aus);
                Aufsummieren(p);
            }
            gesamt.Stop();
            aus.Protokoll("Rechenzeit gesamt: " + gesamt.Elapsed.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s");

            var bericht = new Berichtsschreiber(arg, aus, projekte, stand);
            bericht.Schreiben();

            // Gegenprobe: Die --db hat sich nicht bewegt (die Schreibsperre hält).
            DataRepository.PfadUeberschreibung = null;
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            string hashNachher = Sqlitehilfe.Sha256(arg.Db);
            aus.ProtokollPruefsumme("SHA-256 der Datenbank nach dem Lauf: ", hashNachher +
                          (hashNachher == hashVorher ? " (unverändert)" : " — WARNUNG: verändert"));

            List<Gebaeudezeile> alle = projekte.SelectMany(p => p.Gebaeude).ToList();
            int rot = alle.Count(z => z.Befund.Ampel == Ampel.Fehler);
            aus.Konsole("Ampel: " + Zahl(alle.Count(z => z.Befund.Ampel == Ampel.Erklaert)) + " erklärt, " +
                        Zahl(alle.Count(z => z.Befund.Ampel == Ampel.ZuPruefen)) + " zu prüfen, " +
                        Zahl(rot) + " Fehler");
            if (hashNachher != hashVorher)
                return Abbruch(aus, "Die Datenbank hat sich während des Vergleichs verändert: " + arg.Db);
            return rot > 0 ? Program.ROT : Program.OHNE_ROT;
        }

        // =================================================================================
        //  Projekte und Gebäude lesen
        // =================================================================================

        private static List<Projektzeile> ProjekteLesen(Argumente arg, Ausgabe aus)
        {
            List<int> vorhanden = ProjektCtrl.NamenListe().Select(p => p.Id).Where(id => id > 0)
                                              .Distinct().OrderBy(id => id).ToList();
            IEnumerable<int> gewaehlt = arg.Projekte ?? (IEnumerable<int>)vorhanden;

            var liste = new List<Projektzeile>();
            foreach (int id in gewaehlt)
            {
                var p = new Projektzeile { Id = id };
                liste.Add(p);
                if (!vorhanden.Contains(id))
                {
                    p.Uebersprungen = true;
                    p.Grund = "Projekt nicht vorhanden";
                    continue;
                }

                var projekt = new ProjektCtrl();
                projekt.ReadSingle(id);
                p.Name = projekt.m_szProjektname ?? "";
                int klima = projekt.m_ID_Klimaregion;

                var gruende = new List<string>();
                if (klima <= 0) gruende.Add("keine Klimaregion");
                else if (!KlimaregionCtrl.Koordinaten(klima, out _, out _))
                    gruende.Add("Klimaregion " + Zahl(klima) + " unbekannt oder ohne Koordinaten");

                List<Z_ProjGebModel> zuordnungen = Z_ProjGebCtrl.LiesProjekt(id);
                if (zuordnungen.Count == 0) gruende.Add("kein Gebäude");

                foreach (Z_ProjGebModel z in zuordnungen)
                {
                    var zeile = new Gebaeudezeile { IdProjekt = id, IdZ = z.ID_Z, Projektname = p.Name };
                    ProjektGebaeudeModel g = GebaeudeBedarfCtrl.Projektgebaeude(id, z.ID_Z);
                    if (g != null)
                    {
                        zeile.IdGebaeude = g.ID_Gebaeude;
                        zeile.Gebaeudename = g.Gebaeudename ?? "";
                        zeile.Merkmale = Merkmalbildung.Bilden(g);
                    }
                    p.Gebaeude.Add(zeile);
                }
                p.Gebaeude = p.Gebaeude.OrderBy(z => z.IdGebaeude).ThenBy(z => z.IdZ).ToList();
                p.GebaeudeZugeordnet = p.Gebaeude.Count;

                if (gruende.Count > 0)
                {
                    p.Uebersprungen = true;
                    p.Grund = string.Join("; ", gruende);
                    p.Gebaeude.Clear();
                    aus.Protokoll("Übersprungen P" + Zahl(id) + ": " + p.Grund);
                }
                p.Klimaregion = klima;
            }
            return liste;
        }

        // =================================================================================
        //  Rechnen
        // =================================================================================

        private static void Rechnen(Projektzeile p, Gebaeudezeile z, Argumente arg, Ausgabe aus)
        {
            string wer = "P" + Zahl(p.Id) + "/G" + Zahl(z.IdGebaeude);
            int klima = p.Klimaregion;

            if (z.IdGebaeude <= 0)
            {
                // Die Zuordnung hat keine lesbare Gebäudezeile.
                foreach (Wegergebnis w in new[] { z.Alt, z.Neu })
                {
                    w.Ok = false;
                    w.Fehlercode = "GebaeudezeileFehlt";
                    w.Fehlertext = "Zur Zuordnung " + Zahl(z.IdZ) + " gibt es keine Gebäudezeile in der Sicht.";
                }
                z.Alt.Weg = DbWerte.GEBAEUDE_MODELL_TAGESBILANZ;
                z.Neu.Weg = DbWerte.GEBAEUDE_MODELL_VDI6007;
                wer = "P" + Zahl(p.Id) + "/Z" + Zahl(z.IdZ);
            }
            else
            {
                z.Alt = Weg(p.Id, klima, z.IdZ, DbWerte.GEBAEUDE_MODELL_TAGESBILANZ, wer, aus);
                z.Neu = Weg(p.Id, klima, z.IdZ, DbWerte.GEBAEUDE_MODELL_VDI6007, wer, aus);
            }

            Merkmale m = z.Merkmale;
            Wegergebnis quelle = z.Neu.Ok ? z.Neu : z.Alt;
            m.KuehlungWirksam = quelle.KuehlbetriebProjekt && quelle.KuehlungAktiv;
            m.Gekoppelt = z.Neu.Gekoppelt;

            foreach (Wegergebnis w in new[] { z.Alt, z.Neu })
            {
                if (!w.Ok) continue;
                w.KatalogtrefferProzent = Kennzahlen.KatalogtrefferProzent(w.JahrMwh, m.IstFlaeche, m.SpezWaermeverbrauch, m.Bezugswert);
                w.VerbrauchstrefferProzent = Kennzahlen.VerbrauchstrefferProzent(w.JahrMwh, m.IstFlaeche, m.VerbrauchNeuKwh);
            }

            z.Befund = Ursachenregeln.Anwenden(Eingang(z));

            string zeile = wer + ": alt " + (z.Alt.Ok ? "ok" : "FEHLER " + z.Alt.Fehlercode) +
                           ", neu " + (z.Neu.Ok ? "ok" : "FEHLER " + z.Neu.Fehlercode);
            aus.Konsole(zeile);
            aus.Protokoll(wer + ": " + Ursachenregeln.Text(z.Befund.Ampel) +
                          (z.Befund.Codes.Count > 0 ? " [" + string.Join(", ", z.Befund.Codes) + "]" : "") +
                          " alt " + Sek(z.Alt.Sekunden) + ", neu " + Sek(z.Neu.Sekunden));
        }

        /// <summary>Der Regeleingang einer gerechneten Zeile.</summary>
        internal static Regeleingang Eingang(Gebaeudezeile z)
        {
            Merkmale m = z.Merkmale;
            return new Regeleingang
            {
                AltOk = z.Alt.Ok,
                NeuOk = z.Neu.Ok,
                IstFlaeche = m.IstFlaeche,
                HatZone = m.Zone,
                FesteNennleistung = m.FesteNennleistung,
                KuehlungWirksam = m.KuehlungWirksam,
                HeizkreisAktiv = m.HeizkreisAktiv,
                IstNwg = m.IstNwg,
                SpalteTagesbilanz = m.SpalteModell == DbWerte.GEBAEUDE_MODELL_TAGESBILANZ,
                DeltaJahrProzent = Kennzahlen.DeltaProzent(z.Alt.JahrMwh, z.Neu.JahrMwh),
                DeltaSpitzeProzent = Kennzahlen.DeltaProzent(z.Alt.SpitzeKw, z.Neu.SpitzeKw),
                KatalogtrefferNeuProzent = z.Neu.KatalogtrefferProzent,
                NachtanteilAltProzent = z.Alt.NachtanteilProzent,
                NachtanteilNeuProzent = z.Neu.NachtanteilProzent,
                FensteranteilProzent = m.FensteranteilProzent,
                BauweiseJeM2 = m.BauweiseJeM2,
                InnereGewinneWm2 = m.InnereGewinneWm2,
                AbsenkungK = m.AbsenkungK,
            };
        }

        /// <summary>Ein Aufruf des Controllers auf einem erzwungenen Weg — Fehler gefangen, Kennzahlen gebildet.</summary>
        private static Wegergebnis Weg(int idProjekt, int idKlimaregion, int idZ, string weg, string wer, Ausgabe aus)
        {
            string kurz = weg == DbWerte.GEBAEUDE_MODELL_TAGESBILANZ ? "alt" : "neu";
            var w = new Wegergebnis { Weg = weg };
            aus.ProtokollSofort("Beginn " + wer + " " + kurz + " (" + weg + ")");

            SimulationProtokoll.NeuStarten();
            Stopwatch uhr = Stopwatch.StartNew();
            GebaeudeBedarfErgebnis e = null;
            Exception ausnahme = null;
            using (new Timer(_ => aus.Konsole("Warnung: " + wer + " " + kurz + " rechnet seit mehr als " +
                                              Zahl(WARNUNG_NACH_SEKUNDEN) + " s (ein Abbruch im Prozess ist nicht möglich)"),
                             null, WARNUNG_NACH_SEKUNDEN * 1000, Timeout.Infinite))
            {
                try { e = GebaeudeBedarfCtrl.Rechnen(idProjekt, idKlimaregion, idZ, weg); }
                catch (Exception ex) { ausnahme = ex; }
            }
            uhr.Stop();
            w.Sekunden = uhr.Elapsed.TotalSeconds;

            SimulationProtokoll protokoll = SimulationProtokoll.Aktuell;
            Namensbereinigung b = aus.Bereinigung;
            w.Meldungen = b.Bereinigen(Einzeilig(protokoll.AlsText(false)));

            if (ausnahme != null)
            {
                w.Fehlercode = "Ausnahme:" + ausnahme.GetType().Name;
                w.Fehlertext = b.Bereinigen(Einzeilig(ausnahme.Message));
                aus.Protokoll(wer + " " + kurz + ": Ausnahme " + ausnahme.GetType().Name + " — " + ausnahme.Message);
                return w;
            }
            if (e == null || !e.Erfolgreich)
            {
                string text = protokoll.AlsText(true);
                Match m = FEHLERCODE.Match(text ?? "");
                w.Fehlercode = m.Success ? m.Groups[1].Value : "NichtGerechnet";
                w.Fehlertext = b.Bereinigen(Einzeilig(string.IsNullOrWhiteSpace(text)
                    ? "Der Controller meldet kein Ergebnis (Erfolgreich = false) ohne Meldung."
                    : text));
                return w;
            }
            if (Kennzahlen.NichtEndlich(e.Stundenwerte) || !Kennzahlen.Endlich(e.HeizwaermeMwh))
            {
                w.Fehlercode = "NichtEndlich";
                w.Fehlertext = "Die Stundenreihe oder die Jahreswärme enthält einen nicht endlichen Wert.";
                return w;
            }
            if (!(e.HeizwaermeMwh > 0.0))
            {
                w.Fehlercode = "JahreswaermeNichtPositiv";
                w.Fehlertext = "Die Jahreswärme ist " + e.HeizwaermeMwh.ToString("0.###", CultureInfo.InvariantCulture) + " MWh (≤ 0).";
                return w;
            }

            w.Ok = true;
            w.JahrMwh = e.HeizwaermeMwh;
            w.SpitzeKw = e.MaxLastKw;
            w.TagesmittelKw = e.SpitzeTagesmittelKw ?? double.NaN;
            w.Q95Kw = e.SpitzeQuantil95Kw ?? double.NaN;
            w.VollbenutzungsstundenH = e.VollbenutzungsstundenH ?? double.NaN;
            w.MonateMwh = (double[])e.MonatswerteMwh.Clone();
            w.StundenKw = (double[])e.Stundenwerte.Clone();
            w.NachtanteilProzent = Kennzahlen.NachtanteilProzent(w.StundenKw);
            w.Heizstunden = Kennzahlen.Heizstunden(w.StundenKw);
            w.KuehlbetriebProjekt = e.KuehlbetriebProjekt;
            w.KuehlungAktiv = e.KuehlungAktiv;
            w.Gekoppelt = e.Gekoppelt;

            if (weg == DbWerte.GEBAEUDE_MODELL_VDI6007)
            {
                w.RaumtemperaturMittelC = e.MittlereRaumtemperaturC;
                w.Ueberhitzungsstunden = e.UeberhitzungsstundenH;
                if (e.KuehlbetriebProjekt && e.KuehlungAktiv)
                {
                    w.KaelteMwh = e.KuehlenergieMwh;
                    w.KaeltespitzeKw = e.KaeltelastMaxKw;
                }
            }
            return w;
        }

        /// <summary>Projektsummen über die Gebäude, die auf BEIDEN Wegen gerechnet sind; Spitze aus den addierten Reihen.</summary>
        private static void Aufsummieren(Projektzeile p)
        {
            var alt = new double[8760];
            var neu = new double[8760];
            foreach (Gebaeudezeile z in p.Gebaeude.Where(z => z.BeideOk))
            {
                p.GebaeudeGerechnet++;
                p.SummeAltMwh += z.Alt.JahrMwh;
                p.SummeNeuMwh += z.Neu.JahrMwh;
                Kennzahlen.Addieren(alt, z.Alt.StundenKw);
                Kennzahlen.Addieren(neu, z.Neu.StundenKw);
            }
            p.SpitzeAltKw = p.GebaeudeGerechnet > 0 ? alt.Max() : double.NaN;
            p.SpitzeNeuKw = p.GebaeudeGerechnet > 0 ? neu.Max() : double.NaN;
            if (p.GebaeudeGerechnet == 0) { p.SummeAltMwh = double.NaN; p.SummeNeuMwh = double.NaN; }
            p.Ampel = p.Gebaeude.Count == 0 ? Ampel.Erklaert : p.Gebaeude.Max(z => z.Befund.Ampel);
        }

        private static string Einzeilig(string text)
            => (text ?? "").Replace("\r\n", " | ").Replace("\n", " | ").Replace("\r", " | ").Trim();

        private static string Sek(double s) => s.ToString("0.0", CultureInfo.InvariantCulture) + " s";

        private static string Zahl(int z) => z.ToString(CultureInfo.InvariantCulture);

        private static int Abbruch(Ausgabe aus, string grund)
        {
            aus.Fehler("Abbruch: " + grund);
            return Program.ABBRUCH;
        }
    }
}
