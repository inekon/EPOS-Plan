/****************************** Module Header ******************************\
* Module Name:  SimpleObject.cs
* Project:      CSExeCOMServer
* Copyright (c) Microsoft Corporation.
* 
* The definition of the COM class, SimpleObject, and its ClassFactory, 
* SimpleObjectClassFactory.
* 
* (Please generate new GUIDs when you are writing your own COM server) 
* Program ID: CSExeCOMServer.SimpleObject
* CLSID_SimpleObject: DB9935C1-19C5-4ed2-ADD2-9A57E19F53A3
* IID_ISimpleObject: 941D219B-7601-4375-B68A-61E23A4C8425
* DIID_ISimpleObjectEvents: 014C067E-660D-4d20-9952-CD973CE50436
* 
* Properties:
* // With both get and set accessor methods
* float FloatProperty
* 
* Methods:
* // HelloWorld returns a string "HelloWorld"
* string HelloWorld();
* // GetProcessThreadID outputs the running process ID and thread ID
* void GetProcessThreadID(out int processId, out int threadId);
* 
* Events:
* // FloatPropertyChanging is fired before new value is set to the 
* // FloatProperty property. The Cancel parameter allows the client to cancel 
* // the change of FloatProperty.
* void FloatPropertyChanging(float NewValue, ref bool Cancel);
* 
* This source is subject to the Microsoft Public License.
* See http://www.microsoft.com/opensource/licenses.mspx#Ms-PL.
* All other rights reserved.
* 
* THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
* EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED 
* WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
\***************************************************************************/

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;


