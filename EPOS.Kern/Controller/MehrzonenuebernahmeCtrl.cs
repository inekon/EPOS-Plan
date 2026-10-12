using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace WindowsFormsApplication1
{
    /// <summary>Was die Prüfung der gespeicherten Importquelle vor der Mehrzonenübernahme ergeben hat (GD-4).</summary>
    public enum MehrzonenquellZustand
    {
        /// <summary>Die gewählte Datei trägt das Format und die Prüfsumme der gespeicherten Quelle.</summary>
        Passend,

        /// <summary>Das Gebäude hat keine gespeicherte Importquelle.</summary>
        KeineQuelle,

        /// <summary>Die Datei fehlt oder lässt sich nicht öffnen.</summary>
        DateiFehlt,

        /// <summary>Die Endung trägt kein Format oder ein anderes als das der Quelle.</summary>
        FormatAbweichend,

        /// <summary>Der SHA-256 der Datei weicht vom gespeicherten ab.</summary>
        PruefsummeAbweichend
    }

    /// <summary>Das Ergebnis der Quellprüfung: Zustand, Meldung in der Anzeigekultur (leer bei Passend), Prüfsumme der Datei.</summary>
    internal sealed record Mehrzonenquellpruefung(MehrzonenquellZustand Zustand, string Meldung, string HashDatei)
    {
        internal bool Passend => Zustand == MehrzonenquellZustand.Passend;
    }

    /// <summary>Was das Ersetzen ergeben hat: Ok, Meldung (leer bei Erfolg) und die Zahl der geschriebenen Zonen.</summary>
    internal sealed record Mehrzonenersetzung(bool Ok, string Meldung, int Zonen)
    {
        internal static Mehrzonenersetzung Fehler(string meldung) => new Mehrzonenersetzung(false, meldung ?? "", 0);
    }

    /// <summary>
    /// <b>„Als Mehrzonenmodell übernehmen…“</b> (Entwurf Gebäudedialog 2.4, Welle GD-4; Entscheid E130 F7) — die
    /// Datenbankseite: die gespeicherte Importquelle eines Projektgebäudes, ihre Prüfsumme gegen die neu gewählte Datei
    /// und das <b>Ersetzen der Zonen in einem Vorgang</b>.
    ///
    /// <para><b>Nur aus der gespeicherten Quelle.</b> Weicht die Prüfsumme ab, fehlt die Datei oder trägt sie ein anderes
    /// Format, ist das ein benannter Zustand; es gibt keinen stillen Rückfall. Eine andere Datei ist ein neuer Import.</para>
    ///
    /// <para><b>Ersetzen in einem Vorgang.</b> Im selben <see cref="DbVorgang"/> fallen die Zonen des Gebäudes samt Bauteilen,
    /// Luftströmen und Zonenkalendern (die Kalender, Wochen und Vorgaben mit <c>ID_Zone</c> folgen über
    /// <c>ON DELETE CASCADE</c>) und die alten Importquellen samt Paarungen und Grundrissen; danach schreibt derselbe Weg
    /// wie beim Anlegen (<c>WizardCtrl.GebaeudeZuordnungAnlegen</c>) Baustoffzuordnungen, Bauteilvorschlag
    /// (<see cref="GebaeudeZonenCtrl.VorschlagSchreiben"/>, Zonen mit Profil und Kalendern) und Herkunft. Scheitert ein Schritt,
    /// rollt der Vorgang zurück: Der alte Stand bleibt. Die Felder des Gebäudes außerhalb der Zonen und seine eigenen
    /// Kalender (ohne Zone) bleiben unberührt.</para>
    /// </summary>
    internal static class MehrzonenuebernahmeCtrl
    {
        /// <summary>Zeichen einer gekürzten Prüfsumme in der Meldung.</summary>
        internal const int HASH_KURZ = 12;

        /// <summary>Die jüngste gespeicherte Importquelle des Gebäudes (<c>Tab_Gebaeude.ID</c>); <c>null</c> ohne Quelle.</summary>
        internal static ImportquelleModel Quelle(int idGebaeude)
            => idGebaeude <= 0 || !ImportzuordnungSchema.Vollstaendig() ? null
             : new GebaeudeImportCtrl().LesenQuellen(idGebaeude).FirstOrDefault();

        /// <summary>Der SHA-256 eines Datenstroms in Kleinbuchstaben — dieselbe Schreibweise wie <c>Tab_Importquelle.Hash</c>.</summary>
        internal static string Pruefsumme(Stream inhalt) => Convert.ToHexStringLower(SHA256.HashData(inhalt));

        /// <summary>
        /// Prüft die gewählte Datei gegen die gespeicherte Quelle: Format über die Endung, dann die Prüfsumme über den ganzen
        /// Inhalt. Gelesen wird nur zum Hashen; geschrieben wird nichts.
        /// </summary>
        internal static Mehrzonenquellpruefung QuellePruefen(string pfad, ImportquelleModel quelle)
        {
            if (quelle == null)
                return new Mehrzonenquellpruefung(MehrzonenquellZustand.KeineQuelle, MyResource.Resource.GEBMZ_MSG_KEINE_QUELLE, "");
            string name = GebaeudeQuelle.NurName(pfad ?? "");
            GebaeudeImportProfil profil = GebaeudeImportProfil.FuerDatei(pfad ?? "");
            if (profil == null || !string.Equals(profil.Format, quelle.Format, StringComparison.Ordinal))
                return new Mehrzonenquellpruefung(MehrzonenquellZustand.FormatAbweichend,
                    Formatieren(MyResource.Resource.GEBMZ_MSG_FORMAT, name, quelle.Format), "");

            string hash;
            try
            {
                using FileStream strom = File.OpenRead(pfad);
                hash = Pruefsumme(strom);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException
                                       || ex is NotSupportedException)
            {
                return new Mehrzonenquellpruefung(MehrzonenquellZustand.DateiFehlt,
                    Formatieren(MyResource.Resource.GEBMZ_MSG_DATEI_FEHLT, name, ex.Message), "");
            }
            string gespeichert = (quelle.Hash ?? "").Trim();
            if (!string.Equals(hash, gespeichert, StringComparison.OrdinalIgnoreCase))
                return new Mehrzonenquellpruefung(MehrzonenquellZustand.PruefsummeAbweichend,
                    Formatieren(MyResource.Resource.GEBMZ_MSG_PRUEFSUMME, name, Kurz(hash), Kurz(gespeichert)), hash);
            return new Mehrzonenquellpruefung(MehrzonenquellZustand.Passend, "", hash);
        }

        /// <summary>
        /// <b>Ersetzt die Zonen des Projektgebäudes</b> durch die der Herkunft — in EINEM Vorgang mit Rückweg bei Fehler.
        /// Abgelehnt, bevor etwas geschrieben ist: eine Herkunft ohne Bauteilvorschlag (der Schalter „als Zonen mit
        /// Bauteilen“ stand nicht) und ein Gebäude, das keine Projektkopie ist.
        /// </summary>
        /// <param name="idGebaeude">Die Projektkopie (<c>Tab_Gebaeude.ID</c>).</param>
        /// <param name="herkunft">Die Herkunft des Importlaufs (Quelle, Paarungen, Vorschlag, Grundrisse).</param>
        internal static Mehrzonenersetzung Ersetzen(int idGebaeude, GebaeudeImportHerkunft herkunft)
        {
            if (herkunft?.Vorschlag == null || herkunft.Quelle == null)
                return Mehrzonenersetzung.Fehler(MyResource.Resource.GEBMZ_MSG_OHNE_ZONEN);
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(v))
                {
                    object projekt = v.Skalar("SELECT COALESCE(\"ID_Projekt\", 0) FROM \"Tab_Gebaeude\" WHERE \"ID\" = ?",
                                              new DbParam("@g", idGebaeude));
                    int idProjekt = projekt == null || projekt == DBNull.Value ? 0 : Convert.ToInt32(projekt, CultureInfo.InvariantCulture);
                    if (idProjekt <= 0)
                    {
                        v.Rollback();
                        return Mehrzonenersetzung.Fehler(Formatieren(MyResource.Resource.GEBMZ_MSG_KEINE_PROJEKTKOPIE,
                                                                    idGebaeude.ToString(CultureInfo.InvariantCulture)));
                    }

                    // 1) Der alte Zonenstand: erst die Bauteile (ihr ID_Nachbarzone verweist ohne Kaskade auf eine Zone),
                    //    dann die Zonen - Luftstroeme, Zonenkalender und Paarungen folgen ueber die Kaskade.
                    v.Ausfuehren("DELETE FROM \"Tab_Bauteil\" WHERE \"ID_Zone\" IN " +
                                 "(SELECT \"ID\" FROM \"Tab_Zone\" WHERE \"ID_Gebaeude\" = ?)", new DbParam("@g", idGebaeude));
                    v.Ausfuehren("DELETE FROM \"Tab_Zone\" WHERE \"ID_Gebaeude\" = ?", new DbParam("@g", idGebaeude));

                    // 2) Die alten Quellen dieses Gebaeudes samt Paarungen und Grundrissen; die Herkunft schreibt eine neue.
                    v.Ausfuehren("DELETE FROM \"" + ImportzuordnungSchema.TAB_QUELLE + "\" WHERE \"ID_Gebaeude\" = ?",
                                 new DbParam("@g", idGebaeude));

                    // 3) Derselbe Schreibweg wie beim Anlegen: Baustoffe, Vorschlag, Gebaeudekonditionierung, Herkunft.
                    string fehler = null;
                    IReadOnlyList<GebaeudeQuellzuordnung> paarungen = herkunft.Paarungen ?? Array.Empty<GebaeudeQuellzuordnung>();
                    if (herkunft.Baustoffzuordnungen is { Count: > 0 } zuordnungen)
                    {
                        BaustoffabgleichCtrl.Ergebnis b = BaustoffabgleichCtrl.Schreiben(idProjekt, zuordnungen, v);
                        if (!b.Ok) fehler = b.Meldung ?? "";
                    }
                    int zonen = 0;
                    if (fehler == null)
                    {
                        GebaeudeZonenCtrl.Vorschlagsergebnis z = new GebaeudeZonenCtrl().VorschlagSchreiben(idGebaeude, herkunft.Vorschlag, v);
                        if (!z.Ok) fehler = z.Meldung ?? "";
                        else
                        {
                            zonen = z.Zonen?.Count ?? (z.IdZone > 0 ? 1 : 0);
                            paarungen = paarungen.Concat(z.Zuordnungen).ToList();
                        }
                    }
                    if (fehler == null && (herkunft.Gebaeudekonditionierung != null || herkunft.Gebaeudeprofil != null))
                        fehler = ZonenplanCtrl.ProjektdateiUebernehmen(idGebaeude, null, herkunft.Gebaeudekonditionierung,
                                                                       herkunft.Gebaeudeprofil);
                    if (fehler == null)
                    {
                        GebaeudeImportCtrl.Ergebnis h = new GebaeudeImportCtrl().SchreibeHerkunft(
                            idGebaeude, herkunft.Quelle, paarungen, v, herkunft.Raumgrundrisse);
                        if (!h.Ok) fehler = h.Meldung ?? "";
                    }
                    if (fehler != null)
                    {
                        v.Rollback();
                        return Mehrzonenersetzung.Fehler(Formatieren(MyResource.Resource.GEBMZ_MSG_NICHT_UEBERNOMMEN, fehler));
                    }
                    v.Commit();
                    return new Mehrzonenersetzung(true, "", zonen);
                }
            }
            catch (Exception ex)
            {
                return Mehrzonenersetzung.Fehler(Formatieren(MyResource.Resource.GEBMZ_MSG_NICHT_UEBERNOMMEN, ex.Message));
            }
        }

        private static string Kurz(string hash)
            => string.IsNullOrEmpty(hash) ? "—" : hash.Length <= HASH_KURZ ? hash : hash.Substring(0, HASH_KURZ) + "…";

        private static string Formatieren(string vorlage, params object[] werte)
        {
            try { return string.Format(CultureInfo.CurrentCulture, vorlage ?? "", werte); }
            catch (FormatException) { return vorlage ?? ""; }
        }
    }
}
