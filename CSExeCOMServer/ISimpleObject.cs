using System;
using System.Runtime.InteropServices;

namespace CSExeCOMServer
{
    [ComVisible(true)]
    [Guid("941D219B-7601-4375-B68A-61E23A4C8425")]
    public interface ISimpleObject
    {
        float FloatProperty { get; set; }

        float[] HelloWorld(float[] test);
        int test();
        Single[] I_Watt_To_Kw(ref Single[] ges_strombedarf);
        Single[] CSharp_I_Watt_To_Kw(Single[] ges_strombedarf);
        long CSharp_I_vectoren_addieren(Single[] Quelle, Single[] Ziel);
        int CSharp_I_monats_grenzen(int[] anfang, int[] endew);
        void GetProcessThreadId(out int processId, out int threadId);
        
        Single I_TaeglHeizlastWG(int Day, int WEAbsenkung, Single WETemp,
            int FerienAbsenkung, Single FerienTemp,
            Single RaumsolltempVorgabeTag, Single RaumsolltempVorgabeNacht,
            Single InnereGewinne, Single SolareGewinne,
            Single SpezWaermeverluste, Single GebaeudeKapazitaet,
            Single AussenTemp, Single MaxRaumtemp,
            Single Gesamtflaeche, Single Wohnflaeche);
        int I_SolareGewinneC(Single En, Single An, Single Ew,
            Single Eo, Single Awo, Single Es, Single u_As, Single Transmissionsgrad);
    //    int test([In, MarshalAs(UnmanagedType.AsAny)] int on);
    //    String test2();

        int I_vector_init(ref Single[] ges_strombedarf);

        int I_SpezWaermeverlusteC(Single Kw, Single Aw, Single Kf,
            Single Af, Single Kd, Single Ad, Single Kg, Single Ag, Single Ks,
            Single u_As, Single Kwb1, Single Lwb1, Single Kwb2, Single Lwb2, Single Kwb3,
            Single Lwb3, Single AussenTemp, Single Wohnflaeche, Single Raumhoehe, Single LWR);

        Single[] I_StdWerte(ref Single[] Waermebedarf, int[] TagTyp, Single[] Tagesgang, Single[] Tageslast);

        Single[] I_heapsort(Single[] Waermebedarf, Single[] Dauerlinie);

        Single[] I_normieren(Single[] Dauerlinie, Single MaxWert);
        
        Single I_vector_summe(Single[] Waermebedarf);

        Single[] I_netzverlustec(Single[] Waermebedarf, Single stundl_netzverluste);

        int I_monats_summe(Single[] value, Single[] Sum, int[] mo_anfang, int[] mo_ende);

        Single[] I_strom_wochetojahr(Single[] wo_strombedarf, Single[] monatsverbrauch, int[] mo_anfang, int[] mo_ende);
    }
}
