using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace Com.StartLineSoft.SincronizadorRedCoopcentral
{
    class ModificarCuentaCorriente
    {
        private EntidadesDataContext edx = new EntidadesDataContext();

        Cripter cripter;

        private String JsonEnvio { get; set; }

        private Transaccion transaccion;
        private JObject JsonObject;

        public ModificarCuentaCorriente(Transaccion transaccion)
        {
            this.transaccion = transaccion;
            this.JsonEnvio = this.transaccion.JsonEnvio;
            this.JsonObject = JObject.Parse(this.JsonEnvio);

            this.cripter = new Cripter(this.transaccion.Convenio.LlaveA, this.transaccion.Convenio.LlaveB);
        }

        public Boolean Crear()
        {
            String secuenciaModificarCuentaCorriente = null;
            String xmlModificarCuentaCorriente = null;
            DatoTransaccion dtModificarCuentaCorriente = null;

            secuenciaModificarCuentaCorriente = this.obtenerNumeroSecuencia();
            xmlModificarCuentaCorriente = this.XmlModificarCuentaCorriente(secuenciaModificarCuentaCorriente);
            dtModificarCuentaCorriente = this.CreaDatoTransaccion(secuenciaModificarCuentaCorriente, xmlModificarCuentaCorriente);

            //Se llama a la modificación de la cuenta corriente
            if (dtModificarCuentaCorriente != null && this.LlamarWs(dtModificarCuentaCorriente))
            {
                return false;
            }

            return true;
        }

        private Boolean LlamarWs(DatoTransaccion dato)
        {
            String respuesta = "";
            bool error = false;

            try
            {
                bool bRespuesta = false;
                String res = "";
                WebService ws = new WebService(dato);
                respuesta = ws.ejecutar();
                cripter.AESDecode(respuesta, dato.HashXml, ref bRespuesta, ref res);
                dato.XmlRespuestaEncriptada = respuesta;
                dato.XmlRespuesta = res;

                XmlDocument document = new XmlDocument();
                document.LoadXml(res);
                var lista = document.GetElementsByTagName("S050");
                foreach (XmlNode nodo in lista)
                {
                    if (!nodo.InnerText.Trim().Contains("0")) throw new Exception("Nodo S050 no es 0");
                }
                dato.EsErroneo = false;
            }
            catch (Exception e)
            {
                error = true;
                dato.EsErroneo = true;
            }
            dato.UpdatedAt = DateTime.Now;
            this.edx.SubmitChanges();

            return error;
        }

        private DatoTransaccion CreaDatoTransaccion(String secuencia, String xml)
        {
            String xmlEncriptado = null;
            String hashXml = null;
            this.cripter.AESEncode(xml, ref xmlEncriptado, ref hashXml);

            DatoTransaccion dt = new DatoTransaccion();

            dt.TransaccionId = this.transaccion.Id;
            dt.Anio = int.Parse(DateTime.Now.ToString("yyyy"));
            dt.Mes = int.Parse(DateTime.Now.ToString("MM"));
            dt.Dia = int.Parse(DateTime.Now.ToString("dd"));
            dt.NumeroSecuencia = int.Parse(secuencia.Substring(secuencia.Length - 6));
            //dt.Numero = secuenciaCrearCliente;
            dt.XmlEnvio = xml;
            dt.XmlEnvioEncriptado = xmlEncriptado;
            dt.HashXml = hashXml;
            dt.CreatedAt = DateTime.Now;
            dt.UpdatedAt = DateTime.Now;

            this.edx.DatoTransaccions.InsertOnSubmit(dt);
            this.edx.SubmitChanges();

            return dt;
        }

        /// <summary>
        /// Crea trama XML de creación cuenta de ahorros
        /// </summary>
        /// <param name="secuencia"></param>
        /// <returns></returns>
        private String XmlModificarCuentaCorriente(String secuencia)
        {
            String codigoConvenio = "00000000" + this.transaccion.Convenio.CodigoConvenio;
            codigoConvenio = codigoConvenio.Substring(codigoConvenio.Length - 8);

            var cupo = (string)this.JsonObject["cupoDisponible"];
            cupo = cupo.Replace(".", "");

            var obj = new
            {
                ECG = new
                {
                    S001 = DateTime.Now.ToString("yyyyMMdd"), // Fecha de la transacción
                    S002 = DateTime.Now.ToString("hhmmss"), // Hora de la transacción
                    S003 = codigoConvenio, // Código del convenio
                    S004 = "0000", // Sucursal
                    S005 = "0000", // Terminal
                    S007 = "ECG0", // Grupo transaccional
                    S008 = "1001", // Transacción
                    S009 = secuencia, // Secuencia
                    S010 = "06", // Origen
                    S018 = "04", // Canal
                    S01T = "W4", // Tipo terminal
                    S020 = this.JsonObject["numero_cuenta_corriente"], // Número de cuenta corriente
                    S03A = "50", // Tipo de cuenta
                    S03AT = "CR", // Subcategoría de cuenta
                    S025 = codigoConvenio, // Entidad, Convenio
                    S02V = this.obtenerFecha(this.JsonObject, "fecha_asignacion"), // Fecha apertura de cuenta
                    S040 = this.JsonObject["tercero"]["numero_identificacion"], // Número de identificación del tercero
                    S041 = this.obtenerTipoIdentificacion(this.JsonObject["tercero"]["tipo_identificacion"]), // Tipo de identificación
                    SV00 = 0, // Si la cuenta es principal
                    SX20 = "00", // Esado de la cuenta
                    S054 = cupo, // Saldo total
                    S055 = cupo, // Saldo disponible
                    GMF1 = 0, // GMF acumulado
                    GMF2 = "S", // GMF .... (Sin descripción)
                    S02M = 0, // Cuenta tipo CNB
                    S03X = "CAS", // Usuario cooperativa
                    PSE4 = "181.143.185.66" // IP publica cliente
                }
            };

            String objJson = JsonConvert.SerializeObject(obj);
            XmlDocument xml = (XmlDocument)JsonConvert.DeserializeXmlNode(objJson);
            string xmlPlano = "<?xml version=\"1.0\" encoding=\"Windows-1252\" standalone=\"no\"?>";
            xmlPlano += xml.OuterXml;
            return xmlPlano;
        }

        #region Métodos auxiliares

        /// <summary>
        /// Obtener tipo de identificación
        /// </summary>
        /// <param name="tipoIdentificacion"></param>
        /// <returns></returns>
        private int obtenerTipoIdentificacion(JToken tipoIdentificacion)
        {
            int codigo = 0;
            String tipo = (String)tipoIdentificacion["codigo"];

            switch (tipo)
            {
                case "CC":
                    codigo = 0;
                    break;
                case "NIT":
                    codigo = 9;
                    break;
                case "TI":
                    codigo = 2;
                    break;
                case "CE":
                    codigo = 1;
                    break;
                default:
                    codigo = 0;
                    break;
            }
            return codigo;
        }

        private String obtenerFecha(JToken tercero, String campo)
        {
            String fecha = "";
            try
            {
                var fechaNacimiento = (String)tercero[campo];
                fecha = Regex.Replace(fechaNacimiento, @"[^\w]", "").Replace(" ", "").Trim().Substring(0, 8);
            }
            catch (System.InvalidOperationException e) { }
            catch (System.ArgumentNullException e) { }
            return fecha;
        }

        /// <summary>
        /// Obtiene el número de la secuencia
        /// </summary>
        /// <returns></returns>
        private String obtenerNumeroSecuencia()
        {
            var numeroSecuencia = edx.fn_obtener_numero_transaccion((int)this.transaccion.Convenio.Id);
            var secuencia = "000000" + numeroSecuencia;
            secuencia = DateTime.Now.ToString("yyyyMMdd") + secuencia.Substring(secuencia.Length - 6);
            return secuencia;
        }

        #endregion

        private string Hash(string input)
        {
            var hash = (new SHA1Managed()).ComputeHash(Encoding.UTF8.GetBytes(input));
            return string.Join("", hash.Select(b => b.ToString("x2")).ToArray());
        }
    }
}
