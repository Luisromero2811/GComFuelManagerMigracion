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
                .HasForeignKey(x => x.CodZona);
            //Precio - cliente
            modelBuilder.Entity<Precio>()
                .HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.CodCte);
            //Precio - Producto
            modelBuilder.Entity<Precio>()
                .HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.CodPrd);
            //Precio - Destino
            modelBuilder.Entity<Precio>()
                .HasOne(x => x.Destino)
                .WithMany()
                .HasForeignKey(x => x.CodDes);

            //PrecioHistorico - Zona
            modelBuilder.Entity<PrecioHistorico>()
                .HasOne(x => x.Zona)
                .WithMany()
                .HasForeignKey(x => x.CodZona);
            //PrecioHistorico - cliente
            modelBuilder.Entity<PrecioHistorico>()
                .HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.CodCte);
            //PrecioHistorico - Producto
            modelBuilder.Entity<PrecioHistorico>()
                .HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.CodPrd);
            //PrecioHistorico - Destino
            modelBuilder.Entity<PrecioHistorico>()
                .HasOne(x => x.Destino)
                .WithMany()
                .HasForeignKey(x => x.CodDes);

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

            //modelBuilder.Entity<OrdenPedido>()
            //    .HasOne(x => x.OrdenEmbarque)
            //    .WithOne(x => x.OrdenPedido)
            //    .HasPrincipalKey<OrdenEmbarque>(x => x.Cod)
            //    .HasForeignKey<OrdenPedido>(x => x.CodPed);
            //OrdenEmbarque a Órdenes
            //modelBuilder.Entity<OrdenEmbarque>()
            //    .HasOne(x => x.Orden)
            //    .WithOne(x => x.OrdenEmbarque)
            //    .HasForeignKey<OrdenEmbarque>("Folio", "CompartmentId")
            //    .HasPrincipalKey<Orden>("Folio", "CompartmentId");

            //OrdenEmbarque a Órdenes
            modelBuilder.Entity<OrdenEmbarque>()
                .HasOne(x => x.Orden)
                .WithOne(x => x.OrdenEmbarque)
                .HasForeignKey<OrdenEmbarque>(x => x.FolioSyn)
                .HasPrincipalKey<Orden>(x => x.Ref);

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
                .HasForeignKey(x => x.CodZona);
            //Precio - cliente
            modelBuilder.Entity<PrecioProgramado>()
                .HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.CodCte);
            //Precio - Producto
            modelBuilder.Entity<PrecioProgramado>()
                .HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.CodPrd);
            //Precio - Destino
            modelBuilder.Entity<PrecioProgramado>()
                .HasOne(x => x.Destino)
                .WithMany()
                .HasForeignKey(x => x.CodDes);
            //Cierre - grupo
            modelBuilder.Entity<OrdenCierre>()
                .HasOne(x => x.Grupo)
                .WithMany()
                .HasForeignKey(x => x.CodGru);

            modelBuilder.Entity<OrdenEmbarque>()
                .HasOne(x => x.OrdenPedido)
                .WithOne(x => x.OrdenEmbarque)
                .HasPrincipalKey<OrdenEmbarque>(x => x.Cod)
                .HasForeignKey<OrdenPedido>(x => x.CodPed);

            modelBuilder.Entity<Orden>()
                .HasOne(x => x.OrdEmbDet)
                .WithOne(x => x.Orden)
                .HasPrincipalKey<OrdEmbDet>(x => x.Bol)
                .HasForeignKey<Orden>(x => x.BatchId);

            modelBuilder.Entity<OrdenCierre>()
                .HasMany(x => x.OrdenPedidos)
                .WithOne(x => x.OrdenCierre)
                .HasPrincipalKey(x => x.Cod)
                .HasForeignKey(x => x.CodCierre);

            modelBuilder.Entity<OrdenEmbarque>()
                .HasOne(x => x.Moneda)
                .WithMany()
                .HasForeignKey(x => x.ID_Moneda);

            modelBuilder.Entity<OrdenCierre>()
                .HasOne(x => x.Moneda)
                .WithMany()
                .HasForeignKey(x => x.ID_Moneda);

            modelBuilder.Entity<Precio>()
                .HasOne(x => x.Moneda)
                .WithMany()
                .HasForeignKey(x => x.ID_Moneda);

            modelBuilder.Entity<PrecioProgramado>()
                .HasOne(x => x.Moneda)
                .WithMany()
                .HasForeignKey(x => x.ID_Moneda);

            modelBuilder.Entity<PrecioHistorico>()
                .HasOne(x => x.Moneda)
                .WithMany()
                .HasForeignKey(x => x.ID_Moneda);

            modelBuilder.Entity<Pedimento>()
                .HasOne(x => x.Producto)
                .WithMany()
                .HasForeignKey(x => x.ID_Producto);

            modelBuilder.Entity<Precio>()
                .HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.ID_Usuario);

            modelBuilder.Entity<PrecioProgramado>()
                .HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.ID_Usuario);

            modelBuilder.Entity<PrecioHistorico>()
                .HasOne(x => x.Usuario)
                .WithMany()
                .HasForeignKey(x => x.ID_Usuario);

            modelBuilder.Entity<Redireccionamiento>()
                .HasOne(x => x.Orden)
                .WithOne(x => x.Redireccionamiento)
                .HasForeignKey<Redireccionamiento>(x => x.Id_Orden)
                .HasPrincipalKey<Orden>(x => x.Cod);
            //.HasPrincipalKey<Orden>(x => x.Cod);

            modelBuilder.Entity<Redireccionamiento>()
                .HasOne(x => x.Grupo)
                .WithMany()
                .HasForeignKey(x => x.Grupo_Red);

            modelBuilder.Entity<Redireccionamiento>()
                .HasOne(x => x.Cliente)
                .WithMany()
                .HasForeignKey(x => x.Cliente_Red);

            modelBuilder.Entity<Redireccionamiento>()
                .HasOne(x => x.Destino)
                .WithMany()
                .HasForeignKey(x => x.Destino_Red);

            modelBuilder.Entity<Cliente>()
                .HasOne(x => x.Vendedor)
                .WithMany()
                .HasForeignKey(x => x.Id_Vendedor);

            modelBuilder.Entity<Cliente>()
                .HasOne(x => x.Originador)
                .WithMany()
                .HasForeignKey(x => x.Id_Originador);

            modelBuilder.Entity<Vendedor>()
                .HasMany(x => x.Clientes)
                .WithOne(x => x.Vendedor)
                .HasForeignKey(x => x.Id_Vendedor)
                .HasPrincipalKey(x => x.Id);

            modelBuilder.Entity<Originador>()
                .HasMany(x => x.Clientes)
                .WithOne(x => x.Originador)
                .HasForeignKey(x => x.Id_Originador)
                .HasPrincipalKey(x => x.Id);

            modelBuilder.Entity<Vendedor_Originador>().HasKey(vo => new { vo.VendedorId, vo.OriginadorId });

            modelBuilder.Entity<Vendedor>()
                .HasMany(x => x.Originadores)
                .WithMany(x => x.Vendedores)
                .UsingEntity<Vendedor_Originador>(
                    l => l.HasOne(x => x.Originador).WithMany(x => x.Vendedor_Originador).OnDelete(DeleteBehavior.Restrict),
                    r => r.HasOne(x => x.Vendedor).WithMany(x => x.Vendedor_Originador).OnDelete(DeleteBehavior.Restrict)
                );

            modelBuilder.Entity<Vendedor>()
                .HasMany(x => x.Metas_Vendedor)
                .WithOne(x => x.Vendedor)
                .HasForeignKey(x => x.VendedorId);

            modelBuilder.Entity<Cliente_Tad>().HasKey(ct => new { ct.Id_Cliente, ct.Id_Terminal });
            modelBuilder.Entity<Destino_Tad>().HasKey(dt => new { dt.Id_Destino, dt.Id_Terminal });
            modelBuilder.Entity<GrupoTransportista_Tad>().HasKey(gt => new { gt.Id_GrupoTransportista, gt.Id_Terminal });
            modelBuilder.Entity<Transportista_Tad>().HasKey(tt => new { tt.Id_Transportista, tt.Id_Terminal });
            modelBuilder.Entity<Usuario_Tad>().HasKey(ut => new { ut.Id_Usuario, ut.Id_Terminal });
            modelBuilder.Entity<Chofer_Tad>().HasKey(ut => new { ut.Id_Chofer, ut.Id_Terminal });
            modelBuilder.Entity<Unidad_Tad>().HasKey(ut => new { ut.Id_Unidad, ut.Id_Terminal });

            modelBuilder.Entity<Cliente>()
                .HasMany(x => x.Terminales)
                .WithMany(x => x.Clientes)
                .UsingEntity<Cliente_Tad>(
                    l => l.HasOne(x => x.Terminal).WithMany(x => x.Cliente_Tads).HasForeignKey(x => x.Id_Terminal).OnDelete(DeleteBehavior.Restrict),
                    r => r.HasOne(x => x.Cliente).WithMany(x => x.Cliente_Tads).HasForeignKey(x => x.Id_Cliente).OnDelete(DeleteBehavior.Restrict)
                );

            modelBuilder.Entity<Destino>()
                .HasMany(x => x.Terminales)
                .WithMany(x => x.Destinos)
                .UsingEntity<Destino_Tad>(
                l => l.HasOne(x => x.Terminal).WithMany(x => x.Destino_Tads).HasForeignKey(x => x.Id_Terminal).OnDelete(DeleteBehavior.Restrict),
                r => r.HasOne(x => x.Destino).WithMany(x => x.Destino_Tads).HasForeignKey(x => x.Id_Destino).OnDelete(DeleteBehavior.Restrict)
                );

            modelBuilder.Entity<Transportista>()
                .HasMany(x => x.Terminales)
                .WithMany(x => x.Transportistas)
                .UsingEntity<Transportista_Tad>(
                l => l.HasOne(x => x.Terminal).WithMany(x => x.Transportista_Tads).HasForeignKey(x => x.Id_Terminal).OnDelete(DeleteBehavior.Restrict),
                r => r.HasOne(x => x.Transportista).WithMany(x => x.Transportista_Tads).HasForeignKey(x => x.Id_Transportista).OnDelete(DeleteBehavior.Restrict)
                );

            modelBuilder.Entity<GrupoTransportista>()
                .HasMany(x => x.Terminales)
                .WithMany(x => x.GruposTransportes)
                .UsingEntity<GrupoTransportista_Tad>(
                l => l.HasOne(x => x.Terminal).WithMany(x => x.GrupoTransportista_Tads).HasForeignKey(x => x.Id_Terminal).OnDelete(DeleteBehavior.Restrict),
                r => r.HasOne(x => x.GrupoTransportista).WithMany(x => x.GrupoTransportista_Tads).HasForeignKey(x => x.Id_GrupoTransportista).OnDelete(DeleteBehavior.Restrict)
                );

            modelBuilder.Entity<Chofer>()
                .HasMany(x => x.Terminales)
                .WithMany(x => x.Choferes)
                .UsingEntity<Chofer_Tad>(
                l => l.HasOne(x => x.Terminal).WithMany(x => x.Chofer_Tads).HasForeignKey(x => x.Id_Terminal).OnDelete(DeleteBehavior.Restrict),
                r => r.HasOne(x => x.Chofer).WithMany(x => x.Chofer_Tads).HasForeignKey(x => x.Id_Chofer).OnDelete(DeleteBehavior.Restrict)
                );

            modelBuilder.Entity<Tonel>()
                .HasMany(x => x.Terminales)
                .WithMany(x => x.Unidades)
                .UsingEntity<Unidad_Tad>(
                l => l.HasOne(x => x.Terminal).WithMany(x => x.Unidad_Tads).HasForeignKey(x => x.Id_Terminal).OnDelete(DeleteBehavior.Restrict),
                r => r.HasOne(x => x.Tonel).WithMany(x => x.Unidad_Tads).HasForeignKey(x => x.Id_Unidad).OnDelete(DeleteBehavior.Restrict)
                );



            modelBuilder.Entity<Orden>()
                .HasOne(x => x.Terminal)
                .WithMany()
                .HasForeignKey(x => x.Id_Tad);

            modelBuilder.Entity<OrdenCierre>()
                .HasOne(x => x.Terminal)
                .WithMany()
                .HasForeignKey(x => x.Id_Tad);

            modelBuilder.Entity<Precio>()
                .HasOne(x => x.Terminal)
                .WithMany()
                .HasForeignKey(x => x.Id_Tad);

            modelBuilder.Entity<PrecioHistorico>()
                .HasOne(x => x.Terminal)
                .WithMany()
                .HasForeignKey(x => x.Id_Tad);

            modelBuilder.Entity<PrecioProgramado>()
                .HasOne(x => x.Terminal)
                .WithMany()
                .HasForeignKey(x => x.Id_Tad);

            modelBuilder.Entity<TransportistaGrupo>()
                .HasOne(x => x.Terminal)
                .WithMany()
                .HasForeignKey(x => x.Id_Tad);

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
        public DbSet<Moneda> Moneda { get; set; }
        public DbSet<Consecutivo> Consecutivo { get; set; }
        public DbSet<Pedimento> Pedimentos { get; set; }
        public DbSet<Redireccionamiento> Redireccionamientos { get; set; }
        public DbSet<Vendedor> Vendedores { get; set; }
        public DbSet<Originador> Originadores { get; set; }
        public DbSet<Vendedor_Originador> Vendedor_Originador { get; set; }
        public DbSet<Metas_Vendedor> Metas_Vendedor { get; set; }
        public DbSet<Cliente_Tad> Cliente_Tad { get; set; }
        public DbSet<Destino_Tad> Destino_Tad { get; set; }
        public DbSet<Transportista_Tad> Transportista_Tad { get; set; }
        public DbSet<Usuario_Tad> Usuario_Tad { get; set; }
        public DbSet<GrupoTransportista> GrupoTransportista { get; set; }
        public DbSet<GrupoTransportista_Tad> GrupoTransportista_Tad { get; set; }
        public DbSet<Chofer_Tad> Chofer_Tad { get; set; }
        public DbSet<Unidad_Tad> Unidad_Tad { get; set; }
    }
}