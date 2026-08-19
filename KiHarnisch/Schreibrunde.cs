using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using KiKern;

namespace WindowsFormsApplication1.Referenzlauf
{
    /// <summary>
    /// Pruefteil der Bestaetigungsschicht und der Schreibaktionen
    /// (Fachkonzept 8/Etappe 3, Abnahme) - OHNE NETZ, OHNE MODELL, gegen die
    /// ARBEITSKOPIE der Datenbank.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Die Leitfrage dieser Etappe lautet: „Kann eine Schreibaktion ohne Klick
    /// durchlaufen?" Beantwortet wird sie nicht mit einer Zusage, sondern mit Zahlen:
    /// Zu jedem Prueffall steht der betroffene Wert VORHER und NACHHER im Protokoll,
    /// dazu die Zahl der Saetze in den beruehrten Tabellen und das Aenderungsdatum des
    /// Projekts. Ein einziger veraenderter Wert an der falschen Stelle faellt damit auf.
    /// </para>
    /// <para>
    /// Zwei Prueffaeden werden umgelegt und in JEDEM Fall zurueckgesetzt:
    /// <c>KiAusfuehrer.Uhr</c> (sonst waere der Verfall nur mit einer Minute Warten
    /// pruefbar) und <c>KiAusfuehrer.Schreibrecht</c> (sonst waere der Lesemodus der
    /// Lizenz weder herstellbar noch zurueckdrehbar). Beide sind keine Betriebsschalter:
    /// Das Modell kann sie nicht erreichen, weil sie kein Parameter irgendeiner Aktion
    /// sind.
    /// </para>
    /// </remarks>
    internal static class Schreibrunde
    {
        private const string KONTEXT = "Kostenverwaltung";

        private static Protokoll _log;
        private static int _geprueft;
        private static int _gefallen;

        /// <summary>Die einstellbare Uhr der Freigaben.</summary>
        private static DateTime _jetzt;

        /// <summary>
        /// Welche Registeraktionen dieser Pruefteil aufgerufen hat - der Programmteil
        /// verlangt, dass am Ende JEDE registrierte Aktion mindestens einmal dran war.
        /// </summary>
        internal static readonly HashSet<string> Gerufen = new HashSet<string>(StringComparer.Ordinal);

        // =====================================================================

        internal static void Pruefen(Protokoll log, string protokollDatei, ref int zeilenVorher)
        {
            _log = log;
            _geprueft = 0;
            _gefallen = 0;

            _log.Leerzeile();
            _log.Zeile("=== Bestaetigungsschicht und Schreibaktionen (Etappe 3) ===");

            Func<DateTime> uhrVorher = KiAusfuehrer.Uhr;
            Func<bool> rechtVorher = KiAusfuehrer.Schreibrecht;
            Func<bool> modalVorher = KiAusfuehrer.ModalerDialog;
            KiBestaetigungsfrage wegVorher = KiChatService.Bestaetigungsweg;

            _jetzt = DateTime.Now;
            KiAusfuehrer.Uhr = () => _jetzt;
            KiAusfuehrer.SicherungZuruecksetzen();

            try
            {
                RegisterPruefen();

                Eckwerte eck = EckwerteLesen();
                _log.Zeile("Eckwerte: " + eck);
                if (eck.IdPosition <= 0)
                {
                    _log.Warnung("Keine Kostenposition in der Arbeitskopie - die Schreibfaelle " +
                                 "sind nicht pruefbar.");
                    return;
                }

                zeilenVorher = OhneBestaetigung(eck, protokollDatei, zeilenVorher);
                zeilenVorher = Abgelehnt(eck, protokollDatei, zeilenVorher);
                zeilenVorher = Verfallen(eck, protokollDatei, zeilenVorher);
                zeilenVorher = OhneSchreibrecht(eck, protokollDatei, zeilenVorher);
                zeilenVorher = Erteilt(eck, protokollDatei, zeilenVorher);
                zeilenVorher = SchreibschutzPruefen(eck, protokollDatei, zeilenVorher);
                zeilenVorher = VorschauTreue(eck, protokollDatei, zeilenVorher);
                zeilenVorher = SpeichervariantePruefen(eck, protokollDatei, zeilenVorher);
                zeilenVorher = ModalitaetPruefen(eck, protokollDatei, zeilenVorher);
            }
            finally
            {
                KiChatService.Modellkanal = null;
                KiChatService.Bestaetigungsweg = wegVorher;
                KiAusfuehrer.Uhr = uhrVorher;
                KiAusfuehrer.Schreibrecht = rechtVorher;
                KiAusfuehrer.ModalerDialog = modalVorher;
            }

            _log.Leerzeile();
            _log.Zeile("Schreibrunde: " + (_geprueft - _gefallen) + " von " + _geprueft +
                       " Pruefungen bestanden.");
        }

        // ===================================================================== Register

