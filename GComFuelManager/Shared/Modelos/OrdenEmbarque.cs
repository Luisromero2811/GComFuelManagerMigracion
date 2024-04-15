using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OfficeOpenXml.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace GComFuelManager.Shared.Modelos
{
    public class OrdenEmbarque
    {
        [Key] public int Cod { get; set; }
        public DateTime? FchOrd { get; set; }
        public DateTime? FchPro { get; set; }
        public Int16? Codtad { get; set; } = 1;
        public byte? Codprd { get; set; }

        [DisplayName("Volumen"), EpplusIgnore]
        public double? Vol { get; set; } = 0;
        [DisplayName("Volumen")]
        public string Volumenes { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", Vol); } }
        public int? Codchf { get; set; }
        public int? Coddes { get; set; }
        public byte? Codest { get; set; }
        public DateTime? Fchpet { get; set; }
        public DateTime? Fchcar { get; set; } = DateTime.Today;
        public int? Codton { get; set; }
        public int? Bin { get; set; }
        public int? Codusu { get; set; }
        public int? Folio { get; set; }
        public double? Pre { get; set; } = 0;
        public int? CodordCom { get; set; }
        public string? Bolguidid { get; set; }
        public bool? Tp { get; set; }
        public int? CompartmentId { get; set; }
        public int? Compartment { get; set; } = 1;
        public int? NumTonel { get; set; }
        [EpplusIgnore] public int? ID_Moneda { get; set; } = 0;
        public double? Equibalencia { get; set; } = 1;
        public string? FolioSyn { get; set; } = string.Empty;
        [EpplusIgnore]
        public int? Bol { get; set; }
        public byte? Estatus { get; set; }
        public int? Id_Autorizador { get; set; }
        public int? Folio_Vale { get; set; }
        public int? Id_Multidestino { get; set; }

        [NotMapped] public List<HistorialEstados> HistorialEstados { get; set; } = new();
        [NotMapped] public Destino? Destino { get; set; } = null!;
        [NotMapped] public Tad? Tad { get; set; } = null!;
        [NotMapped] public Producto? Producto { get; set; } = null!;
        [NotMapped] public Tonel? Tonel { get; set; } = null!;
        [NotMapped] public Chofer? Chofer { get; set; } = null!;
        [EpplusIgnore, NotMapped] public Moneda? Moneda { get; set; } = null!;
        [NotMapped] public OrdenCompra? OrdenCompra { get; set; } = null!;
        [NotMapped] public Estado? Estado { get; set; } = null!;
        [NotMapped] public Estado? Estatus_Orden { get; set; } = null!;
        [NotMapped] public Cliente? Cliente { get; set; } = null!;
        [NotMapped] public Usuario? Usuario { get; set; } = null!;
        [NotMapped] public Orden? Orden { get; set; } = null!;
        [NotMapped] public Transportista? Transportista { get; set; } = null!;
        [NotMapped] public OrdenCierre? OrdenCierre { get; set; } = null!;
        [NotMapped] public OrdenPedido? OrdenPedido { get; set; } = null!;
        [NotMapped] public int? Compartimento { get; set; } = null!;

        public OrdenEmbarque ShallowCopy()
        {
            return (OrdenEmbarque)this.MemberwiseClone();
        }
        public OrdenEmbarque HardCopy()
        {
            return new()
            {
                Cod = Cod,
                Codchf = Codchf,
                Coddes = Coddes,
                Codest = Codest,
                CodordCom = CodordCom,
                Codprd = Codprd,
                Codtad = Codtad,
                Codton = Codton,
                Codusu = Codusu,
                Fchcar = Fchcar,
                FchOrd = FchOrd,
                Fchpet = DateTime.Now,
                FchPro = FchPro,
                Folio = Folio,
                FolioSyn = FolioSyn,
                Bin = Bin,
                Pre = Pre,
                Bolguidid = Bolguidid,
                Compartment = Compartment,
                CompartmentId = CompartmentId,
                NumTonel = NumTonel,
                ID_Moneda = ID_Moneda,
                Equibalencia = Equibalencia,
                Bol = Bol,
                Vol = Vol,
                Estatus = Estatus
            };
        }

        [NotMapped, EpplusIgnore] public int Ordenes_A_Crear { get; set; } = 1;
        [NotMapped, EpplusIgnore] public double Costo { get; set; } = 0;
        [NotMapped, EpplusIgnore] public double Utilidad { get; set; } = 0;
        [NotMapped, EpplusIgnore] public double? Utilidad_Sobre_Volumen { get; set; } = 0;
        [NotMapped, EpplusIgnore] public bool Mostrar_Detalle_Orden { get; set; } = false;
        [NotMapped, EpplusIgnore] public List<Orden> Ordenes_Synthesis { get; set; } = new();
        [NotMapped, EpplusIgnore] public string? Terminales { get; set; }

        public double Obtener_Utilidad_Coste()
        {
            Utilidad = ((Pre ?? 0) - Costo);
            return Utilidad;
        }
        public double? Obtener_Utilidad_Sobre_Volumen()
        {
            var vol = Compartment == 1 && Tonel != null ? double.Parse(Tonel!.Capcom!.ToString() ?? "0")
                                        : Compartment == 2 && Tonel != null ? double.Parse(Tonel!.Capcom2!.ToString() ?? "0")
                                        : Compartment == 3 && Tonel != null ? double.Parse(Tonel!.Capcom3!.ToString() ?? "0")
                                        : Compartment == 4 && Tonel != null ? double.Parse(Tonel!.Capcom4!.ToString() ?? "0")
                                        : Vol ?? 0;
            Utilidad_Sobre_Volumen = vol * Utilidad;
            return Utilidad_Sobre_Volumen;
        }
        public double Obtener_Volumen_De_Orden()
        {
            try
            {
                if (Orden is not null)
                    if (Orden.Vol is not null)
                        return (double)Orden.Vol;

                if (Tonel is not null && Codest != 41)
                {
                    if (Compartment == 1) return double.Parse(Tonel.Capcom.ToString() ?? "0");
                    if (Compartment == 2) return double.Parse(Tonel.Capcom2.ToString() ?? "0");
                    if (Compartment == 3) return double.Parse(Tonel.Capcom3.ToString() ?? "0");
                    if (Compartment == 4) return double.Parse(Tonel.Capcom4.ToString() ?? "0");
                }

                if (Vol is not null)
                    return (double)Vol;

                return 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        public string Obtener_Volumen_De_Orden_En_Formato()
        {
            try
            {
                return Obtener_Volumen_De_Orden().ToString("N", CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return "0";
            }
        }
        public string Obtener_Cliente_De_Orden
        {
            get
            {
                if (Orden is not null)
                    if (Orden.Destino is not null)
                        if (Orden.Destino.Cliente is not null)
                            if (!string.IsNullOrEmpty(Orden.Destino.Cliente.Den))
                                return Orden.Destino.Cliente.Den;

                if (OrdenCierre is not null)
                    if (OrdenCierre.Cliente is not null)
                        if (!string.IsNullOrEmpty(OrdenCierre.Cliente.Den))
                            return OrdenCierre.Cliente.Den;

                return "Sin cliente asignado";
            }
        }
        public string Obtener_Destino_De_Orden
        {
            get
            {
                if (Orden is not null)
                    if (Orden.Destino is not null)
                        if (!string.IsNullOrEmpty(Orden.Destino.Den))
                            return Orden.Destino.Den;

                if (Destino is not null)
                    if (!string.IsNullOrEmpty(Destino.Den))
                        return Destino.Den;

                return "Sin destino asignado";
            }
        }
        public string Obtener_Producto_De_Orden
        {
            get
            {
                if (Orden is not null)
                    if (Orden.Producto is not null)
                        if (!string.IsNullOrEmpty(Orden.Producto.Den))
                            return Orden.Producto.Den;

                if (Producto is not null)
                    if (!string.IsNullOrEmpty(Producto.Den))
                        return Producto.Den;

                return "Sin producto asignado";
            }
        }
        public string Obtener_Estado_De_Orden
        {
            get
            {
                if (Orden is not null)
                    if (Orden.Estado is not null)
                        if (!string.IsNullOrEmpty(Orden.Estado.den))
                            return Orden.Estado.den;

                if (Estado is not null)
                    if (!string.IsNullOrEmpty(Estado.den))
                        return Estado.den;

                return "Sin estado asignado";
            }
        }
        public DateTime Obtener_Fecha_De_Carga_De_Orden
        {
            get
            {
                if (Orden is not null)
                    if (Orden.Fchcar is not null)
                        return (DateTime)Orden.Fchcar;

                if (Fchcar is not null)
                    return (DateTime)Fchcar;

                return DateTime.MinValue;
            }
        }
        public string Obtener_Tonel_De_Orden
        {
            get
            {
                if (Orden is not null)
                    if (Orden.Tonel is not null)
                        if (!string.IsNullOrEmpty(Orden.Tonel.Veh))
                            return Orden.Tonel.Veh;

                if (Tonel is not null)
                    if (!string.IsNullOrEmpty(Tonel.Veh))
                        return Tonel.Veh;

                return "Si unidad asignada";
            }
        }
    }

    public class OrdenEmbarque_Excel
    {
        [DisplayName("Fecha de carga")]
        public DateTime Fecha_Programa { get; set; } = DateTime.MinValue;
        [DisplayName("Fecha de llegada estimada")]
        public DateTime Fecha_Estimada { get; set; } = DateTime.MinValue;
        public string Producto { get; set; } = string.Empty;
        public double Volumen { get; set; } = 0;
        public string Cliente { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        //public string Tonel { get; set; } = string.Empty;
        //public int Compartimento { get; set; } = 0;
        //public string Operador { get; set; } = string.Empty;
        public string Turno { get; set; } = string.Empty;

    }
}
