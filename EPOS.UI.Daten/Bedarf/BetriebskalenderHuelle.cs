using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die HÜLLE der Betriebskalender (Entscheidungsvorlage Modellgrenzen PW2, BW2) — plattformfrei:
    /// der Parametersatz der Verwaltung <see cref="BetriebskalenderDialog"/> aus
    /// <see cref="BetriebskalenderCtrl"/>, und die Wahl je Zuordnung für den Bedarfsprofil-Dialog
    /// (<see cref="WahlEinhaengen"/>).
    ///
    /// <para><b>Beide Schalen nehmen dieselbe Hülle:</b> Die Verwaltung ist eine freie Ansicht der
    /// <c>AppWurzel</c> (Seitenschlüssel <c>BETRIEBSKALENDER</c>); unter Windows reicht die
    /// Hauptfensterhülle <see cref="Gaben"/> als Delegat herein.</para>
    ///
    /// <para><b>Die Hülle rechnet und prüft nicht selbst</b> — sie übersetzt zwischen dem DTO der
    /// Oberfläche (<see cref="BetriebskalenderDaten"/>) und dem Modell des Kerns
    /// (<see cref="Betriebskalender"/>); die Regeln stehen in <see cref="Betriebskalender.Pruefen"/>.</para>
    /// </summary>
    internal static class BetriebskalenderHuelle
    {
        /// <summary>
        /// Der Parametersatz der Verwaltung für das geöffnete Projekt (<see cref="Dienste.Projekt"/>) — ohne
        /// <c>Geschlossen</c>, das setzt der Wirt.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben() => Gaben(AktivesProjekt());

        /// <summary>
        /// Der Parametersatz der Verwaltung; <paramref name="projektId"/> nennt das Projekt, dessen Gebäude
        /// „Aus Gebäude übernehmen…“ anbietet (Konzept Kältebedarfsprofile 7.2) — 0 = keines.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(int projektId)
        {
            return new Dictionary<string, object>
            {
                ["Gebaeude"] = new Func<IReadOnlyList<BetriebskalenderGebaeude>>(() => Gebaeude(projektId)),
                ["Laden"] = new Func<IReadOnlyList<BetriebskalenderDaten>>(
                    () => BetriebskalenderCtrl.Liste().Select(AlsDaten).ToList()),
                ["Laender"] = Laender(),
                ["Pruefen"] = new Func<BetriebskalenderDaten, string>(d => AlsModell(d).Pruefen()),
                ["Speichern"] = new Func<BetriebskalenderDaten, string>(Speichern),
                ["Loeschen"] = new Func<int, string>(
                    id => BetriebskalenderCtrl.Loeschen(id) ? null : MyResource.Resource.BKAL_MSG_LOESCHEN_FEHLER),
                ["Verwendungen"] = new Func<int, int>(BetriebskalenderCtrl.Verwendungen),
                ["Herleitung"] = new Func<BetriebskalenderDaten, string>(Herleitung)
            };
        }

        /// <summary>
        /// Hängt die Wahl je Zuordnung an den Parametersatz des Bedarfsprofil-Dialogs (Baustein
        /// <c>Betriebskalenderwahl</c>, Konzept Kältebedarfsprofile 7.1): die Kalender als (ID, Bezeichner) —
        /// ohne Kalender bleibt die Liste leer und der Baustein nennt den Weg —, dazu den Leseweg für das
        /// Neulesen nach dem Blatt „Betriebskalender…“ und den Parametersatz dieses Blattes für
        /// <paramref name="projektId"/> (0 = das geöffnete Projekt).
        /// </summary>
        internal static void WahlEinhaengen(IDictionary<string, object> gaben, int projektId = 0)
        {
            int projekt = projektId > 0 ? projektId : AktivesProjekt();
            gaben["Betriebskalender"] = Wahl();
            gaben["BetriebskalenderNeuLesen"] = new Func<IReadOnlyList<(int Id, string Text)>>(Wahl);
            gaben["BetriebskalenderGaben"] = new Func<IReadOnlyDictionary<string, object>>(() => Gaben(projekt));
        }

        /// <summary>Die Kalender als (ID, Bezeichner), nach Bezeichner.</summary>
        internal static IReadOnlyList<(int Id, string Text)> Wahl()
            => BetriebskalenderCtrl.Liste().Select(k => (k.ID, k.Bezeichner ?? "")).ToList();

        /// <summary>
        /// Die Gebäude des Projekts als Quellen für „Aus Gebäude übernehmen…“ (<see cref="BetriebskalenderGebaeudeCtrl"/>);
        /// leer ohne Projekt — dann sperrt der Dialog den Knopf und nennt den Grund.
        /// </summary>
        internal static IReadOnlyList<BetriebskalenderGebaeude> Gebaeude(int projektId)
            => BetriebskalenderGebaeudeCtrl.Quellen(projektId)
                .Select(q => new BetriebskalenderGebaeude(q.Bezeichner ?? "", q.Feiertagsland,
                                                          q.Ferien.Select(f => (f.Von, f.Bis)).ToList()))
                .ToList();

        /// <summary>Das geöffnete Projekt; 0 = keines.</summary>
        private static int AktivesProjekt()
        {
            IProjektKontext p = Dienste.Projekt;
            return p != null && p.Vorhanden ? p.Id : 0;
        }

        /// <summary>Schreibt einen Satz; die neue ID steht danach im DTO. <c>null</c> = gelungen.</summary>
        internal static string Speichern(BetriebskalenderDaten d)
        {
            if (d == null) return MyResource.Resource.BKAL_MSG_NAME_FEHLT;
            Betriebskalender k = AlsModell(d);
            string grund = k.Pruefen();
            if (grund != null) return grund;
            int id = BetriebskalenderCtrl.Speichern(k);
            if (id <= 0) return MyResource.Resource.BKAL_MSG_SPEICHERN_FEHLER;
            d.Id = id;
            return null;
        }

        /// <summary>
        /// Die Auskunftszeile: Feiertage und Ferientage im Gemeinjahr, im Rückfallraster des Katalogs
        /// (<see cref="Konditionierungdatenweg.Rueckfallraster"/>, E115) und ohne Jahr nach der Konvention
        /// <see cref="Gemeinjahrkalender"/>; im Lauf gilt das Raster des Projekts (<see cref="Konditionierungdatenweg.Raster(int)"/>).
        /// </summary>
        internal static string Herleitung(BetriebskalenderDaten d)
        {
            if (d == null) return "";
            Gemeinjahrkalender raster = Konditionierungdatenweg.Rueckfallraster;
            byte[] arten = AlsModell(d).Tagesarten(raster);
            int feiertage = arten.Count(a => a == Betriebskalender.TAG_FEIERTAG);
            int ferien = arten.Count(a => a == Betriebskalender.TAG_FERIEN);
            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.BKAL_HRL_JAHR,
                                 Kalendertage.Wochentagname(raster.W0), feiertage, ferien);
        }

        /// <summary>Die sechzehn Länder mit Namen in der Oberflächensprache.</summary>
        internal static IReadOnlyList<Bundeslandeintrag> Laender()
            => Landesfeiertage.BUNDESLAENDER.Select(k => new Bundeslandeintrag(k, Landesname(k))).ToList();

        /// <summary>Der Klartext einer Länderkennung.</summary>
        internal static string Landesname(string kennung) => kennung switch
        {
            "BW" => MyResource.Resource.BKAL_LAND_BW,
            "BY" => MyResource.Resource.BKAL_LAND_BY,
            "BE" => MyResource.Resource.BKAL_LAND_BE,
            "BB" => MyResource.Resource.BKAL_LAND_BB,
            "HB" => MyResource.Resource.BKAL_LAND_HB,
            "HH" => MyResource.Resource.BKAL_LAND_HH,
            "HE" => MyResource.Resource.BKAL_LAND_HE,
            "MV" => MyResource.Resource.BKAL_LAND_MV,
            "NI" => MyResource.Resource.BKAL_LAND_NI,
            "NW" => MyResource.Resource.BKAL_LAND_NW,
            "RP" => MyResource.Resource.BKAL_LAND_RP,
            "SL" => MyResource.Resource.BKAL_LAND_SL,
            "SN" => MyResource.Resource.BKAL_LAND_SN,
            "ST" => MyResource.Resource.BKAL_LAND_ST,
            "SH" => MyResource.Resource.BKAL_LAND_SH,
            "TH" => MyResource.Resource.BKAL_LAND_TH,
            _ => kennung ?? ""
        };

        /// <summary>Das Kernmodell als DTO der Oberfläche.</summary>
        internal static BetriebskalenderDaten AlsDaten(Betriebskalender k)
        {
            var d = new BetriebskalenderDaten
            {
                Id = k.ID,
                Bezeichner = k.Bezeichner ?? "",
                Bundesland = k.Bundesland,
                FerienfaktorProzent = Math.Round(k.Ferienfaktor * 100.0, 9),
                FeiertagWieSonntag = k.FeiertagWieSonntag,
                FerienKuerzen = k.FerienKuerzen
            };
            for (int i = 0; i < BetriebskalenderDaten.FERIEN && i < k.Ferien.Count; i++)
            {
                d.Von[i] = k.Ferien[i].Von;
                d.Bis[i] = k.Ferien[i].Bis;
            }
            return d;
        }

        /// <summary>Das DTO als Kernmodell — die Paare, die beide Grenzen tragen, in ihrer Reihenfolge.</summary>
        internal static Betriebskalender AlsModell(BetriebskalenderDaten d)
        {
            var k = new Betriebskalender
            {
                ID = d.Id,
                Bezeichner = (d.Bezeichner ?? "").Trim(),
                Bundesland = string.IsNullOrWhiteSpace(d.Bundesland) ? null : d.Bundesland.Trim(),
                Ferienfaktor = d.FerienfaktorProzent / 100.0,
                FeiertagWieSonntag = d.FeiertagWieSonntag,
                FerienKuerzen = d.FerienKuerzen
            };
            for (int i = 0; i < BetriebskalenderDaten.FERIEN; i++)
                if (d.Von[i].HasValue && d.Bis[i].HasValue)
                    k.Ferien.Add(new Ferienzeitraum(d.Von[i].Value, d.Bis[i].Value));
            return k;
        }
    }
}
