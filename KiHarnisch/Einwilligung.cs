using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using KiKern;

namespace WindowsFormsApplication1.Referenzlauf
{
    /// <summary>
    /// Prüfteil „Rechtshinweis und Abschalter" - OHNE NETZ, OHNE SCHLÜSSEL, OHNE DIALOG.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Geprüft wird die eine Zusage, auf der der ganze Rechtshinweis steht: <b>ohne
    /// Einwilligung geht keine Anfrage hinaus.</b> Nachweisbar ist das nur negativ - der
    /// eingespeiste <see cref="KiChatService.Modellkanal"/> zählt jeden Aufruf mit, den
    /// der Dienst gemacht hätte. Bleibt der Zähler bei null, ist nichts gesendet worden.
    /// </para>
    /// <para>
    /// Vier Fälle: Abschalter gesetzt · Einwilligung fehlt · Einwilligung erteilt ·
    /// Hinweisfassung erhöht. Der Nachfragehaken wird dabei mit einer Attrappe belegt,
    /// die zählt statt einen Dialog zu zeigen (der Harnisch darf keinen öffnen -
    /// <see cref="DialogWaechter"/> würde ihn als Testfehler melden).
    /// </para>
    /// <para>
    /// <b>Registry.</b> Die geprüften Werte liegen in <c>HKCU\Software\wp-plan</c> und
    /// gehören dem angemeldeten Benutzer. Sie werden vor dem Lauf gesichert und danach
    /// byte-genau wiederhergestellt - einschließlich des Falls „Wert war gar nicht da".
    /// </para>
    /// </remarks>
    internal static class Einwilligung
    {
        private const string REG_SCHLUESSEL = @"Software\wp-plan";
        private const string REG_BESTAETIGT = "KiHinweisBestaetigt";
        private const string REG_BESTAETIGT_AM = "KiHinweisBestaetigtAm";
        private const string REG_ABSCHALTER = "KiDeaktiviert";
        private const string KONTEXT = "Projektverwaltung";

        private static readonly string[] WERTE = { REG_BESTAETIGT, REG_BESTAETIGT_AM, REG_ABSCHALTER };

        private static readonly Dictionary<string, string> _sicherung =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private static bool _gesichert;

        private static int _geprueft;
        private static int _gefallen;
        private static Protokoll _log;

        /// <summary>Modellaufrufe seit dem letzten <see cref="ZaehlerNullen"/>.</summary>
        private static int _modellaufrufe;

        /// <summary>Aufrufe des Nachfragehakens seit dem letzten <see cref="ZaehlerNullen"/>.</summary>
        private static int _nachfragen;

        // =====================================================================
        // Sicherung
        // =====================================================================

        /// <summary>Sichert die drei Registry-Werte; Aufruf VOR allem anderen.</summary>
        internal static void Sichern(Protokoll log)
        {
            _sicherung.Clear();
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REG_SCHLUESSEL))
                    foreach (string w in WERTE)
                        _sicherung[w] = key == null ? null : key.GetValue(w) as string;

