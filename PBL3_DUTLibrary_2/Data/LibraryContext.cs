using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PBL3_DUTLibrary.Models;

public partial class LibraryContext : DbContext
{
    public LibraryContext()
    {
    }

    public LibraryContext(DbContextOptions<LibraryContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccessHistory> AccessHistories { get; set; }

    public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; }

    public virtual DbSet<Book> Books { get; set; }

    public virtual DbSet<BookCopy> BookCopies { get; set; }

    public virtual DbSet<BookGenre> BookGenres { get; set; }

    public virtual DbSet<Borrow> Borrows { get; set; }

    public virtual DbSet<Genre> Genres { get; set; }

    public virtual DbSet<Loan> Loans { get; set; }

    public virtual DbSet<ProlongRequest> ProlongRequests { get; set; }

    public virtual DbSet<WebUser> WebUsers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=ADMIN-PC\\SQLEXPRESS;Initial Catalog=Library;Integrated Security=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccessHistory>(entity =>
        {
            entity.HasKey(e => new { e.AccessId, e.UserId }).HasName("pk");

            entity.ToTable("AccessHistory");

            entity.Property(e => e.LoginTime).HasColumnType("datetime");

            entity.HasOne(d => d.User).WithMany(p => p.AccessHistories)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__AccessHis__UserI__5629CD9C");
        });

        modelBuilder.Entity<AspNetRoleClaim>(entity =>
        {
            entity.Property(e => e.RoleId).HasMaxLength(450);
        });

        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.BookId).HasName("pk_books");

            entity.ToTable("books");

            entity.Property(e => e.BookId).ValueGeneratedNever();
            entity.Property(e => e.Amount).HasColumnName("amount");
            entity.Property(e => e.Author)
                .IsUnicode(false)
                .HasColumnName("author");
            entity.Property(e => e.Available)
                .HasDefaultValueSql("('1')")
                .HasColumnName("available");
            entity.Property(e => e.Description)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.Image)
                .IsUnicode(false)
                .HasColumnName("image");
            entity.Property(e => e.Title)
                .IsUnicode(false)
                .HasColumnName("title");

            entity.HasMany(d => d.Genres).WithMany(p => p.Books)
                .UsingEntity<Dictionary<string, object>>(
                    "Belong",
                    r => r.HasOne<BookGenre>().WithMany()
                        .HasForeignKey("GenreId")
                        .HasConstraintName("FK__Belong__GenreId__208CD6FA"),
                    l => l.HasOne<Book>().WithMany()
                        .HasForeignKey("BookId")
                        .HasConstraintName("FK__Belong__BookId__1F98B2C1"),
                    j =>
                    {
                        j.HasKey("BookId", "GenreId").HasName("pk6");
                        j.ToTable("Belong");
                    });
        });

        modelBuilder.Entity<BookCopy>(entity =>
        {
            entity.HasKey(e => new { e.BookCopyId, e.BookId }).HasName("pk3");

            entity.ToTable("book_copy");

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");

            entity.HasOne(d => d.Book).WithMany(p => p.BookCopies)
                .HasForeignKey(d => d.BookId)
                .HasConstraintName("FK__book_copy__BookI__5CD6CB2B");
        });

        modelBuilder.Entity<BookGenre>(entity =>
        {
            entity.HasKey(e => e.GenreId).HasName("PK__BookGenr__0385057E5A6F8F45");

            entity.ToTable("BookGenre");

            entity.Property(e => e.GenreId).ValueGeneratedNever();
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Borrow>(entity =>
        {
            entity.HasKey(e => e.BorrowId).HasName("PK__Borrow__4295F83FFECB09CE");

            entity.ToTable("Borrow");

            entity.Property(e => e.BorrowId).ValueGeneratedNever();
            entity.Property(e => e.Deadline).HasColumnName("deadline");
            entity.Property(e => e.ReturnedTime).HasColumnType("datetime");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Time).HasColumnType("datetime");

            entity.HasOne(d => d.Book).WithMany(p => p.Borrows)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Borrow__BookId__3D2915A8");

            entity.HasOne(d => d.User).WithMany(p => p.Borrows)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Borrow__UserId__3C34F16F");
        });

        modelBuilder.Entity<Genre>(entity =>
        {
            entity.HasKey(e => new { e.BookId, e.Genre1 }).HasName("pk4");

            entity.ToTable("genre");

            entity.Property(e => e.Genre1)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("genre");

            entity.HasOne(d => d.Book).WithMany(p => p.GenresNavigation)
                .HasForeignKey(d => d.BookId)
                .HasConstraintName("FK__genre__BookId__5FB337D6");
        });

        modelBuilder.Entity<Loan>(entity =>
        {
            entity.HasKey(e => e.LoanId).HasName("pk_loans");

            entity.ToTable("loans");

            entity.Property(e => e.LoanId)
                .ValueGeneratedNever()
                .HasColumnName("loan_id");
            entity.Property(e => e.Name)
                .IsUnicode(false)
                .HasColumnName("name");
            entity.Property(e => e.Price).HasColumnName("price");
        });

        modelBuilder.Entity<ProlongRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__ProlongR__33A8517A77761C9C");

            entity.ToTable("ProlongRequest");

            entity.Property(e => e.Reason)
                .HasMaxLength(1000)
                .IsUnicode(false);

            entity.HasOne(d => d.Borrow).WithMany(p => p.ProlongRequests)
                .HasForeignKey(d => d.BorrowId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__ProlongRe__Borro__4F47C5E3");
        });

        modelBuilder.Entity<WebUser>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__WebUser__1788CC4CC2A485BA");

            entity.ToTable("WebUser");

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.Image)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Password)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("password");
            entity.Property(e => e.RealName)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Sdt).HasColumnName("sdt");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasMany(d => d.Books).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "WishList",
                    r => r.HasOne<Book>().WithMany()
                        .HasForeignKey("BookId")
                        .HasConstraintName("FK__WishList__BookId__71D1E811"),
                    l => l.HasOne<WebUser>().WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("FK__WishList__UserId__70DDC3D8"),
                    j =>
                    {
                        j.HasKey("UserId", "BookId").HasName("pk5");
                        j.ToTable("WishList");
                    });
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