        /// <summary>
        /// Was das Register zusagt, bevor irgendetwas laeuft: Stufen, Vorschaupflicht,
        /// Umkehrbarkeit - und dass keine Aktion einen Katalog beschreibt.
        /// </summary>
        private static void RegisterPruefen()
        {
            _log.Leerzeile();
            _log.Zeile("--- Register: Stufen, Vorschaupflicht, Andockpunkte ---");

            KiRegister register = KiAusfuehrer.Register;
            IReadOnlyList<KiAktion> schreibend = register.NachStufe(Schutzstufe.Schreiben);

            _log.Zeile("Aktionen gesamt: " + register.Anzahl +
                       ", davon lesend: " + register.NachStufe(Schutzstufe.Lesen).Count +
                       ", schreibend: " + schreibend.Count +
                       ", rechnend: " + register.NachStufe(Schutzstufe.Rechnen).Count);

            foreach (KiAktion a in schreibend)
                _log.Roh("      · " + a.Name + "  -> " + a.Andockpunkt +
                         "  (rueckholbar: " + (a.Umkehrbar ? "ja" : "nein") + ")");

            Pruefe(schreibend.Count >= 2, "mindestens zwei Schreibaktionen registriert");
            Pruefe(register.NachStufe(Schutzstufe.Rechnen).Count == 0,
                   "keine Rechenaktion registriert (kommt mit Etappe 4)");

            foreach (KiAktion a in register.Alle)
            {
                if (a.Stufe == Schutzstufe.Lesen) continue;
                Pruefe(a.Vorschau != null, a.Name + " fuehrt eine Vorschau");
                Pruefe(KiRiegel.BrauchtBestaetigung(a), a.Name + " ist bestaetigungspflichtig");
                Pruefe(KiRiegel.PruefeStufe(a) == null, a.Name + " ist in dieser Ausbaustufe freigegeben");
            }

            // Katalogpflege ist gar nicht erst deklariert (Fachkonzept 1.2).
            foreach (KiAktion a in register.Alle)
                Pruefe(a.Andockpunkt.IndexOf("StammCtrl", StringComparison.Ordinal) < 0,
                       a.Name + " dockt an keinen Katalogcontroller an");
        }

        // ================================================================ Fall 1

        /// <summary>
        /// OHNE Bestaetigungsweg: Es gibt niemanden zu fragen - also darf nichts
        /// geschrieben werden. Das ist der Zustand jedes Prozesses ohne Chatfenster.
        /// </summary>
        private static int OhneBestaetigung(Eckwerte eck, string protokollDatei, int zeilenVorher)
        {
            _log.Leerzeile();
            _log.Zeile("--- Fall 1: Schreibaktion OHNE Bestaetigungsweg ---");

            KiAusfuehrer.Schreibrecht = () => true;
            KiChatService.Bestaetigungsweg = null;

            Abzug vorher = Abzug.Nehmen(eck);
            KiAntwort antwort = Schreibversuch(eck, vorher.Betrag + 1111.0);
            Abzug nachher = Abzug.Nehmen(eck);

            Vergleiche(vorher, nachher, "keine Aenderung");
            PruefeSchritt(antwort, false);
            PruefeAblehnungAnsModell();

            int neu = Neue(protokollDatei, ref zeilenVorher);
            Pruefe(neu == 1, "genau eine Protokollzeile (gemessen: " + neu + ")");
            Pruefe(KiAusfuehrer.SicherungPfad.Length == 0,
                   "kein Sicherungspunkt angelegt - es wurde nichts vorbereitet");
            Pruefe(antwort.Erfolg, "die Unterhaltung laeuft weiter");
            return zeilenVorher;
        }

        // ================================================================ Fall 2

        /// <summary>Der Anwender klickt „Abbrechen": nichts wird geschrieben, der Chat geht weiter.</summary>
        private static int Abgelehnt(Eckwerte eck, string protokollDatei, int zeilenVorher)
        {
            _log.Leerzeile();
            _log.Zeile("--- Fall 2: Bestaetigung ABGELEHNT ---");

            bool belegtWaehrendWartens = true;
            KiAusfuehrer.Schreibrecht = () => true;
            KiChatService.Bestaetigungsweg = (f, tok) =>
            {
                belegtWaehrendWartens = KiAusfuehrer.Belegt;
                _log.Roh("      Vorschautext:");
                foreach (string z in Zeilen(f.Text)) _log.Roh("        " + z);
                return Task.FromResult(KiEntscheidung.Abgelehnt);
            };

            Abzug vorher = Abzug.Nehmen(eck);
            KiAntwort antwort = Schreibversuch(eck, vorher.Betrag + 2222.0);
            Abzug nachher = Abzug.Nehmen(eck);

            Vergleiche(vorher, nachher, "keine Aenderung");
            PruefeSchritt(antwort, false);
            Pruefe(antwort.Schritte.Count == 1 &&
                   antwort.Schritte[0].Entscheidung == KiEntscheidung.Abgelehnt,
                   "Entscheidung als abgelehnt vermerkt");
            Pruefe(antwort.Schritte.Count == 1 && antwort.Schritte[0].Bestaetigung.Length > 0,
                   "Bestaetigungstext lag vor");
            Pruefe(!belegtWaehrendWartens,
                   "die Laufsperre ist waehrend der Bedenkzeit NICHT belegt (Einlaeufigkeit)");
            Pruefe(antwort.Erfolg && antwort.Runden == 2,
                   "die Unterhaltung laeuft weiter (Runden: " + antwort.Runden + ")");
            PruefeAblehnungAnsModell();

            int neu = Neue(protokollDatei, ref zeilenVorher);
            Pruefe(neu == 1, "genau eine Protokollzeile (gemessen: " + neu + ")");
            Pruefe(KiAusfuehrer.SicherungPfad.Length > 0 && File.Exists(KiAusfuehrer.SicherungPfad),
                   "Sicherungspunkt liegt vor: " + KiAusfuehrer.SicherungPfad);
            return zeilenVorher;
        }

