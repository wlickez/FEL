using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Authenticators;
using Newtonsoft.Json;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using System.Data.SqlClient;
using FirmaXadesNet;
using FirmaXadesNet.Crypto;
using FirmaXadesNet.Signature;
using FirmaXadesNet.Signature.Parameters;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;



namespace FEL
{
    public class web
    {
        static SqlConnection coneccion;
        static RestClient client;
        static int operacion = 0;
        
        public static void Inicia(string sconeccion)
        {
            coneccion = new SqlConnection(sconeccion);
            coneccion.Open();
        }
        

        public static string registraDocumentoXML(int empresa, string serie, int numero, int tipo)
        {
            client = new RestClient();
            client.BaseUrl = new Uri("https://certificador.feel.com.gt/fel/certificacion/dte/ ");
            RegistraDocumentoXMLRequest objetoe = new RegistraDocumentoXMLRequest();
            string resultado = "";
            string sxml = "";
            if (tipo == 4)
                sxml = CreaXML(empresa, serie, numero);
            if (tipo == 2)
                sxml = CreaXMLNC(empresa, serie, numero);
            if (tipo == 17)
                sxml = CreaXMLFE(empresa, serie, numero);
            sxml = sxml.Replace('|', '"');
            sxml = sxml.Replace('&', 'y');
            XmlDocument documento = new XmlDocument();
            documento.LoadXml(sxml);
            documento.Save(serie + Convert.ToString(numero) + ".xml");
            operacion = 1;
            documento.PreserveWhitespace = true;
            documento = FirmarDocumento(FEL.Properties.Settings.Default.Archivo, "Farmacia4$", serie + Convert.ToString(numero) + ".xml");
            objetoe.xml_dte = documento.InnerXml;
            XmlDocument documentof = new XmlDocument();
            documentof.PreserveWhitespace = true;
            documentof.LoadXml(documento.InnerXml);
            documentof.Save("F" + serie + Convert.ToString(numero) + ".xml");
            var encoding = new UnicodeEncoding();
            var texto = System.Text.Encoding.UTF8.GetBytes(documentof.InnerXml);
            var s = System.Convert.ToBase64String(texto);
            string archivo;
            archivo = "{\n";
            archivo = archivo + "   |nit_emisor|: |41256506|,\n";
            archivo = archivo + "   |correo_copia|: |wlickez@emfama.com|,\n";
            archivo = archivo + "   |xml_dte|: |" + s + "|\n";
            archivo = archivo + "}\n";
            archivo = archivo.Replace('|', '"');
            RestRequest request = new RestRequest(Method.POST);
            request.AddHeader("usuario", "ENFAMA");
            request.AddHeader("llave", "609D6930209ADBC27E6AF87E6C69989A");
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("identificador", serie + Convert.ToString(numero));
            request.AddParameter("application/json; charset=utf-8", archivo, ParameterType.RequestBody);
            request.RequestFormat = DataFormat.Json;
            IRestResponse response;
            response = client.Execute(request);
            dteResponse Data;
            Data = JsonConvert.DeserializeObject<dteResponse>(response.Content);
            if (Data.uuid != "")
            {
                SqlCommand comando;
                switch (tipo)
                {
                    case 2:
                        comando = new SqlCommand("UPDATE [CREDITO MAESTRO] SET [IDSat] = '" + Data.uuid + "' WHERE Empresa = " + Convert.ToString(empresa) + " AND Serie = '" + serie + "' AND Tipo = " + Convert.ToString(tipo) + " AND Numero = " + Convert.ToString(numero), coneccion);
                        break;
                    case 4:
                        comando = new SqlCommand("UPDATE [FACTURA MAESTRO] SET [IDSat] = '" + Data.uuid + "' WHERE Empresa = " + Convert.ToString(empresa) + " AND Serie = '" + serie + "' AND Tipo = " + Convert.ToString(tipo) + " AND Numero = " + Convert.ToString(numero), coneccion);
                        break;
                    default:
                        comando = new SqlCommand("UPDATE [CXP MAESTRO] SET [IDSat] = '" + Data.uuid + "' WHERE Empresa = " + Convert.ToString(empresa) + " AND Serie = '" + serie + "' AND Tipo = " + Convert.ToString(tipo) + " AND Numero = " + Convert.ToString(numero), coneccion);
                        break;
                }
                comando.ExecuteNonQuery();
                return Data.uuid;
            }
            else
            { MessageBox.Show(response.Content); }
            return resultado;
        }
        public static string CreaXML(int empresa, string serie, int numero)
        {
            SqlCommand comando = new SqlCommand("SELECT F.Moneda, F.Fecha, F.Credito, E.Correo, E.Nit, S.Comercial AS Nombre, S.Direccion, E.[Codigo Postal], E.Municipio, E.Departamento, F.Total, F.Nombre, UPPER(F.Nit) AS Nit, F.Direccion, F.Total + ISNULL(F.[Valor Descuento],0) AS STotal, ISNULL([Valor Descuento],0) AS Descuento, S.ID, ISNULL(F.Correo,'') AS Correo, E.Nombre AS NEmpresa FROM [FACTURA MAESTRO] AS F INNER JOIN EMPRESA AS E ON E.Codigo = F.Empresa INNER JOIN SERIE AS B ON B.Serie = F.Serie AND B.Empresa = F.Empresa AND B.Tipo = F.Tipo INNER JOIN SUCURSAL AS S ON B.Empresa = S.Empresa AND B.Sucursal = S.Codigo WHERE F.Empresa = " + Convert.ToString(empresa) + " AND F.Serie = '" + serie + "' AND F.Numero = " + Convert.ToString(numero), coneccion);
            SqlDataReader datos = null;
            String xml = "";
            GTDocumento gtdocumento = new GTDocumento();
            datos = comando.ExecuteReader();
            if (datos.HasRows)
            {
                datos.Read();
                xml = "<?xml version=|1.0| encoding=|utf-8| standalone=|no|?>\n";
                xml = xml + "<dte:GTDocumento xmlns:dte=|http://www.sat.gob.gt/dte/fel/0.1.0| Version=|0.4|\n";
                xml = xml + "xmlns:cfc=|http://www.sat.gob.gt/dte/fel/CompCambiaria/0.1.0|\n";
                xml = xml + "xmlns:cno=|http://www.sat.gob.gt/face2/ComplementoReferenciaNota/0.1.0|\n";
                xml = xml + "xmlns:ds=|http://www.w3.org/2000/09/xmldsig#|\n";
                xml = xml + "xmlns:cex=|http://www.sat.gob.gt/face2/ComplementoExportaciones/0.1.0|\n";
                xml = xml + "xmlns:cfe=|http://www.sat.gob.gt/face2/ComplementoFacturaEspecial/0.1.0|>\n";
                xml = xml + "         <dte:SAT ClaseDocumento=|dte|>\n";
                xml = xml + "            <dte:DTE ID=|DatosCertificados|>\n";
                xml = xml + "                <dte:DatosEmision ID=|DatosEmision|>\n";
                if (datos.GetInt32(0) == Convert.ToInt32(1))
                {
                    if (datos.GetBoolean(2) == true)
                    { xml = xml + "                    <dte:DatosGenerales CodigoMoneda=|GTQ| FechaHoraEmision=|" + datos.GetDateTime(1).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + "-06:00" + "| Tipo=|FCAM| />\n"; }
                    else
                    { xml = xml + "                    <dte:DatosGenerales CodigoMoneda=|GTQ| FechaHoraEmision=|" + datos.GetDateTime(1).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + "-06:00" + "| Tipo=|FACT| />\n"; }
                }
                else
                { 
                    if (datos.GetBoolean(2) == true)
                    { xml = xml + "                    <dte:DatosGenerales CodigoMoneda=|USD| FechaHoraEmision=|" + datos.GetDateTime(1).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + "-06:00" + "| Tipo=|FCAM| />\n"; }
                    else
                    { xml = xml + "                    <dte:DatosGenerales CodigoMoneda=|USD| FechaHoraEmision=|" + datos.GetDateTime(1).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + "-06:00" + "| Tipo=|FACT| />\n"; }
                }
                xml = xml + "                    <dte:Emisor AfiliacionIVA=|GEN| CodigoEstablecimiento=|" + datos.GetString(16) + "| CorreoEmisor=|" + datos.GetString(3) + "| NITEmisor=|" + datos.GetString(4).Replace("-", "") + "| NombreComercial=|" + datos.GetString(5) + "| NombreEmisor=|" + datos.GetString(18) + "|>\n";
                xml = xml + "                        <dte:DireccionEmisor>\n";
                xml = xml + "                            <dte:Direccion>" + datos.GetString(6) + "</dte:Direccion>\n";
                xml = xml + "                            <dte:CodigoPostal>" + datos.GetString(7) + "</dte:CodigoPostal>\n";
                xml = xml + "                            <dte:Municipio>" + datos.GetString(8) + "</dte:Municipio>\n";
                xml = xml + "                            <dte:Departamento>" + datos.GetString(9) + "</dte:Departamento>\n";
                xml = xml + "                            <dte:Pais>GT</dte:Pais>\n";
                xml = xml + "                        </dte:DireccionEmisor >\n";
                xml = xml + "                    </dte:Emisor>\n";
                xml = xml + "                    <dte:Receptor CorreoReceptor=|" + datos.GetString(17) + "| IDReceptor=|" + datos.GetString(12).Replace("-", "") + "| NombreReceptor=|" + datos.GetString(11) + "|>\n";
                xml = xml + "                        <dte:DireccionReceptor>\n";
                xml = xml + "                            <dte:Direccion>" + datos.GetString(13) + "</dte:Direccion>\n";
                xml = xml + "                            <dte:CodigoPostal>01001</dte:CodigoPostal>\n";
                xml = xml + "                            <dte:Municipio>Guatemala</dte:Municipio>\n";
                xml = xml + "                            <dte:Departamento>Guatemala</dte:Departamento>\n";
                xml = xml + "                            <dte:Pais>GT</dte:Pais>\n";
                xml = xml + "                        </dte:DireccionReceptor>\n";
                xml = xml + "                    </dte:Receptor>\n";
                xml = xml + "                    <dte:Frases>\n";
                xml = xml + "                        <dte:Frase CodigoEscenario=|1| TipoFrase=|1| />\n";
                xml = xml + "                        <dte:Frase CodigoEscenario=|1| TipoFrase=|2| />\n";
                xml = xml + "                        <dte:Frase CodigoEscenario=|9| TipoFrase=|4| />\n";
                xml = xml + "                    </dte:Frases>\n";
                xml = xml + "                    <dte:Items>\n";
                SqlConnection condetalle;
                condetalle = new SqlConnection(coneccion.ConnectionString);
                condetalle.Open();
                SqlCommand comdetalle = new SqlCommand("SELECT F.Descripcion, F.Cantidad, F.Precio + F.Descuento AS Precio, F.Descuento * F.Cantidad AS Descuento,  (F.Precio + F.Descuento) * F.Cantidad AS Total, P.Exento, F.Precio * F.Cantidad AS PT  FROM [FACTURA DETALLE] AS F INNER JOIN PRODUCTO AS P ON P.Codigo = F.Producto WHERE F.Empresa = " + Convert.ToString(empresa) + " AND F.Serie = '" + serie + "' AND F.Numero = " + Convert.ToString(numero), condetalle);
                SqlDataReader detalle;
                int contador = 1;
                decimal mimpuesto = 0;
                decimal mexento = 0;
                detalle = comdetalle.ExecuteReader();
                if (detalle.HasRows)
                {
                    while (detalle.Read())
                    {
                        xml = xml + "                        <dte:Item BienOServicio=|B| NumeroLinea=|" + Convert.ToString(contador) + "|>\n";
                        xml = xml + "                            <dte:Cantidad>" + detalle.GetDecimal(1).ToString("#0.00") + "</dte:Cantidad>\n";
                        xml = xml + "                            <dte:UnidadMedida>ST</dte:UnidadMedida>\n";
                        if (detalle.GetBoolean(5) == false)
                            xml = xml + "                            <dte:Descripcion>" + detalle.GetString(0) + "</dte:Descripcion>\n";
                        else
                            xml = xml + "                            <dte:Descripcion>" + "*" + detalle.GetString(0) + "</dte:Descripcion>\n";
                        xml = xml + "                            <dte:PrecioUnitario>" + detalle.GetDecimal(2).ToString("#0.000000") + "</dte:PrecioUnitario>\n";
                        xml = xml + "                            <dte:Precio>" + detalle.GetDecimal(4).ToString("#0.000000") + "</dte:Precio>\n";
                        xml = xml + "                            <dte:Descuento>" + detalle.GetDecimal(3).ToString("#0.000000") + "</dte:Descuento>\n";
                        decimal montogravable;
                        if (detalle.GetBoolean(5) == false)
                        {
                            xml = xml + "                            <dte:Impuestos>\n";
                            xml = xml + "                                <dte:Impuesto>\n";
                            xml = xml + "                                    <dte:NombreCorto>IVA</dte:NombreCorto>\n";
                            xml = xml + "                                    <dte:CodigoUnidadGravable>1</dte:CodigoUnidadGravable>\n";
                            montogravable = detalle.GetDecimal(6) / Convert.ToDecimal(1.12);
                            xml = xml + "                                    <dte:MontoGravable>" + montogravable.ToString("#0.000000") + "</dte:MontoGravable>\n";
                            decimal montoimpuesto = montogravable  * Convert.ToDecimal(0.12);
                            mimpuesto = mimpuesto + montoimpuesto;
                            xml = xml + "                                    <dte:MontoImpuesto>" + montoimpuesto.ToString("#0.000000") + "</dte:MontoImpuesto>\n";
                            xml = xml + "                                </dte:Impuesto>\n";
                            xml = xml + "                            </dte:Impuestos>\n";
                        }
                        else
                        {
                            mexento = mexento + detalle.GetDecimal(6);
                            xml = xml + "                            <dte:Impuestos>\n";
                            xml = xml + "                                <dte:Impuesto>\n";
                            xml = xml + "                                    <dte:NombreCorto>IVA</dte:NombreCorto>\n";
                            xml = xml + "                                    <dte:CodigoUnidadGravable>2</dte:CodigoUnidadGravable>\n";
                            xml = xml + "                                    <dte:MontoGravable>" + detalle.GetDecimal(6).ToString("#0.000000") + "</dte:MontoGravable>\n";
                            xml = xml + "                                    <dte:MontoImpuesto>0.00</dte:MontoImpuesto>\n";
                            xml = xml + "                                </dte:Impuesto>\n";
                            xml = xml + "                            </dte:Impuestos>\n";
                        }
                        xml = xml + "                            <dte:Total>" + detalle.GetDecimal(6).ToString("#0.000000") + "</dte:Total>\n";
                        xml = xml + "                        </dte:Item>\n";
                    }
                }
                xml = xml + "                    </dte:Items>\n";
                xml = xml + "                    <dte:Totales>\n";
                xml = xml + "                        <dte:TotalImpuestos>\n";
                xml = xml + "                            <dte:TotalImpuesto NombreCorto=|IVA| TotalMontoImpuesto=|" + mimpuesto.ToString("#0.000000") + "|/>\n";
                xml = xml + "                        </dte:TotalImpuestos>\n";
                xml = xml + "                        <dte:GranTotal>" + datos.GetDecimal(10).ToString("#0.000000") + "</dte:GranTotal>\n";
                xml = xml + "                    </dte:Totales>\n";
                if (datos.GetBoolean(2) == true)
                {
                    xml = xml + "                    <dte:Complementos>";
                    xml = xml + "                         <dte:Complemento IDComplemento=|AbonosFacturaCambiaria| NombreComplemento=|AbonosFacturaCambiaria| URIComplemento=|http://www.sat.gob.gt/dte/fel/CompCambiaria/0.1.0|>";
                    xml = xml + "                              <cfc:AbonosFacturaCambiaria Version=|1|>";
                    xml = xml + "                                   <cfc:Abono>";
                    xml = xml + "                                        <cfc:NumeroAbono>1</cfc:NumeroAbono>";
                    xml = xml + "                                        <cfc:FechaVencimiento>" + datos.GetDateTime(1).ToString("yyyy'-'MM'-'dd") + "</cfc:FechaVencimiento>";
                    xml = xml + "                                        <cfc:MontoAbono>" + datos.GetDecimal(10).ToString("#0.000000") + "</cfc:MontoAbono>";
                    xml = xml + "                                   </cfc:Abono>";
                    xml = xml + "                              </cfc:AbonosFacturaCambiaria>";
                    xml = xml + "                         </dte:Complemento>";
                    xml = xml + "                    </dte:Complementos>";
                }
                xml = xml + "                </dte:DatosEmision>\n";
                xml = xml + "            </dte:DTE>\n";
                if (mexento > 0)
                {
                    xml = xml + "            <dte:Adenda>\n";
                    xml = xml + "               <dte:AdendaDetail id=|AdendaSummary|>\n";
                    xml = xml + "                  <dte:AdendaSummary>\n";
                    xml = xml + "                     <dte:Valor1>* Productos exentos de IVA</dte:Valor1>\n";
                    xml = xml + "                  </dte:AdendaSummary>\n";
                    xml = xml + "               </dte:AdendaDetail>\n";
                    xml = xml + "            </dte:Adenda>\n";
                }
                xml = xml + "         </dte:SAT>\n";
                xml = xml + "</dte:GTDocumento>\n";
            };
            datos.Close();
            return xml;
        }
        public static string CreaXMLFE(int empresa, string serie, int numero)
        {
            SqlCommand comando = new SqlCommand("SELECT F.Moneda, F.Fecha, 1 AS Credito, E.Correo, E.Nit, S.Comercial AS Nombre, S.Direccion, E.[Codigo Postal], E.Municipio, E.Departamento, F.Total, P.Nombre, UPPER('CF') AS Nit, P.Direccion, F.Total  AS STotal, 0 AS Descuento, S.ID, '' AS Correo, ISNULL((SELECT Valor FROM [CXP IMPUESTO] AS I WHERE F.Empresa = I.Empresa AND F.Numero = I.Numero AND F.Tipo = I.Tipo AND I.Impuesto = 5),0) AS ISR, ISNULL((SELECT Valor FROM [CXP IMPUESTO] AS I WHERE F.Empresa = I.Empresa AND F.Numero = I.Numero AND F.Tipo = I.Tipo AND I.Impuesto = 7),0) AS IVA FROM [CXP MAESTRO] AS F INNER JOIN PROVEEDOR AS P ON P.Codigo = F.Proveedor  INNER JOIN EMPRESA AS E ON E.Codigo = F.Empresa INNER JOIN SERIE AS B ON B.Serie = F.Serie AND B.Empresa = F.Empresa AND B.Tipo = 17 INNER JOIN SUCURSAL AS S ON B.Empresa = S.Empresa AND B.Sucursal = S.Codigo WHERE F.Empresa = " + Convert.ToString(empresa) + " AND F.Serie = '" + serie + "' AND F.Factura = " + Convert.ToString(numero), coneccion);
            SqlDataReader datos;
            String xml = "";
            GTDocumento gtdocumento = new GTDocumento();
            datos = comando.ExecuteReader();
            if (datos.HasRows)
            {
                datos.Read();
                xml = "<?xml version=|1.0| encoding=|utf-8| standalone=|no|?>\n";
                xml = xml + "<dte:GTDocumento xmlns:dte=|http://www.sat.gob.gt/dte/fel/0.1.0| Version=|0.4|\n";
                xml = xml + "xmlns:cfc=|http://www.sat.gob.gt/dte/fel/CompCambiaria/0.1.0|\n";
                xml = xml + "xmlns:cno=|http://www.sat.gob.gt/face2/ComplementoReferenciaNota/0.1.0|\n";
                xml = xml + "xmlns:ds=|http://www.w3.org/2000/09/xmldsig#|\n";
                xml = xml + "xmlns:cex=|http://www.sat.gob.gt/face2/ComplementoExportaciones/0.1.0|\n";
                xml = xml + "xmlns:cfe=|http://www.sat.gob.gt/face2/ComplementoFacturaEspecial/0.1.0|>\n";
                xml = xml + "         <dte:SAT ClaseDocumento=|dte|>\n";
                xml = xml + "            <dte:DTE ID=|DatosCertificados|>\n";
                xml = xml + "                <dte:DatosEmision ID=|DatosEmision|>\n";
                if (datos.GetInt32(0) == Convert.ToInt32(1))
                    xml = xml + "                    <dte:DatosGenerales CodigoMoneda=|GTQ| FechaHoraEmision=|" + datos.GetDateTime(1).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + "-06:00" + "| Tipo=|FESP| />\n";
                else
                    xml = xml + "                    <dte:DatosGenerales CodigoMoneda=|USD| FechaHoraEmision=|" + datos.GetDateTime(1).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + "-06:00" + "| Tipo=|FESP| />\n";
                xml = xml + "                    <dte:Emisor AfiliacionIVA=|GEN| CodigoEstablecimiento=|" + datos.GetString(16) + "| CorreoEmisor=|" + datos.GetString(3) + "| NITEmisor=|" + datos.GetString(4).Replace("-", "") + "| NombreComercial=|" + datos.GetString(5) + "| NombreEmisor=|" + datos.GetString(5) + "|>\n";
                xml = xml + "                        <dte:DireccionEmisor>\n";
                xml = xml + "                            <dte:Direccion>" + datos.GetString(6) + "</dte:Direccion>\n";
                xml = xml + "                            <dte:CodigoPostal>" + datos.GetString(7) + "</dte:CodigoPostal>\n";
                xml = xml + "                            <dte:Municipio>" + datos.GetString(8) + "</dte:Municipio>\n";
                xml = xml + "                            <dte:Departamento>" + datos.GetString(9) + "</dte:Departamento>\n";
                xml = xml + "                            <dte:Pais>GT</dte:Pais>\n";
                xml = xml + "                        </dte:DireccionEmisor >\n";
                xml = xml + "                    </dte:Emisor>\n";
                xml = xml + "                    <dte:Receptor CorreoReceptor=|" + datos.GetString(17) + "| IDReceptor=|" + datos.GetString(12).Replace("-", "") + "| NombreReceptor=|" + datos.GetString(11) + "|>\n";
                xml = xml + "                        <dte:DireccionReceptor>\n";
                xml = xml + "                            <dte:Direccion>" + datos.GetString(13) + "</dte:Direccion>\n";
                xml = xml + "                            <dte:CodigoPostal>01001</dte:CodigoPostal>\n";
                xml = xml + "                            <dte:Municipio>Guatemala</dte:Municipio>\n";
                xml = xml + "                            <dte:Departamento>Guatemala</dte:Departamento>\n";
                xml = xml + "                            <dte:Pais>GT</dte:Pais>\n";
                xml = xml + "                        </dte:DireccionReceptor>\n";
                xml = xml + "                    </dte:Receptor>\n";
                xml = xml + "                    <dte:Items>\n";
                SqlConnection condetalle;
                condetalle = new SqlConnection(coneccion.ConnectionString);
                condetalle.Open();
                SqlCommand comdetalle = new SqlCommand("SELECT F.Observaciones AS Descripcion, 1.00 AS Cantidad, F.Total AS Precio , 0.00 AS Descuento,  F.Total AS Total, 0 AS Exento, F.Total AS  PT  FROM [CXP MAESTRO] AS F WHERE F.Empresa = " + Convert.ToString(empresa) + " AND F.Serie = '" + serie + "' AND F.Factura = " + Convert.ToString(numero), condetalle);
                SqlDataReader detalle;
                int contador = 1;
                decimal mimpuesto = 0;
                detalle = comdetalle.ExecuteReader();
                if (detalle.HasRows)
                {
                    while (detalle.Read())
                    {
                        xml = xml + "                        <dte:Item BienOServicio=|B| NumeroLinea=|" + Convert.ToString(contador) + "|>\n";
                        xml = xml + "                            <dte:Cantidad>" + detalle.GetDecimal(1).ToString("#0.00") + "</dte:Cantidad>\n";
                        xml = xml + "                            <dte:UnidadMedida>ST</dte:UnidadMedida>\n";
                        xml = xml + "                            <dte:Descripcion>" + detalle.GetString(0) + "</dte:Descripcion>\n";
                        xml = xml + "                            <dte:PrecioUnitario>" + detalle.GetDecimal(2).ToString("#0.000000") + "</dte:PrecioUnitario>\n";
                        xml = xml + "                            <dte:Precio>" + detalle.GetDecimal(4).ToString("#0.000000") + "</dte:Precio>\n";
                        xml = xml + "                            <dte:Descuento>" + detalle.GetDecimal(3).ToString("#0.000000") + "</dte:Descuento>\n";
                        decimal montogravable;
                        xml = xml + "                            <dte:Impuestos>\n";
                        xml = xml + "                                <dte:Impuesto>\n";
                        xml = xml + "                                    <dte:NombreCorto>IVA</dte:NombreCorto>\n";
                        xml = xml + "                                    <dte:CodigoUnidadGravable>1</dte:CodigoUnidadGravable>\n";
                        montogravable = detalle.GetDecimal(6) / Convert.ToDecimal(1.12);
                        xml = xml + "                                    <dte:MontoGravable>" + montogravable.ToString("#0.000000") + "</dte:MontoGravable>\n";
                        decimal montoimpuesto = montogravable * Convert.ToDecimal(0.12);
                        mimpuesto = mimpuesto + montoimpuesto;
                        xml = xml + "                                    <dte:MontoImpuesto>" + montoimpuesto.ToString("#0.000000") + "</dte:MontoImpuesto>\n";
                        xml = xml + "                                </dte:Impuesto>\n";
                        xml = xml + "                            </dte:Impuestos>\n";
                        xml = xml + "                            <dte:Total>" + detalle.GetDecimal(6).ToString("#0.000000") + "</dte:Total>\n";
                        xml = xml + "                        </dte:Item>\n";
                    }
                }
                xml = xml + "                    </dte:Items>\n";
                xml = xml + "                    <dte:Totales>\n";
                xml = xml + "                        <dte:TotalImpuestos>\n";
                xml = xml + "                            <dte:TotalImpuesto NombreCorto=|IVA| TotalMontoImpuesto=|" + mimpuesto.ToString("#0.000000") + "|/>\n";
                xml = xml + "                        </dte:TotalImpuestos>\n";
                xml = xml + "                        <dte:GranTotal>" + datos.GetDecimal(10).ToString("#0.000000") + "</dte:GranTotal>\n";
                xml = xml + "                    </dte:Totales>\n";
                xml = xml + "                    <dte:Complementos>\n";
                xml = xml + "                        <dte:Complemento IDComplemento=|Especial| NombreComplemento=|Especial| URIComplemento=|http://www.sat.gob.gt/fel/especial.xsd|>\n";
                xml = xml + "                            <cfe:RetencionesFacturaEspecial>\n";
                xml = xml + "                                <cfe:RetencionISR>" + datos.GetDecimal(18).ToString("#0.000000") +  "</cfe:RetencionISR>\n";
                xml = xml + "                                <cfe:RetencionIVA>" + datos.GetDecimal(19).ToString("#0.000000") + "</cfe:RetencionIVA>\n";
                decimal totimp = datos.GetDecimal(10) - datos.GetDecimal(18) - datos.GetDecimal(19);
                xml = xml + "                                <cfe:TotalMenosRetenciones>" + totimp.ToString("#0.000000") + "</cfe:TotalMenosRetenciones>\n";
                xml = xml + "                            </cfe:RetencionesFacturaEspecial>\n";
                xml = xml + "                        </dte:Complemento>\n";
                xml = xml + "                    </dte:Complementos>\n";
                xml = xml + "                </dte:DatosEmision>\n";
                xml = xml + "            </dte:DTE>\n";
                xml = xml + "         </dte:SAT>\n";
                xml = xml + "</dte:GTDocumento>\n";
            };
            datos.Close();
            return xml;
        }
        public static string CreaXMLNC(int empresa, string serie, int numero)
        {
            SqlCommand comando = new SqlCommand("SELECT F.Moneda, F.Fecha, 0 AS Credito, E.Correo, E.Nit, E.Nombre, E.Direccion, E.[Codigo Postal], E.Municipio, E.Departamento, F.Total, M.Nombre, UPPER(F.Nit) AS Nit, F.Direccion, F.Total + ISNULL(F.[Valor Descuento],0) AS STotal, ISNULL(F.[Valor Descuento],0) AS Descuento, substring(M.IDSat,1,10) AS Serie, CASE WHEN M.CAE IS NULL THEN '' ELSE M.IDSat END AS IDSat, M.Fecha,  M.IDSat AS Factura, R.Numero FROM [CREDITO MAESTRO] AS F INNER JOIN [FACTURA MAESTRO] AS M ON M.Empresa = F.Empresa AND M.Numero = F.Factura and M.Serie = F.[Factura Serie] AND M.Tipo = 4 INNER JOIN CLIENTE AS C ON F.Cliente = C.Codigo INNER JOIN EMPRESA AS E ON E.Codigo = F.Empresa LEFT JOIN RESOLUCION AS R ON M.Empresa = R.Empresa AND M.Tipo = R.Tipo AND M.Serie = R.Serie AND M.Numero BETWEEN R.Inicial AND R.Final WHERE F.Empresa = " + Convert.ToString(empresa) + " AND F.Serie = '" + serie + "' AND F.Numero = " + Convert.ToString(numero), coneccion);
            SqlDataReader datos;
            String xml = "";
            datos = comando.ExecuteReader();
            if (datos.HasRows)
            {
                datos.Read();
                xml = "<?xml version=|1.0| encoding=|utf-8| standalone=|no|?>\n";
                xml = xml + "<dte:GTDocumento xmlns:dte=|http://www.sat.gob.gt/dte/fel/0.1.0| Version=|0.4|\n";
                xml = xml + "xmlns:cfc=|http://www.sat.gob.gt/dte/fel/CompCambiaria/0.1.0|\n";
                xml = xml + "xmlns:cno=|http://www.sat.gob.gt/face2/ComplementoReferenciaNota/0.1.0|\n";
                xml = xml + "xmlns:ds=|http://www.w3.org/2000/09/xmldsig#|\n";
                xml = xml + "xmlns:cex=|http://www.sat.gob.gt/face2/ComplementoExportaciones/0.1.0|\n";
                xml = xml + "xmlns:cfe=|http://www.sat.gob.gt/face2/ComplementoFacturaEspecial/0.1.0|>\n";
                xml = xml + "         <dte:SAT ClaseDocumento=|dte|>\n";
                xml = xml + "            <dte:DTE ID=|DatosCertificados|>\n";
                xml = xml + "                <dte:DatosEmision ID=|DatosEmision|>\n";
                if (datos.GetInt32(0) == Convert.ToInt32(1))
                    xml = xml + "                    <dte:DatosGenerales CodigoMoneda=|GTQ| FechaHoraEmision=|" + datos.GetDateTime(1).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + "-06:00" + "| Tipo=|NCRE| />\n";
                else
                    xml = xml + "                    <dte:DatosGenerales CodigoMoneda=|USD| FechaHoraEmision=|" + datos.GetDateTime(1).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + "-06:00" + "| Tipo=|NCRE| />\n";
                xml = xml + "                    <dte:Emisor AfiliacionIVA=|GEN| CodigoEstablecimiento=|1| CorreoEmisor=|" + datos.GetString(3) + "| NITEmisor=|" + datos.GetString(4).Replace("-", "") + "| NombreComercial=|" + datos.GetString(5) + "| NombreEmisor=|" + datos.GetString(5) + "|>\n";
                xml = xml + "                        <dte:DireccionEmisor>\n";
                xml = xml + "                            <dte:Direccion>" + datos.GetString(6) + "</dte:Direccion>\n";
                xml = xml + "                            <dte:CodigoPostal>" + datos.GetString(7) + "</dte:CodigoPostal>\n";
                xml = xml + "                            <dte:Municipio>" + datos.GetString(8) + "</dte:Municipio>\n";
                xml = xml + "                            <dte:Departamento>" + datos.GetString(9) + "</dte:Departamento>\n";
                xml = xml + "                            <dte:Pais>GT</dte:Pais>\n";
                xml = xml + "                        </dte:DireccionEmisor >\n";
                xml = xml + "                    </dte:Emisor>\n";
                xml = xml + "                    <dte:Receptor CorreoReceptor=|| IDReceptor=|" + datos.GetString(12).Replace("-", "") + "| NombreReceptor=|" + datos.GetString(11) + "|>\n";
                xml = xml + "                        <dte:DireccionReceptor>\n";
                xml = xml + "                            <dte:Direccion>" + datos.GetString(13) + "</dte:Direccion>\n";
                xml = xml + "                            <dte:CodigoPostal>01001</dte:CodigoPostal>\n";
                xml = xml + "                            <dte:Municipio>Guatemala</dte:Municipio>\n";
                xml = xml + "                            <dte:Departamento>Guatemala</dte:Departamento>\n";
                xml = xml + "                            <dte:Pais>GT</dte:Pais>\n";
                xml = xml + "                        </dte:DireccionReceptor>\n";
                xml = xml + "                    </dte:Receptor>\n";
                xml = xml + "                    <dte:Items>\n";
                SqlConnection condetalle;
                condetalle = new SqlConnection(coneccion.ConnectionString);
                condetalle.Open();
                SqlCommand comdetalle = new SqlCommand("SELECT F.Descripcion, F.Cantidad, F.Precio, F.Descuento * F.Cantidad AS Descuento, (F.Precio - F.Descuento) * F.Cantidad AS PD, (F.Precio - F.Descuento) * F.Cantidad AS Total, P.Exento, F.Precio * F.Cantidad AS PT  FROM [CREDITO DETALLE] AS F INNER JOIN PRODUCTO AS P ON P.Codigo = F.Producto WHERE F.Empresa = " + Convert.ToString(empresa) + " AND F.Serie = '" + serie + "' AND F.Numero = " + Convert.ToString(numero), condetalle);
                SqlDataReader detalle;
                int contador = 1;
                decimal mimpuesto = 0;
                detalle = comdetalle.ExecuteReader();
                if (detalle.HasRows)
                {
                    while (detalle.Read())
                    {
                        xml = xml + "                        <dte:Item BienOServicio=|B| NumeroLinea=|" + Convert.ToString(contador) + "|>\n";
                        xml = xml + "                            <dte:Cantidad>" + detalle.GetDecimal(1).ToString("#0.00") + "</dte:Cantidad>\n";
                        xml = xml + "                            <dte:UnidadMedida>ST</dte:UnidadMedida>\n";
                        xml = xml + "                            <dte:Descripcion>" + detalle.GetString(0) + "</dte:Descripcion>\n";
                        xml = xml + "                            <dte:PrecioUnitario>" + detalle.GetDecimal(2).ToString("#0.000000") + "</dte:PrecioUnitario>\n";
                        decimal porcentaje;
                        porcentaje = detalle.GetDecimal(7) / datos.GetDecimal(14);
                        porcentaje = datos.GetDecimal(15) * porcentaje;
                        xml = xml + "                            <dte:Precio>" + detalle.GetDecimal(7).ToString("#0.000000") + "</dte:Precio>\n";
                        xml = xml + "                            <dte:Descuento>" + porcentaje.ToString("#0.000000") + "</dte:Descuento>\n";
                        if (detalle.GetBoolean(6) == false)
                        {
                            xml = xml + "                            <dte:Impuestos>\n";
                            xml = xml + "                                <dte:Impuesto>\n";
                            xml = xml + "                                    <dte:NombreCorto>IVA</dte:NombreCorto>\n";
                            xml = xml + "                                    <dte:CodigoUnidadGravable>1</dte:CodigoUnidadGravable>\n";
                            decimal montogravable = (detalle.GetDecimal(4) - porcentaje) / Convert.ToDecimal(1.12);
                            xml = xml + "                                    <dte:MontoGravable>" + montogravable.ToString("#0.000000") + "</dte:MontoGravable>\n";
                            decimal montoimpuesto = montogravable * Convert.ToDecimal(0.12);
                            mimpuesto = mimpuesto + montoimpuesto;
                            xml = xml + "                                    <dte:MontoImpuesto>" + montoimpuesto.ToString("#0.000000") + "</dte:MontoImpuesto>\n";
                            xml = xml + "                                </dte:Impuesto>\n";
                            xml = xml + "                            </dte:Impuestos>\n";
                        }
                        else
                        {
                            xml = xml + "                            <dte:Impuestos>\n";
                            xml = xml + "                                <dte:Impuesto>\n";
                            xml = xml + "                                    <dte:NombreCorto>IVA</dte:NombreCorto>\n";
                            xml = xml + "                                    <dte:CodigoUnidadGravable>2</dte:CodigoUnidadGravable>\n";
                            decimal montogravable = detalle.GetDecimal(4) - porcentaje;
                            xml = xml + "                                    <dte:MontoGravable>" + montogravable.ToString("#0.000000") + "</dte:MontoGravable>\n";
                            xml = xml + "                                    <dte:MontoImpuesto>0.00</dte:MontoImpuesto>\n";
                            xml = xml + "                                </dte:Impuesto>\n";
                            xml = xml + "                            </dte:Impuestos>\n";
                        }
                        decimal total = detalle.GetDecimal(5) - porcentaje;
                        xml = xml + "                            <dte:Total>" + total.ToString("#0.000000") + "</dte:Total>\n";
                        xml = xml + "                        </dte:Item>\n";
                    }
                }
                xml = xml + "                    </dte:Items>\n";
                xml = xml + "                    <dte:Totales>\n";
                xml = xml + "                        <dte:TotalImpuestos>\n";
                xml = xml + "                            <dte:TotalImpuesto NombreCorto=|IVA| TotalMontoImpuesto=|" + mimpuesto.ToString("#0.000000") + "|/>\n";
                xml = xml + "                        </dte:TotalImpuestos>\n";
                xml = xml + "                        <dte:GranTotal>" + datos.GetDecimal(10).ToString("#0.000000") + "</dte:GranTotal>\n";
                xml = xml + "                    </dte:Totales>\n";
                xml = xml + "                    <dte:Complementos>\n";
                xml = xml + "                         <dte:Complemento URIComplemento=|http://www.sat.gob.gt/face2/ComplementoReferenciaNota/0.1.0| NombreComplemento=|Complemento Referencia Nota| IDComplemento=|ComplementoReferenciaNota|>\n";
                if (datos.GetString(17) != "")
                {
                    xml = xml + "                              <cno:ReferenciasNota Version=|0| SerieDocumentoOrigen =|" + datos.GetString(16) + "|\n";
                    xml = xml + "                               NumeroAutorizacionDocumentoOrigen =|" + datos.GetString(20) + "| NumeroDocumentoOrigen=|" + datos.GetString(19) + "| RegimenAntiguo=|Antiguo|\n";
                }
                else
                {
                    xml = xml + "                              <cno:ReferenciasNota Version=|0| SerieDocumentoOrigen =|" + datos.GetString(19).Substring(0, 8) + "|\n";
                    xml = xml + "                               NumeroAutorizacionDocumentoOrigen =|" + datos.GetString(19) + "| MotivoAjuste =|DESCUENTO|\n";
                }
                xml = xml + "                               FechaEmisionDocumentoOrigen =|" + datos.GetDateTime(18).ToString("yyyy'-'MM'-'dd") + "|/>\n";
                xml = xml + "                         </dte:Complemento>\n";
                xml = xml + "                    </dte:Complementos>\n";
                xml = xml + "                </dte:DatosEmision>\n";
                xml = xml + "            </dte:DTE>\n";
                xml = xml + "         </dte:SAT>\n";
                xml = xml + "</dte:GTDocumento>\n";
            };
            datos.Close();
            return xml;
        }
        public static string CreaXMLND(int empresa, string serie, int numero)
        {
            SqlCommand comando = new SqlCommand("SELECT F.Moneda, F.Fecha, 0 AS Credito, E.Correo, E.Nit, E.Nombre, E.Direccion, E.[Codigo Postal], E.Municipio, E.Departamento, F.Total, M.Nombre, M.Nit, M.Direccion, M.Serie, M.Uuid, M.Fecha FROM [DEBITO MAESTRO] AS F INNER JOIN [FACTURA MAESTRO] AS M ON M.Empresa = F.Empresa AND M.Numero = F.Factura and M.Serie = F.[Factura Serie] AND M.Tipo = 4 INNER JOIN CLIENTE AS C ON F.Cliente = C.Codigo INNER JOIN EMPRESA AS E ON E.Codigo = F.Empresa WHERE F.Empresa = " + Convert.ToString(empresa) + " AND F.Serie = '" + serie + "' AND F.Numero = " + Convert.ToString(numero), coneccion);
            SqlDataReader datos;
            String xml = "";
            datos = comando.ExecuteReader();
            if (datos.HasRows)
            {
                datos.Read();
                xml = "<?xml version=|1.0| encoding=|utf-8| standalone=|no|?>\n";
                xml = xml + "<dte:GTDocumento xmlns:dte=|http://www.sat.gob.gt/dte/fel/0.1.0| xmlns:xd=|http://www.w3.org/2000/09/xmldsig#| Version=|0.4|>\n";
                xml = xml + "         <dte:SAT ClaseDocumento=|dte|>\n";
                xml = xml + "            <dte:DTE ID=|DatosCertificados|>\n";
                xml = xml + "                <dte:DatosEmision ID=|DatosEmision|>\n";
                if (datos.GetInt32(0) == Convert.ToInt32(1))
                    xml = xml + "                    <dte:DatosGenerales CodigoMoneda=|GTQ| FechaHoraEmision=|" + datos.GetDateTime(1).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + "-06:00" + "| Tipo=|NDEB| />\n";
                else
                    xml = xml + "                    <dte:DatosGenerales CodigoMoneda=|USD| FechaHoraEmision=|" + datos.GetDateTime(1).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + "-06:00" + "| Tipo=|NDEB| />\n";
                xml = xml + "                    <dte:Emisor AfiliacionIVA=|GEN| CodigoEstablecimiento=|1| CorreoEmisor=|" + datos.GetString(3) + "| NITEmisor=|" + datos.GetString(4).Replace("-", "") + "| NombreComercial=|" + datos.GetString(5) + "| NombreEmisor=|" + datos.GetString(5) + "|>\n";
                xml = xml + "                        <dte:DireccionEmisor>\n";
                xml = xml + "                            <dte:Direccion>" + datos.GetString(6) + "</dte:Direccion>\n";
                xml = xml + "                            <dte:CodigoPostal>" + datos.GetString(7) + "</dte:CodigoPostal>\n";
                xml = xml + "                            <dte:Municipio>" + datos.GetString(8) + "</dte:Municipio>\n";
                xml = xml + "                            <dte:Departamento>" + datos.GetString(9) + "</dte:Departamento>\n";
                xml = xml + "                            <dte:Pais>GT</dte:Pais>\n";
                xml = xml + "                        </dte:DireccionEmisor >\n";
                xml = xml + "                    </dte:Emisor>\n";
                xml = xml + "                    <dte:Receptor CorreoReceptor=|| IDReceptor=|" + datos.GetString(12).Replace("-", "") + "| NombreReceptor=|" + datos.GetString(11) + "|>\n";
                xml = xml + "                        <dte:DireccionReceptor>\n";
                xml = xml + "                            <dte:Direccion>" + datos.GetString(11) + "</dte:Direccion>\n";
                xml = xml + "                            <dte:CodigoPostal>01001</dte:CodigoPostal>\n";
                xml = xml + "                            <dte:Municipio>Guatemala</dte:Municipio>\n";
                xml = xml + "                            <dte:Departamento>Guatemala</dte:Departamento>\n";
                xml = xml + "                            <dte:Pais>GT</dte:Pais>\n";
                xml = xml + "                        </dte:DireccionReceptor>\n";
                xml = xml + "                    </dte:Receptor>\n";
                xml = xml + "                    <dte:Items>\n";
                SqlConnection condetalle;
                condetalle = new SqlConnection(coneccion.ConnectionString);
                condetalle.Open();
                SqlCommand comdetalle = new SqlCommand("SELECT F.Descripcion, F.Cantidad, F.Precio, F.Descuento * F.Cantidad AS Descuento, (F.Precio - F.Descuento) * F.Cantidad AS PD, (F.Precio - F.Descuento) * F.Cantidad AS Total, P.Exento, F.Precio * F.Cantidad AS PT  FROM [DEBITO DETALLE] AS F INNER JOIN PRODUCTO AS P ON P.Codigo = F.Producto WHERE F.Empresa = " + Convert.ToString(empresa) + " AND F.Serie = '" + serie + "' AND F.Numero = " + Convert.ToString(numero), condetalle);
                SqlDataReader detalle;
                int contador = 1;
                decimal mimpuesto = 0;
                detalle = comdetalle.ExecuteReader();
                if (detalle.HasRows)
                {
                    while (detalle.Read())
                    {
                        xml = xml + "                        <dte:Item BienOServicio=|B| NumeroLinea=|" + Convert.ToString(contador) + "|>\n";
                        xml = xml + "                            <dte:Cantidad>" + detalle.GetDecimal(1).ToString("#0.00") + "</dte:Cantidad>\n";
                        xml = xml + "                            <dte:UnidadMedida>ST</dte:UnidadMedida>\n";
                        xml = xml + "                            <dte:Descripcion>" + detalle.GetString(0) + "</dte:Descripcion>\n";
                        xml = xml + "                            <dte:PrecioUnitario>" + detalle.GetDecimal(2).ToString("#0.000000") + "</dte:PrecioUnitario>\n";
                        xml = xml + "                            <dte:Precio>" + detalle.GetDecimal(7).ToString("#0.000000") + "</dte:Precio>\n";
                        xml = xml + "                            <dte:Descuento>" + detalle.GetDecimal(3).ToString("#0.000000") + "</dte:Descuento>\n";
                        if (detalle.GetBoolean(6) == false)
                        {
                            xml = xml + "                            <dte:Impuestos>\n";
                            xml = xml + "                                <dte:Impuesto>\n";
                            xml = xml + "                                    <dte:NombreCorto>IVA</dte:NombreCorto>\n";
                            xml = xml + "                                    <dte:CodigoUnidadGravable>1</dte:CodigoUnidadGravable>\n";
                            decimal montogravable = detalle.GetDecimal(4) / Convert.ToDecimal(1.12);
                            xml = xml + "                                    <dte:MontoGravable>" + montogravable.ToString("#0.000000") + "</dte:MontoGravable>\n";
                            decimal montoimpuesto = montogravable * Convert.ToDecimal(0.12);
                            mimpuesto = mimpuesto + montoimpuesto;
                            xml = xml + "                                    <dte:MontoImpuesto>" + montoimpuesto.ToString("#0.000000") + "</dte:MontoImpuesto>\n";
                            xml = xml + "                                </dte:Impuesto>\n";
                            xml = xml + "                            </dte:Impuestos>\n";
                        }
                        else
                        {
                            xml = xml + "                            <dte:Impuestos>\n";
                            xml = xml + "                                <dte:Impuesto>\n";
                            xml = xml + "                                    <dte:NombreCorto>IVA</dte:NombreCorto>\n";
                            xml = xml + "                                    <dte:CodigoUnidadGravable>2</dte:CodigoUnidadGravable>\n";
                            xml = xml + "                                    <dte:MontoGravable>" + detalle.GetDecimal(4).ToString("#0.000000") + "</dte:MontoGravable>\n";
                            xml = xml + "                                    <dte:MontoImpuesto>0.00</dte:MontoImpuesto>\n";
                            xml = xml + "                                </dte:Impuesto>\n";
                            xml = xml + "                            </dte:Impuestos>\n";
                        }
                        xml = xml + "                            <dte:Total>" + detalle.GetDecimal(5).ToString("#0.000000") + "</dte:Total>\n";
                        xml = xml + "                        </dte:Item>\n";
                    }
                }
                xml = xml + "                    </dte:Items>\n";
                xml = xml + "                    <dte:Totales>\n";
                xml = xml + "                        <dte:TotalImpuestos>\n";
                xml = xml + "                            <dte:TotalImpuesto NombreCorto=|IVA| TotalMontoImpuesto=|" + mimpuesto.ToString("#0.000000") + "|/>\n";
                xml = xml + "                        </dte:TotalImpuestos>\n";
                xml = xml + "                        <dte:GranTotal>" + datos.GetDecimal(10).ToString("#0.000000") + "</dte:GranTotal>\n";
                xml = xml + "                    </dte:Totales>\n";
                xml = xml + "                    <dte:Complementos>\n";
                xml = xml + "                         <dte:Complemento URIComplemento=|http://www.sat.gob.gt/face2/ComplementoReferenciaNota/0.1.0| NombreComplemento=|Complemento Referencia Nota| IDComplemento=|ComplementoReferenciaNota|>\n";
                xml = xml + "                              <cno:ReferenciasNota Version=|0| SerieDocumentoOrigen =|" + datos.GetString(14) + "|\n";
                xml = xml + "                               NumeroAutorizacionDocumentoOrigen =|" + datos.GetString(15) + "| MotivoAjuste =|COBRO|\n";
                xml = xml + "                               FechaEmisionDocumentoOrigen =|" + datos.GetDateTime(16).ToString("yyyy'-'MM'-'dd") + "|\n";
                xml = xml + "                               xmlns:cno=|http://www.sat.gob.gt/face2/ComplementoReferenciaNota/0.1.0|/>\n";
                xml = xml + "                         </dte:Complemento>\n";
                xml = xml + "                    </dte:Complementos>\n";
                xml = xml + "                </dte:DatosEmision>\n";
                xml = xml + "            </dte:DTE>\n";
                xml = xml + "         </dte:SAT>\n";
                xml = xml + "</dte:GTDocumento>\n";
            };
            datos.Close();
            return xml;
        }
        public static string AnulaXML(int empresa, string serie, int numero)
        {
            SqlCommand comando = new SqlCommand("SELECT F.Moneda, F.Fecha, E.Nit, F.Nit, F.UuId FROM [FACTURA MAESTRO] AS F INNER JOIN EMPRESA AS E ON E.Codigo = F.Empresa WHERE F.Empresa = " + Convert.ToString(empresa) + " AND F.Serie = '" + serie + "' AND F.Numero = " + Convert.ToString(numero), coneccion);
            SqlDataReader datos;
            String xml = "";
            datos = comando.ExecuteReader();
            if (datos.HasRows)
            {
                datos.Read();
                xml = "<?xml version=|1.0| encoding=|UTF-8|?>\n";
                xml = xml + "<ns:GTAnulacionDocumento xmlns:ds=|http://www.w3.org/2000/09/xmldsig#| xmlns:ns=|http://www.sat.gob.gt/dte/fel/0.1.0| xmlns:xsi=|http://www.w3.org/2001/XMLSchema-instance|  Version=|0.1| >\n";
                xml = xml + "   <ns:SAT>\n";
                xml = xml + "      <ns:AnulacionDTE ID=|DatosCertificados|>\n";
                xml = xml + "         <ns:DatosGenerales ID=|DatosAnulacion|\n";
                xml = xml + "               NumeroDocumentoAAnular=|" + datos.GetString(4) + "|\n";
                xml = xml + "               NITEmisor=|" + datos.GetString(2).Replace("-", "") + "|\n";
                xml = xml + "               IDReceptor=|" + datos.GetString(3) + "|\n";
                xml = xml + "               FechaEmisionDocumentoAnular=|" + datos.GetDateTime(1).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + "-06:00" + "|\n";
                xml = xml + "               FechaHoraAnulacion=|" + DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + "-06:00" + "|\n";
                xml = xml + "               MotivoAnulacion=|cancelar|/>\n";
                xml = xml + "      </ns:AnulacionDTE>\n";
                xml = xml + "   </ns:SAT>\n";
                xml = xml + "</ns:GTAnulacionDocumento>\n";
            }
            return xml;
        }
        public static string AnulaXMLNC(int empresa, string serie, int numero)
        {
            SqlCommand comando = new SqlCommand("SELECT F.Moneda, F.Fecha, E.Nit, M.Nit, F.UuId FROM [CREDITO MAESTRO] AS F INNER JOIN [FACTURA MAESTRO] AS M ON M.Empresa = F.Empresa AND M.Numero = F.Factura and M.Serie = F.[Factura Serie] AND M.Tipo = 4 INNER JOIN CLIENTE AS C ON F.Cliente = C.Codigo INNER JOIN EMPRESA AS E ON E.Codigo = F.Empresa WHERE F.Empresa = " + Convert.ToString(empresa) + " AND F.Serie = '" + serie + "' AND F.Numero = " + Convert.ToString(numero), coneccion);
            SqlDataReader datos;
            String xml = "";
            datos = comando.ExecuteReader();
            if (datos.HasRows)
            {
                datos.Read();
                xml = "<?xml version=|1.0| encoding=|UTF-8|?>\n";
                xml = xml + "<ns:GTAnulacionDocumento xmlns:ds=|http://www.w3.org/2000/09/xmldsig#| xmlns:ns=|http://www.sat.gob.gt/dte/fel/0.1.0| xmlns:xsi=|http://www.w3.org/2001/XMLSchema-instance|  Version=|0.1| >\n";
                xml = xml + "   <ns:SAT>\n";
                xml = xml + "      <ns:AnulacionDTE ID=|DatosCertificados|>\n";
                xml = xml + "         <ns:DatosGenerales ID=|DatosAnulacion|\n";
                xml = xml + "               NumeroDocumentoAAnular=|" + datos.GetString(4) + "|\n";
                xml = xml + "               NITEmisor=|" + datos.GetString(2).Replace("-", "") + "|\n";
                xml = xml + "               IDReceptor=|" + datos.GetString(3) + "|\n";
                xml = xml + "               FechaEmisionDocumentoAnular=|" + datos.GetDateTime(1).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + "-06:00" + "|\n";
                xml = xml + "               FechaHoraAnulacion=|" + DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + "-06:00" + "|\n";
                xml = xml + "               MotivoAnulacion=|cancelar|/>\n";
                xml = xml + "      </ns:AnulacionDTE>\n";
                xml = xml + "   </ns:SAT>\n";
                xml = xml + "</ns:GTAnulacionDocumento>\n";
            }
            return xml;
        }
        public static string AnulaXMLND(int empresa, string serie, int numero)
        {
            SqlCommand comando = new SqlCommand("SELECT F.Moneda, F.Fecha, E.Nit, M.Nit, F.UuId FROM [DEBITO MAESTRO] AS F INNER JOIN [FACTURA MAESTRO] AS M ON M.Empresa = F.Empresa AND M.Numero = F.Factura and M.Serie = F.[Factura Serie] AND M.Tipo = 4 INNER JOIN CLIENTE AS C ON F.Cliente = C.Codigo INNER JOIN EMPRESA AS E ON E.Codigo = F.Empresa WHERE F.Empresa = " + Convert.ToString(empresa) + " AND F.Serie = '" + serie + "' AND F.Numero = " + Convert.ToString(numero), coneccion);
            SqlDataReader datos;
            String xml = "";
            datos = comando.ExecuteReader();
            if (datos.HasRows)
            {
                datos.Read();
                xml = "<?xml version=|1.0| encoding=|UTF-8|?>\n";
                xml = xml + "<ns:GTAnulacionDocumento xmlns:ds=|http://www.w3.org/2000/09/xmldsig#| xmlns:ns=|http://www.sat.gob.gt/dte/fel/0.1.0| xmlns:xsi=|http://www.w3.org/2001/XMLSchema-instance|  Version=|0.1| >\n";
                xml = xml + "   <ns:SAT>\n";
                xml = xml + "      <ns:AnulacionDTE ID=|DatosCertificados|>\n";
                xml = xml + "         <ns:DatosGenerales ID=|DatosAnulacion|\n";
                xml = xml + "               NumeroDocumentoAAnular=|" + datos.GetString(4) + "|\n";
                xml = xml + "               NITEmisor=|" + datos.GetString(2).Replace("-", "") + "|\n";
                xml = xml + "               IDReceptor=|" + datos.GetString(3) + "|\n";
                xml = xml + "               FechaEmisionDocumentoAnular=|" + datos.GetDateTime(1).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + "-06:00" + "|\n";
                xml = xml + "               FechaHoraAnulacion=|" + DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss") + "-06:00" + "|\n";
                xml = xml + "               MotivoAnulacion=|cancelar|/>\n";
                xml = xml + "      </ns:AnulacionDTE>\n";
                xml = xml + "   </ns:SAT>\n";
                xml = xml + "</ns:GTAnulacionDocumento>\n";
            }
            return xml;
        }

