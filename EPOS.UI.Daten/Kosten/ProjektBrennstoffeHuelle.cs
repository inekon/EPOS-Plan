using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Kosten;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die HÜLLE des Dialogs „Brennstoffe des Projekts" (Anwenderentscheid 03.10.2026: Projektkopie
    /// des Brennstoffkatalogs) — plattformfrei: Sie ruft <see cref="ProjektBrennstoffe"/> im Kern für
    /// das offene Projekt (<c>Dienste.Projekt</c>) und übersetzt in die Anzeigeform der Komponente.
    /// Windows steuert nur das Fenster bei (<c>Views/Kosten/ProjektBrennstoffeFenster.cs</c>).
    ///
    /// <para><b>Jede Handlung schreibt sofort und je Satz</b> (Bearbeiten speichert die Werte EINES
    /// Brennstoffs, Zurücksetzen und Übernehmen wirken auf EINEN Satz) und liefert den neuen Stand
    /// zurück; der Dialog hat deshalb keinen Arbeitsstand über mehrere Sätze.</para>
    /// </summary>
    public sealed class ProjektBrennstoffeHuelle
    {
        /// <summary>Breite des Windows-Fensters — dreizehn Spalten.</summary>
        public const int FENSTER_BREITE = 1320;

        /// <summary>Höhe des Windows-Fensters.</summary>
        public const int FENSTER_HOEHE = 760;

        private readonly int _projekt;
        private readonly string _projektname;

        /// <summary>Die Hülle für das offene Projekt.</summary>
        public ProjektBrennstoffeHuelle() : this(Dienste.Projekt.Id, Dienste.Projekt.Name)
        {
        }

        /// <summary>Die Hülle für ein bestimmtes Projekt (Tests).</summary>
        public ProjektBrennstoffeHuelle(int idProjekt, string projektname)
        {
            _projekt = idProjekt;
            _projektname = projektname ?? "";
        }

        /// <summary>Hat der Dialog geschrieben?</summary>
        public bool Geaendert { get; private set; }

        /// <summary>Der Fenstertitel.</summary>
        public static string Titel() => MyResource.Resource.PBRS_TITEL;

        /// <summary>Der PARAMETERSATZ der Komponente — ohne <c>Geschlossen</c>.</summary>
        public IReadOnlyDictionary<string, object> Gaben() => new Dictionary<string, object>
        {
            ["Texte"] = Texte(),
            ["Stand"] = Stand(),
            ["Speichern"] = new Func<int, IReadOnlyDictionary<string, double?>, Task<ProjektBrennstoffeAntwort>>(
                (id, werte) => Task.FromResult(Speichern(id, werte))),
            ["Zuruecksetzen"] = new Func<int, Task<ProjektBrennstoffeAntwort>>(id => Task.FromResult(Zuruecksetzen(id))),
            ["Uebernehmen"] = new Func<int, Task<ProjektBrennstoffeAntwort>>(id => Task.FromResult(Uebernehmen(id))),
            ["HilfeSchluessel"] = "Form_ProjektBrennstoffe.btn_Help",
        };

        /// <summary>Der Stand des Projekts: Kopien und noch nicht übernommene Katalogsätze.</summary>
        public ProjektBrennstoffeStand Stand()
        {
            if (_projekt <= 0) return ProjektBrennstoffeStand.Leer;
            List<ProjektBrennstoffZeile> zeilen = ProjektBrennstoffe.Liste(_projekt)
                .Select(e => new ProjektBrennstoffZeile(e.IdBrennstoff, e.Bezeichner, e.Einheit,
                                                        new Dictionary<string, double?>(e.Werte),
                                                        new Dictionary<string, double?>(e.Katalogwerte),
                                                        e.KatalogVorhanden, e.Abweichungen.ToList()))
                .ToList();
            List<ProjektBrennstoffKatalogsatz> katalog = ProjektBrennstoffe.OhneKopie(_projekt)
                .Select(k => new ProjektBrennstoffKatalogsatz(k.IdBrennstoff, k.Bezeichner))
                .ToList();
            return new ProjektBrennstoffeStand(_projektname.Length > 0 ? _projektname : "#" +
                                               _projekt.ToString(CultureInfo.InvariantCulture), zeilen, katalog);
        }

        /// <summary>Speichert die Werte eines Brennstoffs.</summary>
        public ProjektBrennstoffeAntwort Speichern(int idBrennstoff, IReadOnlyDictionary<string, double?> werte)
        {
            if (_projekt <= 0) return new ProjektBrennstoffeAntwort(false, MyResource.Resource.PBRS_KEIN_PROJEKT, null);
            string grund = ProjektBrennstoffe.Speichern(_projekt, idBrennstoff, werte);
            if (grund != null) return new ProjektBrennstoffeAntwort(false, grund, null);
            Geaendert = true;
            return new ProjektBrennstoffeAntwort(true, Format(MyResource.Resource.PBRS_MSG_GESPEICHERT, Name(idBrennstoff)), Stand());
        }

        /// <summary>Setzt die Kopie eines Brennstoffs auf den heutigen Katalogstand zurück.</summary>
        public ProjektBrennstoffeAntwort Zuruecksetzen(int idBrennstoff)
        {
            if (_projekt <= 0) return new ProjektBrennstoffeAntwort(false, MyResource.Resource.PBRS_KEIN_PROJEKT, null);
            if (!ProjektBrennstoffe.Zuruecksetzen(_projekt, idBrennstoff))
                return new ProjektBrennstoffeAntwort(false, MyResource.Resource.PBRS_MSG_NICHT_ZURUECKGESETZT, null);
            Geaendert = true;
            return new ProjektBrennstoffeAntwort(true, Format(MyResource.Resource.PBRS_MSG_ZURUECKGESETZT, Name(idBrennstoff)), Stand());
        }

        /// <summary>Übernimmt einen Brennstoff des Katalogs in das Projekt.</summary>
        public ProjektBrennstoffeAntwort Uebernehmen(int idBrennstoff)
        {
            if (_projekt <= 0) return new ProjektBrennstoffeAntwort(false, MyResource.Resource.PBRS_KEIN_PROJEKT, null);
            if (!ProjektBrennstoffe.Uebernehmen(_projekt, idBrennstoff))
                return new ProjektBrennstoffeAntwort(false, MyResource.Resource.PBRS_MSG_KEINE_KOPIE, null);
            Geaendert = true;
            return new ProjektBrennstoffeAntwort(true, Format(MyResource.Resource.PBRS_MSG_UEBERNOMMEN, Name(idBrennstoff)), Stand());
        }

        private string Name(int idBrennstoff) =>
            ProjektBrennstoffe.Liste(_projekt).FirstOrDefault(e => e.IdBrennstoff == idBrennstoff)?.Bezeichner ?? "";

        private static string Format(string muster, string wert) =>
            string.Format(CultureInfo.CurrentCulture, muster, wert);

        /// <summary>Das Textbündel aus dem Ressourcenkatalog.</summary>
        public static ProjektBrennstoffeTexte Texte() => new ProjektBrennstoffeTexte
        {
            Titel = MyResource.Resource.PBRS_TITEL,
            Einleitung = MyResource.Resource.PBRS_EINLEITUNG,
            KeinProjekt = MyResource.Resource.PBRS_KEIN_PROJEKT,
            Leer = MyResource.Resource.PBRS_LEER,
            SpalteBrennstoff = MyResource.Resource.PBRS_SP_BRENNSTOFF,
            SpalteEinheit = MyResource.Resource.PBRS_SP_EINHEIT,
            Felder = new Dictionary<string, string>
            {
                [ProjektBrennstoffFelder.Hi] = MyResource.Resource.PBRS_SP_HI,
                [ProjektBrennstoffFelder.Hs] = MyResource.Resource.PBRS_SP_HS,
                [ProjektBrennstoffFelder.Co2] = MyResource.Resource.PBRS_SP_CO2,
                [ProjektBrennstoffFelder.So2] = MyResource.Resource.PBRS_SP_SO2,
                [ProjektBrennstoffFelder.Nox] = MyResource.Resource.PBRS_SP_NOX,
                [ProjektBrennstoffFelder.Staub] = MyResource.Resource.PBRS_SP_STAUB,
                [ProjektBrennstoffFelder.Pe] = MyResource.Resource.PBRS_SP_PE,
                [ProjektBrennstoffFelder.Grundpreis] = MyResource.Resource.PBRS_SP_GRUNDPREIS,
                [ProjektBrennstoffFelder.Arbeitspreis] = MyResource.Resource.PBRS_SP_ARBEITSPREIS,
                [ProjektBrennstoffFelder.Leistungspreis] = MyResource.Resource.PBRS_SP_LEISTUNGSPREIS,
            },
            SpalteStand = MyResource.Resource.PBRS_SP_STAND,
            SpalteAktionen = MyResource.Resource.PBRS_SP_AKTIONEN,
            WieKatalog = MyResource.Resource.PBRS_STAND_WIE_KATALOG,
            Abweichend = MyResource.Resource.PBRS_STAND_ABWEICHEND,
            OhneKatalog = MyResource.Resource.PBRS_STAND_OHNE_KATALOG,
            TipKatalogwert = MyResource.Resource.PBRS_TIP_KATALOGWERT,
            Bearbeiten = MyResource.Resource.PBRS_BEARBEITEN,
            BearbeitenTitel = MyResource.Resource.PBRS_BEARBEITEN_TITEL,
            Zuruecksetzen = MyResource.Resource.PBRS_ZURUECKSETZEN,
            FrageZuruecksetzen = MyResource.Resource.PBRS_FRAGE_ZURUECKSETZEN,
            Uebernehmen = MyResource.Resource.PBRS_UEBERNEHMEN,
            UebernehmenWahl = MyResource.Resource.PBRS_UEBERNEHMEN_WAHL,
            UebernehmenLeer = MyResource.Resource.PBRS_UEBERNEHMEN_LEER,
            Speichern = MyResource.Resource.ADM_BTN_SPEICHERN,
            Abbrechen = MyResource.Resource.ALLG_BTN_ABBRECHEN,
            Schliessen = MyResource.Resource.KABG_BTN_SCHLIESSEN,
            Ja = MyResource.Resource.ALLG_BTN_JA,
            Nein = MyResource.Resource.ALLG_BTN_NEIN,
        };
    }
}
