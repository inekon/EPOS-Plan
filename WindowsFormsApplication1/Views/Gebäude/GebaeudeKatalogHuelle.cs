using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Bedarf;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE des Gebäude-KATALOGEDITORS (iU9-W9.1) — der Ersatz für
    /// <c>Form_Gebaeude1</c> UND <c>Form_Gebaeude2</c>.
    ///
    /// <para><b>Zwei Masken, ein Satz.</b> Die zweite Maske bekam mit
    /// <c>frm.model = model</c> DASSELBE <see cref="GebaeudeModel"/> in die Hand; sie war
    /// nie ein eigener Datensatz. In der Razor-Fassung sind es zwei Reiter auf einem
    /// Feldsatz.</para>
    ///
    /// <para><b>Die Ableitungen des Vorläufers stehen hier</b>, nicht in der Komponente:
    /// <c>Bewohner</c>, <c>gesamte_Fensterflaeche</c> und
    /// <c>Wohnflaeche</c> entstehen beim Schreiben (<c>InitModelFromControls</c>:174-215),
    /// und die drei Kennzahlen, die keine Maske je anfasst
    /// (<c>spez_Waermeverbrauch</c>, <c>Waermebedarf</c>, <c>ID</c>), bleiben aus dem
    /// geladenen Satz erhalten — der Vorläufer schrieb ebenfalls das GELADENE Modell
    /// zurück und nicht ein frisches.</para>
    ///
    /// <para><b>Die <c>Bauweise</c> steht seit dem Entscheid des Anwenders vom
    /// 04.09.2026 NICHT mehr hier</b> (W9‑O‑2 zu Befund W9‑B6). Sie hing am Index der
    /// GEBÄUDEART-Klappliste; jetzt bestimmt sie die BAUART-Klappliste, und die bedient
    /// der Dialog. Die Hülle reicht die Größe nur noch durch:
    /// <c>AusModell</c> gibt sie heraus, <c>NachModell</c> nimmt sie entgegen.</para>
    /// </summary>
    internal static class GebaeudeKatalogHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 707 × 651 und 607 × 591).</summary>
        private static readonly Size MASS = new Size(1000, 760);

        // =================================================================================
        // Einstiege
        // =================================================================================

        /// <summary>„DB ändern" — ein vorhandener Katalogsatz.</summary>
        internal static void Bearbeiten(IWin32Window besitzer, string bezeichner)
            => Oeffnen(besitzer, bezeichner, GebaeudeKatalogModus.Bearbeiten);

        /// <summary>„DB neu" — ein neuer Katalogsatz; der Name wird im Dialog eingegeben.</summary>
        internal static void Neu(IWin32Window besitzer)
            => Oeffnen(besitzer, "", GebaeudeKatalogModus.Neu);

        /// <summary>
        /// Katalogverwaltung: die Namensklappliste führt ALLE Sätze, „Speichern" ist
        /// gesperrt.
        ///
        /// <para><b>Befund W9‑B10:</b> Dieser Modus (<c>Form_Gebaeude1.m_bAdmin</c>) hatte
        /// im ganzen Bestand KEINEN Aufrufer — er war ausgeschriebener, aber unerreichbarer
        /// Code. Er ist übernommen, weil er vollständig ausformuliert dastand; erreichbar
        /// wird er erst, wenn ihn jemand aufruft.</para>
        /// </summary>
        internal static void Katalogverwaltung(IWin32Window besitzer)
            => Oeffnen(besitzer, "", GebaeudeKatalogModus.Admin);

        private static void Oeffnen(IWin32Window besitzer, string bezeichner,
                                    GebaeudeKatalogModus modus)
        {
            BlazorDialogForm<GebaeudeKatalogDialog> dlg = null;

            var werte = new Dictionary<string, object>(Gaben(besitzer, bezeichner, modus))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(),
                    _ => { if (dlg != null) dlg.Schliessen(true); })
            };

            dlg = new BlazorDialogForm<GebaeudeKatalogDialog>(Titel(), MASS, werte);
            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
        }

        // =================================================================================
        // Der Parametersatz
        // =================================================================================

        /// <summary>
        /// Der PARAMETERSATZ des Dialogs — ohne <c>Geschlossen</c>, damit ihn ab W9.2 auch
        /// die Überlagerung in <c>GebaeudeDialog</c> nehmen kann.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
            IWin32Window besitzer, string bezeichner, GebaeudeKatalogModus modus)
        {
            // Der geladene Satz bleibt in der Huelle stehen: Er traegt die Felder, die
            // keine der beiden Masken je anfasst (ID, spez_Waermeverbrauch, Waermebedarf).
            GebaeudeModel geladen = modus == GebaeudeKatalogModus.Neu
                ? new GebaeudeModel()
                : Laden(bezeichner) ?? new GebaeudeModel();

            // Die Brauchwasser-Zuordnungen des laufenden Projekts. Sie werden erst beim
            // Oeffnen der Ueberlagerung gelesen und bei OK zurueckgeschrieben.
            var brauchwasser = new List<Z_ProjektBrauchwasserModel>();

            var werte = new Dictionary<string, object>
            {
                ["Daten"] = AusModell(geladen),
                ["Modus"] = modus,

                ["Gebaeudetypen"] = new Func<IReadOnlyList<string>>(
                    () => GebaeudeStammCtrl.Gebaeudetypen()),
                ["Gebaeudearten"] = new Func<IReadOnlyList<string>>(
                    () => GebaeudeStammCtrl.Gebaeudearten(null)),
                ["Baualtersklassen"] = GebaeudeStammCtrl.Baualtersklassen(),
                ["Katalognamen"] = new Func<IReadOnlyList<string>>(
                    () => GebaeudeStammCtrl.Katalognamen()),
                ["Lies"] = new Func<string, GebaeudeKatalogDaten>(
                    n => { GebaeudeModel m = Laden(n); return m == null ? null : AusModell(m); }),
                ["Speichern"] = new Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis>(
                    (d, istNeu, bez) => Schreiben(d, istNeu, bez)),

                // iU9-W9.5: "Brauchwasser..." auf dem zweiten Reiter zeigt die
                // Brauchwasser-Profilliste des LAUFENDEN Projekts als Ueberlagerung.
                // Der Vorlaeufer holte sich Projekt-Id und -Name ueber Program.startfrm;
                // hier kommt beides aus Dienste.Projekt.
                ["BrauchwasserGaben"] = new Func<IReadOnlyDictionary<string, object>>(
                    () => BrauchwasserGaben(besitzer, brauchwasser)),
                ["BrauchwasserFertig"] = new Action<bool>(
                    ok => BrauchwasserSchreiben(ok, brauchwasser)),

                ["TitelText"] = Titel(),
                ["ReiterFlaechen"] = Text_("GEBK_REITER_FLAECHEN", "Flächen und U-Werte"),
                // GEB2_TITEL steht seit H11 im Ressourcenkatalog und war der Titel der
                // zweiten Maske - er wird jetzt der Titel des zweiten Reiters.
                ["ReiterTemperaturen"] = MyResource.Resource.GEB2_TITEL,

                ["GruppeKopf"] = Text_("GEBK_GRP_KENNGROESSEN", "Kenngrößen"),
                ["GruppeFlaechen"] = Text_("GEBK_GRP_FLAECHEN", "Flächen [m²]"),
                ["GruppeUWerte"] = Text_("GEBK_GRP_UWERTE", "U-Werte [W/m²K]"),
                ["GruppeRaumtemperaturen"] = Text_("GEBK_GRP_RAUMTEMPERATUREN", "Raumtemperaturen"),
                ["GruppeWaermebruecken"] = Text_("GEBK_GRP_WAERMEBRUECKEN",
                    "Wärmebrückenverlustkoeffizienten [W/(mK)]"),
                ["GruppeAnschlussmasse"] = Text_("GEBK_GRP_ANSCHLUSS", "Abmessung Anschluß [m]"),
                ["GruppeFerienAnfang"] = Text_("GEBK_GRP_FERIEN_ANFANG", "Ferien Anfang"),
                ["GruppeFerienEnde"] = Text_("GEBK_GRP_FERIEN_ENDE", "Ferien Ende"),
                ["GruppeSonstiges"] = Text_("GEBK_GRP_SONSTIGES", "Sonstiges"),

                ["LabelName"] = Text_("GEBK_LBL_NAME", "Name :"),
                ["LabelGebaeudetyp"] = Text_("GEBK_LBL_GEBAEUDETYP", "Gebäudetyp :"),
                ["LabelBeschreibung"] = Text_("GEBK_LBL_BESCHREIBUNG", "Beschreibung :"),
                ["LabelGebaeudeart"] = Text_("GEBK_LBL_GEBAEUDEART", "Gebäudeart :"),
                ["LabelBaujahr"] = Text_("GEBK_LBL_BAUJAHR", "Baujahr :"),
                ["LabelVerwendung"] = Text_("GEBK_LBL_VERWENDUNG", "Verwendung :"),
                ["LabelBauart"] = Text_("GEBK_LBL_BAUART", "Bauart :"),
                ["LabelWohnflaeche"] = Text_("GEBK_LBL_WOHNFLAECHE", "Wohn-/Nutzfläche :"),
                ["LabelFlaecheNutzer"] = Text_("GEBK_LBL_FLAECHE_NUTZER", "Fläche / Nutzer :"),
                ["LabelWaermegewinne"] = Text_("GEBK_LBL_WAERMEGEWINNE", "Interne Wärmegewinne :"),
                ["LabelFensterdurchlassgrad"] =
                    Text_("GEBK_LBL_FENSTERDURCHLASS", "Fensterdurchlaßgrad :"),
                ["HinweisFensterdurchlassgrad"] = Text_("GEBK_HINWEIS_FENSTERDURCHLASS", "(z.B. 0,4)"),
                ["LabelRaumhoehe"] = Text_("GEBK_LBL_RAUMHOEHE", "Raumhöhe :"),

                ["LabelFFNord"] = Text_("GEBK_LBL_FF_NORD", "Fensterfläche Nord :"),
                ["LabelFFSued"] = Text_("GEBK_LBL_FF_SUED", "Fensterfläche Süd :"),
                ["LabelFFOstWest"] = Text_("GEBK_LBL_FF_OSTWEST", "Fensterfläche Ost + West :"),
                ["LabelFlaecheAussenwand"] = Text_("GEBK_LBL_FL_AUSSENWAND", "Fläche Außenwand :"),
                ["LabelDachflaeche"] = Text_("GEBK_LBL_DACHFLAECHE", "Gebäude Dachfläche :"),
                ["LabelGrundflaeche"] = Text_("GEBK_LBL_GRUNDFLAECHE", "Gebäude Grundfläche :"),
                ["LabelSonstigeFlaechen"] = Text_("GEBK_LBL_SONST_FLAECHEN", "sonstige Flächen :"),

                ["LabelUAussenwand"] = Text_("GEBK_LBL_U_AUSSENWAND", "Außenwand :"),
                ["LabelUFenster"] = Text_("GEBK_LBL_U_FENSTER", "Fenster :"),
                ["LabelUDachflaeche"] = Text_("GEBK_LBL_U_DACHFLAECHE", "Dachfläche :"),
                ["LabelUGrundflaeche"] = Text_("GEBK_LBL_U_GRUNDFLAECHE", "Grundfläche :"),
                ["LabelUSonstiges"] = Text_("GEBK_LBL_U_SONSTIGES", "Sonstiges :"),

                ["LabelSollTag"] = Text_("GEBK_LBL_SOLL_TAG", "Soll am Tag :"),
                ["LabelNachtAbsenkung"] = Text_("GEBK_LBL_NACHTABSENKUNG", "Nachtabsenkung auf :"),
                ["LabelMaxTemperatur"] = Text_("GEBK_LBL_MAXTEMPERATUR", "Maximalraumtemperatur :"),
                ["LabelWEAbsenkung"] = Text_("GEBK_LBL_WE_ABSENKUNG", "Wochenendabsenkung :"),
                ["LabelSollFerien"] = Text_("GEBK_LBL_SOLL_FERIEN", "Soll in Ferien :"),
                ["LabelWbvkFenster"] = Text_("GEBK_LBL_FENSTER_WAND", "Fenster-Wand :"),
                ["LabelWbvkKeller"] = Text_("GEBK_LBL_AUSSENWAND_KELLER", "Außenwand-Keller :"),
                ["LabelWbvkDach"] = Text_("GEBK_LBL_WAND_DACH", "Wand-Dach :"),
                ["LabelLuftwechsel"] = Text_("GEBK_LBL_LUFTWECHSEL", "Luftwechselrate :"),
                ["LabelTag"] = Text_("GEBK_LBL_TAG", "Tag :"),
                ["LabelMonat"] = Text_("GEBK_LBL_MONAT", "Monat :"),
                ["LabelBrauchwasserprofile"] =
                    Text_("GEBK_LBL_BRAUCHWASSERPROFILE", "Brauchwasserprofile :"),

                ["Ferienzeitraeume"] = Ferienzeitraeume(),
                ["Bauarten"] = Bauarten(),
                ["Verwendungen"] = Verwendungen(),
                ["Verwendungswerte"] = VERWENDUNGSWERTE,

                ["BtnUeberschreibenText"] = Text_("GEBK_BTN_UEBERSCHREIBEN", "Überschreiben"),
                ["BtnSpeichernUnterText"] = Text_("GEBK_BTN_SPEICHERN_UNTER", "Speichern unter"),
                ["BtnSpeichernText"] = Text_("GEBK_BTN_SPEICHERN", "Speichern"),
                ["BtnBeendenText"] = Text_("GEBK_BTN_BEENDEN", "Beenden"),
                ["BtnUebernehmenText"] = Text_("GEBK_BTN_UEBERNEHMEN", "Werte übernehmen"),
                ["BtnBrauchwasserText"] = Text_("GEBK_BTN_BRAUCHWASSER", "Brauchwasser..."),

                ["MeldungZahlFehlt"] = Text_("GEBK_MSG_ZAHL", "Bitte {0} als Zahl eingeben."),
                ["MeldungNameFehlt"] = Text_("GEBK_MSG_NAME_LEER", "Gebäudenamen eingeben!"),
                ["MeldungGespeichert"] = Text_("GEBK_MSG_GESPEICHERT", "Gebäude ist gespeichert!"),
                ["MeldungUeberschrieben"] =
                    Text_("GEBK_MSG_UEBERSCHRIEBEN", "Gebäude Datensatz ist überschrieben!"),
                ["MeldungUebernommen"] = Text_("GEBK_MSG_UEBERNOMMEN", "Werte übernommen."),
                ["MeldungFerienWinter"] = Text_(Ferienzeit.MELDUNG_WINTER,
                    "Die Ferien müssen über die Jahresgrenze gehen!"),
                ["MeldungFerienOstern"] = Text_(Ferienzeit.MELDUNG_OSTERN,
                    "Fehler: Bei der Eingabe der Osterferien!"),
                ["MeldungFerienSommer"] = Text_(Ferienzeit.MELDUNG_SOMMER,
                    "Fehler: Bei der Eingabe der Sommerferien!"),
                ["MeldungFerienHerbst"] = Text_(Ferienzeit.MELDUNG_HERBST,
                    "Fehler: Bei der Eingabe der Herbstferien!"),

                ["FeldWohnflaeche"] = Text_("GEBK_FELD_WOHNFLAECHE", "Wohn-/Nutzfläche"),
                ["FeldFlaecheNutzer"] = Text_("GEBK_FELD_FLAECHE_NUTZER", "Fläche / Nutzer"),
                ["FeldWaermegewinne"] = Text_("GEBK_FELD_WAERMEGEWINNE", "Interne Wärmegewinne"),
                ["FeldFensterdurchlassgrad"] =
                    Text_("GEBK_FELD_FENSTERDURCHLASS", "Fensterdurchlaßgrad"),
                ["FeldRaumhoehe"] = Text_("GEBK_FELD_RAUMHOEHE", "Raumhöhe"),
                ["FeldFFSued"] = Text_("GEBK_FELD_FF_SUED", "Fensterfläche Süd"),
                ["FeldFFOstWest"] = Text_("GEBK_FELD_FF_OSTWEST", "Fensterfläche Ost + West"),
                ["FeldFFNord"] = Text_("GEBK_FELD_FF_NORD", "Fensterfläche Nord"),
                ["FeldFlaecheAussenwand"] = Text_("GEBK_FELD_FL_AUSSENWAND", "Fläche Außenwand"),
                ["FeldDachflaeche"] = Text_("GEBK_FELD_DACHFLAECHE", "Gebäude Dachfläche"),
                ["FeldGrundflaeche"] = Text_("GEBK_FELD_GRUNDFLAECHE", "Gebäude Grundfläche"),
                ["FeldSonstigeFlaechen"] = Text_("GEBK_FELD_SONST_FLAECHEN", "sonstige Flächen"),
                ["FeldUAussenwand"] = Text_("GEBK_FELD_U_AUSSENWAND", "U-Wert Außenwand"),
                ["FeldUFenster"] = Text_("GEBK_FELD_U_FENSTER", "U-Wert Fenster"),
                ["FeldUDachflaeche"] = Text_("GEBK_FELD_U_DACHFLAECHE", "U-Wert Dachfläche"),
                ["FeldUGrundflaeche"] = Text_("GEBK_FELD_U_GRUNDFLAECHE", "U-Wert Grundfläche"),
                ["FeldUSonstiges"] = Text_("GEBK_FELD_U_SONSTIGES", "U-Wert Sonstiges"),
                ["LabelAnschlussFenster"] =
                    Text_("GEBK_FELD_ANSCHLUSS_FENSTER", "Anschluß Fenster-Wand"),
                ["LabelAnschlussDach"] = Text_("GEBK_FELD_ANSCHLUSS_DACH", "Anschluß Wand-Dach"),
                ["LabelAnschlussKeller"] =
                    Text_("GEBK_FELD_ANSCHLUSS_KELLER", "Anschluß Außenwand-Keller"),

                ["HilfeSchluessel"] = "Form_Gebaeude1.btn_Help"
            };

            return werte;
        }

        // =================================================================================
        // Datenseite
        // =================================================================================

        /// <summary>
        /// Der Parametersatz der Brauchwasser-Profilliste. Die Zuordnungen des laufenden
        /// Projekts werden hier frisch gelesen — der Vorläufer tat dasselbe beim Klick.
        /// </summary>
        private static IReadOnlyDictionary<string, object> BrauchwasserGaben(
            IWin32Window besitzer, List<Z_ProjektBrauchwasserModel> ziel)
        {
            int projektId = Dienste.Projekt.Id;

            ziel.Clear();
            ziel.AddRange(Z_ProjektBrauchwasserCtrl.LiesProjekt(projektId));

            var zeilen = new List<EPOS.UI.Dialoge.Bedarf.BedarfsProfilZeile>();
            foreach (Z_ProjektBrauchwasserModel m in ziel)
                zeilen.Add(new EPOS.UI.Dialoge.Bedarf.BedarfsProfilZeile
                {
                    IdZ = m.ID_Z, IdStamm = m.ID_Brauchwasser,
                    Name = m.szBezeichner ?? "", Summe = m.Summe
                });

            Action geaendert = () =>
            {
                ziel.Clear();
                foreach (EPOS.UI.Dialoge.Bedarf.BedarfsProfilZeile z in zeilen)
                    ziel.Add(new Z_ProjektBrauchwasserModel
                    {
                        ID_Z = z.IdZ, ID_Projekt = projektId, ID_Brauchwasser = z.IdStamm,
                        szBezeichner = z.Name, Summe = z.Summe
                    });
            };

            return BedarfsProfileHuelle.Gaben(besitzer, BedarfsArt.Brauchwasser, projektId,
                                              zeilen, geaendert, wizard: false);
        }

        /// <summary>
        /// Nach OK wird die Zuordnung geschrieben — Löschen + Neuanlegen samt
        /// Änderungsdatum, wörtlich aus <c>btn_Brauchwasser_Click</c>:246-254.
        /// </summary>
        private static void BrauchwasserSchreiben(bool ok, List<Z_ProjektBrauchwasserModel> liste)
        {
            if (!ok) return;

            int projektId = Dienste.Projekt.Id;
            string projektName = Dienste.Projekt.Name;

            var wizctrl = new WizardCtrl();
            wizctrl.Del_Projekt_Brauchwasser(projektId);
            wizctrl.Add_Projekt_Brauchwasser(projektId, liste);

            var projctrl = new ProjektCtrl();
            projctrl.ReadSingle(projektName);
            projctrl.m_Aenderungsdatum = DateTime.Now;
            projctrl.Update();
        }

        private static GebaeudeModel Laden(string bezeichner)
        {
            if (string.IsNullOrEmpty(bezeichner)) return null;

            GebaeudeStammCtrl ctrl = new GebaeudeStammCtrl();
            ctrl.ReadAll("Bezeichner='" + bezeichner + "'");
            return ctrl.rows > 0 ? ctrl.items[0] : null;
        }

        /// <summary>
        /// Der Schreibweg samt ReadOnly-Sperre. Die Sperre prüft die HÜLLE, nicht der
        /// Controller: <c>Overwrite</c> meldet sie über <c>Meldung.Hinweis</c>, und das
        /// wäre in einer WebView ein modaler Kasten über dem Dialog (Muster W8.1).
        /// </summary>
        private static GebaeudeKatalogErgebnis Schreiben(
            GebaeudeKatalogDaten daten, bool istNeu, string bezeichner)
        {
            GebaeudeStammCtrl ctrl = new GebaeudeStammCtrl();

            if (!istNeu && ctrl.IsReadOnly(bezeichner))
                return new GebaeudeKatalogErgebnis(false, Text_("GEBK_MSG_READONLY",
                    "Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht " +
                    "überschrieben werden."));

            GebaeudeModel vorher = Laden(istNeu ? daten.Name : bezeichner) ?? new GebaeudeModel();
            GebaeudeModel modell = NachModell(daten, vorher);

            // Ueberschreiben trifft den URSPRUNGSNAMEN (WHERE Bezeichner = Gebaeudename).
            if (!istNeu) modell.Gebaeudename = bezeichner;

            bool ok = istNeu ? ctrl.Insert(modell) : ctrl.Overwrite(modell);
            return new GebaeudeKatalogErgebnis(ok,
                ok ? "" : Text_("GEBK_MSG_FEHLER", "Fehler beim Speichern!\nAlle Eingaben überprüfen!"));
        }

        /// <summary>Katalogsatz → Feldsatz (<c>SetControls</c>:46-125 und :31-71).</summary>
        internal static GebaeudeKatalogDaten AusModell(GebaeudeModel m)
        {
            var d = new GebaeudeKatalogDaten
            {
                Name = m.Gebaeudename ?? "",
                Typ = m.Typ ?? "",
                Beschreibung = m.Beschreibung ?? "",
                Gebaeudeart = m.Gebaeudeart ?? "",
                Verwendung = string.IsNullOrEmpty(m.Wohngebaeude_Nicht_Wohngebaeude)
                    ? VERWENDUNGSWERTE[0] : m.Wohngebaeude_Nicht_Wohngebaeude,
                Baualtersklasse = GebaeudeStammCtrl.KlassenIndex(m.Baualtersklasse),
                // W9-O-2: Die Bauart bleibt die ANZEIGE der gespeicherten Bauweise; die
                // Bauweise selbst geht mit, weil der Dialog sie ab jetzt bildet.
                Bauart = GebaeudeStammCtrl.BauartAusBauweise(m.Bauweise, m.Wohnflaeche),
                Bauweise = m.Bauweise,

                WohnflaecheGesamt = m.Wohnflaeche_gesamt,
                FlaecheNutzer = m.Flaeche_Nutzer,
                Waermegewinne = m.Interne_Waermegewinne,
                Fensterdurchlassgrad = m.Fensterdurchlassgrad,
                Raumhoehe = m.Raumhoehe,

                FensterflaecheNord = m.Fensterflaeche_Nord,
                FensterflaecheSued = m.Fensterflaeche_Sued,
                FensterflaecheOstWest = m.Fensterflaeche_Ost,
                FlaecheAussenwand = m.Flaeche_Außenwand,
                Dachflaeche = m.Dachflaeche,
                Grundflaeche = m.Grundflaeche,
                SonstigeFlaechen = m.Sonstige_Flaechen,

                UWertAussenwand = m.k_Wert_Außenwand,
                UWertFenster = m.k_Wert_Fenster,
                UWertDachflaeche = m.k_Wert_Dachflaeche,
                UWertGrundflaeche = m.k_Wert_Grundflaeche,
                UWertSonstiges = m.k_Wert_Sonstiges,

                SollTag = m.Raumsolltemperatur_Tag,
                NachtAbsenkung = m.Raumsolltemperatur_Nachtabsenkung,
                MaxTemperatur = m.Maximaleraumtemperatur,
                WochenendAbsenkung = m.Raumsolltemperatur_Wochenende,
                SollFerien = m.Raumsolltemperatur_Ferien,

                WbvkFensterWand = m.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand,
                WbvkAussenwandKeller = m.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke,
                WbvkWandDach = m.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach,

                AnschlussFensterWand = m.Abmessung_Anschluß_Fenster_Wand,
                AnschlussWandDach = m.Abmessung_Anschluß_Wand_Dach,
                AnschlussAussenwandKeller = m.Abmessung_Anschluß_Außenwand_Kellerdecke,

                Luftwechselrate = m.Luftwechselrate,
                Wochenende = m.Wochenende,
                Ferien = m.Ferien,
                WwBedarf = m.WW_Bedarf,
                SpezWaermeverbrauch = m.spez_Waermeverbrauch,
                Waermebedarf = m.Waermebedarf
            };

            d.Ferienbeginn = new[]
            {
                (int)m.Ferienbeginn_1, (int)m.Ferienbeginn_2,
                (int)m.Ferienbeginn_3, (int)m.Ferienbeginn_4
            };
            d.Ferienende = new[]
            {
                (int)m.Ferienende_1, (int)m.Ferienende_2,
                (int)m.Ferienende_3, (int)m.Ferienende_4
            };
            return d;
        }

        /// <summary>
        /// Feldsatz → Katalogsatz samt der vier Ableitungen aus
        /// <c>InitModelFromControls</c>:174-215. <paramref name="vorher"/> ist der
        /// GELADENE Satz; alles, was keine Maske anfasst, bleibt daraus stehen.
        /// </summary>
        internal static GebaeudeModel NachModell(GebaeudeKatalogDaten d, GebaeudeModel vorher)
        {
            GebaeudeModel m = vorher ?? new GebaeudeModel();

            double wfl = d.WohnflaecheGesamt ?? 0;
            double nutzer = d.FlaecheNutzer ?? 0;

            m.Gebaeudename = d.Name ?? "";
            m.Typ = d.Typ ?? "";
            m.Beschreibung = d.Beschreibung ?? "";

            m.Wohnflaeche_gesamt = wfl;

            // "Flaeche_Nutzer == 0 -> 35" und die Bewohnerzahl daraus (:183-184).
            m.Flaeche_Nutzer = nutzer;
            if (nutzer == 0) { m.Flaeche_Nutzer = 35; nutzer = 35; }
            m.Bewohner = wfl / nutzer;

            m.Interne_Waermegewinne = d.Waermegewinne ?? 0;

            // Entscheid W9-O-2 (Anwender, 04.09.2026) zu Befund W9-B6: Die BAUART
            // bestimmt die Bauweise, nicht mehr die Gebaeudeart. Gebildet wird sie im
            // Dialog (GebaeudeKatalogDialog.BauweiseNachfuehren), hier wird sie nur
            // uebernommen.
            m.Bauweise = d.Bauweise;

            m.Fensterflaeche_Sued = d.FensterflaecheSued ?? 0;
            m.Fensterflaeche_Ost = d.FensterflaecheOstWest ?? 0;
            m.Fensterflaeche_Nord = d.FensterflaecheNord ?? 0;
            m.Fensterdurchlassgrad = d.Fensterdurchlassgrad ?? 0;

            m.k_Wert_Außenwand = d.UWertAussenwand ?? 0;
            m.k_Wert_Fenster = d.UWertFenster ?? 0;
            m.k_Wert_Dachflaeche = d.UWertDachflaeche ?? 0;
            m.k_Wert_Grundflaeche = d.UWertGrundflaeche ?? 0;
            m.k_Wert_Sonstiges = d.UWertSonstiges ?? 0;
            m.Flaeche_Außenwand = d.FlaecheAussenwand ?? 0;
            m.gesamte_Fensterflaeche = m.Fensterflaeche_Sued + m.Fensterflaeche_Ost +
                                       m.Fensterflaeche_Nord;
            m.Dachflaeche = d.Dachflaeche ?? 0;
            m.Grundflaeche = d.Grundflaeche ?? 0;
            m.Sonstige_Flaechen = d.SonstigeFlaechen ?? 0;
            m.Wohnflaeche = wfl;
            m.Raumhoehe = d.Raumhoehe ?? 0;

            m.Baualtersklasse = GebaeudeStammCtrl.KlassenBuchstabe(d.Baualtersklasse).ToString();
            m.Gebaeudeart = d.Gebaeudeart ?? "";
            m.Wohngebaeude_Nicht_Wohngebaeude = d.Verwendung ?? VERWENDUNGSWERTE[0];

            // Reiter 2 - die Ableitungen hat die Komponente beim Uebernehmen gemacht.
            m.Raumsolltemperatur_Tag = d.SollTag ?? 0;
            m.Raumsolltemperatur_Nachtabsenkung = d.NachtAbsenkung ?? 0;
            m.Maximaleraumtemperatur = d.MaxTemperatur ?? 0;
            m.Raumsolltemperatur_Wochenende = d.WochenendAbsenkung ?? 0;
            m.Raumsolltemperatur_Ferien = d.SollFerien ?? 0;
            m.Wochenende = d.Wochenende;
            m.Ferien = d.Ferien;

            m.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand = d.WbvkFensterWand ?? 0;
            m.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke =
                d.WbvkAussenwandKeller ?? 0;
            m.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach = d.WbvkWandDach ?? 0;
            m.Abmessung_Anschluß_Fenster_Wand = d.AnschlussFensterWand ?? 0;
            m.Abmessung_Anschluß_Wand_Dach = d.AnschlussWandDach ?? 0;
            m.Abmessung_Anschluß_Außenwand_Kellerdecke = d.AnschlussAussenwandKeller ?? 0;

            m.Ferienbeginn_1 = d.Ferienbeginn[0];
            m.Ferienbeginn_2 = d.Ferienbeginn[1];
            m.Ferienbeginn_3 = d.Ferienbeginn[2];
            m.Ferienbeginn_4 = d.Ferienbeginn[3];
            m.Ferienende_1 = d.Ferienende[0];
            m.Ferienende_2 = d.Ferienende[1];
            m.Ferienende_3 = d.Ferienende[2];
            m.Ferienende_4 = d.Ferienende[3];

            m.Luftwechselrate = d.Luftwechselrate ?? 0;
            m.WW_Bedarf = d.WwBedarf;
            m.spez_Waermeverbrauch = d.SpezWaermeverbrauch;
            m.Waermebedarf = d.Waermebedarf;

            return m;
        }

        // =================================================================================
        // Texte
        // =================================================================================

        /// <summary>
        /// Die beiden STEUERWERTE der Spalte <c>Wohngebaeude_Nicht_Wohngebaeude</c>. Sie
        /// werden NIE übersetzt — die en-US-Satellitendatei des Vorläufers tat es und
        /// schrieb damit englischen Text in die Datenbank (Befund W9‑B8).
        /// </summary>
        internal static readonly string[] VERWENDUNGSWERTE = { "Wohngebaeude", "Nicht Wohngebaeude" };

        private static string[] Verwendungen()
        {
            return new[]
            {
                Text_("GEBK_VERWENDUNG_WOHN", "Wohngebäude"),
                Text_("GEBK_VERWENDUNG_NICHTWOHN", "Nicht Wohngebäude")
            };
        }

        private static string[] Bauarten()
        {
            return new[]
            {
                Text_("GEBK_BAUART_LEICHT", "Leichte Bauart"),
                Text_("GEBK_BAUART_SCHWER", "Schwere Bauart"),
                Text_("GEBK_BAUART_SEHRSCHWER", "Sehr schwere Bauart")
            };
        }

        private static string[] Ferienzeitraeume()
        {
            return new[]
            {
                Text_("GEBK_FERIEN_WINTER", "Winter :"),
                Text_("GEBK_FERIEN_OSTERN", "Ostern :"),
                Text_("GEBK_FERIEN_SOMMER", "Sommer :"),
                Text_("GEBK_FERIEN_HERBST", "Herbst :")
            };
        }

        private static string Titel()
        {
            // Der Designer schreibt "Flaeschen" - ein Tippfehler; gemeint sind Flaechen.
            return Text_("GEBK_TITEL", "Gebäudedaten: Flächen, U-Werte");
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
