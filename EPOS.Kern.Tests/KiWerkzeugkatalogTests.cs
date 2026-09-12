using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der WERKZEUGKATALOG in Anwendersprache — Anwenderbefund <b>W15b‑E‑4</b> der
    /// Windows-Abnahme vom 05.09.2026: „Es ist unklar, was ausgeführt werden kann und
    /// wie."
    /// </summary>
    /// <remarks>
    /// <para>
    /// Die Werkzeugliste zeigte rohe Bezeichner (<c>minimale_spitze_ermitteln</c>,
    /// <c>speichervariante_aktiv_setzen</c>). Seither trägt jede der <b>24</b> Aktionen
    /// des Registers einen TITEL und ein BEISPIEL, beides zweisprachig aus
    /// <c>MyResource</c> — <see cref="KiAktionsTexte"/> bildet den Aktionsnamen auf den
    /// Ressourcenschlüssel ab.
    /// </para>
    /// <para>
    /// <b>Warum der Fall hier steht.</b> Die Registrierungen selbst liegen im
    /// Anwendungsprojekt (<c>Allgemein/KI/Aktionen/</c>) und sind auf Linux nicht zu
    /// übersetzen; die TEXTE liegen im Kern und sind es. Geprüft wird deshalb der
    /// Katalog: Zu jeder Aktion gibt es beide Texte, in beiden Sprachen, nicht leer und
    /// verschieden vom Bezeichner. Fehlt einer, fällt dieser Fall — und nicht erst der
    /// Anwender, der wieder einen Bezeichner liest.
    /// </para>
    /// </remarks>
    public class KiWerkzeugkatalogTests
    {
        /// <summary>
        /// Die 24 Aktionen des Registers als Paar aus BEZEICHNER und Schlüsselstamm.
        /// Der Stamm ist der Bezeichner in Großschreibung; wo der Ressourcenschlüssel
        /// aus historischen Gründen kürzer ist, steht er hier ausdrücklich.
        /// </summary>
        public static TheoryData<string, string> AlleAktionen
        {
            get
            {
                var daten = new TheoryData<string, string>();
                foreach ((string name, string stamm) in Katalog) daten.Add(name, stamm);
                return daten;
            }
        }

        private static readonly (string Name, string Stamm)[] Katalog =
        {
            ("projekte_auflisten", "PROJEKTE_AUFLISTEN"),
            ("projekt_aktiv", "PROJEKT_AKTIV"),
            ("projekt_suchen", "PROJEKT_SUCHEN"),
            ("projekt_lesen", "PROJEKT_LESEN"),
            ("varianten_auflisten", "VARIANTEN_AUFLISTEN"),
            ("speichervarianten_auflisten", "SPEICHERVARIANTEN_AUFLISTEN"),
            ("ergebnisse_lesen", "ERGEBNISSE_LESEN"),
            ("wirtschaftlichkeit_parameter_lesen", "PARAMETER_LESEN"),
            ("kostenlage_pruefen", "KOSTENLAGE_PRUEFEN"),
            ("uebernahme_vorschau", "UEBERNAHME_VORSCHAU"),
            ("merkmal_vorschau", "MERKMAL_VORSCHAU"),
            ("lastgang_pruefen", "LASTGANG_PRUEFEN"),
            ("ganglinien_auflisten", "GANGLINIEN_AUFLISTEN"),
            ("minimale_spitze_ermitteln", "MINIMALE_SPITZE"),
            ("letzte_aktionen", "LETZTE_AKTIONEN"),
            ("energietraeger_pruefen", "ENERGIETRAEGER_PRUEFEN"),
            ("dialog_lesen", "DIALOG_LESEN"),
            ("dialog_parameter_erklaeren", "DIALOG_ERKLAEREN"),
            ("feld_setzen", "FELD_SETZEN"),
            ("formular_ausfuellen", "FORMULAR_AUSFUELLEN"),
            ("dialog_aktion_ausfuehren", "DIALOG_AKTION"),
            ("variante_anlegen", "VARIANTE_ANLEGEN"),
            ("speichervariante_aktiv_setzen", "SPEICHERVARIANTE_AKTIV"),
            ("kostenposition_setzen", "KOSTENPOSITION_SETZEN")
        };

        private static string Text(string schluessel, string sprache)
        {
            return Resource.ResourceManager.GetString(
                schluessel, new CultureInfo(sprache));
        }

        // ==================================================================
        //  Titel und Beispiel, zweisprachig
        // ==================================================================

        /// <summary>
        /// <b>Zu jeder Aktion gibt es einen Titel — in beiden Sprachen, nicht leer und
        /// nie der Bezeichner.</b> Genau das war der Befund.
        /// </summary>
        [Theory]
        [MemberData(nameof(AlleAktionen))]
        public void Jede_Aktion_hat_einen_Titel_in_beiden_Sprachen(string name, string stamm)
        {
            string schluessel = "KI_REG_TITEL_" + stamm;

            foreach (string sprache in new[] { "de-DE", "en-US" })
            {
                string titel = Text(schluessel, sprache);

                Assert.False(string.IsNullOrWhiteSpace(titel),
                             schluessel + " fehlt in " + sprache);
                Assert.NotEqual(name, titel);
                Assert.DoesNotContain("_", titel, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// <b>Zu jeder Aktion gibt es ein Beispiel</b> — der Satz, mit dem der Anwender
        /// dieselbe Aktion im Gespräch erreicht. Auch zweisprachig; er steht in der
        /// Werkzeugliste und im eingebauten Hilfewissen.
        /// </summary>
        [Theory]
        [MemberData(nameof(AlleAktionen))]
        public void Jede_Aktion_hat_ein_Beispiel_in_beiden_Sprachen(string name, string stamm)
        {
            string schluessel = "KI_REG_BEISPIEL_" + stamm;

            foreach (string sprache in new[] { "de-DE", "en-US" })
            {
                string beispiel = Text(schluessel, sprache);

                Assert.False(string.IsNullOrWhiteSpace(beispiel),
                             schluessel + " fehlt in " + sprache);
                Assert.NotEqual(name, beispiel);
                // Ein Beispiel ist ein SATZ, kein Stichwort.
                Assert.True(beispiel.Trim().Length > 15, schluessel + " ist kein Satz: " + beispiel);
            }
        }

        /// <summary>
        /// Deutsch und Englisch sind wirklich zwei Fassungen — ein Katalogeintrag, der
        /// in beiden Sprachen zeichengleich ist, wäre eine vergessene Übersetzung.
        /// </summary>
        [Theory]
        [MemberData(nameof(AlleAktionen))]
        public void Titel_und_Beispiel_sind_wirklich_uebersetzt(string name, string stamm)
        {
            _ = name;

            foreach (string vorsatz in new[] { "KI_REG_TITEL_", "KI_REG_BEISPIEL_" })
            {
                string de = Text(vorsatz + stamm, "de-DE");
                string en = Text(vorsatz + stamm, "en-US");

                Assert.NotEqual(de, en);
            }
        }

        /// <summary>
        /// <b>Die Zuordnung liegt an EINER Stelle</b> (<see cref="KiAktionsTexte"/>) —
        /// die Aktionsdateien kennen keinen Ressourcennamen. Der Fall zieht die
        /// Eigenschaften über Reflexion und hält sie gegen den Katalog: Wer eine Aktion
        /// aufnimmt und die zwei Texte vergisst, fällt hier auf.
        /// </summary>
        [Fact]
        public void Zu_jeder_Aktion_gibt_es_die_zwei_Eigenschaften_in_KiAktionsTexte()
        {
            Type t = typeof(KiAktionsTexte);
            var fehlen = new List<string>();

            foreach ((string _, string stamm) in Katalog)
            {
                string pascal = string.Concat(stamm.Split('_')
                    .Select(w => w.Substring(0, 1) + w.Substring(1).ToLowerInvariant()));

                foreach (string vorsatz in new[] { "Titel", "Beispiel" })
                {
                    string eigenschaft = vorsatz + pascal;
                    if (t.GetProperty(eigenschaft,
                            System.Reflection.BindingFlags.Static |
                            System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Public) == null)
                        fehlen.Add(eigenschaft);
                }
            }

            Assert.True(fehlen.Count == 0,
                        "KiAktionsTexte fehlen: " + string.Join(", ", fehlen));
        }
    }
}
