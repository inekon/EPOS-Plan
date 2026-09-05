using System;
using KiKern;
using Xunit;

namespace KiKern.Tests
{
    /// <summary>
    /// <see cref="KiDienstfehler"/> und <see cref="KiDienstAusnahme"/> — Anwenderbefund
    /// <b>W15b-B-2</b> der Windows-Abnahme vom 05.09.2026
    /// („Hilfeassistent funktioniert nicht bei Fragen").
    /// </summary>
    /// <remarks>
    /// <para>
    /// Im Chatfenster stand der ROHTEXT des Anbieters: <i>„Die Anfrage konnte nicht
    /// beantwortet werden: HTTP 401 - Request had invalid authentication credentials.
    /// Expected OAuth 2 access token, login cookie or other valid authentication
    /// credential. See https://developers.google.com/identity/sign-in/web/devconsole-project."</i>
    /// Ein Satz aus einer Entwicklerkonsole, mitsamt einer Adresse, die den Anwender
    /// nirgendwohin führt.
    /// </para>
    /// <para>
    /// <b>Diese Fälle prüfen die Trennung:</b> Anwendersatz in den Verlauf, Rohtext ins
    /// Protokoll. Sie brauchen weder Netz noch Datenbank noch Ressourcen — ohne
    /// eingehängten <see cref="KiTexte.Lieferant"/> greifen die deutschen Vorgaben des
    /// Kerns, und genau die stehen hier.
    /// </para>
    /// </remarks>
    public class KiDienstfehlerTests
    {
        // =====================================================================
        //  Kein Schluessel: die Anfrage geht gar nicht erst hinaus
        // =====================================================================

        /// <summary>
        /// <b>Die Kennzahl 0 ist der wichtigste Fall.</b> Sie heisst „es ging keine
        /// Anfrage hinaus", und der Satz nennt die EINE Stelle, an der es zu beheben
        /// ist.
        /// </summary>
        [Fact]
        public void Ohne_Anfrage_nennt_der_Satz_die_Einstellungen()
        {
            string satz = KiDienstfehler.Anwendersatz(KiDienstfehler.OhneAnfrage);

            Assert.Contains("Kein API-Schl", satz, StringComparison.Ordinal);
            Assert.Contains("Einstellungen", satz, StringComparison.Ordinal);

            // Kein Anbietertext, keine Kennzahl - es gab ja keine Antwort.
            Assert.DoesNotContain("HTTP", satz, StringComparison.Ordinal);
            Assert.DoesNotContain("OAuth", satz, StringComparison.Ordinal);
        }

        // =====================================================================
        //  Die HTTP-Kennzahlen
        // =====================================================================

        /// <summary>
        /// 401 und 403 sind Zugangsdaten, 400 ist ein unbrauchbarer Schlüssel — beide
        /// verweisen auf die Einstellungen, denn dort ist der Schlüssel zu ändern.
        /// </summary>
        [Theory]
        [InlineData(400)]
        [InlineData(401)]
        [InlineData(403)]
        public void Bei_Zugangsfehlern_verweist_der_Satz_auf_die_Einstellungen(int status)
        {
            string satz = KiDienstfehler.Anwendersatz(status);

            Assert.Contains("Einstellungen", satz, StringComparison.Ordinal);
            Assert.Contains("(" + status + ")", satz, StringComparison.Ordinal);
            Assert.DoesNotContain("OAuth", satz, StringComparison.Ordinal);
        }

        /// <summary>
        /// 429 und 5xx sind Lagen, an denen der Anwender nichts ändern kann — der Satz
        /// bittet um Geduld statt um eine Prüfung des Schlüssels.
        /// </summary>
        [Theory]
        [InlineData(429)]
        [InlineData(500)]
        [InlineData(503)]
        public void Bei_Ueberlast_und_Stoerung_bittet_der_Satz_um_Geduld(int status)
        {
            string satz = KiDienstfehler.Anwendersatz(status);

            Assert.Contains("später", satz, StringComparison.Ordinal);
            Assert.Contains("(" + status + ")", satz, StringComparison.Ordinal);
            Assert.DoesNotContain("Schlüssel", satz, StringComparison.Ordinal);
        }

