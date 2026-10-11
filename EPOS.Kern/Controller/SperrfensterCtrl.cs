using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Sperrfenster einer Wärmepumpe</b> (Welle V14, Tabelle <c>Tab_Sperrfenster</c>): lesen,
    /// samt Altfenster für die Anzeige, schreiben je Anlage, Vorlagen. Alle Zugriffe über
    /// <see cref="DataRepository"/> mit <c>?</c>-Parametern.
    /// </summary>
    public static class SperrfensterCtrl
    {
        /// <summary>Die Vorlagen der Oberfläche — dieselben Namen wie das Sperrprofil der Pufferauslegung.</summary>
        public static readonly IReadOnlyList<string> VORLAGEN = new[] { "KEINE", "ZWEI_MAL_ZWEI", "DREI_MAL_ZWEI" };

        private const string SQL_LOESCHEN =
            "DELETE FROM " + WaermepumpeSperrprofilSchema.TAB + " WHERE ID_Energieanlage = ?";

        private const string SQL_EINFUEGEN =
            "INSERT INTO " + WaermepumpeSperrprofilSchema.TAB +
            " (ID_Energieanlage, Von_h, Dauer_h, Wochentage, Heizstab_gesperrt, Reihenfolge) VALUES (?, ?, ?, ?, ?, ?)";

        private const string SQL_ALTFENSTER_AUS =
            "UPDATE Tab_Energieanlagen SET Sperrung = 0 WHERE ID = ?";

        /// <summary>Steht die Tabelle (Schemaschritt <see cref="WaermepumpeSperrprofilSchema.SCHRITT"/>)?</summary>
        public static bool TabelleVorhanden() => WaermepumpeSperrprofilSchema.Vollstaendig();

        /// <summary>Die gespeicherten Fenster der Anlage, in Listenreihenfolge.</summary>
        public static List<Sperrfenster> Lesen(int idAnlage) => Sperrprofil.Lesen(idAnlage);

        /// <summary>
        /// Das Altfenster der Anlagenzeile als Sperrfenster — <c>null</c>, wenn es nicht aktiv ist oder keine
        /// Stunde sperrt. Jeden Tag, Heizstab NICHT mitgesperrt: so rechnet das Altfenster.
        /// </summary>
        public static Sperrfenster Altfenster(bool sperrung, int von, int bis)
        {
            if (!sperrung || bis <= von) return null;
            int a = Math.Max(0, von), b = Math.Min(24, bis);
            if (b <= a) return null;
            return new Sperrfenster
            {
                VonH = a, DauerH = b - a, Wochentage = WaermepumpeSperrprofilSchema.ALLE_TAGE, HeizstabGesperrt = false
            };
        }

        /// <summary>
        /// Die Fensterliste für den Dialog: das Altfenster (falls aktiv) als erste Zeile, dann die Zeilen
        /// der Tabelle. Beim Speichern dieser Liste wird das Altfenster in die Tabelle überführt.
        /// </summary>
        public static List<Sperrfenster> MitAltfenster(int idAnlage, bool sperrung, int von, int bis)
        {
            var l = new List<Sperrfenster>();
            Sperrfenster alt = Altfenster(sperrung, von, bis);
            if (alt != null) l.Add(alt);
            l.AddRange(Lesen(idAnlage));
            return l;
        }

        /// <summary>Die Fenster einer Vorlage (<see cref="VORLAGEN"/>), jeden Tag, Heizstab mitgesperrt.</summary>
        public static List<Sperrfenster> Vorlage(string name)
        {
            var l = new List<Sperrfenster>();
            foreach (PufferSperrfenster f in PufferSperrprofil.Fenster(name))
                l.Add(new Sperrfenster { VonH = f.BeginnH, DauerH = f.DauerH });
            return l;
        }

        /// <summary>
        /// Schreibt die Fenster der Anlage — die Liste ersetzt den Bestand, auch leer — und schaltet das
        /// Altfenster der Anlagenzeile ab (<c>Sperrung = 0</c>), damit es EINE Quelle gibt. In einem Vorgang.
        /// </summary>
        /// <param name="altfensterAus">Das Altfenster abschalten (Dialog); die Rettung beim Neuanlegen lässt es stehen.</param>
        /// <returns><c>false</c>, wenn die Tabelle fehlt, ein Fenster ungültig ist oder die Datenbank ablehnt.</returns>
        public static bool Schreiben(int idAnlage, IReadOnlyList<Sperrfenster> fenster, bool altfensterAus = true)
        {
            if (idAnlage <= 0 || !TabelleVorhanden()) return false;
            if (fenster != null)
                foreach (Sperrfenster f in fenster)
                    if (Pruefen(f) != null) return false;
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    v.Ausfuehren(SQL_LOESCHEN, new DbParam("@a", idAnlage));
                    int rang = 0;
                    if (fenster != null)
                        foreach (Sperrfenster f in fenster)
                        {
                            rang++;
                            v.Ausfuehren(SQL_EINFUEGEN,
                                new DbParam("@a", idAnlage),
                                new DbParam("@von", f.VonH),
                                new DbParam("@dauer", f.DauerH),
                                new DbParam("@tage", f.Wochentage),
                                new DbParam("@stab", f.HeizstabGesperrt ? 1 : 0),
                                new DbParam("@rang", rang));
                        }
                    if (altfensterAus) v.Ausfuehren(SQL_ALTFENSTER_AUS, new DbParam("@id", idAnlage));
                    v.Commit();
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Sperrfenster der Anlage " + idAnlage + " konnten nicht geschrieben werden: " + ex.Message);
                return false;
            }
        }

        /// <summary>Der Grund, aus dem ein Fenster ungültig ist; <c>null</c> = gültig.</summary>
        public static string Pruefen(Sperrfenster f)
        {
            if (f == null) return "leer";
            if (double.IsNaN(f.VonH) || f.VonH < 0 || f.VonH > 24) return "Beginn außerhalb 0 … 24 h";
            if (double.IsNaN(f.DauerH) || f.DauerH <= 0 || f.DauerH > 24) return "Dauer außerhalb 0 … 24 h";
            if (f.Wochentage < 1 || f.Wochentage > WaermepumpeSperrprofilSchema.ALLE_TAGE) return "kein Wochentag";
            return null;
        }
    }
}
