using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bulky.Models;

public class Category
{
    public int Id { get; set; }

    [DisplayName("Category Name")]
    [MaxLength(30, ErrorMessage ="{0} cannot have more than 30 characters.")]
    public required string Name { get; set; }

    [DisplayName("Display Order")]
    [Range(1, int.MaxValue, ErrorMessage ="{0} must be between {1} and {2}.")]
    public int DisplayOrder { get; set; }
}
