﻿namespace Wowthing.Tool.Models.TransmogSets;

public class DataTransmogSetGroup
{
    public bool? Completionist { get; set; }
    public string Name { get; set; }
    public string Prefix { get; set; }
    public string Type { get; set; }
    public Dictionary<string, string> BonusIds { get; set; }
    public List<string> MatchTags { get; set; }
}