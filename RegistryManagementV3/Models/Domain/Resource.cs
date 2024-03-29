﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace RegistryManagementV3.Models.Domain
{
    public class Resource
    {
        [Key]
        public long Id { get; set; }
        
        [Column(TypeName = "NVARCHAR(100)")]
        
        public string Title { get; set; }
        
        [Column(TypeName = "NVARCHAR(400)")]
        public string Description { get; set; }
        
        [Column(TypeName = "NVARCHAR(50)")]
        public string Language { get; set; }
        
        [Column(TypeName = "NVARCHAR(50)")]
        public string Format { get; set; }
        public DateTime CreationTimestamp { get; set; }
        public DateTime? ApprovalTimestamp { get; set; }
        [DefaultValue(5)]
        [Range(1, 10)]
        public int SecurityLevel { get; set; } = 5;

        public int Priority { get; set; } = 5;
        public ResourceStatus ResourceStatus { get; set; }

        public string? AuthorId { get; set; }
        
        [ForeignKey("AuthorId")]
        public virtual ApplicationUser? Author { get; set; }

        public bool IsEditable { get; set; } = false;

        [Column(TypeName = "NVARCHAR(50)")] 
        public string FileName { get; set; }
        
        [Column(TypeName = "NVARCHAR(1000)")]
        public string Location { get; set; }
        public long? CatalogId { get; set; }
        [ForeignKey("CatalogId")]
        public virtual Catalog Catalog { get; set; }
        
        public virtual List<TagResources> TagResources { get; set; }
        
        [NotMapped]
        public ICollection<Tag> Tags => TagResources.Select(tag => new Tag { TagValue = tag.Tag.TagValue}).ToList();
    }
}