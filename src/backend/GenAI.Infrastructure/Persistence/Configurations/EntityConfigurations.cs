using GenAI.Domain.Entities;
using GenAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;

namespace GenAI.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.FileName).HasMaxLength(500).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(d => d.StoragePath).HasMaxLength(1000).IsRequired();
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasMany(d => d.Chunks).WithOne(c => c.Document).HasForeignKey(c => c.DocumentId);
    }
}

public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("document_chunks");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Content).IsRequired();
        builder.Property(c => c.Embedding)
            .HasColumnType("vector(1536)")
            .HasConversion(
                v => v == null ? null : new Vector(v),
                v => v == null ? null : v.ToArray());
        builder.HasIndex(c => c.DocumentId);
    }
}

public class AgentRunConfiguration : IEntityTypeConfiguration<AgentRun>
{
    public void Configure(EntityTypeBuilder<AgentRun> builder)
    {
        builder.ToTable("agent_runs");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.AgentName).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(30);
        builder.HasMany(r => r.Steps).WithOne(s => s.AgentRun).HasForeignKey(s => s.AgentRunId);
    }
}

public class AgentStepConfiguration : IEntityTypeConfiguration<AgentStep>
{
    public void Configure(EntityTypeBuilder<AgentStep> builder)
    {
        builder.ToTable("agent_steps");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.StepType).HasMaxLength(50).IsRequired();
    }
}
