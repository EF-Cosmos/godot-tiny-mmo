using System;

namespace Game.GameService.DTOs;

public class WorldDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public int Port { get; set; }
    public int CurrentPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public string Status { get; set; }
}

public class CharacterDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Level { get; set; }
    public int SkinColor { get; set; }
    public int HairStyle { get; set; }
    public int HairColor { get; set; }
    public int ShirtColor { get; set; }
    public int PantsColor { get; set; }
}

public class CreateCharacterRequest
{
    public string Name { get; set; }
    public int SkinColor { get; set; }
    public int HairStyle { get; set; }
    public int HairColor { get; set; }
    public int ShirtColor { get; set; }
    public int PantsColor { get; set; }
}

public class EnterWorldRequest
{
    public int CharacterId { get; set; }
    public string WorldId { get; set; }
}

public class EnterWorldResponse
{
    public string Token { get; set; }
    public string Host { get; set; }
    public int Port { get; set; }
    public int CharacterId { get; set; }
}
