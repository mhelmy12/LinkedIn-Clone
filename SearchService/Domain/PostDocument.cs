using System;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;

namespace SearchService.Domain;

public class PostDocument
{
    public long Id { get; set; }
    public string AuthorId { get; set; }
    public string? Content { get; set; }
    public List<string> Hashtags { get; set; } = new();
    public List<string> MentionedUserIds { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime IndexedAt { get; set; }
}


public class PostDocumentConfiguration
{
    public static void Configure(IndexSettingsDescriptor settings) { }

    public static void ConfigureMappings(TypeMappingDescriptor<PostDocument> map)
    {
        
          map.Properties(p => p
                .Keyword(k => k.Id)
                .Keyword(k => k.AuthorId)

                .Text(t => t.Content, txt => txt
                    .Analyzer("standard")
                    .Fields(f => f
                        .Keyword("keyword", kw => kw.IgnoreAbove(256))))
                
                .Keyword(k => k.Hashtags)
                .Keyword(k => k.MentionedUserIds)

                .Date(d => d.CreatedAt)
                .Date(d => d.UpdatedAt)
                .Date(d => d.IndexedAt));
    }
}