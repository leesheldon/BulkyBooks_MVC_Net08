using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Bulky.Models;

public class Product
{
    public int Id { get; set; }

    [MaxLength(40, ErrorMessage ="{0} cannot have more than {1} characters.")]
    public required string Title { get; set; }
    
    public string Description { get; set; }
    public required string ISBN { get; set; }
    public required string Author { get; set; }

    [Display(Name = "List Price")]
    [Range(1, 1000000, ErrorMessage ="{0} must be between {1} and {2}.")]
    public required double ListPrice { get; set; }

    [Display(Name = "Price for 1-50")]
    [Range(1, 1000, ErrorMessage ="{0} must be between {1} and {2}.")]
    public required double Price { get; set; }

    [Display(Name = "Price for 50+")]
    [Range(1, 100000, ErrorMessage ="{0} must be between {1} and {2}.")]
    public required double Price50 { get; set; }

    [Display(Name = "Price for 100+")]
    [Range(1, 10000000, ErrorMessage ="{0} must be between {1} and {2}.")]
    public required double Price100 { get; set; }

    public int CategoryId { get; set; }

    [ForeignKey("CategoryId")]
    [ValidateNever]
    public Category Category { get; set; }

    [ValidateNever]
    public List<ProductImage> ProductImages { get; set; }
}
