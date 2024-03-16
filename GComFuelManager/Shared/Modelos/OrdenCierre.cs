using GComFuelManager.Shared.DTOs;
using Newtonsoft.Json;
using OfficeOpenXml.Attributes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace GComFuelManager.Shared.Modelos
{
    public class OrdenCierre
    {
        [Key, EpplusIgnore] public int Cod { get; set; }

        [EpplusIgnore] public DateTime? FchCierre { get; set; } = DateTime.Today;
        [EpplusIgnore] public DateTime? FchVencimiento { get; set; } = DateTime.Today;

        [DisplayName("Fecha de cierre"), NotMapped] public string? Fch { get { return FchCierre!.Value.ToString("dd/MM/yyyy"); } }

        [DisplayName("BOL")]
        public string? BOL { get { return OrdenEmbarque is not null ? OrdenEmbarque.Orden is not null ? OrdenEmbarque.Orden.BatchId.ToString() : string.Empty : string.Empty; } }

        [DisplayName("Fecha de vencimiento"), NotMapped]
        public string? FchVen { get { return FchVencimiento?.ToString("dd/MM/yyyy"); } }

        [JsonPropertyName("folio"), DisplayName("Folio")]
        public string? Folio { get; set; } = string.Empty;

        [NotMapped, DisplayName("Unidad de Negocio")]
        public string? Unidad_Negocio { get { return Terminal != null ? Terminal.Den : string.Empty; } }

        [EpplusIgnore]
        public string? Contacto { get { return ContactoN != null ? ContactoN.Nombre : string.Empty; } }

        [EpplusIgnore]
        public string? Email { get { return ContactoN != null ? ContactoN.Correo : string.Empty; } }

        [EpplusIgnore]
        public byte? CodPrd { get; set; }

        [DisplayName("producto")]
        public string Pro { get { return Producto != null && !string.IsNullOrEmpty(Producto.Den) ? Producto.Den : "Sin producto asignado"; } }

        [EpplusIgnore]
        public int? CodCte { get; set; }

        [NotMapped, EpplusIgnore]
        public Cliente? Cliente { get; set; } = null!;

        [EpplusIgnore]
        public string? ModeloVenta { get; set; } = string.Empty;

        [DisplayName("Tipo de Venta")]
        public string? TipoVenta { get; set; } = string.Empty;

        [DisplayName("Tipo de Pago")]
        public string? TipoPago { get; set; } = "Credito";

        [DisplayName("Precio"), DisplayFormat(DataFormatString = "{0:C}")]
        public double Precio { get; set; } = 0;

        //[JsonPropertyName("temperatura"), DisplayName("Temperatura")]
        //[NotMapped] public double? Temperatura { get; set; } = null!;

        [DisplayName("Vendedor")]
        public string? Vendedor { get; set; } = string.Empty;

        [EpplusIgnore]
        public int? CodDes { get; set; }

        [NotMapped, EpplusIgnore]
        public Destino? Destino { get; set; } = null!;

        [DisplayName("destino")]
        public string? Des { get { return Destino != null ? Destino.Den : "Sin destino asignado"; } }

        [DisplayName("cliente")]
        public string? Cli { get { return Cliente != null ? Cliente.Den : "Sin cliente asignado"; } }

        [EpplusIgnore]
        public int? Volumen { get; set; }
        [DisplayName("Volumen"), NotMapped]
        public string Volumenes { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", Volumen); } }

        [DisplayName("Observaciones")]
        public string? Observaciones { get; set; } = string.Empty;

        [EpplusIgnore]
        public bool? Estatus { get; set; } = true;

        [EpplusIgnore]
        public bool? Activa { get; set; } = true;

        [EpplusIgnore]
        public bool? Confirmada { get; set; } = true;

        [EpplusIgnore]
        public int? CodCon { get; set; }

        [EpplusIgnore]
        public int? CodPed { get; set; } = 0;

        [EpplusIgnore] public DateTime? FchLlegada { get; set; } = DateTime.Now;

        [DisplayName("Fecha de llegada"), NotMapped]
        public string? FechaLlegada { get { return FchLlegada!.Value.ToString("D"); } }

        [DisplayName("Turno")]
        public string? Turno { get; set; } = string.Empty;

        [EpplusIgnore]
        public Int16? CodGru { get; set; }

        [EpplusIgnore, NotMapped] public DateTime? FchCar { get; set; } = DateTime.Today;
        [EpplusIgnore, NotMapped] public short? Id_Tad { get; set; } = 0;
        [EpplusIgnore] public int? ID_Moneda { get; set; } = 0;
        [EpplusIgnore] public double? Equibalencia { get; set; } = 1;

        [EpplusIgnore, NotMapped] public bool IsCierreVolumen { get; set; } = true;
        [EpplusIgnore, NotMapped] public bool IsDifferentVol { get; set; } = false;
        [EpplusIgnore, NotMapped] public bool IsDifferentTurn { get; set; } = false;
        [EpplusIgnore, NotMapped] public bool IsAutoPrecio { get; set; } = true;
        [EpplusIgnore, NotMapped] public bool isGroup { get; set; } = false;
        [EpplusIgnore, NotMapped] public bool PrecioOverDate { get; set; }
        [EpplusIgnore] public DateTime? fchPrecio { get; set; } = DateTime.Now;
        [DisplayName("Fecha de Precio"), NotMapped]
        public string? FchPre { get { return fchPrecio?.ToString("dd/MM/yyyy"); } }
        [DisplayName("Estado"), NotMapped]
        public string Estado_Pedido
        {
            get
            {
                if (Estatus == false)
                    return "Cancelada";

                if (Activa == false)
                    return "Cerrada";

                if (Activa != false)
                    return "Activa";

                if (OrdenEmbarque is not null)
                    if (OrdenEmbarque.Orden is not null)
                        if (OrdenEmbarque.Orden.Estado is not null)
                            if (!string.IsNullOrEmpty(OrdenEmbarque.Orden.Estado.den))
                                return OrdenEmbarque.Orden.Estado.den;

                if (OrdenEmbarque is not null)
                    if (OrdenEmbarque.Estado is not null)
                        if (!string.IsNullOrEmpty(OrdenEmbarque.Estado.den))
                            return OrdenEmbarque.Estado.den;

                return "Sin estado asignado";

            }
        }
        [NotMapped, EpplusIgnore] public int? Cantidad_Sugerida { get; set; } = 0;
        [NotMapped, EpplusIgnore] public int? Cantidad_Confirmada { get; set; } = 0;
        [NotMapped, EpplusIgnore] public int? Volumen_Seleccionado { get; set; } = 62000;
        [NotMapped, EpplusIgnore] public int? Volumen_Por_Unidad { get { return Volumen_Seleccionado >= 62000 ? Volumen_Seleccionado / 2 : Volumen_Seleccionado; } }
        [EpplusIgnore] public bool Precio_Manual { get; set; } = true;
        [NotMapped, EpplusIgnore] public int? Ordenes_Relacionadas { get; set; } = 0;
        [NotMapped, EpplusIgnore] public string? Folio_Perteneciente { get; set; } = string.Empty;

        [EpplusIgnore, NotMapped] public Grupo? Grupo { get; set; } = null!;
        [EpplusIgnore, NotMapped] public OrdenPedido? ordenPedido { get; set; } = null!;
        [NotMapped, EpplusIgnore] public Producto? Producto { get; set; } = null!;
        [EpplusIgnore, NotMapped] public VolumenDisponibleDTO? VolumenDisponible { get; set; } = new VolumenDisponibleDTO();
        [NotMapped, EpplusIgnore] public List<OrdenPedido> OrdenPedidos { get; set; } = new List<OrdenPedido>();
        [EpplusIgnore, NotMapped] public Moneda? Moneda { get; set; } = null!;
        [EpplusIgnore, NotMapped] public OrdenEmbarque? OrdenEmbarque { get; set; } = null!;
        [NotMapped, EpplusIgnore] public Contacto? ContactoN { get; set; } = null!;
        [NotMapped, EpplusIgnore] public Tad? Terminal { get; set; } = null!;

        public OrdenCierre ShallowCopy()
        {
            return (OrdenCierre)this.MemberwiseClone();
        }
        public OrdenCierre HardCopy()
        {
            return new()
            {
                Cod = Cod,
                CodCte = CodCte,
                CodCon = CodCon,
                CodDes = CodDes,
                CodGru = CodGru,
                CodPed = CodPed,
                CodPrd = CodPrd,
                Id_Tad = Id_Tad,
                fchPrecio = fchPrecio,
                FchCar = FchCar,
                FchCierre = FchCierre,
                FchLlegada = FchLlegada,
                FchVencimiento = FchVencimiento,
                Volumen = Volumen,
                Precio = Precio,
                Folio = Folio,
                Turno = Turno,
                ModeloVenta = ModeloVenta,
                TipoPago = TipoPago,
                TipoVenta = TipoVenta,
                Estatus = Estatus,
                Activa = Activa,
                Confirmada = Confirmada,
                Observaciones = Observaciones,
                ID_Moneda = ID_Moneda,
                Equibalencia = Equibalencia,
                Vendedor = Vendedor
            };
        }
        public int? GetCantidadSugerida()
        {
            try
            {
                Cantidad_Sugerida = Convert.ToInt32(Volumen_Disponible) / Volumen_Por_Unidad;
                Cantidad_Sugerida = Cantidad_Sugerida % 2 == 0 ? Cantidad_Sugerida : Cantidad_Sugerida - 1 == 0 ? 1 : Cantidad_Sugerida - 1;
                return Cantidad_Sugerida;
            }
            catch (DivideByZeroException e)
            {
                return 0;
            }
        }

        #region Calculo de volumenes
        [NotMapped, EpplusIgnore] public double? Volumen_Solicitado { get; set; } = 0;
        public double? GetVolumenSolicitado()
        {
            try
            {
                if (OrdenPedidos is not null && CodPed == 0 && OrdenPedidos.Count > 0)
                {
                    Volumen_Solicitado = OrdenPedidos.Where(x => x.OrdenEmbarque != null
                        && x.OrdenEmbarque.Codprd == CodPrd
                        && x.OrdenEmbarque.Codest == 9
                        && string.IsNullOrEmpty(x.OrdenEmbarque.Bolguidid)
                        && x.OrdenEmbarque.FchOrd is null).Sum(x => x.OrdenEmbarque?.Vol);
                }
                return Volumen_Solicitado;
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        [NotMapped, EpplusIgnore] public double? Cantidad_De_Solicitado { get; set; } = 0;
        public double? GetCantidadSolicitado()
        {
            try
            {
                if (OrdenPedidos is not null && CodPed == 0 && OrdenPedidos.Count > 0)
                {
                    Cantidad_De_Solicitado = OrdenPedidos.Where(x => x.OrdenEmbarque != null
                        && x.OrdenEmbarque.Codprd == CodPrd
                        && x.OrdenEmbarque.Codest == 9
                        && string.IsNullOrEmpty(x.OrdenEmbarque.Bolguidid)
                        && x.OrdenEmbarque.FchOrd is null).Count();
                }
                return Cantidad_De_Solicitado;
            }
            catch (Exception e)
            {
                return 0;
            }
        }
        //public string Volumenes { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", Volumen); } }
        [NotMapped, EpplusIgnore] public double? Volumen_Programado { get; set; } = 0;
        [NotMapped, EpplusIgnore]
        public string VolumenProgramadoFormateado { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", Volumen_Programado); } }
        public double? GetVolumenProgramado()
        {
            try
            {
                if (OrdenPedidos is not null && CodPed == 0 && OrdenPedidos.Count > 0)
                {
                    Volumen_Programado = OrdenPedidos.Where(x => x.OrdenEmbarque != null
                    && x.OrdenEmbarque.Folio is null
                    && x.OrdenEmbarque.Codprd == CodPrd
                    && x.OrdenEmbarque.Codest == 3
                    && x.OrdenEmbarque.FchOrd is not null
                    && string.IsNullOrEmpty(x.OrdenEmbarque.Bolguidid))
                        .Sum(x => x.OrdenEmbarque?.Vol);
                }
                return Volumen_Programado;
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        [NotMapped, EpplusIgnore] public double? Cantidad_De_Programado { get; set; } = 0;
        public double? GetCantidadProgramado()
        {
            try
            {
                if (OrdenPedidos is not null && CodPed == 0 && OrdenPedidos.Count > 0)
                {
                    Cantidad_De_Programado = OrdenPedidos.Where(x => x.OrdenEmbarque != null
                    && x.OrdenEmbarque.Folio is null
                    && x.OrdenEmbarque.Codprd == CodPrd
                    && x.OrdenEmbarque.Codest == 3
                    && x.OrdenEmbarque.FchOrd is not null
                    && string.IsNullOrEmpty(x.OrdenEmbarque.Bolguidid)).Count();
                }
                return Cantidad_De_Programado;
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        [NotMapped, EpplusIgnore] public double? Volumen_Cosumido { get; set; } = 0;
        [NotMapped, EpplusIgnore]
        public string ConsumidoFormateado { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", Volumen_Cosumido); } }
        public double? GetVolumenConsumido()
        {
            try
            {
                if (OrdenPedidos is not null && CodPed == 0 && OrdenPedidos.Count > 0)
                {
                    Volumen_Cosumido = OrdenPedidos.Where(x => x.OrdenEmbarque != null
                    && x.OrdenEmbarque.Folio is not null && x.OrdenEmbarque?.Orden?.Codprd == CodPrd
                    && x.OrdenEmbarque?.Codest != 14 && x.OrdenEmbarque?.Orden?.Codest != 14
                    && x.OrdenEmbarque?.Orden?.BatchId is not null)
                    .Sum(x => x.OrdenEmbarque?.Orden?.Vol);
                }
                return Volumen_Cosumido;
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        [NotMapped, EpplusIgnore] public double? Cantidad_De_Cosumido { get; set; } = 0;
        public double? GetCantidadConsumido()
        {
            try
            {
                if (OrdenPedidos is not null && CodPed == 0 && OrdenPedidos.Count > 0)
                {
                    Cantidad_De_Cosumido = OrdenPedidos.Where(x => x.OrdenEmbarque != null
                    && x.OrdenEmbarque.Folio is not null && x.OrdenEmbarque?.Orden?.Codprd == CodPrd
                    && x.OrdenEmbarque?.Codest != 14 && x.OrdenEmbarque?.Orden?.Codest != 14
                    && x.OrdenEmbarque?.Orden?.BatchId is not null).Count();
                }
                return Cantidad_De_Cosumido;
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        [NotMapped, EpplusIgnore] public double? Volumen_Espera_Carga { get; set; } = 0;
        [NotMapped, EpplusIgnore] public string VolumenEsperaFormateada { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", Volumen_Espera_Carga); } }
        public double? GetVolumenEsperaCarga()
        {
            try
            {
                if (OrdenPedidos is not null && CodPed == 0 && OrdenPedidos.Count > 0)
                {
                    Volumen_Espera_Carga = OrdenPedidos.Where(x => x.OrdenEmbarque != null
                    && x.OrdenEmbarque?.Codprd == CodPrd && x.OrdenEmbarque?.Codest == 22
                    && x.OrdenEmbarque?.Folio is not null && x.OrdenEmbarque?.Orden is null)
                        .Sum(x => x.OrdenEmbarque?.Compartment == 1 && x.OrdenEmbarque?.Tonel is not null ? double.Parse(x?.OrdenEmbarque?.Tonel?.Capcom?.ToString() ?? "0")
                        : x.OrdenEmbarque?.Compartment == 2 && x.OrdenEmbarque?.Tonel is not null ? double.Parse(x?.OrdenEmbarque?.Tonel?.Capcom2?.ToString() ?? "0")
                        : x.OrdenEmbarque?.Compartment == 3 && x.OrdenEmbarque?.Tonel is not null ? double.Parse(x?.OrdenEmbarque?.Tonel?.Capcom3?.ToString() ?? "0")
                        : x.OrdenEmbarque?.Compartment == 4 && x.OrdenEmbarque?.Tonel is not null ? double.Parse(x?.OrdenEmbarque?.Tonel?.Capcom4?.ToString() ?? "0")
                        : x.OrdenEmbarque?.Vol);
                }
                return Volumen_Espera_Carga;
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        [NotMapped, EpplusIgnore] public double? Cantidad_De_Espera_Carga { get; set; } = 0;
        public double? GetCantidadEsperaCarga()
        {
            try
            {
                if (OrdenPedidos is not null && CodPed == 0 && OrdenPedidos.Count > 0)
                {
                    Cantidad_De_Espera_Carga = OrdenPedidos.Where(x => x.OrdenEmbarque != null
                    && x.OrdenEmbarque?.Codprd == CodPrd && x.OrdenEmbarque?.Codest == 22
                    && x.OrdenEmbarque?.Folio is not null && x.OrdenEmbarque?.Orden is null).Count();
                }
                return Cantidad_De_Espera_Carga;
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        [NotMapped, EpplusIgnore] public double? Volumen_Disponible { get; set; } = 0;
        [NotMapped, EpplusIgnore] public string DisponibleFormateado { get { return string.Format(new System.Globalization.CultureInfo("en-US"), "{0:N2}", Volumen_Disponible); } }
        public double? GetVolumenDisponible()
        {
            try
            {
                Volumen_Disponible = Volumen - (Volumen_Solicitado + Volumen_Espera_Carga + Volumen_Programado + Volumen_Cosumido);
                return Volumen_Disponible;
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        public void SetVolumen()
        {
            try
            {
                Volumen_Solicitado = GetVolumenSolicitado();
                Volumen_Programado = GetVolumenProgramado();
                Volumen_Espera_Carga = GetVolumenEsperaCarga();
                Volumen_Cosumido = GetVolumenConsumido();
                Volumen_Disponible = GetVolumenDisponible();
            }
            catch (Exception e)
            {

                throw;
            }
        }
        [NotMapped, EpplusIgnore] public bool Tiene_Volumen_Disponible { get; set; } = true;
        public bool GetTieneVolumenDisponible(Porcentaje porcentaje)
        {
            try
            {
                SetVolumen();
                Tiene_Volumen_Disponible = !(GetPromedioCarga() >= (Volumen_Disponible * (porcentaje.Porcen != 0 ? porcentaje.Porcen : 100) / 100));
                return Tiene_Volumen_Disponible;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool GetPuedeCrearOrden()
        {
            try
            {
                SetVolumen();
                return (Volumen_Disponible < (Volumen_Por_Unidad * Cantidad_Confirmada));
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public void SetCantidades()
        {
            Cantidad_De_Cosumido = GetCantidadConsumido();
            Cantidad_De_Espera_Carga = GetCantidadEsperaCarga();
            Cantidad_De_Programado = GetCantidadProgramado();
            Cantidad_De_Solicitado = GetCantidadSolicitado();
        }

        [NotMapped, EpplusIgnore]
        public double? Promedio_Carga { get; set; } = 0;
        public double? GetPromedioCarga()
        {
            try
            {
                SetVolumen();
                SetCantidades();
                var totalvolumen = Volumen_Cosumido + Volumen_Espera_Carga + Volumen_Programado + Volumen_Solicitado;
                var totalcantidad = Cantidad_De_Cosumido + Cantidad_De_Espera_Carga + Cantidad_De_Programado + Cantidad_De_Solicitado;

                if (totalcantidad > 0 && totalvolumen > 0)
                    Promedio_Carga = totalvolumen / totalcantidad;

                return Promedio_Carga;
            }
            catch (DivideByZeroException e)
            {
                return 0;
            }
        }
        #endregion
    }
}
