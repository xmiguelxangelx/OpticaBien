using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace Optica1.Models;

public partial class ProyectoopticaContext : DbContext
{
    public ProyectoopticaContext()
    {
    }

    public ProyectoopticaContext(DbContextOptions<ProyectoopticaContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Citum> Cita { get; set; }

    public virtual DbSet<Compra> Compras { get; set; }

    public virtual DbSet<Diagnostico> Diagnosticos { get; set; }

    public virtual DbSet<Historiaclinica> Historiaclinicas { get; set; }

    public virtual DbSet<MedioDePago> MedioDePagos { get; set; }

    public virtual DbSet<Perfil> Perfiles { get; set; }

    public virtual DbSet<Persona> Personas { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<ProductoCompra> ProductoCompras { get; set; }

    public virtual DbSet<ProductoVentum> ProductoVenta { get; set; }

    public virtual DbSet<Proveedor> Proveedors { get; set; }

    public virtual DbSet<ProveedorCompra> ProveedorCompras { get; set; }

    public virtual DbSet<Tratamiento> Tratamientos { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<UsuarioPerfil> UsuarioPerfils { get; set; }

    public virtual DbSet<VentaPago> VentaPagos { get; set; }

    public virtual DbSet<Ventum> Venta { get; set; }

    public virtual DbSet<VwHistoriaClinicaCompletum> VwHistoriaClinicaCompleta { get; set; }

    public virtual DbSet<VwInventarioCompra> VwInventarioCompras { get; set; }

    public virtual DbSet<VwVentasDetallada> VwVentasDetalladas { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=localhost;port=3306;database=proyectooptica;uid=root", Microsoft.EntityFrameworkCore.ServerVersion.Parse("10.4.32-mariadb"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Citum>(entity =>
        {
            entity.HasKey(e => e.IdCita).HasName("PRIMARY");

            entity.ToTable("cita");

            entity.HasIndex(e => e.IdHistoriaclinica, "fk_c_idhistoriaclinica_idx");

            entity.HasIndex(e => e.IdUsuarioempleado, "fk_c_idusuarioempleado_idx");

            entity.HasIndex(e => e.IdUsuariopaciente, "fk_c_idusuariopaciente_idx");

            entity.Property(e => e.IdCita)
                .HasColumnType("int(11)")
                .HasColumnName("id_cita");
            entity.Property(e => e.Estado)
                .HasMaxLength(45)
                .HasColumnName("estado");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.Hora)
                .HasColumnType("time")
                .HasColumnName("hora");
            entity.Property(e => e.IdHistoriaclinica)
                .HasColumnType("int(11)")
                .HasColumnName("id_historiaclinica");
            entity.Property(e => e.IdUsuarioempleado)
                .HasColumnType("int(11)")
                .HasColumnName("id_usuarioempleado");
            entity.Property(e => e.IdUsuariopaciente)
                .HasColumnType("int(11)")
                .HasColumnName("id_usuariopaciente");
            entity.Property(e => e.Motivo)
                .HasMaxLength(255)
                .HasColumnName("motivo");

            entity.HasOne(d => d.IdHistoriaclinicaNavigation).WithMany(p => p.Cita)
                .HasForeignKey(d => d.IdHistoriaclinica)
                .HasConstraintName("fk_c_idhistoriaclinica");

            entity.HasOne(d => d.IdUsuarioempleadoNavigation).WithMany(p => p.CitumIdUsuarioempleadoNavigations)
                .HasForeignKey(d => d.IdUsuarioempleado)
                .HasConstraintName("fk_c_idusuarioempleado");

            entity.HasOne(d => d.IdUsuariopacienteNavigation).WithMany(p => p.CitumIdUsuariopacienteNavigations)
                .HasForeignKey(d => d.IdUsuariopaciente)
                .HasConstraintName("fk_c_idusuariopaciente");
        });

        modelBuilder.Entity<Compra>(entity =>
        {
            entity.HasKey(e => e.IdCompra).HasName("PRIMARY");

            entity.ToTable("compra");

            entity.Property(e => e.IdCompra)
                .HasColumnType("int(11)")
                .HasColumnName("id_compra");
            entity.Property(e => e.FechaCompra).HasColumnName("fecha_compra");
        });

        modelBuilder.Entity<Diagnostico>(entity =>
        {
            entity.HasKey(e => e.IdDiagnostico).HasName("PRIMARY");

            entity.ToTable("diagnostico");

            entity.HasIndex(e => e.IdHistoriaclinica, "fk_d_idhistoriaclinica_idx");

            entity.Property(e => e.IdDiagnostico)
                .HasColumnType("int(11)")
                .HasColumnName("id_diagnostico");
            entity.Property(e => e.Descripcion)
                .HasColumnType("text")
                .HasColumnName("descripcion");
            entity.Property(e => e.FechaEdicion).HasColumnName("fecha_edicion");
            entity.Property(e => e.IdHistoriaclinica)
                .HasColumnType("int(11)")
                .HasColumnName("id_historiaclinica");
            entity.Property(e => e.Observaciones)
                .HasColumnType("text")
                .HasColumnName("observaciones");

            entity.HasOne(d => d.IdHistoriaclinicaNavigation).WithMany(p => p.Diagnosticos)
                .HasForeignKey(d => d.IdHistoriaclinica)
                .HasConstraintName("fk_d_idhistoriaclinica");
        });

        modelBuilder.Entity<Historiaclinica>(entity =>
        {
            entity.HasKey(e => e.IdHistoriaclinica).HasName("PRIMARY");

            entity.ToTable("historiaclinica");

            entity.Property(e => e.IdHistoriaclinica)
                .HasColumnType("int(11)")
                .HasColumnName("id_historiaclinica");
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
        });

        modelBuilder.Entity<MedioDePago>(entity =>
        {
            entity.HasKey(e => e.IdMedioDePago).HasName("PRIMARY");

            entity.ToTable("medio_de_pago");

            entity.Property(e => e.IdMedioDePago)
                .HasColumnType("int(11)")
                .HasColumnName("id_medio_de_pago");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(45)
                .HasColumnName("descripcion");
        });

        modelBuilder.Entity<Perfil>(entity =>
        {
            entity.HasKey(e => e.IdPerfil).HasName("PRIMARY");

            entity.ToTable("perfil");

            entity.Property(e => e.IdPerfil)
                .HasColumnType("int(11)")
                .HasColumnName("id_perfil");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(45)
                .HasColumnName("descripcion");
        });

        modelBuilder.Entity<Persona>(entity =>
        {
            entity.HasKey(e => e.IdPersona).HasName("PRIMARY");

            entity.ToTable("persona");

            entity.Property(e => e.IdPersona)
                .ValueGeneratedNever()
                .HasColumnType("bigint(20)")
                .HasColumnName("id_persona");
            entity.Property(e => e.Correo)
                .HasMaxLength(45)
                .HasColumnName("correo");
            entity.Property(e => e.Direccion)
                .HasMaxLength(150)
                .HasColumnName("direccion");
            entity.Property(e => e.FechaNacimiento).HasColumnName("fecha_nacimiento");
            entity.Property(e => e.PrimerApellido)
                .HasMaxLength(45)
                .HasColumnName("primer_apellido");
            entity.Property(e => e.PrimerNombre)
                .HasMaxLength(45)
                .HasColumnName("primer_nombre");
            entity.Property(e => e.SegundoApellido)
                .HasMaxLength(45)
                .HasColumnName("segundo_apellido");
            entity.Property(e => e.SegundoNombre)
                .HasMaxLength(45)
                .HasColumnName("segundo_nombre");
            entity.Property(e => e.Telefono)
                .HasColumnType("bigint(20)")
                .HasColumnName("telefono");
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.IdProducto).HasName("PRIMARY");

            entity.ToTable("producto");

            entity.Property(e => e.IdProducto)
                .HasColumnType("int(11)")
                .HasColumnName("id_producto");
            entity.Property(e => e.FechaActualizacion).HasColumnName("fecha_actualizacion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(45)
                .HasColumnName("nombre");
            entity.Property(e => e.Precio).HasColumnName("precio");
            entity.Property(e => e.Stock)
                .HasColumnType("int(11)")
                .HasColumnName("stock");
        });

        modelBuilder.Entity<ProductoCompra>(entity =>
        {
            entity.HasKey(e => e.IdProductocompra).HasName("PRIMARY");

            entity.ToTable("producto_compra");

            entity.HasIndex(e => e.IdCompra, "fk_pc_idcompra_idx");

            entity.HasIndex(e => e.IdProducto, "fk_pc_idproducto_idx");

            entity.Property(e => e.IdProductocompra)
                .HasColumnType("int(11)")
                .HasColumnName("id_productocompra");
            entity.Property(e => e.Cantidad)
                .HasColumnType("int(11)")
                .HasColumnName("cantidad");
            entity.Property(e => e.IdCompra)
                .HasColumnType("int(11)")
                .HasColumnName("id_compra");
            entity.Property(e => e.IdProducto)
                .HasColumnType("int(11)")
                .HasColumnName("id_producto");

            entity.HasOne(d => d.IdCompraNavigation).WithMany(p => p.ProductoCompras)
                .HasForeignKey(d => d.IdCompra)
                .HasConstraintName("fk_pc_idcompra");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.ProductoCompras)
                .HasForeignKey(d => d.IdProducto)
                .HasConstraintName("fk_pc_idproducto");
        });

        modelBuilder.Entity<ProductoVentum>(entity =>
        {
            entity.HasKey(e => e.IdProductoventa).HasName("PRIMARY");

            entity.ToTable("producto_venta");

            entity.HasIndex(e => e.IdProducto, "fk_pv_idproducto_idx");

            entity.HasIndex(e => e.IdVenta, "fk_pv_idventa_idx");

            entity.Property(e => e.IdProductoventa)
                .HasColumnType("int(11)")
                .HasColumnName("id_productoventa");
            entity.Property(e => e.Cantidad)
                .HasColumnType("int(11)")
                .HasColumnName("cantidad");
            entity.Property(e => e.IdProducto)
                .HasColumnType("int(11)")
                .HasColumnName("id_producto");
            entity.Property(e => e.IdVenta)
                .HasColumnType("int(11)")
                .HasColumnName("id_venta");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.ProductoVenta)
                .HasForeignKey(d => d.IdProducto)
                .HasConstraintName("fk_pv_idproducto");

            entity.HasOne(d => d.IdVentaNavigation).WithMany(p => p.ProductoVenta)
                .HasForeignKey(d => d.IdVenta)
                .HasConstraintName("fk_pv_idventa");
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasKey(e => e.IdProveedorNit).HasName("PRIMARY");

            entity.ToTable("proveedor");

            entity.Property(e => e.IdProveedorNit)
                .HasColumnType("int(11)")
                .HasColumnName("id_proveedor_nit");
            entity.Property(e => e.Correo)
                .HasMaxLength(45)
                .HasColumnName("correo");
            entity.Property(e => e.Direccion)
                .HasMaxLength(150)
                .HasColumnName("direccion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(45)
                .HasColumnName("nombre");
            entity.Property(e => e.Telefono)
                .HasColumnType("bigint(20)")
                .HasColumnName("telefono");
        });

        modelBuilder.Entity<ProveedorCompra>(entity =>
        {
            entity.HasKey(e => e.IdProveedorcompra).HasName("PRIMARY");

            entity.ToTable("proveedor_compra");

            entity.HasIndex(e => e.IdCompra, "fk_pc_idcompra_idx");

            entity.HasIndex(e => e.IdProveedorNit, "fk_pc_idproveedornit_idx");

            entity.Property(e => e.IdProveedorcompra)
                .HasColumnType("int(11)")
                .HasColumnName("id_proveedorcompra");
            entity.Property(e => e.IdCompra)
                .HasColumnType("int(11)")
                .HasColumnName("id_compra");
            entity.Property(e => e.IdProveedorNit)
                .HasColumnType("int(11)")
                .HasColumnName("id_proveedor_nit");

            entity.HasOne(d => d.IdCompraNavigation).WithMany(p => p.ProveedorCompras)
                .HasForeignKey(d => d.IdCompra)
                .HasConstraintName("fk_proveedorc_idcompra");

            entity.HasOne(d => d.IdProveedorNitNavigation).WithMany(p => p.ProveedorCompras)
                .HasForeignKey(d => d.IdProveedorNit)
                .HasConstraintName("fk_proveedorc_idproveedornit");
        });

        modelBuilder.Entity<Tratamiento>(entity =>
        {
            entity.HasKey(e => e.IdTratamiento).HasName("PRIMARY");

            entity.ToTable("tratamiento");

            entity.HasIndex(e => e.IdDiagnostico, "fk_t_iddiagnostico_idx");

            entity.Property(e => e.IdTratamiento)
                .HasColumnType("int(11)")
                .HasColumnName("id_tratamiento");
            entity.Property(e => e.DetallesTratamiento)
                .HasColumnType("text")
                .HasColumnName("detalles_tratamiento");
            entity.Property(e => e.FechaEdicion).HasColumnName("fecha_edicion");
            entity.Property(e => e.IdDiagnostico)
                .HasColumnType("int(11)")
                .HasColumnName("id_diagnostico");
            entity.Property(e => e.Indicaciones)
                .HasColumnType("text")
                .HasColumnName("indicaciones");

            entity.HasOne(d => d.IdDiagnosticoNavigation).WithMany(p => p.Tratamientos)
                .HasForeignKey(d => d.IdDiagnostico)
                .HasConstraintName("fk_t_iddiagnostico");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("PRIMARY");

            entity.ToTable("usuario");

            entity.HasIndex(e => e.IdPersona, "fk_u_idpersona_idx");

            entity.HasIndex(e => e.NombreUsuario, "nombre_usuario_UNIQUE").IsUnique();

            entity.Property(e => e.IdUsuario)
                .HasColumnType("int(11)")
                .HasColumnName("id_usuario");
            entity.Property(e => e.Clave)
                .HasMaxLength(45)
                .HasColumnName("clave");
            entity.Property(e => e.IdPersona)
                .HasColumnType("bigint(20)")
                .HasColumnName("id_persona");
            entity.Property(e => e.NombreUsuario)
                .HasMaxLength(45)
                .HasColumnName("nombre_usuario");

            entity.HasOne(d => d.IdPersonaNavigation).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.IdPersona)
                .HasConstraintName("fk_u_idpersona");
        });

