using Newtonsoft.Json;
using OfficeOpenXml.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GComFuelManager.Shared.Modelos
{
    public class PrecioProgramado
    {
        [Key, EpplusIgnore] public int? Cod { get; set; }
        [EpplusIgnore] public int? CodZona { get; set; } = 0;
        [EpplusIgnore] public int? CodDes { get; set; } = 0;
        [EpplusIgnore] public int? CodCte { get; set; } = 0;
        [EpplusIgnore] public short? CodGru { get; set; } = 0;
        [EpplusIgnore] public byte? CodPrd { get; set; } = 0;
        public double Pre { get; set; } = 0;
        [EpplusIgnore] public DateTime FchActualizacion { get; set; } = DateTime.Now;
        [EpplusIgnore] public DateTime FchDia { get; set; } = DateTime.Now;
        [EpplusIgnore] public bool Activo { get; set; } = true;
        [EpplusIgnore] public int? ID_Moneda { get; set; } = 0;
        public double? Equibalencia { get; set; } = 1;
        [EpplusIgnore] public int? ID_Usuario { get; set; } = 0;
        [EpplusIgnore] public short? Id_Tad { get; set; } = 0;

        [NotMapped, EpplusIgnore] public Tad? Terminal { get; set; } = null!;
        [EpplusIgnore, NotMapped] public Moneda? Moneda { get; set; } = null!;
        [NotMapped, EpplusIgnore] public Zona? Zona { get; set; } = null!;
        [NotMapped, EpplusIgnore] public Cliente? Cliente { get; set; } = null!;
        [NotMapped, EpplusIgnore] public Producto? Producto { get; set; } = null!;
        [NotMapped, EpplusIgnore] public Destino? Destino { get; set; } = null!;
        [NotMapped, EpplusIgnore] public Usuario? Usuario { get; set; } = null!;

        public Precio ToPrecio()
        {
            try
            {
                Precio precio = new Precio();

                precio.FchDia = FchDia;
                precio.FchActualizacion = FchActualizacion;
                precio.CodPrd = CodPrd;
                precio.CodCte = CodCte;
                precio.CodGru = CodGru;
                precio.CodDes = CodDes;
                precio.CodZona = CodZona;
                precio.Cliente = Cliente;
                precio.Destino = Destino;
                precio.Zona = Zona;
                precio.Producto = Producto;
                precio.Activo = Activo;
                precio.Pre = Pre;

                return precio;
            }
            catch (Exception e)
            {
                return new Precio();
            }
        }
    }
}
