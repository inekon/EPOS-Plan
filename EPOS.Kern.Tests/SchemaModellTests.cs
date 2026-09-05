using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <see cref="SchemaModell"/> gegen die TESTDATENBANK — der Nachweis zum
    /// Anwenderbefund <b>W10b-B-1</b> der Windows-Abnahme vom 05.09.2026, Punkt 3
    /// („Kaskade").
    ///
    /// <para><b>Der Befund.</b> Im Bildschirmfoto trugen zwei Erzeugerkaesten
    /// „Quelle: Puffer 3000Ltr · Kaskade", und darunter stand „Keine Kaskade im Projekt —
    /// kein Erzeuger bezieht seine Waerme aus einem Pufferspeicher". Beides kam aus
    /// diesem Modell: der Kastentext aus dem Quellpuffer, der Satz aus der leeren
    /// Kaskadenkette.</para>
    ///
    /// <para><b>Die Ursache steht in der Testdatenbank.</b> Projekt <b>1042</b> ist genau
    /// dieser Fall: Anlage 14817 laedt Puffer 1054196 auf <b>Rang 2</b>, und aus eben
    /// diesem Puffer beziehen die Anlagen 14818 und 14854 ihre Waerme. Die
    /// Kettenableitung sah nur RANG 1 und fand deshalb keinen Kettenanfang — Kaskade
    /// vorhanden, Band leer, Satz falsch.</para>
    ///
    /// <para><b>Ohne die 77-MB-Datei schweigen die Faelle</b> (siehe
    /// <see cref="TestDatenbank"/>); die Sammlung ist noetig, weil
    /// <c>DataRepository.PfadUeberschreibung</c> statisch ist.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class SchemaModellTests : IClassFixture<TestDatenbank>
    {
        /// <summary>Projekt MIT Kaskade — zwei Erzeuger beziehen aus Puffer 1054196.</summary>
        private const int PROJEKT_KASKADE = 1042;

        /// <summary>Projekt OHNE Kaskade — die Basis des Regressionsnetzes.</summary>
        private const int PROJEKT_OHNE_KASKADE = 1030;

        private readonly TestDatenbank _db;

        public SchemaModellTests(TestDatenbank db) { _db = db; }

        // ================================================================== Kaskade

        [Fact]
        public void Ein_Kaskadenprojekt_wird_als_Kaskade_erkannt()
        {
            if (!_db.Vorhanden) return;

            SchemaModell m = SchemaModell.Aufbauen(PROJEKT_KASKADE, null);

            Assert.True(m.HatKaskade,
                        "Projekt 1042 fuehrt zwei Erzeuger mit Quellpuffer 1054196.");

            // Und die Kaesten sagen dasselbe: mindestens ein Erzeuger traegt das
            // Kaskadenkennzeichen, aus dem die Zeile „Quelle: … · Kaskade" entsteht.
            bool kasten = false;
            foreach (SchemaModell.Knoten k in m.Spalte(SchemaModell.Knotenart.Erzeuger))
                if (k.Kaskade) kasten = true;

            Assert.True(kasten);
        }

        [Fact]
        public void Ein_Projekt_ohne_Quellpuffer_fuehrt_keine_Kaskade()
        {
            if (!_db.Vorhanden) return;

            SchemaModell m = SchemaModell.Aufbauen(PROJEKT_OHNE_KASKADE, null);

            Assert.False(m.HatKaskade);
            Assert.Empty(m.Ketten);

            foreach (SchemaModell.Knoten k in m.Spalte(SchemaModell.Knotenart.Erzeuger))
                Assert.False(k.Kaskade);

            foreach (SchemaModell.Kante e in m.Kantenliste)
                Assert.NotEqual(SchemaModell.Kantenart.Kaskade, e.Art);
        }

        /// <summary>
        /// Der Kern des Befunds: Kaskade im Projekt UND Band im Bild — nicht das eine
        /// ohne das andere. Vor W10b-B-1 war <c>Ketten</c> hier leer, weil der Lader den
        /// Quellpuffer auf Rang 2 belaedt.
        /// </summary>
        [Fact]
        public void Kaskade_und_Kaskadenband_widersprechen_sich_nicht()
        {
            if (!_db.Vorhanden) return;

            foreach (int idProjekt in new[] { PROJEKT_KASKADE, PROJEKT_OHNE_KASKADE, 1043, 1044, 1007 })
            {
                SchemaModell m = SchemaModell.Aufbauen(idProjekt, null);
                if (m.IstLeer) continue;

                Assert.True(m.HatKaskade == (m.Ketten.Count > 0),
                            "Projekt " + idProjekt + ": HatKaskade=" + m.HatKaskade +
                            ", Ketten=" + m.Ketten.Count);

                // Eine Kaskadenkante gibt es genau dann, wenn es die Kaskade gibt.
                bool kante = false;
                foreach (SchemaModell.Kante e in m.Kantenliste)
                    if (e.Art == SchemaModell.Kantenart.Kaskade) kante = true;

                Assert.Equal(m.HatKaskade, kante);
            }
        }

        [Fact]
        public void Die_Kaskadenkette_beginnt_beim_Lader_und_fuehrt_ueber_den_Quellpuffer()
        {
            if (!_db.Vorhanden) return;

            SchemaModell m = SchemaModell.Aufbauen(PROJEKT_KASKADE, null);
            Assert.NotEmpty(m.Ketten);

            // Jede Kette laeuft ueber den Speicher, aus dem die nachgeschalteten
            // Erzeuger beziehen — und hinter jedem Speicherglied steht ein Erzeuger
            // oder ein Abnehmer, nie ein zweiter Speicher (Invariante S-1).
            foreach (List<SchemaModell.Kettenglied> kette in m.Ketten)
            {
                Assert.NotEmpty(kette);

                bool speicher = false;
                foreach (SchemaModell.Kettenglied g in kette)
                    if (g.Art == SchemaModell.Knotenart.Speicher) speicher = true;

                Assert.True(speicher, "Eine Kaskadenkette ohne Speicherglied.");
            }

            Assert.Empty(m.Pruefen());
        }

        // ================================================================== Anordnung

        /// <summary>
        /// Punkt 2 des Befunds an einem ECHTEN Projekt: Keine Leitung des angeordneten
        /// Schemas kreuzt einen Kasten — auch nicht die Kaskade zurueck zum Erzeuger und
        /// auch nicht die Direktdeckung, die die Speicherspalte ueberspringt.
        /// </summary>
        [Theory]
        [InlineData(PROJEKT_KASKADE)]
        [InlineData(PROJEKT_OHNE_KASKADE)]
        [InlineData(1043)]
        [InlineData(1044)]
        [InlineData(1007)]
        [InlineData(1017)]
        public void Keine_Leitung_eines_echten_Projekts_kreuzt_einen_Kasten(int idProjekt)
        {
            if (!_db.Vorhanden) return;

            SchemaLayout l = SchemaLayout.Anordnen(SchemaModell.Aufbauen(idProjekt, null), 0);
            if (l.IstLeer) return;

            foreach (SchemaLayout.Kantenzug z in l.Kanten)
            {
                Assert.True(z.Punkte.Count >= 2);
                Assert.Equal(SchemaLayout.LINIE_BREITE, z.Breite);
                Assert.True(z.Breite >= SchemaLayout.LINIE_BREITE_MIN);
                Assert.True(z.Breite <= SchemaLayout.LINIE_BREITE_MAX);

                for (int i = 1; i < z.Punkte.Count; i++)
                {
                    Assert.True(z.Punkte[i].X == z.Punkte[i - 1].X ||
                                z.Punkte[i].Y == z.Punkte[i - 1].Y,
                                "Schraeges Stueck in Projekt " + idProjekt);

                    foreach (SchemaLayout.Knotenflaeche k in l.Knoten)
                        Assert.False(Kreuzt(z.Punkte[i - 1], z.Punkte[i], k.Flaeche),
                                     "Projekt " + idProjekt + ": " + z.Kante.Von + " -> " +
                                     z.Kante.Nach + " kreuzt " + k.Schluessel);
                }
            }
        }

        private static bool Kreuzt(SchemaLayout.Punkt a, SchemaLayout.Punkt b,
                                   SchemaLayout.Rechteck r)
        {
            int x1 = System.Math.Min(a.X, b.X), x2 = System.Math.Max(a.X, b.X);
            int y1 = System.Math.Min(a.Y, b.Y), y2 = System.Math.Max(a.Y, b.Y);

            return x2 > r.X && x1 < r.Rechts && y2 > r.Y && y1 < r.Unten;
        }
    }
}
