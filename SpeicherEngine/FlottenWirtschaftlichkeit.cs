using System;
using System.Collections.Generic;
using System.Linq;

namespace SpeicherEngine;

/// <summary>Kapitalwert aus expliziten vollstaendigen Jahreskonten.</summary>
public static class FlottenWirtschaftlichkeit
{
    public static FlottenJahreskonto ErzeugeJahreskonto(
        int jahr,
        FlottenStudienErgebnis studie,
        IReadOnlyList<FlottenEinheit> einheiten,
        double? energieAusgleichEuroProKWh,
        bool istVollstaendigesJahr,
        string projektionskennzeichnung = "Tatsaechlich simuliertes Projektjahr")
    {
        if (studie is null || einheiten is null) throw new ArgumentNullException();
        var delta = studie.EndenergieAenderungKWhJeSpeicher.Count > 0
            ? studie.EndenergieAenderungKWhJeSpeicher.Sum()
            : studie.Variante.SpeicherKennzahlen.Sum(x => x.EndenergieKWh - x.AnfangsenergieKWh);
        if (Math.Abs(delta) > 1e-7 && energieAusgleichEuroProKWh is null)
            throw new ArgumentException("Veraenderte Endenergie muss explizit bewertet werden.");
        var opex = einheiten.Sum(x => x.JaehrlicheFixeOpexEuro
            + x.JaehrlicheOpexEuroProKWhKapazitaet * x.KapazitaetKWh
            + x.JaehrlicheOpexEuroProKw * Math.Max(x.LadeleistungKw, x.EntladeleistungKw));
        var throughput = studie.Variante.SpeicherKennzahlen.Zip(einheiten,
            (k, b) => k.EntladeenergieAcKWh * b.DurchsatzkostenEuroProKWhEntladung).Sum();
        var replacement = einheiten.Where(x => x.ErsatzintervallJahre > 0 &&
            jahr % x.ErsatzintervallJahre == 0).Sum(x => x.ErsatzkostenEuro);
        var energy = delta * (energieAusgleichEuroProKWh ?? 0);
        var cash = studie.Referenzrechnung.GesamtEuro - studie.Variantenrechnung.GesamtEuro
            - opex - throughput - replacement + energy;
        return new FlottenJahreskonto
        {
            Jahr = jahr,
            Referenzrechnung = Kopiere(studie.Referenzrechnung),
            Variantenrechnung = Kopiere(studie.Variantenrechnung),
            OpexEuro = opex,
            DurchsatzkostenEuro = throughput,
            ErsatzkostenEuro = replacement,
            EndenergieAusgleichEuro = energy,
            NettoCashflowEuro = cash,
            IstVollstaendigesJahr = istVollstaendigesJahr,
            Projektionskennzeichnung = projektionskennzeichnung
        };
    }

