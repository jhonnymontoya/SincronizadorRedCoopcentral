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
            try
            {
                //Crear tarjeta habientes
                //CREARTARJETAHABIENTE
                var transacciones = from t
                                    in edx.Transaccions
                                    where t.Estado == "PENDIENTE" && t.TipoTransaccion == "CREARTARJETAHABIENTE"
                                    orderby t.Id ascending
                                    select t;

                foreach (Transaccion t in transacciones)
                {
                    t.Estado = "ENPROCESO";
                    t.UpdatedAt = DateTime.Now;
                    this.edx.SubmitChanges();
                    var cth = new CrearTarjetaHabiente(t);
                    var estado = cth.Crear();
                    t.Estado = estado ? "PROCESADO" : "ERROR";
                    t.UpdatedAt = DateTime.Now;
                    this.edx.SubmitChanges();
                }

                //modificar cuentas corrientes
                //MODIFICARCUENTACORRIENTE
                transacciones = from t
                                    in edx.Transaccions
                                where t.Estado == "PENDIENTE" && t.TipoTransaccion == "MODIFICARCUENTACORRIENTE"
                                orderby t.Id ascending
                                select t;

                foreach (Transaccion t in transacciones)
                {
                    var cth = new ModificarCuentaCorriente(t);
                    var estado = cth.Crear();
                    t.Estado = estado ? "PROCESADO" : "ERROR";
                    t.UpdatedAt = DateTime.Now;
                    this.edx.SubmitChanges();
                }
            } catch(Exception e)
            {
            }            
        }
    }
}
