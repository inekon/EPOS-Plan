using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Erzeuger;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der drei BEDARFSPROFIL-Masken (iU9-W9.5) — sie löst
    /// <c>Form_Prozesswaerme</c>, <c>Form_Stromverbraucher</c> und
    /// <c>Form_Brauchwasser</c> ab.
    ///
    /// <para><b>Drillinge, eine Hülle.</b> Die drei Masken haben denselben Aufbau und
    /// unterscheiden sich in Titel, Beschriftungen, Zieltabelle, Rechenweg und einer
    /// Handvoll Meldungen. Alles davon hängt an <see cref="BedarfsArt"/>.</para>
    ///
    /// <para><b>Die Hülle RECHNET, die Komponente zeigt.</b> Der Vorläufer hielt ein
    /// lebendes <c>SimulationWaermebedarf</c> bzw. <c>SimulationStrombedarf</c> und gab es
    /// dem Ergebnisdialog in die Hand. Hier bleibt es hier: Die Hülle rechnet, baut den
    /// Parametersatz des Ergebnisdialogs und reicht nur den herein (Risiko R‑W9‑4, wie
    /// W8.2).</para>
    ///
    /// <para><b>Vier Überlagerungen statt vier Fenstern</b> (Risiko R2): Ergebnis (W8.2),
    /// Stammkopf (W8.1) in beiden Modi und Wochen-Stundenprofil (W8.3).</para>
    ///
    /// <para><b>Das Zapfprofil</b> (Umsetzungskonzept Zapfprofilgenerator 5.2, 5.5): Bei
    /// Brauchwasser hängt die Hülle den Einstieg der plattformfreien
    /// <see cref="ZapfprofilHuelle"/> ein — Delegaten für das Zapfprofil-Blatt, die
    /// Optionsgruppe „Rechenweg Brauchwasser" über einen <see cref="ZapfprofilBehaelter"/> je
    /// Öffnen, der Knopf „Simulation" mit dessen Arbeitsstand. Geschrieben wird
    /// im OK des Dialogs, bevor er schließt, im selben Vorgang wie die Zuordnungen
    /// (<see cref="ZapfprofilHuelle.Schreibweg"/>); eine Ablehnung hält ihn offen.</para>
    /// </summary>
    internal static class BedarfsProfileHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 1004 × 636 bzw. 964 × 574).</summary>
        private static readonly Size MASS = new Size(1000, 720);

        /// <summary>
        /// Das Wunschmaß des Zapfprofil-BLATTES (N35, Nachtrag zu #572): dasselbe Maß, auf das
        /// die Überlagerung des Gebäudekatalogs sich über
        /// <c>.epos-ueberlagerung:has(.epos-blatt--breit)</c> weitet (<c>epos-ueberlagerung--breit</c>,
        /// 1400 px) — im eigenen Fenster gibt es diese Überlagerung nicht, die Hülle wünscht das
        /// Maß deshalb selbst, sobald sie den Zapfprofil-Weg reicht
        /// (<see cref="EPOS.UI.Dienste.Fenstermass.MitUeberlagerung"/>); sonst stünde das
        /// zweispaltige Blatt im 1000-px-Fenster ebenso beschnitten wie zuvor die Überlagerung
        /// (Befund vom 26.09.2026).
        /// </summary>
        private static readonly Size ZAPFPROFIL_MASS = new Size(1400, 720);

        /// <summary>Die vorläufige Id einer noch nicht gespeicherten Zuordnung.</summary>
        private const int STARTINDEX = 100000;

        // =================================================================================
        // Einstiege
        // =================================================================================

        /// <summary>Die PROZESSWÄRME eines Projekts.</summary>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId, string projektName,
                                     List<Z_ProjektProzesswaermeModel> modelle)
        {
            List<BedarfsProfilZeile> zeilen = ProzessZeilen(modelle);
            Action geaendert = ProzessRueckweg(projektId, zeilen, modelle);

            return Zeigen(besitzer, BedarfsArt.Prozesswaerme, projektId, zeilen, geaendert,
                          wizard: false);
        }

        /// <summary>Die STROMVERBRAUCHER eines Projekts.</summary>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId, string projektName,
                                     List<Z_ProjektStromverbraucherModel> modelle)
        {
            var zeilen = new List<BedarfsProfilZeile>();
            foreach (Z_ProjektStromverbraucherModel m in modelle)
                zeilen.Add(new BedarfsProfilZeile
                {
                    IdZ = m.m_ID_Z, IdStamm = m.m_ID_Stromverbraucher,
                    Name = m.m_szVerbraucher ?? "", Summe = m.m_Summe,
                    KalenderId = m.ID_Betriebskalender
                });

            Action geaendert = () =>
            {
                modelle.Clear();
                foreach (BedarfsProfilZeile z in zeilen)
                    modelle.Add(new Z_ProjektStromverbraucherModel
                    {
                        m_ID_Z = z.IdZ, m_ID_Projekt = projektId, m_ID_Stromverbraucher = z.IdStamm,
                        m_szVerbraucher = z.Name, m_Summe = z.Summe,
                        ID_Betriebskalender = z.KalenderId
                    });
            };

            return Zeigen(besitzer, BedarfsArt.Stromverbraucher, projektId, zeilen, geaendert,
                          wizard: false);
        }

        /// <summary>
        /// Die BRAUCHWASSERPROFILE eines Projekts. Der <paramref name="behaelter"/> nimmt den
        /// Arbeitsstand des Zapfprofils auf (5.2); das OK des Dialogs schreibt ihn im selben
        /// Vorgang wie die Zuordnungen, bevor der Dialog schließt (<c>Speichern</c>) — der
        /// Aufrufer schreibt nichts mehr. <c>null</c> = ohne Zapfprofil.
        /// </summary>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId, string projektName,
                                     List<Z_ProjektBrauchwasserModel> modelle,
                                     ZapfprofilBehaelter behaelter = null)
        {
            var zeilen = new List<BedarfsProfilZeile>();
            foreach (Z_ProjektBrauchwasserModel m in modelle)
                zeilen.Add(new BedarfsProfilZeile
                {
                    IdZ = m.ID_Z, IdStamm = m.ID_Brauchwasser,
                    Name = m.szBezeichner ?? "", Summe = m.Summe,
                    KalenderId = m.ID_Betriebskalender
                });

            Action geaendert = () =>
            {
                modelle.Clear();
                foreach (BedarfsProfilZeile z in zeilen)
                    modelle.Add(new Z_ProjektBrauchwasserModel
                    {
                        ID_Z = z.IdZ, ID_Projekt = projektId, ID_Brauchwasser = z.IdStamm,
                        szBezeichner = z.Name, Summe = z.Summe,
                        ID_Betriebskalender = z.KalenderId
                    });
            };

            return Zeigen(besitzer, BedarfsArt.Brauchwasser, projektId, zeilen, geaendert,
                          wizard: false, behaelter);
        }

        /// <summary>
        /// Der PARAMETERSATZ der PROZESSWÄRME-Seite des Assistenten (Seite 4).
        ///
        /// <para>iU9-W16a.5: Die Fabrikmethode <c>AssistentSeiteProzess()</c> ist
        /// entfallen — der Assistent ist selbst eine Razor-Seite und braucht kein
        /// randloses WinForms-Formular mehr.</para>
        /// </summary>
        internal static IReadOnlyDictionary<string, object> AssistentGabenProzess(
            int projektId, List<Z_ProjektProzesswaermeModel> modelle)
        {
            List<BedarfsProfilZeile> zeilen = ProzessZeilen(modelle);
            Action geaendert = ProzessRueckweg(projektId, zeilen, modelle);

            return Gaben(null, BedarfsArt.Prozesswaerme, projektId, zeilen, geaendert,
                         wizard: true);
        }

        /// <summary>
        /// Die Zeilen der Prozesswärme samt Temperaturpaar der Projektkopie (PW1 Stufe 1) — EINE
        /// Stelle für Verwaltung und Assistent.
        /// </summary>
        private static List<BedarfsProfilZeile> ProzessZeilen(List<Z_ProjektProzesswaermeModel> modelle)
        {
            var zeilen = new List<BedarfsProfilZeile>();
            foreach (Z_ProjektProzesswaermeModel m in modelle)
                zeilen.Add(new BedarfsProfilZeile
                {
                    IdZ = m.ID_Z, IdStamm = m.ID_Prozesswaerme,
                    Name = m.szProzessname ?? "", Summe = m.Summe,
                    Vorlauf = m.Vorlauf, Ruecklauf = m.Ruecklauf,
                    TemperaturGeaendert = m.TemperaturGeaendert,
                    KalenderId = m.ID_Betriebskalender
                });
            return zeilen;
        }

        /// <summary>
        /// Der Rückweg der Prozesswärme in die Modelle des Aufrufers — mit Temperaturpaar und
        /// Änderungskennzeichen; geschrieben wird beim Speichern des Projekts
        /// (<c>WizardCtrl.Add_Projekt_Prozess</c>).
        /// </summary>
        private static Action ProzessRueckweg(int projektId, List<BedarfsProfilZeile> zeilen,
                                              List<Z_ProjektProzesswaermeModel> modelle)
        {
            return () =>
            {
                modelle.Clear();
                foreach (BedarfsProfilZeile z in zeilen)
                    modelle.Add(new Z_ProjektProzesswaermeModel
                    {
                        ID_Z = z.IdZ, ID_Projekt = projektId,
                        ID_Prozesswaerme = z.IdStamm,
                        szProzessname = z.Name, Summe = z.Summe,
                        Vorlauf = z.Vorlauf, Ruecklauf = z.Ruecklauf,
                        TemperaturGeaendert = z.TemperaturGeaendert,
                        ID_Betriebskalender = z.KalenderId
                    });
            };
        }

        /// <summary>Der PARAMETERSATZ der STROMVERBRAUCHER-Seite des Assistenten (Seite 5).</summary>
        internal static IReadOnlyDictionary<string, object> AssistentGabenStrom(
            int projektId, List<Z_ProjektStromverbraucherModel> modelle)
        {
            var zeilen = new List<BedarfsProfilZeile>();
            foreach (Z_ProjektStromverbraucherModel m in modelle)
                zeilen.Add(new BedarfsProfilZeile
                {
                    IdZ = m.m_ID_Z, IdStamm = m.m_ID_Stromverbraucher,
                    Name = m.m_szVerbraucher ?? "", Summe = m.m_Summe,
                    KalenderId = m.ID_Betriebskalender
                });

            Action geaendert = () =>
            {
                modelle.Clear();
                foreach (BedarfsProfilZeile z in zeilen)
                    modelle.Add(new Z_ProjektStromverbraucherModel
                    {
                        m_ID_Z = z.IdZ, m_ID_Projekt = projektId,
                        m_ID_Stromverbraucher = z.IdStamm,
                        m_szVerbraucher = z.Name, m_Summe = z.Summe,
                        ID_Betriebskalender = z.KalenderId
                    });
            };

            return Gaben(null, BedarfsArt.Stromverbraucher, projektId, zeilen, geaendert,
                         wizard: true);
        }

        // =================================================================================

        private static bool Zeigen(IWin32Window besitzer, BedarfsArt art, int projektId,
                                   List<BedarfsProfilZeile> zeilen, Action geaendert, bool wizard,
                                   ZapfprofilBehaelter behaelter = null)
        {
            bool ok = false;
            BlazorDialogForm<BedarfsProfileDialog> dlg = null;

            var werte = new Dictionary<string, object>(
                Gaben(besitzer, art, projektId, zeilen, geaendert, wizard, behaelter))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            // N35: mit Zapfprofil-Weg (Brauchwasser, behaelter gesetzt) wuenscht das Fenster
            // mindestens das breite Mass des Blattes - sonst wuerde das Blatt im 1000-px-Fenster
            // ebenso beschnitten wie bis #572 die Ueberlagerung.
            (int breite, int hoehe) = behaelter != null
                ? EPOS.UI.Dienste.Fenstermass.MitUeberlagerung(
                    MASS.Width, MASS.Height, ZAPFPROFIL_MASS.Width, ZAPFPROFIL_MASS.Height)
                : (MASS.Width, MASS.Height);

            dlg = new BlazorDialogForm<BedarfsProfileDialog>(Titel(art), new Size(breite, hoehe), werte);
            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        // =================================================================================
        // Der Parametersatz
        // =================================================================================

        /// <summary>
        /// Der Parametersatz. Bei Brauchwasser mit <paramref name="behaelter"/> kommt der
        /// Zapfprofil-Einstieg dazu (5.2): Knopf, Optionsgruppe, Arbeitsstand der Leiste.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            IWin32Window besitzer, BedarfsArt art, int projektId,
            List<BedarfsProfilZeile> zeilen, Action geaendert, bool wizard,
            ZapfprofilBehaelter behaelter = null)
        {
            int[] naechsteId = { STARTINDEX };
            bool zapfprofil = art == BedarfsArt.Brauchwasser && behaelter != null;

            // Das Rechenobjekt gehoert der HUELLE - genau wie im Vorlaeufer, wo es ein Feld
            // der Maske war. Es rechnet mit den ZEILEN des Dialogs (Jahresverbrauch samt
            // "Uebernehmen", auch ungespeichert), beim Brauchwasser zusaetzlich mit dem
            // Arbeitsstand des Zapfprofils (5.2).
            var rechenstand = new Rechenstand(art, projektId, zeilen, zapfprofil ? behaelter : null);

            var gaben = new Dictionary<string, object>
            {
                ["Art"] = art,
                ["Zeilen"] = zeilen,
                ["Wizard"] = wizard,
                ["Geaendert"] = geaendert,

                // W14a-E-10 / S3.1: die Katalogliste des Hauses statt der zwei- bis
                // dreispaltigen Tabelle. Sie kommt aus EINER Abfrage und traegt fuenf
                // Spalten plus "im Projekt verwendet" (Q12).
                // SCHLOSS SETZEN / AUFHEBEN an der Katalogliste (AD-Q15) - derselbe Weg wie in
                // der Verwaltung. Die Verwendung im Projekt sperrt nichts (eigene Kopie).
                ["Schloss"] = Schlosswege.Aus((ids, gesperrt) => BedarfStammCtrl.SchlossSetzen(art, ids, gesperrt)),
                ["Katalogzeilen"] = new Func<IReadOnlyList<Katalogfilterzeile>>(
                    () => BedarfStammCtrl.Katalogfilterzeilen(art)),
                ["Katalogprofil"] = Katalogfilterprofil.FuerBedarf(art, BedarfAdminHuelle.Filtertext)
                                                       .MitVerwendungsspalte(BedarfAdminHuelle.Filtertext),
                // W14a-E-10 / S3.3: die Zeilen des Vergleichs kommen aus DERSELBEN
                // Quelle wie die Parameteruebersicht (W14a-E-8) - keine zweite Liste.
                ["Vergleichsparameter"] = new Func<string, IReadOnlyList<Parameterwert>>(
                    n => BedarfStammCtrl.Vergleichszeilen(art, n, BedarfAdminHuelle.Filtertext)),
                ["Info"] = new Func<string, BedarfsProfilInfo>(name => Info(art, name)),
                ["Jahressumme"] = new Func<string, double>(
                    name => BedarfStammCtrl.Jahressumme(art, name)),
                ["Aufnehmen"] = new Func<string, BedarfsProfilZeile>(
                    name => Aufnehmen(art, name, naechsteId)),
                ["KatalogLoeschen"] = new Func<string, bool>(name => KatalogLoeschen(art, name)),
                ["ProjektGespeichert"] = new Func<bool>(() => ProjektCtrl.Existiert(projektId)),
                ["SummeSichern"] = new Action<string, double>(
                    (name, wert) => SummeSichern(art, projektId, name, wert)),

                ["Simulieren"] = new Func<IReadOnlyList<string>, IReadOnlyDictionary<string, object>>(
                    namen => rechenstand.Rechnen(namen)),

                ["TypStammGaben"] =
                    new Func<string, string, string, bool, IReadOnlyDictionary<string, object>>(
                        (name, beschr, typ, istNeu) => TypStammHuelle.Gaben(
                            art, name, istNeu ? "" : beschr, istNeu ? "" : typ,
                            istNeu ? KatalogModus.Neu : KatalogModus.Bearbeiten)),
                ["TypProfilGaben"] = new Func<IReadOnlyDictionary<string, object>>(
                    () => TypStammHuelle.ProfilGaben(art)),

                ["TitelText"] = Titel(art),
                ["KopfbandText"] = Titel(art),
                ["LabelProjektliste"] = Text_(art, "BPF_LBL_PROJEKTLISTE",
                    "Ausgewählte Prozesse im Projekt",
                    "Ausgewählte Strombedarfe im Projekt",
                    "Ausgewählte Profile im Projekt"),
                ["LabelKatalog"] = Text_(art, "BPF_LBL_KATALOG",
                    "Datenbank Prozesswärme", "Datenbank Strombedarf", "Datenbank Profile"),
                ["GruppeInfo"] = TextEinfach("BPF_GRP_INFO", "Profil"),
                ["GruppeVerbrauch"] =
                    TextEinfach("BPF_GRP_VERBRAUCH", "Ändern des Jahresverbrauchs"),
                ["LabelName"] = TextEinfach("BTYP_LBL_NAME", "Name:"),
                ["LabelTyp"] = TextEinfach("BPF_LBL_TYP", "Typ:"),
                ["LabelBeschreibung"] = TextEinfach("BTYP_LBL_BESCHREIBUNG", "Beschreibung:"),
                ["LabelJahresverbrauch"] = Text_(art, "BPF_LBL_JAHRESVERBRAUCH",
                    "jährlicher Prozesswärmebedarf:", "jährlicher Strombedarf:",
                    "jährlicher Wärmebedarf:"),
                ["LabelSumme"] = Text_(art, "BPF_LBL_SUMME",
                    "Summe aller ausgew. Prozesse:",
                    "Summe aller ausgewählten Strombedarfe:",
                    "Summe Brauchwasserprofile:"),
                ["LabelNeuerWert"] = TextEinfach("BPF_LBL_NEUER_WERT", "neuer Wert"),
                ["LabelEinheit"] = TextEinfach("ALLG_LBL_EINHEIT", "Einheit:"),
                // Die Anzeigeeinheit (Entscheid W9-O-3 vom 04.09.2026): MWh als Vorgabe,
                // kWh waehlbar. Sie kommt aus derselben gemerkten Wahl wie die des
                // Ergebnisdialogs - sonst stuende hier MWh und in der Ueberlagerung kWh.
                // Die Summen der Projektzeilen und die Jahressummen des Katalogs liegen
                // in MWh; die Komponente rechnet nur fuer Anzeige und Eingabe um.
                ["Einheit"] = BedarfEinheitWahl.Lies(),
                ["EinheitGewaehlt"] = new Action<Energieeinheit>(BedarfEinheitWahl.Schreib),
                ["SpalteWahl"] = TextEinfach("KFAK_SP_WAHL", "Wahl"),
                ["SpalteName"] = TextEinfach("BHKWV_SP_NAME", "Name"),
                ["SpalteTyp"] = TextEinfach("BPF_SP_TYP", "Typ"),

                // Entscheid #76 (05.09.2026): Das Zeichen setzt der Baustein
                // Zweispaltenauswahl je nach Anordnung - der Knopf traegt Klartext.
                ["BtnHinzuText"] = TextEinfach("AUSWAHL_BTN_UEBERNEHMEN", "In das Projekt übernehmen"),
                ["BtnEntfernenText"] = TextEinfach("AUSWAHL_BTN_ENTFERNEN", "Aus dem Projekt entfernen"),
                ["BtnDbAendernText"] = Text_(art, "BPF_BTN_DB_AENDERN",
                    "Prozess in DB ändern", "Stromverbraucher ändern...", "Profil in DB ändern"),
                ["BtnDbNeuText"] = Text_(art, "BPF_BTN_DB_NEU",
                    "Prozess in DB neu", "Stromverbraucher neu...", "Profil in DB neu"),
                ["BtnDbLoeschenText"] = Text_(art, "BPF_BTN_DB_LOESCHEN",
                    "Prozess in DB löschen", "Stromverbraucher löschen", "Profil in DB löschen"),
                ["BtnTypAendernText"] = Text_(art, "BPF_BTN_TYP_AENDERN",
                    "Typ in DB ändern", "Typ in DB ändern...", "Typ in DB ändern"),
                ["BtnSimulationText"] = Text_(art, "BPF_BTN_SIMULATION",
                    "Simulation", "Simulation...", "Simulation"),
                ["BtnUebernehmenText"] = TextEinfach("BPF_BTN_UEBERNEHMEN", "Übernehmen"),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["JaText"] = MyResource.Resource.ALLG_BTN_JA,
                ["NeinText"] = MyResource.Resource.ALLG_BTN_NEIN,

                ["MeldungKeineAuswahl"] = TextEinfach("BPF_MSG_KEINE_AUSWAHL",
                    "Bitte einen Eintrag aus der Liste auswählen!"),
                ["MeldungKeineZeile"] = TextEinfach("BPF_MSG_KEINE_ZEILE",
                    "Bitte einen Eintrag aus der Liste auswählen und einen Wert eingeben!"),
                // W9-B7 ERLEDIGT (Entscheid des Anwenders vom 04.09.2026): Der Bestand
                // nannte beim Stromverbraucher kWh, bei Prozess und Brauchwasser MWh -
                // fuer DIESELBE Groesse. Es gibt jetzt EINEN Text mit einem Platzhalter
                // fuer die Einheit; welche darin steht, entscheidet die Wahl im Dialog.
                ["MeldungWertUngueltig"] = TextEinfach("BPF_MSG_WERT",
                    "Bitte den Jahresverbrauch als Zahl in {0} eingeben, z. B. 12,5."),
                ["MeldungUebernommen"] =
                    TextEinfach("BPF_MSG_UEBERNOMMEN", "Jahresverbrauch übernommen."),
                ["MeldungLoeschfrage"] =
                    TextEinfach("BPRO_FRAGE_LOESCHEN", "Soll {0} wirklich gelöscht werden ?"),
                // Nur die Prozessmaske meldete den Erfolg (btn_Prozess_loeschen_Click:491).
                ["MeldungGeloescht"] = art == BedarfsArt.Prozesswaerme
                    ? TextEinfach("BPF_MSG_GELOESCHT", "Prozess erfolgreich gelöscht.") : "",
                ["MeldungNameFehlt"] =
                    TextEinfach("BTYP_MSG_NAME_LEER", "Bitte einen Namen eingeben!"),

                ["HilfeSchluessel"] = HilfeSchluessel(art),

                // H13 (06.09.2026): der zweite Knopf im Kopf - der RECHENWEG der
                // Auspraegung. Er zeigt auf die Rubrik "Programm Dokumentation/
                // Berechnung"; der Fensterknopf daneben bleibt die Bedienhilfe.
                ["HilfeSchluesselBerechnung"] = BerechnungsSchluessel(art),
                ["HilfeKurztextBerechnung"] = BerechnungsKurztext(art)
            };

            // PW2/BW2: die Betriebskalender zur Wahl je Zuordnung - nur, wenn es die Tabelle gibt.
            if (BetriebskalenderCtrl.TabelleVorhanden())
                BetriebskalenderHuelle.WahlEinhaengen(gaben);

            // PW1 Stufe 1: das Temperaturpaar je Prozess - die Pruefung steht im Kern
            // (Prozesstemperatur.Paarpruefung), dieselbe wie die Pruefklauseln des Schemas.
            if (art == BedarfsArt.Prozesswaerme)
                gaben["TemperaturPruefen"] = new Func<double?, double?, string>(Prozesstemperatur.Paarpruefung);

            // Brauchwasser (5.2): Das OK schreibt Zuordnungen und Zapfprofil in EINEM Vorgang,
            // BEVOR der Dialog schliesst - lehnt der Schreibweg ab, bleibt er offen und nennt den
            // Grund. Ohne Behaelter (Verwaltung) schreibt derselbe Weg nur die Zuordnungen.
            if (art == BedarfsArt.Brauchwasser && !wizard)
                gaben["Speichern"] = new Func<string>(
                    () => ZapfprofilHuelle.Schreibweg(projektId, zeilen, behaelter));

            // Zapfprofil (5.2; ZU4, ZU6, ZU10): Knopf nur mit gespeichertem Projekt, sonst
            // benannt gesperrt; die Meldung der Leiste kommt aus dem Rechenstand.
            if (zapfprofil)
            {
                ZapfprofilHuelle.Einhaengen(gaben, projektId, ProjektCtrl.Existiert(projektId), behaelter);
                gaben["SimulationMeldung"] = new Func<string>(() => rechenstand.Meldung);
            }
            return gaben;
        }

        // =================================================================================
        // Der Rechenstand - er gehoert der Huelle
        // =================================================================================

        /// <summary>
        /// Hält das Rechenobjekt des Knopfes „Simulation" — im Vorläufer war es ein Feld
        /// der Maske.
        ///
        /// <para><b>Die RECHNUNG steht seit dem Befund W8‑B‑3 im Kern</b>
        /// (<c>BedarfsVorschauCtrl.ProjektVorschau</c>, Windows-Abnahme 05.09.2026). Hier
        /// stand sie bis dahin ein zweites Mal, von Hand nachgezogen — und beim Strom
        /// fehlte darin die Zeile, die <c>Strombedarf_Gebaeude_gesamt</c> belegt: Die
        /// Ergebnisanzeige zeigte „Gesamter Strombedarf 0" und „Strombedarf Gebäude 0"
        /// neben einem gerechneten Spitzenwert von 3,72 kW. Die Hülle HÄLT den Stand
        /// jetzt nur noch; gerechnet wird einmal, im Kern.</para>
        /// </summary>
        private sealed class Rechenstand
        {
            private readonly BedarfsArt _art;
            private readonly int _projektId;
            private readonly ZapfprofilBehaelter _zapfprofil;
            private readonly IReadOnlyList<BedarfsProfilZeile> _zeilen;
            private BedarfsVorschau _stand;
            private string _titelZusatz = "";

            internal Rechenstand(BedarfsArt art, int projektId, IReadOnlyList<BedarfsProfilZeile> zeilen,
                                 ZapfprofilBehaelter zapfprofil = null)
            {
                _art = art;
                _projektId = projektId;
                _zeilen = zeilen;
                _zapfprofil = zapfprofil;
            }

            /// <summary>
            /// Der Jahresverbrauch je Profilname [MWh], wie ihn der Dialog gerade zeigt — die
            /// Zeilenliste ist dieselbe, die der Dialog bearbeitet. Er geht der gespeicherten
            /// Zuordnung vor; sonst zeigte „Simulation" nach „Übernehmen" noch die alte oder
            /// die Katalogsumme.
            /// </summary>
            private IReadOnlyDictionary<string, double> Jahressummen()
            {
                var summen = new Dictionary<string, double>(StringComparer.Ordinal);
                if (_zeilen == null) return summen;
                foreach (BedarfsProfilZeile z in _zeilen)
                    if (!string.IsNullOrEmpty(z.Name)) summen[z.Name] = z.Summe;
                return summen;
            }

            /// <summary>Die Meldung des letzten Laufs (Zapfprofilweg); leer = keine.</summary>
            internal string Meldung { get; private set; } = "";

            internal IReadOnlyDictionary<string, object> Rechnen(IReadOnlyList<string> namen)
            {
                // Beim Brauchwasser mit dem ARBEITSSTAND des Zapfprofils (5.2): null = der
                // gespeicherte Stand; steht er auf dem Generator, rechnet der Zapfprofilweg.
                BedarfsVorschau v = BedarfsVorschauCtrl.ProjektVorschau(_art, _projektId, namen,
                                                                       _zapfprofil?.Arbeitsstand,
                                                                       Jahressummen());
                Meldung = _zapfprofil != null ? ZapfprofilHuelle.Leistenmeldung(v) : "";
                if (!v.Erfolgreich) return null;

                _stand = v;

                // Nur der Brauchwasserweg haengt den Profilnamen an den Fenstertitel
                // (Form_Brauchwasser.btn_Berechnen_Click:308); auf dem Zapfprofilweg steht
                // dort der Generator (N8 g).
                _titelZusatz = v.Zapfprofilweg
                    ? TextEinfach("BPF_TITEL_ZAPFPROFILGENERATOR", "Zapfprofilgenerator")
                    : (_art == BedarfsArt.Brauchwasser && namen != null && namen.Count > 0) ? namen[0] : "";

                return LetzterStand();
            }

            /// <summary>
            /// Der Parametersatz des Ergebnisdialogs zum letzten Stand. Die Startreiter sind
            /// wörtlich die der Vorläufer: 1 bei Prozess und Strom, 2 (Grafik samt
            /// Brauchwassersicht) beim Brauchwasser.
            /// </summary>
            private IReadOnlyDictionary<string, object> LetzterStand()
            {
                if (_stand == null) return null;

                switch (_art)
                {
                    case BedarfsArt.Stromverbraucher:
                        return BedarfErgebnisHuelle.Gaben(_stand.Strom, 1);
                    case BedarfsArt.Prozesswaerme:
                        return BedarfErgebnisHuelle.Gaben(_stand.Waerme, false, 1, "");
                    default:
                        return BedarfErgebnisHuelle.Gaben(_stand.Waerme, true, 2, _titelZusatz);
                }
            }
        }

        // =================================================================================
        // Die Wege hinter den Delegaten
        // =================================================================================

        private static BedarfsProfilInfo Info(BedarfsArt art, string name)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher:
                    var s = new StromverbraucherStammCtrl();
                    s.ReadSingle(name);
                    return s.rows > 0
                        ? new BedarfsProfilInfo(name, s.m_szBeschreibung ?? "", s.m_szTyp ?? "")
                        : null;

                case BedarfsArt.Prozesswaerme:
                    var p = new ProzesswaermeStammCtrl();
                    p.ReadSingle(name);
                    return p.rows > 0
                        ? new BedarfsProfilInfo(name, p.m_szBeschreibung ?? "", p.m_szTyp ?? "",
                                                p.m_Vorlauf, p.m_Ruecklauf)
                        : null;

                default:
                    var b = new BrauchwasserStammCtrl();
                    b.ReadSingle(name);
                    return b.rows > 0
                        ? new BedarfsProfilInfo(name, b.m_szBeschreibung ?? "", b.m_szTyp ?? "")
                        : null;
            }
        }

        /// <summary>
        /// „◀" — die Stamm-Id per Name, die Summe als Σ der zwölf Monatswerte
        /// (<c>btn_Hinzu_Click</c> der drei Vorläufer).
        /// </summary>
        private static BedarfsProfilZeile Aufnehmen(BedarfsArt art, string name, int[] naechsteId)
        {
            int idStamm = DataRepository.GetIdByName(BedarfStammCtrl.KopfTabelle(art),
                                                     "Bezeichner", name);
            if (idStamm <= 0) return null;

            // PW1 Stufe 1: die Vorbelegung des Katalogs - die Kopie übernimmt sie beim Speichern.
            (double? vorlauf, double? ruecklauf) = BedarfStammCtrl.Temperaturpaar(art, name);
            return new BedarfsProfilZeile
            {
                IdZ = naechsteId[0]++,      // noch nicht gespeichert, also noch unbekannt
                IdStamm = idStamm,
                Name = name ?? "",
                Summe = BedarfStammCtrl.Jahressumme(art, name),
                Vorlauf = vorlauf,
                Ruecklauf = ruecklauf
            };
        }

        private static bool KatalogLoeschen(BedarfsArt art, string name)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return new StromverbraucherStammCtrl().Delete(name);
                case BedarfsArt.Prozesswaerme:    return new ProzesswaermeStammCtrl().Delete(name);
                default:                          return new BrauchwasserStammCtrl().Delete(name);
            }
        }

        private static void SummeSichern(BedarfsArt art, int projektId, string name, double wert)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher:
                    new Z_ProjektStromverbraucherCtrl().UpdateSumme(wert, name, projektId);
                    break;
                case BedarfsArt.Prozesswaerme:
                    new Z_ProjektProzesswaermeCtrl().UpdateSumme(wert, name, projektId);
                    break;
                default:
                    new Z_ProjektBrauchwasserCtrl().UpdateSumme(wert, name, projektId);
                    break;
            }
        }

        // =================================================================================
        // Texte
        // =================================================================================

        internal static string Titel(BedarfsArt art)
        {
            return Text_(art, "BPF_TITEL", "Prozesswärme", "Standard Stromprofil",
                         "Brauchwasserwärme");
        }

        private static string HilfeSchluessel(BedarfsArt art)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return "Form_Stromverbraucher.btn_Help";
                case BedarfsArt.Prozesswaerme:    return "Form_Prozesswaerme.btn_Help";
                default:                          return "Form_Brauchwasser.btn_Help";
            }
        }

        /// <summary>
        /// H13 — der Schlüssel des RECHENWEG-Knopfes je Ausprägung. Dieselben drei
        /// Präfixe wie oben, nur mit dem Nachnamen <c>.Berechnung</c>; die Ziele stehen
        /// in <c>help_mapping.txt</c>, Abschnitt „H13 - Rubrik Berechnung".
        /// </summary>
        private static string BerechnungsSchluessel(BedarfsArt art)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return "Form_Stromverbraucher.Berechnung";
                case BedarfsArt.Prozesswaerme:    return "Form_Prozesswaerme.Berechnung";
                default:                          return "Form_Brauchwasser.Berechnung";
            }
        }

        /// <summary>
        /// Der Kurztext am Rechenweg-Knopf, solange der Hilfekatalog den Schlüssel nicht
        /// kennt (die Wikiseiten sind erst anzulegen). Kennt er ihn, gewinnt der Tooltip
        /// des Katalogs — „Berechnung: Brauchwasser".
        /// </summary>
        private static string BerechnungsKurztext(BedarfsArt art)
        {
            return Text_(art, "BPF_HILFE_BERECHNUNG",
                         "Berechnungsweg: Prozesswärme",
                         "Berechnungsweg: Strombedarf",
                         "Berechnungsweg: Brauchwasser");
        }

        /// <summary>
        /// Ein Text je Ausprägung. Der Ressourcenschlüssel bekommt das Kürzel der
        /// Ausprägung angehängt (<c>_PROZ</c>, <c>_STROM</c>, <c>_BW</c>).
        /// </summary>
        private static string Text_(BedarfsArt art, string schluessel,
                                    string prozess, string strom, string brauchwasser)
        {
            switch (art)
            {
                case BedarfsArt.Stromverbraucher: return TextEinfach(schluessel + "_STROM", strom);
                case BedarfsArt.Prozesswaerme:    return TextEinfach(schluessel + "_PROZ", prozess);
                default:                          return TextEinfach(schluessel + "_BW", brauchwasser);
            }
        }

        private static string TextEinfach(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
