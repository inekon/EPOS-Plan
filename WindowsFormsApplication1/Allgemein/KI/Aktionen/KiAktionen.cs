using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das gefuellte Aktionsregister (Fachkonzept 3.2, Katalog 5.1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum hier und nicht im Kern.</b> Nur an dieser Stelle darf UI- und DB-Code
    /// stehen (Fachkonzept 3.7). Weil die Registerbefuellung in DERSELBEN Assembly liegt
    /// wie die Controller, ist weder <c>InternalsVisibleTo</c> noch eine oeffentliche
    /// Fassade noetig - <c>ProjektCtrl</c>, <c>TechnikPlanwertCtrl</c> und
    /// <c>KostenPositionCtrl</c> sind <c>internal</c> (Fachkonzept 5.5).
    /// </para>
    /// <para>
    /// <b>Umfang.</b> Stufe 1 (lesend) aus Fachkonzept 5.1 - OHNE <c>maske_oeffnen</c>
    /// und <c>projekt_oeffnen</c>: beide oeffnen blockierend-modal
    /// (<c>MenueCtrl.cs:251-389</c>, <c>:130</c>, <c>:178</c>) und gehoeren in eine
    /// spaetere Etappe. Dazu seit Etappe 3 die drei Schreibaktionen der Stufe 2
    /// (<see cref="KiAktionenSchreiben"/>). Stufe 3 (rechnen) ist noch nicht registriert;
    /// <c>KiRiegel.HoechsteStufe</c> weist sie ohnehin ab.
    /// </para>
    /// <para>
    /// <b><c>maske_oeffnen</c> bleibt auch nach Etappe 3b aussen vor.</b> Die vier
    /// Startmasken der Formularsteuerung lassen sich aus dem Bestand heraus nicht
    /// nebenwirkungsfrei oeffnen: <c>Form_Heizkessel_Bearbeiten</c>,
    /// <c>Form_PufferSp_Bearbeiten</c> und <c>Form_PV</c> brauchen Kontext (einen
    /// gewaehlten Katalogsatz bzw. ein offenes Projekt), und der einzige kontextfreie Weg -
    /// <c>MenueCtrl.WP_Administration</c> (<c>Controller\MenueCtrl.cs:250</c>) - ruft
    /// <c>ShowDialog()</c>. Blockierend-modal aus einer Aktion heraus geoeffnet, hielte er
    /// die Einlaeufigkeitssperre UND den prozessweiten dialogfreien Modus fuer die ganze
    /// Lebensdauer der Maske; damit waere kein einziger weiterer Assistentenaufruf mehr
    /// moeglich - gerade die Feldsetzung, fuer die die Maske geoeffnet werden sollte.
    /// Freigeschaltet wird die Aktion deshalb erst mit einem nicht blockierenden
    /// Oeffnungsweg (Bestandspflege, eigene Runde).
    /// </para>
    /// <para>
    /// <b>Regeln, die hier eingehalten werden.</b> Nur benannte Aktionen (kein generisches
    /// SQL, keine Reflexion); Parameter primitiv oder IDs aus einer Leseaktion;
    /// Aufzaehlungswerte, die auf Datenbankwerte abbilden, stammen aus
    /// <see cref="DbWerte"/> bzw. aus der Landkarte des jeweiligen Controllers - nie aus
    /// Modelltext; Zahlen invariant, Anzeige in <see cref="CultureInfo.CurrentCulture"/>.
    /// </para>
    /// </remarks>
    internal static class KiAktionen
    {
        /// <summary>Baut das vollstaendige Register.</summary>
        internal static KiRegister Erzeuge()
        {
            var register = new KiRegister();

            // ---- Projekte, Varianten, Speichervarianten
            register.Aufnehmen(KiAktionenProjekt.ProjekteAuflisten());

            // projekt_suchen gehoert unmittelbar daneben: Es ist die Antwort auf die
            // Frage „gibt es ein Projekt X?", die sich aus der platzgehaltenen Liste
            // NICHT beantworten laesst (Fachkonzept 4.2, Fehlerfall 23.08.2026).
            register.Aufnehmen(KiAktionenProjekt.ProjektSuchen());
            register.Aufnehmen(KiAktionenProjekt.ProjektLesen());
            register.Aufnehmen(KiAktionenProjekt.VariantenAuflisten());
            register.Aufnehmen(KiAktionenProjekt.SpeichervariantenAuflisten());

            // ---- Wirtschaftlichkeit und Kosten
            register.Aufnehmen(KiAktionenWirtschaft.ErgebnisseLesen());
            register.Aufnehmen(KiAktionenWirtschaft.ParameterLesen());
            register.Aufnehmen(KiAktionenWirtschaft.KostenlagePruefen());

            // ---- Energietraeger-Einheiten (Konzept Kosten/Energietraeger § 4.4)
            register.Aufnehmen(KiAktionenEnergie.EnergietraegerPruefen());

            // ---- Trockenlaeufe der Uebernahme
            register.Aufnehmen(KiAktionenUebernahme.UebernahmeVorschau());
            register.Aufnehmen(KiAktionenUebernahme.MerkmalVorschau());

            // ---- Lastgang und Peak-Shaving
            register.Aufnehmen(KiAktionenLastgang.LastgangPruefen());
            register.Aufnehmen(KiAktionenLastgang.GanglinienAuflisten());
            register.Aufnehmen(KiAktionenLastgang.MinimaleSpitzeErmitteln());

            // ---- Sitzungsgedaechtnis
            register.Aufnehmen(KiAktionenSitzung.LetzteAktionen());

            // ---- Schreibaktionen der Stufe 2 (Etappe 3, Fachkonzept 5.2). Sie laufen
            //      NUR nach ausdruecklicher Bestaetigung; den Riegel dafuer haelt
            //      KiAusfuehrer, nicht diese Liste.
            register.Aufnehmen(KiAktionenSchreiben.VarianteAnlegen());
            register.Aufnehmen(KiAktionenSchreiben.SpeichervarianteAktivSetzen());
            register.Aufnehmen(KiAktionenSchreiben.KostenpositionSetzen());

            // ---- Formularsteuerung (Etappe 3b, Fachkonzept 11.4). Die beiden lesenden
            //      Aktionen gehoeren zu Stufe 1; die drei uebrigen sind Schreibaktionen
            //      mit dem Kennzeichen „Formularaktion" und laufen deshalb - wie jede
            //      Schreibaktion - nur nach ausdruecklicher Bestaetigung. Sie wirken in
            //      eine offene Maske und nicht in die Datenbank; DB-wirksam wird der
            //      Vorgang erst durch den Aktionsknopf der Maske.
            register.Aufnehmen(KiAktionenDialog.DialogLesen());
            register.Aufnehmen(KiAktionenDialog.DialogParameterErklaeren());
            register.Aufnehmen(KiAktionenDialog.FeldSetzen());
            register.Aufnehmen(KiAktionenDialog.FormularAusfuellen());
            register.Aufnehmen(KiAktionenDialog.DialogAktionAusfuehren());

            return register;
        }
    }

    /// <summary>
    /// Gemeinsame Bausteine der Registerbefuellung: Zeilenbau, Namensaufloesung,
    /// wiederkehrende Vorbedingungen.
    /// </summary>
    internal static class KiHilfe
    {
        /// <summary>
        /// Standardparameter „Projekt" - gefragt wird nach dem Namen, nicht nach
        /// der Datensatznummer. Der Anwender kennt Projektnamen; IDs stehen in
        /// keiner Maske, die er zu sehen bekommt.
        /// </summary>
        internal static KiParameter ProjektParameter(string erlaeuterung = null, bool pflicht = true,
                                                     string name = "projekt", string anzeigename = null)
        {
            return new KiParameter(
                name, KiParameterTyp.Text,
                erlaeuterung ?? KiAktionsTexte.ProjektIdErlaeuterung,
                pflicht: pflicht,
                anzeigename: anzeigename ?? KiAktionsTexte.ProjektIdName,
                maxLaenge: 200);
        }

        /// <summary>Ein Eintrag, aus dem sich der Anwender per Klartextnamen bedient.</summary>
        internal sealed class Kandidat
        {
            internal readonly int Id;
            internal readonly string Name;
            internal readonly string Zusatz;

            internal Kandidat(int id, string name, string zusatz)
            {
                Id = id;
                Name = name ?? "";
                Zusatz = zusatz ?? "";
            }
        }

        /// <summary>Ergebnis einer Namensaufloesung: Treffer oder Klartextgrund.</summary>
        internal struct Auswahl
        {
            internal int Id;
            internal string Name;
            internal string Fehler;

            internal bool Ok { get { return Fehler == null; } }
        }

        /// <summary>
        /// Waehlt anhand des Namens. Zuerst zaehlt die genaue Uebereinstimmung,
        /// danach die reine Datensatznummer, zuletzt ein eindeutiger Teiltreffer.
        /// Bleibt es mehrdeutig, nennt die Meldung die Kandidaten - es wird
        /// ausdruecklich nicht nach einer Nummer gefragt, sonst waere fuer den
        /// Anwender nichts gewonnen.
        /// </summary>
        /// <remarks>
        /// <b>Warum die Nummer ueberhaupt gilt.</b> In den Rueckmeldungen an das Modell
        /// werden Bezeichner platzgehalten, IDs aber NICHT (Fachkonzept 4.2). Die Zahl aus
        /// einer Ergebniszeile ist damit der einzige Bezug, den das Modell woertlich
        /// zurueckgeben kann. Sie zaehlt erst NACH der genauen Namensuebereinstimmung -
        /// heisst ein Projekt tatsaechlich „12", gewinnt sein Name.
        /// </remarks>
        internal static Auswahl Waehle(string eingabe, List<Kandidat> kandidaten, string was)
        {
            Auswahl ergebnis = new Auswahl();
            string gesucht = (eingabe ?? "").Trim();

            if (gesucht.Length == 0)
            {
                ergebnis.Fehler = string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.NameFehlt, was);
                return ergebnis;
            }
            if (kandidaten == null || kandidaten.Count == 0)
            {
                ergebnis.Fehler = string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.NameKeine, was);
                return ergebnis;
            }

            List<Kandidat> genau = kandidaten.FindAll(
                k => string.Equals(k.Name.Trim(), gesucht, StringComparison.CurrentCultureIgnoreCase));

            int nummer;
            if (genau.Count == 0
                && int.TryParse(gesucht, NumberStyles.Integer, CultureInfo.InvariantCulture, out nummer))
            {
                Kandidat ueberId = kandidaten.Find(k => k.Id == nummer);
                if (ueberId != null)
                {
                    ergebnis.Id = ueberId.Id;
                    ergebnis.Name = ueberId.Name;
                    return ergebnis;
                }
            }

            List<Kandidat> treffer = genau.Count > 0
                ? genau
                : kandidaten.FindAll(
                      k => k.Name.IndexOf(gesucht, StringComparison.CurrentCultureIgnoreCase) >= 0);

            if (treffer.Count == 1)
            {
                ergebnis.Id = treffer[0].Id;
                ergebnis.Name = treffer[0].Name;
                return ergebnis;
            }

            ergebnis.Fehler = treffer.Count > 1
                ? string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.NameMehrdeutig,
                                gesucht, Aufzaehlen(treffer))
                : string.Format(CultureInfo.CurrentCulture, KiAktionsTexte.NameUnbekannt,
                                was, gesucht, Aufzaehlen(kandidaten));
            return ergebnis;
        }

        /// <summary>Kandidaten lesbar aufzaehlen; lange Listen werden gekuerzt.</summary>
        internal static string Aufzaehlen(List<Kandidat> kandidaten)
        {
            const int HOECHSTENS = 12;

            var teile = new List<string>();
            for (int i = 0; i < kandidaten.Count && i < HOECHSTENS; i++)
            {
                Kandidat k = kandidaten[i];
                teile.Add(k.Zusatz.Length > 0 ? k.Name + " (" + k.Zusatz + ")" : k.Name);
            }

            string text = string.Join(", ", teile);
            if (kandidaten.Count > HOECHSTENS)
                text += ", ... (" + (kandidaten.Count - HOECHSTENS) + ")";
            return text;
        }

        /// <summary>
        /// Alle Projekte als Auswahlgrundlage - dieselbe Quelle, aus der auch
        /// projekte_auflisten schoepft. Damit kann die Auswahl nie etwas anderes
        /// anbieten als die Liste zeigt.
        /// </summary>
        internal static List<Kandidat> ProjektKandidaten()
        {
            var liste = new List<Kandidat>();
            try
            {
                var ctrl = new ProjektCtrl();
                ctrl.ReadAll();
                foreach (ProjektModel p in ctrl.items)
                    liste.Add(new Kandidat(p.m_ID, p.m_szProjektname, p.m_szKunde));
            }
            catch { }
            return liste;
        }

        /// <summary>Projekt aus dem genannten Parameter aufloesen.</summary>
        internal static Auswahl ProjektWaehlen(KiAufruf a, string parameter = "projekt")
        {
            return Waehle(a.Text(parameter), ProjektKandidaten(), KiAktionsTexte.ProjektIdName);
        }

        /// <summary>Projekt-ID zum Namen; 0, wenn er sich nicht aufloesen laesst.</summary>
        internal static int ProjektId(KiAufruf a, string parameter = "projekt")
        {
            return ProjektWaehlen(a, parameter).Id;
        }

        /// <summary>
        /// Projekt-ID, wobei „nichts angegeben" ausdruecklich erlaubt ist und 0
        /// ergibt - etwa bei den Ganglinien, wo ohne Projekt der Stammkatalog gilt.
        /// </summary>
        internal static int ProjektIdOptional(KiAufruf a, string parameter = "projekt")
        {
            return (a.Text(parameter) ?? "").Trim().Length == 0 ? 0 : ProjektWaehlen(a, parameter).Id;
        }

        /// <summary>Vorbedingung „der Projektname ist eindeutig aufloesbar".</summary>
        internal static string ProjektMussAufloesbarSein(KiAufruf a, string parameter = "projekt")
        {
            return ProjektWaehlen(a, parameter).Fehler;
        }


        /// <summary>
        /// Mehrere Projekte aus einer Aufzaehlung von Namen (Semikolon getrennt).
        /// Unbekannte Namen werden gemeldet und nicht stillschweigend uebergangen -
        /// sonst waertet die Aktion weniger Projekte aus, als der Anwender wollte.
        /// </summary>
        internal static List<int> ProjektIds(KiAufruf a, string parameter, out string fehler)
        {
            fehler = null;
            List<Kandidat> kandidaten = ProjektKandidaten();
            var ids = new List<int>();
            var offen = new List<string>();

            foreach (string teil in (a.Text(parameter) ?? "").Split(';'))
            {
                string name = teil.Trim();
                if (name.Length == 0) continue;

                Auswahl w = Waehle(name, kandidaten, KiAktionsTexte.ProjektIdName);
                if (w.Ok)
                {
                    if (!ids.Contains(w.Id)) ids.Add(w.Id);
                }
                else
                {
                    offen.Add(w.Fehler);
                }
            }

            if (ids.Count == 0 && offen.Count == 0)
                fehler = string.Format(CultureInfo.CurrentCulture,
                                       KiAktionsTexte.NameFehlt, KiAktionsTexte.ProjekteName);
            else if (offen.Count > 0)
                fehler = string.Join(" ", offen);

            return ids;
        }

        /// <summary>Baut eine Ergebniszeile aus Name/Wert-Paaren.</summary>
        internal static IReadOnlyDictionary<string, object> Zeile(params object[] paare)
        {
            var zeile = new Dictionary<string, object>(StringComparer.Ordinal);
            for (int i = 0; i + 1 < paare.Length; i += 2)
                zeile[(string)paare[i]] = paare[i + 1];
            return zeile;
        }

        /// <summary>Leere, typrichtige Zeilenliste.</summary>
        internal static List<IReadOnlyDictionary<string, object>> Liste()
        {
            return new List<IReadOnlyDictionary<string, object>>();
        }

        /// <summary>Projektname zu einer ID; leer, wenn es die ID nicht gibt.</summary>
        internal static string ProjektName(int idProjekt)
        {
            if (idProjekt <= 0) return "";
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT Projektname FROM Tab_Projekt WHERE ID = ?",
                    new OleDbParameter("@id", (Int32)idProjekt));
                return o == null || o == DBNull.Value ? "" : o.ToString();
            }
            catch { return ""; }
        }

        /// <summary>Zahl fuer die Ergebniszeile; <c>null</c> bleibt <c>null</c>.</summary>
        internal static object Wert(double? zahl)
        {
            return zahl.HasValue ? (object)Math.Round(zahl.Value, 4) : null;
        }

        /// <summary>Zahl fuer die Ergebniszeile.</summary>
        internal static object Wert(double zahl)
        {
            return Math.Round(zahl, 4);
        }

        /// <summary>Text fuer die Ergebniszeile; <c>null</c> wird zu leer.</summary>
        internal static object Text(string text)
        {
            return text ?? "";
        }

        /// <summary>„n von m" - die Kurzfassung, die in jeder Ergebnismeldung auftaucht.</summary>
        internal static string Anzahltext(int anzahl, string einzahl, string mehrzahl)
        {
            return anzahl + " " + (anzahl == 1 ? einzahl : mehrzahl);
        }

        /// <summary>Datum fuer die Ergebniszeile - invariant, damit es maschinenlesbar bleibt.</summary>
        internal static object Datum(DateTime wert)
        {
            return wert == default(DateTime) ? "" : wert.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }
}
