using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Admin;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die plattformfreie Hülle der Verwaltung „Kältemittel"</b> (KM4-E3-c, Fachkonzept Kältemittel als
    /// Stammdatum 7.6): baut aus <see cref="KaeltemittelCtrl"/> den Parametersatz des
    /// <see cref="KaeltemittelDialog"/> — die Zeilen samt Anzeigetexten von Gruppe, PFAS-Status und Status und der
    /// Verwendung je Zeile, die Wertemengen aus <see cref="DbWerte"/> (<c>KAELTEMITTEL_*</c>) und die Schreibwege
    /// Speichern, Löschen und Schloss.
    ///
    /// <para><b>Die Regeln stehen im Kern</b> (Auslieferungszeile gesperrt, Code eindeutig und bei Verwendung fest,
    /// Löschen nur ohne Verwendung, Wertemengen); die Hülle übersetzt nur. Unter Windows trägt sie das Fenster
    /// <c>KaeltemittelFenster</c>, auf iOS <c>IosProjektQuelle.KaeltemittelGaben</c> in die <c>AppWurzel</c>.</para>
    /// </summary>
    public sealed class KaeltemittelHuelle
    {
        /// <summary>Fensterbreite unter Windows.</summary>
        public const int FENSTER_BREITE = 1180;

        /// <summary>Fensterhöhe unter Windows.</summary>
        public const int FENSTER_HOEHE = 820;

        /// <summary>Der Fenstertitel.</summary>
        public static string Titel() => MyResource.Resource.KMT_TITEL;

        /// <summary>Hat einer der Schreibwege etwas geschrieben?</summary>
        public bool Geaendert { get; private set; }

        /// <summary>Der Parametersatz des Dialogs (ohne <c>Geschlossen</c>, das setzt der Wirt).</summary>
        public IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Zeilen"] = Zeilen(),
                ["Neuladen"] = new Func<IReadOnlyList<KaeltemittelZeile>>(Zeilen),
                ["Pruefen"] = new Func<KaeltemittelZeile, string>(z => KaeltemittelCtrl.Pruefen(Satz(z))),
                ["Speichern"] = new Func<KaeltemittelZeile, KaeltemittelAntwort>(z =>
                    Antwort(KaeltemittelCtrl.Speichern(Satz(z)))),
                ["Loeschen"] = new Func<int, KaeltemittelAntwort>(id => Antwort(KaeltemittelCtrl.Loeschen(id))),
                ["Schloss"] = Schlosswege.Aus((ids, gesperrt) =>
                {
                    Auslieferungskennzeichen.Ergebnis e = KaeltemittelCtrl.SchlossSetzen(ids, gesperrt);
                    if (e != null && e.Ok) Geaendert = true;
                    return e;
                }),
                ["Gruppen"] = Gruppen(),
                ["Sicherheitsgruppen"] = Sicherheitsgruppen(),
                ["PfasWerte"] = PfasWerte(),
                ["Statuswerte"] = Statuswerte(),
                ["Lesemodus"] = !Schreibnaht.DarfSchreiben(),
            };
        }

        // =================================================================== Zeilen

        /// <summary>Die Zeilen der Tabelle mit Anzeigetexten und Verwendung.</summary>
        public static IReadOnlyList<KaeltemittelZeile> Zeilen()
        {
            var zeilen = new List<KaeltemittelZeile>();
            foreach (KaeltemittelCtrl.Kaeltemittelsatz s in KaeltemittelCtrl.Liste())
                zeilen.Add(new KaeltemittelZeile
                {
                    Id = s.Id,
                    Code = s.Code ?? "",
                    Bezeichnung = s.Bezeichnung ?? "",
                    Gruppe = s.Gruppe ?? "",
                    Gwp = s.GWP,
                    GwpQuelle = s.GWP_Quelle ?? "",
                    FGas = s.F_Gas,
                    Natuerlich = s.Natuerlich,
                    Sicherheitsgruppe = s.Sicherheitsgruppe ?? "",
                    PfasStatus = s.PFAS_Status ?? "",
                    Status = s.Status ?? "",
                    Quelle = s.Quelle ?? "",
                    Sortierung = s.Sortierung,
                    Gesperrt = s.ReadOnly,
                    Verwendung = KaeltemittelCtrl.Verwendung(s.Id),
                    GruppeText = Anzeige(Gruppen(), s.Gruppe),
                    PfasText = Anzeige(PfasWerte(), s.PFAS_Status),
                    StatusText = Anzeige(Statuswerte(), s.Status),
                });
            return zeilen;
        }

        // =================================================================== Wertemengen

        /// <summary>Die Stoffgruppen in Schemafolge mit Anzeigetext.</summary>
        public static IReadOnlyList<KaeltemittelWahl> Gruppen() => Menge(DbWerte.KAELTEMITTEL_GRUPPEN, "KMT_GRUPPE_");

        /// <summary>Die Sicherheitsgruppen (der Persistenzwert ist zugleich der Anzeigetext).</summary>
        public static IReadOnlyList<KaeltemittelWahl> Sicherheitsgruppen()
        {
            var l = new List<KaeltemittelWahl>();
            foreach (string w in DbWerte.KAELTEMITTEL_SICHERHEITSGRUPPEN) l.Add(new KaeltemittelWahl(w, w));
            return l;
        }

        /// <summary>Die PFAS-Status mit Anzeigetext.</summary>
        public static IReadOnlyList<KaeltemittelWahl> PfasWerte() => Menge(DbWerte.KAELTEMITTEL_PFAS_STATUS, "KMT_PFAS_");

        /// <summary>Die Status (Belegstand) mit Anzeigetext.</summary>
        public static IReadOnlyList<KaeltemittelWahl> Statuswerte() => Menge(DbWerte.KAELTEMITTEL_STATUS, "KMT_STATUS_");

        private static IReadOnlyList<KaeltemittelWahl> Menge(IReadOnlyList<string> werte, string praefix)
        {
            var l = new List<KaeltemittelWahl>();
            foreach (string w in werte) l.Add(new KaeltemittelWahl(w, Text(praefix + w, w)));
            return l;
        }

        private static string Anzeige(IReadOnlyList<KaeltemittelWahl> menge, string wert)
        {
            if (string.IsNullOrEmpty(wert)) return "";
            foreach (KaeltemittelWahl e in menge) if (e.Wert == wert) return e.Text;
            return wert;
        }

        // =================================================================== Übersetzung

        private static KaeltemittelCtrl.Kaeltemittelsatz Satz(KaeltemittelZeile z) => new KaeltemittelCtrl.Kaeltemittelsatz(
            z.Id, z.Code, Leer(z.Bezeichnung), Leer(z.Gruppe), z.Gwp, Leer(z.GwpQuelle), z.FGas, z.Natuerlich,
            Leer(z.Sicherheitsgruppe), Leer(z.PfasStatus), Leer(z.Status), Leer(z.Quelle), z.Sortierung, z.Gesperrt);

        private KaeltemittelAntwort Antwort(KaeltemittelCtrl.Ergebnis e)
        {
            if (e.Ok) Geaendert = true;
            return new KaeltemittelAntwort(e.Ok, e.Meldung ?? "", e.Id);
        }

        private static string Leer(string t) => string.IsNullOrWhiteSpace(t) ? null : t;

        private static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel, MyResource.Resource.Culture); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