                _gesichert = true;
                foreach (KeyValuePair<string, string> p in _sicherung)
                    log.Roh("      gesichert: " + p.Key + " = " +
                            (p.Value == null ? "(nicht vorhanden)" : "\"" + p.Value + "\""));
            }
            catch (Exception ex)
            {
                log.FehlerZeile("Registry-Sicherung fehlgeschlagen: " + ex.Message);
            }
        }

        /// <summary>
        /// Stellt den Ausgangszustand wieder her. Mehrfachaufruf ist unschaedlich - der
        /// erste Aufruf steht im Protokoll, der zweite (aus dem finally) tut nichts mehr.
        /// </summary>
        internal static void Wiederherstellen(Protokoll log)
        {
            if (!_gesichert) return;
            _gesichert = false;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(REG_SCHLUESSEL))
                {
                    if (key == null) return;
                    foreach (KeyValuePair<string, string> p in _sicherung)
                    {
                        if (p.Value == null)
                        {
                            if (key.GetValue(p.Key) != null) key.DeleteValue(p.Key, false);
                        }
                        else key.SetValue(p.Key, p.Value);
                    }
                }
                log.Zeile("Registry-Werte des Rechtshinweises wiederhergestellt.");
            }
            catch (Exception ex)
            {
                log.FehlerZeile("Registry-Wiederherstellung fehlgeschlagen: " + ex.Message);
            }
        }

        // =====================================================================
        // Lauf
        // =====================================================================

        /// <summary>
        /// Führt die vier Fälle aus. Am Ende ist der Abschalter AUS und die Einwilligung
        /// für die aktuelle Fassung erteilt - so, wie die anschliessende Werkzeugrunde es
        /// braucht.
        /// </summary>
        internal static void Pruefen(Protokoll log)
        {
            _log = log;
            _geprueft = 0;
            _gefallen = 0;

            Func<bool> hakenVorher = KiEinwilligung.Nachfragen;
            int anfragenVorher = KiChatService.AnfragenHeute;

            // Eine maschinenweite Sperre (HKLM) laesst sich aus dem Programm heraus nicht
            // loesen - genau so ist sie gemeint. Dieser Harnisch koennte dann aber Fall 3
            // und 4 nicht pruefen; das muss sichtbar sein statt still schiefzugehen.
            if (KiEinwilligung.AbschalterMaschine)
                _log.FehlerZeile("HKLM\\Software\\wp-plan\\KiDeaktiviert ist gesetzt - " +
                                 "die Faelle 3 und 4 sind auf diesem Rechner nicht pruefbar.");

            try
            {
                AbschalterPruefen();
                OhneEinwilligungPruefen();
                MitEinwilligungPruefen();
                FassungPruefen();
            }
            finally
            {
                KiChatService.Modellkanal = null;
                KiEinwilligung.Nachfragen = hakenVorher;
            }

            Pruefe(KiChatService.AnfragenHeute == anfragenVorher,
                   "keine Anfrage gezaehlt (" + anfragenVorher + " vorher, " +
                   KiChatService.AnfragenHeute + " nachher)");

            // Ausgangslage fuer die Werkzeugrunde: abgeschaltet nein, eingewilligt ja.
            AbschalterSetzen(false);
            KiEinwilligung.Erteilen();

            _log.Leerzeile();
            _log.Zeile("Rechtshinweis: " + (_geprueft - _gefallen) + " von " + _geprueft +
                       " Pruefungen bestanden.");
        }

        // --------------------------------------------------------------- Fall 1

        /// <summary>Abschalter der Installation: nichts ist nutzbar, nichts geht hinaus.</summary>
        private static void AbschalterPruefen()
        {
            _log.Leerzeile();
            _log.Zeile("--- Fall 1: Abschalter der Installation gesetzt ---");

            KiEinwilligung.Erteilen();          // Einwilligung liegt vor …
            AbschalterSetzen(true);             // … der Abschalter schlaegt sie trotzdem

            Haken(true);                        // Nachfrage waere moeglich - darf nicht kommen
            ZaehlerNullen();

            Pruefe(KiEinwilligung.Abgeschaltet, "KiEinwilligung.Abgeschaltet meldet true");
            Pruefe(!KiEinwilligung.Erteilt, "KiEinwilligung.Erteilt meldet trotz Bestaetigung false");
            Pruefe(!KiEinwilligung.Sicherstellen(), "Sicherstellen() weist ab");

            KiAntwort hilfe = Hilfefrage("Wie lege ich ein Projekt an?");
            KiAntwort aktion = Aktionsfrage("Welche Projekte gibt es?");

            string erwartet = MyResource.Resource.KI_ABSCHALTER_MELDUNG;
            Pruefe(!hilfe.Erfolg && hilfe.Fehler == erwartet,
                   "Hilfefall mit Abschaltermeldung abgewiesen: " + Einzeilig(hilfe.Fehler));
            Pruefe(!aktion.Erfolg && aktion.Fehler == erwartet,
                   "Aktionsbetrieb mit Abschaltermeldung abgewiesen: " + Einzeilig(aktion.Fehler));
            Pruefe(_modellaufrufe == 0, "KEIN Modellaufruf (gemessen: " + _modellaufrufe + ")");
            Pruefe(_nachfragen == 0, "keine Nachfrage gestellt (gemessen: " + _nachfragen + ")");
            Pruefe(aktion.Schritte.Count == 0, "keine Aktion ausgefuehrt");
        }

        // --------------------------------------------------------------- Fall 2

        /// <summary>Ohne Einwilligung und ohne Nachfragehaken darf nichts hinausgehen.</summary>
        private static void OhneEinwilligungPruefen()
        {
            _log.Leerzeile();
            _log.Zeile("--- Fall 2: keine Einwilligung, kein Dialog eingehaengt ---");

            AbschalterSetzen(false);
            EinwilligungLoeschen();
            Haken(false);                       // wie im Harnisch: gar kein Weg zur Zustimmung
            ZaehlerNullen();

            Pruefe(KiEinwilligung.BestaetigteFassung == 0, "keine bestaetigte Fassung hinterlegt");
            Pruefe(!KiEinwilligung.Erteilt, "KiEinwilligung.Erteilt meldet false");
            Pruefe(!KiEinwilligung.Sicherstellen(), "Sicherstellen() weist ohne Haken ab");

            KiAntwort hilfe = Hilfefrage("Wie lege ich ein Projekt an?");
            KiAntwort aktion = Aktionsfrage("Welche Projekte gibt es?");

            string erwartet = MyResource.Resource.KI_HINWEIS_ABGELEHNT;
            Pruefe(!hilfe.Erfolg && hilfe.Fehler == erwartet,
                   "Hilfefall mit Einwilligungsmeldung abgewiesen: " + Einzeilig(hilfe.Fehler));
            Pruefe(!aktion.Erfolg && aktion.Fehler == erwartet,
                   "Aktionsbetrieb mit Einwilligungsmeldung abgewiesen: " + Einzeilig(aktion.Fehler));
            Pruefe(_modellaufrufe == 0, "KEIN Modellaufruf (gemessen: " + _modellaufrufe + ")");
            Pruefe(KiEinwilligung.BestaetigteFassung == 0, "es wurde nichts stillschweigend gemerkt");

            // Und der Anwender, der ablehnt: der Haken liefert false.
            Haken(true, antwort: false);
            ZaehlerNullen();
            KiAntwort abgelehnt = Aktionsfrage("Welche Projekte gibt es?");

            Pruefe(_nachfragen == 1, "genau einmal nachgefragt (gemessen: " + _nachfragen + ")");
            Pruefe(!abgelehnt.Erfolg, "nach Ablehnung abgewiesen: " + Einzeilig(abgelehnt.Fehler));
            Pruefe(_modellaufrufe == 0, "KEIN Modellaufruf nach Ablehnung (gemessen: " + _modellaufrufe + ")");
            Pruefe(KiEinwilligung.BestaetigteFassung == 0, "Ablehnung wurde NICHT als Zustimmung gemerkt");
        }

        // --------------------------------------------------------------- Fall 3

        /// <summary>Mit Einwilligung laeuft alles - und sie wird genau einmal eingeholt.</summary>
        private static void MitEinwilligungPruefen()
        {
            _log.Leerzeile();
            _log.Zeile("--- Fall 3: Einwilligung wird erteilt ---");

            AbschalterSetzen(false);
            EinwilligungLoeschen();
            Haken(true, antwort: true);
            ZaehlerNullen();

            KiAntwort erste = Aktionsfrage("Welche Projekte gibt es?");

            Pruefe(_nachfragen == 1, "genau einmal nachgefragt (gemessen: " + _nachfragen + ")");
            Pruefe(erste.Erfolg, "Anfrage laeuft: " + Einzeilig(erste.Fehler));
            Pruefe(_modellaufrufe > 0, "Modellkanal wurde gerufen (gemessen: " + _modellaufrufe + ")");
            Pruefe(erste.Schritte.Count == 1, "ein Aktionsschritt (gemessen: " + erste.Schritte.Count + ")");
            Pruefe(KiEinwilligung.BestaetigteFassung == KiEinwilligung.FASSUNG,
                   "Fassung " + KiEinwilligung.FASSUNG + " gemerkt (gemessen: " +
                   KiEinwilligung.BestaetigteFassung + ")");
            Pruefe(KiEinwilligung.BestaetigtAm.Length > 0,
                   "Zeitpunkt gemerkt: " + KiEinwilligung.BestaetigtAm);
            Pruefe(KiEinwilligung.Erteilt, "KiEinwilligung.Erteilt meldet true");

            // Zweite Frage: es darf NICHT erneut gefragt werden.
            ZaehlerNullen();
            KiAntwort zweite = Aktionsfrage("Und wie viele sind es?");

            Pruefe(_nachfragen == 0, "keine erneute Nachfrage (gemessen: " + _nachfragen + ")");
            Pruefe(zweite.Erfolg, "zweite Anfrage laeuft: " + Einzeilig(zweite.Fehler));
            Pruefe(_modellaufrufe > 0, "Modellkanal erneut gerufen (gemessen: " + _modellaufrufe + ")");
        }

        // --------------------------------------------------------------- Fall 4

        /// <summary>
        /// Erhoehte Hinweisfassung: die alte Zustimmung deckt sie nicht ab, es wird
        /// erneut gefragt.
        /// </summary>
        /// <remarks>
        /// <see cref="KiEinwilligung.FASSUNG"/> ist eine Konstante und laesst sich zur
        /// Laufzeit nicht anheben. Nachgestellt wird die Lage deshalb ueber die HINTERLEGTE
        /// Nummer: „Text geaendert, FASSUNG von n auf n+1 erhoeht" ist genau die Lage
        /// „hinterlegt ist n, verlangt wird n+1" - also ein hinterlegter Wert UNTERHALB
        /// der verlangten Fassung. Geprueft wird der Vergleich an beiden Raendern: gleich
        /// (keine Nachfrage), kleiner (Nachfrage), groesser (keine Nachfrage - eine
        /// spaetere Fassung entwertet die Zustimmung nicht).
        /// </remarks>
        private static void FassungPruefen()
        {
            _log.Leerzeile();
            _log.Zeile("--- Fall 4: Hinweistext geaendert, Fassung erhoeht ---");

            AbschalterSetzen(false);
            Haken(true, antwort: true);

            // ---- Rand 1: hinterlegt == verlangt -> keine Nachfrage
            FassungSetzen(KiEinwilligung.FASSUNG);
            ZaehlerNullen();
            KiAntwort gleich = Aktionsfrage("Welche Projekte gibt es?");
            Pruefe(KiEinwilligung.Erteilt, "hinterlegt " + KiEinwilligung.FASSUNG +
                                           " = verlangt " + KiEinwilligung.FASSUNG + ": gedeckt");
            Pruefe(_nachfragen == 0, "keine Nachfrage (gemessen: " + _nachfragen + ")");
            Pruefe(gleich.Erfolg, "laeuft durch: " + Einzeilig(gleich.Fehler));

            // ---- Rand 2: hinterlegt > verlangt -> eine spaetere Zustimmung bleibt gueltig
            FassungSetzen(KiEinwilligung.FASSUNG + 1);
            ZaehlerNullen();
            Pruefe(KiEinwilligung.Erteilt, "hinterlegt " + (KiEinwilligung.FASSUNG + 1) +
                                           " > verlangt " + KiEinwilligung.FASSUNG + ": gedeckt");
            KiAntwort spaeter = Aktionsfrage("Welche Projekte gibt es?");
            Pruefe(_nachfragen == 0, "keine Nachfrage (gemessen: " + _nachfragen + ")");
            Pruefe(spaeter.Erfolg, "laeuft durch: " + Einzeilig(spaeter.Fehler));

            // ---- Rand 3: hinterlegt < verlangt -> der eigentliche Fall
            int alt = KiEinwilligung.FASSUNG - 1;
            FassungSetzen(alt);
            Haken(false);                       // erst ohne Weg zur Zustimmung
            ZaehlerNullen();

            _log.Roh("      hinterlegte Fassung: " + KiEinwilligung.BestaetigteFassung +
                     ", verlangt: " + KiEinwilligung.FASSUNG +
                     " (Lage nach einer Texterweiterung mit erhoehter FASSUNG)");

            Pruefe(!KiEinwilligung.Erteilt, "hinterlegt " + alt + " < verlangt " +
                                            KiEinwilligung.FASSUNG + ": NICHT mehr gedeckt");

            KiAntwort ohne = Aktionsfrage("Welche Projekte gibt es?");
            Pruefe(!ohne.Erfolg, "abgewiesen: " + Einzeilig(ohne.Fehler));
            Pruefe(_modellaufrufe == 0, "KEIN Modellaufruf (gemessen: " + _modellaufrufe + ")");

            // Jetzt mit Weg zur Zustimmung: es MUSS erneut gefragt werden.
            Haken(true, antwort: true);
            ZaehlerNullen();
            KiAntwort mit = Aktionsfrage("Welche Projekte gibt es?");

            Pruefe(_nachfragen == 1, "erneut nachgefragt (gemessen: " + _nachfragen + ")");
            Pruefe(mit.Erfolg, "danach laeuft es: " + Einzeilig(mit.Fehler));
            Pruefe(KiEinwilligung.BestaetigteFassung == KiEinwilligung.FASSUNG,
                   "neue Fassung gemerkt (gemessen: " + KiEinwilligung.BestaetigteFassung + ")");
        }

        // =====================================================================
        // Attrappen
        // =====================================================================

        /// <summary>
        /// Legt den Nachfragehaken. <paramref name="eingehaengt"/> = false stellt den
        /// Harnischfall nach: es gibt gar keine Oberflaeche, die fragen koennte.
        /// </summary>
        private static void Haken(bool eingehaengt, bool antwort = true)
        {
            if (!eingehaengt) { KiEinwilligung.Nachfragen = null; return; }
            KiEinwilligung.Nachfragen = delegate { _nachfragen++; return antwort; };
        }

        /// <summary>
        /// Der eingespeiste Modellkanal zaehlt jeden Aufruf mit. Er liefert eine
        /// Werkzeugrunde (Aktion + Textantwort) - so entsteht bei erteilter Einwilligung
        /// ein vollstaendiger, echter Lauf gegen die Arbeitskopie.
        /// </summary>
        private static void ZaehlerNullen()
        {
            _modellaufrufe = 0;
            _nachfragen = 0;

            string[] antworten =
            {
                "{\"candidates\":[{\"content\":{\"role\":\"model\",\"parts\":[{\"functionCall\":" +
                "{\"name\":\"projekte_auflisten\",\"args\":{}}}]},\"finishReason\":\"STOP\"}]}",
                "{\"candidates\":[{\"content\":{\"role\":\"model\",\"parts\":[{\"text\":" +
                JsonSerializer.Serialize("In der Datenbank stehen mehrere Projekte.") +
                "}]},\"finishReason\":\"STOP\"}]}"
            };

            int i = 0;
            KiChatService.Modellkanal = delegate (string rumpf, string modell, CancellationToken tok)
            {
                string a = antworten[Math.Min(i, antworten.Length - 1)];
                i++;
                _modellaufrufe++;
                return Task.FromResult(a);
            };
        }

        private static KiAntwort Hilfefrage(string text)
        {
            try
            {
                return KiChatService.FrageAsync(text, KONTEXT, null).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _log.FehlerZeile("Hilfefrage: AUSNAHME nach aussen durchgeschlagen - " + ex);
                return new KiAntwort { Fehler = ex.Message };
            }
        }

        private static KiAntwort Aktionsfrage(string text)
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
                _log.FehlerZeile("Aktionsfrage: AUSNAHME nach aussen durchgeschlagen - " + ex);
                return new KiAntwort { Fehler = ex.Message };
            }
        }

        // =====================================================================
        // Registry von Hand (der Harnisch stellt Lagen her, die es sonst nicht gibt)
        // =====================================================================

        private static void AbschalterSetzen(bool aus)
        {
            KiEinwilligung.Abgeschaltet = aus;
        }

        private static void EinwilligungLoeschen()
        {
            KiEinwilligung.Zuruecknehmen();
        }

        /// <summary>
        /// Legt eine beliebige Fassungsnummer ins Register - auch eine, die es im Betrieb
        /// nicht geben kann. Nur so lassen sich beide Raender des Vergleichs pruefen.
        /// </summary>
        private static void FassungSetzen(int fassung)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(REG_SCHLUESSEL))
                    if (key != null)
                    {
                        key.SetValue(REG_BESTAETIGT,
                                     fassung.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        key.SetValue(REG_BESTAETIGT_AM, "2026-01-01 08:00");
                    }
            }
            catch (Exception ex) { _log.FehlerZeile("Fassung nicht setzbar: " + ex.Message); }
        }

        // =====================================================================
        // Hilfen
        // =====================================================================

        private static void Pruefe(bool bedingung, string was)
        {
            _geprueft++;
            if (bedingung) { _log.Roh("      OK    " + was); return; }
            _gefallen++;
            _log.FehlerZeile("Rechtshinweis: " + was);
        }

        private static string Einzeilig(string text)
        {
            return (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
        }
    }
}
