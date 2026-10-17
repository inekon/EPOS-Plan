using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using EPOS.UI.Dialoge.Admin;
using EPOS.UI.Dialoge.Lizenz;
using EPOS.UI.Dialoge.Projekt;
using EPOS.UI.Dialoge.Simulation;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten.Simulation;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace Rasterprobe.Wirt.Seiten;

/// <summary>
/// Die Beispieldaten der übrigen Wiki-Bilder (Einstellungen, Lizenz, Projektverwaltung, Varianten,
/// Projekttransfer, Wärmequelle Erdreich, Simulation): neutrale Namen, runde Werte, keine Datenbank,
/// keine echten Personen, Firmen oder Lizenzen. Jeder Dialog steht über <c>DynamicComponent</c> mit
/// seinem Parametersatz auf der Seite — die Fälle hier ergänzen den Schalter in Wikibild.razor.
/// </summary>
public partial class Wikibild
{
    /// <summary>Komponententyp und Parametersatz eines der übrigen Bilder, sonst <c>null</c>.</summary>
    private (Type Typ, IDictionary<string, object> Gaben)? Uebrig => _bild switch
    {
        "einstellungen" => (typeof(EinstellungenDialog), EinstellungenGaben()),
        "lizenz" => (typeof(LizenzDialog), LizenzGabenBild()),
        "projektverwaltung" => (typeof(ProjektWahlDialog), new Dictionary<string, object>
        {
            ["Zeilen"] = Beispielprojekte,
            ["Vorauswahl"] = "Beispielprojekt Wohnquartier",
            ["SortSpalte"] = ProjektListe.SPALTE_GEAENDERT,
            ["SortAbsteigend"] = true
        }),
        "varianten" => (typeof(ProjektVarianteDialog), new Dictionary<string, object>
        {
            ["StammName"] = "Beispielprojekt Wohnquartier",
            ["Zeilen"] = Beispielprojekte,
            ["Zielname"] = new Func<string, string>(q => "Beispielprojekt Wohnquartier - Variante Wärmepumpe")
        }),
        "projekttransfer" => (typeof(ProjektTransferDialog), new Dictionary<string, object>
        {
            ["Daten"] = TransferDaten()
        }),
        "erdreich" => (typeof(QuelleErdreichDialog), ErdreichGaben()),
        // Die Pufferspeicher-Auslegung (Beispieldaten in Wikibild.Pufferauslegung.cs).
        "pufferauslegung" => (typeof(EPOS.UI.Seiten.Pufferspeicher.PufferAuslegungSeite), _pufferauslegung ??= PufferauslegungGaben()),
        "simulation" => (typeof(KomponentenKonfigurationDialog), new Dictionary<string, object>
        {
            ["Komponente"] = Komponentenart.Waermepumpe,
            ["Werte"] = new ParameterDaten(),
            ["Anlage"] = WpMitTraeger(),
            ["Kuehlung"] = new WaermepumpeKuehlGaben
            {
                Vorlaeufe = _ => new[] { new KuehlVorlaufEintrag(7), new KuehlVorlaufEintrag(12), new KuehlVorlaufEintrag(18) },
                Stromtraeger = new[] { (1, "Strom Standard") },
                ProjektStromtraeger = 1
            },
            ["Traegerkatalog"] = new[]
            {
                new EnergietraegerWahl.Eintrag(1, "Strom", "Strom Standard"),
                new EnergietraegerWahl.Eintrag(2, "Strom", "Strom Wärmepumpentarif")
            }
        }),
        _ => null
    };

    // ---------------------------------------------------------------- Projekte
    private static readonly IReadOnlyList<ProjektKopfZeile> Beispielprojekte = new[]
    {
        new ProjektKopfZeile(1, "Beispielprojekt Wohnquartier", "Wohnungsbau Beispiel", "Quartier mit 40 Wohnungen",
                             new DateTime(2026, 10, 12, 9, 30, 0)),
        new ProjektKopfZeile(2, "Beispielprojekt Wohnquartier - Variante BHKW", "Wohnungsbau Beispiel", "Variante mit BHKW",
                             new DateTime(2026, 10, 14, 15, 0, 0), StammId: 1, Bezeichner: "Variante BHKW",
                             StammName: "Beispielprojekt Wohnquartier"),
        new ProjektKopfZeile(3, "Beispielprojekt Bürogebäude", "Verwaltung Beispiel", "Bürogebäude mit Kühlung",
                             new DateTime(2026, 9, 28, 11, 0, 0)),
        new ProjektKopfZeile(4, "Beispielprojekt Hotel", "Hotel Beispiel", "Hotel mit 80 Zimmern",
                             new DateTime(2026, 9, 3, 8, 15, 0)),
        new ProjektKopfZeile(5, "Beispielprojekt Gewerbehalle", "Gewerbe Beispiel", "Halle mit Prozesswärme",
                             new DateTime(2026, 8, 20, 16, 45, 0)),
        new ProjektKopfZeile(6, "Beispielprojekt Schule", "Schulträger Beispiel", "Schule mit Turnhalle",
                             new DateTime(2026, 7, 7, 10, 0, 0)),
    };