namespace CSExeCOMServer
{
    
       
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)] // No ClassInterface
    [Guid("DB9935C1-19C5-4ed2-ADD2-9A57E19F53A3")]
    [ComSourceInterfaces(typeof(ISimpleObjectEvents))]
    public class SimpleObject : ISimpleObject
    {

        [DllImport("bhkwplan.dll")]
        public static extern int vector_init(Single[] ges_strombedarf);
        [DllImport("bhkwplan.dll")]
        public static extern int Watt_To_kW(Single[] ges_strombedarf);
        [DllImport("bhkwplan.dll")]
        public static extern long vectoren_addieren(Single[] verb_strombedarf, Single[] ges_strombedarfgle);
        [DllImport("bhkwplan.dll")]
        public static extern int monats_grenzen(int[] anfang, int[] endew);
        [DllImport("bhkwplan.dll")]
        public static extern int TaeglHeizlastWG(int Day, int WEAbsenkung, Single WETemp,
            int FerienAbsenkung, Single FerienTemp,
            Single RaumsolltempVorgabeTag, Single RaumsolltempVorgabeNacht,
            Single InnereGewinne, Single SolareGewinne,
            Single SpezWaermeverluste, Single GebaeudeKapazitaet,
            Single AussenTemp, Single MaxRaumtemp,
            Single Gesamtflaeche, Single Wohnflaeche);
        [DllImport("bhkwplan.dll")]
        public static extern int SolareGewinneC(Single En, Single An, Single Ew,
            Single Eo, Single Awo, Single Es, Single u_As, Single Transmissionsgrad);
        [DllImport("bhkwplan.dll")]
        public static extern int SpezWaermeverlusteC(Single Kw, Single Aw, Single Kf,
            Single Af, Single Kd, Single Ad, Single Kg, Single Ag, Single Ks,
            Single u_As, Single Kwb1, Single Lwb1, Single Kwb2, Single Lwb2, Single Kwb3,
            Single Lwb3, Single AussenTemp, Single Wohnflaeche, Single Raumhoehe, Single LWR);
        [DllImport("bhkwplan.dll")]
        public static extern int StdWerte(Single[] Waermebedarf, int[] TagTyp, Single[] Tagesgang, Single[] Tageslast);
        [DllImport("bhkwplan.dll")]
        public static extern int heapsort(Single[] Waermebedarf, Single[] Dauerlinie);
        [DllImport("bhkwplan.dll")]
        public static extern int normieren(Single[] Dauerlinie, Single MaxWert);
        [DllImport("bhkwplan.dll")]
        public static extern int vector_summe(Single[] Waermebedarf, ref Single summe_waermebedarf);
        [DllImport("bhkwplan.dll")]
        public static extern int netzverlustec(Single[] Waermebedarf, Single stundl_netzverluste);


        Dispatcher UserDispatcher = Dispatcher.CurrentDispatcher;
        public SimpleObject()
        {
            // Increment the lock count of objects in the COM server.
            ExecutableComServer.Instance.Lock();
            Console.WriteLine("SimpleObject");
        }

        ~SimpleObject()
        {
            // Decrement the lock count of objects in the COM server.
            ExecutableComServer.Instance.Unlock();
        }

        private float fField = 0f;

        public float FloatProperty
        {
            get { return this.fField; }
            set
            {
                bool cancel = false;
                // Raise the event FloatPropertyChanging
                if (null != FloatPropertyChanging)
                    FloatPropertyChanging(value, ref cancel);
                if (!cancel)
                    this.fField = value;
            }
        }

        public int I_SpezWaermeverlusteC(Single Kw, Single Aw, Single Kf,
            Single Af, Single Kd, Single Ad, Single Kg, Single Ag, Single Ks,
            Single u_As, Single Kwb1, Single Lwb1, Single Kwb2, Single Lwb2, Single Kwb3,
            Single Lwb3, Single AussenTemp, Single Wohnflaeche, Single Raumhoehe, Single LWR)
        {
            return SpezWaermeverlusteC(Kw, Aw, Kf, Af, Kd, Ad, Kg, Ag, Ks,
            u_As, Kwb1, Lwb1, Kwb2, Lwb2, Kwb3,
            Lwb3, AussenTemp, Wohnflaeche, Raumhoehe, LWR);
        }

        public int I_vector_init(ref Single[] ges_strombedarf)
        {
            return vector_init(ges_strombedarf);
        }

        public int I_SolareGewinneC(Single En, Single An, Single Ew,
            Single Eo, Single Awo, Single Es, Single u_As, Single Transmissionsgrad)
        {
            return SolareGewinneC(En,An,Ew,Eo,Awo,Es,u_As,Transmissionsgrad);
        }
        
        public Single I_TaeglHeizlastWG(int Day, int WEAbsenkung, Single WETemp,
            int FerienAbsenkung, Single FerienTemp,
            Single RaumsolltempVorgabeTag, Single RaumsolltempVorgabeNacht,
            Single InnereGewinne, Single SolareGewinne,
            Single SpezWaermeverluste, Single GebaeudeKapazitaet,
            Single AussenTemp, Single MaxRaumtemp,
            Single Gesamtflaeche, Single Wohnflaeche)
        {
            return TaeglHeizlastWG(Day, WEAbsenkung, WETemp,
             FerienAbsenkung, FerienTemp,
             RaumsolltempVorgabeTag, RaumsolltempVorgabeNacht,
             InnereGewinne, SolareGewinne,
             SpezWaermeverluste, GebaeudeKapazitaet,
             AussenTemp, MaxRaumtemp,
             Gesamtflaeche, Wohnflaeche);
        }

        public Single[] I_Watt_To_Kw(ref Single[] ges_strombedarf)
        {
            Console.WriteLine("I_Watt_To_Kw");
            Watt_To_kW(ges_strombedarf);
            return ges_strombedarf;
        }

        public Single[] I_StdWerte(ref Single[] Waermebedarf, int[] TagTyp, Single[] Tagesgang, Single[] Tageslast)
        {
            StdWerte(Waermebedarf, TagTyp, Tagesgang, Tageslast);
            return Waermebedarf;
        }

        public Single[] CSharp_I_Watt_To_Kw(Single[] ges_strombedarf)
        {
            Watt_To_kW(ges_strombedarf);
            return ges_strombedarf;
        }

        public int CSharp_I_monats_grenzen(int[] anfang, int[] endew)
        {
            return monats_grenzen(anfang, endew);
        }
        
        public long CSharp_I_vectoren_addieren(Single[] Quelle, Single[] Ziel)
        {
            return vectoren_addieren(Quelle, Ziel);
        }

        public Single[] I_heapsort(Single[] Waermebedarf, Single[] Dauerlinie)
        {
            heapsort(Waermebedarf, Dauerlinie);
            return Dauerlinie; 
        }

        public Single[] I_normieren(Single[] Dauerlinie, Single MaxWert)
        {
             normieren(Dauerlinie, MaxWert);
             return Dauerlinie;
        }

        public Single I_vector_summe(Single[] Waermebedarf)
        {
            Single summe_waermebedarf = 0;
            vector_summe(Waermebedarf, ref summe_waermebedarf);
            return summe_waermebedarf;
        }

        public Single[] I_netzverlustec(Single[] Waermebedarf, Single stundl_netzverluste)
        {
            netzverlustec(Waermebedarf, stundl_netzverluste);
            return Waermebedarf;
        }

        public int test()
        {
            return 55;
        }
        public float[] HelloWorld(float[] test)
        {
            float[] Quelle = new float[8760];
            float[] Ziel = new float[8760];
            Quelle[0] = 11 + test[0];
            Ziel[0] = 22;
            vectoren_addieren(Quelle, Ziel);
            return Ziel;
        }

        public void GetProcessThreadId(out int processId, out int threadId)
        {
            processId = NativeMethods.GetCurrentProcessId();
            threadId = NativeMethods.GetCurrentThreadId();
        }



        [ComVisible(false)]
        public delegate void FloatPropertyChangingEventHandler(float NewValue, ref bool Cancel);
        public event FloatPropertyChangingEventHandler FloatPropertyChanging;

        
        
        // These routines perform the additional COM registration needed by 
        // the service.

        [ComRegisterFunction]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Register(Type t)
        {
            try
            {
                HelperMethods.RegasmRegisterLocalServer(t);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); // Log the error
                throw ex; // Re-throw the exception
            }
        }

        [ComUnregisterFunction]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Unregister(Type t)
        {
            try
            {
                HelperMethods.RegasmUnregisterLocalServer(t);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); // Log the error
                throw ex; // Re-throw the exception
            }
        }
    }
}
