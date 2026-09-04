using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>Wie eine Importkette ausgegangen ist.</summary>
    public enum ImportAusgang
    {
        /// <summary>Die Reihe steht — bei <c>MitAblage</c> zusaetzlich: sie ist gespeichert.</summary>
        Erfolg,

        /// <summary>Der Anwender hat abgebrochen. Stiller Ausstieg, keine Meldung.</summary>
        Abgebrochen,

        /// <summary>Die Datei ist nicht lesbar oder die Pruefung hat einen Fehler gefunden.</summary>
        Fehler,

        /// <summary>Der Konfliktdialog stand auf „Auslassen" — es wurde nichts gespeichert.</summary>
        Ausgelassen
    }

    /// <summary>Ergebnis einer Importkette.</summary>
    public sealed class GanglinienImportErgebnis
    {
        /// <summary>Wie es ausgegangen ist.</summary>
        public ImportAusgang Ausgang = ImportAusgang.Abgebrochen;

        /// <summary>Der Zielname — Dateiname ohne Erweiterung, oder der umbenannte Name.</summary>
        public string Bezeichner = "";

        /// <summary>Raster der Reihe: 1 = Stunde, 4 = Viertelstunde.</summary>
        public int Zeitinterval;

        /// <summary>Die geprueften Werte [kW]; leer, wenn die Kette nicht durchlief.</summary>
        public double[] Werte = Array.Empty<double>();

        /// <summary>
        /// Der Text, den der Wirt als Banner zeigt — bereits formatiert und
        /// uebersetzt. Leer heisst: nichts zu melden.
        /// </summary>
        public string Meldung = "";

        /// <summary>Dringlichkeit der <see cref="Meldung"/>.</summary>
        public PruefStufe MeldungStufe = PruefStufe.Info;

        /// <summary>Das zusammengefuehrte Pruefprotokoll (Lesen + Pruefen).</summary>
        public List<PruefMeldung> Protokoll = new List<PruefMeldung>();

        /// <summary>Kurzform fuer „die Reihe steht".</summary>
        public bool Erfolgreich => Ausgang == ImportAusgang.Erfolg;
    }

    /// <summary>
    /// Die drei Entscheidungen, die die Kette dem Anwender vorlegt. Jeder Rueckruf
    /// darf <c>null</c> sein — dann bricht die Kette an dieser Stelle ab; sie zeigt
    /// nie selbst etwas an.
    /// </summary>
    public sealed class GanglinienImportRueckrufe
    {
        /// <summary>
        /// Optionen bestaetigen oder uebersteuern (Schritt 2). <c>null</c> als
        /// Rueckgabe = Abbruch.
        /// </summary>
        public Func<string, GanglinienVorschau, Task<GanglinienImportOptionen>> Optionen;

        /// <summary>
        /// Protokoll vorlegen (Schritt 5): Meldungen, „Import moeglich",
        /// „Bestaetigung noetig". <c>false</c> = Abbruch. Ein sauberer Lauf ruft
        /// diesen Rueckruf GAR NICHT — dieselbe Regel wie <c>Zeigen</c> im
        /// Vorlaeufer.
        /// </summary>
        public Func<IList<PruefMeldung>, bool, bool, Task<bool>> Protokoll;

        /// <summary>
        /// Konflikte aufloesen (Schritt 6): Pruefungen und die vergebenen Namen.
        /// <c>null</c> als Rueckgabe = Abbruch. Wird nur gerufen, wenn der Befund
        /// nicht <c>Neu</c> ist.
        /// </summary>
        public Func<List<ImportPruefung>, HashSet<string>, Task<List<KonfliktEntscheidung>>> Konflikte;
    }

    /// <summary>
    /// <b>Die AP5-Importkette — EINMAL</b> (iU9-W12.0d, Befund W12-B1).
    ///
    /// <para><b>Warum es sie gibt.</b> Bis zu dieser Welle stand die Kette ZWEIMAL
    /// woertlich im Bestand: <c>Form_Stromganglinie_Admin.btn_Einlesen_Click</c>
    /// (:93-261, mit Ablage) und <c>Form_PeakShaving.Datei_Click</c> (:322-396, ohne
    /// Ablage). Die Doppelung war im Quelltext selbst vermerkt
    /// (<c>Form_PeakShaving.cs:29-36</c>: „der einzige Unterschied ist der letzte
    /// Schritt"). Zwei Fassungen derselben Kette laufen beim ersten Fachwechsel
    /// auseinander — und genau diese Kette ist der bitgleiche Nachweis der Welle.</para>
    ///
    /// <para><b>Die Schritte</b> (Kommentarblock <c>Form_Stromganglinie_Admin.cs:79-92</c>):</para>
    /// <list type="number">
    ///   <item><description><see cref="GanglinienDatei.Erkenne"/> — Trennzeichen, Dezimaltrenner, Kopfzeile, Spalten</description></item>
    ///   <item><description>Rueckruf <see cref="GanglinienImportRueckrufe.Optionen"/> — Vorbelegung anzeigen und uebersteuern lassen</description></item>
    ///   <item><description><see cref="GanglinienDatei.Lies"/> — CSV/TXT ueber NReco, Excel als ein Bulk-Read</description></item>
    ///   <item><description><see cref="GanglinienPruefung.Pruefe"/> — Raster, Einheit, Schaltjahr, Sommerzeit, Plausibilitaet</description></item>
    ///   <item><description>Rueckruf <see cref="GanglinienImportRueckrufe.Protokoll"/> — Fehler blockieren, Eingriffe brauchen Bestaetigung</description></item>
    ///   <item><description><see cref="DublettenPruefung"/> und Rueckruf <see cref="GanglinienImportRueckrufe.Konflikte"/> — Namensabgleich gegen den Katalog</description></item>
    ///   <item><description>Ablage in einer Transaktion — NUR bei <see cref="MitAblage"/></description></item>
    /// </list>
    ///
    /// <para><b>Die Kette zeigt nichts an und kennt keine Oberflaeche.</b> Sie legt
    /// Entscheidungen als Rueckrufe vor und liefert ihre Meldungen als Text im
    /// Ergebnis; ob daraus ein Warnbanner, eine <c>MessageBox</c> oder eine
    /// iOS-Blase wird, entscheidet der Wirt.</para>
    /// </summary>
    public static class GanglinienImportAblauf
    {
        /// <summary>Der Unterordner der verlustfreien Originalablage unter <c>Dienste.Pfade.BenutzerLokal</c>.</summary>
        public const string OrdnerStrom = "Strom";

        /// <summary>Der Katalogschluessel der Stromganglinien.</summary>
        public const string Katalog = "STROMGANGLINIE";

        // ==================================================================
        // Der Ordner der Originalablage
        // ==================================================================

        /// <summary>
        /// <c>&lt;BenutzerLokal&gt;\Strom</c> — der Startordner der Dateiwahl und das
        /// Ziel der verlustfreien Originalkopie. Legt NICHTS an.
        /// </summary>
        public static string AblageOrdner()
            => Dienste.Pfade.Verbinde(Dienste.Pfade.BenutzerLokal, OrdnerStrom);

        // ==================================================================
        // Die zwei Auspraegungen
        // ==================================================================

        /// <summary>
        /// Die Kette MIT Ablage — der Weg der Stammdatenverwaltung.
        /// </summary>
        /// <param name="pfad">Die vom Anwender gewaehlte Datei.</param>
        /// <param name="rasterVorgabe">
        /// Das Raster aus der Auswahlliste der Maske; es uebersteuert die Erkennung
        /// (Vorlaeufer :149). <see cref="GanglinienRaster.Unbekannt"/> laesst die
        /// Erkennung entscheiden.
        /// </param>
        /// <param name="rueckrufe">Die drei Entscheidungen.</param>
        public static async Task<GanglinienImportErgebnis> MitAblage(
            string pfad, GanglinienRaster rasterVorgabe, GanglinienImportRueckrufe rueckrufe)
        {
            GanglinienImportErgebnis erg = new GanglinienImportErgebnis();
            if (string.IsNullOrEmpty(pfad)) return erg;

            // --- Verlustfreie Originalablage --------------------------------
            // Die Datenbank fuehrt nur die NORMALISIERTE Reihe; die Quelldatei
            // bleibt daneben liegen. Anders als im Vorlaeufer (catch { }, Befund
            // W12-B13) wird ein Fehlschlag NICHT verschluckt, sondern steht als
            // Warnung im Protokoll: Wer glaubt, sein Original sei gesichert, und es
            // ist nicht so, merkt es sonst erst, wenn er es braucht.
            string dateiname = Path.GetFileName(pfad);
            string lesepfad = pfad;
            try
            {
                string ordner = AblageOrdner();
                string ziel = Path.Combine(ordner, dateiname);
                if (!File.Exists(ziel))
                {
                    Directory.CreateDirectory(ordner);
                    File.Copy(pfad, ziel, true);
                }
                // WOERTLICH aus dem Vorlaeufer (:132-133): Gelesen wird die Kopie,
                // wenn es sie gibt. Traegt der Ordner schon eine gleichnamige Datei,
                // gewinnt DIESE - nicht die soeben gewaehlte (Befund W12-B28, offener
                // Punkt).
                if (File.Exists(ziel)) lesepfad = ziel;
            }
            catch (Exception ex)
            {
                erg.Protokoll.Add(new PruefMeldung(PruefStufe.Warnung,
                    GanglinienDatei.SchluesselLesefehler, ex.Message));
            }

            // Bezeichner = Dateiname ohne Erweiterung, woertlich und unveraenderbar.
            string bezeichner = Path.GetFileNameWithoutExtension(dateiname);

            GanglinienPruefErgebnis geprueft = await Lauf(lesepfad, rasterVorgabe, rueckrufe, erg);
            if (geprueft == null) return erg;

            // --- 6) Dublettenpruefung ---------------------------------------
            string zielName = bezeichner;
            bool ueberschreiben = false;

            KatalogDefinition k = KatalogRegistry.Finde(Katalog);
            ImportKandidat kandidat = new ImportKandidat { Name = bezeichner };
            kandidat.Werte["Zeitinterval"] = geprueft.Zeitinterval;
            List<ImportPruefung> pruefungen = DublettenPruefung.PruefeKandidaten(
                k, new List<ImportKandidat> { kandidat });

            if (pruefungen.Count > 0 && pruefungen[0].Befund != ImportBefund.Neu)
            {
                if (rueckrufe == null || rueckrufe.Konflikte == null)
                {
                    erg.Ausgang = ImportAusgang.Abgebrochen;
                    return erg;
                }

                List<KonfliktEntscheidung> entscheidungen =
                    await rueckrufe.Konflikte(pruefungen, DublettenPruefung.VergebeneNamen(k));
                if (entscheidungen == null || entscheidungen.Count == 0)
                {
                    erg.Ausgang = ImportAusgang.Abgebrochen;   // stiller Ausstieg
                    return erg;
                }

                KonfliktEntscheidung ent = entscheidungen[0];
                switch (ent.Aktion)
                {
                    case KonfliktAktion.Auslassen:
                        erg.Ausgang = ImportAusgang.Ausgelassen;
                        erg.Meldung = MyResource.Resource.IMPORT_MSG_BEREITS_VORHANDEN;
                        erg.MeldungStufe = PruefStufe.Info;
                        return erg;
                    case KonfliktAktion.Ueberschreiben:
                        ueberschreiben = true;
                        break;
                    case KonfliktAktion.Umbenennen:
                        zielName = ent.NeuerName;
                        break;
                    default:
                        // Importieren (nur bei Befund InhaltsGleich waehlbar):
                        // normaler Neuimport unter dem Originalnamen.
                        break;
                }
            }

            // --- 7) Ablage - unveraendertes Transaktionsmuster ---------------
            StromganglinieStammCtrl ctrl = new StromganglinieStammCtrl();
            bool gespeichert = ueberschreiben
                ? ctrl.ErsetzeGanglinie(zielName, geprueft.Zeitinterval, geprueft.Werte)
                : ctrl.ImportGanglinie(zielName, geprueft.Zeitinterval, geprueft.Werte);

            erg.Bezeichner = zielName;
            erg.Zeitinterval = geprueft.Zeitinterval;
            erg.Werte = geprueft.Werte;

            if (!gespeichert)
            {
                erg.Ausgang = ImportAusgang.Fehler;
                erg.Meldung = MyResource.Resource.IMPORT_MSG_FEHLER_SPEICHERN;
                erg.MeldungStufe = PruefStufe.Fehler;
                return erg;
            }

            erg.Ausgang = ImportAusgang.Erfolg;
            erg.Meldung = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.IMPORT_MSG_ERFOLG,
                zielName, geprueft.Werte.Length, geprueft.Zeitinterval);
            erg.MeldungStufe = PruefStufe.Info;
            return erg;
        }

        /// <summary>
        /// Die Kette OHNE Ablage — der Weg der Lastspitzenkappung. Schritt 6 lautet
        /// dort „KEINE Ablage": die geprueften Werte bleiben im Speicher.
        /// </summary>
        /// <param name="pfad">Die vom Anwender gewaehlte Datei.</param>
        /// <param name="rueckrufe">Die zwei Entscheidungen (Konflikte gibt es hier nicht).</param>
        public static async Task<GanglinienImportErgebnis> OhneAblage(
            string pfad, GanglinienImportRueckrufe rueckrufe)
        {
            GanglinienImportErgebnis erg = new GanglinienImportErgebnis();
            if (string.IsNullOrEmpty(pfad) || !File.Exists(pfad)) return erg;

            GanglinienPruefErgebnis geprueft =
                await Lauf(pfad, GanglinienRaster.Unbekannt, rueckrufe, erg);
            if (geprueft == null) return erg;

            erg.Bezeichner = Path.GetFileNameWithoutExtension(pfad);
            erg.Zeitinterval = geprueft.Zeitinterval;
            erg.Werte = geprueft.Werte;
            erg.Ausgang = ImportAusgang.Erfolg;
            return erg;
        }

        // ==================================================================
        // Die gemeinsamen Schritte 1 bis 5
        // ==================================================================

        /// <summary>
        /// Erkennen, Optionen, Lesen, Pruefen, Protokoll. Liefert das Pruefergebnis
        /// oder <c>null</c>; im zweiten Fall traegt <paramref name="erg"/> bereits
        /// den Ausgang.
        /// </summary>
        private static async Task<GanglinienPruefErgebnis> Lauf(
            string pfad, GanglinienRaster rasterVorgabe,
            GanglinienImportRueckrufe rueckrufe, GanglinienImportErgebnis erg)
        {
            // --- 1) Format erkennen -----------------------------------------
            GanglinienVorschau vorschau = GanglinienDatei.Erkenne(pfad);

            if (vorschau == null || !vorschau.Lesbar)
            {
                if (vorschau != null) erg.Protokoll.AddRange(vorschau.Meldungen);
                erg.Ausgang = ImportAusgang.Fehler;

                // Der Vorlaeufer zeigte hier das Protokoll mit (false, true) und
                // stieg danach aus — der Rueckgabewert wurde nicht ausgewertet.
                if (rueckrufe != null && rueckrufe.Protokoll != null)
                    await rueckrufe.Protokoll(erg.Protokoll, false, true);
                return null;
            }

            // Das Raster der Auswahlliste uebersteuert die Erkennung (Vorlaeufer :149).
            if (rasterVorgabe != GanglinienRaster.Unbekannt)
                vorschau.Vorschlag.Raster = rasterVorgabe;

            // --- 2) Optionen bestaetigen oder uebersteuern -------------------
            if (rueckrufe == null || rueckrufe.Optionen == null)
            {
                erg.Ausgang = ImportAusgang.Abgebrochen;
                return null;
            }

            GanglinienImportOptionen optionen = await rueckrufe.Optionen(pfad, vorschau);
            if (optionen == null)
            {
                erg.Ausgang = ImportAusgang.Abgebrochen;
                return null;
            }

            // --- 3) lesen und 4) pruefen ------------------------------------
            GanglinienRohdaten roh = GanglinienDatei.Lies(pfad, optionen);
            GanglinienPruefErgebnis ergebnis = null;
            if (roh.Erfolgreich)
            {
                ergebnis = GanglinienPruefung.Pruefe(new GanglinienPruefEingang
                {
                    Rohwerte = roh.Werte,
                    Zeitstempel = roh.Zeitstempel,
                    Einheit = optionen.Einheit,
                    DeklariertesRaster = optionen.Raster,
                    Konvention = optionen.Konvention
                });
            }

            // --- 5) Protokoll zusammenfuehren und vorlegen -------------------
            erg.Protokoll.AddRange(roh.Meldungen);
            if (ergebnis != null) erg.Protokoll.AddRange(ergebnis.Protokoll);

            bool moeglich = roh.Erfolgreich && ergebnis != null && ergebnis.Erfolgreich;
            bool bestaetigen = !moeglich || ergebnis == null || ergebnis.BestaetigungNoetig;

            // Ein sauberer Lauf wird gar nicht erst vorgelegt — dieselbe Regel wie
            // Form_GanglinieProtokoll.Zeigen (:93).
            if (moeglich && !bestaetigen) return ergebnis;

            if (rueckrufe.Protokoll == null)
            {
                erg.Ausgang = moeglich ? ImportAusgang.Abgebrochen : ImportAusgang.Fehler;
                return null;
            }

            bool weiter = await rueckrufe.Protokoll(erg.Protokoll, moeglich, bestaetigen);
            if (!weiter || !moeglich)
            {
                erg.Ausgang = moeglich ? ImportAusgang.Abgebrochen : ImportAusgang.Fehler;
                return null;
            }

            return ergebnis;
        }
    }
}
