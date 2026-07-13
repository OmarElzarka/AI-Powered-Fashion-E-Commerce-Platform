using System;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Brand).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Category).IsRequired().HasMaxLength(100);
        builder.Property(x => x.SubCategory).HasMaxLength(100);
        builder.Property(x => x.ArticleType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Gender).HasMaxLength(50);
        builder.Property(x => x.AgeGroup).HasMaxLength(50);
        builder.Property(x => x.BaseColor).HasMaxLength(50);
        builder.Property(x => x.Season).HasMaxLength(50);
        builder.Property(x => x.Usage).HasMaxLength(100);
        builder.Property(x => x.Material).HasMaxLength(200);
        builder.Property(x => x.Pattern).HasMaxLength(100);
        builder.Property(x => x.Fit).HasMaxLength(100);
        builder.Property(x => x.StyleType).HasMaxLength(50);
        builder.Property(x => x.FashionType).HasMaxLength(50);
        builder.Property(x => x.Price).HasColumnType("decimal(18,2)");
        builder.Property(x => x.DiscountPercentage).HasColumnType("decimal(5,2)");
        builder.Property(x => x.ImageUrl).IsRequired().HasMaxLength(500);
        builder.Property(x => x.FrontImageUrl).HasMaxLength(500);
        builder.Property(x => x.BackImageUrl).HasMaxLength(500);
        builder.Property(x => x.SearchImageUrl).HasMaxLength(500);
        builder.Property(x => x.Tags).HasMaxLength(1000);
        builder.Property(x => x.Description).HasMaxLength(2000);

        // Indexes for fast filtering
        builder.HasIndex(x => x.Brand);
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.Gender);
        builder.HasIndex(x => x.Season);
        builder.HasIndex(x => x.BaseColor);
        builder.HasIndex(x => x.ArticleType);
        builder.HasIndex(x => x.Usage);
        builder.HasIndex(x => x.IsFeatured);
        builder.HasIndex(x => x.IsNewArrival);
        builder.HasIndex(x => x.Price);
        builder.HasIndex(x => x.Rating);
    }
}