        public class SolicitaTokenRequest
        {
            public string usuario { get; set; }
            public string apikey { get; set; }

        }
        public class SolicitaTokenResponse
        {
            public string tipo_respuesta { get; set; }
            public string cod_error { get; set; }
            public string desc_error { get; set; }
            public string token { get; set; }
            public string vigencia { get; set; }
        }
        public class RegistraDocumentoXMLResponse
        {
            public string xml_dte { get; set; }
            public string uuid { get; set; }
        }

        public class RegistraDocumentoXMLRequest
        {
            public string  xml_dte { get; set; }
        }
        public class AnulaDocumentoXMLRequest
        {
            public string xml_dte { get; set; }
        }
        public class GTDocumento
        {
            private DSAT _sat;
            public DSAT SAT
            {
                get => _sat;
                set => _sat = value;
            }
            public class DSAT
            {
                public string ClaseDocumento { get; set; }
                public Cdte DTE { get; set; }

                public class Cdte
                {
                    public string ID { get; set; }
                    private CDatosEmision _datosemision;
                    public CDatosEmision DatosEmision
                    {
                        get => _datosemision;
                        set => _datosemision = value;
                    }
                    public class CDatosEmision
                    {
                        private DDatosGenerales _datosgenerales;
                        public DDatosGenerales DatosGenerales
                        {
                            get => _datosgenerales;
                            set => _datosgenerales = value;
                        }
                        public class DDatosGenerales
                        {
                            public string CodigoMoneda { get; set; }
                            public DateTime FechaHoraEmision { get; set; }
                            public string NumeroAcceso { get; set; }
                            public string Tipo { get; set; }
                        }
                        private DEmisor _emisor;
                        public DEmisor Emisor
                        {
                            get => _emisor;
                            set => _emisor = value;
                        }
                        public class DEmisor
                        {
                            public string AfiliacionIva { get; set; }
                            public string CodigoEstablecimiento { get; set; }
                            public string CorreoEmisor { get; set; }
                            public string NITEmisor { get; set; }
                            public string NombreComercial { get; set; }
                            public string NombreEmisor { get; set; }
                            private DDireccionEmisor _direccionemisor;
                            public DDireccionEmisor DireccionEmisor
                            {
                                get => _direccionemisor;
                                set => _direccionemisor = value;
                            }
                            public class DDireccionEmisor
                            {
                                public string Direccion { get; set; }
                                public string CodigoPostal { get; set; }
                                public string Municipio { get; set; }
                                public string Departamento { get; set; }
                                public string Pais { get; set; }
                            }
                        }
                        private DReceptor _receptor;
                        public DReceptor Receptor
                        {
                            get => _receptor;
                            set => _receptor = value;
                        }
                        public class DReceptor
                        {
                            public string CorreoReceptor { get; set; }
                            public string IDReceptor { get; set; }
                            public string NombreReceptor { get; set; }
                        }
                        private List<Frase> _frases;
                        public List<Frase> Frases
                        {
                            get => _frases;
                            set => _frases = value;
                        }
                        public class Frase
                        {
                            public string CodigoEscenario { get; set; }
                            public string TipoFrase { get; set; }
                        }
                        
