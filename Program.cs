using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FEL
{
    public static class Program
    {
       [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            web.Inicia(FEL.Properties.Settings.Default.Coneccion);
            string resultado;
            try
            {
                for (int i = 2; i <= 2; i++)
                {
                    resultado = web.registraDocumentoXML(1, "dpncfel4", i, 2);
                }                
            }
            catch
            { 
            }
        }
        //readonly static ClsConsultarDocumentos doc = new ClsConsultarDocumentos();
        //static void Main(string[] args)
        //{


        //        Application.EnableVisualStyles();
        //        Application.SetCompatibleTextRenderingDefault(false);
        //        web.Inicia(FEL.Properties.Settings.Default.Coneccion);
        //        string resultado;
        //        {
        //            DataTable dt = doc.ConsultarDocumentos();

        //            //resultado = web.registraDocumentoXML(Convert.ToInt32(args[0]), args[1], Convert.ToInt32(args[2]), Convert.ToInt32(args[3])); 
        //            foreach (DataRow fila in dt.Rows)
        //            {
        //                try
        //                {
        //                resultado = web.registraDocumentoXML(Convert.ToInt32(fila[0]), Convert.ToString(fila[1]), Convert.ToInt32(fila[2]), Convert.ToInt32(fila[3]));

        //                }
        //            catch(Exception ex)
        //            {
        //                MessageBox.Show(ex.Message);
        //            }

        //            }
        //        }
        //    }




    }
}
