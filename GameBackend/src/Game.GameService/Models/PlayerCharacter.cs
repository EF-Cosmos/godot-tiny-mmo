using System;
using System.ComponentModel.DataAnnotations;

namespace Game.GameService.Models;

public class PlayerCharacter
{
    public int Id { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(32)]
    public string Name { get; set; }
    
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
    
    // Appearance
    public int SkinColor { get; set; } = 0;
    public int HairStyle { get; set; } = 0;
    public int HairColor { get; set; } = 0;
    public int ShirtColor { get; set; } = 0;
    public int PantsColor { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastPlayedAt { get; set; } = DateTime.UtcNow;
}