                        private List<Item> _items;
                        public List<Item> Items
                        {
                            get => _items;
                            set => _items = value;
                        }
                        public class Item
                        {
                            public string BienOServicio { get; set; }
                            public int NumeroLinea { get; set; }
                            public decimal Cantidad { get; set; }
                            public string UnidadMedida { get; set; }
                            public string Descripcion { get; set; }
                            public decimal PrecioUnitario { get; set; }
                            public decimal Precio { get; set; }
                            public decimal Descuento { get; set; }

                            private List<Impuesto> _impuestos;
                            public List<Impuesto> Impuestos
                            {
                                get => _impuestos;
                                set => _impuestos = value;
                            }
                            public class Impuesto
                            {
                                public string NombreCorto { get; set; }
                                public decimal CodigoUnidadGravable { get; set; }
                                public decimal MontoGravable { get; set; }
                                public decimal MontoImpuesto { get; set; }
                            }
                            public decimal Total { get; set; }
                        }
                        private DTotales _totales;
                        public DTotales Totales
                        {
                            get => _totales;
                            set => _totales = value;
                        }
                        public class DTotales
                        {
                            private List<TotalImpuesto> _totalimpuestos;
                            public List<TotalImpuesto> TotalImpuestos
                            {
                                get => _totalimpuestos;
                                set => _totalimpuestos = value;
                            }
                            public class TotalImpuesto
                            {
                                public string NombreCorto { get; set; }
                                public decimal TotalMontoImpuesto { get; set; }
                            }
                            public decimal GranTotal { get; set; }
                        }
                    }
                }
            }
        }
        //Invocación de la firma de documento, retorno  y almacenamiento de este
        public static XmlDocument FirmarDocumento(string rutaCertificado, string contraseñaCertificado, string rutaDocumento, string ubicacionDestino)
        {
            X509Certificate2 cert;
            cert = new X509Certificate2(rutaCertificado, contraseñaCertificado, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            SignatureParameters parametros = ParametrosdeFirma();
            DateTime fecha = DateTime.Now;
            parametros.SigningDate = fecha.AddSeconds(-120);
            var nombredocumento = Path.GetFileNameWithoutExtension(rutaDocumento);
            using (parametros.Signer = new Signer(cert))
            {
                var documento = FirmaXades(parametros, rutaDocumento);
                AlmacenamientoDocumento(documento, ubicacionDestino, nombredocumento);
                return documento.Document;
            }
        }
        //Invocación de la firma de documento y retorno de este
        public static XmlDocument FirmarDocumento(string rutaCertificado, string contraseñaCertificado, string rutaDocumento)
        {
            X509Certificate2 cert = new X509Certificate2(rutaCertificado, contraseñaCertificado, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            SignatureParameters parametros = ParametrosdeFirma();
            DateTime fecha = DateTime.Now;
            parametros.SigningDate = fecha.AddSeconds(-120);
            using (parametros.Signer = new Signer(cert))
            {
                return FirmaXades(parametros, rutaDocumento).Document;
            }
        }
        //Firma del documento
        private static SignatureDocument FirmaXades(SignatureParameters sp, string ruta)
        {
            XadesService xadesService = new XadesService();
            using (FileStream fs = new FileStream(ruta, FileMode.Open))
            {
                var documento = xadesService.Sign(fs, sp);
                MoverNodoFirma(documento);
                return documento;
            }
        }
        //Almacenamiento e ruta especifica
        private static void AlmacenamientoDocumento(SignatureDocument sd, string ruta, string nombre)
        {
            ruta = @"{ruta}\{nombre}-Firmado.xml";
            sd.Save(ruta);
        }
        //Parametros para la firma del documento
        private static SignatureParameters ParametrosdeFirma()
        {
            SignatureParameters parametros = new SignatureParameters
            {
                SignaturePackaging = SignaturePackaging.INTERNALLY_DETACHED,
                InputMimeType = "text/xml",
                ElementIdToSign = "DatosEmision",
                SignatureMethod = SignatureMethod.RSAwithSHA256,
                DigestMethod = DigestMethod.SHA256
            };
            if (operacion == 2)
            { parametros.ElementIdToSign = "DatosAnulacion"; }
            return parametros;
        }
        //Cambio de posicion del nodo de la firma en el nodo padre del documento
        private static void MoverNodoFirma(SignatureDocument sd)
        {
            var documento = sd.Document;
            var NodoFirma = documento.GetElementsByTagName("ds:Signature")[0];
            NodoFirma.ParentNode.RemoveChild(NodoFirma);
            documento.DocumentElement.AppendChild(NodoFirma);
        }
        public class dteResponse
        {
            public string xml_certificado { get; set; }
            public string uuid { get; set; }
            public string numero { get; set; }
            public string serie { get; set; }
        }
    }
}
