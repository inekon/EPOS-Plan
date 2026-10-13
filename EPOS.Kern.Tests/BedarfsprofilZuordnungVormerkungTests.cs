using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Projektkopie einer entfernten Bedarfszeile wird abgeräumt, obwohl die Zuordnungen erst der
    /// Aufrufer schreibt</b> (Nachzug der Katalogauswahl, Konzept Katalogauswahl 5.13) — gegen eine
    /// Arbeitskopie der Testdatenbank, für jede der drei Arten mit Zuordnungen nach dem Schließen.
    ///
    /// <para><b>Der Befund.</b> Bei Stromverbrauchern, Prozesswärme und Kältebedarf schreibt der Dialog
    /// seine Zuordnungen nicht selbst; das tut der Kachelweg der Startseite nach dem Schließen. Der
    /// Löschweg des Kerns (<see cref="BedarfStammCtrl.ProjektkopieEntfernen"/>) räumt eine Kopie nur ab,
    /// wenn in der DATENBANK keine Zuordnung mehr auf sie zeigt — die alte Zuordnung hielt sie fest, und
    /// die Kopie blieb als Waise im Projekt.</para>
    ///
    /// <para><b>Der Weg.</b> <see cref="Projektkopievormerkung.Schliessen"/> schreibt die Zuordnungen des
    /// Aufrufers VOR dem Bestandsabgleich und dem Abschluss; die Hülle der Bedarfsprofile reicht ihren
    /// Schreibweg dafür herein (Wache in <c>EPOS.UI.Tests/Dialoge/BedarfsprofilVormerkungWacheTests</c>).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BedarfsprofilZuordnungVormerkungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Die drei Arten, deren Zuordnungen erst nach dem Schließen geschrieben werden.</summary>
        public static TheoryData<BedarfsArt> Arten() => new()
        {
            BedarfsArt.Stromverbraucher, BedarfsArt.Prozesswaerme, BedarfsArt.Kaelte
        };

        /// <summary>
        /// <b>Der Befund.</b> Wird die Vormerkung VOR dem Schreiben abgeschlossen, bleibt die Projektkopie der
        /// entfernten Zeile stehen — ohne Zeile und ohne Zuordnung.
        /// </summary>
        [Theory]
        [MemberData(nameof(Arten))]
        public void Der_Abschluss_vor_dem_Schreiben_laesst_die_Projektkopie_als_Waise_stehen(BedarfsArt art)
        {
            if (!_db.Vorhanden) return;
            (int projekt, string nameA, int kopieA, string nameB, int kopieB) =
                Vorbereiten(art, "NK3 Waise " + art);

            Projektkopievormerkung v = Vormerkung(art, projekt);
            Func<int, bool> nochReferenziert = id => id == kopieB;

            // Die alte Reihenfolge: abschließen, dann schreiben.
            v.EntferntGegenueber(Bestand(nameA, kopieA, nameB, kopieB), nochReferenziert);
            v.Abschliessen(true, nochReferenziert);
            Schreiben(art, projekt, (nameB, kopieB));

            Assert.Equal(kopieA, BedarfStammCtrl.ProjektkopieId(art, nameA, projekt));
            Assert.Equal(0, Zuordnungen(art, projekt, kopieA));
        }

        /// <summary>
        /// <b>Der Weg.</b> Mit <see cref="Projektkopievormerkung.Schliessen"/> stehen die neuen Zuordnungen,
        /// wenn der Löschweg fragt: Die Kopie der entfernten Zeile geht samt Typprofil, die der gebliebenen bleibt.
        /// </summary>
        [Theory]
        [MemberData(nameof(Arten))]
        public void Schliessen_raeumt_die_Projektkopie_der_entfernten_Zeile_ab(BedarfsArt art)
        {
            if (!_db.Vorhanden) return;
            (int projekt, string nameA, int kopieA, string nameB, int kopieB) =
                Vorbereiten(art, "NK3 Abschluss " + art);

            Projektkopievormerkung v = Vormerkung(art, projekt);

            v.Schliessen(true, () => Schreiben(art, projekt, (nameB, kopieB)),
                         Bestand(nameA, kopieA, nameB, kopieB), id => id == kopieB);

            Assert.True(BedarfStammCtrl.ProjektkopieId(art, nameA, projekt) <= 0,
                        art + ": die Kopie der entfernten Zeile steht noch.");
            Assert.Equal(kopieB, BedarfStammCtrl.ProjektkopieId(art, nameB, projekt));
            Assert.Equal(1, Zuordnungen(art, projekt, kopieB));
        }

        /// <summary>
        /// <b>Abbrechen.</b> Ohne OK wird nichts geschrieben; die in der Sitzung NEU übernommene Kopie geht,
        /// die Kopie der entfernten Zeile und die alten Zuordnungen bleiben.
        /// </summary>
        [Theory]
        [MemberData(nameof(Arten))]
        public void Abbrechen_schreibt_keine_Zuordnung_und_raeumt_nur_die_neue_Kopie_ab(BedarfsArt art)
        {
            if (!_db.Vorhanden) return;
            (int projekt, string nameA, int kopieA, string nameB, int kopieB) =
                Vorbereiten(art, "NK3 Abbruch " + art);
            string nameC = Katalogname(art, 2);
            int kopieC = BedarfStammCtrl.CopyFromStamm(art, nameC, projekt);
            Assert.True(kopieC > 0);

            Projektkopievormerkung v = Vormerkung(art, projekt);
            v.Angelegt(nameC, kopieC);

            v.Schliessen(false, () => Schreiben(art, projekt, (nameB, kopieB)),
                         Bestand(nameA, kopieA, nameB, kopieB), id => id == kopieB);

            Assert.True(BedarfStammCtrl.ProjektkopieId(art, nameC, projekt) <= 0);
            Assert.Equal(kopieA, BedarfStammCtrl.ProjektkopieId(art, nameA, projekt));
            Assert.Equal(1, Zuordnungen(art, projekt, kopieA));
        }

        // =====================================================================

        /// <summary>Der Bestand beim Öffnen: zwei Zeilen, jede mit ihrer Projektkopie.</summary>
        private static IEnumerable<(string Bezeichner, int KopieId)> Bestand(
            string nameA, int kopieA, string nameB, int kopieB)
            => new[] { (nameA, kopieA), (nameB, kopieB) };

        /// <summary>Die Vormerkung der Hülle: Löschweg des Kerns über den Bezeichner.</summary>
        private static Projektkopievormerkung Vormerkung(BedarfsArt art, int projekt)
            => new Projektkopievormerkung(name => BedarfStammCtrl.ProjektkopieEntfernen(
                   art, BedarfStammCtrl.ProjektkopieId(art, name, projekt), projekt));

        /// <summary>
        /// Ein frisches Projekt mit zwei übernommenen Katalogsätzen und den Zuordnungen beider — der Stand,
        /// mit dem der Dialog öffnet.
        /// </summary>
        private static (int, string, int, string, int) Vorbereiten(BedarfsArt art, string projektname)
        {
            int projekt = ProjektAnlegen(projektname);
            string nameA = Katalogname(art, 0);
            string nameB = Katalogname(art, 1);
            int kopieA = BedarfStammCtrl.CopyFromStamm(art, nameA, projekt);
            int kopieB = BedarfStammCtrl.CopyFromStamm(art, nameB, projekt);
            Assert.True(kopieA > 0 && kopieB > 0, art + ": die Projektkopien sind nicht entstanden (Projekt "
                        + projekt + ", " + nameA + "=" + kopieA + ", " + nameB + "=" + kopieB + ").");

            Schreiben(art, projekt, (nameA, kopieA), (nameB, kopieB));
            Assert.Equal(1, Zuordnungen(art, projekt, kopieA));
            return (projekt, nameA, kopieA, nameB, kopieB);
        }

        /// <summary>Der Schreibweg des Aufrufers je Art — derselbe, den die Kachelwege der Startseite reichen.</summary>
        private static void Schreiben(BedarfsArt art, int projekt, params (string Name, int Kopie)[] zeilen)
        {
            var wiz = new WizardCtrl();
            switch (art)
            {
                case BedarfsArt.Stromverbraucher:
                    Assert.True(wiz.Del_Projekt_Stromverbraucher(projekt));
                    Assert.True(wiz.Add_Projekt_Stromverbraucher(projekt, zeilen.Select(z =>
                        new Z_ProjektStromverbraucherModel
                        {
                            m_ID_Projekt = projekt, m_ID_Stromverbraucher = z.Kopie,
                            m_szVerbraucher = z.Name, m_Summe = 10
                        }).ToList()));
                    break;
                case BedarfsArt.Prozesswaerme:
                    Assert.True(wiz.Del_Projekt_Prozess(projekt));
                    Assert.True(wiz.Add_Projekt_Prozess(projekt, zeilen.Select(z =>
                        new Z_ProjektProzesswaermeModel
                        {
                            ID_Projekt = projekt, ID_Prozesswaerme = z.Kopie,
                            szProzessname = z.Name, Summe = 10
                        }).ToList()));
                    break;
                default:
                    Assert.Null(KaeltebedarfsprofilAbbildung.Speichern(projekt, zeilen.Select(z =>
                        new Z_ProjektKaeltebedarfModel
                        {
                            ID_Projekt = projekt, ID_Kaeltebedarf = z.Kopie,
                            Bezeichner = z.Name, Summe = 10
                        }).ToList()));
                    break;
            }
        }

        /// <summary>Wie viele Zuordnungen des Projekts zeigen auf diese Projektkopie?</summary>
        private static int Zuordnungen(BedarfsArt art, int projekt, int kopie)
        {
            (string tabelle, string spalte) = art == BedarfsArt.Stromverbraucher
                ? ("Z_Projekt_Stromverbraucher", "ID_Stromverbraucher")
                : art == BedarfsArt.Prozesswaerme
                    ? ("Z_Projekt_Prozesswaerme", "ID_Prozesswaerme")
                    : ("Z_Projekt_Kaeltebedarf", "ID_Kaeltebedarf");
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM " + tabelle + " WHERE " + spalte + " = ? AND ID_Projekt = ?",
                new DbParam("@id", kopie), new DbParam("@proj", projekt));
            return o == null || o == DBNull.Value ? -1 : (int)Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Der Bezeichner des n-ten Katalogsatzes dieser Art.</summary>
        private static string Katalogname(BedarfsArt art, int stelle)
        {
            string name = BedarfStammCtrl.Katalogfilterzeilen(art).Skip(stelle).First().Bezeichner;
            Assert.False(string.IsNullOrWhiteSpace(name));
            return name;
        }

        private static int ProjektAnlegen(string name)
        {
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_Projekt (Projektname) VALUES (?)",
                                           new DbParam("@name", DbParamTyp.VarWChar) { Wert = name });
            int id = DataRepository.GetMaxID("Tab_Projekt", "ID");
            Assert.True(id > 0, "Das Projekt \"" + name + "\" ist nicht entstanden.");
            return id;
        }
    }
}