        // ================================================================ Fall 3

        /// <summary>
        /// Die Vorschau verfaellt, WAEHREND der Anwender ueberlegt - und sein spaeteres
        /// „Ausfuehren" ist wertlos (Fachkonzept 3.5, Punkt 5).
        /// </summary>
        private static int Verfallen(Eckwerte eck, string protokollDatei, int zeilenVorher)
        {
            _log.Leerzeile();
            _log.Zeile("--- Fall 3: Bestaetigung VERFALLEN (Uhr kuenstlich vorgerueckt) ---");

            KiAusfuehrer.Schreibrecht = () => true;
            KiChatService.Bestaetigungsweg = (f, tok) =>
            {
                // Der Anwender laesst sich Zeit: 61 Sekunden bei einer Frist von 60.
                _jetzt = _jetzt.AddSeconds(KiFreigabe.VerfallSekunden + 1);
                _log.Roh("      Uhr vorgerueckt um " + (KiFreigabe.VerfallSekunden + 1) +
                         " s; Restzeit: " + (int)f.Restzeit().TotalSeconds + " s");
                return Task.FromResult(KiEntscheidung.Erteilt);   // zu spaet
            };

            Abzug vorher = Abzug.Nehmen(eck);
            KiAntwort antwort = Schreibversuch(eck, vorher.Betrag + 3333.0);
            Abzug nachher = Abzug.Nehmen(eck);

            Vergleiche(vorher, nachher, "keine Aenderung");
            PruefeSchritt(antwort, false);
            Pruefe(antwort.Schritte.Count == 1 &&
                   antwort.Schritte[0].Entscheidung == KiEntscheidung.Verfallen,
                   "Entscheidung als verfallen vermerkt");
            if (antwort.Schritte.Count == 1)
                _log.Roh("      Grund: " + Einzeilig(antwort.Schritte[0].Grund));
            PruefeAblehnungAnsModell();

            int neu = Neue(protokollDatei, ref zeilenVorher);
            Pruefe(neu == 1, "genau eine Protokollzeile (gemessen: " + neu + ")");

            _jetzt = DateTime.Now;      // Uhr wieder auf Normalgang
            return zeilenVorher;
        }

        // ================================================================ Fall 4

        /// <summary>Lizenz im Lesemodus: auch eine erteilte Bestaetigung schreibt nichts.</summary>
        private static int OhneSchreibrecht(Eckwerte eck, string protokollDatei, int zeilenVorher)
        {
            _log.Leerzeile();
            _log.Zeile("--- Fall 4: DarfSchreiben() == false trotz Bestaetigung ---");

            bool gefragt = false;
            KiAusfuehrer.Schreibrecht = () => { gefragt = true; return false; };
            KiChatService.Bestaetigungsweg = (f, tok) => Task.FromResult(KiEntscheidung.Erteilt);

            Abzug vorher = Abzug.Nehmen(eck);
            KiAntwort antwort = Schreibversuch(eck, vorher.Betrag + 4444.0);
            Abzug nachher = Abzug.Nehmen(eck);

            Vergleiche(vorher, nachher, "keine Aenderung");
            Pruefe(gefragt, "DarfSchreiben() wurde ueberhaupt gefragt");
            PruefeSchritt(antwort, false);
            if (antwort.Schritte.Count == 1)
                _log.Roh("      Grund: " + Einzeilig(antwort.Schritte[0].Grund));
            PruefeAblehnungAnsModell();

            int neu = Neue(protokollDatei, ref zeilenVorher);
            Pruefe(neu == 1, "genau eine Protokollzeile (gemessen: " + neu + ")");

            KiAusfuehrer.Schreibrecht = () => true;
            return zeilenVorher;
        }

        // ================================================================ Fall 5

