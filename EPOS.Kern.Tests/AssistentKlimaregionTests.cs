using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Seiten.Start;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Klimaregion im Projektassistenten</b> (#527, Anwenderbefund 25.09.2026):
    /// Die Klappliste des Schritts „Projektkonfiguration" ist dieselbe wie die der
    /// Kopfleiste — Einträge, Kurzform, Reihenfolge, Herkunftszeile —, und die Wahl
    /// landet im neuen Projekt, wo die Kopfleiste sie wieder liest.
    ///
    /// <para>Der Lauf geht über dieselben Delegaten wie die Oberfläche
    /// (<c>AssistentAnsichtQuelle.Gaben</c>: Seitengaben, Seite verlassen, Speichern);
    /// die Wahl in der Klappliste spielt der Fall so nach, wie
    /// <c>ProjektKopfSeite.KlimaGewaehlt</c> sie schreibt — Stamm-Id und blanker
    /// Stammname aus den Gaben der Hülle.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class AssistentKlimaregionTests : IDisposable
    {
        private const string NEU_NAME = "#527 Klimaprobe";

        /// <summary>Der Halter ist statisch - jeder Fall bekommt ihn zurueck, wie er war.</summary>
        private readonly WizardCtrl _vorherCtrl = WizardCtrl.Aktueller;

        public void Dispose() => WizardCtrl.Aktueller = _vorherCtrl;

        /// <summary>
        /// Die Liste des Assistenten IST die Liste der Kopfleiste: dieselben (Id, Text)
        /// in derselben Reihenfolge; die Texte tragen die Kurzform der Herkunft, und der
        /// blanke Stammname je Id steht daneben.
        /// </summary>
        [Fact]
        public void Die_Klappliste_des_Assistenten_ist_die_der_Kopfleiste()
        {
            using (TestDatenbank db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                // Eine Region mit TRY-Herkunft, damit die Kurzform etwas zu sagen hat.
                int berlin = KlimaregionStammCtrl.IdVonName("Berlin");
                Assert.True(berlin > 0);
                Assert.True(DataRepository.ExecuteSQL(
                    "UPDATE Tab_Klimaregion_STAMM SET Quelle = ?, Szenario = ?, Bezugsjahr = ? WHERE ID_Klimaregion = ?",
                    new DbParam("@q", DbWerte.KLIMA_QUELLE_TRY_REGIONAL),
                    new DbParam("@s", DbWerte.KLIMA_SZENARIO_SOMMERWARM),
                    new DbParam("@j", 2045),
                    new DbParam("@id", berlin)));

                IReadOnlyDictionary<string, object> gaben = KopfGaben(AssistentCtrl.BETRIEBSART_NEU, out _);
                var eintraege = (IReadOnlyList<(int Id, string Text)>)gaben["Klimaregionen"];
                var namen = (IReadOnlyDictionary<int, string>)gaben["Klimanamen"];

                IReadOnlyList<(int Id, string Name)> kopfleiste = StartseiteCtrl.KlimaregionenMitId();
                Assert.NotEmpty(kopfleiste);
                Assert.Equal(kopfleiste.Select(k => (k.Id, k.Name)).ToList(), eintraege.ToList());

                // Die Kurzform der Kopfleiste: „Berlin (TRY 2045 sommerwarm)".
                string text = eintraege.Single(e => e.Id == berlin).Text;
                Assert.Equal(KlimaAnzeige.Eintrag("Berlin", DbWerte.KLIMA_QUELLE_TRY_REGIONAL,
                                                  DbWerte.KLIMA_SZENARIO_SOMMERWARM, 2045), text);
                Assert.NotEqual("Berlin", text);

                // Je Id der blanke Stammname - der Schluessel des Speicherwegs.
                Assert.Equal(eintraege.Count, namen.Count);
                foreach ((int id, string _) in eintraege)
                    Assert.Equal(KlimaregionStammCtrl.NameVonId(id), namen[id]);

                // Die Herkunftszeile der Wahl baut sich aus dem Katalogsatz.
                var herkunft = (Func<int, KlimaHerkunftGaben>)gaben["KlimaHerkunft"];
                KlimaHerkunftGaben h = herkunft(berlin);
                Assert.NotNull(h);
                Assert.Equal("Berlin", h.Bezeichner);
                Assert.Equal(KlimaAnzeige.Quellenzeile(DbWerte.KLIMA_QUELLE_TRY_REGIONAL,
                                                       DbWerte.KLIMA_SZENARIO_SOMMERWARM, 2045), h.Quelle);
                Assert.Null(herkunft(0));
            }
        }

        /// <summary>
        /// Die Vorbelegung eines neuen Projekts ist die Region des aktiven Projekts — als
        /// STAMM-Id, die in der Klappliste steht, und mit ihrem Namen. Vorher kam die Id
        /// der Projektkopie herein: kein Eintrag gewählt, die Pflichtregel aber zufrieden.
        /// </summary>
        [Fact]
        public void Die_Vorbelegung_steht_als_Stammregion_in_der_Klappliste()
        {
            using (TestDatenbank db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                int aktiv = ProjektKontextCtrl.ZuletztGeoeffnet().Id;
                Assert.True(aktiv > 0, "Die Testdatenbank fuehrt kein aktives Projekt.");
                int erwartet = StartseiteCtrl.ProjektKlimaregionStammId(aktiv);
                Assert.True(erwartet > 0);

                IReadOnlyDictionary<string, object> gaben = KopfGaben(AssistentCtrl.BETRIEBSART_NEU, out _);
                var daten = (ProjektKopfDaten)gaben["Daten"];
                var eintraege = (IReadOnlyList<(int Id, string Text)>)gaben["Klimaregionen"];

                Assert.Equal(erwartet, daten.IdKlimaregion);
                Assert.Contains(eintraege, e => e.Id == daten.IdKlimaregion);
                Assert.Equal(StartseiteCtrl.ProjektKlimazone(aktiv), daten.Klimaname);
                Assert.Equal(erwartet, ProjektCtrl.KlimaregionDesAktivenProjekts());
            }
        }

        /// <summary>
        /// Der NEU-Zweig: Die gewählte Region steht nach dem Anlegen im Projekt —
        /// <c>Tab_Projekt.ID_Klimaregion</c> zeigt auf ihre Projektkopie, und die
        /// Kopfleiste liest dieselbe Stamm-Id und dieselbe Herkunft wieder.
        /// </summary>
        [Fact]
        public void Die_im_Assistenten_gewaehlte_Region_steht_im_neuen_Projekt()
        {
            using (TestDatenbank db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                WizardCtrl vorher = WizardCtrl.Aktueller;
                try
                {
                    int berlin = KlimaregionStammCtrl.IdVonName("Berlin");
                    Assert.True(berlin > 0);
                    // Nicht die Vorbelegung - sonst bewiese der Fall nichts.
                    Assert.NotEqual(berlin, ProjektCtrl.KlimaregionDesAktivenProjekts());

                    IReadOnlyDictionary<string, object> ablauf;
                    IReadOnlyDictionary<string, object> gaben = KopfGaben(AssistentCtrl.BETRIEBSART_NEU, out ablauf);
                    var daten = (ProjektKopfDaten)gaben["Daten"];
                    var namen = (IReadOnlyDictionary<int, string>)gaben["Klimanamen"];

                    // So schreibt ProjektKopfSeite.KlimaGewaehlt.
                    daten.Name = NEU_NAME;
                    daten.IdKlimaregion = berlin;
                    daten.Klimaname = namen[berlin];

                    ((Action<int>)ablauf["SeiteVerlassen"])(WizardItemClass.PROJEKT_ITEM);
                    (string Text, string Titel)? fehler = ((Func<(string Text, string Titel)?>)ablauf["Speichern"])();
                    Assert.True(fehler == null, fehler?.Text);

                    int id = ProjektCtrl.IdVonName(NEU_NAME);
                    Assert.True(id > 0, "Das Projekt wurde nicht angelegt.");

                    object kopie = DataRepository.ExecuteScalar(
                        "SELECT COUNT(*) FROM Tab_Klimaregion k JOIN Tab_Projekt p ON p.ID_Klimaregion = k.ID " +
                        "WHERE p.ID = ? AND k.ID_Projekt = ? AND k.Bezeichner = ?",
                        new DbParam("@p", id), new DbParam("@kp", id), new DbParam("@b", "Berlin"));
                    Assert.Equal(1, Convert.ToInt32(kopie));

                    // Die Kopfleiste liest dieselbe Region.
                    Assert.Equal(berlin, StartseiteCtrl.ProjektKlimaregionStammId(id));
                    Assert.Equal("Berlin", StartseiteCtrl.KlimaHerkunft(id).Bezeichner);
                    Assert.Equal(berlin, ProjektCtrl.Kopf(NEU_NAME).IdKlimaregion);
                }
                finally { WizardCtrl.Aktueller = vorher; }
            }
        }

        /// <summary>
        /// Die Id führt: Meldet die Klappliste nur die Stamm-Id (der Name im Kopf steht
        /// noch auf der Vorbelegung), schreibt der Speicherweg die Region der Id.
        /// </summary>
        [Fact]
        public void Die_Stamm_Id_fuehrt_vor_einem_veralteten_Namen()
        {
            using (TestDatenbank db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                WizardCtrl vorher = WizardCtrl.Aktueller;
                try
                {
                    int berlin = KlimaregionStammCtrl.IdVonName("Berlin");

                    IReadOnlyDictionary<string, object> ablauf;
                    IReadOnlyDictionary<string, object> gaben = KopfGaben(AssistentCtrl.BETRIEBSART_NEU, out ablauf);
                    var daten = (ProjektKopfDaten)gaben["Daten"];
                    Assert.False(string.IsNullOrEmpty(daten.Klimaname), "Ohne Vorbelegung bewiese der Fall nichts.");
                    Assert.NotEqual("Berlin", daten.Klimaname);

                    daten.Name = NEU_NAME;
                    daten.IdKlimaregion = berlin;          // der Name bleibt stehen

                    Assert.Equal("Berlin", AssistentAbgleich.Regionsname(daten));

                    ((Action<int>)ablauf["SeiteVerlassen"])(WizardItemClass.PROJEKT_ITEM);
                    (string Text, string Titel)? fehler = ((Func<(string Text, string Titel)?>)ablauf["Speichern"])();
                    Assert.True(fehler == null, fehler?.Text);

                    Assert.Equal("Berlin", StartseiteCtrl.ProjektKlimazone(ProjektCtrl.IdVonName(NEU_NAME)));
                }
                finally { WizardCtrl.Aktueller = vorher; }
            }
        }

        /// <summary>
        /// Ohne Region bleibt es beim benannten Ausgang „Klimazone fehlt" — kein Projekt
        /// entsteht ohne Region.
        /// </summary>
        [Fact]
        public void Ohne_Region_entsteht_kein_Projekt()
        {
            using (TestDatenbank db = new TestDatenbank())
            {
                if (!db.Vorhanden) return;

                WizardCtrl vorher = WizardCtrl.Aktueller;
                try
                {
                    IReadOnlyDictionary<string, object> ablauf;
                    IReadOnlyDictionary<string, object> gaben = KopfGaben(AssistentCtrl.BETRIEBSART_NEU, out ablauf);
                    var daten = (ProjektKopfDaten)gaben["Daten"];
                    daten.Name = NEU_NAME;
                    daten.IdKlimaregion = 0;
                    daten.Klimaname = "";

                    Assert.Equal(ProjektKopfBefund.KlimaLeer, ProjektKopfRegeln.Pruefe(daten, Array.Empty<string>()));
                    Assert.NotNull(((Func<int, string>)ablauf["SeitePruefen"])(WizardItemClass.PROJEKT_ITEM));

                    (string Text, string Titel)? fehler = ((Func<(string Text, string Titel)?>)ablauf["Speichern"])();
                    Assert.True(fehler != null);
                    Assert.Equal(0, ProjektCtrl.IdVonName(NEU_NAME));
                }
                finally { WizardCtrl.Aktueller = vorher; }
            }
        }

        /// <summary>
        /// Der Parametersatz des Projektkopfs — über denselben Weg wie die Oberfläche; der
        /// Ablauf (Seite verlassen, Speichern) kommt mit heraus.
        /// </summary>
        private static IReadOnlyDictionary<string, object> KopfGaben(
            int betriebsart, out IReadOnlyDictionary<string, object> ablauf)
        {
            WizardCtrl.Aktueller = new WizardCtrl();
            AssistentCtrl a = new AssistentCtrl { Betriebsart = betriebsart };
            ablauf = AssistentAnsichtQuelle.Gaben(a, null);
            var seiten = (Func<int, IReadOnlyDictionary<string, object>>)ablauf["SeiteGaben"];
            return seiten(WizardItemClass.PROJEKT_ITEM);
        }
    }
}
