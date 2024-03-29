﻿using DarkDhamon.Common.EntityFramework.Model;

namespace SlipMap.Model.MapElements;

public class Planet:IEntity<int>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}