        /// <summary>Jede andere Kennzahl verweist auf das Protokoll.</summary>
        [Fact]
        public void Eine_unbekannte_Kennzahl_verweist_auf_das_Protokoll()
        {
            string satz = KiDienstfehler.Anwendersatz(418);

            Assert.Contains("(418)", satz, StringComparison.Ordinal);
            Assert.Contains("Protokoll", satz, StringComparison.Ordinal);
        }

        // =====================================================================
        //  Der Rohtext bleibt vollstaendig
        // =====================================================================

        /// <summary>
        /// <b>Der Wortlaut des Anbieters wird nicht geschönt.</b> Er ist der Nachweis
        /// dafür, was tatsächlich zurückkam — und steht deshalb ungekürzt in der
        /// Protokollzeile.
        /// </summary>
        [Fact]
        public void Die_Protokollzeile_traegt_den_Wortlaut_des_Anbieters()
        {
            const string roh = "Request had invalid authentication credentials. " +
                               "Expected OAuth 2 access token, login cookie or other valid " +
                               "authentication credential.";

            string zeile = KiDienstfehler.Protokollzeile(401, roh);

            Assert.Contains("401", zeile, StringComparison.Ordinal);
            Assert.Contains(roh, zeile, StringComparison.Ordinal);
        }

        /// <summary>Ohne Anfrage gibt es keine Kennzahl — dort steht ein Strich.</summary>
        [Fact]
        public void Ohne_Anfrage_traegt_die_Protokollzeile_einen_Strich()
        {
            string zeile = KiDienstfehler.Protokollzeile(
                KiDienstfehler.OhneAnfrage, "Anfrage nicht gesendet: kein API-Schluessel.");

            Assert.Contains("-", zeile, StringComparison.Ordinal);
            Assert.Contains("kein API-Schluessel", zeile, StringComparison.Ordinal);
        }

        // =====================================================================
        //  Die Ausnahme traegt beides
        // =====================================================================

        /// <summary>
        /// Die Ausnahme trägt den ANWENDERSATZ als Meldung und den Rohtext daneben.
        /// So kann der Aufrufer beides auseinanderhalten, ohne zu raten — im Bestand
        /// hiess die Meldung <c>"HTTP 401 - " + Rohtext</c>, und danach gab es keine
        /// Stelle mehr, an der sich das eine vom anderen trennen liess.
        /// </summary>
        [Fact]
        public void Die_Ausnahme_traegt_Anwendersatz_und_Rohtext_getrennt()
        {
            var absage = new KiDienstAusnahme(401, "Expected OAuth 2 access token");

            Assert.Equal(KiDienstfehler.Anwendersatz(401), absage.Message);
            Assert.Equal(401, absage.Status);
            Assert.Equal("Expected OAuth 2 access token", absage.Rohtext);
            Assert.Contains("Expected OAuth 2 access token", absage.Protokollzeile(),
                            StringComparison.Ordinal);
            Assert.DoesNotContain("OAuth", absage.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Ein Lieferant, der eine Vorlage OHNE Platzhalter liefert (eine Übersetzung,
        /// die ihn vergisst), darf keinen Fehler auslösen — dieselbe Milde wie
        /// <see cref="KiTexte.Hole"/>.
        /// </summary>
        [Fact]
        public void Eine_Vorlage_ohne_Platzhalter_bricht_nichts()
        {
            Func<string, string?>? vorher = KiTexte.Lieferant;
            try
            {
                KiTexte.Lieferant = s => s.EndsWith("DIENST_ABGELEHNT", StringComparison.Ordinal)
                    ? "Der Dienst hat abgelehnt."
                    : null;

                Assert.Equal("Der Dienst hat abgelehnt.", KiDienstfehler.Anwendersatz(401));
            }
            finally
            {
                KiTexte.Lieferant = vorher;
            }
        }
    }
}