        /// <summary>Der Regelfall: bestaetigt - GENAU EINE Aenderung, sonst nichts.</summary>
        private static int Erteilt(Eckwerte eck, string protokollDatei, int zeilenVorher)
        {
            _log.Leerzeile();
            _log.Zeile("--- Fall 5: Bestaetigung ERTEILT ---");

            KiAusfuehrer.Schreibrecht = () => true;
            KiChatService.Bestaetigungsweg = (f, tok) => Task.FromResult(KiEntscheidung.Erteilt);

            Abzug vorher = Abzug.Nehmen(eck);
            double neuerBetrag = Math.Round(vorher.Betrag + 5555.55, 2);
            KiAntwort antwort = Schreibversuch(eck, neuerBetrag);
            Abzug nachher = Abzug.Nehmen(eck);

            _log.Roh("      Betrag vorher : " + Z(vorher.Betrag));
            _log.Roh("      Betrag nachher: " + Z(nachher.Betrag) + " (erwartet " + Z(neuerBetrag) + ")");

            Pruefe(Math.Abs(nachher.Betrag - neuerBetrag) < 0.005,
                   "der Betrag steht auf dem bestaetigten Wert");
            Pruefe(nachher.Projekte == vorher.Projekte && nachher.Positionen == vorher.Positionen,
                   "keine Zeile angelegt oder entfernt (Projekte " + vorher.Projekte + "->" +
                   nachher.Projekte + ", Positionen " + vorher.Positionen + "->" + nachher.Positionen + ")");
            Pruefe(nachher.Aenderungsdatum != vorher.Aenderungsdatum ||
                   nachher.Aenderungsdatum.Date == DateTime.Today,
                   "Aenderungsdatum des Projekts gesetzt (" + vorher.Aenderungsdatum.ToString("g", CultureInfo.InvariantCulture) +
                   " -> " + nachher.Aenderungsdatum.ToString("g", CultureInfo.InvariantCulture) + ")");

            PruefeSchritt(antwort, true);
            Pruefe(antwort.Schritte.Count == 1 &&
                   antwort.Schritte[0].Entscheidung == KiEntscheidung.Erteilt,
                   "Entscheidung als erteilt vermerkt");
            Pruefe(antwort.Runden == 2, "Rundendeckel gewahrt (Runden: " + antwort.Runden + " von " +
                                        KiWerkzeuge.Rundendeckel + ")");
            Pruefe(!KiAusfuehrer.Belegt, "die Laufsperre ist nach dem Lauf wieder frei");

            string sicherung = KiAusfuehrer.SicherungPfad;
            Pruefe(sicherung.Length > 0 && File.Exists(sicherung),
                   "Sicherungspunkt existiert: " + sicherung);
            Pruefe(sicherung.IndexOf(KiSicherungsordner, StringComparison.OrdinalIgnoreCase) >= 0,
                   "Sicherungspunkt liegt in " + KiSicherungsordner);
            if (antwort.Schritte.Count == 1)
            {
                Pruefe(antwort.Schritte[0].Sicherungspunkt == sicherung,
                       "der Schritt nennt denselben Sicherungspunkt");
                _log.Roh("      Protokollzeile: " + Einzeilig(antwort.Schritte[0].Protokollzeile));
            }

            int neu = Neue(protokollDatei, ref zeilenVorher);
            Pruefe(neu == 1, "genau eine Protokollzeile (gemessen: " + neu + ")");

            // Zweiter Anlauf mit DERSELBEN Freigabe ist unmoeglich: die Werkzeugrunde legt
            // je Aufruf eine neue an. Geprueft wird stattdessen, dass eine zweite Runde
            // wieder eine Bestaetigung verlangt - kein „ab jetzt immer".
            int gefragt = 0;
            KiChatService.Bestaetigungsweg = (f, tok) => { gefragt++; return Task.FromResult(KiEntscheidung.Abgelehnt); };
            Abzug v2 = Abzug.Nehmen(eck);
            Schreibversuch(eck, v2.Betrag + 10.0);
            Abzug n2 = Abzug.Nehmen(eck);
            Pruefe(gefragt == 1, "die naechste Schreibaktion fragt wieder - kein ab-jetzt-immer");
            Pruefe(Math.Abs(n2.Betrag - v2.Betrag) < 0.005, "und schreibt bei Ablehnung nichts");
            Neue(protokollDatei, ref zeilenVorher);

            return zeilenVorher;
        }

        private const string KiSicherungsordner = "DB-Backup";

        // ================================================================ Fall 6

        /// <summary>
        /// Schreibschutz: ein als <c>ReadOnly</c> gekennzeichneter Satz wird abgelehnt -
        /// auch mit Bestaetigung (Fachkonzept 4.5).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Die drei Zieltabellen der Schreibaktionen sind Projekttabellen und fuehren im
        /// heutigen Schema KEIN Feld <c>ReadOnly</c> - es sitzt in den <c>*_STAMM</c>-
        /// Katalogen, an die der Assistent gar nicht erst andockt. Die Wache ist deshalb
        /// schematolerant gebaut, und genau das wird hier nachgewiesen: In der
        /// ARBEITSKOPIE - und nur dort - bekommt <c>Tab_ProjektWerte</c> das Feld, so wie
        /// eine spaetere Migration es nachtragen koennte. Danach muss der VOLLE Weg
        /// (Modellaufruf, Vorschau, erteilte Bestaetigung) an der Wache scheitern, ohne
        /// dass am Assistenten etwas geaendert wurde.
        /// </para>
        /// <para>
        /// Ein Ersatznachweis am Katalog waere schwaecher gewesen: Dort greift schon die
        /// Katalogregel, und der ReadOnly-Pfad selbst bliebe ungelaufen.
        /// </para>
        /// </remarks>
        private static int SchreibschutzPruefen(Eckwerte eck, string protokollDatei, int zeilenVorher)
        {
            _log.Leerzeile();
            _log.Zeile("--- Fall 6: ReadOnly-Satz ---");

            bool imBestand = KiSchreibschutz.FuehrtSchreibschutz("Tab_ProjektWerte");
            bool angelegt = !imBestand && SpalteAnlegen("Tab_ProjektWerte", "ReadOnly");

            _log.Zeile("Tab_ProjektWerte fuehrt ein Feld ReadOnly: " +
                       (imBestand ? "ja (im Bestand)"
                                  : angelegt ? "ja (fuer diesen Prueflauf in der Arbeitskopie angelegt)"
                                             : "nein"));

            if (imBestand || angelegt)
            {
                Ausfuehren("UPDATE Tab_ProjektWerte SET [ReadOnly] = TRUE WHERE ID = ?", eck.IdPosition);
                Pruefe(KiSchreibschutz.Gesperrt("Tab_ProjektWerte", "ID", eck.IdPosition) != null,
                       "die Wache erkennt den gesetzten Schreibschutz");

                try
                {
                    KiAusfuehrer.Schreibrecht = () => true;
                    KiChatService.Bestaetigungsweg = (f, tok) => Task.FromResult(KiEntscheidung.Erteilt);

                    Abzug vorher = Abzug.Nehmen(eck);
                    KiAntwort antwort = Schreibversuch(eck, vorher.Betrag + 6666.0);
                    Abzug nachher = Abzug.Nehmen(eck);

                    Vergleiche(vorher, nachher, "keine Aenderung am geschuetzten Satz");
                    PruefeSchritt(antwort, false);
                    if (antwort.Schritte.Count == 1)
                        _log.Roh("      Grund: " + Einzeilig(antwort.Schritte[0].Grund));

                    int neu = Neue(protokollDatei, ref zeilenVorher);
                    Pruefe(neu == 1, "genau eine Protokollzeile (gemessen: " + neu + ")");
                }
                finally
                {
                    Ausfuehren("UPDATE Tab_ProjektWerte SET [ReadOnly] = FALSE WHERE ID = ?", eck.IdPosition);
                }

                Pruefe(KiSchreibschutz.Gesperrt("Tab_ProjektWerte", "ID", eck.IdPosition) == null,
                       "ein zurueckgenommener Schreibschutz gibt den Satz wieder frei");

                if (angelegt) SpalteEntfernen("Tab_ProjektWerte", "ReadOnly");
            }
            else
            {
                _log.Warnung("Das Feld ReadOnly liess sich in der Arbeitskopie nicht anlegen - " +
                             "der Nachweis am vollen Weg entfaellt.");
            }

            // Katalogsaetze werden pauschal abgewiesen, ohne den Feldwert ueberhaupt zu
            // lesen: Katalogpflege ist gar nicht erst Aufgabe des Assistenten (1.2).
            Pruefe(KiSchreibschutz.IstKatalogtabelle("Tab_BHKW_STAMM"),
                   "Tab_BHKW_STAMM gilt als Katalogtabelle");
            Pruefe(!KiSchreibschutz.IstKatalogtabelle("Tab_ProjektWerte"),
                   "Tab_ProjektWerte gilt nicht als Katalogtabelle");
            Pruefe(KiSchreibschutz.Gesperrt("Tab_BHKW_STAMM", "ID", 1) != null,
                   "in einen Katalog koennte auch mit Bestaetigung nichts geschrieben werden");

            return zeilenVorher;
        }

