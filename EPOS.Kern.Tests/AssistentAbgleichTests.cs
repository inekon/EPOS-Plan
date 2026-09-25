using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der ABGLEICH des Bearbeiten-Zweigs im Projektassistenten (#490,
    /// <see cref="AssistentAbgleich"/>): Ein Speichern ohne Eingabe schreibt nichts —
    /// Änderungsdatum, Ids und Zeilen bleiben stehen —, und eine Eingabe in genau einem
    /// Gewerk schreibt genau dieses Gewerk und setzt das Datum.
    ///
    /// <para><b>Die Probe.</b> Projekt 1041 führt Wärmepumpe, Kessel, Pufferzeilen samt
    /// Senken, Prozesswärme, externen Wärmebedarf, Stromverbraucher und ein Gebäude; die
    /// sechste Zuordnung (Stromganglinie) legt die Vorbereitung über den Assistenten
    /// selbst an. Jeder Fall läuft auf einer EIGENEN Arbeitskopie.</para>
    ///
    /// <para><b>Verglichen wird mit Ids.</b> Anders als die Nachweise des Speicherwegs
    /// in <c>AssistentCtrlTests</c> (Inhalt ohne Autowerte) hält dieses Abbild die Ids
    /// fest — ein Löschen + Neuanlegen mit gleichem Inhalt fiele sonst nicht auf.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class AssistentAbgleichTests
    {
        private const int ID = 1041;
        private const string STROMGANGLINIE = "Lastgang_Strom_NestleLB-05-2010-05-2011";
        private static readonly DateTime ALT = new DateTime(2020, 1, 2, 3, 4, 5);

        // =========================================================================
        // Ohne Änderung
        // =========================================================================

        /// <summary>
        /// Laden, die Seiten „betreten" (Wärmepumpen- und Wärmebedarfsseite füllen ihre
        /// Zeilen nach) und speichern: kein Gewerk, kein Kopf, kein Datum, keine Id. Ein
        /// zweites Speichern desselben Laufs ebenso.
        /// </summary>
        [Fact]
        public void Ohne_Aenderung_bleiben_Datum_Ids_und_Zeilen_stehen()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                WizardCtrl vorherCtrl = WizardCtrl.Aktueller;
                try
                {
                    string name = SechsGewerkeVorbereiten();

                    WizardCtrl.Aktueller = new WizardCtrl();
                    AssistentCtrl a = Bearbeitenlauf(name);
                    // Wie die Huelle nach Projektwahl, Seitenschaltung und Kopfseite.
                    a.ZustandMerken();
                    SeitenBetreten(a);
                    Assert.False(a.HatAenderungen, "Das Betreten der Seiten gilt als Eingabe.");

                    DateTime? alt = DatumSetzen();
                    string vorher = Abbild(ID);

                    AssistentErgebnis e = a.Speichern();
                    Assert.True(e.Erfolg, "Speichern scheiterte an: " + e.Schritt);
                    Assert.Empty(a.GeschriebeneGewerke);
                    Assert.False(a.KopfGeschrieben);
                    // #497: Kein Traegersatz fehlt - die Heilung schreibt nichts.
                    Assert.Equal(0, a.GeheilteTraegersaetze);
                    Assert.Equal(alt, MerkmalUebernahmeCtrl.Aenderungsdatum(ID));
                    Assert.Equal(vorher, Abbild(ID));

                    // Der Assistent bleibt nach dem Speichern offen - ein zweites Speichern
                    // ohne Eingabe schreibt ebenso nichts.
                    e = a.Speichern();
                    Assert.True(e.Erfolg, "Zweites Speichern scheiterte an: " + e.Schritt);
                    Assert.Empty(a.GeschriebeneGewerke);
                    Assert.Equal(alt, MerkmalUebernahmeCtrl.Aenderungsdatum(ID));
                    Assert.Equal(vorher, Abbild(ID));
                }
                finally { WizardCtrl.Aktueller = vorherCtrl; }
            }
        }

        // =========================================================================
        // Je Gewerk eine Änderung
        // =========================================================================

        /// <summary>
        /// Eine Eingabe in GENAU einem Gewerk: Dieses wird geschrieben, das Datum steht
        /// auf jetzt; alle anderen Gewerke und ihre Nebenwirkungen (Anlagenzeilen samt
        /// Puffer, Senken, Stränge, Projektgeräte, Trägersätze, Kostenpositionen) bleiben
        /// mit ihren Ids stehen.
        /// </summary>
        [Theory]
        [InlineData("Erzeuger")]
        [InlineData("Prozess")]
        [InlineData("Stromganglinie")]
        [InlineData("Waermebedarf")]
        [InlineData("Stromverbraucher")]
        [InlineData("Kopf")]
        public void Eine_Aenderung_schreibt_genau_ihr_Gewerk_und_setzt_das_Datum(string was)
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                WizardCtrl vorherCtrl = WizardCtrl.Aktueller;
                try
                {
                    string name = SechsGewerkeVorbereiten();

                    WizardCtrl.Aktueller = new WizardCtrl();
                    AssistentCtrl a = Bearbeitenlauf(name);
                    SeitenBetreten(a);

                    DateTime? alt = DatumSetzen();
                    Dictionary<string, string> vorher = Abbilder(ID);
                    string[] wpVorher = WpStammfelder(ID);

                    string geaendert = Aendern(a, was);

                    AssistentErgebnis e = a.Speichern();
                    Assert.True(e.Erfolg, "Speichern scheiterte an: " + e.Schritt);

                    if (was == "Kopf")
                    {
                        Assert.Empty(a.GeschriebeneGewerke);
                        Assert.True(a.KopfGeschrieben);
                    }
                    else
                    {
                        Assert.Equal(new[] { (AssistentGewerk)Enum.Parse(typeof(AssistentGewerk), was) },
                                     a.GeschriebeneGewerke.ToArray());
                        Assert.False(a.KopfGeschrieben);
                    }

                    DateTime? neu = MerkmalUebernahmeCtrl.Aenderungsdatum(ID);
                    Assert.True(neu.HasValue && neu.Value > alt.Value.AddDays(1),
                                "Das Aenderungsdatum wurde nicht gesetzt: " + neu);

                    Dictionary<string, string> nachher = Abbilder(ID);
                    foreach (KeyValuePair<string, string> t in vorher)
                    {
                        if (t.Key == geaendert) continue;
                        if (was == "Erzeuger" && ERZEUGER_NEBENWIRKUNG.Contains(t.Key)) continue;
                        Assert.True(t.Value == nachher[t.Key], was + ": " + t.Key + " hat sich geaendert.");
                    }
                    if (geaendert != null)
                        Assert.True(vorher[geaendert] != nachher[geaendert], geaendert + " wurde nicht geschrieben.");

                    if (was == "Erzeuger")
                    {
                        // Das Neuschreiben der Anlagen laesst die Pufferzeilen stehen (FR-1),
                        // rettet die Senken, und die Stammfelder der WP-Projektkopie kommen
                        // mit ihren Werten wieder - nicht leer (Laden fuellt sie, #490).
                        Assert.Equal(Pufferzeilen(vorher), Pufferzeilen(nachher));
                        Assert.Equal(Zeilenzahl(vorher["Z_AnlageSenke"]), Zeilenzahl(nachher["Z_AnlageSenke"]));
                        Assert.Equal(wpVorher, WpStammfelder(ID));
                    }
                }
                finally { WizardCtrl.Aktueller = vorherCtrl; }
            }
        }

        /// <summary>
        /// #527: Der Kopf eines bestehenden Projekts trägt die Klimaregion als STAMM-Id
        /// (<c>ProjektCtrl.Kopf</c>) — dieselbe Id wie die Klappliste. Wechselt der
        /// Anwender dort die Region, schreibt der Bearbeiten-Zweig den Projektsatz und
        /// stempelt das Datum; die Kopfleiste liest danach die neue Region. Wählt er die
        /// bisherige erneut, bleibt alles stehen.
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ein_Regionswechsel_wird_geschrieben_die_gleiche_Region_nicht(bool wechseln)
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                WizardCtrl vorherCtrl = WizardCtrl.Aktueller;
                try
                {
                    // Derselbe Ausgangsstand wie die Faelle ohne Aenderung: Ein Speichern
                    // ohne Eingabe schreibt dann nichts (#490).
                    string name = SechsGewerkeVorbereiten();
                    int bisher = StartseiteCtrl.ProjektKlimaregionStammId(ID);
                    Assert.True(bisher > 0, "Projekt " + ID + " fuehrt keine Klimaregion.");

                    WizardCtrl.Aktueller = new WizardCtrl();
                    AssistentCtrl a = Bearbeitenlauf(name);
                    Assert.Equal(bisher, a.Kopf[0].IdKlimaregion);
                    a.ZustandMerken();
                    SeitenBetreten(a);

                    DateTime? alt = DatumSetzen();

                    // So schreibt ProjektKopfSeite.KlimaGewaehlt: Stamm-Id und Stammname.
                    int ziel = wechseln ? KlimaregionStammCtrl.IdVonName("Berlin") : bisher;
                    Assert.True(ziel > 0);
                    if (wechseln) Assert.NotEqual(bisher, ziel);
                    a.Kopf[0].IdKlimaregion = ziel;
                    a.Kopf[0].Klimaname = KlimaregionStammCtrl.NameVonId(ziel);

                    AssistentErgebnis e = a.Speichern();
                    Assert.True(e.Erfolg, "Speichern scheiterte an: " + e.Schritt);
                    Assert.Empty(a.GeschriebeneGewerke);
                    Assert.Equal(wechseln, a.KopfGeschrieben);

                    DateTime? neu = MerkmalUebernahmeCtrl.Aenderungsdatum(ID);
                    if (wechseln)
                        Assert.True(neu.HasValue && neu.Value > alt.Value.AddDays(1),
                                    "Das Aenderungsdatum wurde nicht gesetzt: " + neu);
                    else
                        Assert.Equal(alt, neu);

                    Assert.Equal(ziel, StartseiteCtrl.ProjektKlimaregionStammId(ID));
                    Assert.Equal(ziel, ProjektCtrl.Kopf(name).IdKlimaregion);
                }
                finally { WizardCtrl.Aktueller = vorherCtrl; }
            }
        }

        /// <summary>
        /// Eine Wärmepumpenzeile, deren Seite NIE gezeigt wurde, schreibt ihre Stammfelder
        /// nicht leer in die Projektkopie: Laden füllt sie wie die Seite (#490). Geändert
        /// wird dafür nur der Vorlauf des Kessels — die Wärmepumpe läuft mit.
        /// </summary>
        [Fact]
        public void Ohne_Waermepumpenseite_bleiben_die_Stammfelder_der_Projektkopie_stehen()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                WizardCtrl vorherCtrl = WizardCtrl.Aktueller;
                try
                {
                    WizardCtrl.Aktueller = new WizardCtrl();
                    string name = Projektname(ID);
                    string[] wpVorher = WpStammfelder(ID);
                    Assert.Contains(wpVorher, f => f.StartsWith("Firma=", StringComparison.Ordinal) && f.Length > 6);

                    AssistentCtrl a = Bearbeitenlauf(name);
                    Aendern(a, "Erzeuger");

                    AssistentErgebnis e = a.Speichern();
                    Assert.True(e.Erfolg, "Speichern scheiterte an: " + e.Schritt);
                    Assert.Contains(AssistentGewerk.Erzeuger, a.GeschriebeneGewerke);
                    Assert.Equal(wpVorher, WpStammfelder(ID));
                }
                finally { WizardCtrl.Aktueller = vorherCtrl; }
            }
        }

        /// <summary>
        /// Laden trägt den Kanal der Wärmebedarfszuordnung (Schritt 48) — ohne ihn
        /// schriebe ein Speichern, das die Seite nie zeigt, jede Ganglinie als Heizung
        /// zurück.
        /// </summary>
        [Fact]
        public void Laden_traegt_den_Kanal_des_Waermebedarfs()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                Assert.True(DataRepository.ExecuteSQL(
                    "UPDATE Z_ProjektWaermebedarf SET Kanal = ? WHERE ID_Projekt = ?",
                    new DbParam("@k", DbWerte.KANAL_PROZESS), new DbParam("@p", ID)));

                AssistentCtrl a = new AssistentCtrl();
                a.Laden(Projektname(ID));

                Assert.NotEmpty(a.Waermebedarf);
                Assert.All(a.Waermebedarf, w => Assert.Equal(DbWerte.KANAL_PROZESS, w.Kanal));
            }
        }

        /// <summary>
        /// Ohne Vergleichsstand — das Ladekennzeichen ist zurückgesetzt, etwa weil links
        /// ein anderes Projekt markiert wurde — schreibt der Zweig wie vor dem Abgleich
        /// jedes Gewerk: im Zweifel geschrieben.
        /// </summary>
        [Fact]
        public void Ohne_Vergleichsstand_wird_jedes_Gewerk_geschrieben()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                WizardCtrl vorherCtrl = WizardCtrl.Aktueller;
                try
                {
                    WizardCtrl.Aktueller = new WizardCtrl();
                    AssistentCtrl a = Bearbeitenlauf(Projektname(ID));
                    a.BereitsGeladen = false;

                    AssistentErgebnis e = a.Speichern();
                    Assert.True(e.Erfolg, "Speichern scheiterte an: " + e.Schritt);
                    Assert.Equal(AssistentAbgleich.GEWERKE, a.GeschriebeneGewerke.Count);
                }
                finally { WizardCtrl.Aktueller = vorherCtrl; }
            }
        }

        // =========================================================================
        // #497 (a): Trägersätze heilen unabhängig vom Erzeuger-Abdruck
        // =========================================================================

        /// <summary>
        /// Fehlt einer vorhandenen Anlage ihr projektgebundener Trägersatz, legt ein
        /// Speichern OHNE Anlagenänderung genau diesen an und setzt das Änderungsdatum;
        /// die übrigen Sätze behalten ihre Ids. Ein zweites Speichern findet keine Lücke
        /// mehr und schreibt nichts.
        /// </summary>
        [Fact]
        public void Ein_fehlender_Traegersatz_heilt_beim_Speichern_ohne_Aenderung()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                WizardCtrl vorherCtrl = WizardCtrl.Aktueller;
                try
                {
                    string name = SechsGewerkeVorbereiten();

                    // Die Saetze des Projekts: ein Brenner-Traeger (Kessel) und der
                    // Stromtraeger (Waermepumpe). Der Brenner-Satz wird entfernt.
                    int kesselTraeger = Convert.ToInt32(DataRepository.ExecuteScalar(
                        "SELECT ID_Carrier FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                        new DbParam("@p", ID), new DbParam("@t", WizardItemClass.KESSEL_TYP)), CultureInfo.InvariantCulture);
                    Assert.True(kesselTraeger > 0, "Projekt 1041 fuehrt keinen Kessel mit Traeger.");
                    Assert.Equal(1, Saetze(kesselTraeger));
                    string andereVorher = AndereSaetze(kesselTraeger);

                    Assert.True(DataRepository.ExecuteSQL(
                        "DELETE FROM energy_Project_settings WHERE ID_Projekt = ? AND ID_Energieträger = ?",
                        new DbParam("@p", ID), new DbParam("@c", kesselTraeger)));
                    Assert.True(DataRepository.ExecuteSQL(
                        "DELETE FROM energy_price WHERE id_projekt = ? AND carrier_id = ?",
                        new DbParam("@p", ID), new DbParam("@c", kesselTraeger)));
                    Assert.Equal(0, Saetze(kesselTraeger));

                    WizardCtrl.Aktueller = new WizardCtrl();
                    AssistentCtrl a = Bearbeitenlauf(name);
                    SeitenBetreten(a);
                    DateTime? alt = DatumSetzen();

                    AssistentErgebnis e = a.Speichern();
                    Assert.True(e.Erfolg, "Speichern scheiterte an: " + e.Schritt);
                    Assert.Empty(a.GeschriebeneGewerke);
                    Assert.Equal(1, a.GeheilteTraegersaetze);
                    Assert.Equal(1, Saetze(kesselTraeger));
                    Assert.Equal(andereVorher, AndereSaetze(kesselTraeger));

                    DateTime? neu = MerkmalUebernahmeCtrl.Aenderungsdatum(ID);
                    Assert.True(neu.HasValue && neu.Value > alt.Value.AddDays(1),
                                "Die Heilung hat das Aenderungsdatum nicht gesetzt: " + neu);

                    // Zweites Speichern: keine Luecke, kein Schreiben, kein Stempel.
                    alt = DatumSetzen();
                    string vorher = Abbild(ID);
                    e = a.Speichern();
                    Assert.True(e.Erfolg, "Zweites Speichern scheiterte an: " + e.Schritt);
                    Assert.Equal(0, a.GeheilteTraegersaetze);
                    Assert.Equal(alt, MerkmalUebernahmeCtrl.Aenderungsdatum(ID));
                    Assert.Equal(vorher, Abbild(ID));
                }
                finally { WizardCtrl.Aktueller = vorherCtrl; }
            }
        }

        // =========================================================================
        // #497 (b): Id-Nachzug der Zuordnungen
        // =========================================================================

        /// <summary>
        /// Neu aufgenommene Zeilen der vier Zuordnungen tragen vorläufige Ids ab 100000;
        /// nach dem Speichern tragen alle Zeilen die Ids der Datenbank (und den Verweis
        /// auf ihre Projektkopie), und ein zweites Speichern schreibt nichts.
        /// </summary>
        [Fact]
        public void Nach_dem_Speichern_tragen_die_Zuordnungen_ihre_echten_Ids()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                WizardCtrl vorherCtrl = WizardCtrl.Aktueller;
                try
                {
                    string name = SechsGewerkeVorbereiten();

                    WizardCtrl.Aktueller = new WizardCtrl();
                    AssistentCtrl a = Bearbeitenlauf(name);
                    SeitenBetreten(a);

                    a.Prozess.Add(new Z_ProjektProzesswaermeModel
                    {
                        ID_Z = 100000, ID_Projekt = ID, szProzessname = a.Prozess[0].szProzessname, Summe = 7
                    });
                    a.Stromverbraucher.Add(new Z_ProjektStromverbraucherModel
                    {
                        m_ID_Z = 100000, m_ID_Projekt = ID, m_szVerbraucher = a.Stromverbraucher[0].m_szVerbraucher, m_Summe = 7
                    });
                    a.Waermebedarf.Add(new Z_ProjWaermebedarfModel
                    {
                        m_ID_Z = 100000, m_ID_Projekt = ID, m_szBezeichner = a.Waermebedarf[0].m_szBezeichner,
                        Kanal = DbWerte.KANAL_PROZESS
                    });
                    a.Stromganglinie.Add(new Z_ProjektStromganglinieModel
                    {
                        m_ID_Z = 100001, m_ID_Projekt = ID, m_szStromganglinie = STROMGANGLINIE
                    });

                    AssistentErgebnis e = a.Speichern();
                    Assert.True(e.Erfolg, "Speichern scheiterte an: " + e.Schritt);
                    Assert.Equal(4, a.GeschriebeneGewerke.Count);

                    // Keine vorlaeufige Id mehr - und genau die Ids der Datenbank.
                    Assert.Equal(Paare("Z_Projekt_Prozesswaerme", "ID", "ID_Prozesswaerme"),
                                 Sortiert(a.Prozess.Select(p => (p.ID_Z, p.ID_Prozesswaerme))));
                    Assert.Equal(Paare("Z_Projekt_Stromverbraucher", "ID", "ID_Stromverbraucher"),
                                 Sortiert(a.Stromverbraucher.Select(v => (v.m_ID_Z, v.m_ID_Stromverbraucher))));
                    Assert.Equal(Paare("Z_ProjektWaermebedarf", "ID_Z", "ID_Ganglinie"),
                                 Sortiert(a.Waermebedarf.Select(w => (w.m_ID_Z, w.m_ID_Ganglinie))));
                    Assert.Equal(Paare("Z_ProjektStromganglinie", "ID", "ID_Ganglinie"),
                                 Sortiert(a.Stromganglinie.Select(s => (s.m_ID_Z, s.m_ID_Stromganglinie))));
                    Assert.All(a.Prozess, p => Assert.True(p.ID_Z < 100000 && p.ID_Projekt == ID));
                    Assert.All(a.Stromverbraucher, v => Assert.True(v.m_ID_Z < 100000 && v.m_ID_Projekt == ID));
                    Assert.All(a.Waermebedarf, w => Assert.True(w.m_ID_Z < 100000 && w.m_ID_Projekt == ID));
                    Assert.All(a.Stromganglinie, s => Assert.True(s.m_ID_Z < 100000 && s.m_ID_Projekt == ID));

                    // Ein zweites Speichern desselben Laufs schreibt nichts.
                    DateTime? alt = DatumSetzen();
                    string vorher = Abbild(ID);
                    e = a.Speichern();
                    Assert.True(e.Erfolg, "Zweites Speichern scheiterte an: " + e.Schritt);
                    Assert.Empty(a.GeschriebeneGewerke);
                    Assert.Equal(alt, MerkmalUebernahmeCtrl.Aenderungsdatum(ID));
                    Assert.Equal(vorher, Abbild(ID));
                }
                finally { WizardCtrl.Aktueller = vorherCtrl; }
            }
        }

        // =========================================================================
        // #497 (c): Speichern für ein anderes Projekt als das geladene
        // =========================================================================

        /// <summary>
        /// Die Listen stammen aus Projekt 1041; die linke Spalte markiert danach 1030
        /// (Projekt-Id gesetzt, Ladekennzeichen zurück), gespeichert wird ohne neues
        /// Laden. Der Lauf lehnt BENANNT ab, und keines der beiden Projekte ist berührt.
        /// </summary>
        [Fact]
        public void Speichern_fuer_ein_anderes_Projekt_als_das_geladene_wird_abgelehnt()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                const int ANDERES = 1030;
                WizardCtrl vorherCtrl = WizardCtrl.Aktueller;
                try
                {
                    WizardCtrl.Aktueller = new WizardCtrl();
                    AssistentCtrl a = Bearbeitenlauf(Projektname(ID));
                    Assert.Equal(ID, a.ListenProjektId);

                    // Wie ProjektMarkiert in der Huelle - ohne danach die Projektseite
                    // zu durchlaufen.
                    a.ProjektId = ANDERES;
                    a.BereitsGeladen = false;

                    DateTime? datumAnderes = MerkmalUebernahmeCtrl.Aenderungsdatum(ANDERES);
                    DateTime? datumEigen = MerkmalUebernahmeCtrl.Aenderungsdatum(ID);
                    string anderesVorher = Abbild(ANDERES);
                    string eigenVorher = Abbild(ID);

                    AssistentErgebnis e = a.Speichern();
                    Assert.Equal(AssistentAusgang.ProjektGewechselt, e.Ausgang);
                    Assert.False(e.Erfolg);
                    Assert.False(a.Gespeichert);
                    Assert.Empty(a.GeschriebeneGewerke);
                    Assert.False(string.IsNullOrEmpty(AssistentCtrl.Meldungstext(e)));
                    Assert.False(string.IsNullOrEmpty(AssistentCtrl.Meldungstitel(e)));

                    Assert.Equal(anderesVorher, Abbild(ANDERES));
                    Assert.Equal(eigenVorher, Abbild(ID));
                    Assert.Equal(datumAnderes, MerkmalUebernahmeCtrl.Aenderungsdatum(ANDERES));
                    Assert.Equal(datumEigen, MerkmalUebernahmeCtrl.Aenderungsdatum(ID));

                    // Nach dem Laden des markierten Projekts geht es wieder.
                    a.Laden(Projektname(ANDERES));
                    Assert.Equal(ANDERES, a.ListenProjektId);
                }
                finally { WizardCtrl.Aktueller = vorherCtrl; }
            }
        }

        /// <summary>
        /// Ohne Datenbank: Ein Bearbeiten-Lauf, der nie geladen hat, schreibt nicht —
        /// er lehnt vor jedem Datenbankzugriff ab; der Neu-Zweig kennt die Prüfung nicht.
        /// </summary>
        [Fact]
        public void Bearbeiten_ohne_geladene_Listen_wird_abgelehnt()
        {
            WizardCtrl vorherCtrl = WizardCtrl.Aktueller;
            try
            {
                WizardCtrl.Aktueller = new WizardCtrl();
                AssistentCtrl a = new AssistentCtrl();
                a.Betriebsart = AssistentCtrl.BETRIEBSART_BEARBEITEN;
                a.ProjektId = 4711;
                a.Kopf[0].Name = "Irgendwas";
                a.Kopf[0].Klimaname = "Region 12 Mannheim";

                Assert.Equal(0, a.ListenProjektId);
                AssistentErgebnis e = a.Speichern();
                Assert.Equal(AssistentAusgang.ProjektGewechselt, e.Ausgang);
                Assert.False(a.Gespeichert);
            }
            finally { WizardCtrl.Aktueller = vorherCtrl; }
        }

        // =========================================================================
        // #507 (a): Nachweis R-W16-6 für ein über den Assistenten NEU angelegtes Projekt
        // =========================================================================

        /// <summary>
        /// Ein Projekt, das der NEU-Zweig aus Katalogsätzen anlegt — Kopf, Gebäude,
        /// Kessel samt Energieträger, Prozesswärme, Stromverbraucher, Stromganglinie und
        /// externer Wärmebedarf —, bleibt bei einem anschließenden Speichern ohne Änderung
        /// über den Bearbeiten-Zweig Zeile für Zeile stehen: kein Gewerk, kein Kopf, keine
        /// Trägerheilung, kein Änderungsdatum, keine Id, in keiner projektgebundenen
        /// Tabelle. Ein zweites Speichern desselben Laufs ebenso.
        ///
        /// <para>Das ist der Kern-Teil des Nachweises R-W16-6 für den Neu-Fall (#507); den
        /// Feld-für-Feld-Vergleich der Simulationsergebnisse davor und danach führt
        /// <c>Referenzlauf.exe lauf/vergleich</c> auf derselben Probe am Windows-Gerät.
        /// Brauchwasser ist nicht dabei: Der Assistent führt dafür keine Seite.</para>
        /// </summary>
        [Fact]
        public void Ein_neu_angelegtes_Projekt_bleibt_beim_Speichern_ohne_Aenderung_stehen()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                WizardCtrl vorherCtrl = WizardCtrl.Aktueller;
                try
                {
                    // --- Der NEU-Lauf ---------------------------------------------------
                    WizardCtrl.Aktueller = new WizardCtrl();
                    AssistentCtrl neu = Neuanlage(NEU_NAME);

                    AssistentErgebnis e = neu.Speichern();
                    Assert.True(e.Erfolg, "Anlegen scheiterte an: " + e.Schritt);

                    int id = ProjektCtrl.IdVonName(NEU_NAME);
                    Assert.True(id > 0, "Das neue Projekt wurde nicht angelegt.");
                    Assert.Equal(id, neu.ProjektId);

                    // Alle sechs Gewerke und der Energietraegersatz stehen.
                    foreach (string t in new[] { "Tab_Energieanlagen", "Z_ProjektGebaeude", "Z_ProjektWaermebedarf",
                                                 "Z_Projekt_Prozesswaerme", "Z_Projekt_Stromverbraucher",
                                                 "Z_ProjektStromganglinie", "energy_project_settings", "energy_price",
                                                 "Tab_Gebaeude", "Tab_Heizkessel", "Tab_Klimaregion" })
                        Assert.True(Zeilen(t, id) > 0, t + " ist nach dem Anlegen leer.");

                    // --- Der Bearbeiten-Lauf ohne Eingabe -------------------------------
                    WizardCtrl.Aktueller = new WizardCtrl();
                    AssistentCtrl a = Bearbeitenlauf(NEU_NAME, id);
                    a.ZustandMerken();
                    SeitenBetreten(a, id);
                    Assert.False(a.HatAenderungen, "Das Betreten der Seiten gilt als Eingabe.");

                    DateTime? alt = DatumSetzen(id);
                    string vorher = VollesAbbild(id);

                    e = a.Speichern();
                    Assert.True(e.Erfolg, "Speichern scheiterte an: " + e.Schritt);
                    Assert.Empty(a.GeschriebeneGewerke);
                    Assert.False(a.KopfGeschrieben);
                    Assert.Equal(0, a.GeheilteTraegersaetze);
                    Assert.Equal(alt, MerkmalUebernahmeCtrl.Aenderungsdatum(id));
                    Assert.Equal(vorher, VollesAbbild(id));

                    e = a.Speichern();
                    Assert.True(e.Erfolg, "Zweites Speichern scheiterte an: " + e.Schritt);
                    Assert.Empty(a.GeschriebeneGewerke);
                    Assert.Equal(alt, MerkmalUebernahmeCtrl.Aenderungsdatum(id));
                    Assert.Equal(vorher, VollesAbbild(id));
                }
                finally { WizardCtrl.Aktueller = vorherCtrl; }
            }
        }

        // =========================================================================
        // #507 (b): Der Verweis auf die Projektkopie nach einem Rückzug
        // =========================================================================

        /// <summary>
        /// <c>Add_Projekt_Prozess</c> und <c>Add_Projekt_Stromverbraucher</c> tragen den
        /// Verweis auf die Projektkopie schon VOR dem Festschreiben in die Listenzeile ein.
        /// Rollt der Lauf zurück, zeigt er auf eine Kopie, die es nicht gibt — folgenlos:
        /// Der nächste Speicherlauf leitet ihn aus dem Namen neu ab, bevor er ihn schreibt,
        /// und der Abdruck der Gewerke vergleicht ihn nicht. Erzwungen wird der Rückzug in
        /// <c>Add_Projekt_Stromverbraucher</c> selbst (ein zweiter Verbraucher ohne
        /// Katalogsatz) — dann sind Prozesswärme und der erste Verbraucher schon kopiert.
        /// </summary>
        [Fact]
        public void Nach_einem_Rueckzug_schreibt_der_naechste_Lauf_gueltige_Verweise()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                WizardCtrl vorherCtrl = WizardCtrl.Aktueller;
                try
                {
                    WizardCtrl.Aktueller = new WizardCtrl();
                    AssistentCtrl a = Neuanlage(NEU_NAME);
                    int projekteVorher = Zeilen("Tab_Projekt", 0);

                    a.Stromverbraucher.Add(new Z_ProjektStromverbraucherModel
                    {
                        m_ID_Z = 100001, m_szVerbraucher = "Kein Verbraucher dieses Namens (#507)", m_Summe = 1
                    });

                    AssistentErgebnis e = a.Speichern();
                    Assert.Equal(AssistentAusgang.Fehlgeschlagen, e.Ausgang);
                    Assert.Equal("Add_Projekt_Stromverbraucher", e.Schritt);
                    Assert.Equal(projekteVorher, Zeilen("Tab_Projekt", 0));

                    // Die Listenzeilen zeigen jetzt auf zurueckgerollte Projektkopien.
                    int pwVerweis = a.Prozess[0].ID_Prozesswaerme;
                    int svVerweis = a.Stromverbraucher[0].m_ID_Stromverbraucher;
                    Assert.Null(DataRepository.ExecuteScalar("SELECT ID FROM Tab_Prozesswaerme WHERE ID = ?",
                                                             new DbParam("@i", pwVerweis)));
                    Assert.Null(DataRepository.ExecuteScalar("SELECT ID FROM Tab_Stromverbraucher WHERE ID = ?",
                                                             new DbParam("@i", svVerweis)));

                    // Die fehlerhafte Zeile weg, erneut speichern: gelingt, und jeder
                    // Verweis zeigt auf eine Kopie DIESES Projekts.
                    a.Stromverbraucher.RemoveAt(a.Stromverbraucher.Count - 1);
                    e = a.Speichern();
                    Assert.True(e.Erfolg, "Zweiter Versuch scheiterte an: " + e.Schritt);

                    int id = ProjektCtrl.IdVonName(NEU_NAME);
                    Assert.True(id > 0);
                    Assert.Equal(Paare("Z_Projekt_Prozesswaerme", "ID", "ID_Prozesswaerme", id),
                                 Sortiert(a.Prozess.Select(p => (p.ID_Z, p.ID_Prozesswaerme))));
                    Assert.Equal(Paare("Z_Projekt_Stromverbraucher", "ID", "ID_Stromverbraucher", id),
                                 Sortiert(a.Stromverbraucher.Select(v => (v.m_ID_Z, v.m_ID_Stromverbraucher))));
                    Assert.Equal((long)id, Convert.ToInt64(DataRepository.ExecuteScalar(
                        "SELECT ID_Projekt FROM Tab_Prozesswaerme WHERE ID = ?",
                        new DbParam("@i", a.Prozess[0].ID_Prozesswaerme)), CultureInfo.InvariantCulture));
                    Assert.Equal((long)id, Convert.ToInt64(DataRepository.ExecuteScalar(
                        "SELECT ID_Projekt FROM Tab_Stromverbraucher WHERE ID = ?",
                        new DbParam("@i", a.Stromverbraucher[0].m_ID_Stromverbraucher)), CultureInfo.InvariantCulture));
                    Assert.Empty(DataRepository.GetDataTable("PRAGMA foreign_key_check").Rows);
                }
                finally { WizardCtrl.Aktueller = vorherCtrl; }
            }
        }

        // =========================================================================
        // Ohne Datenbank: der Abdruck
        // =========================================================================

        /// <summary>
        /// Der Abdruck sieht, was der Schreibweg schreibt, und übersieht, was er nicht
        /// schreibt: Ids der Zuordnungen und Pufferzeilen zählen nicht, Summe, Name,
        /// Kanal, Anlagenwerte und eine gesetzte Strangliste schon.
        /// </summary>
        [Fact]
        public void Der_Abdruck_zaehlt_nur_was_geschrieben_wird()
        {
            AssistentCtrl a = new AssistentCtrl();
            a.Prozess.Add(new Z_ProjektProzesswaermeModel { ID_Z = 1, ID_Prozesswaerme = 5, szProzessname = "P", Summe = 10 });
            a.Waermebedarf.Add(new Z_ProjWaermebedarfModel { m_ID_Z = 1, m_szBezeichner = "W" });
            a.Erzeuger.Add(new WErzeugerModel { ID = 7, ID_Type = WizardItemClass.KESSEL_TYP, Bezeichner = "K", Vorlauf = 70 });

            string[] stand = AssistentAbgleich.Abdruecke(a);

            // Ids und Projektverweise: kein Unterschied.
            a.Prozess[0].ID_Z = 99;
            a.Prozess[0].ID_Prozesswaerme = 123;
            a.Waermebedarf[0].m_ID_Z = 99;
            a.Erzeuger[0].ID = 100001;
            // NULL-Kanal und Heizung sind dasselbe - so schreibt es der Add-Weg.
            a.Waermebedarf[0].Kanal = DbWerte.KANAL_HEIZUNG;
            // Pufferzeilen schreibt der Bearbeiten-Zweig nicht.
            a.Erzeuger.Add(new WErzeugerModel { ID_Type = WizardItemClass.PUFFER_TYP, Bezeichner = "Puffer" });
            Assert.Equal(stand, AssistentAbgleich.Abdruecke(a));

            a.Prozess[0].Summe = 11;
            Assert.NotEqual(stand[(int)AssistentGewerk.Prozess], AssistentAbgleich.Abdruck(a, AssistentGewerk.Prozess));
            a.Prozess[0].Summe = 10;

            a.Waermebedarf[0].Kanal = DbWerte.KANAL_PROZESS;
            Assert.NotEqual(stand[(int)AssistentGewerk.Waermebedarf], AssistentAbgleich.Abdruck(a, AssistentGewerk.Waermebedarf));

            a.Erzeuger[0].Vorlauf = 71;
            Assert.NotEqual(stand[(int)AssistentGewerk.Erzeuger], AssistentAbgleich.Abdruck(a, AssistentGewerk.Erzeuger));
            a.Erzeuger[0].Vorlauf = 70;
            Assert.Equal(stand[(int)AssistentGewerk.Erzeuger], AssistentAbgleich.Abdruck(a, AssistentGewerk.Erzeuger));

            // "Alle Straenge entfernt" ist eine Eingabe (leere, gesetzte Liste).
            a.Erzeuger[0].PV_Straenge = new List<AnlageStrangModel>();
            Assert.NotEqual(stand[(int)AssistentGewerk.Erzeuger], AssistentAbgleich.Abdruck(a, AssistentGewerk.Erzeuger));
        }

        // =========================================================================
        // Hilfen
        // =========================================================================

        /// <summary>
        /// Bringt Projekt 1041 auf sechs belegte Gewerke: Die fehlende Stromganglinie
        /// legt ein Assistentenlauf selbst an. Liefert den Projektnamen.
        /// </summary>
        private static string SechsGewerkeVorbereiten()
        {
            string name = Projektname(ID);

            WizardCtrl.Aktueller = new WizardCtrl();
            AssistentCtrl a = Bearbeitenlauf(name);
            a.SeiteSchalten(WizardItemClass.STROMLASTGANG_ITEM, true);
            a.Stromganglinie.Add(new Z_ProjektStromganglinieModel { m_szStromganglinie = STROMGANGLINIE });

            AssistentErgebnis e = a.Speichern();
            Assert.True(e.Erfolg, "Vorbereitung scheiterte an: " + e.Schritt);

            foreach (string t in new[] { "Tab_Energieanlagen", "Z_ProjektGebaeude", "Z_ProjektWaermebedarf",
                                         "Z_Projekt_Prozesswaerme", "Z_Projekt_Stromverbraucher",
                                         "Z_ProjektStromganglinie" })
            {
                object n = DataRepository.ExecuteScalar("SELECT COUNT(*) FROM " + t + " WHERE ID_Projekt = ?",
                                                        new DbParam("@p", ID));
                Assert.True(Convert.ToInt32(n, CultureInfo.InvariantCulture) > 0, t + " ist leer.");
            }
            return name;
        }

        // --- #507: die Neuanlage aus Katalogsätzen ------------------------------------

        private const string NEU_NAME = "#507 Neuanlage-Probe";
        private const string NEU_KLIMA = "stuttgart";
        private const string NEU_GEBAEUDE = "EFH-A-U-347s";
        private const string NEU_KESSEL = "GC7000F 22 23 - MX25";
        private const string NEU_BRENNSTOFF = "Erdgas E";
        private const string NEU_PROZESS = "Hotel_1";
        private const string NEU_STROMVERBRAUCHER = "EFH_3_Pers";
        private const string NEU_WAERMEBEDARF = "Wärmebedarf_Laurentiuskirche";

        /// <summary>
        /// Ein NEU-Lauf, so gefüllt, wie die Seiten ihn füllen: Kopf mit Klimazone, die
        /// Kacheln geschaltet, je Gewerk eine Zeile mit vorläufiger Id ab 100000 und
        /// Katalogverweis. Der Kessel geht den Weg der Kesselseite
        /// (<c>HeizkesselHuelle.Aufnehmen</c>): Trägervariante im Assistentenbetrieb
        /// (nur Katalogträger), Temperaturen aus dem Katalogsatz, Stamm-Id als
        /// Platzhalter der Projektkopie.
        /// </summary>
        private static AssistentCtrl Neuanlage(string name)
        {
            AssistentCtrl a = new AssistentCtrl();
            a.Betriebsart = AssistentCtrl.BETRIEBSART_NEU;
            a.ProjektId = new ProjektCtrl().GetMaxID() + 1;

            a.Kopf[0].Name = name;
            a.Kopf[0].Beschreibung = "Nachweis R-W16-6, Neu-Fall";
            a.Kopf[0].Kunde = "Kunde";
            a.Kopf[0].Bearbeiter = "Bearbeiter";
            a.Kopf[0].Erstelldatum = new DateTime(2026, 9, 25);
            a.Kopf[0].Klimaname = NEU_KLIMA;
            a.KopfMerken();

            foreach (int seite in new[] { WizardItemClass.GEBAEUDE_ITEM, WizardItemClass.WAERMEBEDARF_ITEM,
                                          WizardItemClass.PROZESS_ITEM, WizardItemClass.STROMSTD_ITEM,
                                          WizardItemClass.STROMLASTGANG_ITEM, WizardItemClass.KESSEL_ITEM })
                a.SeiteSchalten(seite, true);

            int gebaeude = KatalogId("Tab_Gebaeude_STAMM", NEU_GEBAEUDE);
            a.Gebaeude.Add(new Z_ProjGebModel
            {
                ID_Z = 100000, ID_Projekt = a.ProjektId, ID_Gebaeude = gebaeude, ID_Gebaeude_Stamm = gebaeude,
                Gebaeudename = NEU_GEBAEUDE, Wohnflaeche = 201, Einheit = "Wohnfläche [m²]", Jahresnutzungsgrad = 1
            });

            int kessel = KatalogId("Tab_Heizkessel_STAMM", NEU_KESSEL);
            int brennstoff = KatalogId("Tab_Brennstoff_Stamm", NEU_BRENNSTOFF);
            EnergietraegerVarianteCtrl.VariantenErgebnis traeger =
                EnergietraegerVarianteCtrl.Anlegen(a.ProjektId, true, brennstoff, NEU_BRENNSTOFF, NEU_BRENNSTOFF);
            Assert.True(traeger.CarrierId > 0, "Traegervariante: " + traeger.Meldung);
            WErzeugerModel k = new WErzeugerModel
            {
                ID = 100000, ID_Projekt = a.ProjektId, ID_Type = WizardItemClass.KESSEL_TYP,
                Bezeichner = NEU_KESSEL, ID_Carrier = traeger.CarrierId, ID_Kessel = kessel
            };
            AnlagenTemperaturen.AusStammsatz(k, kessel);
            a.Erzeuger.Add(k);

            a.Prozess.Add(new Z_ProjektProzesswaermeModel
            {
                ID_Z = 100000, ID_Projekt = a.ProjektId, szProzessname = NEU_PROZESS,
                ID_Prozesswaerme = KatalogId("Tab_Prozesswaerme_STAMM", NEU_PROZESS), Summe = 30
            });
            a.Stromverbraucher.Add(new Z_ProjektStromverbraucherModel
            {
                m_ID_Z = 100000, m_ID_Projekt = a.ProjektId, m_szVerbraucher = NEU_STROMVERBRAUCHER,
                m_ID_Stromverbraucher = KatalogId("Tab_Stromverbraucher_STAMM", NEU_STROMVERBRAUCHER), m_Summe = 8
            });
            a.Stromganglinie.Add(new Z_ProjektStromganglinieModel
            {
                m_ID_Z = 100000, m_ID_Projekt = a.ProjektId, m_szStromganglinie = STROMGANGLINIE,
                m_ID_Stromganglinie = KatalogId("Tab_Stromganglinie_STAMM", STROMGANGLINIE)
            });
            a.Waermebedarf.Add(new Z_ProjWaermebedarfModel
            {
                m_ID_Z = 100000, m_ID_Projekt = a.ProjektId, m_szBezeichner = NEU_WAERMEBEDARF,
                m_ID_Ganglinie = KatalogId("Tab_Waermebedarf_STAMM", NEU_WAERMEBEDARF), Kanal = DbWerte.KANAL_HEIZUNG
            });
            return a;
        }

        private static int KatalogId(string tabelle, string bezeichner)
        {
            int id = DataRepository.GetIdByName(tabelle, "Bezeichner", bezeichner);
            Assert.True(id > 0, "Katalogsatz fehlt: " + tabelle + " \"" + bezeichner + "\"");
            return id;
        }

        /// <summary>Zeilen einer projektgebundenen Tabelle (<c>idProjekt</c> 0 = alle Zeilen).</summary>
        private static int Zeilen(string tabelle, int idProjekt)
        {
            object n = idProjekt == 0
                ? DataRepository.ExecuteScalar("SELECT COUNT(*) FROM [" + tabelle + "]")
                : DataRepository.ExecuteScalar("SELECT COUNT(*) FROM [" + tabelle + "] WHERE ID_Projekt = ?",
                                               new DbParam("@p", idProjekt));
            return Convert.ToInt32(n, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Das Abbild ALLER projektgebundenen Tabellen — jede Tabelle der Datenbank mit
        /// einer Spalte <c>ID_Projekt</c> oder <c>ProjektID</c>, dazu <c>Tab_Projekt</c>,
        /// Senken und Stränge der Anlagen; mit Ids, ohne Zeitpunktspalten.
        /// </summary>
        private static string VollesAbbild(int idProjekt)
        {
            DataTable tabellen = DataRepository.GetDataTable(
                "SELECT m.name AS t, (SELECT p.name FROM pragma_table_info(m.name) p " +
                "WHERE lower(p.name) IN ('id_projekt','projektid') LIMIT 1) AS s " +
                "FROM sqlite_master m WHERE m.type = 'table' ORDER BY m.name");
            StringBuilder sb = new StringBuilder();
            sb.Append("== Tab_Projekt\n").Append(Tabelle("SELECT * FROM Tab_Projekt WHERE ID = ?", idProjekt));
            foreach (DataRow r in tabellen.Rows)
            {
                if (r["s"] == DBNull.Value) continue;
                string t = Convert.ToString(r["t"], CultureInfo.InvariantCulture);
                string s = Convert.ToString(r["s"], CultureInfo.InvariantCulture);
                sb.Append("== ").Append(t).Append('\n')
                  .Append(Tabelle("SELECT * FROM [" + t + "] WHERE [" + s + "] = ? ORDER BY 1", idProjekt));
            }
            foreach (string t in new[] { "Z_AnlageSenke", "Z_AnlageStrang" })
            {
                if (!DataRepository.TabelleVorhanden(t)) continue;
                sb.Append("== ").Append(t).Append('\n')
                  .Append(Tabelle("SELECT s.* FROM [" + t + "] s JOIN Tab_Energieanlagen e ON e.ID = s.ID_Anlage " +
                                  "WHERE e.ID_Projekt = ? ORDER BY 1", idProjekt));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Ein Lauf im BEARBEITEN-Zweig, so gestellt, wie ihn der Komponentenschritt und
        /// die Kopfseite stellen würden (wie in <c>AssistentCtrlTests</c>).
        /// </summary>
        private static AssistentCtrl Bearbeitenlauf(string name)
        {
            return Bearbeitenlauf(name, ID);
        }

        private static AssistentCtrl Bearbeitenlauf(string name, int id)
        {
            AssistentCtrl a = new AssistentCtrl();
            a.Betriebsart = AssistentCtrl.BETRIEBSART_BEARBEITEN;
            a.ProjektId = id;
            a.Laden(name);

            KomponentenBestandCtrl bestand = KomponentenBestandCtrl.Lesen(id);
            for (int k = 0; k < KomponentenBestandCtrl.ANZAHL; k++)
                a.SeiteSchalten(bestand[k].SeitenIndex, bestand[k].Vorhanden);

            ProjektKopfDaten kopf = ProjektCtrl.Kopf(name);
            Assert.NotNull(kopf);
            a.Kopf[0].Name = kopf.Name;
            a.Kopf[0].Beschreibung = kopf.Beschreibung;
            a.Kopf[0].Kunde = kopf.Kunde;
            a.Kopf[0].Bearbeiter = kopf.Bearbeiter;
            a.Kopf[0].Erstelldatum = kopf.Erstelldatum;
            a.Kopf[0].Aenderungsdatum = kopf.Aenderungsdatum;
            a.Kopf[0].IdKlimaregion = kopf.IdKlimaregion;
            a.Kopf[0].Klimaname = kopf.Klimaname;
            return a;
        }

        /// <summary>
        /// Was die Seiten beim Aufbau an den geteilten Listen tun: Die Wärmepumpenseite
        /// füllt die Stammfelder nach (<c>WaermepumpenHuelle.Gaben</c>), die
        /// Wärmebedarfsseite den Kanal (<c>WaermebedarfExternHuelle.Gaben</c>).
        /// </summary>
        private static void SeitenBetreten(AssistentCtrl a)
        {
            SeitenBetreten(a, ID);
        }

        private static void SeitenBetreten(AssistentCtrl a, int id)
        {
            foreach (WErzeugerModel m in a.Erzeuger)
                if (m.ID_Type == WizardItemClass.WP_TYP)
                    WaermepumpeGeraeteCtrl.GeraetedatenFuellen(m, m.ID_WP);
            Z_ProjektGebGanglinieCtrl.KanaeleNachladen(id, a.Waermebedarf);
        }

        /// <summary>Gibt eine Eingabe in das Gewerk und nennt die Tabelle, die sich ändern muss.</summary>
        private static string Aendern(AssistentCtrl a, string was)
        {
            switch (was)
            {
                case "Erzeuger":
                    WErzeugerModel kessel = a.Erzeuger.First(m => m.ID_Type == WizardItemClass.KESSEL_TYP);
                    kessel.Vorlauf += 1;
                    return "Tab_Energieanlagen";
                case "Prozess":
                    a.Prozess[0].Summe += 1;
                    return "Z_Projekt_Prozesswaerme";
                case "Stromganglinie":
                    a.Stromganglinie.Clear();
                    return "Z_ProjektStromganglinie";
                case "Waermebedarf":
                    a.Waermebedarf[0].Kanal = DbWerte.KANAL_PROZESS;
                    return "Z_ProjektWaermebedarf";
                case "Stromverbraucher":
                    a.Stromverbraucher[0].m_Summe += 1;
                    return "Z_Projekt_Stromverbraucher";
                case "Kopf":
                    a.Kopf[0].Beschreibung = (a.Kopf[0].Beschreibung ?? "") + " (geaendert)";
                    return "Tab_Projekt";
                default:
                    throw new ArgumentException(was);
            }
        }

        /// <summary>Tabellen, die ein Neuschreiben der Anlagen zwangsläufig berührt (neue Anlagen-Ids).</summary>
        private static readonly HashSet<string> ERZEUGER_NEBENWIRKUNG = new HashSet<string>(StringComparer.Ordinal)
        {
            "Tab_Energieanlagen", "Z_AnlageSenke", "Z_AnlageStrang", "Tab_ProjektWerte",
            "Tab_WP", "Tab_Heizkessel", "energy_price", "energy_project_settings"
        };

        /// <summary>Die projektgebundenen Tabellen samt Projektspalte — MIT Ids, ohne Zeitpunktspalten.</summary>
        private static readonly string[] TABELLEN =
        {
            "Tab_Projekt:ID",
            "Tab_Energieanlagen:ID_Projekt",
            "Z_ProjektGebaeude:ID_Projekt",
            "Z_ProjektWaermebedarf:ID_Projekt",
            "Z_Projekt_Prozesswaerme:ID_Projekt",
            "Z_Projekt_Stromverbraucher:ID_Projekt",
            "Z_ProjektStromganglinie:ID_Projekt",
            "Tab_Gebaeude:ID_Projekt",
            "Tab_WP:ID_Projekt",
            "Tab_Heizkessel:ID_Projekt",
            "Tab_Pufferspeicher:ID_Projekt",
            "Tab_Prozesswaerme:ID_Projekt",
            "Tab_Waermebedarf:ID_Projekt",
            "Tab_Stromganglinie:ID_Projekt",
            "Tab_Stromverbraucher:ID_Projekt",
            "Tab_ProjektWerte:ProjektID",
            "energy_price:ID_Projekt",
            "energy_project_settings:ID_Projekt",
        };

        private static Dictionary<string, string> Abbilder(int idProjekt)
        {
            Dictionary<string, string> a = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string eintrag in TABELLEN)
            {
                string[] teile = eintrag.Split(':');
                if (!DataRepository.TabelleVorhanden(teile[0])) continue;
                a[teile[0]] = Tabelle(
                    "SELECT * FROM [" + teile[0] + "] WHERE [" + teile[1] + "] = ? ORDER BY 1", idProjekt);
            }
            foreach (string t in new[] { "Z_AnlageSenke", "Z_AnlageStrang" })
            {
                if (!DataRepository.TabelleVorhanden(t)) continue;
                a[t] = Tabelle("SELECT s.* FROM [" + t + "] s JOIN Tab_Energieanlagen e ON e.ID = s.ID_Anlage " +
                               "WHERE e.ID_Projekt = ? ORDER BY 1", idProjekt);
            }
            return a;
        }

        private static string Abbild(int idProjekt)
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, string> t in Abbilder(idProjekt))
                sb.Append("== ").Append(t.Key).Append('\n').Append(t.Value);
            return sb.ToString();
        }

        private static string Tabelle(string sql, int idProjekt)
        {
            return Tabelle(sql, new DbParam("@p", idProjekt));
        }

        private static string Tabelle(string sql, params DbParam[] parameter)
        {
            DataTable dt = DataRepository.GetDataTable(sql, parameter);
            StringBuilder sb = new StringBuilder();
            if (dt == null) return "";
            foreach (DataRow r in dt.Rows)
            {
                foreach (DataColumn c in dt.Columns)
                {
                    // Zeitpunkte bleiben draussen (Aenderungsdatum, valid_from …) - das
                    // Aenderungsdatum pruefen die Faelle eigens.
                    if (c.DataType == typeof(DateTime)) continue;
                    if (c.ColumnName.IndexOf("datum", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    if (c.ColumnName.IndexOf("valid_", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    sb.Append(c.ColumnName).Append('=')
                      .Append(Convert.ToString(r[c], CultureInfo.InvariantCulture)).Append('|');
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        private static int Zeilenzahl(string abbild)
        {
            return abbild.Count(ch => ch == '\n');
        }

        /// <summary>Die Pufferzeilen (ID_Type 12) aus dem Abbild der Anlagen — mit Ids.</summary>
        private static string[] Pufferzeilen(Dictionary<string, string> abbilder)
        {
            return abbilder["Tab_Energieanlagen"].Split('\n')
                .Where(z => z.Contains("|ID_Type=" + WizardItemClass.PUFFER_TYP + "|"))
                .ToArray();
        }

        /// <summary>Die Stammfelder der WP-Projektkopien, die der Assistent nachzieht.</summary>
        private static string[] WpStammfelder(int idProjekt)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, Firma, Beschreibung, Typ, Regelung, Aufstellung, Baujahr, Nennleistung " +
                "FROM Tab_WP WHERE ID_Projekt = ? ORDER BY ID", new DbParam("@p", idProjekt));
            List<string> felder = new List<string>();
            if (dt != null)
                foreach (DataRow r in dt.Rows)
                    foreach (DataColumn c in dt.Columns)
                        felder.Add(c.ColumnName + "=" + Convert.ToString(r[c], CultureInfo.InvariantCulture));
            return felder.ToArray();
        }

        /// <summary>Zahl der Projekteinstellungssätze des Projekts zu einem Träger.</summary>
        private static int Saetze(int traeger)
        {
            object n = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM energy_Project_settings WHERE ID_Projekt = ? AND ID_Energieträger = ?",
                new DbParam("@p", ID), new DbParam("@c", traeger));
            return Convert.ToInt32(n, CultureInfo.InvariantCulture);
        }

        /// <summary>Die Sätze der ÜBRIGEN Träger des Projekts — mit Ids, ohne Zeitpunkte.</summary>
        private static string AndereSaetze(int traeger)
        {
            return Tabelle("SELECT * FROM energy_Project_settings WHERE ID_Projekt = ? AND ID_Energieträger <> ? ORDER BY 1",
                           new DbParam("@p", ID), new DbParam("@c", traeger))
                 + Tabelle("SELECT * FROM energy_price WHERE id_projekt = ? AND carrier_id <> ? ORDER BY 1",
                           new DbParam("@p", ID), new DbParam("@c", traeger));
        }

        /// <summary>(Zuordnungs-Id, Verweis) je Zeile einer Zuordnungstabelle des Projekts, sortiert.</summary>
        private static List<(int, int)> Paare(string tabelle, string idSpalte, string verweisSpalte)
        {
            return Paare(tabelle, idSpalte, verweisSpalte, ID);
        }

        private static List<(int, int)> Paare(string tabelle, string idSpalte, string verweisSpalte, int idProjekt)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT [" + idSpalte + "], [" + verweisSpalte + "] FROM [" + tabelle + "] WHERE ID_Projekt = ?",
                new DbParam("@p", idProjekt));
            List<(int, int)> paare = new List<(int, int)>();
            foreach (DataRow r in dt.Rows)
                paare.Add((Convert.ToInt32(r[0], CultureInfo.InvariantCulture),
                           Convert.ToInt32(r[1], CultureInfo.InvariantCulture)));
            return Sortiert(paare);
        }

        private static List<(int, int)> Sortiert(IEnumerable<(int, int)> paare)
        {
            return paare.OrderBy(p => p.Item1).ThenBy(p => p.Item2).ToList();
        }

        private static string Projektname(int idProjekt)
        {
            object n = DataRepository.ExecuteScalar("SELECT Projektname FROM Tab_Projekt WHERE ID = ?",
                                                    new DbParam("@p", idProjekt));
            Assert.NotNull(n);
            return Convert.ToString(n, CultureInfo.InvariantCulture);
        }

        /// <summary>Setzt das Änderungsdatum auf einen alten Stand und liefert ihn, wie die Datenbank ihn liest.</summary>
        private static DateTime? DatumSetzen()
        {
            return DatumSetzen(ID);
        }

        private static DateTime? DatumSetzen(int id)
        {
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Projekt SET Aenderungsdatum = ? WHERE ID = ?",
                new DbParam("@d", DbParamTyp.Date) { Wert = ALT }, new DbParam("@p", id)));
            DateTime? gelesen = MerkmalUebernahmeCtrl.Aenderungsdatum(id);
            Assert.True(gelesen.HasValue);
            return gelesen;
        }
    }
}
