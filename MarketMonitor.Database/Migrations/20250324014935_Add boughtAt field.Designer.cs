﻿// <auto-generated />
using System;
using MarketMonitor.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace MarketMonitor.Database.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20250324014935_Add boughtAt field")]
    partial class AddboughtAtfield
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("MarketMonitor.Database.Entities.CharacterEntity", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<ulong>("Id"));

                    b.Property<string>("DatacenterName")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<bool>("IsVerified")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("LodestoneId")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<int?>("NotificationRegionId")
                        .HasColumnType("int");

                    b.Property<Guid?>("VerificationValue")
                        .HasColumnType("char(36)");

                    b.HasKey("Id");

                    b.HasAlternateKey("Name");

                    b.HasIndex("DatacenterName");

                    b.HasIndex("NotificationRegionId");

                    b.ToTable("Characters");
                });

            modelBuilder.Entity("MarketMonitor.Database.Entities.DatacenterEntity", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("Region")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Name");

                    b.ToTable("Datacenters");
                });

            modelBuilder.Entity("MarketMonitor.Database.Entities.ItemEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Icon")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("varchar(256)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("varchar(128)");

                    b.HasKey("Id");

                    b.ToTable("Items");
                });

            modelBuilder.Entity("MarketMonitor.Database.Entities.ListingEntity", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<int>("Flags")
                        .HasColumnType("int");

                    b.Property<bool>("IsHq")
                        .HasColumnType("tinyint(1)");

                    b.Property<bool>("IsNotified")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("ItemId")
                        .HasColumnType("int");

                    b.Property<int>("PricePerUnit")
                        .HasColumnType("int");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<string>("RetainerName")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<ulong>("RetainerOwnerId")
                        .HasColumnType("bigint unsigned");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("WorldId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ItemId");

                    b.HasIndex("WorldId");

                    b.HasIndex("RetainerName", "RetainerOwnerId");

                    b.ToTable("Listings");
                });

            modelBuilder.Entity("MarketMonitor.Database.Entities.PurchaseEntity", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint unsigned");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<ulong>("Id"));

                    b.Property<ulong>("CharacterId")
                        .HasColumnType("bigint unsigned");

                    b.Property<bool>("IsHq")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("ItemId")
                        .HasColumnType("int");

                    b.Property<int>("PricePerUnit")
                        .HasColumnType("int");

                    b.Property<DateTime>("PurchasedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("Quantity")
                        .HasColumnType("int");

                    b.Property<int>("WorldId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("CharacterId");

                    b.HasIndex("ItemId");

                    b.HasIndex("WorldId");

                    b.ToTable("Purchases");
                });

            modelBuilder.Entity("MarketMonitor.Database.Entities.RetainerEntity", b =>
                {
                    b.Property<string>("Name")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<ulong>("OwnerId")
                        .HasColumnType("bigint unsigned");

                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<bool>("IsVerified")
                        .HasColumnType("tinyint(1)");

                    b.Property<int?>("VerificationItem")
                        .HasColumnType("int");

                    b.Property<int?>("VerificationPrice")
                        .HasColumnType("int");

                    b.HasKey("Name", "OwnerId");

                    b.HasIndex("OwnerId");

                    b.ToTable("Retainers");
                });

            modelBuilder.Entity("MarketMonitor.Database.Entities.SaleEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("BoughtAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("BuyerName")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("ListingId")
                        .IsRequired()
                        .HasColumnType("varchar(64)");

                    b.HasKey("Id");

                    b.HasIndex("ListingId");

                    b.ToTable("Sales");
                });

            modelBuilder.Entity("MarketMonitor.Database.Entities.WorldEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("DatacenterName")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("DatacenterName");

                    b.ToTable("Worlds");
                });

            modelBuilder.Entity("MarketMonitor.Database.Entities.CharacterEntity", b =>
                {
                    b.HasOne("MarketMonitor.Database.Entities.DatacenterEntity", "Datacenter")
                        .WithMany()
                        .HasForeignKey("DatacenterName")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MarketMonitor.Database.Entities.WorldEntity", "NotificationRegion")
                        .WithMany()
                        .HasForeignKey("NotificationRegionId");

                    b.Navigation("Datacenter");

                    b.Navigation("NotificationRegion");
                });

            modelBuilder.Entity("MarketMonitor.Database.Entities.ListingEntity", b =>
                {
                    b.HasOne("MarketMonitor.Database.Entities.ItemEntity", "Item")
                        .WithMany()
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MarketMonitor.Database.Entities.WorldEntity", "World")
                        .WithMany()
                        .HasForeignKey("WorldId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MarketMonitor.Database.Entities.RetainerEntity", "Retainer")
                        .WithMany("Listings")
                        .HasForeignKey("RetainerName", "RetainerOwnerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Item");

                    b.Navigation("Retainer");

                    b.Navigation("World");
                });

            modelBuilder.Entity("MarketMonitor.Database.Entities.PurchaseEntity", b =>
                {
                    b.HasOne("MarketMonitor.Database.Entities.CharacterEntity", "Character")
                        .WithMany()
                        .HasForeignKey("CharacterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MarketMonitor.Database.Entities.ItemEntity", "Item")
                        .WithMany()
                        .HasForeignKey("ItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("MarketMonitor.Database.Entities.WorldEntity", "World")
                        .WithMany()
                        .HasForeignKey("WorldId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Character");

                    b.Navigation("Item");

                    b.Navigation("World");
                });

            modelBuilder.Entity("MarketMonitor.Database.Entities.RetainerEntity", b =>
                {
                    b.HasOne("MarketMonitor.Database.Entities.CharacterEntity", "Owner")
                        .WithMany("Retainers")
                        .HasForeignKey("OwnerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Owner");
                });

            modelBuilder.Entity("MarketMonitor.Database.Entities.SaleEntity", b =>
                {
                    b.HasOne("MarketMonitor.Database.Entities.ListingEntity", "Listing")
                        .WithMany()
                        .HasForeignKey("ListingId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Listing");
                });

            modelBuilder.Entity("MarketMonitor.Database.Entities.WorldEntity", b =>
                {
                    b.HasOne("MarketMonitor.Database.Entities.DatacenterEntity", "Datacenter")
                        .WithMany("Worlds")
                        .HasForeignKey("DatacenterName")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Datacenter");
                });

            modelBuilder.Entity("MarketMonitor.Database.Entities.CharacterEntity", b =>
                {
                    b.Navigation("Retainers");
                });

            modelBuilder.Entity("MarketMonitor.Database.Entities.DatacenterEntity", b =>
                {
                    b.Navigation("Worlds");
                });

            modelBuilder.Entity("MarketMonitor.Database.Entities.RetainerEntity", b =>
                {
                    b.Navigation("Listings");
                });
#pragma warning restore 612, 618
        }
    }
}
