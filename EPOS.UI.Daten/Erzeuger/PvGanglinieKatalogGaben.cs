using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Datenseite des Dialogs „Photovoltaik Ganglinie"</b> (PVG, Schemaschritt 206) — plattformfrei:
    /// Katalog samt Kennzahlen (<see cref="ZeitreihenKatalogCtrl"/>, Ausprägung <see cref="Zeitreihenart.PvGanglinie"/>),
    /// Verwendung in Projekten, Löschen mit Projektzuordnungssperre, Schloss und Import
    /// (<see cref="PvGanglinieImportCtrl"/>); dazu der Projektbetrieb: Zuordnungen lesen, aufnehmen, entfernen und
    /// beim OK schreiben (<see cref="PvGanglinieStammCtrl.ZuordnungenSchreiben"/>).
    ///
    /// <para><b>Was die Schale beisteuert</b>, kommt über die Naht <see cref="Katalogwege.PvGanglinienDatei"/>
    /// (Dateiwahl, Ablage, Anzeigen); ohne sie lehnt „Import…" benannt ab (<c>PVG_IMP_NICHT_VERFUEGBAR</c>).</para>
    /// </summary>
    internal static class PvGanglinieKatalogGaben
    {
        /// <summary>Die Wege der Katalogseite.</summary>
        internal static GanglinienKatalogwege Wege()
        {
            GanglinienDateiwege datei = Katalogwege.PvGanglinienDatei?.Invoke();
            return new GanglinienKatalogwege
            {
                Katalogzeilen = () => Task.FromResult(
                    ZeitreihenKatalogCtrl.Katalogfilterzeilen(Zeitreihenart.PvGanglinie)),
                Verwendung = () => ZeitreihenAdminWege.Verwendung(Zeitreihenart.PvGanglinie),
                HatProjektzuordnung = n => Task.FromResult(PvGanglinieStammCtrl.HatProjektzuordnung(n)),
                Loeschen = n => Task.FromResult(PvGanglinieStammCtrl.Loeschen(n)),
                Schloss = Schlosswege.Aus((ids, gesperrt) =>
                    ZeitreihenKatalogCtrl.SchlossSetzen(Zeitreihenart.PvGanglinie, ids, gesperrt)),
                DateiWaehlen = datei?.DateiWaehlen,
                Ablegen = datei?.Ablegen,
                MitSystemOeffnen = datei?.MitSystemOeffnen,
                Ordner = datei?.Ordner ?? "",
                Einlesen = datei is null ? null : Einlesen,
                // Die Nennleistung wird beim Import abgefragt (Vorbelegung aus Dateikopf oder Spitze),
                // gegen die Reihe geprueft und am Katalogsatz gehalten (Nennleistung_kWp).
                NennleistungVorschlagen = datei is null ? null : Vorschlagen,
                NennleistungPruefen = PvGanglinieImportCtrl.Pruefhinweis,
                EinlesenMitNennleistung = datei is null ? null : EinlesenMitNennleistung,
                ImportAbgelehnt = datei is null ? MyResource.Resource.PVG_IMP_NICHT_VERFUEGBAR : "",
                // Die Nennleistung eines Katalogsatzes nachtraeglich: auf jeder Plattform (keine Datei noetig).
                NennleistungSchreiben = (n, kwp) => Task.FromResult(NennleistungSchreiben(n, kwp)),
                NennleistungPruefenFuer = PvGanglinieStammCtrl.Pruefhinweis
            };
        }

        /// <summary>
        /// Schreibt die Nennleistung [kWp] an den Katalogsatz (Kern: <see cref="PvGanglinieStammCtrl.NennleistungSetzen"/>)
        /// und übersetzt den Ausgang in die Texte der Katalogseite — samt Prüfhinweis nach dem Speichern.
        /// </summary>
        internal static GanglinienNennleistungsschrieb NennleistungSchreiben(string bezeichner, double? nennleistungKwp)
        {
            switch (PvGanglinieStammCtrl.NennleistungSetzen(bezeichner, nennleistungKwp))
            {
                case PvNennleistungSchreibergebnis.Geschrieben:
                    return new GanglinienNennleistungsschrieb(true,
                        string.Format(System.Globalization.CultureInfo.CurrentCulture,
                                      MyResource.Resource.PVG_MSG_NENN_GESPEICHERT, bezeichner),
                        PvGanglinieStammCtrl.Pruefhinweis(bezeichner, nennleistungKwp));
                case PvNennleistungSchreibergebnis.Schreibgeschuetzt:
                    return new GanglinienNennleistungsschrieb(false, MyResource.Resource.PVG_MSG_NENN_SCHREIBGESCHUETZT, "");
                case PvNennleistungSchreibergebnis.Unbekannt:
                    return new GanglinienNennleistungsschrieb(false, MyResource.Resource.PVG_MSG_NENN_UNBEKANNT, "");
                case PvNennleistungSchreibergebnis.Ungueltig:
                    return new GanglinienNennleistungsschrieb(false, MyResource.Resource.PVG_MSG_NENN_UNGUELTIG, "");
                default:
                    return new GanglinienNennleistungsschrieb(false, MyResource.Resource.PVG_MSG_SCHREIBFEHLER, "");
            }
        }

        /// <summary>
        /// Der Satz „weicht vom Katalog ab: Nennleistung, Reihe" zur Projektkopie eines Namens (Kern:
        /// <see cref="PvGanglinieStammCtrl.AbweichungZumKatalog"/>); "" bei Gleichheit, ohne Kopie oder ohne Katalogsatz.
        /// </summary>
        internal static string AbweichungText(int projektId, string bezeichner)
        {
            PvKatalogabweichung a = PvGanglinieStammCtrl.AbweichungZumKatalog(projektId, bezeichner);
            if (!a.Weicht) return "";
            var teile = new List<string>();
            if (a.Nennleistung) teile.Add(MyResource.Resource.PVG_ABW_NENNLEISTUNG);
            if (a.Raster) teile.Add(MyResource.Resource.PVG_ABW_RASTER);
            if (a.Jahressumme) teile.Add(MyResource.Resource.PVG_ABW_JAHRESSUMME);
            if (a.Reihe) teile.Add(MyResource.Resource.PVG_ABW_REIHE);
            return string.Format(System.Globalization.CultureInfo.CurrentCulture,
                                 MyResource.Resource.PVG_ABW_TEXT, string.Join(", ", teile));
        }

        /// <summary>
        /// Erneuert die Projektkopie eines Namens aus dem Katalog (Kern: <see cref="PvGanglinieStammCtrl.AusKatalogErneuern"/>)
        /// und übersetzt den Ausgang in den Satz des Dialogs.
        /// </summary>
        internal static PvGanglinieErneuerung Erneuern(int projektId, string bezeichner)
        {
            switch (PvGanglinieStammCtrl.AusKatalogErneuern(projektId, bezeichner))
            {
                case PvKatalogerneuerung.Erneuert:
                    return new PvGanglinieErneuerung(true, string.Format(System.Globalization.CultureInfo.CurrentCulture,
                                                                         MyResource.Resource.PVG_MSG_ERNEUERT, bezeichner));
                case PvKatalogerneuerung.KeinKatalogsatz:
                    return new PvGanglinieErneuerung(false, MyResource.Resource.PVG_MSG_ERNEUERN_KEIN_KATALOG);
                case PvKatalogerneuerung.KeineProjektkopie:
                    return new PvGanglinieErneuerung(false, MyResource.Resource.PVG_MSG_ERNEUERN_KEINE_KOPIE);
                case PvKatalogerneuerung.Gleich:
                    return new PvGanglinieErneuerung(false, MyResource.Resource.PVG_MSG_ERNEUERN_GLEICH);
                default:
                    return new PvGanglinieErneuerung(false, MyResource.Resource.PVG_MSG_SCHREIBFEHLER);
            }
        }

        /// <summary>Die Spalten der Liste — mit oder ohne „im Projekt verwendet".</summary>
        internal static Katalogfilterprofil Profil(bool mitVerwendung)
        {
            Katalogfilterprofil p = Katalogfilterprofil.FuerZeitreihe(Zeitreihenart.PvGanglinie, Katalogtexte.Fuer);
            return mitVerwendung ? p.MitVerwendungsspalte(Katalogtexte.Fuer) : p;
        }

        /// <summary>Die Ganglinie der Detailzeile (DZ1): Kennzahlen, Bild, Farbe, Einheit.</summary>
        internal static void Grafik(Dictionary<string, object> g)
        {
            foreach (KeyValuePair<string, object> e in GanglinienGrafikGaben.Gaben(GanglinienQuelle.PvGanglinie,
                         MyResource.Resource.CHART_TITEL_PVERTRAG_JAHRESGANGLINIE, MyResource.Resource.CHART_ACHSE_PVERTRAG,
                         WindowsFormsApplication1.Zeichnung.Farbrolle.STROM_PV))
                g[e.Key] = e.Value;
        }

        /// <summary>Die gemeinsamen Texte beider Betriebsarten.</summary>
        private static void Texte(IDictionary<string, object> g)
        {
            g["LabelName"] = Katalogtexte.Fuer("HZK_LBL_NAME");
            g["LabelBeschreibung"] = Katalogtexte.Fuer("SGL_LBL_BESCHREIBUNG");
            g["SpalteWahl"] = MyResource.Resource.KFAK_SP_WAHL;
            g["HinweisText"] = MyResource.Resource.PVG_HINWEIS_WEICHE;
        }

        /// <summary>
        /// Der Parametersatz des Dialogs OHNE Projekt — der Weg des Administrationsmenüs
        /// („Profile &amp; Lastgänge → PV-Ganglinie").
        /// </summary>
        internal static IReadOnlyDictionary<string, object> KatalogGaben()
        {
            var g = new Dictionary<string, object>
            {
                ["Katalogbetrieb"] = true,
                ["Katalogwege"] = Wege(),
                ["Katalogprofil"] = Profil(false),
                ["TitelText"] = MyResource.Resource.PVG_TITEL_KATALOG,
                ["HilfeSchluessel"] = "Form_PvGanglinie_Admin.btn_Help"
            };
            Texte(g);
            Grafik(g);
            return g;
        }

        /// <summary>
        /// Der Parametersatz des Dialogs MIT Projekt: die Zuordnungen als geteilte Liste <paramref name="liste"/>
        /// (sie gehört dem Aufrufer und wird erst beim OK über <see cref="Speichern"/> geschrieben), Aufnehmen und
        /// Entfernen arbeiten nur an ihr.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> ProjektGaben(int projektId, List<PvGanglinieZuordnung> liste)
        {
            var zeilen = new List<ErzeugerZeile>();
            var zuModell = new Dictionary<int, PvGanglinieZuordnung>();
            int naechster = 100000;   // vorläufige Schlüssel neuer Zeilen; die echte ID entsteht beim Schreiben
            foreach (PvGanglinieZuordnung z in liste)
            {
                int schluessel = z.Id > 0 ? z.Id : naechster++;
                if (schluessel >= naechster) naechster = schluessel + 1;
                zuModell[schluessel] = z;
                zeilen.Add(new ErzeugerZeile { Schluessel = schluessel, Bezeichner = z.Bezeichner, GeraetId = z.IdGanglinie });
            }

            var g = new Dictionary<string, object>
            {
                ["Zeilen"] = zeilen,
                ["Katalogwege"] = Wege(),
                ["Katalogprofil"] = Profil(true),
                ["Aufnehmen"] = new Func<int, ErzeugerZeile>(stammId =>
                {
                    string bez = PvGanglinieStammCtrl.BezeichnerZu(stammId);
                    if (string.IsNullOrEmpty(bez)) return null;
                    var z = new PvGanglinieZuordnung { Id = 0, IdGanglinie = stammId, Bezeichner = bez };
                    int schluessel = naechster++;
                    liste.Add(z);
                    zuModell[schluessel] = z;
                    return new ErzeugerZeile { Schluessel = schluessel, Bezeichner = bez, GeraetId = stammId };
                }),
                // Projektkopie und Katalog (E113): die Abweichung je Name und das Erneuern der Kopie.
                ["KatalogAbweichung"] = new Func<string, string>(n => AbweichungText(projektId, n)),
                // Eine neu aufgenommene Zuordnung (ID 0, noch ohne OK) hat noch keine Projektkopie: „noch nicht gespeichert".
                ["Ungespeichert"] = new Func<ErzeugerZeile, bool>(zeile =>
                    zuModell.TryGetValue(zeile.Schluessel, out PvGanglinieZuordnung z) && z.Id <= 0),
                ["AusKatalogErneuern"] = new Func<string, Task<PvGanglinieErneuerung>>(n =>
                    Task.FromResult(Erneuern(projektId, n))),
                ["Entfernen"] = new Action<ErzeugerZeile>(zeile =>
                {
                    if (!zuModell.TryGetValue(zeile.Schluessel, out PvGanglinieZuordnung z)) return;
                    liste.Remove(z);
                    zuModell.Remove(zeile.Schluessel);
                }),
                ["TitelText"] = MyResource.Resource.PVG_TITEL,
                ["LabelProjektliste"] = MyResource.Resource.PVG_LBL_PROJEKTLISTE,
                ["LabelKatalogliste"] = MyResource.Resource.PVG_LBL_KATALOGLISTE,
                ["SpalteName"] = Katalogtexte.Fuer("BHKWV_SP_NAME"),
                ["LabelHinzu"] = Katalogtexte.Fuer("HZK_TIP_HINZU"),
                ["LabelEntfernen"] = Katalogtexte.Fuer("HZK_TIP_ENTFERNEN"),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN
            };
            Texte(g);
            Grafik(g);
            return g;
        }

        /// <summary>Die Zuordnungen eines Projekts — der Arbeitsstand, den <see cref="ProjektGaben"/> teilt.</summary>
        internal static List<PvGanglinieZuordnung> Lesen(int projektId) => PvGanglinieStammCtrl.Zuordnungen(projektId);

        /// <summary>Der OK-Weg: schreibt die Zuordnungen des Projekts in EINER Transaktion.</summary>
        internal static bool Speichern(int projektId, IEnumerable<PvGanglinieZuordnung> liste)
        {
            var namen = new List<string>();
            foreach (PvGanglinieZuordnung z in liste) namen.Add(z.Bezeichner);
            return PvGanglinieStammCtrl.ZuordnungenSchreiben(projektId, namen);
        }

        /// <summary>
        /// Die Importkette im Hintergrund (Kulturweitergabe): lesen mit Formaterkennung, Namen prüfen, schreiben
        /// in einer Transaktion — alles im Kern; das Raster der Datei bleibt.
        /// </summary>
        internal static Task<GanglinienKatalogimport> Einlesen(string pfad, IProgress<ImportFortschritt> melder)
            => EinlesenMitNennleistung(pfad, null, melder);

        /// <summary>Die Vorbelegung der Nennleistung aus der gewählten Datei (Kern: <c>PvGanglinieImportCtrl.Vorschlagen</c>).</summary>
        internal static async Task<GanglinienNennleistungsvorschlag> Vorschlagen(string pfad)
        {
            PvGanglinieVorschlag v = await Importfang.Starten(pfad, () => PvGanglinieImportCtrl.Vorschlagen(pfad),
                                                              _ => new PvGanglinieVorschlag());
            return new GanglinienNennleistungsvorschlag(v.VorschlagKwp, v.AusDateikopf, v.SpitzeKw);
        }

        /// <summary>Der Import samt abgefragter Nennleistung [kWp]; <c>null</c> = nicht bekannt.</summary>
        internal static async Task<GanglinienKatalogimport> EinlesenMitNennleistung(string pfad, double? nennleistungKwp,
                                                                                    IProgress<ImportFortschritt> melder)
        {
            melder?.Report(new ImportFortschritt(null, "IMP_KAT_PROT_LESEN"));
            PvGanglinieImportBericht b = await Importfang.Starten(pfad,
                () => PvGanglinieImportCtrl.Einlesen(pfad, nennleistungKwp),
                a => new PvGanglinieImportBericht { IstFehler = true, Meldung = a.Text,
                                                    Bezeichner = System.IO.Path.GetFileNameWithoutExtension(pfad ?? "") });
            return new GanglinienKatalogimport(b.Erfolgreich, b.IstFehler, b.Bezeichner ?? "",
                                               b.Meldung ?? "", b.Protokoll ?? "");
        }
    }
}
