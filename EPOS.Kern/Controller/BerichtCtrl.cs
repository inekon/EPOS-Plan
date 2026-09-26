using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ablauf- und Persistenzsteuerung des Berichtsmoduls (Konzept Kap. 8.4).
    /// Berichtskonfiguration je Stammprojekt in der DB (Tabelle Berichtskonfiguration:
    /// ProjektID, KonfigJson, GeaendertAm) sowie Word- und Excel-Erzeugung
    /// (Dateiname, Zielordner, Kollisionsbehandlung → WordBerichtGenerator /
    /// ExcelBerichtGenerator).
    ///
    /// <para><b>Word aus einer Vorlage</b> (Konzept Berichtsvorlagen 6.8, 10.2, 10.3; Etappe BV-E1):
    /// Die Vorlage des Laufs wählt <see cref="BerichtsvorlagenCtrl.VorlageFuer"/> — Abweichung des
    /// Stammprojekts, Vorgabe der Installation, Standardvorlage, Rückfall —, gelesen wird sie EINMAL,
    /// gefüllt über <see cref="WordBerichtGenerator.ErzeugeMitVorlage"/>. Fehlt die Standardvorlage
    /// selbst, entsteht der Bericht auf dem bisherigen Weg (Stilvorlage, sonst eingebaute Formate),
    /// benannt im <see cref="Berichtslauf"/>. Vor dem Start prüft <see cref="PruefeVorStart"/> die
    /// gelesenen Bytes; der Lauf füllt genau diese (<see cref="ErzeugeWord(BerichtsDaten, BerichtsKonfiguration, Startbefund, Startweg)"/>).
    /// Die Laufmeldung für die Hülle bildet <see cref="Laufmeldung"/>.</para>
    /// </summary>
    public class BerichtCtrl
    {
        /// <summary>
        /// Tabelle der Berichtskonfiguration. Der Name steht seit iU3 (Kante K7) bei
        /// <see cref="SchemaKatalog.TAB_BERICHTSKONFIGURATION"/>; hier bleibt die
        /// Weiterleitung.
        ///
        /// <para><b>Die Tabelle legt dieser Controller nicht an.</b> Sie steht im
        /// Grundschema (<c>sql/schema/001_grundschema.sql</c>), aus dem jede Datenbank
        /// hervorgeht, und bekommt in Schemaschritt 96 ihren Fremdschlüssel auf
        /// <c>Tab_Projekt</c> mit <c>ON DELETE CASCADE</c>
        /// (<see cref="ProjektFremdschluessel"/>). Eine Anlage an dieser Stelle entstünde
        /// ohne Fremdschlüssel und ohne <c>STRICT</c>, und Schritt 96 baute eine solche
        /// Tabelle nicht um, sondern bräche an ihr ab
        /// (<see cref="ProjektFremdschluessel.Zieltext"/>). Fehlt die Tabelle doch, meldet
        /// der Zugriff den Datenbankfehler sichtbar; <see cref="Lade"/> fällt dann auf die
        /// Standardkonfiguration zurück, <see cref="Speichere"/> liefert <c>false</c>.</para>
        /// </summary>
        public const string TAB_KONFIG = SchemaKatalog.TAB_BERICHTSKONFIGURATION;

        /// <summary>Wie viele Punkte ein Abschnitt der Laufmeldung und die Rückfrage höchstens aufzählen.</summary>
        public const int MAX_PUNKTE = 10;

        /// <summary>Wie viele Ausweichnamen (<c>_2</c> … <c>_20</c>) ein Lauf versucht, bevor er aufgibt.</summary>
        private const int MAX_AUSWEICHNAMEN = 20;

        private readonly BerichtsvorlagenCtrl _vorlagen;

        /// <summary>Mit den Diensten der Plattform (Vorlagen über <see cref="Dienste.Pfade"/> und <see cref="Dienste.Einstellungen"/>).</summary>
        public BerichtCtrl() : this(null)
        {
        }

        /// <summary>
        /// Mit einem hereingereichten Vorlagen-Controller (Prüfstand: eigene Pfade und Einstellungen);
        /// <c>null</c> = der Controller mit den Diensten der Plattform.
        /// </summary>
        public BerichtCtrl(BerichtsvorlagenCtrl vorlagen)
        {
            _vorlagen = vorlagen ?? new BerichtsvorlagenCtrl();
        }

        // =====================================================================
        //  Word
        // =====================================================================

        /// <summary>
        /// Erzeugt den Word-Bericht (Konzept Kap. 3.1: Dateiname
        /// &lt;Projektname&gt;_Bericht_&lt;JJJJ-MM-TT&gt;.docx, kein stilles Überschreiben —
        /// bei Kollision/Sperre wird automatisch _2, _3 … angehängt) aus der Vorlage des Laufs.
        /// Rückgabe: Pfad der geschriebenen Datei; die Laufmeldung liefert <see cref="ErzeugeWordLauf"/>.
        /// </summary>
        public string ErzeugeWord(BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            return ErzeugeWordLauf(daten, konfig).Pfad;
        }

        /// <summary>
        /// Erzeugt den Word-Bericht OHNE Vorprüfung: wählt die Vorlage (Abweichung → Vorgabe →
        /// Standard → Rückfall), liest sie einmal und füllt sie. Ließe sie sich nicht lesen oder
        /// füllen, springt die Standardvorlage ein, danach der bisherige Weg — jeweils benannt.
        /// </summary>
        public Berichtslauf ErzeugeWordLauf(BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            Vorlagenwahl wahl = _vorlagen.VorlageFuer(konfig);
            byte[] bytes = null;
            string lesefehler = null;
            if (wahl.Grund != Vorlagenwahlgrund.Rueckfall) bytes = Lies(wahl.Eintrag, out lesefehler);
            bool englisch = Berichtssprache.AusPaket(bytes) ?? BerichtTexte.Englisch;
            return Fuelle(daten, konfig, wahl, bytes, lesefehler, new List<string>(wahl.Meldungen), englisch);
        }

        /// <summary>
        /// Erzeugt den Word-Bericht nach der Vorprüfung aus DENSELBEN Bytes, die
        /// <see cref="PruefeVorStart"/> gelesen und geprüft hat (Konzept 6.8) — auch wenn die Datei
        /// inzwischen in Word gespeichert wurde. <paramref name="weg"/> ist die Antwort auf die
        /// erweiterte Rückfrage: <see cref="Startweg.Gewaehlt"/> füllt die gewählte Vorlage
        /// (unbekannte Stellen bleiben gelb), <see cref="Startweg.Standard"/> nimmt für diesen Lauf
        /// die Standardvorlage und nennt die ersetzte. Ohne Startbefund wie <see cref="ErzeugeWordLauf"/>.
        /// </summary>
        /// <param name="sprache">Die Sprache des Laufs (BV-Q7 b, <see cref="Berichtssprache.Fuer"/>); <c>null</c> = aus
        /// dem Startbefund und dem Weg, ohne Excel-Vorlage.</param>
        public Berichtslauf ErzeugeWord(BerichtsDaten daten, BerichtsKonfiguration konfig, Startbefund start,
                                        Startweg weg = Startweg.Gewaehlt, Berichtssprache sprache = null)
        {
            if (start == null || start.Wahl == null) return ErzeugeWordLauf(daten, konfig);
            bool englisch = (sprache ?? Berichtssprache.Fuer(start, weg, null, true, BerichtTexte.Englisch)).Englisch;

            if (weg == Startweg.Standard && !start.Wahl.Eintrag.IstStandard)
            {
                Vorlageneintrag standard = _vorlagen.Standardeintrag();
                var rueckfaelle = new List<string>(start.Wahl.Meldungen)
                {
                    T(nameof(R.BV_LAUF_ERSETZT), start.Wahl.Eintrag.Name, standard.Name),
                };
                Vorlagenwahl wahl = WahlDerStandardvorlage(standard, rueckfaelle, start.Wahl.FehlendeId);
                byte[] bytes = null;
                string lesefehler = null;
                if (wahl.Grund != Vorlagenwahlgrund.Rueckfall) bytes = Lies(standard, out lesefehler);
                return Fuelle(daten, konfig, wahl, bytes, lesefehler, rueckfaelle, englisch);
            }

            string fehler = start.Lesefehler;
            if (start.Bytes == null && start.Wahl.Grund != Vorlagenwahlgrund.Rueckfall && fehler == null)
                fehler = T(nameof(R.BV_VORLAGEN_FEHLT), start.Wahl.Eintrag.Name);
            return Fuelle(daten, konfig, start.Wahl, start.Bytes, fehler, new List<string>(start.Wahl.Meldungen), englisch);
        }

        /// <summary>
        /// Der Kern beider Wege: erst die gewählte Vorlage, dann — wenn sie sich nicht lesen oder
        /// füllen ließ und nicht selbst die Standardvorlage ist — die Standardvorlage, zuletzt der
        /// bisherige Weg. Jeder Schritt zurück steht in <paramref name="rueckfaelle"/> (in der Oberflächensprache).
        /// Gefüllt wird in der Sprache des Laufs <paramref name="englisch"/> (BV-Q7 b) — auch nach einem Rückfall.
        /// </summary>
        private Berichtslauf Fuelle(BerichtsDaten daten, BerichtsKonfiguration konfig, Vorlagenwahl wahl,
                                    byte[] bytes, string lesefehler, List<string> rueckfaelle, bool englisch)
        {
            string ordner = Zielordner(konfig);
            string basis = Dateistamm(daten);

            if (wahl.Grund != Vorlagenwahlgrund.Rueckfall)
            {
                if (bytes == null)
                {
                    rueckfaelle.Add(T(nameof(R.BV_LAUF_NICHT_LESBAR), wahl.Eintrag.Name, lesefehler ?? ""));
                }
                else
                {
                    Berichtslauf lauf = MitVorlage(daten, konfig, wahl, bytes, rueckfaelle, ordner, basis, englisch, out string fuellfehler);
                    if (lauf != null) return lauf;
                    rueckfaelle.Add(T(nameof(R.BV_LAUF_NICHT_FUELLBAR), wahl.Eintrag.Name, fuellfehler));
                }

                if (!wahl.Eintrag.IstStandard)
                {
                    Vorlageneintrag standard = _vorlagen.Standardeintrag();
                    if (standard.Vorhanden)
                    {
                        byte[] standardBytes = Lies(standard, out string standardfehler);
                        Vorlagenwahl ersatz = WahlDerStandardvorlage(standard, rueckfaelle, wahl.FehlendeId);
                        if (standardBytes == null)
                        {
                            rueckfaelle.Add(T(nameof(R.BV_LAUF_NICHT_LESBAR), standard.Name, standardfehler ?? ""));
                        }
                        else
                        {
                            Berichtslauf lauf = MitVorlage(daten, konfig, ersatz, standardBytes, rueckfaelle, ordner, basis,
                                                           englisch, out string standardFuellfehler);
                            if (lauf != null) return lauf;
                            rueckfaelle.Add(T(nameof(R.BV_LAUF_NICHT_FUELLBAR), standard.Name, standardFuellfehler));
                        }
                    }
                }
            }

            return OhneVorlage(daten, konfig, wahl, rueckfaelle, ordner, basis, englisch);
        }

        /// <summary>
        /// Füllt die Vorlage über die Engine; <c>null</c> mit <paramref name="fehler"/>, wenn die Vorlage
        /// kein füllbares Word-Dokument ist (kein Word, Makros). Andere Ausnahmen — keine Berichtsdaten,
        /// Fehler der Bausteine, eine gesperrte Datei nach allen Ausweichnamen — gehen weiter: Sie
        /// träfen jede Vorlage gleich.
        /// </summary>
        private Berichtslauf MitVorlage(BerichtsDaten daten, BerichtsKonfiguration konfig, Vorlagenwahl wahl, byte[] bytes,
                                        List<string> rueckfaelle, string ordner, string basis, bool englisch, out string fehler)
        {
            fehler = null;
            Erstellerangaben ersteller = _vorlagen.Ersteller();
            try
            {
                Fuellergebnis ergebnis = MitAusweichnamen(ordner, basis, ".docx", pfad =>
                {
                    using (BerichtTexte.ImLauf(englisch))
                        return new WordBerichtGenerator().ErzeugeMitVorlage(daten, konfig, bytes, ersteller, pfad);
                });
                return new Berichtslauf(ergebnis.Zieldatei, wahl, wahl.Eintrag.Name, false, rueckfaelle, ergebnis,
                                        Vorlagenpruefer.Pruefsumme(bytes), ergebnis.Englisch);
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is NotSupportedException ||
                                       (ex is ArgumentException a && a.ParamName == "vorlage"))
            {
                // Die leere Vorlage meldet die Engine als Argumentfehler — ohne den Parameterzusatz genannt.
                fehler = ex is ArgumentException
                    ? WordVorlagentexte.T(WordVorlagentexte.VORLAGE_LEER, BerichtTexte.Englisch)
                    : ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Der bisherige Weg (Konzept 8.4): die Stilvorlage <see cref="BerichtsvorlagenCtrl.DATEI_RUECKFALL"/>
        /// neben der Standardvorlage, sonst die eingebauten Formate — immer benannt. Die Wahl des Laufs
        /// wird zum Rückfall; war sie das nicht schon, nennt ein Satz die fehlende Standardvorlage.
        /// </summary>
        private Berichtslauf OhneVorlage(BerichtsDaten daten, BerichtsKonfiguration konfig, Vorlagenwahl wahl,
                                         List<string> rueckfaelle, string ordner, string basis, bool englisch)
        {
            Vorlageneintrag standard = _vorlagen.Standardeintrag();
            string stil = standard.Rueckfallpfad ?? WordBerichtGenerator.FindeVorlage();
            string name = stil != null ? Path.GetFileName(stil) : T(nameof(R.BV_LAUF_EINGEBAUT));

            // Derselbe Satz wie in der Vorlagenwahl — steht er dort schon, nicht ein zweites Mal.
            if (!standard.Vorhanden)
            {
                string satz = stil != null
                    ? T(nameof(R.BV_VORLAGEN_STANDARD_FEHLT), standard.Dateiname, name)
                    : T(nameof(R.BV_VORLAGEN_STANDARD_FEHLT_CODE), standard.Dateiname);
                if (!rueckfaelle.Contains(satz)) rueckfaelle.Add(satz);
            }

            Vorlagenwahl rueckfall = wahl.Grund == Vorlagenwahlgrund.Rueckfall
                ? wahl
                : new Vorlagenwahl(standard, Vorlagenwahlgrund.Rueckfall, T(nameof(R.BV_VORLAGEN_GRUND_RUECKFALL)),
                                   rueckfaelle, wahl.FehlendeId);

            string geschrieben = MitAusweichnamen(ordner, basis, ".docx", pfad =>
            {
                using (BerichtTexte.ImLauf(englisch))
                    return new WordBerichtGenerator().Erzeuge(daten, konfig, pfad, stil);
            });
            return new Berichtslauf(geschrieben, rueckfall, name, true, rueckfaelle, null, "", englisch);
        }

        /// <summary>Die Standardvorlage als Wahl dieses Laufs (Ersatz für eine nicht nutzbare Vorlage).</summary>
        private static Vorlagenwahl WahlDerStandardvorlage(Vorlageneintrag standard, IReadOnlyList<string> meldungen,
                                                           string fehlendeId)
        {
            return standard.Vorhanden
                ? new Vorlagenwahl(standard, Vorlagenwahlgrund.Standard, T(nameof(R.BV_VORLAGEN_GRUND_STANDARD)), meldungen, fehlendeId)
                : new Vorlagenwahl(standard, Vorlagenwahlgrund.Rueckfall, T(nameof(R.BV_VORLAGEN_GRUND_RUECKFALL)), meldungen, fehlendeId);
        }

        /// <summary>Liest eine Vorlage einmal; <c>null</c> mit Grund, wenn sie fehlt oder nicht lesbar ist.</summary>
        private byte[] Lies(Vorlageneintrag eintrag, out string fehler)
        {
            fehler = null;
            try
            {
                byte[] bytes = _vorlagen.LiesBytes(eintrag);
                if (bytes == null) fehler = T(nameof(R.BV_VORLAGEN_FEHLT), eintrag?.Name ?? "");
                return bytes;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                fehler = ex.Message;
                return null;
            }
        }

        // =====================================================================
        //  Vorprüfung
        // =====================================================================

        /// <summary>
        /// <b>Die Vorprüfung vor dem Start</b> (Konzept 6.8, 10.2 Schritte 1 und 2): wählt die Vorlage
        /// wie der Lauf, liest sie EINMAL und prüft genau diese Bytes schnell. Mit Fehlern, abweichender
        /// Sprache oder unpassender Sicht — im zweiten Einstieg auch ohne Schlüssel der Wirtschaftlichkeit
        /// — kommt statt der heutigen Startrückfrage die erweiterte Rückfrage mit drei Wegen
        /// (<see cref="Startbefund.BrauchtRueckfrage"/>). Die Bytes gehen mit dem Befund an
        /// <see cref="ErzeugeWord(BerichtsDaten, BerichtsKonfiguration, Startbefund, Startweg)"/>.
        /// Den Bedarf der Vorlage — Stundenreihen, Verlauf, Emissionsbilanz — legt die Vorprüfung in
        /// <see cref="Startbefund.Bedarf"/> (Konzept 5.1, BV-E3; <see cref="Berichtsbedarf.AusVorlage"/>).
        /// </summary>
        /// <param name="konfig">Die Konfiguration des Laufs (Abweichung, Varianten).</param>
        /// <param name="englisch">Entsteht der Bericht auf Englisch? Sprache der Befunde und Texte.</param>
        /// <param name="sicht">Die Vergleichssicht der Ergebnisansicht: 1 oder 2 (Paarvergleich).</param>
        /// <param name="erzwingtWirtschaftlichkeit">Zweiter Einstieg „Bericht erzeugen“ der
        /// Wirtschaftlichkeitsseite: Führt die Vorlage keinen Schlüssel der Wirtschaftlichkeit, bietet die
        /// Rückfrage für diesen Lauf die Standardvorlage an und nennt die gewählte.</param>
        public Startbefund PruefeVorStart(BerichtsKonfiguration konfig, bool englisch, int sicht,
                                          bool erzwingtWirtschaftlichkeit = false)
        {
            Vorlagenwahl wahl = _vorlagen.VorlageFuer(konfig);
            int projekte = (konfig?.VariantenIds?.Count ?? 0) + 1;
            Pruefkontext kontext = Pruefkontext.Aus(konfig, englisch, sicht);

            byte[] bytes = null;
            string lesefehler = null;
            Pruefbefund befund = null;
            if (wahl.Grund != Vorlagenwahlgrund.Rueckfall)
            {
                bytes = Lies(wahl.Eintrag, out lesefehler);
                befund = bytes != null
                    ? _vorlagen.Pruefe(bytes, wahl.Eintrag, Pruefstufe.Schnell, kontext)
                    : _vorlagen.Pruefe(wahl.Eintrag, Pruefstufe.Schnell, kontext);   // benennt den Lesefehler als Befund
            }

            bool sprache = befund?.SpracheAbweichend == true;
            bool sichtUnpassend = befund != null && sicht != 2 && NutztPaarvergleich(befund.Schluessel, projekte - 1);
            bool ohneWirtschaft = erzwingtWirtschaftlichkeit && befund != null && befund.IstLesbar && !befund.HatWirtschaftlichkeit;
            // BV-Q7 b: Eine abweichende Sprache hält nicht an — der Bericht entsteht in der Sprache der Vorlage.
            bool rueckfrage = befund?.HatFehler == true || sichtUnpassend || ohneWirtschaft;

            var befunde = new List<Berichtsmeldung>();
            string text = "";
            if (rueckfrage)
            {
                befunde = Rueckfragebefunde(befund, wahl, englisch, sichtUnpassend, ohneWirtschaft);
                text = Rueckfragetext(wahl, projekte, befunde, bytes != null && befund?.HatFehler == true, englisch);
            }

            // BV-E3 (Konzept 5.1): der Bedarf der Vorlage aus ihren Platzhaltern — Stundenreihen, Verlauf und
            // Emissionsbilanz erhebt der Sammler nur, wenn die Vorlage sie zeigt. Ohne lesbare Vorlage (Rückfall
            // auf den bisherigen Weg) die Vorgabe der Häkchen.
            Berichtsbedarf bedarf = wahl.Grund == Vorlagenwahlgrund.Rueckfall
                ? Berichtsbedarf.Vorgabe(konfig)
                : Berichtsbedarf.AusVorlage(befund, konfig);

            return new Startbefund(wahl, befund, bytes, lesefehler, englisch, projekte, sprache, sichtUnpassend, ohneWirtschaft,
                                   befunde, text,
                                   Tk(englisch, nameof(R.BV_START_WEG_EIGENE)), Tk(englisch, nameof(R.BV_START_WEG_STANDARD)),
                                   Tk(englisch, nameof(R.BV_START_WEG_ABBRECHEN)), bedarf);
        }

        /// <summary>
        /// <b>Die Vorprüfung der Excel-Vorlage vor dem Start</b> (Konzept 6.8, 7.4, 10.2; Anwenderentscheid BV-E7-3):
        /// wählt die Excel-Vorlage wie der Lauf (<see cref="BerichtsvorlagenCtrl.ExcelVorlageFuer"/>), liest sie EINMAL
        /// und prüft genau diese Bytes schnell. Ihre Fehler kommen in die erweiterte Startrückfrage — wie die der
        /// Word-Vorlage; die Bytes gehen mit dem Befund an
        /// <see cref="ErzeugeExcelLauf(BerichtsDaten, BerichtsKonfiguration, Excelstartbefund, bool)"/>.
        /// Ohne Excel-Vorlage ein Befund ohne Prüfung.
        /// </summary>
        public Excelstartbefund PruefeExcelVorStart(BerichtsKonfiguration konfig, bool englisch, int sicht)
        {
            Vorlagenwahl wahl = _vorlagen.ExcelVorlageFuer(konfig);
            if (wahl?.Eintrag == null || string.Equals(wahl.Eintrag.Id, BerichtsvorlagenCtrl.ID_OHNE, StringComparison.OrdinalIgnoreCase))
                return new Excelstartbefund(wahl, null, null, englisch, null);

            Pruefkontext kontext = Pruefkontext.Aus(konfig, englisch, sicht);
            byte[] bytes = Lies(wahl.Eintrag, out _);
            Pruefbefund befund = bytes != null
                ? _vorlagen.Pruefe(bytes, wahl.Eintrag, Pruefstufe.Schnell, kontext)
                : _vorlagen.Pruefe(wahl.Eintrag, Pruefstufe.Schnell, kontext);   // benennt den Lesefehler als Befund

            var befunde = new List<Berichtsmeldung>();
            foreach (Pruefmeldung m in befund.Meldungen)
            {
                if (m.Stufe != Befundstufe.Fehler) continue;   // Warnungen und Hinweise zeigt die Prüfliste
                string text = string.IsNullOrEmpty(m.Fundort) ? m.Text : Tk(englisch, nameof(R.BV_START_PUNKT), m.Text, m.Fundort);
                befunde.Add(new Berichtsmeldung(m.Kennung, Tk(englisch, nameof(R.BV_XL_START_PUNKT), text)));
            }
            return new Excelstartbefund(wahl, befund, bytes, englisch, befunde);
        }

        /// <summary>
        /// <b>Word und Excel in einer Sprache</b> (BV-Q7 b): Tragen die Word-Vorlage des Laufs und die gewählte
        /// Excel-Vorlage verschiedene Sprachen, entsteht auch die Mappe in der Sprache der Word-Vorlage, und der
        /// Excel-Befund bekommt den Widerspruch als Befund der Rückfrage (<see cref="Excelstartbefund.Sprachwiderspruch"/>)
        /// — in derselben erweiterten Rückfrage wie Fehler der Excel-Vorlage: „Mit meiner Vorlage“ füllt beide in der
        /// Sprache der Word-Vorlage, der zweite Weg erzeugt die Mappe ohne Vorlage. Ohne Widerspruch der Befund
        /// unverändert; ein schon abgeglichener bleibt, wie er ist.
        /// </summary>
        public static Excelstartbefund SpracheAbgleichen(Startbefund word, Excelstartbefund excel)
        {
            if (excel == null || excel.Sprachwiderspruch || word == null) return excel;
            Berichtssprache sprache = Berichtssprache.Fuer(word, Startweg.Gewaehlt, excel, false, excel.Englisch);
            if (!sprache.Widerspruch) return excel;

            bool englisch = excel.Englisch;
            var befunde = new List<Berichtsmeldung>(excel.Befunde)
            {
                new Berichtsmeldung(KiMeldungskennung.VF_PRUEF_SPRACHE,
                    Tk(englisch, nameof(R.BV_XL_START_SPRACHE_PUNKT), sprache.ExcelVorlage,
                       Berichtssprache.Sprachname(sprache.ExcelEnglisch == true, englisch), sprache.Vorlage,
                       Berichtssprache.Sprachname(sprache.Englisch, englisch))),
            };
            return new Excelstartbefund(excel.Wahl, excel.Pruefbefund, excel.Bytes, englisch, befunde, true);
        }

        // =====================================================================
        //  Stellen der Kapitel (Anhang-E-Checkliste)
        // =====================================================================

        /// <summary>
        /// <b>Die Stellen der Kapitel in der gewählten Vorlage</b> (Konzept Berichtsvorlagen 11 Nr. 3; Etappe
        /// BV-E2) — ohne zu füllen: je Bausteinschlüssel (<see cref="Berichtskapitel.Stellenschluessel"/>;
        /// der Anhang E, der seinen Bausteinschlüssel mit der Wirtschaftlichkeit teilt, unter
        /// <see cref="Berichtskapitel.ANHANG_E"/>) die Überschrift, unter der das Kapitel im Bericht steht:
        /// der Kapitelkopf der Vorlage vor dem Anker, sonst die eigene Überschrift des Bausteins, in der
        /// Sprache <paramref name="englisch"/>. <c>null</c> = nicht im Bericht — die Vorlage führt das
        /// Kapitel nicht, oder sein Häkchen ist nicht gesetzt (ein Deckblatt aus Platzhaltern steht
        /// unabhängig vom Häkchen). Die Vorlage wählt der Lauf (<see cref="BerichtsvorlagenCtrl.VorlageFuer"/>);
        /// lässt sie sich nicht lesen, gilt die Standardvorlage, fehlt auch die, der bisherige Weg mit den
        /// eigenen Überschriften der angehakten Kapitel. Dieselben Stellen nennt die Checkliste des Berichts
        /// (<see cref="Fuellergebnis.Kapitelstellen"/>); <see cref="AnhangECheckliste.Punkte(ChecklistenLage, IReadOnlyDictionary{string, string})"/>
        /// macht aus ihnen die Spalte „Stelle“.
        /// </summary>
        public IReadOnlyDictionary<string, string> KapitelstellenDerVorlage(BerichtsKonfiguration konfig, bool englisch)
        {
            Vorlagenwahl wahl = _vorlagen.VorlageFuer(konfig);
            var kandidaten = new List<Vorlageneintrag>();
            if (wahl.Grund != Vorlagenwahlgrund.Rueckfall) kandidaten.Add(wahl.Eintrag);
            Vorlageneintrag standard = _vorlagen.Standardeintrag();
            if (standard.Vorhanden && !kandidaten.Any(k => k.IstStandard)) kandidaten.Add(standard);

            foreach (Vorlageneintrag eintrag in kandidaten)
            {
                byte[] bytes = Lies(eintrag, out _);
                if (bytes == null) continue;
                Pruefbefund befund = Vorlagenpruefer.Pruefe(bytes, Pruefstufe.Schnell, Pruefkontext.Aus(konfig, englisch));
                if (!befund.IstLesbar) continue;

                var stellen = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (Berichtskapitel k in Berichtskapitel.Alle)
                {
                    befund.Kapitelstellen.TryGetValue(k.Stellenschluessel, out string stelle);
                    if (stelle != null && !k.IstAktiv(konfig))
                        stelle = k.Name == Berichtskapitel.DECKBLATT && befund.DeckblattAusPlatzhaltern ? k.Ueberschrift(englisch) : null;
                    stellen[k.Stellenschluessel] = stelle;
                }
                return stellen;
            }
            return Berichtskapitel.EigeneStellen(konfig, englisch);
        }

        /// <summary>
        /// Nutzt die Vorlage den Paarvergleich (<c>stand.a</c>, <c>stand.b</c>, Konzept 4.7)? In Sicht 1
        /// bleibt <c>stand.b</c> allein zulässig, wenn genau eine Variante gewählt ist.
        /// </summary>
        internal static bool NutztPaarvergleich(IEnumerable<string> schluessel, int anzahlVarianten)
        {
            List<string> liste = (schluessel ?? Enumerable.Empty<string>()).ToList();
            bool a = liste.Any(k => IstStand(k, "a"));
            bool b = liste.Any(k => IstStand(k, "b"));
            if (!a && !b) return false;
            return a || anzahlVarianten != 1;
        }

        private static bool IstStand(string schluessel, string seite)
        {
            string praefix = "stand." + seite;
            return string.Equals(schluessel, praefix, StringComparison.Ordinal) ||
                   (schluessel ?? "").StartsWith(praefix + ".", StringComparison.Ordinal);
        }

        /// <summary>Die Befunde der Rückfrage: Fehler der Prüfung, die Sicht, die Wirtschaftlichkeit.</summary>
        private static List<Berichtsmeldung> Rueckfragebefunde(Pruefbefund befund, Vorlagenwahl wahl, bool englisch,
                                                               bool sichtUnpassend, bool ohneWirtschaft)
        {
            var liste = new List<Berichtsmeldung>();
            if (befund != null)
            {
                foreach (Pruefmeldung m in befund.Meldungen)
                {
                    // Die Fehler und „in Word geöffnet“ (gefüllt wird der gespeicherte Stand) — Hinweise
                    // und übrige Warnungen zeigt die Prüfliste. Die Sprache der Vorlage ist kein Befund der
                    // Rückfrage (BV-Q7 b): Sie steht als Information darüber (Berichtssprache.Hinweis).
                    bool zaehlt = m.Stufe == Befundstufe.Fehler ||
                                  string.Equals(m.Kennung, nameof(R.BV_VORLAGEN_IN_WORD), StringComparison.Ordinal);
                    if (!zaehlt) continue;
                    string text = string.IsNullOrEmpty(m.Fundort) ? m.Text : Tk(englisch, nameof(R.BV_START_PUNKT), m.Text, m.Fundort);
                    liste.Add(new Berichtsmeldung(m.Kennung, text));
                }
            }
            if (sichtUnpassend)
                liste.Add(new Berichtsmeldung(KiMeldungskennung.BV_START_SICHT, Tk(englisch, nameof(R.BV_START_SICHT))));
            if (ohneWirtschaft)
                liste.Add(new Berichtsmeldung(KiMeldungskennung.BV_START_OHNE_WIRTSCHAFT,
                                              Tk(englisch, nameof(R.BV_START_OHNE_WIRTSCHAFT), wahl.Eintrag.Name)));
            return liste;
        }

        /// <summary>Der Text der erweiterten Rückfrage: Kopf, Fehlendes, Befunde (gekappt), Hinweis auf Gelb, Frage.</summary>
        private static string Rueckfragetext(Vorlagenwahl wahl, int projekte, IReadOnlyList<Berichtsmeldung> befunde,
                                             bool gelbMoeglich, bool englisch)
        {
            var sb = new StringBuilder();
            sb.Append(Tk(englisch, nameof(R.BV_START_KOPF), projekte, wahl.Eintrag.Name));
            foreach (string meldung in wahl.Meldungen) sb.Append("\r\n").Append(meldung);
            sb.Append("\r\n\r\n").Append(Tk(englisch, nameof(R.BV_START_BEFUNDE)));
            foreach (string punkt in Kappe(befunde.Select(b => b.Text).ToList(), englisch)) sb.Append("\r\n• ").Append(punkt);
            if (gelbMoeglich) sb.Append("\r\n\r\n").Append(Tk(englisch, nameof(R.BV_START_GELB)));
            sb.Append("\r\n\r\n").Append(Tk(englisch, nameof(R.BV_START_FRAGE)));
            return sb.ToString();
        }

        // =====================================================================
        //  Laufmeldung
        // =====================================================================

        /// <summary>
        /// <b>Die Laufmeldung eines Word-Laufs</b> (Konzept 10.2 Schritt 5) als Abschnitte, je mit
        /// Kennung für „erklären lassen“: die Vorlage mit ihrem Grund, die Rückfälle, die gelb stehen
        /// gebliebenen und die zusammengefasst leeren Platzhalter, die entfernten Kommentare und alle
        /// Warnungen der Engine. Die Rahmentexte stehen in der gewählten Sprache; Fundorte, Gründe und
        /// Warnungen der Engine in der Sprache des Laufs.
        /// </summary>
        public static IReadOnlyList<Berichtsmeldung> Laufabschnitte(Berichtslauf lauf, bool englisch)
        {
            var liste = new List<Berichtsmeldung>();
            if (lauf == null) return liste;

            liste.Add(new Berichtsmeldung(KiMeldungskennung.BV_LAUF_VORLAGE,
                                          Tk(englisch, nameof(R.BV_LAUF_VORLAGE), lauf.VorlageName, Grundtext(lauf.Grund, englisch))));
            // BV-Q7 b: Entstand der Bericht nicht in der Sprache der Meldung (der Oberfläche), kam die Sprache aus der Vorlage.
            if (lauf.Englisch != englisch)
                liste.Add(new Berichtsmeldung(KiMeldungskennung.VF_PRUEF_SPRACHE,
                                              Tk(englisch, nameof(R.BV_LAUF_SPRACHE), Berichtssprache.Sprachname(lauf.Englisch, englisch))));
            if (lauf.Rueckfaelle.Count > 0)
                liste.Add(new Berichtsmeldung(KiMeldungskennung.BV_LAUF_RUECKFALL, Tk(englisch, nameof(R.BV_LAUF_RUECKFALL)),
                                              Kappe(lauf.Rueckfaelle, englisch)));
            if (lauf.Unbekannte.Count > 0)
                liste.Add(new Berichtsmeldung(KiMeldungskennung.BV_LAUF_UNBEKANNT,
                                              Tk(englisch, nameof(R.BV_LAUF_UNBEKANNT), lauf.Unbekannte.Count),
                                              Kappe(lauf.Unbekannte.Select(b => Tk(englisch, nameof(R.BV_LAUF_PUNKT_UNBEKANNT),
                                                                                    b.Normalform, b.Grund, b.Fundort)).ToList(), englisch)));
            IReadOnlyList<string> leere = Berichtslauf.FasseLeereZusammen(lauf.Leere, lauf.Stellen, englisch);
            if (leere.Count > 0)
                liste.Add(new Berichtsmeldung(KiMeldungskennung.BV_LAUF_LEER, Tk(englisch, nameof(R.BV_LAUF_LEER), leere.Count),
                                              Kappe(leere, englisch)));
            if (lauf.EntfernteKommentare > 0)
                liste.Add(new Berichtsmeldung(KiMeldungskennung.BV_LAUF_KOMMENTARE,
                                              Tk(englisch, nameof(R.BV_LAUF_KOMMENTARE), lauf.EntfernteKommentare)));
            if (lauf.Warnungen.Count > 0)
                liste.Add(new Berichtsmeldung(KiMeldungskennung.BV_LAUF_WARNUNGEN,
                                              Tk(englisch, nameof(R.BV_LAUF_WARNUNGEN), lauf.Warnungen.Count),
                                              Kappe(lauf.Warnungen, englisch)));
            return liste;
        }

        /// <summary>
        /// Die Laufmeldung als Text für die Hülle (Konzept 10.2 Schritt 5): je Abschnitt eine Zeile,
        /// die Aufzählung mit „• “ darunter. Nennt immer die Vorlage; alles Weitere nur, wenn es etwas
        /// zu nennen gibt.
        /// </summary>
        public static string Laufmeldung(Berichtslauf lauf, bool englisch)
        {
            var sb = new StringBuilder();
            foreach (Berichtsmeldung abschnitt in Laufabschnitte(lauf, englisch))
            {
                if (sb.Length > 0) sb.Append("\r\n");
                sb.Append(abschnitt.Text);
                foreach (string punkt in abschnitt.Punkte) sb.Append("\r\n• ").Append(punkt);
            }
            return sb.ToString();
        }

        /// <summary>Der Grund der Vorlagenwahl in der gewählten Sprache.</summary>
        private static string Grundtext(Vorlagenwahlgrund grund, bool englisch)
        {
            switch (grund)
            {
                case Vorlagenwahlgrund.Abweichung: return Tk(englisch, nameof(R.BV_VORLAGEN_GRUND_ABWEICHUNG));
                case Vorlagenwahlgrund.Vorgabe: return Tk(englisch, nameof(R.BV_VORLAGEN_GRUND_VORGABE));
                case Vorlagenwahlgrund.Rueckfall: return Tk(englisch, nameof(R.BV_VORLAGEN_GRUND_RUECKFALL));
                default: return Tk(englisch, nameof(R.BV_VORLAGEN_GRUND_STANDARD));
            }
        }

        /// <summary>Höchstens <see cref="MAX_PUNKTE"/> Punkte, danach „… und n weitere“.</summary>
        private static IReadOnlyList<string> Kappe(IReadOnlyList<string> punkte, bool englisch)
        {
            if (punkte == null || punkte.Count <= MAX_PUNKTE) return punkte ?? Array.Empty<string>();
            var gekappt = punkte.Take(MAX_PUNKTE).ToList();
            gekappt.Add(Tk(englisch, nameof(R.BV_LAUF_WEITERE), punkte.Count - MAX_PUNKTE));
            return gekappt;
        }

        // =====================================================================
        //  Excel
        // =====================================================================

        /// <summary>
        /// Erzeugt die Excel-Ausgabe (Konzept Kap. 9; ClosedXML) — gleiche Namens-
        /// und Kollisionslogik wie ErzeugeWord, Endung .xlsx.
        /// Rückgabe: Pfad der geschriebenen Datei; die Laufmeldung liefert <see cref="ErzeugeExcelLauf"/>.
        /// </summary>
        public string ErzeugeExcel(BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            return ErzeugeExcelLauf(daten, konfig).Pfad;
        }

        /// <summary>
        /// <b>Die Excel-Mappe eines Laufs</b> (Konzept Berichtsvorlagen 7, 10.3; Etappe BV-E7): Die Excel-Vorlage wählt
        /// <see cref="BerichtsvorlagenCtrl.ExcelVorlageFuer"/> — Abweichung des Stammprojekts, Vorgabe der Installation,
        /// sonst ohne Vorlage. Ohne Vorlage entsteht die Mappe wie bisher im Code (<see cref="ExcelBerichtGenerator"/>,
        /// unverändert); mit Vorlage füllt sie der <see cref="ExcelVorlagenfueller"/>. Lässt sich die Vorlage nicht lesen
        /// oder füllen (keine Excel-Mappe, Makros, Ladefehler), entsteht die Mappe ohne Vorlage, und der Lauf nennt es.
        /// </summary>
        public Berichtslauf ErzeugeExcelLauf(BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            return ErzeugeExcelLauf(daten, konfig, null, false);
        }

        /// <summary>
        /// Die Excel-Mappe eines Laufs nach der Vorprüfung (Anwenderentscheid BV-E7-3): Passt der Befund zur Wahl dieses
        /// Laufs (dieselbe Vorlage), füllt der Lauf GENAU die geprüften Bytes. <paramref name="ohneVorlage"/> ist die
        /// Antwort der Rückfrage auf Fehler der Excel-Vorlage — für diesen Lauf entsteht die Mappe ohne Vorlage, und die
        /// Laufmeldung nennt es. Die Mappe entsteht in der Sprache des Laufs <paramref name="sprache"/> (BV-Q7 b: bei
        /// einem Word-Bericht dieselbe wie dessen); ohne Angabe in der Sprache der gefüllten Excel-Vorlage, sonst in der
        /// Oberflächensprache.
        /// </summary>
        public Berichtslauf ErzeugeExcelLauf(BerichtsDaten daten, BerichtsKonfiguration konfig, Excelstartbefund start,
                                             bool ohneVorlage, Berichtssprache sprache = null)
        {
            string ordner = Zielordner(konfig);
            string basis = Dateistamm(daten);
            bool englisch = BerichtTexte.Englisch;
            bool laufEnglisch = sprache?.Englisch ?? englisch;
            Vorlagenwahl wahl = _vorlagen.ExcelVorlageFuer(konfig);
            var rueckfaelle = new List<string>(wahl.Meldungen);
            bool mitVorlage = !string.Equals(wahl.Eintrag.Id, BerichtsvorlagenCtrl.ID_OHNE, StringComparison.OrdinalIgnoreCase);

            if (mitVorlage && ohneVorlage)
            {
                rueckfaelle.Add(Tk(englisch, nameof(R.BV_XL_LAUF_OHNE_GEWAEHLT), wahl.Eintrag.Name));
            }
            else if (mitVorlage)
            {
                bool geprueft = start?.Bytes != null && start.Wahl?.Eintrag != null
                                && string.Equals(start.Wahl.Eintrag.Id, wahl.Eintrag.Id, StringComparison.Ordinal);
                string lesefehler = null;
                byte[] bytes = geprueft ? start.Bytes : Lies(wahl.Eintrag, out lesefehler);
                if (bytes == null)
                {
                    rueckfaelle.Add(Tk(englisch, nameof(R.BV_XL_LAUF_RUECKFALL), wahl.Eintrag.Name, lesefehler ?? ""));
                }
                else
                {
                    Erstellerangaben ersteller = _vorlagen.Ersteller();
                    bool vorlagenEnglisch = sprache?.Englisch ?? Berichtssprache.AusPaket(bytes) ?? englisch;
                    try
                    {
                        Fuellergebnis ergebnis = MitAusweichnamen(ordner, basis, ".xlsx", pfad =>
                        {
                            using (BerichtTexte.ImLauf(vorlagenEnglisch))
                                return new ExcelVorlagenfueller().Fuelle(bytes, daten, konfig, ersteller, pfad);
                        });
                        return new Berichtslauf(ergebnis.Zieldatei, wahl, wahl.Eintrag.Name, false, rueckfaelle, ergebnis,
                                                Vorlagenpruefer.Pruefsumme(bytes), ergebnis.Englisch);
                    }
                    catch (Exception ex) when (ex is InvalidDataException || ex is NotSupportedException ||
                                               (ex is ArgumentException a && a.ParamName == "vorlage"))
                    {
                        string grund = ex is ArgumentException ? Tk(englisch, nameof(R.VF_PRUEF_GRUND_LEER)) : ex.Message;
                        rueckfaelle.Add(Tk(englisch, nameof(R.BV_XL_LAUF_RUECKFALL), wahl.Eintrag.Name, grund));
                    }
                }
            }

            Vorlageneintrag ohne = _vorlagen.OhneExcelEintrag();
            Vorlagenwahl ohneWahl = wahl.Eintrag.Id == ohne.Id && rueckfaelle.Count == wahl.Meldungen.Count
                ? wahl
                : new Vorlagenwahl(ohne, Vorlagenwahlgrund.Rueckfall, T(nameof(R.BV_VORLAGEN_GRUND_RUECKFALL)), rueckfaelle, wahl.FehlendeId);
            string geschrieben = MitAusweichnamen(ordner, basis, ".xlsx", pfad =>
            {
                using (BerichtTexte.ImLauf(laufEnglisch))
                    return new ExcelBerichtGenerator().Erzeuge(daten, konfig, pfad);
            });
            return new Berichtslauf(geschrieben, ohneWahl, ohne.Name, true, rueckfaelle, null, "", laufEnglisch);
        }

        /// <summary>
        /// Die Laufmeldung der Excel-Mappe (Konzept 10.2 Schritt 5): die Vorlage mit ihrem Grund, die Rückfälle, die gelb
        /// stehen gebliebenen und die leeren Platzhalter, die Warnungen und Hinweise der Engine. Ohne Vorlage und ohne
        /// Rückfall leer — die Mappe entstand wie immer.
        /// </summary>
        public static string LaufmeldungExcel(Berichtslauf lauf, bool englisch)
        {
            if (lauf == null || (lauf.IstRueckfall && lauf.Rueckfaelle.Count == 0)) return "";
            var sb = new StringBuilder();
            IReadOnlyList<Berichtsmeldung> abschnitte = Laufabschnitte(lauf, englisch);
            for (int i = 0; i < abschnitte.Count; i++)
            {
                if (sb.Length > 0) sb.Append("\r\n");
                sb.Append(i == 0
                    ? (lauf.IstRueckfall ? Tk(englisch, nameof(R.BV_XL_LAUF_OHNE)) : Tk(englisch, nameof(R.BV_XL_LAUF_VORLAGE), lauf.VorlageName,
                                                                                     Grundtext(lauf.Grund, englisch)))
                    : abschnitte[i].Text);
                foreach (string punkt in abschnitte[i].Punkte) sb.Append("\r\n• ").Append(punkt);
            }
            if (lauf.Hinweise.Count > 0)
            {
                sb.Append("\r\n").Append(Tk(englisch, nameof(R.BV_XL_LAUF_HINWEISE), lauf.Hinweise.Count));
                foreach (string punkt in Kappe(lauf.Hinweise, englisch)) sb.Append("\r\n• ").Append(punkt);
            }
            return sb.ToString();
        }

        // =====================================================================
        //  Dateiname und Zielordner
        // =====================================================================

        /// <summary>Der Zielordner der Konfiguration (leer = Dokumente-Ordner), bei Bedarf angelegt.</summary>
        private static string Zielordner(BerichtsKonfiguration konfig)
        {
            string ordner = konfig != null && !string.IsNullOrWhiteSpace(konfig.ZielOrdner)
                ? konfig.ZielOrdner
                : Dienste.Pfade.Dokumente;
            // Ein vorhandener Ordner wird nicht angelegt: Windows meldet beim Anlegen „Zugriff verweigert“ auch dann,
            // wenn der Überwachte Ordnerzugriff das Programm sperrt — benannt wird das beim Schreiben der Datei.
            try
            {
                if (!Directory.Exists(ordner)) Directory.CreateDirectory(ordner);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new OrdnerGesperrtException(ordner, OrdnerGesperrtException.Zielordner(ordner), ex);
            }
            return ordner;
        }

        /// <summary>&lt;Projektname&gt;_Bericht_&lt;JJJJ-MM-TT&gt; ohne Endung.</summary>
        private static string Dateistamm(BerichtsDaten daten)
        {
            return BereinigeDateiname(daten?.Stammprojektname) + "_Bericht_" +
                   DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Vorhandene Dateien nicht still überschreiben (Konzept Kap. 10): freier Name basis, basis_2, …
        /// wird gewählt; scheitert das Schreiben an einer Sperre, der nächste Name — bis _20.
        /// </summary>
        private static T MitAusweichnamen<T>(string ordner, string basis, string endung, Func<string, T> schreibe)
        {
            string pfad = Path.Combine(ordner, basis + endung);
            int n = 2;
            while (File.Exists(pfad)) { pfad = Path.Combine(ordner, basis + "_" + n + endung); n++; }

            while (true)
            {
                try
                {
                    return schreibe(pfad);
                }
                catch (UnauthorizedAccessException ex) when (!(ex is OrdnerGesperrtException))
                {
                    // Zugriff verweigert: Ein anderer Name hilft nicht — der Ordner ist gesperrt (benannt).
                    throw new OrdnerGesperrtException(ordner, OrdnerGesperrtException.Zielordner(ordner), ex);
                }
                catch (IOException)
                {
                    // Datei gesperrt/nicht schreibbar → Alternativname versuchen.
                    pfad = Path.Combine(ordner, basis + "_" + n + endung);
                    if (++n > MAX_AUSWEICHNAMEN) throw;
                }
            }
        }

        private static string BereinigeDateiname(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "EPOS-Plan";
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name.Trim();
        }

        /// <summary>Ein Text der Oberfläche (Sprache der Oberfläche) — wie die Meldungen der Vorlagenwahl.</summary>
        private static string T(string schluessel, params object[] argumente)
        {
            string muster = null;
            try { muster = R.ResourceManager.GetString(schluessel, CultureInfo.CurrentUICulture); }
            catch (Exception) { muster = null; }
            muster ??= schluessel;
            if (argumente == null || argumente.Length == 0) return muster;
            try { return string.Format(CultureInfo.CurrentCulture, muster, argumente); }
            catch (FormatException) { return muster; }
        }

        /// <summary>Ein Text in der Sprache des Berichts.</summary>
        private static string Tk(bool englisch, string schluessel, params object[] argumente)
        {
            return Berichtslauftexte.T(englisch, schluessel, argumente);
        }

        // =====================================================================
        //  Konfiguration
        // =====================================================================

        /// <summary>Lädt die gespeicherte Konfiguration des Stammprojekts (sonst Standard).</summary>
        public BerichtsKonfiguration Lade(int idStammProjekt)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT KonfigJson FROM " + TAB_KONFIG + " WHERE ProjektID = ?",
                    new DbParam("@p", idStammProjekt));
                return BerichtsKonfiguration.AusJson(o as string);
            }
            catch { return BerichtsKonfiguration.Standard(); }
        }

        /// <summary>Speichert die Konfiguration des Stammprojekts (Insert oder Update).</summary>
        public bool Speichere(int idStammProjekt, BerichtsKonfiguration konfig)
        {
            if (idStammProjekt <= 0 || konfig == null) return false;

            string json = konfig.NachJson();
            try
            {
                int rows = DataRepository.ExecuteNonQuery(
                    "UPDATE " + TAB_KONFIG + " SET KonfigJson = ?, GeaendertAm = ? WHERE ProjektID = ?",
                    new DbParam("@json", json),
                    new DbParam("@am", DbParamTyp.Date) { Wert = DateTime.Now },
                    new DbParam("@p", idStammProjekt));
                if (rows > 0) return true;

                int id = DataRepository.GetMaxID(TAB_KONFIG, "ID") + 1;
                return DataRepository.ExecuteSQL(
                    "INSERT INTO " + TAB_KONFIG + " (ID, ProjektID, KonfigJson, GeaendertAm) VALUES (?,?,?,?)",
                    new DbParam("@id", id),
                    new DbParam("@p", idStammProjekt),
                    new DbParam("@json", json),
                    new DbParam("@am", DbParamTyp.Date) { Wert = DateTime.Now });
            }
            catch { return false; }
        }
    }
}
