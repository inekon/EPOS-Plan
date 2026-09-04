using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die DATENSEITE des Projektassistenten (iU9-W16a.4, K3) — Seitenschaltung,
    /// Filter, Ladewege und der Speicherlauf.
    ///
    /// <para><b>Warum es diese Fälle gibt.</b> Bis W16a stand all das im
    /// RAHMENFENSTER und war damit nur am Windows-Gerät prüfbar: Der Referenzlauf
    /// rechnet einen BESTEHENDEN Projektstand nach, er legt kein Projekt an und
    /// schreibt keines fort. Für die 340 Zeilen, die diese Welle verschiebt, gab es
    /// vorher keinen einzigen Test.</para>
    ///
    /// <para><b>Die Fälle ohne Datenbank stehen vorn</b> (Seitenschaltung, Filter,
    /// Meldungstexte); danach kommen die lesenden mit geteilter Arbeitskopie und
    /// zuletzt der SCHREIBENDE mit einer EIGENEN Kopie je Probe.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class AssistentCtrlTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public AssistentCtrlTests(TestDatenbank db)
        {
            _db = db;
        }

        // =========================================================================
        // Seitenschaltung - ohne Datenbank
        // =========================================================================

        /// <summary>
        /// Der Anfangszustand ist der des Rahmenkonstruktors: Komponentenschritt und
        /// Projektkopf an, die elf Fachseiten aus.
        /// </summary>
        [Fact]
        public void Am_Anfang_sind_genau_zwei_Seiten_frei()
        {
            AssistentCtrl a = new AssistentCtrl();

            Assert.True(a.SeiteAktiv(WizardItemClass.KOMPONENTEN_ITEM));
            Assert.True(a.SeiteAktiv(WizardItemClass.PROJEKT_ITEM));

            for (int i = WizardItemClass.GEBAEUDE_ITEM; i < AssistentCtrl.SEITEN; i++)
                Assert.False(a.SeiteAktiv(i));

            Assert.Equal(AssistentCtrl.BETRIEBSART_NEU, a.Betriebsart);
            Assert.Equal(0, a.ProjektId);
            Assert.False(a.Gespeichert);
        }

        /// <summary>
        /// „Weiter" überspringt jede abgeschaltete Seite — der Ersatz für
        /// <c>GetNextUpIndex</c>.
        /// </summary>
        [Fact]
        public void Weiter_geht_nur_ueber_aktive_Seiten()
        {
            AssistentCtrl a = new AssistentCtrl();
            a.SeiteSchalten(WizardItemClass.BHKW_ITEM, true);

            // Vom Anfang (-1) auf den Komponentenschritt, von dort auf den Projektkopf.
            Assert.Equal(WizardItemClass.KOMPONENTEN_ITEM, a.NaechsteAktive(-1, +1));
            Assert.Equal(WizardItemClass.PROJEKT_ITEM,
                         a.NaechsteAktive(WizardItemClass.KOMPONENTEN_ITEM, +1));

            // Gebaeude bis Kessel sind aus, BHKW ist an - und BHKW ist die letzte Seite.
            Assert.Equal(WizardItemClass.BHKW_ITEM,
                         a.NaechsteAktive(WizardItemClass.PROJEKT_ITEM, +1));
        }

        /// <summary>„Zurück" ebenso — der Ersatz für <c>GetNextDownIndex</c>.</summary>
        [Fact]
        public void Zurueck_geht_nur_ueber_aktive_Seiten()
        {
            AssistentCtrl a = new AssistentCtrl();
            a.SeiteSchalten(WizardItemClass.WP_ITEM, true);

            Assert.Equal(WizardItemClass.WP_ITEM,
                         a.NaechsteAktive(WizardItemClass.BHKW_ITEM, -1));
            Assert.Equal(WizardItemClass.PROJEKT_ITEM,
                         a.NaechsteAktive(WizardItemClass.WP_ITEM, -1));
            Assert.Equal(WizardItemClass.KOMPONENTEN_ITEM,
                         a.NaechsteAktive(WizardItemClass.PROJEKT_ITEM, -1));
        }

        /// <summary>
        /// Auf der letzten aktiven Seite wird „Weiter" zu „Speichern" — wörtlich
        /// <c>lastIndex</c>.
        /// </summary>
        [Fact]
        public void Der_Weiter_Knopf_wird_auf_der_letzten_aktiven_Seite_zum_Speichern()
        {
            AssistentCtrl a = new AssistentCtrl();

            // Nur die zwei Anfangsseiten: der Projektkopf ist schon die letzte.
            Assert.False(a.LetzteAktive(WizardItemClass.KOMPONENTEN_ITEM));
            Assert.True(a.LetzteAktive(WizardItemClass.PROJEKT_ITEM));

            a.SeiteSchalten(WizardItemClass.SOLAR_ITEM, true);
            Assert.False(a.LetzteAktive(WizardItemClass.PROJEKT_ITEM));
            Assert.True(a.LetzteAktive(WizardItemClass.SOLAR_ITEM));

            // Die allerletzte Seite ist es immer.
            Assert.True(a.LetzteAktive(WizardItemClass.BHKW_ITEM));
        }

        /// <summary>
        /// Brauchwasser und Pufferspeicher melden <c>-1</c> als Seitenindex — das darf
        /// nichts umschalten und nicht werfen.
        /// </summary>
        [Fact]
        public void Ein_Index_ohne_Seite_schaltet_nichts()
        {
            AssistentCtrl a = new AssistentCtrl();

            a.SeiteSchalten(-1, true);
            a.SeiteSchalten(99, true);

            Assert.False(a.SeiteAktiv(-1));
            Assert.False(a.SeiteAktiv(99));
        }

        // =========================================================================
        // Die beiden Filter - ohne Datenbank
        // =========================================================================

        /// <summary>
        /// Der Anlagenfilter — inklusive der P5/E3-Zeile für den SPITZENKESSEL, die im
        /// Vorläufer fehlte: Ein abgewählter Kessel ließ seine Anlagenzeilen stehen,
        /// der Speicherweg legte sie danach wieder an.
        /// </summary>
        [Theory]
        [InlineData(WizardItemClass.KESSEL_TYP, WizardItemClass.KESSEL_ITEM)]
        [InlineData(WizardItemClass.WP_TYP, WizardItemClass.WP_ITEM)]
        [InlineData(WizardItemClass.BHKW_TYP, WizardItemClass.BHKW_ITEM)]
        [InlineData(WizardItemClass.SOLAR_TYP, WizardItemClass.SOLAR_ITEM)]
        [InlineData(WizardItemClass.PV_TYP, WizardItemClass.PV_ITEM)]
        [InlineData(WizardItemClass.SP_TYP, WizardItemClass.SP_ITEM)]
        public void Eine_abgewaehlte_Anlage_faellt_aus_der_Liste(int typ, int seite)
        {
            AssistentCtrl a = new AssistentCtrl();
            WErzeugerModel anlage = new WErzeugerModel { ID_Type = typ };

            // Seite aus: die Zeile faellt.
            Assert.True(a.NichtAktivesElement(anlage));

            // Seite an: sie bleibt.
            a.SeiteSchalten(seite, true);
            Assert.False(a.NichtAktivesElement(anlage));
        }

        /// <summary>
        /// FR-1: Pufferzeilen fallen IMMER aus der Liste — der Assistent führt keine
        /// Pufferseite, und <c>Del_Projekt_Waermeerzeuger</c> verschont ID_Type 12.
        /// Blieben sie stehen, legte <c>Add_WP_Waermeerzeuger</c> sie doppelt an.
        /// </summary>
        [Fact]
        public void Pufferzeilen_fallen_immer_aus_der_Liste()
        {
            AssistentCtrl a = new AssistentCtrl();
            WErzeugerModel puffer = new WErzeugerModel { ID_Type = WizardItemClass.PUFFER_TYP };

            Assert.True(a.NichtAktivesElement(puffer));

            // Auch wenn ALLE Seiten frei sind.
            for (int i = 0; i < AssistentCtrl.SEITEN; i++) a.SeiteSchalten(i, true);
            Assert.True(a.NichtAktivesElement(puffer));
        }

        /// <summary>
        /// Referenzanlagen (ID_Type 5…9) zählen nirgends mit und bleiben unberührt.
        /// </summary>
        [Theory]
        [InlineData(WizardItemClass.REF_KESSEL_TYP)]
        [InlineData(WizardItemClass.REF_WP_TYP)]
        [InlineData(WizardItemClass.REF_PV_TYP)]
        public void Referenzanlagen_bleiben_stehen(int typ)
        {
            AssistentCtrl a = new AssistentCtrl();

            Assert.False(a.NichtAktivesElement(new WErzeugerModel { ID_Type = typ }));
        }

        /// <summary>
        /// Die fünf Zuordnungslisten werden geleert, sobald ihre Seite abgewählt ist —
        /// vor P5 hing das vom Zufall ab (nie besuchte Seiten galten als leer,
        /// Stromganglinie und Stromverbraucher wurden gar nicht angetastet).
        /// </summary>
        [Fact]
        public void Abgewaehlte_Zuordnungen_werden_geleert()
        {
            AssistentCtrl a = new AssistentCtrl();
            a.Gebaeude.Add(new Z_ProjGebModel());
            a.Waermebedarf.Add(new Z_ProjWaermebedarfModel());
            a.Prozess.Add(new Z_ProjektProzesswaermeModel());
            a.Stromverbraucher.Add(new Z_ProjektStromverbraucherModel());
            a.Stromganglinie.Add(new Z_ProjektStromganglinieModel());

            // Nur Gebaeude bleibt frei.
            a.SeiteSchalten(WizardItemClass.GEBAEUDE_ITEM, true);
            a.EntferneNichtAktiveZuordnungen();

            Assert.Single(a.Gebaeude);
            Assert.Empty(a.Waermebedarf);
            Assert.Empty(a.Prozess);
            Assert.Empty(a.Stromverbraucher);
            Assert.Empty(a.Stromganglinie);
        }

        // =========================================================================
        // Der Projektkopf - ohne Datenbank
        // =========================================================================

        [Fact]
        public void Der_Projektkopf_wandert_Feld_fuer_Feld_in_den_Projektsatz()
        {
            AssistentCtrl a = new AssistentCtrl();
            a.Kopf[0].Name = "Laurentiuskirche";
            a.Kopf[0].Beschreibung = "Denkmalschutz";
            a.Kopf[0].Kunde = "Kirchengemeinde";
            a.Kopf[0].Bearbeiter = "M. Muster";
            a.Kopf[0].Erstelldatum = new DateTime(2020, 1, 12);
            a.Kopf[0].IdKlimaregion = 12;

            DateTime vorher = DateTime.Now.AddSeconds(-1);
            a.ProjektkopfUebernehmen();

            Assert.Equal("Laurentiuskirche", a.Projekt.m_szProjektname);
            Assert.Equal("Denkmalschutz", a.Projekt.m_szBeschreibung);
            Assert.Equal("Kirchengemeinde", a.Projekt.m_szKunde);
            Assert.Equal("M. Muster", a.Projekt.m_szBearbeiter);
            Assert.Equal(new DateTime(2020, 1, 12), a.Projekt.m_Erstelldatum);
            Assert.Equal(12, a.Projekt.m_ID_Klimaregion);

            // Das Aenderungsdatum steht ausdruecklich auf JETZT - das war die Absicht
            // hinter dem alten GetDatum(), das DateTime.Now statt des Feldes lieferte.
            Assert.True(a.Projekt.m_Aenderungsdatum >= vorher);
        }

        // =========================================================================
        // Die Pflichtpruefungen - ohne Datenbankzugriff (sie kehren vorher um)
        // =========================================================================

        [Fact]
        public void Ohne_Klimazone_wird_nicht_geschrieben()
        {
            WizardCtrl vorher = WizardCtrl.Aktueller;
            try
            {
                WizardCtrl.Aktueller = new WizardCtrl();

                AssistentCtrl a = new AssistentCtrl();
                a.Kopf[0].Name = "Irgendwas";
                a.Kopf[0].Klimaname = "";

                AssistentErgebnis e = a.Speichern();

                Assert.Equal(AssistentAusgang.KlimazoneFehlt, e.Ausgang);
                Assert.False(e.Erfolg);
                Assert.False(a.Gespeichert);
            }
            finally { WizardCtrl.Aktueller = vorher; }
        }

        [Fact]
        public void Ohne_Projektnamen_wird_nicht_geschrieben()
        {
            WizardCtrl vorher = WizardCtrl.Aktueller;
            try
            {
                WizardCtrl.Aktueller = new WizardCtrl();

                AssistentCtrl a = new AssistentCtrl();
                a.Kopf[0].Name = "";
                a.Kopf[0].Klimaname = "Region 12 Mannheim";

                AssistentErgebnis e = a.Speichern();

                Assert.Equal(AssistentAusgang.ProjektnameFehlt, e.Ausgang);
                Assert.False(a.Gespeichert);
            }
            finally { WizardCtrl.Aktueller = vorher; }
        }

        // =========================================================================
        // Die Meldungen - Befund W16-B17: vier deutsche Literale werden Schluessel
        // =========================================================================

        [Fact]
        public void Die_Pflichtmeldungen_stehen_in_beiden_Sprachen()
        {
            CultureInfo vorher = CultureInfo.CurrentUICulture;
            CultureInfo katalogVorher = Resource.Culture;
            try
            {
                AssistentErgebnis klima = Ergebnis(AssistentAusgang.KlimazoneFehlt);
                AssistentErgebnis name = Ergebnis(AssistentAusgang.ProjektnameFehlt);

                Sprache("de-DE");
                Assert.Equal("Bitte eine Klimazone auswählen!", AssistentCtrl.Meldungstext(klima));
                Assert.Equal("Klimazone fehlt", AssistentCtrl.Meldungstitel(klima));
                Assert.Equal("Bitte einen Projektnamen eingeben!", AssistentCtrl.Meldungstext(name));
                Assert.Equal("Projektname fehlt", AssistentCtrl.Meldungstitel(name));

                Sprache("en-US");
                Assert.Equal("Please select a climate zone!", AssistentCtrl.Meldungstext(klima));
                Assert.Equal("Climate zone missing", AssistentCtrl.Meldungstitel(klima));
                Assert.Equal("Please enter a project name!", AssistentCtrl.Meldungstext(name));
                Assert.Equal("Project name missing", AssistentCtrl.Meldungstitel(name));
            }
            finally
            {
                Resource.Culture = katalogVorher;
                Thread.CurrentThread.CurrentUICulture = vorher;
                CultureInfo.CurrentUICulture = vorher;
            }
        }

        /// <summary>
        /// Die EINE Fehlermeldung des Speicherwegs (Entscheid E-4) nennt den Schritt,
        /// an dem es lag — der Vorläufer schwieg siebzehnmal.
        /// </summary>
        [Fact]
        public void Die_Fehlermeldung_nennt_den_Schritt()
        {
            CultureInfo vorher = CultureInfo.CurrentUICulture;
            CultureInfo katalogVorher = Resource.Culture;
            try
            {
                AssistentErgebnis e = Ergebnis(AssistentAusgang.Fehlgeschlagen, "Add_Stromganglinie");

                Sprache("de-DE");
                string text = AssistentCtrl.Meldungstext(e);
                Assert.Contains("Add_Stromganglinie", text);
                Assert.Contains("nicht vollständig gespeichert", text);
                Assert.Equal("Speichern fehlgeschlagen", AssistentCtrl.Meldungstitel(e));

                Sprache("en-US");
                Assert.Contains("Add_Stromganglinie", AssistentCtrl.Meldungstext(e));
                Assert.Equal("Saving failed", AssistentCtrl.Meldungstitel(e));
            }
            finally
            {
                Resource.Culture = katalogVorher;
                Thread.CurrentThread.CurrentUICulture = vorher;
                CultureInfo.CurrentUICulture = vorher;
            }
        }

        [Fact]
        public void Ein_gelungener_Lauf_meldet_nichts()
        {
            AssistentErgebnis e = Ergebnis(AssistentAusgang.Gespeichert);

            Assert.Equal("", AssistentCtrl.Meldungstext(e));
            Assert.Equal("", AssistentCtrl.Meldungstitel(e));
            Assert.True(e.Erfolg);
        }

        // =========================================================================
        // Die Ladewege - LESEND, geteilte Arbeitskopie
        // =========================================================================

        /// <summary>
        /// Die sechs Ladewege gegen ein Referenzprojekt: Was
        /// <see cref="KomponentenBestandCtrl"/> zählt, muss auch hier ankommen — beide
        /// lesen dieselben Tabellen.
        /// </summary>
        [Fact]
        public void Die_sechs_Ladewege_fuellen_die_Listen_des_Projekts()
        {
            if (!_db.Vorhanden) return;

            AssistentCtrl a = new AssistentCtrl();
            a.Betriebsart = AssistentCtrl.BETRIEBSART_BEARBEITEN;
            a.Laden("Referenz BHKW-Kaskade (Regressionstest)");

            Assert.True(a.BereitsGeladen);

            KomponentenBestandCtrl bestand = KomponentenBestandCtrl.Lesen(1030);

            Assert.Equal(bestand[KomponentenBestandCtrl.GEBAEUDE].Anzahl, a.Gebaeude.Count);
            Assert.Equal(bestand[KomponentenBestandCtrl.WAERMEBEDARF].Anzahl, a.Waermebedarf.Count);
            Assert.Equal(bestand[KomponentenBestandCtrl.PROZESS].Anzahl, a.Prozess.Count);
            Assert.Equal(bestand[KomponentenBestandCtrl.STROMSTD].Anzahl, a.Stromverbraucher.Count);
            Assert.Equal(bestand[KomponentenBestandCtrl.STROMLASTGANG].Anzahl, a.Stromganglinie.Count);

            // Die Anlagen kommen VOLLSTAENDIG herein (kein Teilkopieren mehr).
            Assert.NotEmpty(a.Erzeuger);
            Assert.All(a.Erzeuger, e => Assert.Equal(1030, e.ID_Projekt));
        }

        /// <summary>Ein leerer Projektname lädt nichts — das ist der Neu-Zweig.</summary>
        [Fact]
        public void Ohne_Projektnamen_wird_nichts_geladen()
        {
            if (!_db.Vorhanden) return;

            AssistentCtrl a = new AssistentCtrl();
            a.Laden("");

            Assert.Empty(a.Erzeuger);
            Assert.Empty(a.Gebaeude);
            Assert.Empty(a.Waermebedarf);
            Assert.Empty(a.Prozess);
            Assert.Empty(a.Stromverbraucher);
            Assert.Empty(a.Stromganglinie);
        }

        // =========================================================================
        // Der Speicherlauf - SCHREIBEND, EIGENE Arbeitskopie je Probe
        // =========================================================================

        /// <summary>
        /// NACHWEIS zu Risiko R-W16-6: Ein Bearbeiten-Lauf OHNE Änderung lässt das
        /// Projekt inhaltlich stehen.
        ///
        /// <para>Der Speicherweg ist „Löschen + Neuanlegen" je Gewerk; die
        /// Autowert-Ids ändern sich dabei zwangsläufig. Verglichen wird deshalb der
        /// INHALT: Zahl und Bezeichner der Anlagen je Typ, die fünf
        /// Zuordnungstabellen und die Kopffelder des Projekts.</para>
        ///
        /// <para>Die Probe läuft auf einer EIGENEN Arbeitskopie — sie schreibt, und die
        /// Vergleichsbasis der lesenden Fälle darf sie nicht verschieben.</para>
        /// </summary>
        [Fact]
        public void Ein_Bearbeiten_Lauf_ohne_Aenderung_laesst_das_Projekt_stehen()
        {
            using (TestDatenbank eigen = new TestDatenbank())
            {
                if (!eigen.Vorhanden) return;

                WizardCtrl vorherCtrl = WizardCtrl.Aktueller;
                try
                {
                    WizardCtrl.Aktueller = new WizardCtrl();

                    const string NAME = "Referenz BHKW-Kaskade (Regressionstest)";
                    const int ID = 1030;

                    Dictionary<string, int> vorher = Zaehlstand(ID);
                    int bitmaskeVorher = KomponentenBestandCtrl.Lesen(ID).Bitmaske;
                    string[] anlagenVorher = Anlagenbezeichner(ID);

                    AssistentCtrl a = new AssistentCtrl();
                    a.Betriebsart = AssistentCtrl.BETRIEBSART_BEARBEITEN;
                    a.ProjektId = ID;
                    a.Laden(NAME);

                    // Alle dreizehn Seiten so stellen, wie der Komponentenschritt sie
                    // aus dem Bestand stellen wuerde - sonst wirft der Filter weg, was
                    // im Projekt steht.
                    KomponentenBestandCtrl bestand = KomponentenBestandCtrl.Lesen(ID);
                    for (int k = 0; k < KomponentenBestandCtrl.ANZAHL; k++)
                        a.SeiteSchalten(bestand[k].SeitenIndex, bestand[k].Vorhanden);

                    // Der Projektkopf, wie ihn die erste Seite liefern wuerde.
                    ProjektKopfDaten kopf = ProjektCtrl.Kopf(NAME);
                    Assert.NotNull(kopf);
                    a.Kopf[0].Name = kopf.Name;
                    a.Kopf[0].Beschreibung = kopf.Beschreibung;
                    a.Kopf[0].Kunde = kopf.Kunde;
                    a.Kopf[0].Bearbeiter = kopf.Bearbeiter;
                    a.Kopf[0].Erstelldatum = kopf.Erstelldatum;
                    a.Kopf[0].IdKlimaregion = kopf.IdKlimaregion;
                    a.Kopf[0].Klimaname = kopf.Klimaname;

                    AssistentErgebnis e = a.Speichern();

                    Assert.True(e.Erfolg, "Speichern scheiterte an: " + e.Schritt);
                    Assert.True(a.Gespeichert);

                    // Inhaltlich unveraendert.
                    Assert.Equal(vorher, Zaehlstand(ID));
                    Assert.Equal(bitmaskeVorher, KomponentenBestandCtrl.Lesen(ID).Bitmaske);
                    Assert.Equal(anlagenVorher, Anlagenbezeichner(ID));

                    ProjektKopfDaten nachher = ProjektCtrl.Kopf(NAME);
                    Assert.Equal(kopf.Name, nachher.Name);
                    Assert.Equal(kopf.Beschreibung, nachher.Beschreibung);
                    Assert.Equal(kopf.Kunde, nachher.Kunde);
                    Assert.Equal(kopf.Bearbeiter, nachher.Bearbeiter);
                    Assert.Equal(kopf.Klimaname, nachher.Klimaname);
                }
                finally { WizardCtrl.Aktueller = vorherCtrl; }
            }
        }

        // =========================================================================
        // Hilfen
        // =========================================================================

        private static readonly string[] TABELLEN =
        {
            "Z_ProjektGebaeude", "Z_ProjektWaermebedarf", "Z_Projekt_Prozesswaerme",
            "Z_Projekt_Stromverbraucher", "Z_ProjektStromganglinie",
            "Z_Projekt_Brauchwasser", "Tab_Energieanlagen"
        };

        private static Dictionary<string, int> Zaehlstand(int idProjekt)
        {
            Dictionary<string, int> stand = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string t in TABELLEN)
            {
                object v = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM " + t + " WHERE ID_Projekt = ?",
                    new DbParam("@id", idProjekt));
                stand[t] = v == null ? 0 : Convert.ToInt32(v);
            }
            return stand;
        }

        private static string[] Anlagenbezeichner(int idProjekt)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID_Type, Bezeichner FROM Tab_Energieanlagen WHERE ID_Projekt = ? " +
                "ORDER BY ID_Type, Bezeichner",
                new DbParam("@id", idProjekt));

            List<string> zeilen = new List<string>();
            if (dt != null)
                foreach (DataRow r in dt.Rows)
                    zeilen.Add(r["ID_Type"] + "|" + r["Bezeichner"]);
            return zeilen.ToArray();
        }

        private static AssistentErgebnis Ergebnis(AssistentAusgang ausgang, string schritt = "")
        {
            // Der Konstruktor ist internal; EPOS.Kern.Tests sieht ihn (InternalsVisibleTo).
            return (AssistentErgebnis)Activator.CreateInstance(
                typeof(AssistentErgebnis),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, new object[] { ausgang, schritt }, null);
        }

        private static void Sprache(string kuerzel)
        {
            CultureInfo k = new CultureInfo(kuerzel);
            Thread.CurrentThread.CurrentUICulture = k;
            CultureInfo.CurrentUICulture = k;
            Resource.Culture = k;
        }
    }
}
