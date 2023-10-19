using System;
using GComFuelManager.Shared.Modelos;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using GComFuelManager.Server.Identity;
using GComFuelManager.Shared.DTOs;

namespace GComFuelManager.Server
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUsuario>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public virtual async Task<int> SaveChangesAsync(string? id, int? accion)
        {
            if (!string.IsNullOrEmpty(id) && accion != 0)
                OnBeforeSaveChanges(id, accion);
            var Results = await base.SaveChangesAsync();
            return Results;
        }

        private void OnBeforeSaveChanges(string? id, int? accion)
        {
            ChangeTracker.DetectChanges();
            var AccEntries = new List<ActividadDTO>();
            foreach (var item in ChangeTracker.Entries())
            {
                if (item.Entity is ActividadRegistrada || item.State == EntityState.Detached || item.State == EntityState.Unchanged)
                    continue;

                var actividad = new ActividadDTO(item);

                actividad.TableName = item.Entity.GetType().Name; //obtiene el nombre de la tabla
                actividad.UserId = id;
                actividad.AuditType = accion;

                AccEntries.Add(actividad);

                foreach (var prop in item.Properties)
                {
                    string propName = prop.Metadata.Name;

                    //si la propiedad actual es una clave principal, agréguela al diccionario de claves principales y sáltela.
                    if (prop.Metadata.IsPrimaryKey())
                    {
                        actividad.KeyValues[propName] = prop.CurrentValue!;
                    }

                    //en el switch detectamos el estado de la entidad (Agregado, Eliminado o Modificado)
                    //y por cada caso agregamos nuevos datos a cada campo de la tabla auditoria
                    switch (item.State)
                    {
                        case EntityState.Deleted:
                            actividad.OldValues[propName] = prop.OriginalValue!;
                            break;
                        case EntityState.Modified:
                            if (prop.IsModified)
                            {
                                actividad.ChangedColumns.Add(propName);
                                actividad.OldValues[propName] = prop.OriginalValue!;
                                actividad.NewValues[propName] = prop.CurrentValue!;
                            }
                            break;
                        case EntityState.Added:
                            actividad.NewValues[propName] = prop.CurrentValue!;
                            break;
                        default:
                            break;
                    }
                }
            }
            //convertimos todas las Entradas de Auditoría a Auditorías y guardamos los cambios en el metodo original: var result = await base.SaveChangesAsync();
            foreach (var item in AccEntries)
                ActividadRegistrada.Add(item.ToAudit());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Configuracion de la entidad a través de Modelbuilder.Entity
            //Con esta entidad podemos aplicar la llave compartida para generar un campo de muchos a muchos

            //Relaciones en su mayoria de tabla OrdenEmbarques
            //Estacion
            modelBuilder.Entity<OrdenEmbarque>()
                    .HasOne(x => x.Destino)
                    .WithMany()
                    .HasForeignKey(x => x.Coddes);
            //Destino
            modelBuilder.Entity<Destino>()
                .HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.Codcte);
            //OrdenCierre

            //modelBuilder.Entity<OrdenEmbarque>()
            //    .HasOne(x => x.OrdenCierre)
            //    .WithOne(x => x.OrdenEmbarque)
            //    .HasForeignKey<OrdenCierre>(x => x.CodPed);

            //Terminal
            modelBuilder.Entity<OrdenEmbarque>()
                .HasOne(x => x.Tad)
                .WithMany()
                .HasForeignKey(x => x.Codtad);
            //Producto
            modelBuilder.Entity<OrdenEmbarque>()
                .HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.Codprd);
            //Cantidad
            modelBuilder.Entity<OrdenEmbarque>()
                .HasOne(x => x.Tonel)
                .WithMany()
                .HasForeignKey(x => x.Codton);


            //TransportistaOrdenEmbarque
            modelBuilder.Entity<OrdenEmbarque>()
                .HasOne(x => x.Chofer)
                .WithMany()
                .HasForeignKey(x => x.Codchf);
            //Tonel
            modelBuilder.Entity<Tonel>()
                .HasOne(x => x.Transportista)
                .WithMany()
                .HasPrincipalKey(x => x.CarrId)
                .IsRequired(false)
                .HasForeignKey(x => x.Carid)
                .IsRequired(false);
            //Orden compra
            modelBuilder.Entity<OrdenEmbarque>()
                .HasOne(x => x.OrdenCompra)
                .WithMany()
                .HasForeignKey(x => x.CodordCom);
            //cliente - destino
            modelBuilder.Entity<Destino>()
                .HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.Codcte);
            //Estado
            modelBuilder.Entity<OrdenEmbarque>()
                .HasOne(x => x.Estado)
                .WithMany()
                .HasForeignKey(x => x.Codest);
            //Relaciones Tabla de OrdEmbDet
            modelBuilder.Entity<OrdEmbDet>()
                .HasOne(x => x.Orden)
                .WithMany()
                .HasPrincipalKey(x => x.BatchId)
                .HasForeignKey(x => x.Bol);
            //Relaciones Tabla orden
            //Orden-OrdEmbDet
            modelBuilder.Entity<Orden>()
                .HasOne(x => x.OrdEmbDet)
                .WithMany()
                .HasPrincipalKey(x => x.Bol)
                .HasForeignKey(x => x.BatchId);
            //Orden-Estado 
            modelBuilder.Entity<Orden>()
                .HasOne(x => x.Estado)
                .WithMany()
                .HasForeignKey(x => x.Codest);
            //Orden-Estacion(Destino)
            modelBuilder.Entity<Orden>()
                .HasOne(x => x.Destino)
                .WithMany()
                .HasForeignKey(x => x.Coddes);
            //Orden-Producto
            modelBuilder.Entity<Orden>()
                .HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.Codprd);
            //Orden-Tonel
            modelBuilder.Entity<Orden>()
                .HasOne(x => x.Tonel)
                .WithMany()
                .HasForeignKey(x => x.Coduni);
            //Orden-Chofer
            modelBuilder.Entity<Orden>()
                .HasOne(x => x.Chofer)
                .WithMany()
                .HasForeignKey(x => x.Codchf);
            //OrdenCierre-Destino
            modelBuilder.Entity<OrdenCierre>()
                .HasOne(x => x.Destino)
                .WithMany()
                .HasForeignKey(x => x.CodDes);
            //OrdenCierre-Cliente
            modelBuilder.Entity<OrdenCierre>()
                .HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.CodCte);
            //OrdenCierre-Producto
            modelBuilder.Entity<OrdenCierre>()
                .HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.CodPrd);

            //Contacto - Cliente
            modelBuilder.Entity<Contacto>()
                .HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.CodCte);

            //Contacto - Cliente
            modelBuilder.Entity<OrdenCierre>()
                .HasOne(x => x.ContactoN)
                .WithMany()
                .HasForeignKey(x => x.CodCon);

            //OrdenEmbarque - Cierre
            modelBuilder.Entity<OrdenCierre>()
                .HasOne(x => x.OrdenEmbarque)
                .WithMany()
                .HasForeignKey(x => x.CodPed);

            //Precio - Zona
            modelBuilder.Entity<Precio>()
                .HasOne(x => x.Zona)
                .WithMany()
                .HasForeignKey(x => x.codZona);
            //Precio - cliente
            modelBuilder.Entity<Precio>()
                .HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.codCte);
            //Precio - Producto
            modelBuilder.Entity<Precio>()
                .HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.codPrd);
            //Precio - Destino
            modelBuilder.Entity<Precio>()
                .HasOne(x => x.Destino)
                .WithMany()
                .HasForeignKey(x => x.codDes);

            //PrecioHistorico - Zona
            modelBuilder.Entity<PrecioHistorico>()
                .HasOne(x => x.Zona)
                .WithMany()
                .HasForeignKey(x => x.codZona);
            //PrecioHistorico - cliente
            modelBuilder.Entity<PrecioHistorico>()
                .HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.codCte);
            //PrecioHistorico - Producto
            modelBuilder.Entity<PrecioHistorico>()
                .HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.codPrd);
            //PrecioHistorico - Destino
            modelBuilder.Entity<PrecioHistorico>()
                .HasOne(x => x.Destino)
                .WithMany()
                .HasForeignKey(x => x.codDes);

            modelBuilder.Entity<ZonaCliente>()
                .HasOne(x => x.Zona)
                .WithMany()
                .HasForeignKey(x => x.ZonaCod);
            modelBuilder.Entity<ZonaCliente>()
                .HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.CteCod);
            modelBuilder.Entity<ZonaCliente>()
                .HasOne(x => x.Destino)
                .WithMany()
                .HasForeignKey(x => x.DesCod);
            modelBuilder.Entity<OrdenPedido>()
                .HasOne(x => x.OrdenEmbarque)
                .WithMany()
                .HasForeignKey(x => x.CodPed);
            //OrdenEmbarque a Órdenes
            modelBuilder.Entity<OrdenEmbarque>()
                .HasOne(x => x.Orden)
                .WithOne(x => x.OrdenEmbarque)
                .HasForeignKey<OrdenEmbarque>("Folio", "CompartmentId")
                .HasPrincipalKey<Orden>("Folio", "CompartmentId");

            modelBuilder.Entity<AccionCorreo>()
                .HasOne(x => x.Accion)
                .WithMany()
                .HasForeignKey(x => x.CodAccion);

            modelBuilder.Entity<AccionCorreo>()
                .HasOne(x => x.Contacto)
                .WithMany()
                .HasForeignKey(x => x.CodContacto);

            modelBuilder.Entity<Contacto>()
                .HasMany(x => x.AccionCorreos)
                .WithOne(x => x.Contacto)
                .HasForeignKey(x => x.CodContacto);

            modelBuilder.Entity<OrdenEmbarque>()
                .HasOne(x => x.OrdenCierre)
                .WithOne(x => x.OrdenEmbarque)
                .HasForeignKey<OrdenCierre>(x => x.CodPed);

            //Precio - Zona
            modelBuilder.Entity<PrecioProgramado>()
                .HasOne(x => x.Zona)
                .WithMany()
                .HasForeignKey(x => x.codZona);
            //Precio - cliente
            modelBuilder.Entity<PrecioProgramado>()
                .HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.codCte);
            //Precio - Producto
            modelBuilder.Entity<PrecioProgramado>()
                .HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.codPrd);
            //Precio - Destino
            modelBuilder.Entity<PrecioProgramado>()
                .HasOne(x => x.Destino)
                .WithMany()
                .HasForeignKey(x => x.codDes);
            //Cierre - grupo
            modelBuilder.Entity<OrdenCierre>()
                .HasOne(x => x.Grupo)
                .WithOne()
                .HasForeignKey<OrdenCierre>(x => x.CodGru);
            modelBuilder.Entity<OrdenEmbarque>()
                .HasOne(x => x.OrdenPedido)
                .WithOne(x => x.OrdenEmbarque)
                .HasForeignKey<OrdenPedido>(x => x.CodPed);
            modelBuilder.Entity<Orden>()
                .HasOne(x => x.OrdEmbDet)
                .WithOne(x => x.Orden)
                .HasPrincipalKey<OrdEmbDet>(x => x.Bol)
                .HasForeignKey<Orden>(x => x.BatchId);

        }


        public DbSet<Actividad> Actividad { get; set; }
        public DbSet<BitacoraModificacion> BitacoraModificacion { get; set; }
        public DbSet<Chofer> Chofer { get; set; }
        public DbSet<Ciudad> Ciudad { get; set; }
        public DbSet<Cliente> Cliente { get; set; }
        public DbSet<Descarga> Descarga { get; set; }
        public DbSet<Destino> Destino { get; set; }
        public DbSet<Entidad> Entidad { get; set; }
        public DbSet<FormaPago> FormaPago { get; set; }
        public DbSet<Grupo> Grupo { get; set; }
        public DbSet<GrupoUsuario> GrupoUsuario { get; set; }
        public DbSet<Inventario> Inventario { get; set; }
        public DbSet<Log> Log { get; set; }
        public DbSet<OrdEmbDet> OrdEmbDet { get; set; }
        public DbSet<Orden> Orden { get; set; }
        public DbSet<OrdenCompra> OrdenCompra { get; set; }
        public DbSet<OrdenEmbarque> OrdenEmbarque { get; set; }
        public DbSet<Pipa> Pipa { get; set; }
        public DbSet<Privilegio> Privilegio { get; set; }
        public DbSet<PrivilegioUsuario> PrivilegioUsuario { get; set; }
        public DbSet<Producto> Producto { get; set; }
        public DbSet<Tad> Tad { get; set; }
        public DbSet<Tonel> Tonel { get; set; }
        public DbSet<Transportista> Transportista { get; set; }
        public DbSet<TransportistaGrupo> TransportistaGrupo { get; set; }
        public DbSet<users_descarga> Users_descarga { get; set; }
        public DbSet<Usuario> Usuario { get; set; }
        public DbSet<Shared.Modelos.Version> Version { get; set; }
        public DbSet<OrdenCierre> OrdenCierre { get; set; }
        public DbSet<Contacto> Contacto { get; set; }
        public DbSet<Precio> Precio { get; set; }
        public DbSet<PrecioHistorico> PreciosHistorico { get; set; }
        public DbSet<Zona> Zona { get; set; }
        public DbSet<ZonaCliente> ZonaCliente { get; set; }
        public DbSet<OrdenPedido> OrdenPedido { get; set; }
        public DbSet<Accion> Accion { get; set; }
        public DbSet<AccionCorreo> AccionCorreo { get; set; }
        public DbSet<Porcentaje> Porcentaje { get; set; }
        public DbSet<PrecioProgramado> PrecioProgramado { get; set; }
        public DbSet<ActividadRegistrada> ActividadRegistrada { get; set; }
        public DbSet<CierrePrecioDespuesFecha> CierrePrecioDespuesFecha { get; set; }
        public DbSet<Errors> Errors { get; set; }
    }
}
