using System;
using GComFuelManager.Shared.Modelos;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Net.NetworkInformation;
using System.ComponentModel.DataAnnotations;

namespace GComFuelManager.Shared.DTOs
{
    public class FolioCierreDTO
    {

        [EpplusIgnore]
        public Cliente? cliente { get; set; } = null!;
        [EpplusIgnore]
        public Destino? destino { get; set; } = null!;
        [EpplusIgnore]
        public Producto? Producto { get; set; } = null!;
        [EpplusIgnore]
        public DateTime? FchCierre { get; set; } = DateTime.MinValue;
        [EpplusIgnore]
        public DateTime? FchCierre_Vencimiento { get; set; } = DateTime.MinValue;
        [EpplusIgnore]
        public Grupo? Grupo { get; set; } = null!;
        [EpplusIgnore]
        public string? Estado { get; set; } = null!;
        [EpplusIgnore]
        public Tad? Terminal { get; set; } = null!;
        [EpplusIgnore]
        public OrdenEmbarque? ordenEmbarque { get; set; } = null!;
        [EpplusIgnore]
        public OrdenCierre? ordenCierre { get; set; } = null!;

        [DisplayName("Fecha")]
        public string FchCie { get { return string.Format(new System.Globalization.CultureInfo("es-MX"), "{0:dd/MM/yyyy}", FchCierre); } }
        [DisplayName("Folio de Cierre / OC")]
        public string? Folio { get; set; } = null!;
        [DisplayName("Unidad de Negocio")]
        public string Terminales { get; set; } = null!;
        [DisplayName("Grupo")]
        public string NGrupo { get { return Grupo != null ? Grupo.Den! : string.Empty; } }
        [DisplayName("Cliente")]
        public string NCliente { get { return cliente != null ? cliente.Den! : string.Empty; } }
        [DisplayName("Destino")]
        public string NDestino { get { return destino != null ? destino.Den! : string.Empty; } }
        [DisplayName("Producto")]
        public string NProducto { get { return Producto != null ? Producto.Den! : string.Empty; } }
        [DisplayName("Volumen Total")]
        public int? Volumen { get; set; }
        [EpplusIgnore]
        public string Volumenes { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", Volumen); } }
        //Precio
        [DisplayName("Precio")]
        public double Precio { get; set; } = 0;

        [DisplayName("Fecha de vencimiento")]
        public string FchCie_Ven { get { return string.Format(new System.Globalization.CultureInfo("es-MX"), "{0:dd/MM/yyyy}", FchCierre_Vencimiento); } }
        //public string FchCie_Ven => FchCierre_Vencimiento.ToString("dd/MM/yyyy");
        [DisplayName("Volumen Cargado")]
        public double? Volumen_Cosumido { get; set; } = 0;
        [EpplusIgnore]
        public string VolumenCargado { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", Volumen_Cosumido); } }
        [DisplayName("Volumen Programado")]
        public double? Volumen_Programado { get; set; } = 0;
        [EpplusIgnore]
        public string VolumenProgramado { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", Volumen_Programado); } }
        [DisplayName("Volumen en espera de carga")]
        public double? Volumen_Espera_Carga { get; set; } = 0;
        [EpplusIgnore]
        public string VolumenEsperaCarga { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", Volumen_Espera_Carga); } }
        [DisplayName("Volumen restante")]
        public double? Volumen_Restante { get; set; } = 0;
        [EpplusIgnore]
        public string VolumenRestante { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", Volumen_Restante); } }
        [DisplayName("Volumen Disponible")]
        public double? Volumen_Disponible { get; set; }
        [EpplusIgnore]
        public string VolumenDisponible { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", Volumen_Disponible); } }
        //Observaciones
        public string? Observaciones { get; set; } = string.Empty;

        //Estatus del cierre Activa-Cerrada
        [NotMapped, EpplusIgnore]
        public bool? Activa { get; set; } = true;
        [NotMapped, EpplusIgnore]
        public bool? Estatus { get; set; } = true;
        //Volumenes
        [DisplayName("Estado"), EpplusIgnore]
        public string? Estado_Pedido
        {
            get
            {
                return Activa == false ? "Cerrada" : ordenEmbarque?.Orden != null ? ordenEmbarque?.Orden?.Estado?.den
                : ordenEmbarque?.Estado != null ? ordenEmbarque?.Estado?.den : "Activa";
            }
        }

        [DisplayName("Estado de ODC")]
        public string Estado_Pedidos
        {
            get
            {
                if (Estatus == false)
                    return "Cancelada";

                if (Activa == false)
                    return "Cerrada";

                if (Activa != false)
                    return "Activa";

                if (ordenEmbarque is not null)
                    if (ordenEmbarque.Orden is not null)
                        if (ordenEmbarque.Orden.Estado is not null)
                            if (!string.IsNullOrEmpty(ordenEmbarque.Orden.Estado.den))
                                return ordenEmbarque.Orden.Estado.den;

                if (ordenEmbarque is not null)
                    if (ordenEmbarque.Estado is not null)
                        if (!string.IsNullOrEmpty(ordenEmbarque.Estado.den))
                            return ordenEmbarque.Estado.den;

                return "Sin estado asignado";

            }
        }

        [DisplayName("Tipo de Venta")]
        public string? Tipo_Venta { get; set; } = string.Empty;
    }
}

