
using OfficeOpenXml.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GComFuelManager.Shared.Modelos
{
    public class Tonel
    {
        [Key] public int Cod { get; set; }
        [MaxLength(30)] public string? Placa { get; set; } = string.Empty;
        public int? Codsyn { get; set; } = 0;
        [AllowNull, DefaultValue("")] public string? Carid { get; set; } = string.Empty;
        public int? Nrocom { get; set; } = 0;
        public int? Idcom { get; set; } = 0;
        public decimal? Capcom { get; set; } = decimal.Zero;
        public int? Nrocom2 { get; set; } = 0;
        public int? Idcom2 { get; set; } = 0;
        public decimal? Capcom2 { get; set; } = decimal.Zero;
        public int? Nrocom3 { get; set; } = 0;
        public int? Idcom3 { get; set; } = 0;
        public decimal? Capcom3 { get; set; } = decimal.Zero;
        [MaxLength(20)] public string? Tracto { get; set; } = string.Empty;
        [MaxLength(20)] public string? Placatracto { get; set; } = string.Empty;
        public bool? Activo { get; set; } = true;
        public bool? Gps { get; set; } = false;
        public int? Nrocom4 { get; set; } = 0;
        public int? Idcom4 { get; set; } = 0;
        public int? Capcom4 { get; set; } = 0;
        public short? Id_Tad { get; set; }

        [NotMapped, JsonIgnore] public Tad? Tad { get; set; }
        [NotMapped, JsonIgnore] public List<Tad> Terminales { get; set; } = new();
        [NotMapped, JsonIgnore] public List<Unidad_Tad> Unidad_Tads { get; set; } = new();
        [NotMapped] public Transportista? Transportista { get; set; } = null!;

        public string Den { get { return $"{Tracto} {Placatracto} {Placa} {Capcom!} {Capcom2!} {Capcom3!} {Capcom4!} {Codsyn!}"; } }
        public string Veh { get { return $"{Tracto} {Placa}"; } }
        [NotMapped, EpplusIgnore]
        public int CapDisponible
        {
            get
            {
                int c = 0;
                if (Idcom != null && Idcom != 0 && Capcom != 0 && Capcom != null)
                    c++;
                if (Capcom2 != null && Idcom2 != 0 && Capcom2 != 0 && Capcom2 != null)
                    c++;
                if (Capcom3 != null && Idcom3 != 0 && Capcom3 != 0 && Capcom3 != null)
                    c++;
                if (Capcom4 != null && Idcom4 != 0 && Capcom4 != 0 && Capcom4 != null)
                    c++;
                return c;
            }
        }
        [NotMapped,EpplusIgnore]
        public List<CapTonel> Capacidades
        {
            get
            {
                CapTonel capTonel = new CapTonel();
                List<CapTonel> capTonels = new List<CapTonel>();

                if(Nrocom != null && Nrocom != 0)
                {
                    capTonel.CapCom = Capcom;
                    capTonel.IdCom = Idcom;
                    capTonel.NroCom = Nrocom;
                    capTonels.Add(capTonel);
                    capTonel = new CapTonel();
                }

                if (Nrocom2 != null && Nrocom2 != 0)
                {
                    capTonel.CapCom = Capcom2;
                    capTonel.IdCom = Idcom2;
                    capTonel.NroCom = Nrocom2;
                    capTonels.Add(capTonel);
                    capTonel = new CapTonel();
                }

                if (Nrocom3 != null && Nrocom3 != 0)
                {
                    capTonel.CapCom = Capcom3;
                    capTonel.IdCom = Idcom3;
                    capTonel.NroCom = Nrocom3;
                    capTonels.Add(capTonel);
                    capTonel = new CapTonel();
                }

                if (Nrocom4 != null && Nrocom4 != 0)
                {
                    capTonel.CapCom = Capcom4;
                    capTonel.IdCom = Idcom4;
                    capTonel.NroCom = Nrocom4;
                    capTonels.Add(capTonel);
                    capTonel = new CapTonel();
                }

                return capTonels;
            }
        }
    }

    public class CapTonel
    {
        [Key] public int? IdCom { get; set; } = 0;
        public decimal? CapCom { get; set; } = 0;
        public int? NroCom { get; set; } = 0;
    }
}