        /// <summary>Legt eine Ja/Nein-Spalte in der ARBEITSKOPIE an; false, wenn es nicht geht.</summary>
        private static bool SpalteAnlegen(string tabelle, string spalte)
        {
            try
            {
                DataRepository.ExecuteSQL("ALTER TABLE [" + tabelle + "] ADD COLUMN [" + spalte + "] YESNO");
                return KiSchreibschutz.FuehrtSchreibschutz(tabelle);
            }
            catch (Exception ex)
            {
                _log.Warnung("Spalte " + spalte + " nicht anlegbar: " + ex.Message);
                return false;
            }
        }

        /// <summary>Nimmt die Pruefspalte wieder zurueck.</summary>
        private static void SpalteEntfernen(string tabelle, string spalte)
        {
            try
            {
                DataRepository.ExecuteSQL("ALTER TABLE [" + tabelle + "] DROP COLUMN [" + spalte + "]");
            }
            catch (Exception ex) { _log.Warnung("Spalte " + spalte + " nicht entfernbar: " + ex.Message); }
        }

        // ================================================================ Fall 7

        /// <summary>
        /// Stimmt die Vorschau mit dem Ergebnis ueberein? Geprueft am Projektnamen, den
        /// <c>variante_anlegen</c> ankuendigt und danach wirklich vergibt.
        /// </summary>
        private static int VorschauTreue(Eckwerte eck, string protokollDatei, int zeilenVorher)
        {
            _log.Leerzeile();
            _log.Zeile("--- Fall 7: Vorschau und Ergebnis stimmen ueberein (variante_anlegen) ---");

            if (eck.IdStamm <= 0)
            {
                _log.Warnung("Kein geeignetes Stammprojekt gefunden.");
                return zeilenVorher;
            }

            string bezeichner = "KI-Pruefung " +
                                DateTime.Now.ToString("HHmmss", CultureInfo.InvariantCulture);
            string vorschau = "";

            KiAusfuehrer.Schreibrecht = () => true;
            KiChatService.Bestaetigungsweg = (f, tok) =>
            {
                vorschau = f.Text;
                return Task.FromResult(KiEntscheidung.Erteilt);
            };

            int projekteVorher = Skalar("SELECT COUNT(*) FROM Tab_Projekt");

            Gerufen.Add("variante_anlegen");
            Werkzeugrunde.Kanal(
                Werkzeugrunde.Werkzeugantwort("variante_anlegen",
                    "{\"stamm_id\":" + eck.IdStamm + ",\"bezeichner\":" + Json(bezeichner) + "}"),
                Werkzeugrunde.Textantwort("Die Variante ist angelegt."));

            KiAntwort antwort = Frage("Bitte eine Variante anlegen.");

            int projekteNachher = Skalar("SELECT COUNT(*) FROM Tab_Projekt");
            string neuerName = Text("SELECT MAX(Projektname) FROM Tab_Projekt WHERE Projektname LIKE ?",
                                    "%" + bezeichner + "%");

            _log.Roh("      Projekte vorher/nachher: " + projekteVorher + " / " + projekteNachher);
            _log.Roh("      Angelegter Projektname : " + neuerName);
            foreach (string z in Zeilen(vorschau)) _log.Roh("      " + z);

            PruefeSchritt(antwort, true);
            Pruefe(projekteNachher == projekteVorher + 1,
                   "genau ein Projekt mehr (" + projekteVorher + " -> " + projekteNachher + ")");
            Pruefe(neuerName.Length > 0, "die Variante ist auffindbar");
            Pruefe(neuerName.Length > 0 && vorschau.Contains(neuerName),
                   "die Vorschau hat genau diesen Namen angekuendigt");

            int neu = Neue(protokollDatei, ref zeilenVorher);
            Pruefe(neu == 1, "genau eine Protokollzeile (gemessen: " + neu + ")");
            return zeilenVorher;
        }

