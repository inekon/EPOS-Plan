using System;
using System.Collections.Generic;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// Die EINE Stelle, an der ueber die Bestaetigungspflicht entschieden wird
    /// (<see cref="KiBestaetigungspflicht"/>, Fachkonzept 11.5, Paket F4).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum hier NICHT abgeschaltet wird.</b> <see cref="KiFeldsicherung"/> ist
    /// prozessweit und laesst sich nicht zuruecksetzen; welchen Zustand diese Klasse
    /// antrifft, haengt davon ab, ob <c>KiFeldsicherungTests</c> vorher gelaufen ist. Die
    /// Faelle hier pruefen deshalb ausschliesslich Zusagen, die in BEIDEN Zustaenden
    /// gelten muessen - dann ist der Befund von der Reihenfolge unabhaengig. Der eine
    /// Fall, der den Uebergang selbst braucht, steht dort, wo der Uebergang stattfindet:
    /// in <c>KiFeldsicherungTests.DerLebenslauf_...</c>.
    /// </para>
    /// </remarks>
    public class KiBestaetigungspflichtTests
    {
        private static KiAktion Leseaktion()
            => new KiAktion("projekt_lesen", "Liest ein Projekt.", Schutzstufe.Lesen,
                            "ProjektCtrl.Read");

        private static KiAktion Schreibaktion(string name = "kostenposition_setzen")
            => new KiAktion(name, "Setzt eine Kostenposition.", Schutzstufe.Schreiben,
                            "KostenCtrl.Update",
                            vorschau: _ => "Ich wuerde eine Kostenposition setzen.");

        private static KiAktion Formularaktion(string name = "feld_setzen",
                                               bool datenbankwirksam = false)
            => new KiAktion(name, "Traegt einen Wert ein.", Schutzstufe.Schreiben,
                            "KiDialogZugriff.Setze",
                            vorschau: _ => "Wartungskosten · 850 → 1200",
                            formularaktion: true,
                            datenbankwirksam: datenbankwirksam);

        [Fact]
        public void OhneAktionGibtEsNichtsZuBestaetigen()
        {
            Assert.False(KiBestaetigungspflicht.Gilt((KiAktion?)null));
            Assert.False(KiBestaetigungspflicht.Gilt((KiAufruf?)null));
        }

        [Fact]
        public void ReinesLesenBrauchtNieEineBestaetigung()
        {
            Assert.False(KiBestaetigungspflicht.Gilt(Leseaktion()));
        }

        [Fact]
        public void EineGewoehnlicheSchreibaktionBrauchtIMMERDieBestaetigung()
        {
            // Der Kern der Zusage aus Fachkonzept 11.5: Der Schalter erreicht die Stufe 2
            // nicht. Der Fall gilt unabhaengig davon, ob die Feldsicherung in diesem
            // Testlauf bereits abgeschaltet wurde - genau das ist sein Zweck.
            foreach (string name in new[] { "kostenposition_setzen", "variante_anlegen" })
            {
                KiAktion a = Schreibaktion(name);
                Assert.True(KiBestaetigungspflicht.Gilt(a),
                            name + " muss in JEDEM Fall bestaetigungspflichtig bleiben.");
                Assert.Equal(KiRiegel.BrauchtBestaetigung(a), KiBestaetigungspflicht.Gilt(a));
            }
        }

        [Fact]
        public void EineFormularaktionHaengtGenauAnDerFeldsicherung()
        {
            Assert.Equal(KiFeldsicherung.Aktiv, KiBestaetigungspflicht.Gilt(Formularaktion()));
        }

        [Fact]
        public void DerSchalterKannNurEINSCHRAENKEN_NieErweitern()
        {
            // Was der Riegel durchlaesst, darf die Feldsicherung nicht nachtraeglich
            // bestaetigungspflichtig machen; was er sperrt, darf sie nicht freistellen.
            var alle = new List<KiAktion>
            {
                Leseaktion(), Schreibaktion(), Formularaktion(),
                Formularaktion("dialog_aktion_ausfuehren", datenbankwirksam: true)
            };

            foreach (KiAktion a in alle)
                if (KiBestaetigungspflicht.Gilt(a))
                    Assert.True(KiRiegel.BrauchtBestaetigung(a),
                                a.Name + ": die Feldsicherung hat die Pflicht ERWEITERT.");
        }

        [Fact]
        public void DerAufrufwegLiefertDasselbeWieDerAktionsweg()
        {
            // Beide Ueberladungen muessen dieselbe Antwort geben - sonst haengt das
            // Ergebnis davon ab, welche Aufrufstelle gerade fragt.
            KiAktion a = Schreibaktion();
            KiPruefErgebnis p = KiPruefung.Pruefe(a, new Dictionary<string, object?>());
            Assert.True(p.Gueltig, p.FehlerText());

            Assert.Equal(KiBestaetigungspflicht.Gilt(a), KiBestaetigungspflicht.Gilt(p.Aufruf));
        }

        // =====================================================================
        // Sicherungspunkt-Regel (Festlegung Paket F4)
        // =====================================================================

        [Fact]
        public void OhneAngabeIstEineAktionDatenbankwirksam()
        {
            // Die Vorgabe zeigt in die unschaedliche Richtung: eine vergessene Angabe
            // kostet eine ueberfluessige Kopie, nicht den Rueckweg.
            Assert.True(Schreibaktion().Datenbankwirksam);
            Assert.True(Schreibaktion().BrauchtSicherungspunkt);
            Assert.True(Leseaktion().Datenbankwirksam);
        }

        [Fact]
        public void ReinesLesenBrauchtKeinenSicherungspunkt()
        {
            Assert.False(Leseaktion().BrauchtSicherungspunkt);
        }

        [Fact]
        public void EinReinerOberflaechenEintragBrauchtKeinenSicherungspunkt()
        {
            // feld_setzen / formular_ausfuellen: Text in ein Eingabefeld, Datenbank
            // unberuehrt. Die Bestaetigungspflicht bleibt davon voellig unberuehrt.
            KiAktion a = Formularaktion();

            Assert.False(a.Datenbankwirksam);
            Assert.False(a.BrauchtSicherungspunkt);
            Assert.Equal(Schutzstufe.Schreiben, a.Stufe);
        }

        [Fact]
        public void DialogAktionAusfuehrenBehaeltIhrenSicherungspunkt()
        {
            // Der ausgeloeste Knopf schreibt ueber den Bestand in die Datenbank - deshalb
            // wird die Frage je Aktion entschieden und nicht pauschal fuer alle
            // Formularaktionen.
            KiAktion a = Formularaktion("dialog_aktion_ausfuehren", datenbankwirksam: true);

            Assert.True(a.Formularaktion);
            Assert.True(a.BrauchtSicherungspunkt);
        }

        [Fact]
        public void NurEineFormularaktionDarfSichVomSicherungspunktFreistellen()
        {
            // Sonst entstuende genau der Fall, den Fachkonzept 4.4 ausschliesst: eine
            // Aenderung am Datenbestand ohne Rueckweg.
            Assert.Throws<ArgumentException>(() => new KiAktion(
                "kostenposition_setzen", "Setzt eine Kostenposition.", Schutzstufe.Schreiben,
                "KostenCtrl.Update",
                vorschau: _ => "Vorschau",
                datenbankwirksam: false));
        }
    }
}
