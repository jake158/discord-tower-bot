﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tower.Persistence;

#nullable disable

namespace Tower.Persistence.Migrations
{
    [DbContext(typeof(TowerDbContext))]
    [Migration("20241116104527_AddMaliciousLinkInOffense")]
    partial class AddMaliciousLinkInOffense
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder, 1L, 1);

            modelBuilder.Entity("Tower.Persistence.Entities.GuildEntity", b =>
                {
                    b.Property<decimal>("GuildId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<bool>("IsPremium")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("UserId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("GuildId");

                    b.HasIndex("UserId");

                    b.ToTable("Guilds");
                });

            modelBuilder.Entity("Tower.Persistence.Entities.GuildSettingsEntity", b =>
                {
                    b.Property<decimal>("GuildId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal?>("AlertChannel")
                        .HasColumnType("decimal(20,0)");

                    b.Property<bool>("IsScanEnabled")
                        .HasColumnType("bit");

                    b.HasKey("GuildId");

                    b.ToTable("GuildSettings");
                });

            modelBuilder.Entity("Tower.Persistence.Entities.GuildStatsEntity", b =>
                {
                    b.Property<decimal>("GuildId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<DateTime>("JoinDate")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("LastScanDate")
                        .HasColumnType("datetime2");

                    b.Property<int>("MalwareFoundCount")
                        .HasColumnType("int");

                    b.Property<int>("ScansToday")
                        .HasColumnType("int");

                    b.Property<int>("TotalScans")
                        .HasColumnType("int");

                    b.HasKey("GuildId");

                    b.ToTable("GuildStats");
                });

            modelBuilder.Entity("Tower.Persistence.Entities.ScannedLinkEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<DateTimeOffset?>("ExpireTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<bool>("IsMalware")
                        .HasColumnType("bit");

                    b.Property<bool>("IsSuspicious")
                        .HasColumnType("bit");

                    b.Property<string>("LinkHash")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("nvarchar(64)");

                    b.Property<string>("MD5hash")
                        .HasMaxLength(32)
                        .HasColumnType("nvarchar(32)");

                    b.Property<DateTime>("ScannedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("LinkHash")
                        .IsUnique();

                    b.ToTable("ScannedLinks");
                });

            modelBuilder.Entity("Tower.Persistence.Entities.UserEntity", b =>
                {
                    b.Property<decimal>("UserId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<bool>("Blacklisted")
                        .HasColumnType("bit");

                    b.HasKey("UserId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Tower.Persistence.Entities.UserOffenseEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"), 1L, 1);

                    b.Property<decimal?>("GuildId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<string>("MaliciousLink")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<DateTime>("OffenseDate")
                        .HasColumnType("datetime2");

                    b.Property<int>("ScannedLinkId")
                        .HasColumnType("int");

                    b.Property<decimal>("UserId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("GuildId");

                    b.HasIndex("ScannedLinkId");

                    b.HasIndex("UserId");

                    b.ToTable("UserOffenses");
                });

            modelBuilder.Entity("Tower.Persistence.Entities.UserStatsEntity", b =>
                {
                    b.Property<decimal>("UserId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<DateTime?>("LastScanDate")
                        .HasColumnType("datetime2");

                    b.Property<int>("ScansToday")
                        .HasColumnType("int");

                    b.Property<int>("TotalScans")
                        .HasColumnType("int");

                    b.HasKey("UserId");

                    b.ToTable("UserStats");
                });

            modelBuilder.Entity("Tower.Persistence.Entities.GuildEntity", b =>
                {
                    b.HasOne("Tower.Persistence.Entities.UserEntity", "Owner")
                        .WithMany("OwnedGuilds")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Owner");
                });

            modelBuilder.Entity("Tower.Persistence.Entities.GuildSettingsEntity", b =>
                {
                    b.HasOne("Tower.Persistence.Entities.GuildEntity", "Guild")
                        .WithOne("Settings")
                        .HasForeignKey("Tower.Persistence.Entities.GuildSettingsEntity", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("Tower.Persistence.Entities.GuildStatsEntity", b =>
                {
                    b.HasOne("Tower.Persistence.Entities.GuildEntity", "Guild")
                        .WithOne("Stats")
                        .HasForeignKey("Tower.Persistence.Entities.GuildStatsEntity", "GuildId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");
                });

            modelBuilder.Entity("Tower.Persistence.Entities.UserOffenseEntity", b =>
                {
                    b.HasOne("Tower.Persistence.Entities.GuildEntity", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("Tower.Persistence.Entities.ScannedLinkEntity", "ScannedLink")
                        .WithMany()
                        .HasForeignKey("ScannedLinkId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Tower.Persistence.Entities.UserEntity", "User")
                        .WithMany("Offenses")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Guild");

                    b.Navigation("ScannedLink");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Tower.Persistence.Entities.UserStatsEntity", b =>
                {
                    b.HasOne("Tower.Persistence.Entities.UserEntity", "User")
                        .WithOne("Stats")
                        .HasForeignKey("Tower.Persistence.Entities.UserStatsEntity", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Tower.Persistence.Entities.GuildEntity", b =>
                {
                    b.Navigation("Settings")
                        .IsRequired();

                    b.Navigation("Stats")
                        .IsRequired();
                });

            modelBuilder.Entity("Tower.Persistence.Entities.UserEntity", b =>
                {
                    b.Navigation("Offenses");

                    b.Navigation("OwnedGuilds");

                    b.Navigation("Stats")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