        // ================================================================ Fall 8

        /// <summary>Die umkehrbare Aktion: aktive Speichervariante umsetzen, Vorzustand belegt.</summary>
        private static int SpeichervariantePruefen(Eckwerte eck, string protokollDatei, int zeilenVorher)
        {
            _log.Leerzeile();
            _log.Zeile("--- Fall 8: speichervariante_aktiv_setzen (umkehrbar) ---");

            if (eck.IdSpeicherProjekt <= 0 || eck.IdSpeicherVariante <= 0)
            {
                _log.Warnung("Keine Speichervariante in der Arbeitskopie - Fall 8 entfaellt.");
                return zeilenVorher;
            }

            KiAusfuehrer.Schreibrecht = () => true;
            string vorschau = "";
            KiChatService.Bestaetigungsweg = (f, tok) => { vorschau = f.Text; return Task.FromResult(KiEntscheidung.Erteilt); };

            int aktivVorher = Skalar(
                "SELECT MIN(v.ID) FROM Tab_StromspeicherVariante AS v " +
                "INNER JOIN Tab_Energieanlagen AS a ON v.ID_Energieanlage = a.ID " +
                "WHERE a.ID_Projekt = " + eck.IdSpeicherProjekt + " AND v.Aktiv = TRUE");

            Gerufen.Add("speichervariante_aktiv_setzen");
            Werkzeugrunde.Kanal(
                Werkzeugrunde.Werkzeugantwort("speichervariante_aktiv_setzen",
                    "{\"projekt_id\":" + eck.IdSpeicherProjekt + ",\"variante_id\":" +
                    eck.IdSpeicherVariante + "}"),
                Werkzeugrunde.Textantwort("Die Variante ist jetzt aktiv."));

            KiAntwort antwort = Frage("Bitte diese Speichervariante aktiv setzen.");

            int aktivNachher = Skalar(
                "SELECT MIN(v.ID) FROM Tab_StromspeicherVariante AS v " +
                "INNER JOIN Tab_Energieanlagen AS a ON v.ID_Energieanlage = a.ID " +
                "WHERE a.ID_Projekt = " + eck.IdSpeicherProjekt + " AND v.Aktiv = TRUE");
            int anzahlAktiv = Skalar(
                "SELECT COUNT(*) FROM Tab_StromspeicherVariante AS v " +
                "INNER JOIN Tab_Energieanlagen AS a ON v.ID_Energieanlage = a.ID " +
                "WHERE a.ID_Projekt = " + eck.IdSpeicherProjekt + " AND v.Aktiv = TRUE");

            _log.Roh("      Aktive Variante vorher/nachher: " + aktivVorher + " / " + aktivNachher);
            foreach (string z in Zeilen(vorschau)) _log.Roh("      " + z);

            PruefeSchritt(antwort, true);
            Pruefe(aktivNachher == eck.IdSpeicherVariante,
                   "die gewuenschte Variante ist aktiv (" + aktivNachher + ")");
            Pruefe(anzahlAktiv == 1, "es ist GENAU EINE Variante aktiv (gemessen: " + anzahlAktiv + ")");
            Pruefe(vorschau.Contains(KiTexte.RueckholbarJa),
                   "die Bestaetigung nennt die Aktion ausdruecklich rueckholbar");

            int neu = Neue(protokollDatei, ref zeilenVorher);
            Pruefe(neu == 1, "genau eine Protokollzeile (gemessen: " + neu + ")");
            return zeilenVorher;
        }

        // ================================================================ Fall 9

