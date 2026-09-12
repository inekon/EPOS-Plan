using System;
using System.Data;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Woher die Kennlinien einer Wärmepumpen-ANLAGE kommen — <b>Befund W7‑B‑3</b>
    /// der Windows-Abnahme V2 vom 07.09.2026: „Energieerzeuger → Wärmepumpe (Projekt
    /// ‚Stromspeicher mit Wärmepumpe'): Hier im Beispiel T800-2, im
    /// Projekt-Wärmepumpen-Dialog keine Kennlinie."
    ///
    /// <para><b>Die Ursache war die Tabelle, nicht die Datenlage.</b> Der Delegat
    /// <c>Bilder</c> der Anlagenhülle rief <c>WaermepumpeStammHuelle.BilderZu</c> und
    /// damit <c>KenndatenCtrl.Reihen</c> — die Abfrage geht auf
    /// <c>Tab_Kenndaten_STAMM</c>. Übergeben wird ihr aber <c>Daten.IdWp</c>, und das
    /// ist bei einer gespeicherten Anlage die Id der PROJEKTKOPIE
    /// (<c>Tab_WP.ID</c>), deren Kennlinien in <c>Tab_Kenndaten</c> stehen. Gemessen
    /// an <c>Referenzlaeufe/Kenndaten_Test.sqlite</c>: T 800-2 im Projekt 1006 hat
    /// die Id 1006020 und 16 Stützstellen in <c>Tab_Kenndaten</c> —
    /// <c>Tab_Kenndaten_STAMM</c> führt unter dieser Id keine einzige Zeile. Das gilt
    /// für ALLE 38 Projektgeräte der Testdatenbank; der Dialog zeigte für jede
    /// gespeicherte Anlage „Keine Kennlinien vorhanden".</para>
    ///
    /// <para><b>Die Ordnung ist dieselbe wie überall im Haus: Projektkopie vor
    /// Stammkatalog</b> (<see cref="WaermepumpeGeraeteCtrl.Geraetedaten"/> für die
    /// Stammfelder, <c>AnlagenTemperaturen.VorlaufAusKennlinien</c> für die
    /// Vorlaufstufe). Neu ist nur, dass der Dialog SAGT, welche der beiden er zeigt —
    /// eine Katalogkennlinie an einer Anlage ist eine Herleitung und keine
    /// Projektwahrheit, denn der Lauf rechnet ausschließlich mit
    /// <c>Tab_Kenndaten</c>.</para>
    ///
    /// <para><b>Der dritte Fall bleibt gültig:</b> Solange der Anwender im Dialog eine
    /// Wärmepumpe erst AUSWÄHLT, trägt <c>Daten.IdWp</c> die Katalog-Id — die
    /// Projektkopie legt <c>WizardCtrl</c> erst beim Speichern an. Dann ist der
    /// Katalog die einzige Quelle, und nachzuholen gibt es nichts.</para>
    /// </summary>
    internal static class WaermepumpeKennlinienCtrl
    {
        /// <summary>Woher der gezeigte Satz stammt.</summary>
        internal enum Herkunft
        {
            /// <summary>Es gibt keine Kennlinien — weder im Projekt noch im Katalog.</summary>
            Ohne = 0,

            /// <summary>Aus der Projektkopie (<c>Tab_Kenndaten</c>) — das, womit der Lauf rechnet.</summary>
            Projekt = 1,

            /// <summary>Aus dem Stammkatalog (<c>Tab_Kenndaten_STAMM</c>) — eine Herleitung.</summary>
            Katalog = 2
        }

        /// <summary>Der Satz samt seiner Herkunft.</summary>
        /// <param name="Satz">Die Reihen für die zwei Bilder.</param>
        /// <param name="Woher">Projektkopie, Katalog oder gar nichts.</param>
        /// <param name="KatalogId">
        /// Der Stammsatz, aus dem der Rückfall kommt (0, wenn keiner gefunden wurde).
        /// </param>
        /// <param name="Nachholbar">
        /// Lässt sich die Projektkopie der Kennlinien NACHHOLEN? Das setzt beides
        /// voraus: eine vorhandene Gerätekopie im Projekt UND einen Katalogsatz mit
        /// Kennlinien. Eine noch nicht gespeicherte Auswahl hat keine Gerätekopie —
        /// dort wäre der Knopf ein Versprechen ohne Ziel.
        /// </param>
        internal sealed record Quelle(KennlinienSatz Satz, Herkunft Woher, int KatalogId, bool Nachholbar);

        /// <summary>Nichts gefunden.</summary>
        internal static readonly Quelle Leer = new Quelle(KennlinienSatz.Leer, Herkunft.Ohne, 0, false);

        /// <summary>
        /// Die WÄRME-Kennlinien zu der Id, die eine Anlagenzeile in <c>ID_WP</c> führt —
        /// Projektkopie vor Stammkatalog.
        /// </summary>
        internal static Quelle FuerAnlage(int idWp)
        {
            if (idWp <= 0) return Leer;

            // (1) Die Projektkopie. Sie ist die Wahrheit des Laufs.
            KennlinienSatz projekt = KenndatenCtrl.ReihenProjekt(idWp);
            if (projekt.Vorlaeufe.Count > 0)
                return new Quelle(projekt, Herkunft.Projekt, 0, false);

            // (2) Gibt es die Gerätekopie überhaupt? Dann führt der Bezeichner zum
            //     Katalogsatz — die Kopie trägt ihn unverändert (CopyFromStamm).
            string bezeichner = BezeichnerDerKopie(idWp);
            bool istKopie = bezeichner != null;

            int katalogId = istKopie
                ? DataRepository.GetIdByName(WPStammCtrl.TABLE, "Bezeichner", bezeichner)
                : (IstKatalogsatz(idWp) ? idWp : 0);

            if (katalogId <= 0) return Leer;

            // (3) Der Katalogsatz gleichen Namens.
            KennlinienSatz katalog = KenndatenCtrl.Reihen(katalogId);
            if (katalog.Vorlaeufe.Count == 0) return new Quelle(KennlinienSatz.Leer, Herkunft.Ohne, katalogId, false);

            return new Quelle(katalog, Herkunft.Katalog, katalogId, istKopie);
        }

        /// <summary>
        /// Die Vorlaufstufen derselben Quelle — die Auswahlliste „Vorlauf" des
        /// Anlagendialogs. Sie hing an derselben falschen Tabelle wie die Bilder und
        /// blieb bei jeder gespeicherten Anlage leer.
        /// </summary>
        internal static System.Collections.Generic.IReadOnlyList<int> VorlaeufeFuerAnlage(int idWp)
        {
            return FuerAnlage(idWp).Satz.Vorlaeufe;
        }

        /// <summary>
        /// Der Bezeichner der Gerätekopie, oder <c>null</c>, wenn <paramref name="idWp"/>
        /// keine Zeile in <c>Tab_WP</c> ist.
        /// </summary>
        private static string BezeichnerDerKopie(int idWp)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Bezeichner FROM Tab_WP WHERE ID = ?", new DbParam("@id", idWp));
            if (dt == null || dt.Rows.Count == 0) return null;
            object v = dt.Rows[0]["Bezeichner"];
            return v == DBNull.Value ? "" : v.ToString();
        }

        /// <summary>Steht diese Id im STAMMKATALOG? (Der Fall der noch nicht gespeicherten Wahl.)</summary>
        private static bool IstKatalogsatz(int idWp)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_WP_STAMM WHERE ID = ?", new DbParam("@id", idWp));
            return v != null && v != DBNull.Value && Convert.ToInt32(v) > 0;
        }
    }
}
