using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Was mit den Kostenpositionen (<c>Tab_ProjektWerte</c>) einer Anlage geschieht, wenn
    /// die Anlagenzeile verschwindet. <c>Tab_ProjektWerte.ID_Anlage</c> trägt keinen
    /// Fremdschlüssel — ohne diesen Baustein zeigten die Positionen danach ins Leere.
    ///
    /// <para><b>Löschen</b> (eine Anlage wird entfernt, Anwenderentscheid 07.10.2026 nach
    /// dem Vorbild von <c>WizardCtrl.Del_Projekt_ID_Waermeerzeuger</c>): Eine entfernte
    /// Anlage hinterlässt keine Kosten. Genutzt von Kältemaschine löschen und Projektpuffer
    /// entfernen.</para>
    ///
    /// <para><b>Umhängen</b> (die Komponentenübernahme ersetzt die Anlagen eines Gewerks):
    /// Die Positionen der ersetzten Anlagen wandern der Reihe nach (beide Seiten nach ID)
    /// auf die neuen Anlagen, Beträge unverändert. Bleibt eine alte Anlage ohne Gegenstück,
    /// werden ihre Positionen lose (<c>ID_Anlage</c> und Geräteanker leer) und gezählt —
    /// der leere Anker verhindert, dass <c>KostenProjektPositionenCtrl.ZuordnungReparieren</c>
    /// sie über eine zufällig wiederverwendete Geräte-ID (<c>MAX(ID)+1</c>) wieder anhängt.</para>
    /// </summary>
    internal static class AnlagenKostenpositionen
    {
        private const string SQL_LOESCHEN =
            "DELETE FROM Tab_ProjektWerte WHERE ProjektID = ? AND ID_Anlage = ?";

        private const string SQL_UMHAENGEN =
            "UPDATE Tab_ProjektWerte SET ID_Anlage = ?, ID_AnlageGeraet = ? " +
            "WHERE ProjektID = ? AND ID_Anlage = ?";

        private const string SQL_LOESEN =
            "UPDATE Tab_ProjektWerte SET ID_Anlage = NULL, ID_AnlageGeraet = NULL " +
            "WHERE ProjektID = ? AND ID_Anlage = ?";

        /// <summary>
        /// Löscht die Kostenpositionen der Anlage <paramref name="idAnlage"/> im Projekt.
        /// Mit <paramref name="v"/> in dessen Transaktion, sonst über <c>DataRepository</c>.
        /// </summary>
        /// <returns>Zahl der gelöschten Positionen; 0 auch dann, wenn die Spalte fehlt.</returns>
        internal static int Loeschen(DbVorgang v, int idProjekt, int idAnlage)
        {
            if (idProjekt <= 0 || idAnlage <= 0) return 0;
            if (!KostenPositionCtrl.StelleSpaltenSicher()) return 0;
            DbParam[] p = { new DbParam("@p", idProjekt), new DbParam("@a", idAnlage) };
            if (v != null) return v.Ausfuehren(SQL_LOESCHEN, p);
            DataRepository.ExecuteSQL(SQL_LOESCHEN, p);
            return 0;
        }

        /// <summary>
        /// Hängt die Positionen der Anlagen <paramref name="alteAnlagen"/> der Reihe nach auf
        /// <paramref name="neueAnlagen"/> um (Geräteanker aus <paramref name="neueGeraete"/>,
        /// 0 = leer). Positionen alter Anlagen ohne Gegenstück werden gelöst.
        /// </summary>
        /// <returns>Zahl der gelösten (nun losen) Positionen.</returns>
        internal static int Umhaengen(DbVorgang v, int idProjekt, IList<int> alteAnlagen,
                                      IList<int> neueAnlagen, IList<int> neueGeraete)
        {
            if (v == null) throw new ArgumentNullException(nameof(v));
            if (idProjekt <= 0 || alteAnlagen == null) return 0;
            int lose = 0;
            for (int i = 0; i < alteAnlagen.Count; i++)
            {
                int alt = alteAnlagen[i];
                if (alt <= 0) continue;
                if (neueAnlagen != null && i < neueAnlagen.Count && neueAnlagen[i] > 0)
                {
                    int geraet = neueGeraete != null && i < neueGeraete.Count ? neueGeraete[i] : 0;
                    v.Ausfuehren(SQL_UMHAENGEN,
                        new DbParam("@a", neueAnlagen[i]),
                        new DbParam("@g", geraet > 0 ? (object)geraet : DBNull.Value),
                        new DbParam("@p", idProjekt),
                        new DbParam("@alt", alt));
                }
                else
                    lose += v.Ausfuehren(SQL_LOESEN,
                        new DbParam("@p", idProjekt),
                        new DbParam("@alt", alt));
            }
            return lose;
        }
    }
}