        /// <summary>
        /// Modalitaet: Steht ein modaler Dialog offen, laeuft nichts, was schreibt
        /// (Fachkonzept 3.4, Pflicht 2). Geprueft wird an BEIDEN Kanten - die Vorbereitung
        /// weist ab, bevor eine Vorschau entsteht, und der Lauf weist ein zweites Mal ab.
        /// </summary>
        private static int ModalitaetPruefen(Eckwerte eck, string protokollDatei, int zeilenVorher)
        {
            _log.Leerzeile();
            _log.Zeile("--- Fall 9: modaler Dialog offen ---");

            KiAusfuehrer.Schreibrecht = () => true;
            bool gefragt = false;
            KiChatService.Bestaetigungsweg = (f, tok) =>
            {
                gefragt = true;
                return Task.FromResult(KiEntscheidung.Erteilt);
            };

            KiAusfuehrer.ModalerDialog = () => true;
            try
            {
                Abzug vorher = Abzug.Nehmen(eck);
                KiAntwort antwort = Schreibversuch(eck, vorher.Betrag + 9999.0);
                Abzug nachher = Abzug.Nehmen(eck);

                Vergleiche(vorher, nachher, "keine Aenderung bei offenem modalem Dialog");
                PruefeSchritt(antwort, false);
                Pruefe(!gefragt, "es wurde gar nicht erst nach einer Bestaetigung gefragt");
                if (antwort.Schritte.Count == 1)
                    _log.Roh("      Grund: " + Einzeilig(antwort.Schritte[0].Grund));

                int neu = Neue(protokollDatei, ref zeilenVorher);
                Pruefe(neu == 1, "genau eine Protokollzeile (gemessen: " + neu + ")");
            }
            finally
            {
                KiAusfuehrer.ModalerDialog = () => false;
            }

            // Gegenprobe: ohne modalen Dialog laeuft dieselbe Aktion wieder.
            Abzug v2 = Abzug.Nehmen(eck);
            double ziel = Math.Round(v2.Betrag + 7.77, 2);
            KiAntwort a2 = Schreibversuch(eck, ziel);
            Abzug n2 = Abzug.Nehmen(eck);

            _log.Roh("      Gegenprobe Betrag vorher/nachher: " + Z(v2.Betrag) + " / " + Z(n2.Betrag));
            PruefeSchritt(a2, true);
            Pruefe(Math.Abs(n2.Betrag - ziel) < 0.005, "ohne modalen Dialog laeuft die Aktion wieder");
            Neue(protokollDatei, ref zeilenVorher);

            return zeilenVorher;
        }

        // ===================================================================== Ablauf

        /// <summary>Ein vollstaendiger Schreibversuch ueber die Werkzeugrunde.</summary>
        private static KiAntwort Schreibversuch(Eckwerte eck, double betrag)
        {
            Gerufen.Add("kostenposition_setzen");
            Werkzeugrunde.Kanal(
                Werkzeugrunde.Werkzeugantwort("kostenposition_setzen",
                    "{\"projekt_id\":" + eck.IdProjekt +
                    ",\"positions_id\":" + eck.IdPosition +
                    ",\"betrag\":" + betrag.ToString("0.##", CultureInfo.InvariantCulture) + "}"),
                Werkzeugrunde.Textantwort("Ich habe es notiert."));

            return Frage("Bitte den Betrag setzen.");
        }

        private static KiAntwort Frage(string text)
        {
            try
            {
                return KiChatService
                    .FrageMitAktionenAsync(text, KONTEXT, null, new KiPlatzhalter(), null,
                                           CancellationToken.None)
                    .GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.FehlerZeile("Schreibrunde: AUSNAHME nach aussen durchgeschlagen - " + ex);
                return new KiAntwort { Fehler = ex.Message };
            }
        }

        private static void PruefeSchritt(KiAntwort antwort, bool erwarteAusgefuehrt)
        {
            Pruefe(antwort.Schritte.Count == 1,
                   "genau ein Aktionsschritt (gemessen: " + antwort.Schritte.Count + ")");
            if (antwort.Schritte.Count != 1) return;

            KiSchritt s = antwort.Schritte[0];
            Pruefe(s.Bestaetigungspflichtig, "der Schritt ist als bestaetigungspflichtig vermerkt");
            Pruefe(s.Ausgefuehrt == erwarteAusgefuehrt,
                   erwarteAusgefuehrt ? "die Aktion ist gelaufen: " + Einzeilig(s.Grund)
                                      : "die Aktion ist NICHT gelaufen");
            if (!erwarteAusgefuehrt)
                Pruefe(s.Grund.Length > 0, "es liegt ein Klartextgrund vor");
        }

        /// <summary>Die Ablehnung muss als sachliche functionResponse zurueckgehen.</summary>
        private static void PruefeAblehnungAnsModell()
        {
            IReadOnlyList<string> anfragen = Werkzeugrunde.Anfragen;
            Pruefe(anfragen.Count >= 2, "eine zweite Runde ging an das Modell (gemessen: " +
                                        anfragen.Count + ")");
            if (anfragen.Count < 2) return;

            string zweite = anfragen[1];
            Pruefe(zweite.Contains("functionResponse"), "Ergebnis als functionResponse zurueck");
            Pruefe(zweite.Contains("abgelehnt"), "Status abgelehnt in der Rueckmeldung");
        }

        // ===================================================================== Abzug

        /// <summary>Der Datenstand vor bzw. nach einem Versuch - die Zahlen des Berichts.</summary>
        private sealed class Abzug
        {
            internal double Betrag;
            internal DateTime Aenderungsdatum;
            internal int Projekte;
            internal int Positionen;
            internal int Varianten;

            internal static Abzug Nehmen(Eckwerte eck)
            {
                return new Abzug
                {
                    Betrag = Gleit("SELECT EingegebenerWert FROM Tab_ProjektWerte WHERE ID = " + eck.IdPosition),
                    Aenderungsdatum = Datum("SELECT Aenderungsdatum FROM Tab_Projekt WHERE ID = " + eck.IdProjekt),
                    Projekte = Skalar("SELECT COUNT(*) FROM Tab_Projekt"),
                    Positionen = Skalar("SELECT COUNT(*) FROM Tab_ProjektWerte"),
                    Varianten = Skalar("SELECT COUNT(*) FROM Tab_Variante")
                };
            }

