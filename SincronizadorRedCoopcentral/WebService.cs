using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Com.StartLineSoft.SincronizadorRedCoopcentral
{
    class WebService
    {
        private DatoTransaccion datoTransaccion { get; set; }
        private WSRedCoopcentral.IncomingWebServiceClient cliente = null;
        private WSRedCoopcentral.TECGIncomingRequest solicitud;
        private WSRedCoopcentral.TECGIncomingResponse respuesta;
        private String convenio { get; set; }

        public WebService(DatoTransaccion datoTransaccion)
        {
            this.datoTransaccion = datoTransaccion;
            String url = (String)this.datoTransaccion.Transaccion.Convenio.Uri;

            Binding binding = new BasicHttpsBinding();
            EndpointAddress endpointAddress = new EndpointAddress(url);

            this.cliente = new WSRedCoopcentral.IncomingWebServiceClient(binding, endpointAddress);

            this.solicitud = new WSRedCoopcentral.TECGIncomingRequest();

            String codigoConvenio = "00000000" + this.datoTransaccion.Transaccion.Convenio.CodigoConvenio;
            this.convenio = codigoConvenio.Substring(codigoConvenio.Length - 8);
        }

        public String ejecutar()
        {
            this.solicitud.Input = this.datoTransaccion.XmlEnvioEncriptado;
            this.solicitud.InputHash = this.datoTransaccion.HashXml;
            this.solicitud.InputIdentify = this.convenio;
            this.solicitud.InputSecuence = this.datoTransaccion.Numero;

            this.respuesta = this.cliente.Request(this.solicitud);

            if(this.respuesta.Error != 0)
            {
                throw new Exception(this.respuesta.ErrorMsg);
            }

            return this.respuesta.Output;
        }
    }
}
