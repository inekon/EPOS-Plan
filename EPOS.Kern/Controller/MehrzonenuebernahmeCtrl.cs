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

    /// <summary>Was die Zusammenführung ergeben hat: Ok, Meldung (leer bei Erfolg) und die Zahl der Zonen der Datei.</summary>
    internal sealed record Mehrzonenersetzung(bool Ok, string Meldung, int Zonen)
    {
        internal static Mehrzonenersetzung Fehler(string meldung) => new Mehrzonenersetzung(false, meldung ?? "", 0);
    }

    /// <summary>
    /// <b>„Als Mehrzonenmodell übernehmen…“</b> (Entwurf Gebäudedialog 2.4, Welle GD-4; Entscheid E130 F7) — die
    /// Datenbankseite: die gespeicherte Importquelle eines Projektgebäudes, ihre Prüfsumme gegen die neu gewählte Datei
    /// und die <b>Zusammenführung in einem Vorgang</b> (MA-6, Teildatei <c>MehrzonenuebernahmeCtrl.Zusammenfuehrung.cs</c>).
    ///
    /// <para><b>Aus der gespeicherten Quelle oder einer geänderten Fassung.</b> Fehlt die Datei oder trägt sie ein anderes
    /// Format, ist das ein benannter Zustand; es gibt keinen stillen Rückfall. Eine abweichende Prüfsumme ist eine geänderte
    /// Fassung derselben Quelle: Sie läuft in die Zusammenführung mit Vorschau der Unterschiede (Entwurf Geführter
    /// Modellaufbau 2.6).</para>
    ///
    /// <para><b>Zusammenführen statt Ersetzen.</b> Zonen und Bauteile werden über ihre Quellkennung wiedergefunden und
    /// behalten ihre Id; Geometrie und Dateiwerte kommen aus der Datei, Ergänzungen von Hand bleiben (Regel je Feld
    /// Hand &gt; Datei &gt; Vorgabe). Scheitert ein Schritt, rollt der Vorgang zurück: Der alte Stand bleibt. Die Felder des
    /// Gebäudes außerhalb der Zonen und seine eigenen Kalender (ohne Zone) bleiben unberührt.</para>
    /// </summary>
    internal static partial class MehrzonenuebernahmeCtrl
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

        private static string Kurz(string hash)
            => string.IsNullOrEmpty(hash) ? "—" : hash.Length <= HASH_KURZ ? hash : hash.Substring(0, HASH_KURZ) + "…";

        private static string Formatieren(string vorlage, params object[] werte)
        {
            try { return string.Format(CultureInfo.CurrentCulture, vorlage ?? "", werte); }
            catch (FormatException) { return vorlage ?? ""; }
        }
    }
}
