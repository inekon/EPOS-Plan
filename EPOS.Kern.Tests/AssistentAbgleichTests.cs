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

        /// <summary>
        /// Ein Lauf im BEARBEITEN-Zweig, so gestellt, wie ihn der Komponentenschritt und
        /// die Kopfseite stellen würden (wie in <c>AssistentCtrlTests</c>).
        /// </summary>
        private static AssistentCtrl Bearbeitenlauf(string name)
        {
            AssistentCtrl a = new AssistentCtrl();
            a.Betriebsart = AssistentCtrl.BETRIEBSART_BEARBEITEN;
            a.ProjektId = ID;
            a.Laden(name);

            KomponentenBestandCtrl bestand = KomponentenBestandCtrl.Lesen(ID);
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
            foreach (WErzeugerModel m in a.Erzeuger)
                if (m.ID_Type == WizardItemClass.WP_TYP)
                    WaermepumpeGeraeteCtrl.GeraetedatenFuellen(m, m.ID_WP);
            Z_ProjektGebGanglinieCtrl.KanaeleNachladen(ID, a.Waermebedarf);
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
            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("@p", idProjekt));
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
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Projekt SET Aenderungsdatum = ? WHERE ID = ?",
                new DbParam("@d", DbParamTyp.Date) { Wert = ALT }, new DbParam("@p", ID)));
            DateTime? gelesen = MerkmalUebernahmeCtrl.Aenderungsdatum(ID);
            Assert.True(gelesen.HasValue);
            return gelesen;
        }
    }
}