    private static ProjektTransferDaten TransferDaten() => new(
        Projekte: Beispielprojekte,
        Varianten: name => name == "Beispielprojekt Wohnquartier"
            ? new[] { "Variante BHKW" } : Array.Empty<string>(),
        Exportieren: (_, _, _, _) => false,
        PaketLesen: null,
        PaketSchreiben: null,
        Vorschau: _ => new PaketVorschau("", "", 0, Array.Empty<string>(), ""),
        Importieren: (_, _, _, _) => new ImportErgebnis(0, "", Array.Empty<string>(), ""),
        SicherungAnlegen: null,
        BerichtSchreiben: null);

    // ---------------------------------------------------------------- Einstellungen
    private IDictionary<string, object> EinstellungenGaben() => new Dictionary<string, object>
    {
        ["Satz"] = new Einstellungensatz
        {
            VdiPfad = @"C:\ProgramData\EPOS_PLAN\VDI-3805",
            DbExportPfad = @"C:\ProgramData\EPOS_PLAN\DB-Backup",
            DbImportPfad = @"C:\ProgramData\EPOS_PLAN\DB-Import",
            DbPfad = @"C:\ProgramData\EPOS_PLAN",
            DbName = "Kenndaten.sqlite",
            WikiUrl = "https://wiki.epos-plan.de",
            PvgisUrl = "https://re.jrc.ec.europa.eu/api/v5_2/",
            GeokodierungUrl = "https://nominatim.openstreetmap.org/",
            TryPortalUrl = "https://www.dwd.de/",
            TryRegionalUrl = "https://www.dwd.de/",
            AllgemeinPfad = @"C:\ProgramData\EPOS_PLAN"
        },
        ["Farbrollen"] = WindowsFormsApplication1.Zeichnung.Diagrammfarben.Gaben(),
        ["Firma"] = "Musterbüro Planung",
        ["Vorlagenordner"] = @"C:\ProgramData\EPOS_PLAN\Vorlagen",
        // Mit Ordnerwähler und Rückweg der Firma stehen die Pfade frei und die Rubrik „Bericht“ sichtbar.
        ["OrdnerWaehler"] = new Func<string, Task<string?>>(_ => Task.FromResult<string?>(null)),
        ["FirmaChanged"] = EventCallback.Factory.Create<string>(this, (string _) => { })
    };

    private static WaermepumpeAnlageDaten WpMitTraeger()
    {
        var a = Wikibeispiele.WaermepumpeAnlage();
        a.CarrierId = 2;
        return a;
    }

    // ---------------------------------------------------------------- Lizenz
    private static IDictionary<string, object> LizenzGabenBild()
    {
        string status = string.Format(Resource.LIZ_ST_GUELTIG, "Einzelplatz", "31.12.2027");
        var lage = new LizenzGaben("GUELTIG", status,
            string.Format(Resource.LIZ_DETAIL, "XXXX-XXXX-XXXX", "Musterbüro", "anwender@example.org", "Arbeitsplatz 1"),
            true, "");
        return new Dictionary<string, object>
        {
            ["Lizenzstatus"] = status,
            ["StartReiter"] = "STATUS",
            ["VerwaltungGaben"] = new Dictionary<string, object>
            {
                ["Lage"] = lage,
                ["EmailVorgabe"] = "anwender@example.org",
                ["Auffrischen"] = new Func<Task<LizenzGaben>>(() => Task.FromResult(lage)),
                ["Texte"] = new LizenzTexte()
            },
            ["Text"] = new LizenzTextGaben("Lizenzvereinbarung (Beispieltext).", "Beispiel", "01.01.2026")
        };
    }

    // ---------------------------------------------------------------- Wärmequelle Erdreich
    private static IDictionary<string, object> ErdreichGaben()
    {
        var aussen = new double[8760];
        for (int i = 0; i < aussen.Length; i++)
            aussen[i] = 9.0 - 10.0 * Math.Cos(2 * Math.PI * i / 8760.0 - 0.35) + 4.0 * Math.Sin(2 * Math.PI * ((i % 24) - 6) / 24.0);
        var daten = new QuelleErdreichDaten
        {
            WPName = "Wärmepumpe A",
            Quellsystem = ErdreichTemperatur.QUELLSYSTEM_SONDE,
            Tiefe = 100,
            Anzahl = 3,
            Bodentyp = ErdreichTemperatur.BODENTYP_DEFAULT,
            Klimazone = 6,
            Spreizung = 3,
            Aussentemperatur = aussen,
            Auslegung = new[] { new WpAuslegung("Wärmepumpe A", 10, 4.5, "B0/W35") }
        };
        var gaben = new Dictionary<string, object>(QuelleErdreichHuelle.Gaben(daten))
        {
            ["Lauf"] = ErdreichAuswertung.ErdreichLaufErgebnis.Keines
        };
        return gaben;
    }
}
