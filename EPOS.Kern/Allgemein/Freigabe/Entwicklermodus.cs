using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Woher der Entwicklermodus des laufenden Prozesses kommt.</summary>
    public enum Entwicklermodusquelle
    {
        /// <summary>Der Modus ist aus (oder gesperrt).</summary>
        Keine,

        /// <summary>Umgebungsvariable <see cref="Entwicklermodus.UMGEBUNGSVARIABLE"/> = 1.</summary>
        Umgebung,

        /// <summary>Startargument <see cref="Entwicklermodus.STARTARGUMENT"/>.</summary>
        Startargument,

        /// <summary>Dauerhafte Einstellung <see cref="Entwicklermodus.SCHLUESSEL"/> = 1 (Tastenfolge).</summary>
        Einstellung,
    }

    /// <summary>
    /// Der Entwicklermodus (Konzept Administration und Support, Abschnitte 3.1 und 3.3;
    /// Entwicklerhandbuch Abschnitt 1). Er wird EINMAL beim Start ermittelt; niemand sonst
    /// liest dafür Registry, Umgebung oder Startzeile.
    ///
    /// <para><b>Rangfolge.</b> Die maschinenweite Sperre des Administrators
    /// (<see cref="SCHLUESSEL_SPERRE"/>, gelesen über <c>Dienste.Einstellungen.LiesMaschine</c>
    /// wie der KI-Abschalter) schlägt alles: Die Stufe bleibt <see cref="Freigabestufe.Anwender"/>.
    /// Sonst gilt die erste Quelle aus Umgebungsvariable, Startargument, Einstellung.</para>
    ///
    /// <para>Die Umgebung erreicht der Baustein nur über die Dienste; die Umgebungsvariable
    /// liest er selbst, im Testzugang über eine hereingereichte Funktion.</para>
    /// </summary>
    public static class Entwicklermodus
    {
        /// <summary>Einstellung des Anwenders (Zahl, 1 = an), über <c>Dienste.Einstellungen</c>.</summary>
        public const string SCHLUESSEL = "Entwicklermodus";

        /// <summary>Maschinenweite Sperre („1" = gesperrt), über <c>Dienste.Einstellungen.LiesMaschine</c>.</summary>
        public const string SCHLUESSEL_SPERRE = "EntwicklermodusGesperrt";

        /// <summary>Umgebungsvariable; der Wert „1" schaltet den Modus für den Lauf ein.</summary>
        public const string UMGEBUNGSVARIABLE = "EPOS_ENTWICKLER";

        /// <summary>Startargument; schaltet den Modus für den Lauf ein.</summary>
        public const string STARTARGUMENT = "--entwickler";

        private static readonly object _sperre = new object();

        /// <summary>Die gültige Freigabestufe des Laufs.</summary>
        public static Freigabestufe Stufe { get; private set; } = Freigabestufe.Anwender;

        /// <summary>true, wenn die Stufe <see cref="Freigabestufe.Entwickler"/> ist.</summary>
        public static bool Aktiv => Stufe == Freigabestufe.Entwickler;

        /// <summary>Woher der Modus kommt; <see cref="Entwicklermodusquelle.Keine"/>, wenn er aus ist.</summary>
        public static Entwicklermodusquelle Quelle { get; private set; } = Entwicklermodusquelle.Keine;

        /// <summary>Die maschinenweite Sperre des Administrators liegt.</summary>
        public static bool Gesperrt { get; private set; }

        /// <summary>Ressourcentext zur Quelle (Tooltip der Marke „Entwickler").</summary>
        public static string QuelleText
        {
            get
            {
                if (Gesperrt) return MyResource.Resource.ENTW_QUELLE_GESPERRT;
                switch (Quelle)
                {
                    case Entwicklermodusquelle.Umgebung: return MyResource.Resource.ENTW_QUELLE_UMGEBUNG;
                    case Entwicklermodusquelle.Startargument: return MyResource.Resource.ENTW_QUELLE_STARTARGUMENT;
                    case Entwicklermodusquelle.Einstellung: return MyResource.Resource.ENTW_QUELLE_EINSTELLUNG;
                    default: return MyResource.Resource.ENTW_QUELLE_KEINE;
                }
            }
        }

        /// <summary>Ermittelt Stufe und Quelle aus Sperre, Umgebung, Startargumenten und Einstellung.</summary>
        /// <param name="startargumente">Die Startargumente des Prozesses (ohne Programmnamen); null = keine.</param>
        public static void Ermitteln(IReadOnlyList<string> startargumente)
        {
            Ermitteln(startargumente, Environment.GetEnvironmentVariable);
        }

        /// <summary>
        /// Testzugang: wie <see cref="Ermitteln(IReadOnlyList{string})"/>, die Umgebungsvariable
        /// kommt aber aus <paramref name="umgebung"/> (Name → Wert, null = nicht gesetzt).
        /// </summary>
        public static void Ermitteln(IReadOnlyList<string> startargumente, Func<string, string> umgebung)
        {
            lock (_sperre)
            {
                Gesperrt = SperreGesetzt();
                if (Gesperrt)
                {
                    Setzen(Entwicklermodusquelle.Keine);
                    return;
                }

                if (Ist(Lesen(umgebung)))
                    Setzen(Entwicklermodusquelle.Umgebung);
                else if (ArgumentGesetzt(startargumente))
                    Setzen(Entwicklermodusquelle.Startargument);
                else if (EinstellungGesetzt())
                    Setzen(Entwicklermodusquelle.Einstellung);
                else
                    Setzen(Entwicklermodusquelle.Keine);
            }
        }

        /// <summary>
        /// Schaltet den Modus dauerhaft ein (Tastenfolge): schreibt die Einstellung 1.
        /// false, wenn die Sperre liegt — dann bleibt alles, wie es ist.
        /// </summary>
        public static bool Einschalten()
        {
            lock (_sperre)
            {
                if (Gesperrt) return false;
                Dienste.Einstellungen.SchreibZahl(SCHLUESSEL, 1);
                // Eine ranghöhere Quelle (Umgebung, Startargument) bleibt die genannte.
                if (Quelle == Entwicklermodusquelle.Keine) Setzen(Entwicklermodusquelle.Einstellung);
                return true;
            }
        }

        /// <summary>
        /// Schaltet den Modus aus: löscht die Einstellung. Läuft er über Umgebung oder
        /// Startargument oder ist er gar nicht an, geschieht nichts, und
        /// <paramref name="grund"/> nennt den Grund (Ressourcentext).
        /// </summary>
        public static bool Ausschalten(out string grund)
        {
            lock (_sperre)
            {
                switch (Quelle)
                {
                    case Entwicklermodusquelle.Umgebung:
                        grund = MyResource.Resource.ENTW_AUS_UMGEBUNG;
                        return false;
                    case Entwicklermodusquelle.Startargument:
                        grund = MyResource.Resource.ENTW_AUS_STARTARGUMENT;
                        return false;
                    case Entwicklermodusquelle.Einstellung:
                        Dienste.Einstellungen.Loesche(SCHLUESSEL);
                        Setzen(Entwicklermodusquelle.Keine);
                        grund = "";
                        return true;
                    default:
                        grund = MyResource.Resource.ENTW_AUS_NICHT_AKTIV;
                        return false;
                }
            }
        }

        // ------------------------------------------------------------------
        //  Lesen — jede Quelle in eigener Fehlerschranke: ein Lesefehler heißt „nicht gesetzt".
        // ------------------------------------------------------------------

        private static void Setzen(Entwicklermodusquelle quelle)
        {
            Quelle = quelle;
            Stufe = quelle == Entwicklermodusquelle.Keine ? Freigabestufe.Anwender : Freigabestufe.Entwickler;
        }

        private static bool SperreGesetzt()
        {
            try { return Ist(Dienste.Einstellungen.LiesMaschine(SCHLUESSEL_SPERRE)); }
            catch { return false; }
        }

        private static string Lesen(Func<string, string> umgebung)
        {
            try { return umgebung == null ? null : umgebung(UMGEBUNGSVARIABLE); }
            catch { return null; }
        }

        private static bool ArgumentGesetzt(IReadOnlyList<string> startargumente)
        {
            if (startargumente == null) return false;
            foreach (string a in startargumente)
            {
                if (a != null && string.Equals(a.Trim(), STARTARGUMENT, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool EinstellungGesetzt()
        {
            try { return Dienste.Einstellungen.LiesZahl(SCHLUESSEL, 0) == 1; }
            catch { return false; }
        }

        private static bool Ist(string wert)
        {
            return wert != null && wert.Trim() == "1";
        }
    }
}
