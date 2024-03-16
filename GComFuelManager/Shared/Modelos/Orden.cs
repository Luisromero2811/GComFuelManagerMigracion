using Newtonsoft.Json;
using OfficeOpenXml.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GComFuelManager.Shared.Modelos
{
    public class Orden
    {
        [Key] public Int64? Cod { get; set; } = null!;
        public DateTime? Fch { get; set; } = null!;
        [MaxLength(32)] public string? Ref { get; set; } = string.Empty;
        public int? Coddes { get; set; } = 0;
        public byte? Codprd { get; set; } = 0;
        [EpplusIgnore] public double? Vol { get; set; } = null!;
        public DateTime? Fchcar { get; set; } = DateTime.MinValue;
        public byte? Codest { get; set; } = 0;
        public int? Coduni { get; set; } = 0;
        public int? Codchf { get; set; } = 0;
        [MaxLength(256)] public string? Bolguiid { get; set; } = string.Empty;
        public Int64? Liniteid { get; set; } = null!;
        public int? Codprd2 { get; set; } = 0;
        [MaxLength(256)] public string? Dendes { get; set; } = string.Empty;
        [EpplusIgnore] public double? Vol2 { get; set; } = null!;
        [NotMapped] public string? Volumenes { get { return Vol2 != null ? Vol2?.ToString("N2") : string.Empty; } }
        public int? BatchId { get; set; }
        public int? CompartmentId { get; set; } = null!;
        [MaxLength(128)] public string? SealNumber { get; set; } = string.Empty;
        [NotMapped] public long? Codprdsyn { get; set; } = 0;
        [NotMapped] public long? Codprd2syn { get; set; } = 0;
        [NotMapped] public long? Codchfsyn { get; set; } = 0;
        [EpplusIgnore] public short? Id_Tad { get; set; } = 0;
        public int? Folio { get; set; } = 0;

        [DisplayName("Unidad de Negocio"), NotMapped]
        public string? Unidad_Negocio { get; set; } = string.Empty;

        //Prop de nav Estado
        [NotMapped] public Tad? Terminal { get; set; } = null!;
        [NotMapped] public Estado? Estado { get; set; } = null!;
        [NotMapped] public Destino? Destino { get; set; } = null!;
        [NotMapped] public Producto? Producto { get; set; } = null!;
        [NotMapped] public Transportista? Transportista { get; set; } = null!;
        [NotMapped] public Tonel? Tonel { get; set; } = null!;
        [NotMapped] public Chofer? Chofer { get; set; } = null!;
        [NotMapped] public OrdEmbDet? OrdEmbDet { get; set; } = null!;
        [NotMapped] public OrdenEmbarque? OrdenEmbarque { get; set; } = null!;
        [NotMapped] public int? Compartimento { get; set; } = null!;
        [NotMapped] public OrdenCierre? OrdenCierre { get; set; } = null!;
        [NotMapped] public Redireccionamiento? Redireccionamiento { get; set; } = null!;

        [NotMapped]
        public string Obtener_Cliente
        {
            get
            {
                if (Redireccionamiento is not null)
                    return Redireccionamiento.Nombre_Cliente;

                if (Destino is not null)
                    if (Destino.Cliente is not null)
                        if (!string.IsNullOrEmpty(Destino.Cliente.Den))
                            return Destino.Cliente.Den;

                return string.Empty;
            }
        }
        [NotMapped]
        public string Obtener_Cliente_De_Orden
        {
            get
            {
                if (Destino is not null)
                    if (Destino.Cliente is not null)
                        if (!string.IsNullOrEmpty(Destino.Cliente.Den))
                            return Destino.Cliente.Den;

                return string.Empty;
            }
        }
        [NotMapped]
        public string Obtener_Destino
        {
            get
            {
                if (Redireccionamiento is not null)
                    return Redireccionamiento.Nombre_Destino;

                if (Destino is not null)
                    if (!string.IsNullOrEmpty(Destino.Den))
                        return Destino.Den;

                return string.Empty;
            }
        }
        [NotMapped]
        public string Obtener_Destino_De_Orden
        {
            get
            {
                if (Destino is not null)
                    if (!string.IsNullOrEmpty(Destino.Den))
                        return Destino.Den;

                return string.Empty;
            }
        }

        [NotMapped]
        public double Obtener_Precio
        {
            get
            {
                if (Redireccionamiento is not null)
                    return Redireccionamiento.Precio_Red;

                if (OrdenEmbarque is not null)
                    if (OrdenEmbarque.Pre is not null)
                        return (double)OrdenEmbarque.Pre;

                return 0;
            }
        }

        [NotMapped]
        public double Obtener_Precio_Original
        {
            get
            {
                if (OrdenEmbarque is not null)
                    if (OrdenEmbarque.Pre is not null)
                        return (double)OrdenEmbarque.Pre;

                return 0;
            }
        }
        [NotMapped]
        public double Obtener_Precio_Orden_Embarque
        {
            get
            {
                if (OrdenEmbarque is not null)
                    if (OrdenEmbarque.Pre is not null)
                        return (double)OrdenEmbarque.Pre;
                return 0;
            }
        }
        [NotMapped]
        public string Obtener_Nombre_Producto
        {
            get
            {
                if (Producto is not null)
                    if (!string.IsNullOrEmpty(Producto.Den))
                        return Producto.Den;
                return string.Empty;
            }
        }

        [NotMapped]
        public double Obtener_Volumen
        {
            get
            {
                if (Vol is not null)
                    return (double)Vol;
                return 0;
            }
        }
    }
}
