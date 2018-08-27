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
    class CrearTarjetaHabiente
    {
        private EntidadesDataContext edx = new EntidadesDataContext();

        Cripter cripter;

        private String JsonEnvio { get; set; }

        private Transaccion transaccion;
        private JObject JsonObject;

        public CrearTarjetaHabiente(Transaccion transaccion)
        {
            this.transaccion = transaccion;
            this.JsonEnvio = this.transaccion.JsonEnvio;
            this.JsonObject = JObject.Parse(this.JsonEnvio);

            this.cripter = new Cripter(this.transaccion.Convenio.LlaveA, this.transaccion.Convenio.LlaveB);
        }

        public Boolean crear()
        {
            String secuenciaCrearCliente = this.obtenerNumeroSecuencia();
            String xmlCrearCliente = this.XmlCrearCliente(secuenciaCrearCliente);
            var dtCrearCliente = this.creaDatoTransaccion(secuenciaCrearCliente, xmlCrearCliente);

            String secuenciaCrearCuentaAhorros = null;
            String xmlCrearCuentaAhorros = null;
            DatoTransaccion dtCrearCuentaAhorros = null;

            String secuenciaCrearCuentaCorriente = null;
            String xmlCrearCuentaCorriente = null;
            DatoTransaccion dtCrearCuentaCorriente = null;

            String secuenciaCrearRelacionCuentaAhorro = null;
            String xmlCrearRelacionCuentaAhorro = null;
            DatoTransaccion dtCrearRelacionCuentaAhorro = null;

            String secuenciaCrearRelacionCuentaCorriente = null;
            String xmlCrearRelacionCuentaCorriente = null;
            DatoTransaccion dtCrearRelacionCuentaCorriente = null;

            switch ((String)this.JsonObject["producto"]["modalidad"])
            {
                case "CUENTAAHORROS":
                    secuenciaCrearCuentaAhorros = this.obtenerNumeroSecuencia();
                    xmlCrearCuentaAhorros = this.XmlCrearCuentaAhorros(secuenciaCrearCuentaAhorros);
                    dtCrearCuentaAhorros = this.creaDatoTransaccion(secuenciaCrearCuentaAhorros, xmlCrearCuentaAhorros);
                    break;
                case "CREDITO":
                    secuenciaCrearCuentaCorriente = this.obtenerNumeroSecuencia();
                    xmlCrearCuentaCorriente = this.XmlCrearCuentaCorriente(secuenciaCrearCuentaCorriente);
                    dtCrearCuentaCorriente = this.creaDatoTransaccion(secuenciaCrearCuentaCorriente, xmlCrearCuentaCorriente);
                    break;
                case "MIXTO":
                    secuenciaCrearCuentaAhorros = this.obtenerNumeroSecuencia();
                    xmlCrearCuentaAhorros = this.XmlCrearCuentaAhorros(secuenciaCrearCuentaAhorros);
                    dtCrearCuentaAhorros = this.creaDatoTransaccion(secuenciaCrearCuentaAhorros, xmlCrearCuentaAhorros);

                    secuenciaCrearCuentaCorriente = this.obtenerNumeroSecuencia();
                    xmlCrearCuentaCorriente = this.XmlCrearCuentaCorriente(secuenciaCrearCuentaCorriente);
                    dtCrearCuentaCorriente = this.creaDatoTransaccion(secuenciaCrearCuentaCorriente, xmlCrearCuentaCorriente);
                    break;
            }

            String secuenciaCrearTarjeta = this.obtenerNumeroSecuencia();
            String xmlCrearTarjeta = this.XmlCrearTarjeta(secuenciaCrearTarjeta);
            DatoTransaccion dtCrearTarjeta = this.creaDatoTransaccion(secuenciaCrearTarjeta, xmlCrearTarjeta);

            if (dtCrearCuentaAhorros != null)
            {
                secuenciaCrearRelacionCuentaAhorro = this.obtenerNumeroSecuencia();
                xmlCrearRelacionCuentaAhorro = this.XmlCrearRelacionCuentaAhorro(secuenciaCrearRelacionCuentaAhorro);
                dtCrearRelacionCuentaAhorro = this.creaDatoTransaccion(secuenciaCrearRelacionCuentaAhorro, xmlCrearRelacionCuentaAhorro);
            }

            if (dtCrearCuentaCorriente != null)
            {
                secuenciaCrearRelacionCuentaCorriente = this.obtenerNumeroSecuencia();
                xmlCrearRelacionCuentaCorriente = this.XmlCrearRelacionCuentaCorriente(secuenciaCrearRelacionCuentaCorriente);
                dtCrearRelacionCuentaCorriente = this.creaDatoTransaccion(secuenciaCrearRelacionCuentaCorriente, xmlCrearRelacionCuentaCorriente);
            }

            //Se llama a la creación del cliente
            if (this.llamarWs(dtCrearCliente))
            {
                return false;
            }

            //Se llama a la creación de la cuenta de ahorros
            if (dtCrearCuentaAhorros != null && this.llamarWs(dtCrearCuentaAhorros))
            {
                return false;
            }

            //Se llama a la creación de la cuenta corriente
            if (dtCrearCuentaCorriente != null && this.llamarWs(dtCrearCuentaCorriente))
            {
                return false;
            }

            //Se llama a la creación de la tarjeta
            if (this.llamarWs(dtCrearTarjeta))
            {
                return false;
            }

            //Se llama a la creación de la relación con la tarjeta de ahorros
            if (dtCrearCuentaAhorros != null & this.llamarWs(dtCrearRelacionCuentaAhorro))
            {
                return false;
            }

            //Se llama a la creación de la relación con la tarjeta de ahorros
            if (dtCrearCuentaCorriente != null & this.llamarWs(dtCrearRelacionCuentaCorriente))
            {
                return false;
            }

            return true;
        }

        private Boolean llamarWs(DatoTransaccion dato)
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
                foreach(XmlNode nodo in lista)
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

        private DatoTransaccion creaDatoTransaccion(String secuencia, String xml)
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
        /// Crea trama XML de creación de cliente
        /// </summary>
        /// <returns></returns>
        private String XmlCrearCliente(String secuencia)
        {
            String codigoConvenio = "00000000" + this.transaccion.Convenio.CodigoConvenio;
            codigoConvenio = codigoConvenio.Substring(codigoConvenio.Length - 8);

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
                    S008 = "0001", // Transacción
                    S009 = secuencia, // Secuencia
                    S010 = "06", // Origen
                    S018 = "04", // Canal
                    S01T = "W4", // Tipo terminal
                    S025 = codigoConvenio, // Entidad, Convenio
                    S040 = this.JsonObject["tercero"]["numero_identificacion"], // Número de identificación del tercero
                    S041 = this.obtenerTipoIdentificacion(this.JsonObject["tercero"]["tipo_identificacion"]), // Tipo de identificación
                    S042A = this.JsonObject["tercero"]["primer_nombre"], // Primer nombre
                    S042B = this.JsonObject["tercero"]["segundo_nombre"], // Segundo nombre
                    S043A = this.JsonObject["tercero"]["primer_apellido"], // Primer apellido
                    S043B = this.JsonObject["tercero"]["segundo_apellido"], // Segundo apellido
                    S044 = this.obtenerDireccion(this.JsonObject["tercero"]), // Dirección
                    S045 = "", // Dirección laboral
                    S046 = this.obtenerTelefono(this.JsonObject["tercero"]), // Teléfono
                    S047 = "", // Teléfono laboral
                    S048 = this.obtenerCelular(this.JsonObject["tercero"]), // Teléfono celular
                    S049 = this.obtenerFecha(this.JsonObject["tercero"], "fecha_nacimiento"), // Fecha de nacimiento
                    S04N = this.obtenerFecha(this.JsonObject["tercero"], "fecha_expedicion_documento_identidad"), // Fecha expedición documento de identidad
                    S04A = this.obtenerSexo(this.JsonObject["tercero"]["sexo"]), // Sexo
                    S04B = this.obtenerEmail(this.JsonObject["tercero"]["sexo"]), // Correo electrónico
                    S04C = this.obtenerCodigoPais(this.JsonObject["tercero"]), // Código páis nacimiento
                    S04D = this.obtenerCodigoDepartamento(this.JsonObject["tercero"]), // Código departamento nacimiento
                    S04E = this.obtenerCodigoCiudad(this.JsonObject["tercero"]), // Código ciudad nacimiento
                    S04F = this.obtenerCodigoPaisContacto(this.JsonObject["tercero"]), // Código país de residencia
                    S04G = this.obtenerCodigoDepartamentoContacto(this.JsonObject["tercero"]), // Código departamento de residencia
                    S04H = this.obtenerCodigoCiudadContacto(this.JsonObject["tercero"]), // Código ciudad de residencia
                    S03X = "CAS", // Usuario cooperativa
                    S04M = this.JsonObject["producto"]["numero_retiros_sin_cobro_red"], //Numero de transacciones sin costo
                    SE60 = 1, // Activación SMS
                    SE61 = 0, // Activación OTP
                    PSE4 = "181.143.185.66" // IP publica cliente
                }
            };

            String objJson = JsonConvert.SerializeObject(obj);
            XmlDocument xml = (XmlDocument)JsonConvert.DeserializeXmlNode(objJson);
            string xmlPlano = "<?xml version=\"1.0\" encoding=\"Windows-1252\" standalone=\"no\"?>";
            xmlPlano += xml.OuterXml;
            return xmlPlano;
        }

        /// <summary>
        /// Crea trama XML de creación cuenta de ahorros
        /// </summary>
        /// <param name="secuencia"></param>
        /// <returns></returns>
        private String XmlCrearCuentaAhorros(String secuencia)
        {
            String codigoConvenio = "00000000" + this.transaccion.Convenio.CodigoConvenio;
            codigoConvenio = codigoConvenio.Substring(codigoConvenio.Length - 8);

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
                    S020 = this.JsonObject["cuenta_ahorro"]["numero_cuenta"], // Número de cuenta ahorros
                    S03A = "10", // Tipo de cuenta
                    S03AT = "AV", // Subcategoría de cuenta
                    S025 = codigoConvenio, // Entidad, Convenio
                    S02V = this.obtenerFecha(this.JsonObject["cuenta_ahorro"], "fecha_apertura"), // Fecha apertura de cuenta
                    S040 = this.JsonObject["tercero"]["numero_identificacion"], // Número de identificación del tercero
                    S041 = this.obtenerTipoIdentificacion(this.JsonObject["tercero"]["tipo_identificacion"]), // Tipo de identificación
                    SV00 = 1, // Si la cuenta es principal
                    SX20 = "00", // Esado de la cuenta
                    S054 = 0, // Saldo total
                    S055 = 0, // Saldo disponible
                    GMF1 = 0, // GMF acumulado
                    GMF2 = "S", // GMF .... (Sin descripción)
                    S02M = 1, // Cuenta tipo CNB
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

        /// <summary>
        /// Crea trama XML de creación cuenta de ahorros
        /// </summary>
        /// <param name="secuencia"></param>
        /// <returns></returns>
        private String XmlCrearCuentaCorriente(String secuencia)
        {
            String codigoConvenio = "00000000" + this.transaccion.Convenio.CodigoConvenio;
            codigoConvenio = codigoConvenio.Substring(codigoConvenio.Length - 8);

            var cupo = (string)this.JsonObject["cupo"];
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

        /// <summary>
        /// Crea trama XML de creación de tarjeta
        /// </summary>
        /// <param name="secuencia"></param>
        /// <returns></returns>
        private String XmlCrearTarjeta(String secuencia)
        {
            String codigoConvenio = "00000000" + this.transaccion.Convenio.CodigoConvenio;
            codigoConvenio = codigoConvenio.Substring(codigoConvenio.Length - 8);

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
                    S008 = "3004", // Transacción
                    S009 = secuencia, // Secuencia
                    S010 = "06", // Origen
                    S018 = "04", // Canal
                    S01T = "W4", // Tipo terminal
                    S025 = codigoConvenio, // Entidad, Convenio
                    S030 = this.JsonObject["tarjeta"]["numero"], // Número de la tarjeta
                    S040 = this.JsonObject["tercero"]["numero_identificacion"], // Número de identificación del tercero
                    S041 = this.obtenerTipoIdentificacion(this.JsonObject["tercero"]["tipo_identificacion"]), // Tipo de identificación
                    SX04 = 0, // Estado de la tarjeta
                    PSE4 = "181.143.185.66" // IP publica cliente
                }
            };

            String objJson = JsonConvert.SerializeObject(obj);
            XmlDocument xml = (XmlDocument)JsonConvert.DeserializeXmlNode(objJson);
            string xmlPlano = "<?xml version=\"1.0\" encoding=\"Windows-1252\" standalone=\"no\"?>";
            xmlPlano += xml.OuterXml;
            return xmlPlano;
        }

        /// <summary>
        /// Crea trama XML de creación de relación cuenta ahorro
        /// </summary>
        /// <param name="secuencia"></param>
        /// <returns></returns>
        private String XmlCrearRelacionCuentaAhorro(String secuencia)
        {
            String codigoConvenio = "00000000" + this.transaccion.Convenio.CodigoConvenio;
            codigoConvenio = codigoConvenio.Substring(codigoConvenio.Length - 8);

            String numeroTarjeta = (String)this.JsonObject["tarjeta"]["numero"];
            String hashNumeroTarjeta = this.Hash(numeroTarjeta).ToUpper(); //this.cripter.GetSHA1(numeroTarjeta);
            numeroTarjeta = "************" + numeroTarjeta.Substring(numeroTarjeta.Length - 4);

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
                    S008 = "3001", // Transacción
                    S009 = secuencia, // Secuencia
                    S010 = "06", // Origen
                    S018 = "04", // Canal
                    S01T = "W4", // Tipo terminal
                    S020 = this.JsonObject["cuenta_ahorro"]["numero_cuenta"], // Número de cuenta
                    S03A = 10, // Tipo de cuenta
                    S025 = codigoConvenio, // Entidad, Convenio
                    S030 = numeroTarjeta, // Número de la tarjeta
                    S030A = hashNumeroTarjeta, // Alias tarjeta HASH
                    S040 = this.JsonObject["tercero"]["numero_identificacion"], // Número de identificación del tercero
                    S041 = this.obtenerTipoIdentificacion(this.JsonObject["tercero"]["tipo_identificacion"]), // Tipo de identificación
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

        /// <summary>
        /// Crea trama XML de creación de relación cuenta corriente
        /// </summary>
        /// <param name="secuencia"></param>
        /// <returns></returns>
        private String XmlCrearRelacionCuentaCorriente(String secuencia)
        {
            String codigoConvenio = "00000000" + this.transaccion.Convenio.CodigoConvenio;
            codigoConvenio = codigoConvenio.Substring(codigoConvenio.Length - 8);

            String numeroTarjeta = (String)this.JsonObject["tarjeta"]["numero"];
            String hashNumeroTarjeta = this.Hash(numeroTarjeta).ToUpper();
            numeroTarjeta = "************" + numeroTarjeta.Substring(numeroTarjeta.Length - 4);

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
                    S008 = "3001", // Transacción
                    S009 = secuencia, // Secuencia
                    S010 = "06", // Origen
                    S018 = "04", // Canal
                    S01T = "W4", // Tipo terminal
                    S020 = this.JsonObject["numero_cuenta_corriente"], // Número de cuenta
                    S03A = 50, // Tipo de cuenta
                    S025 = codigoConvenio, // Entidad, Convenio
                    S030 = numeroTarjeta, // Número de la tarjeta
                    S030A = hashNumeroTarjeta, // Alias tarjeta HASH
                    S040 = this.JsonObject["tercero"]["numero_identificacion"], // Número de identificación del tercero
                    S041 = this.obtenerTipoIdentificacion(this.JsonObject["tercero"]["tipo_identificacion"]), // Tipo de identificación
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

        private String obtenerDireccion(JToken tercero)
        {
            String dir = "";
            try
            {
                var direccion = (String)tercero.SelectTokens("$.contactos[?(@.es_preferido == true)].direccion").First();
                dir = Regex.Replace(direccion, @"[^\w\s]", "").Replace("  ", "").Trim();
            }
            catch(System.InvalidOperationException e) {}
            catch (System.ArgumentNullException e) {}
            return dir;
        }

        private String obtenerTelefono(JToken tercero)
        {
            String telefono = "";
            try
            {
                var direccion = (String)tercero.SelectTokens("$.contactos[?(@.es_preferido == true)].telefono").First();
                telefono = Regex.Replace(direccion, @"[^\w]", "").Replace(" ", "").Trim();
            }
            catch (System.InvalidOperationException e) {}
            catch (System.ArgumentNullException e) {}
            return telefono;
        }

        private String obtenerCelular(JToken tercero)
        {
            String celular = "";
            try
            {
                var direccion = (String)tercero.SelectTokens("$.contactos[?(@.es_preferido == true)].movil").First();
                celular = Regex.Replace(direccion, @"[^\w]", "").Replace(" ", "").Trim();
            }
            catch (System.InvalidOperationException e) {}
            catch (System.ArgumentNullException e) {}
            return celular;
        }

        private String obtenerFecha(JToken tercero, String campo)
        {
            String fecha = "";
            try
            {
                var fechaNacimiento = (String)tercero[campo];
                fecha = Regex.Replace(fechaNacimiento, @"[^\w]", "").Replace(" ", "").Trim().Substring(0, 8);
            }
            catch (System.InvalidOperationException e) {}
            catch (System.ArgumentNullException e) {}
            return fecha;
        }

        private String obtenerSexo(JToken sexo)
        {
            String codigo = "";
            String tipo = (String)sexo["nombre"];

            switch (tipo)
            {
                case "masculino":
                    codigo = "M";
                    break;
                case "femenino":
                    codigo = "F";
                    break;
                default:
                    codigo = "M";
                    break;
            }
            return codigo;
        }

        private String obtenerEmail(JToken tercero)
        {
            String email = "";
            try
            {
                var correo = (String)tercero.SelectTokens("$.contactos[?(@.es_preferido == true)].email").First();
                email = Regex.Replace(correo, @"[^\w\@\.]", "").Replace(" ", "").Trim();
            }
            catch (System.InvalidOperationException e) {}
            catch (System.ArgumentNullException e) {}
            return email;
        }

        private String obtenerCodigoPais(JToken tercero)
        {
            String codigoPais = "";
            try
            {
                codigoPais = (String)tercero.SelectTokens("$.ciudad_nacimiento.departamento.pais.prefijo").First();
            }
            catch (System.InvalidOperationException e) { }
            catch (System.ArgumentNullException e) { }
            return codigoPais;
        }

        /// <summary>
        /// Obtiene el código del departamento
        /// </summary>
        /// <param name="tercero"></param>
        /// <returns></returns>
        private String obtenerCodigoDepartamento(JToken tercero)
        {
            String codigoDepartamento = "";
            try
            {
                codigoDepartamento = "000" + (String)tercero.SelectTokens("$.ciudad_nacimiento.departamento.codigo").First();
                codigoDepartamento = codigoDepartamento.Substring(codigoDepartamento.Length - 3);

            }
            catch (System.InvalidOperationException e) { }
            catch (System.ArgumentNullException e) { }
            return codigoDepartamento;
        }

        /// <summary>
        /// Obtiene el código de la ciudad
        /// </summary>
        /// <param name="tercero"></param>
        /// <returns></returns>
        private String obtenerCodigoCiudad(JToken tercero)
        {
            String codigoCiudad = "";
            try
            {
                codigoCiudad = "0000" + (String)tercero.SelectTokens("$.ciudad_nacimiento.codigo").First();
                codigoCiudad = codigoCiudad.Substring(codigoCiudad.Length - 4);

            }
            catch (System.InvalidOperationException e) { }
            catch (System.ArgumentNullException e) { }
            return codigoCiudad;
        }

        private String obtenerCodigoPaisContacto(JToken tercero)
        {
            String codigoPais = "";
            try
            {
                codigoPais = (String)tercero.SelectTokens("$.contactos[?(@.es_preferido == true)].ciudad.departamento.pais.prefijo").First();
            }
            catch (System.InvalidOperationException e) { }
            catch (System.ArgumentNullException e) { }
            return codigoPais;
        }

        /// <summary>
        /// Obtiene el código del departamento
        /// </summary>
        /// <param name="tercero"></param>
        /// <returns></returns>
        private String obtenerCodigoDepartamentoContacto(JToken tercero)
        {
            String codigoDepartamento = "";
            try
            {
                codigoDepartamento = "000" + (String)tercero.SelectTokens("$.contactos[?(@.es_preferido == true)].ciudad.departamento.codigo").First();
                codigoDepartamento = codigoDepartamento.Substring(codigoDepartamento.Length - 3);

            }
            catch (System.InvalidOperationException e) { }
            catch (System.ArgumentNullException e) { }
            return codigoDepartamento;
        }

        /// <summary>
        /// Obtiene el código de la ciudad
        /// </summary>
        /// <param name="tercero"></param>
        /// <returns></returns>
        private String obtenerCodigoCiudadContacto(JToken tercero)
        {
            String codigoCiudad = "";
            try
            {
                codigoCiudad = "0000" + (String)tercero.SelectTokens("$.contactos[?(@.es_preferido == true)].ciudad.codigo").First();
                codigoCiudad = codigoCiudad.Substring(codigoCiudad.Length - 4);

            }
            catch (System.InvalidOperationException e) { }
            catch (System.ArgumentNullException e) { }
            return codigoCiudad;
        }

        #endregion

        private string Hash(string input)
        {
            var hash = (new SHA1Managed()).ComputeHash(Encoding.UTF8.GetBytes(input));
            return string.Join("", hash.Select(b => b.ToString("x2")).ToArray());
        }
    }
}