            public override string ToString()
                => "Betrag=" + Z(Betrag) + ", geaendert=" +
                   Aenderungsdatum.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) +
                   ", Projekte=" + Projekte + ", Positionen=" + Positionen + ", Varianten=" + Varianten;
        }

        private static void Vergleiche(Abzug vorher, Abzug nachher, string was)
        {
            _log.Roh("      vorher : " + vorher);
            _log.Roh("      nachher: " + nachher);

            bool gleich = Math.Abs(vorher.Betrag - nachher.Betrag) < 0.005
                          && vorher.Aenderungsdatum == nachher.Aenderungsdatum
                          && vorher.Projekte == nachher.Projekte
                          && vorher.Positionen == nachher.Positionen
                          && vorher.Varianten == nachher.Varianten;

            Pruefe(gleich, was + " - 0 Schreibvorgaenge in der Arbeitskopie");
        }

        // ===================================================================== Eckwerte

        private sealed class Eckwerte
        {
            internal int IdPosition;
            internal int IdProjekt;
            internal int IdStamm;
            internal int IdSpeicherProjekt;
            internal int IdSpeicherVariante;

            public override string ToString()
                => "Position=" + IdPosition + " (Projekt " + IdProjekt + ")" +
                   ", Stammprojekt=" + IdStamm +
                   ", Speicherprojekt=" + IdSpeicherProjekt + "/Variante " + IdSpeicherVariante;
        }

        private static Eckwerte EckwerteLesen()
        {
            var e = new Eckwerte();

            e.IdPosition = Skalar(
                "SELECT MIN(w.ID) FROM Tab_ProjektWerte AS w " +
                "INNER JOIN Tab_Projekt AS p ON w.ProjektID = p.ID");
            if (e.IdPosition > 0)
                e.IdProjekt = Skalar("SELECT ProjektID FROM Tab_ProjektWerte WHERE ID = " + e.IdPosition);

            // Ein Projekt, das SELBST keine Variante ist - nur zu einem Stamm entsteht eine.
            e.IdStamm = Skalar(
                "SELECT MIN(p.ID) FROM Tab_Projekt AS p " +
                "WHERE p.ID NOT IN (SELECT ID_Projekt FROM Tab_Variante)");
            if (e.IdStamm <= 0) e.IdStamm = Skalar("SELECT MIN(ID) FROM Tab_Projekt");

            e.IdSpeicherVariante = Skalar("SELECT MIN(ID) FROM Tab_StromspeicherVariante");
            if (e.IdSpeicherVariante > 0)
                e.IdSpeicherProjekt = Skalar(
                    "SELECT MIN(a.ID_Projekt) FROM Tab_StromspeicherVariante AS v " +
                    "INNER JOIN Tab_Energieanlagen AS a ON v.ID_Energieanlage = a.ID " +
                    "WHERE v.ID = " + e.IdSpeicherVariante);

            return e;
        }

        // ===================================================================== Hilfen

        private static int Skalar(string sql)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(sql);
                return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            catch { return 0; }
        }

        private static double Gleit(string sql)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(sql);
                return o == null || o == DBNull.Value ? 0.0 : Convert.ToDouble(o, CultureInfo.InvariantCulture);
            }
            catch { return 0.0; }
        }

        private static DateTime Datum(string sql)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(sql);
                return o == null || o == DBNull.Value ? default(DateTime) : Convert.ToDateTime(o);
            }
            catch { return default(DateTime); }
        }

        private static string Text(string sql, string muster)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(sql, new OleDbParameter("@m", muster));
                return o == null || o == DBNull.Value ? "" : Convert.ToString(o);
            }
            catch { return ""; }
        }

        private static void Ausfuehren(string sql, params object[] werte)
        {
            try
            {
                var ps = new OleDbParameter[werte.Length];
                for (int i = 0; i < werte.Length; i++) ps[i] = new OleDbParameter("@p" + i, werte[i]);
                DataRepository.ExecuteSQL(sql, ps);
            }
            catch (Exception ex) { _log.Warnung("Vorbereitung fehlgeschlagen: " + ex.Message); }
        }

        private static string Json(string text)
            => System.Text.Json.JsonSerializer.Serialize(text);

        private static string Z(double wert)
            => wert.ToString("0.00", CultureInfo.InvariantCulture);

        private static string[] Zeilen(string text)
            => (text ?? "").Replace("\r\n", "\n").TrimEnd().Split('\n');

        private static string Einzeilig(string text)
            => (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();

        private static int Neue(string protokollDatei, ref int zeilenVorher)
        {
            int jetzt = Protokollzeilen(protokollDatei);
            int neu = jetzt - zeilenVorher;
            zeilenVorher = jetzt;
            return neu;
        }

        private static int Protokollzeilen(string datei)
        {
            if (!File.Exists(datei)) return 0;
            try
            {
                int n = 0;
                foreach (string z in File.ReadLines(datei, System.Text.Encoding.UTF8))
                    if (z.Length > 0 && !z.StartsWith("#", StringComparison.Ordinal)) n++;
                return n;
            }
            catch { return 0; }
        }

        private static void Pruefe(bool bedingung, string was)
        {
            _geprueft++;
            if (bedingung)
            {
                _log.Roh("      OK      " + was);
            }
            else
            {
                _gefallen++;
                _log.FehlerZeile("Schreibrunde: " + was);
            }
        }
    }
}