    public static FlottenWirtschaftlichkeitErgebnis Bewerte(FlottenWirtschaftlichkeitEingang input)
    {
        if (input is null) throw new ArgumentNullException(nameof(input));
        var x = Kopiere(input);
        if (!double.IsFinite(x.Kalkulationszins) || x.Kalkulationszins <= -1)
            throw new ArgumentOutOfRangeException(nameof(input), "Kalkulationszins muss groesser als -100 Prozent sein.");
        if (x.Jahreskonten.Count == 0) throw new ArgumentException("Explizite Jahreskonten fehlen.");
        if (x.Jahreskonten.Any(y => !y.IstVollstaendigesJahr))
            throw new ArgumentException("Teiljahre duerfen nicht als Jahrescashflow diskontiert werden.");

        List<FlottenJahreskonto> accounts;
        var repeated = false;
        if (x.ReferenzjahrExplizitWiederholen)
        {
            if (x.Jahreskonten.Count != 1 || x.ProjektjahreBeiWiederholung <= 0)
                throw new ArgumentException("Eine Referenzjahrprojektion benoetigt genau ein Konto und eine positive Projektlaufzeit.");
            repeated = true;
            accounts = new List<FlottenJahreskonto>(x.ProjektjahreBeiWiederholung);
            for (var year = 1; year <= x.ProjektjahreBeiWiederholung; year++)
            {
                var a = Kopiere(x.Jahreskonten[0]);
                a.Jahr = year;
                a.Projektionskennzeichnung = "Explizit wiederholte Referenzjahr-Projektion";
                a.ErsatzkostenEuro = x.Einheiten.Where(b => b.ErsatzintervallJahre > 0 &&
                    year % b.ErsatzintervallJahre == 0).Sum(b => b.ErsatzkostenEuro);
                a.NettoCashflowEuro = a.Referenzrechnung.GesamtEuro - a.Variantenrechnung.GesamtEuro
                    - a.OpexEuro - a.DurchsatzkostenEuro - a.ErsatzkostenEuro + a.EndenergieAusgleichEuro;
                accounts.Add(a);
            }
        }
        else
        {
            accounts = x.Jahreskonten.OrderBy(y => y.Jahr).Select(Kopiere).ToList();
            for (var i = 0; i < accounts.Count; i++)
                if (accounts[i].Jahr != i + 1)
                    throw new ArgumentException("Jahreskonten muessen lueckenlos bei Jahr 1 beginnen.");
        }

        foreach (var a in accounts)
            a.NettoCashflowEuro = a.Referenzrechnung.GesamtEuro - a.Variantenrechnung.GesamtEuro
                - a.OpexEuro - a.DurchsatzkostenEuro - a.ErsatzkostenEuro + a.EndenergieAusgleichEuro;

        var capex = x.Einheiten.Sum(Investition);
        var residual = x.RestwertEuro + x.Einheiten.Sum(b => b.RestwertEuro);
        var npv = -capex;
        var accumulated = -capex;
        int? payback = null;
        var flows = new List<double>(accounts.Count);
        for (var year = 1; year <= accounts.Count; year++)
        {
            var flow = accounts[year - 1].NettoCashflowEuro;
            if (!double.IsFinite(flow)) throw new ArgumentException("Jahrescashflow ist nicht endlich.");
            flows.Add(flow);
            var discounted = flow / Math.Pow(1 + x.Kalkulationszins, year);
            npv += discounted;
            accumulated += discounted;
            if (payback is null && accumulated >= 0) payback = year;
        }
        npv += residual / Math.Pow(1 + x.Kalkulationszins, accounts.Count);

        return new FlottenWirtschaftlichkeitErgebnis
        {
            InvestitionEuro = capex,
            KapitalwertEuro = npv,
            DiskontierteAmortisationJahr = payback,
            JahresCashflowsEuro = flows,
            Jahreskonten = accounts,
            IstWiederholteReferenzjahrProjektion = repeated
        };
    }

    internal static double Investition(FlottenEinheit b) => b.InvestitionEuro
        + b.InvestitionEuroProKWh * b.KapazitaetKWh
        + b.InvestitionEuroProKw * Math.Max(b.LadeleistungKw, b.EntladeleistungKw);

    internal static FlottenWirtschaftlichkeitEingang Kopiere(FlottenWirtschaftlichkeitEingang x) => new()
    {
        Einheiten = x.Einheiten.Select(FlottenKopie.Einheit).ToList(),
        Jahreskonten = x.Jahreskonten.Select(Kopiere).ToList(),
        Kalkulationszins = x.Kalkulationszins,
        RestwertEuro = x.RestwertEuro,
        ReferenzjahrExplizitWiederholen = x.ReferenzjahrExplizitWiederholen,
        ProjektjahreBeiWiederholung = x.ProjektjahreBeiWiederholung
    };

    private static FlottenJahreskonto Kopiere(FlottenJahreskonto x) => new()
    {
        Jahr = x.Jahr, Referenzrechnung = Kopiere(x.Referenzrechnung), Variantenrechnung = Kopiere(x.Variantenrechnung),
        OpexEuro = x.OpexEuro, DurchsatzkostenEuro = x.DurchsatzkostenEuro,
        ErsatzkostenEuro = x.ErsatzkostenEuro, EndenergieAusgleichEuro = x.EndenergieAusgleichEuro,
        NettoCashflowEuro = x.NettoCashflowEuro, IstVollstaendigesJahr = x.IstVollstaendigesJahr,
        Projektionskennzeichnung = x.Projektionskennzeichnung
    };

    private static FlottenRechnung Kopiere(FlottenRechnung x) => new()
    {
        EnergiekostenEuro = x.EnergiekostenEuro, LeistungskostenEuro = x.LeistungskostenEuro,
        FixkostenEuro = x.FixkostenEuro, PeakKw = x.PeakKw, GesamtEuro = x.GesamtEuro
    };
}