        modelBuilder.Entity<UsuarioPerfil>(entity =>
        {
            entity.HasKey(e => e.IdUsuarioperfil).HasName("PRIMARY");

            entity.ToTable("usuario_perfil");

            entity.HasIndex(e => e.IdPerfil, "fk_up_idperfil_idx");

            entity.HasIndex(e => e.IdUsuario, "fk_up_idusuario_idx");

            entity.Property(e => e.IdUsuarioperfil)
                .HasColumnType("int(11)")
                .HasColumnName("id_usuarioperfil");
            entity.Property(e => e.IdPerfil)
                .HasColumnType("int(11)")
                .HasColumnName("id_perfil");
            entity.Property(e => e.IdUsuario)
                .HasColumnType("int(11)")
                .HasColumnName("id_usuario");

            entity.HasOne(d => d.IdPerfilNavigation).WithMany(p => p.UsuarioPerfils)
                .HasForeignKey(d => d.IdPerfil)
                .HasConstraintName("fk_up_idperfil");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.UsuarioPerfils)
                .HasForeignKey(d => d.IdUsuario)
                .HasConstraintName("fk_up_idusuario");
        });

        modelBuilder.Entity<VentaPago>(entity =>
        {
            entity.HasKey(e => e.IdVentapago).HasName("PRIMARY");

            entity.ToTable("venta_pago");

            entity.HasIndex(e => e.IdMedioDePago, "fk_vp_idmediodepago_idx");

            entity.HasIndex(e => e.IdVenta, "fk_vp_idventa_idx");

            entity.Property(e => e.IdVentapago)
                .HasColumnType("int(11)")
                .HasColumnName("id_ventapago");
            entity.Property(e => e.IdMedioDePago)
                .HasColumnType("int(11)")
                .HasColumnName("id_medio_de_pago");
            entity.Property(e => e.IdVenta)
                .HasColumnType("int(11)")
                .HasColumnName("id_venta");

            entity.HasOne(d => d.IdMedioDePagoNavigation).WithMany(p => p.VentaPagos)
                .HasForeignKey(d => d.IdMedioDePago)
                .HasConstraintName("fk_vp_idmediodepago");

            entity.HasOne(d => d.IdVentaNavigation).WithMany(p => p.VentaPagos)
                .HasForeignKey(d => d.IdVenta)
                .HasConstraintName("fk_vp_idventa");
        });

        modelBuilder.Entity<Ventum>(entity =>
        {
            entity.HasKey(e => e.IdVenta).HasName("PRIMARY");

            entity.ToTable("venta");

            entity.HasIndex(e => e.IdUsuarioempleado, "fk_v_idusuarioempleado_idx");

            entity.HasIndex(e => e.IdUsuariopaciente, "fk_v_idusuariopaciente_idx");

            entity.Property(e => e.IdVenta)
                .HasColumnType("int(11)")
                .HasColumnName("id_venta");
            entity.Property(e => e.Abono).HasColumnName("abono");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.FechaEntrega).HasColumnName("fecha_entrega");
            entity.Property(e => e.IdUsuarioempleado)
                .HasColumnType("int(11)")
                .HasColumnName("id_usuarioempleado");
            entity.Property(e => e.IdUsuariopaciente)
                .HasColumnType("int(11)")
                .HasColumnName("id_usuariopaciente");
            entity.Property(e => e.Total).HasColumnName("total");

            entity.HasOne(d => d.IdUsuarioempleadoNavigation).WithMany(p => p.VentumIdUsuarioempleadoNavigations)
                .HasForeignKey(d => d.IdUsuarioempleado)
                .HasConstraintName("fk_v_idusuarioempleado");

            entity.HasOne(d => d.IdUsuariopacienteNavigation).WithMany(p => p.VentumIdUsuariopacienteNavigations)
                .HasForeignKey(d => d.IdUsuariopaciente)
                .HasConstraintName("fk_v_idusuariopaciente");
        });

        modelBuilder.Entity<VwHistoriaClinicaCompletum>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_historia_clinica_completa");

            entity.Property(e => e.DetallesTratamiento)
                .HasColumnType("text")
                .HasColumnName("detalles_tratamiento");
            entity.Property(e => e.Diagnostico)
                .HasColumnType("text")
                .HasColumnName("diagnostico");
            entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion");
            entity.Property(e => e.IdHistoriaclinica)
                .HasColumnType("int(11)")
                .HasColumnName("id_historiaclinica");
            entity.Property(e => e.Indicaciones)
                .HasColumnType("text")
                .HasColumnName("indicaciones");
            entity.Property(e => e.Observaciones)
                .HasColumnType("text")
                .HasColumnName("observaciones");
            entity.Property(e => e.PrimerApellido)
                .HasMaxLength(45)
                .HasColumnName("primer_apellido");
            entity.Property(e => e.PrimerNombre)
                .HasMaxLength(45)
                .HasColumnName("primer_nombre");
        });

        modelBuilder.Entity<VwInventarioCompra>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_inventario_compras");

            entity.Property(e => e.IdProducto)
                .HasColumnType("int(11)")
                .HasColumnName("id_producto");
            entity.Property(e => e.Nombre)
                .HasMaxLength(45)
                .HasColumnName("nombre");
            entity.Property(e => e.Precio).HasColumnName("precio");
            entity.Property(e => e.Stock)
                .HasColumnType("int(11)")
                .HasColumnName("stock");
            entity.Property(e => e.TotalComprado)
                .HasPrecision(32)
                .HasColumnName("total_comprado");
            entity.Property(e => e.UltimaCompra).HasColumnName("ultima_compra");
        });

        modelBuilder.Entity<VwVentasDetallada>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ventas_detalladas");

            entity.Property(e => e.Abono).HasColumnName("abono");
            entity.Property(e => e.ApellidoCliente)
                .HasMaxLength(45)
                .HasColumnName("apellido_cliente");
            entity.Property(e => e.ApellidoEmpleado)
                .HasMaxLength(45)
                .HasColumnName("apellido_empleado");
            entity.Property(e => e.Cantidad)
                .HasColumnType("int(11)")
                .HasColumnName("cantidad");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.FechaEntrega).HasColumnName("fecha_entrega");
            entity.Property(e => e.IdVenta)
                .HasColumnType("int(11)")
                .HasColumnName("id_venta");
            entity.Property(e => e.NombreCliente)
                .HasMaxLength(45)
                .HasColumnName("nombre_cliente");
            entity.Property(e => e.NombreEmpleado)
                .HasMaxLength(45)
                .HasColumnName("nombre_empleado");
            entity.Property(e => e.Precio).HasColumnName("precio");
            entity.Property(e => e.Producto)
                .HasMaxLength(45)
                .HasColumnName("producto");
            entity.Property(e => e.Subtotal).HasColumnName("subtotal");
            entity.Property(e => e.Total).HasColumnName("total");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
