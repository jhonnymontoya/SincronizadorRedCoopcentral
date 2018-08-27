using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Com.StartLineSoft.SincronizadorRedCoopcentral
{
    class Monitor
    {
        private EntidadesDataContext edx = new EntidadesDataContext();


        public Monitor()
        {
        }

        public void Ejecutar()
        {
            var transacciones = from t in edx.Transaccions where t.Estado == "PENDIENTE" orderby t.Id ascending select t;

            foreach(Transaccion t in transacciones)
            {
                var cth = new CrearTarjetaHabiente(t);
                var estado = cth.crear();
                t.Estado = estado ? "PROCESADO" : "ERROR";
                t.UpdatedAt = DateTime.Now;
                this.edx.SubmitChanges();
            }
        }
    }
}
