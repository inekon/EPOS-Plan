using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Wache zum Schemaschritt 90: die sechs Spalten bleiben weg.</b>
    ///
    /// <para><b>Die Falle, gegen die sie steht.</b>
    /// <c>WirtschaftlichkeitCtrl.StelleTabellenSicher</c> legt seine Tabellen selbst an
    /// und zieht fehlende Spalten nach (<c>SpalteSicher</c>) — die bekannte doppelte
    /// Schema-Wahrheit dieses Moduls. Bliebe dort auch nur EINE der sechs Zeilen stehen,
    /// legte der erste Programmstart nach der Migration die Spalte wieder an: Der
    /// Schemaschritt haette sie entfernt, das Modul haette sie zurueckgeholt, und niemand
    /// saehe daran, dass sie nichts mehr rechnet.</para>
    ///
    /// <para>Geprueft wird nicht die Quelle, sondern die WIRKUNG: nach einem Aufruf von
    /// <c>StelleTabellenSicher</c> auf der Arbeitskopie fuehrt
    /// <c>Tab_ProjektWirtschaftlichkeit</c> keine der sechs Spalten.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KwkgProjektspaltenWacheTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public KwkgProjektspaltenWacheTests(TestDatenbank db) { _db = db; }

        [Fact]
        public void Die_sechs_Spalten_kehren_nach_StelleTabellenSicher_nicht_zurueck()
        {
            if (!_db.Vorhanden) return;

            // Der Ausgangszustand der Arbeitskopie ist der Zielstand - die sechs sind weg.
            Assert.Equal(0, KwkgProjektaltspalten.Offen());

            new WirtschaftlichkeitCtrl().StelleTabellenSicher();

            Assert.Equal(0, KwkgProjektaltspalten.Offen());
            foreach (KeyValuePair<string, string> s in KwkgProjektaltspalten.Spalten)
                Assert.False(KwkgProjektaltspalten.Vorhanden(s.Key, s.Value),
                             s.Key + "." + s.Value + " ist zurueckgekehrt - eine " +
                             "SpalteSicher-Zeile oder ein CREATE-Fragment steht noch.");

            // Die Nachbarn, die bleiben sollen, stehen weiterhin.
            Assert.True(DataRepository.SpalteVorhanden(KwkgProjektaltspalten.TABELLE,
                                                       SchemaKatalog.SPALTE_PW_KWKG_KOSTENANTEIL));
            Assert.True(DataRepository.SpalteVorhanden(KwkgProjektaltspalten.TABELLE,
                                                       SchemaKatalog.SPALTE_PW_KWKG_PAUSCHALMODUS));
        }
    }
}
