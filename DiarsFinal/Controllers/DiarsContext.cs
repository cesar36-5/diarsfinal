using System;
using System.Collections.Generic;
using DiarsFinal.Models;
using Microsoft.EntityFrameworkCore;

namespace DiarsFinal.Controllers;

public partial class DiarsContext : DbContext
{
    public DiarsContext()
    {
    }

    public DiarsContext(DbContextOptions<DiarsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Categorium> Categoria { get; set; }

    public virtual DbSet<Compatibilidad> Compatibilidades { get; set; }

    public virtual DbSet<DetallePedido> DetallePedidos { get; set; }

    public virtual DbSet<DetallePersonalizado> DetallePersonalizados { get; set; }

    public virtual DbSet<Especificacione> Especificaciones { get; set; }

    public virtual DbSet<Pedido> Pedidos { get; set; }

    public virtual DbSet<Producto> Productos { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Categorium>(entity =>
        {
            entity.HasKey(e => e.IdCategoria).HasName("XPKCategoria");

            entity.Property(e => e.Nombre)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<DetallePedido>(entity =>
        {
            entity.HasKey(e => new { e.IdProducto, e.IdPedido }).HasName("XPKDetallePedido");

            entity.ToTable("DetallePedido");

            entity.HasOne(d => d.IdPedidoNavigation).WithMany(p => p.DetallePedidos)
                .HasForeignKey(d => d.IdPedido)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("R_7");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.DetallePedidos)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("R_3");
        });

        modelBuilder.Entity<DetallePersonalizado>(entity =>
        {
            entity.HasKey(e => new { e.IdPedido, e.IdProducto }).HasName("XPKDetallePersonalizado");

            entity.ToTable("DetallePersonalizado");

            entity.Property(e => e.Comentario)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.IdPedidoNavigation).WithMany(p => p.DetallePersonalizados)
                .HasForeignKey(d => d.IdPedido)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("R_6");

            entity.HasOne(d => d.IdProductoNavigation).WithMany(p => p.DetallePersonalizados)
                .HasForeignKey(d => d.IdProducto)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("R_8");
        });

        modelBuilder.Entity<Especificacione>(entity =>
        {
            entity.HasKey(e => e.IdEspecificaciones).HasName("XPKEspecificaciones");

            entity.Property(e => e.DiasTrabajo)
                .HasMaxLength(7)
                .IsUnicode(false)
                .IsFixedLength();
        });

        modelBuilder.Entity<Pedido>(entity =>
        {
            entity.HasKey(e => e.IdPedido).HasName("XPKPedido");

            entity.ToTable("Pedido");

            entity.Property(e => e.Comentario)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Dni)
                .HasMaxLength(8)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("DNI");
            entity.Property(e => e.Estado)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.FechaCreacion).HasColumnType("datetime");
            entity.Property(e => e.FechaEntrega).HasColumnType("datetime");
            entity.Property(e => e.Nombre)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Precio).HasColumnType("decimal(7, 2)");
            entity.Property(e => e.Telefono)
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Tipo)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
        });

        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(e => e.IdProducto).HasName("XPKProducto");

            entity.ToTable("Producto");

            entity.Property(e => e.Activo)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Descripcion)
                .HasMaxLength(128)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Precio).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.RutaImagen)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Tipo)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();

            entity.HasOne(d => d.IdCategoriaNavigation).WithMany(p => p.Productos)
                .HasForeignKey(d => d.IdCategoria)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("R_1");

            
        });

        modelBuilder.Entity<Compatibilidad>(entity =>
        {
            entity.HasKey(e => new { e.IdProductoA, e.IdProductoB })
                .HasName("XPKCompatibilidad");

            entity.ToTable("Compatibilidad");

            entity.HasOne(d => d.ProductoA)
                .WithMany(p => p.Compatibilidades)
                .HasForeignKey(d => d.IdProductoA)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("R_20");

            entity.HasOne(d => d.ProductoB)
                .WithMany()
                .HasForeignKey(d => d.IdProductoB)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("R_23");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.IdUsuario).HasName("XPKUsuario");

            entity.ToTable("Usuario");

            entity.Property(e => e.Contrasena)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Usuario1)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Usuario");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
