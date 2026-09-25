using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using WindowsFormsApplication1;

namespace Gebaeudevergleich
{
    /// <summary>
    /// <b><c>variante</c> — die Projektwirkung vorbereiten</b> (Anwenderentscheid F1 = b): eine
    /// Kopie der <c>--db</c>, in der JEDES Gebäude auf demselben Rechenweg steht
    /// (<c>Gebaeude_Modell</c> = <c>TAGESBILANZ</c> bzw. <c>VDI6007</c>). Zwei solche Kopien,
    /// je mit <c>EPOS.Referenzlauf lauf</c> gerechnet und mit <c>vergleich</c> gegenübergestellt,
    /// zeigen, was der Wechsel des Gebäudewegs an Deckung, Restwärme und Erzeugern bewirkt.
    ///
    /// <para><b>Die Quelle bleibt unverändert.</b> Kopiert wird mit <c>VACUUM INTO</c> über eine
    /// nur lesende Verbindung; das eine <c>UPDATE</c> läuft auf der KOPIE, über eine eigene
    /// Verbindung, nicht über die Zugriffsschicht (deren Schreibsperre bleibt stehen). Der
    /// SHA-256 der Quelle wird davor und danach verglichen.</para>
    /// </summary>
    internal static class Variante
    {
        internal static int Ausfuehren(Argumente arg, Ausgabe aus)
        {
            aus.Konsole("Gebaeudevergleich variante " + arg.Modell);
            string bindung = Einstieg.DatenbankBinden(arg.Db);
            if (bindung != null) return Abbruch(aus, bindung);

            int stand = Schemapruefung.Stand();
            aus.Protokoll("Datenbank " + arg.Db + ", Schemastand " + Zahl(stand) + ", Zielstand " + Zahl(SchemaStand.Zielversion));
            string schema = Schemapruefung.Pruefen(stand);
            if (schema != null) return Abbruch(aus, schema);
            aus.Scharfschalten(Namensbereinigung.AusDatenbank());

            // Die Zugriffsschicht wird hier nicht mehr gebraucht - freigeben, bevor gehasht wird.
            DataRepository.PfadUeberschreibung = null;
            SqliteConnection.ClearAllPools();

            string hashVorher = Sqlitehilfe.Sha256(arg.Db);
            aus.ProtokollPruefsumme("SHA-256 Quelle vorher:  ", hashVorher);

            using (SqliteConnection v = Sqlitehilfe.NurLesend(arg.Db))
                Sqlitehilfe.VacuumInto(v, arg.Ziel);
            int zeilen = Sqlitehilfe.ModellSetzen(arg.Ziel, arg.Modell);
            string integritaet = Sqlitehilfe.Integritaet(arg.Ziel);
            SqliteConnection.ClearAllPools();
            aus.Protokoll("Gebaeude_Modell = " + arg.Modell + " an " + Zahl(zeilen) + " Gebäudezeilen");
            aus.Protokoll("integrity_check der Variante: " + integritaet);

            string hashNachher = Sqlitehilfe.Sha256(arg.Db);
            aus.ProtokollPruefsumme("SHA-256 Quelle nachher: ", hashNachher);
            if (hashNachher != hashVorher)
                return Abbruch(aus, "Die Quelle hat sich während der Variante verändert: " + arg.Db);
            if (integritaet != "ok")
            {
                Loeschen(arg.Ziel);
                return Abbruch(aus, "Die Variante besteht den integrity_check nicht: " + integritaet);
            }

            aus.ProtokollPruefsumme("SHA-256 Variante: ", Sqlitehilfe.Sha256(arg.Ziel));
            aus.Konsole("Variante geschrieben: " + Zahl(zeilen) + " Gebäudezeilen auf " + arg.Modell);
            return Program.OHNE_ROT;
        }

        private static void Loeschen(string ziel)
        {
            SqliteConnection.ClearAllPools();
            foreach (string d in new[] { ziel, ziel + "-wal", ziel + "-shm", ziel + "-journal" })
                if (File.Exists(d)) File.Delete(d);
        }

        private static string Zahl(int z) => z.ToString(CultureInfo.InvariantCulture);

        private static int Abbruch(Ausgabe aus, string grund)
        {
            aus.Fehler("Abbruch: " + grund);
            return Program.ABBRUCH;
        }
    }
}
